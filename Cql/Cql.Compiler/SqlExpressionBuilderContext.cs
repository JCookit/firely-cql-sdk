/* 
 * Copyright (c) 2023, NCQA and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-cql-sdk/main/LICENSE
 */

using Hl7.Cql.Runtime;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.TransactSql.ScriptDom;

using System;
using System.Collections.Generic;
using System.Linq;
using elm = Hl7.Cql.Elm;

namespace Hl7.Cql.Compiler
{
    /// <summary>
    /// The SqlExpressionBuilderContext class maintains scope information for the traversal of ElmPackage statements during <see cref="SqlExpressionBuilder.Build"/>.
    /// 
    /// in sql, it also contains state information for the currently-building sql construct
    /// </summary>
    internal class SqlExpressionBuilderContext : ExpressionBuilderContextBase<SqlExpressionBuilderContext, SqlExpressionBuilder>
    {
        private TableReference? currentFromTables;

        public TableReference? FromTables => currentFromTables;

        internal DefinitionDictionary<TSqlFragment> Definitions { get; }

        /// <summary>
        /// Parameters for function definitions.
        /// </summary>
        //internal IDictionary<string, ParameterExpression> Operands { get; } = new Dictionary<string, ParameterExpression>();

        internal IDictionary<string, DefinitionDictionary<TSqlFragment>> Libraries { get; } = new Dictionary<string, DefinitionDictionary<TSqlFragment>>();

        internal SqlExpressionBuilderContext(
            SqlExpressionBuilder builder,
            IDictionary<string, string> localLibraryIdentifiers,
            DefinitionDictionary<TSqlFragment> definitions)
            : base(builder, localLibraryIdentifiers)
        {
            this.Definitions = definitions;

            AddNullTableReference();
        }

        private SqlExpressionBuilderContext(
            SqlExpressionBuilderContext other)
            : base(other.Builder, other.LocalLibraryIdentifiers, other.ImpliedAlias, other.Predecessors.ToList())
        {
            Libraries = other.Libraries;
            //RuntimeContextParameter = other.RuntimeContextParameter;
            Definitions = other.Definitions;
            currentFromTables = other.FromTables;

            //Operands = other.Operands;
            //Scopes = other.Scopes;
            Predecessors = other.Predecessors.ToList(); // copy it
        }

        protected override SqlExpressionBuilderContext CopyForDeeper()
        {
            return new SqlExpressionBuilderContext(this);
        }

        private void AddNullTableReference()
        {
            this.currentFromTables = new QueryDerivedTable
            {
                QueryExpression = new QuerySpecification
                {
                    SelectElements =
                                {
                                    new SelectScalarExpression
                                    {
                                        Expression = new NullLiteral(),
                                        ColumnName = new IdentifierOrValueExpression
                                        {
                                            Identifier = new Identifier { Value = "unused_column" }
                                        }
                                    }
                                }
                },
                Alias = new Identifier { Value = "UNUSED" }
            };
        }

        internal void AddJoinFunctionReference(string functionName, string alias)
        {
            // TODO:  need to change singleton of the From clause --- right now this is just changing the deepest context

            var functionTableReference = new SchemaObjectFunctionTableReference
            {
                SchemaObject = new SchemaObjectName
                {
                    Identifiers =
                    {
                        new Identifier { Value = functionName }
                    }
                },
                Alias = new Identifier { Value = alias }
            };

            if (this.currentFromTables == null)
            {
                throw new InvalidOperationException("Table clause should already be populated");
            }
            else
            {
                this.currentFromTables = new UnqualifiedJoin
                {
                    FirstTableReference = this.currentFromTables,
                    SecondTableReference = functionTableReference,
                    UnqualifiedJoinType = UnqualifiedJoinType.CrossApply
                };
            }
        }

        //private SqlExpressionBuilderContext(ExpressionBuilderContext other,
        //    Dictionary<string, (Expression, elm.Element)> scopes) : this(other)
        //{
        //    Scopes = scopes;
        //}

        //internal Expression GetScopeExpression(string elmAlias)
        //{
        //    var normalized = NormalizeIdentifier(elmAlias!)!;
        //    if (Scopes.TryGetValue(normalized, out var expression))
        //        return expression.Item1;
        //    else throw new ArgumentException($"The scope alias {elmAlias}, normalized to {normalized}, is not present in the scopes dictionary.", nameof(elmAlias));
        //}

        //internal (Expression, elm.Element) GetScope(string elmAlias)
        //{
        //    var normalized = NormalizeIdentifier(elmAlias!)!;
        //    if (Scopes.TryGetValue(normalized, out var expression))
        //        return expression;
        //    else throw new ArgumentException($"The scope alias {elmAlias}, normalized to {normalized}, is not present in the scopes dictionary.", nameof(elmAlias));
        //}

        //internal Expression? ImpliedAliasExpression => ImpliedAlias != null ? GetScopeExpression(ImpliedAlias) : null;

        ///// <summary>
        ///// Contains query aliases and let declarations, and any other symbol that is now "in scope"
        ///// </summary>
        //private IDictionary<string, (Expression, elm.Element)> Scopes { get; } = new Dictionary<string, (Expression, elm.Element)>();


        //internal bool HasScope(string elmAlias) => Scopes.ContainsKey(elmAlias);


        ///// <summary>
        ///// Creates a copy with the scopes provided.
        ///// </summary>
        //internal ExpressionBuilderContext WithScopes(params KeyValuePair<string, (Expression, elm.Element)>[] kvps)
        //{
        //    var scopes = new Dictionary<string, (Expression, elm.Element)>(Scopes);
        //    if (Builder.Settings.AllowScopeRedefinition)
        //    {
        //        foreach (var kvp in kvps)
        //        {
        //            var normalized = NormalizeIdentifier(kvp.Key);
        //            if (!string.IsNullOrWhiteSpace(normalized))
        //            {
        //                scopes[normalized] = kvp.Value;
        //            }
        //            else throw new InvalidOperationException();
        //        }
        //    }
        //    else
        //    {
        //        foreach (var kvp in kvps)
        //        {
        //            var normalized = NormalizeIdentifier(kvp.Key);
        //            if (!string.IsNullOrWhiteSpace(normalized))
        //            {
        //                if (scopes.ContainsKey(normalized))
        //                    throw new InvalidOperationException($"Scope {kvp.Key}, normalized to {NormalizeIdentifier(kvp.Key)}, is already defined and this builder does not allow scope redefinition.  Check the CQL source, or set {nameof(ExpressionBuilderSettings.AllowScopeRedefinition)} to true");
        //                scopes.Add(normalized, kvp.Value);
        //            }
        //            else throw new InvalidOperationException();
        //        }
        //    }
        //    var subContext = new ExpressionBuilderContext(this, scopes);
        //    return subContext;
        //}

        //internal ExpressionBuilderContext WithImpliedAlias(string aliasName, Expression linqExpression, elm.Element elmExpression)
        //{
        //    var subContext = WithScopes(new KeyValuePair<string, (Expression, elm.Element)>(aliasName, (linqExpression, elmExpression)));
        //    subContext.ImpliedAlias = aliasName;

        //    return subContext;
        //}

    }
}
