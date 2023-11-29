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
using System.Linq;
using System.Linq.Expressions;

using elm = Hl7.Cql.Elm;

namespace Hl7.Cql.Compiler
{
    internal class ScopedExpressionBase
    {
        public elm.Element ElmExpression { get; }

        public Type Type { get; }

        public ScopedExpressionBase(elm.Element elmExpression, Type type)
        {
            ElmExpression = elmExpression;
            Type = type;
        }
    }

    internal class ScopedExpression : ScopedExpressionBase
    {
        public Expression Expression { get; }

        public ScopedExpression(Expression expression, elm.Element elmExpression)
            : base(elmExpression, expression.Type)
        {
            Expression = expression;
        }
    }

    internal class ScopedSqlExpression : ScopedExpressionBase
    {
        public TSqlFragment SqlExpressionBuilder { get; }

        public ScopedSqlExpression(TSqlFragment sql, elm.Element elmExpression, Type type)
            : base(elmExpression, type)
        {
            SqlExpressionBuilder = sql;
        }
    }

    internal class OperandExpressionBase
    {
        public Type Type { get; }

        public OperandExpressionBase(Type type)
        {
            Type = type;
        }
    }

    internal class OperandExpression : OperandExpressionBase
    {
        public ParameterExpression Expression { get; }

        public OperandExpression(ParameterExpression expression)
            : base(expression.Type)
        {
            Expression = expression;
        }
    }

    /// <summary>
    ///  TODO: not sure what this means
    /// </summary>
    internal class SqlOperandExpression : OperandExpressionBase
    {
        public TSqlFragment SqlExpressionBuilder { get; }

        public SqlOperandExpression(TSqlFragment sql, Type type)
            : base(type)
        {
            SqlExpressionBuilder = sql;
        }
    }

    /// <summary>
    /// The ExpressionBuilderContext class maintains scope information for the traversal of ElmPackage statements during <see cref="ExpressionBuilder.Build"/>.
    /// </summary>
    /// <remarks>
    /// The scope information in this class is useful for <see cref="IExpressionMutator"/> and is supplied to <see cref="IExpressionMutator.Mutate(Expression, elm.Element, ExpressionBuilderContext)"/>.
    /// </remarks>
    internal class ExpressionBuilderContext : ExpressionBuilderContextBase<ExpressionBuilderContext, ExpressionBuilder, LambdaExpression>
    {
        internal ExpressionBuilderContext(ExpressionBuilder builder,
            ParameterExpression contextParameter,
            DefinitionDictionary<LambdaExpression> definitions,
            IDictionary<string, string> localLibraryIdentifiers)
            : base(builder, localLibraryIdentifiers)
        {
            RuntimeContextParameter = contextParameter;
            Definitions = definitions;
        }

        private ExpressionBuilderContext(ExpressionBuilderContext other, Dictionary<string, ScopedExpressionBase>? scopes = null)
            : base(other.Builder, other.LocalLibraryIdentifiers, other.ImpliedAlias, other.Predecessors, other.Scopes)
        {
            Libraries = other.Libraries;
            RuntimeContextParameter = other.RuntimeContextParameter;
            Definitions = other.Definitions;
            Operands = other.Operands;

            if (scopes != null)
                Scopes = scopes;
        }

        protected override ExpressionBuilderContext Copy(Dictionary<string, ScopedExpressionBase>? scopes)
        {
            return new ExpressionBuilderContext(this, scopes);
        }



        /// <summary>
        /// Gets the <see cref="ParameterExpression"/> which is passed to the <see cref="OperatorBinding"/> for operators to use.        
        /// </summary>
        /// <remarks>
        /// Having access to the <see cref="CqlContext"/> is almost always necessary when implementing operators because the context contains all comparers, value sets, CQL parameter values, and other data provided at runtime.
        /// </remarks>
        public ParameterExpression RuntimeContextParameter { get; }

        internal DefinitionDictionary<LambdaExpression> Definitions { get; }

        internal IDictionary<string, DefinitionDictionary<LambdaExpression>> Libraries { get; } = new Dictionary<string, DefinitionDictionary<LambdaExpression>>();

        internal Expression? ImpliedAliasExpression => ImpliedAlias != null ? GetScope(ImpliedAlias).Expression : null;

        // TODO: does this get hoisted into the base class?   are Scopes common handling?
        internal ExpressionBuilderContext WithImpliedAlias(string aliasName, Expression linqExpression, elm.Element elmExpression)
        {
            var subContext = WithScopes(new KeyValuePair<string, ScopedExpression>(aliasName, new ScopedExpression(linqExpression, elmExpression)));
            subContext.ImpliedAlias = aliasName;

            return subContext;
        }

        /// <summary>
        /// TODO: delightfully inefficient - it copies the list in order to leverage the base class behavior.  Figure out a better way
        /// </summary>
        /// <param name="kvps"></param>
        /// <returns></returns>
        internal ExpressionBuilderContext WithScopes(params KeyValuePair<string, ScopedExpression>[] kvps)
        {
            KeyValuePair<string, ScopedExpressionBase>[] baseType;

            baseType = kvps.Select(kvp => new KeyValuePair<string, ScopedExpressionBase>(kvp.Key, kvp.Value)).ToArray();

            return base.WithScopes(baseType);
        }

        new internal ScopedExpression GetScope(string elmAlias)
        {
            var normalized = NormalizeIdentifier(elmAlias!)!;
            if (Scopes.TryGetValue(normalized, out var expression))
                return expression as ScopedExpression ?? throw new InvalidOperationException();
            else throw new ArgumentException($"The scope alias {elmAlias}, normalized to {normalized}, is not present in the scopes dictionary.", nameof(elmAlias));
        }


    }
}
