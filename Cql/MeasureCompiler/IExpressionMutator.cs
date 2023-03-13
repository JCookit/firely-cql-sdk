﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Ncqa.Cql.MeasureCompiler
{
    /// <summary>
    /// Provides a method for altering an expression, e.g., translating a Conditional to an if/else block, or adding a call to a logging method.
    /// </summary>
    public interface IExpressionMutator
    {
        /// <summary>
        /// Gets the keys that are required by this interceptor on <see cref="RuntimeContext.Externals"/>.  
        /// Using two IExpressionVisitors on the same ExpressionBuilder will result in an <see cref="InvalidOperationException"/>;
        /// </summary>
        public IEnumerable<(string, Type)> RuntimeContextKeys { get; }

        /// <summary>
        /// Intercepts this expression and possibly modifies it.  If no modification is desired, return <paramref name="linqExpression"/>
        /// </summary>
        /// <param name="linqExpression">The source expression.</param>
        /// <param name="context">The build context.  Be careful modifying this value as it can have unexpected side effects (e.g., removing key from <see cref="ExpressionBuilderContext.Scopes"/> could break the builder.).</param>
        /// 
        public System.Linq.Expressions.Expression Mutate(System.Linq.Expressions.Expression linqExpression,
            Elm.Expressions.Expression elmExpression,
            ExpressionBuilderContext context);
    }
}