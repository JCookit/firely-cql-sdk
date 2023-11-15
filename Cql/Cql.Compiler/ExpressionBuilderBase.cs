using Hl7.Cql.Abstractions;
using Hl7.Cql.Elm;
using Hl7.Cql.Runtime;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.TransactSql.ScriptDom;

using System;

namespace Hl7.Cql.Compiler
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">Derived type</typeparam>
    /// <typeparam name="E">Base expression type (ie LambdaExpression)</typeparam>
    internal abstract class ExpressionBuilderBase<T, E> 
        where E : class
        where T : class
    {
        public ExpressionBuilderBase(Library elm, ILogger<T> logger)
        {
            Library = elm ?? throw new ArgumentNullException(nameof(elm));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (Library.identifier == null)
                throw new ArgumentException("Package is missing a library identifier", nameof(elm));
        }

        /// <summary>
        /// The <see cref="Library"/> this builder will build.
        /// </summary>
        public Library Library { get; }

        protected internal ILogger<T> Logger { get; init; }

        internal string ThisLibraryKey => Library.NameAndVersion
            ?? throw new InvalidOperationException("Name and version is null.");

        abstract protected internal TypeResolver TypeResolver { get; }

        /// <summary>
        /// Gets the settings used during Build.
        /// These should be set as desired before Build is called.
        /// </summary>
        public ExpressionBuilderSettings Settings { get; } = new ExpressionBuilderSettings();

        public abstract DefinitionDictionary<E> Build();
    }
}