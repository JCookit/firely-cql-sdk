/* 
 * Copyright (c) 2023, NCQA and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-cql-sdk/main/LICENSE
 */

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;

using elm = Hl7.Cql.Elm;

namespace Hl7.Cql.Compiler
{
    internal abstract class ScopedSymbolsContext
    {

        /// <summary>
        /// Contains query aliases and let declarations, and any other symbol that is now "in scope"
        /// 
        /// TODO: this is ugly --- basically maintaining the dictionary of the base class, and doing an explicit case to derived type only if needed
        /// it's the lazy approach for now to prevent having to change more code
        /// 
        /// TODO: this probably makes more sense as a has-a, not is-a relationship?
        /// </summary>
        protected IDictionary<string, ScopedExpressionBase> Scopes { get;  init; } = new Dictionary<string, ScopedExpressionBase>();


        // TODO: is this sufficient for SQL also?
        internal static string? NormalizeIdentifier(string? identifier)
        {
            if (identifier == null)
                return null;

            identifier = identifier.Replace(" ", "_");
            identifier = identifier.Replace("-", "_");
            identifier = identifier.Replace(".", "_");
            identifier = identifier.Replace(",", "_");
            identifier = identifier.Replace("[", "_");
            identifier = identifier.Replace("]", "_");
            identifier = identifier.Replace("(", "_");
            identifier = identifier.Replace(")", "_");
            identifier = identifier.Replace(":", "_");
            identifier = identifier.Replace("/", "_");
            identifier = identifier.Replace("+", "plus");
            identifier = identifier.Replace("-", "minus");
            identifier = identifier.Replace("\"", "");
            identifier = identifier.Replace("'", "");
            identifier = identifier.Replace(";", "_");
            identifier = identifier.Replace("&", "and");


            if (identifier.StartsWith("$"))
                identifier = identifier.Substring(1);
            var keyword = SyntaxFacts.GetKeywordKind(identifier);
            if (keyword != SyntaxKind.None)
            {
                identifier = $"@{identifier}";
            }
            if (char.IsDigit(identifier[0]))
                identifier = "_" + identifier;
            return identifier;
        }

        /// <summary>
        /// </summary>
        /// <param name="elmAlias"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal ScopedExpressionBase GetScope(string elmAlias)
        {
            var normalized = NormalizeIdentifier(elmAlias!)!;
            if (Scopes.TryGetValue(normalized, out var expression))
                return expression;
            else throw new ArgumentException($"The scope alias {elmAlias}, normalized to {normalized}, is not present in the scopes dictionary.", nameof(elmAlias));
        }

        internal bool HasScope(string elmAlias) => Scopes.ContainsKey(elmAlias);

        /// <summary>
        /// Parameters for function definitions.
        /// </summary>
        internal IDictionary<string, OperandExpressionBase> Operands { get; init; } = new Dictionary<string, OperandExpressionBase>();

    }


    /// <summary>
    /// TODO: make a base class with minimal generics (with copy and scope operations?)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="B"></typeparam>
    /// <typeparam name="E"></typeparam>
    internal abstract class ExpressionBuilderContextBase<T, B, E> : ScopedSymbolsContext
        where T : ExpressionBuilderContextBase<T, B, E>
        where B : ExpressionBuilderBase<B, E>
        where E : class
    {
        internal ExpressionBuilderContextBase(
            B builder, 
            IDictionary<string, string> localLibraryIdentifiers)
        {
            Builder = builder;
            LocalLibraryIdentifiers = localLibraryIdentifiers;
        }

        public ExpressionBuilderContextBase(
            B builder, 
            IDictionary<string, string> localLibraryIdentifiers, 
            string? impliedAlias, 
            IList<elm.Element> predecessors,
            IDictionary<string, ScopedExpressionBase> scopes) 
            : this(builder, localLibraryIdentifiers)
        {
            ImpliedAlias = impliedAlias;
            this.Predecessors = predecessors.ToList(); // copy
            this.Scopes = scopes;  // not copy
        }

        /// <summary>
        /// Make a (mostly shallow) copy of this context.   Predecessors are deeply copied.  
        /// </summary>
        /// <returns></returns>
        abstract protected T Copy(Dictionary<string, ScopedExpressionBase>? optionalScopes = null);

        protected IList<elm.Element> Predecessors { get; init; } = new List<elm.Element>();

        /// <summary>
        /// Gets the builder from which this context derives.
        /// </summary>
        public B Builder { get; }

        /// <summary>
        /// Gets the parent of the context's current expression.
        /// </summary>
        public elm.Element? Parent
        {
            get
            {
                if (Predecessors.Count < 0)
                    return null;
                else if (Predecessors.Count == 1)
                    return Predecessors[0];
                else return Predecessors[Predecessors.Count - 2];
            }
        }

        /// <summary>
        /// Used for mappings such as:
        ///     include canonical_id version '1.0.0' called alias
        /// The key is "alias" and the value is "canonical_id.1.0.0"
        /// </summary>
        internal IDictionary<string, string> LocalLibraryIdentifiers = new Dictionary<string, string>();


        /// <summary>
        /// Gets key value pairs mapping the library identifier to its library-local alias.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> LibraryIdentifiers
        {
            // Don't return the dictionary, to protect against cast attacks.  The source dictionary must be readonly.
            get
            {
                foreach (var kvp in LibraryIdentifiers)
                    yield return kvp;
            }
        }

        /// <summary>
        /// In dodgy sort expressions where the properties are named using the undocumented IdentifierRef expression type,
        /// this value is the implied alias name that should qualify it, e.g. from DRR-E 2022:
        /// <code>
        ///     "PHQ-9 Assessments" PHQ
        ///      where ...
        ///      sort by date from start of FHIRBase."Normalize Interval"(effective) asc
        /// </code> 
        /// The use of "effective" here is unqualified and is implied to be PHQ.effective
        /// No idea how this is supposed to work with queries with multiple sources (e.g., with let statements)
        /// </summary>
        protected internal string? ImpliedAlias { get; protected set; } = null;
        internal string FormatMessage(string message, elm.Element? element)
        {
            var locator = element?.locator;
            if (!string.IsNullOrWhiteSpace(locator))
            {
                return $"{Builder.ThisLibraryKey} line {locator}: {message}";
            }
            else return $"{Builder.ThisLibraryKey}: {message}";
        }

        internal void LogError(string message, elm.Element? element = null)
        {
            Builder.Logger.LogError(FormatMessage(message, element));
        }

        internal void LogWarning(string message, elm.Element? expression = null)
        {
            Builder.Logger.LogWarning(FormatMessage(message, expression));
        }

        /// <summary>
        /// Clones this ExpressionBuilderContext, adding the current context as a predecessor.
        /// </summary>
        internal T Deeper(elm.Element expression)
        {
            var subContext = Copy();
            subContext.Predecessors.Add(expression);
            return subContext;
        }

        /// <summary>
        /// Creates a copy with the scopes provided.
        /// </summary>
        internal T WithScopes(params KeyValuePair<string, ScopedExpressionBase>[] kvps)
        {
            var scopes = new Dictionary<string, ScopedExpressionBase>(Scopes);
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
            var subContext = this.Copy(scopes);
            return subContext;
        }
    }
}
