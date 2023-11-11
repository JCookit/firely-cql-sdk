using Hl7.Cql.Abstractions;
using Hl7.Cql.Elm;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;

namespace Hl7.Cql.Compiler
{
    internal abstract class ExpressionBuilderBase<T>
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
    }
}