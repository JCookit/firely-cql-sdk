﻿using Hl7.Cql.Comparers;
using Hl7.Cql.Operators;
using Hl7.Cql.Primitives;
using Hl7.Cql.ValueSets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hl7.Cql.Poco.Fhir.R4.Model;

namespace Hl7.Cql.Runtime.FhirR4
{
    public abstract class FhirDataRetriever : IDataRetriever
    {
 
        public FhirTypeResolver FhirTypeResolver { get; }

        public IValueSetDictionary ValueSets { get; }

        public ICqlComparer CodeComparer { get; protected set; }
        public ICqlComparer SystemComparer { get; protected set; }


        protected FhirDataRetriever(FhirTypeResolver fhirTypeResolver, IValueSetDictionary valueSets,
            StringComparer? stringComparer = null)
        {
            FhirTypeResolver = fhirTypeResolver ?? throw new ArgumentNullException(nameof(fhirTypeResolver));
            ValueSets = valueSets ?? throw new ArgumentNullException(nameof(valueSets));
            var stringCqlComparer = new StringCqlComparer(stringComparer ?? StringComparer.OrdinalIgnoreCase);
            CodeComparer = stringCqlComparer;
            SystemComparer = stringCqlComparer;
        }

        public abstract IEnumerable<T> RetrieveByCodes<T>(IEnumerable<CqlCode?>? codes, PropertyInfo? codeProperty)
            where T : class;
        public abstract IEnumerable<T> RetrieveByValueSet<T>(CqlValueSet? valueSet, PropertyInfo? codeProperty)
            where T : class;

        protected abstract IEnumerable<T> Retrieve<T>()
            where T : class;

        protected Func<T, IEnumerable<Coding>> FunctionForCodeProperty<T>(PropertyInfo property) where T : class
        {
            Func<T, IEnumerable<Coding>?>? getCoding = null;
            var type = property.PropertyType;
            if (type == typeof(Element))
            {
                getCoding = (resource) =>
                {
                    var t = property.GetValue(resource);
                    if (t == null)
                        return Enumerable.Empty<Coding>();
                    else switch (t)
                        {
                            case IEnumerable<Coding> codings:
                                return (property.GetValue(t) as IEnumerable<Coding>) ?? Enumerable.Empty<Coding>();
                            case Coding coding:
                                return new[] { coding };
                            case CodeElement codeElement:
                                return new[] { new Coding { code = codeElement } };
                            case CodeableConcept codeableConcept:
                                return codeableConcept.coding ?? Enumerable.Empty<Coding>();
                            case IEnumerable<CodeableConcept> codeableConcepts:
                                return codeableConcepts.SelectMany(c => c.coding ?? Enumerable.Empty<Coding>())
                                   ?? Enumerable.Empty<Coding>();
                            default:
                                throw new NotImplementedException($"Property {property.Name} has type {nameof(Element)}, and does not have a choice specifier of a compatible code type.");
                        }
                };
            }
            else if (typeof(IEnumerable<Coding>).IsAssignableFrom(type))
            {
                getCoding = (t) => (property.GetValue(t) as IEnumerable<Coding>) ?? Enumerable.Empty<Coding>();
            }
            else if (type == typeof(Coding))
            {
                getCoding = (t) =>
                {
                    var coding = property.GetValue(t) as Coding;
                    return coding != null
                        ? new[] { coding }
                        : Array.Empty<Coding>();
                };
            }
            else if (type == typeof(CodeElement))
            {
                getCoding = (t) =>
                {
                    var code = property.GetValue(t) as CodeElement;
                    return code != null
                        ? new[] { new Coding { code = code } }
                        : Array.Empty<Coding>();
                };
            }
            else if (type == typeof(CodeableConcept))
            {
                getCoding = (t) => (property.GetValue(t) as CodeableConcept)?.coding
                    ?? Enumerable.Empty<Coding>();
            }
            else if (typeof(IEnumerable<CodeableConcept>).IsAssignableFrom(type))
            {
                getCoding = (t) => (property.GetValue(t) as IEnumerable<CodeableConcept>)?
                    .SelectMany(c => c.coding ?? Enumerable.Empty<Coding>())
                    ?? Enumerable.Empty<Coding>();
            }
            else throw new NotImplementedException($"Property {property.Name} has type {type}, which is not a valid code type.");
            return getCoding!;
        }
    
    }
}
