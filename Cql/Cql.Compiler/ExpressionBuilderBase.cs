using Hl7.Cql.Abstractions;
using Hl7.Cql.Elm;
using Hl7.Cql.Model;
using Hl7.Cql.Runtime;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.TransactSql.ScriptDom;

using System;
using System.Collections.Generic;

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

        // Yeah, hardwired to FHIR 4.0.1 for now.
        protected static readonly IDictionary<string, ClassInfo> modelMapping = Models.ClassesById(Models.Fhir401);

        public ExpressionBuilderBase(Library elm, TypeManager typeManager, ILogger<T> logger)
        {
            Library = elm ?? throw new ArgumentNullException(nameof(elm));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            TypeManager = typeManager ?? throw new ArgumentNullException(nameof(typeManager));
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

        /// <summary>
        /// Gets the settings used during Build.
        /// These should be set as desired before Build is called.
        /// </summary>
        public ExpressionBuilderSettings Settings { get; } = new ExpressionBuilderSettings();

        /// <summary>
        /// The <see cref="TypeManager"/> used to resolve and create types referenced in <see cref="Library"/>.
        /// </summary>
        public TypeManager TypeManager { get; }

        protected internal TypeResolver TypeResolver => TypeManager.Resolver;

        public abstract DefinitionDictionary<E> Build();
    }
}