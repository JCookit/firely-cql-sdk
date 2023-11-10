/* 
 * Copyright (c) 2023, NCQA and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-cql-sdk/main/LICENSE
 */

using Hl7.Cql.Runtime;

using Microsoft.SqlServer.TransactSql.ScriptDom;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using elm = Hl7.Cql.Elm;

namespace Hl7.Cql.Compiler
{
    internal class ScopedExpression
    {
        public Expression Expression { get; }
        public elm.Element ElmExpression { get; }

        public ScopedExpression(Expression expression, elm.Element elmExpression)
        {
            Expression = expression;
            ElmExpression = elmExpression;
        }
    }


    /// <summary>
    /// The ExpressionBuilderContext class maintains scope information for the traversal of ElmPackage statements during <see cref="ExpressionBuilder.Build"/>.
    /// </summary>
    /// <remarks>
    /// The scope information in this class is useful for <see cref="IExpressionMutator"/> and is supplied to <see cref="IExpressionMutator.Mutate(Expression, elm.Element, ExpressionBuilderContext)"/>.
    /// </remarks>
    internal class ExpressionBuilderContext : ExpressionBuilderContextBase<ExpressionBuilderContext, ExpressionBuilder>
    {
        internal bool HasScope(string elmAlias) => Scopes.ContainsKey(elmAlias);

        /// <summary>
        /// Contains query aliases and let declarations, and any other symbol that is now "in scope"
        /// </summary>
        protected IDictionary<string, ScopedExpression> Scopes { get; } = new Dictionary<string, ScopedExpression>();


        internal ExpressionBuilderContext(ExpressionBuilder builder,
            ParameterExpression contextParameter,
            DefinitionDictionary<LambdaExpression> definitions,
            IDictionary<string, string> localLibraryIdentifiers)
            : base(builder, localLibraryIdentifiers)
        {
            RuntimeContextParameter = contextParameter;
            Definitions = definitions;
        }

        private ExpressionBuilderContext(ExpressionBuilderContext other)
            : base(other.Builder, other.LocalLibraryIdentifiers, other.ImpliedAlias, other.Predecessors)
        {
            Libraries = other.Libraries;
            RuntimeContextParameter = other.RuntimeContextParameter;
            Definitions = other.Definitions;
            Operands = other.Operands;
            Scopes = other.Scopes;
        }

        private ExpressionBuilderContext(ExpressionBuilderContext other,
            Dictionary<string, ScopedExpression> scopes) : this(other)
        {
            Scopes = scopes;
        }

        protected override ExpressionBuilderContext CopyForDeeper()
        {
            return new ExpressionBuilderContext(this);
        }


        /// <summary>
        /// Gets the <see cref="ParameterExpression"/> which is passed to the <see cref="OperatorBinding"/> for operators to use.        
        /// </summary>
        /// <remarks>
        /// Having access to the <see cref="CqlContext"/> is almost always necessary when implementing operators because the context contains all comparers, value sets, CQL parameter values, and other data provided at runtime.
        /// </remarks>
        public ParameterExpression RuntimeContextParameter { get; }

        internal DefinitionDictionary<LambdaExpression> Definitions { get; }

        /// <summary>
        /// Parameters for function definitions.
        /// </summary>
        internal IDictionary<string, ParameterExpression> Operands { get; } = new Dictionary<string, ParameterExpression>();

        internal IDictionary<string, DefinitionDictionary<LambdaExpression>> Libraries { get; } = new Dictionary<string, DefinitionDictionary<LambdaExpression>>();

        internal Expression GetScopeExpression(string elmAlias)
        {
            var normalized = NormalizeIdentifier(elmAlias!)!;
            if (Scopes.TryGetValue(normalized, out var expression))
                return expression.Expression;
            else throw new ArgumentException($"The scope alias {elmAlias}, normalized to {normalized}, is not present in the scopes dictionary.", nameof(elmAlias));
        }

        internal ScopedExpression GetScope(string elmAlias)
        {
            var normalized = NormalizeIdentifier(elmAlias!)!;
            if (Scopes.TryGetValue(normalized, out var expression))
                return expression;
            else throw new ArgumentException($"The scope alias {elmAlias}, normalized to {normalized}, is not present in the scopes dictionary.", nameof(elmAlias));
        }

        internal Expression? ImpliedAliasExpression => ImpliedAlias != null ? GetScopeExpression(ImpliedAlias) : null;

        /// <summary>
        /// Creates a copy with the scopes provided.
        /// </summary>
        internal ExpressionBuilderContext WithScopes(params KeyValuePair<string, ScopedExpression>[] kvps)
        {
            var scopes = new Dictionary<string, ScopedExpression>(Scopes);
            if (Builder.Settings.AllowScopeRedefinition)
            {
                foreach (var kvp in kvps)
                {
                    var normalized = NormalizeIdentifier(kvp.Key);
                    if (!string.IsNullOrWhiteSpace(normalized))
                    {
                        scopes[normalized] = kvp.Value;
                    }
                    else throw new InvalidOperationException();
                }
            }
            else
            {
                foreach (var kvp in kvps)
                {
                    var normalized = NormalizeIdentifier(kvp.Key);
                    if (!string.IsNullOrWhiteSpace(normalized))
                    {
                        if (scopes.ContainsKey(normalized))
                            throw new InvalidOperationException($"Scope {kvp.Key}, normalized to {NormalizeIdentifier(kvp.Key)}, is already defined and this builder does not allow scope redefinition.  Check the CQL source, or set {nameof(ExpressionBuilderSettings.AllowScopeRedefinition)} to true");
                        scopes.Add(normalized, kvp.Value);
                    }
                    else throw new InvalidOperationException();
                }
            }
            var subContext = new ExpressionBuilderContext(this, scopes);
            return subContext;
        }

        // TODO: does this get hoisted into the base class?   are Scopes common handling?
        internal ExpressionBuilderContext WithImpliedAlias(string aliasName, Expression linqExpression, elm.Element elmExpression)
        {
            var subContext = WithScopes(new KeyValuePair<string, ScopedExpression>(aliasName, new ScopedExpression(linqExpression, elmExpression)));
            subContext.ImpliedAlias = aliasName;

            return subContext;
        }

    }
}
