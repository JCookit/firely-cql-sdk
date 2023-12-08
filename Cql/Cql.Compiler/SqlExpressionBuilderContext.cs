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
using static System.Formats.Asn1.AsnWriter;

using elm = Hl7.Cql.Elm;

namespace Hl7.Cql.Compiler
{
    internal class SqlOutputContext
    {
        private TableReference? currentFromTables;

        //public TableReference? FromTables => currentFromTables;

        // TODO: this might be obsolete --- because the FROM clause is now carried along with each expression (mostly?)
        public FromClause FromClause => new FromClause { TableReferences = { currentFromTables } };

        public SqlOutputContext()
        {
            AddNullTableReference();
        }   

        /// <summary>
        /// builds the first part of the From clause (an empty table)
        /// TODO: not clear this is needed in all cases, but certainly for scalar math
        /// </summary>
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

        /// <summary>
        /// Add a table reference to from clause (which corresponds to a scalar function call)
        /// 
        /// TODO: think about correct way to de-dupe
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="alias"></param>
        /// <exception cref="InvalidOperationException"></exception>
        internal void AddJoinFunctionReference(string functionName, string alias)
        {
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
    }

    /// <summary>
    /// The SqlExpressionBuilderContext class maintains scope information for the traversal of ElmPackage statements during <see cref="SqlExpressionBuilder.Build"/>.
    /// 
    /// in sql, it also contains state information for the currently-building sql construct
    /// </summary>
    internal class SqlExpressionBuilderContext : ExpressionBuilderContextBase<SqlExpressionBuilderContext, SqlExpressionBuilder, TSqlFragment>
    {
        internal DefinitionDictionary<TSqlFragment> Definitions { get; }

        public SqlOutputContext OutputContext { get; } = new SqlOutputContext();

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
        }

        private SqlExpressionBuilderContext(
            SqlExpressionBuilderContext other,
            Dictionary<string, ScopedExpressionBase>? scopes = null)
            : base(other.Builder, other.LocalLibraryIdentifiers, other.ImpliedAlias, other.Predecessors.ToList(), other.Scopes)
        {
            Libraries = other.Libraries;
            //RuntimeContextParameter = other.RuntimeContextParameter;
            Definitions = other.Definitions;
            OutputContext = other.OutputContext;    

            //Operands = other.Operands;
            //Scopes = other.Scopes;
            Predecessors = other.Predecessors.ToList(); // copy it

            if (scopes != null)
                Scopes = scopes;
        }

        protected override SqlExpressionBuilderContext Copy(Dictionary<string, ScopedExpressionBase>? scopes)
        {
            return new SqlExpressionBuilderContext(this, scopes);
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

        /// <summary>
        /// TODO: delightfully inefficient - it copies the list in order to leverage the base class behavior.  Figure out a better way
        /// </summary>
        /// <param name="kvps"></param>
        /// <returns></returns>
        internal SqlExpressionBuilderContext WithScopes(params KeyValuePair<string, ScopedSqlExpression>[] kvps)
        {
            KeyValuePair<string, ScopedExpressionBase>[] baseType;

            baseType = kvps.Select(kvp => new KeyValuePair<string, ScopedExpressionBase>(kvp.Key, kvp.Value)).ToArray();

            return base.WithScopes(baseType);
        }

        new internal ScopedSqlExpression GetScope(string elmAlias)
        {
            var normalized = NormalizeIdentifier(elmAlias!)!;
            if (Scopes.TryGetValue(normalized, out var expression))
                return expression as ScopedSqlExpression ?? throw new InvalidOperationException();
            else throw new ArgumentException($"The scope alias {elmAlias}, normalized to {normalized}, is not present in the scopes dictionary.", nameof(elmAlias));
        }


        //internal ExpressionBuilderContext WithImpliedAlias(string aliasName, Expression linqExpression, elm.Element elmExpression)
        //{
        //    var subContext = WithScopes(new KeyValuePair<string, (Expression, elm.Element)>(aliasName, (linqExpression, elmExpression)));
        //    subContext.ImpliedAlias = aliasName;

        //    return subContext;
        //}

    }
}
