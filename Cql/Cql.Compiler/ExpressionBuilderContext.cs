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


    /// <summary>
    /// The ExpressionBuilderContext class maintains scope information for the traversal of ElmPackage statements during <see cref="ExpressionBuilder.Build"/>.
    /// </summary>
    /// <remarks>
    /// The scope information in this class is useful for <see cref="IExpressionMutator"/> and is supplied to <see cref="IExpressionMutator.Mutate(Expression, elm.Element, ExpressionBuilderContext)"/>.
    /// </remarks>
    internal class ExpressionBuilderContext : ExpressionBuilderContextBase<ExpressionBuilderContext, ExpressionBuilder, ScopedExpression>
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

        private ExpressionBuilderContext(ExpressionBuilderContext other)
            : base(other.Builder, other.LocalLibraryIdentifiers, other.ImpliedAlias, other.Predecessors, other.Scopes)
        {
            Libraries = other.Libraries;
            RuntimeContextParameter = other.RuntimeContextParameter;
            Definitions = other.Definitions;
            Operands = other.Operands;
        }

        private ExpressionBuilderContext(
            ExpressionBuilderContext other,
            Dictionary<string, ScopedExpression>? scopes = null) 
            : this(other)
        {
            if (scopes != null)
                Scopes = scopes;
        }

        protected override ExpressionBuilderContext Copy(Dictionary<string, ScopedExpression>? scopes)
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

        /// <summary>
        /// Parameters for function definitions.
        /// </summary>
        internal IDictionary<string, ParameterExpression> Operands { get; } = new Dictionary<string, ParameterExpression>();

        internal IDictionary<string, DefinitionDictionary<LambdaExpression>> Libraries { get; } = new Dictionary<string, DefinitionDictionary<LambdaExpression>>();

        internal Expression? ImpliedAliasExpression => ImpliedAlias != null ? GetScope(ImpliedAlias).Expression : null;

        // TODO: does this get hoisted into the base class?   are Scopes common handling?
        internal ExpressionBuilderContext WithImpliedAlias(string aliasName, Expression linqExpression, elm.Element elmExpression)
        {
            var subContext = WithScopes(new KeyValuePair<string, ScopedExpression>(aliasName, new ScopedExpression(linqExpression, elmExpression)));
            subContext.ImpliedAlias = aliasName;

            return subContext;
        }

    }
}
