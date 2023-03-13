﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using AgileObjects.ReadableExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Ncqa.Cql.Runtime;
using Ncqa.Cql.Runtime.Primitives;
using Ncqa.Graph;

namespace Ncqa.Cql.MeasureCompiler.CodeGeneration
{
    public partial class SourceCodeWriter
    {
        public static Func<string, Stream> DirectoryWriter(DirectoryInfo rootDirectory) =>
            (lib) =>
            {
                var fi = new FileInfo(Path.Combine(rootDirectory.FullName, $"{lib}.cs"));
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }
                return new FileStream(fi.FullName, FileMode.Create);
            };

        public SourceCodeWriter(ILogger<SourceCodeWriter> log, bool forDebugging = false)
        {
            Log = log;
            ForDebugging = forDebugging;
        }

        public MethodBuilder MethodBuilder { get; } = new MethodBuilder();

        public string? Namespace { get; set; }
        public bool PartialClass { get; set; }
        public AccessModifier ContextAccessModifier { get; set; } = CodeGeneration.AccessModifier.Internal;
        public AccessModifier CachedDefinitionAccessModifier { get; set; } = CodeGeneration.AccessModifier.Internal;


        public readonly IList<string> Usings = new List<string>
        {
            nameof(System),
            $"{nameof(System)}.{nameof(System.Linq)}",
            $"{nameof(System)}.{nameof(System.Collections)}.{nameof(System.Collections.Generic)}",
            $"{nameof(Ncqa)}.{nameof(Ncqa.Cql)}.{nameof(Ncqa.Cql.Runtime)}",
            $"{nameof(Ncqa)}.{nameof(Ncqa.Cql)}.{nameof(Ncqa.Cql.Runtime)}.{nameof(Ncqa.Cql.Runtime.Primitives)}",

        };
        public readonly IList<(string, string)> AliasedUsings = new List<(string, string)>
        {
        };
        internal readonly IList<Type> Ambiguities = new List<Type>
        {
        };

        public string Version => GetType().Assembly?.GetName()?.Version?.ToString() ?? "1.0.0";
        public string Product =>
                GetType()
                .Assembly?
                .GetCustomAttributes(false)
                .OfType<AssemblyProductAttribute>()
                .SingleOrDefault()?
                .Product
                ?? "Test Deck Generator";

        public ILogger<SourceCodeWriter> Log { get; }
        public bool ForDebugging { get; }

        private TranslationSettings Settings(TranslationSettings c) => Settings(c, 0);

        private TranslationSettings Settings(TranslationSettings c, int indent)
        {
            return c
                .UseFullyQualifiedTypeNames
                .TranslateConstantsUsing((type, @object) =>
                {
                    return WriteConstantValue(type, @object);
                })
                .IndentUsing(TextWriterExtensions.Indent);
        }

        public void Write(DefinitionDictionary<LambdaExpression> definitions,
            DirectedGraph dependencyGraph,
            Func<string, Stream> libraryNameToStream,
            Func<string, string?>? libraryNameToClassName = null,
            bool closeStream = true,
            Func<string,bool>? writeFile = null)
        {
            if (libraryNameToClassName == null)
                libraryNameToClassName = ExpressionBuilderContext.NormalizeIdentifier;

            bool defaultWriteFile(string nodeId) => true;
            writeFile ??= defaultWriteFile;
            var typesAndMethods = MethodBuilder.CreateMethodsFor(definitions);
            var methods = typesAndMethods.Item1;
            var interfaceTypes = typesAndMethods.Item2;
            var minimalGraph = dependencyGraph;
            var buildOrder = DetermineBuildOrder(minimalGraph);

            var tupleTypes = new HashSet<Type>();
            foreach (var libNav in definitions.Libraries)
            {
                if (definitions.TryGetTupleTypes(libNav, out var libTupleTypes))
                {
                    foreach (var ltt in libTupleTypes!)
                        tupleTypes.Add(ltt);
                }
            }
            if (tupleTypes.Any())
            {
                foreach (var tupleType in tupleTypes!)
                {
                    var tupleTypeStream = libraryNameToStream(Path.Combine(tupleType.Namespace, tupleType.Name));
                    try
                    {
                        var writer = new StreamWriter(tupleTypeStream);
                        WriteUsings(writer);
                        var indentLevel = 0;
                        writer.WriteLine(indentLevel, $"namespace {tupleType.Namespace}");
                        writer.WriteLine(indentLevel, "{");
                        indentLevel += 1;
                        WriteTupleType(writer, indentLevel, tupleType);
                        indentLevel -= 1;
                        writer.WriteLine(indentLevel, "}");
                        writer.Flush();
                    }
                    finally
                    {
                        if (closeStream && tupleTypeStream != null)
                        {
                            tupleTypeStream.Close();
                            tupleTypeStream = null;
                        }
                    }
                }
            }
            else
            {
                Log.LogInformation($"No tuple types detected; skipping.");
            }
            foreach (var library in buildOrder)
            {
                string libraryName = library.NodeId;
                if (!writeFile(libraryName))
                    continue;
                if (!definitions.Libraries.Contains(libraryName))
                {
                    Log.LogInformation($"Skipping library {libraryName}, which has no definitions");
                    continue;
                }
                var stream = libraryNameToStream(libraryName);
                try
                {
                    using var writer = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true);
                    int indentLevel = 0;
                    WriteUsings(writer);
                    if (!string.IsNullOrWhiteSpace(Namespace))
                    {
                        writer.WriteLine(indentLevel, $"namespace {Namespace}");
                        writer.WriteLine(indentLevel, "{");
                        writer.WriteLine();
                        indentLevel += 1;
                    }
                    // Namespace
                    {
                        writer.WriteLine(indentLevel, $"[System.CodeDom.Compiler.GeneratedCode(\"{Product}\", \"{Version}\")]");

                        var libraryAttribute = libraryName;
                        var versionAttribute = string.Empty;
                        var nameAndVersion = libraryName.Split('-');
                        if (nameAndVersion.Length == 2)
                        {
                            if (System.Version.TryParse(nameAndVersion[1], out var version))
                            {
                                libraryAttribute = nameAndVersion[0];
                                versionAttribute = nameAndVersion[1];
                            }
                        }

                        writer.WriteLine(indentLevel, $"[CqlLibrary(\"{libraryAttribute}\", \"{versionAttribute}\")]");
                        var className = ExpressionBuilderContext.NormalizeIdentifier(libraryName);
                        if (PartialClass)
                            writer.WriteLine(indentLevel, $"partial class {className}");
                        else
                            writer.WriteLine(indentLevel, $"public class {className}");
                        writer.WriteLine(indentLevel, "{");
                        writer.WriteLine();
                        indentLevel += 1;
                        // Class
                        {
                            writer.WriteLine();

                            writer.WriteLine(indentLevel, $"{AccessModifier(ContextAccessModifier)} RuntimeContext context;");
                            writer.WriteLine();
                            writer.WriteLine(indentLevel, "#region Cached values");
                            writer.WriteLine();
                            var accessModifier = AccessModifier(CachedDefinitionAccessModifier);
                            var lazy = ForDebugging ? "DebugLazy" : "Lazy";
                            foreach (var kvp in definitions.DefinitionsForLibrary(libraryName))
                            {
                                foreach (var overload in kvp.Value)
                                {
                                    if (IsDefinition(overload))
                                    {
                                        var methodName = ExpressionBuilderContext.NormalizeIdentifier(kvp.Key);
                                        var cachedValueName = DefinitionCacheKeyForMethod(methodName!);
                                        var returnType = PrettyTypeName(overload.Item2.ReturnType);
                                        writer.WriteLine(indentLevel, $"{accessModifier} {lazy}<{returnType}> {cachedValueName};");
                                    }
                                }
                            }
                            writer.WriteLine();
                            writer.WriteLine(indentLevel, "#endregion");
                            writer.WriteLine(indentLevel, $"public {className}(RuntimeContext context)");
                            writer.WriteLine(indentLevel, "{");
                            {
                                indentLevel += 1;

                                writer.WriteLine(indentLevel, "this.context = context ?? throw new ArgumentNullException(\"context\");");
                                writer.WriteLine();

                                var node = dependencyGraph.Nodes[libraryName];
                                var requiredLibraries = node.ForwardEdges?
                                    .Select(edge => edge.ToId)
                                    .Except(new[] { dependencyGraph.EndNode.NodeId })
                                    .Distinct();
                                foreach (var dependentLibrary in requiredLibraries!)
                                {
                                    var typeName = libraryNameToClassName!(dependentLibrary);
                                    var memberName = typeName;
                                    writer.WriteLine(indentLevel, $"{memberName} = new {typeName}(context);");
                                }
                                writer.WriteLine();
                                foreach (var kvp in definitions.DefinitionsForLibrary(libraryName))
                                {
                                    foreach (var overload in kvp.Value)
                                    {
                                        if (IsDefinition(overload))
                                        {
                                            var methodName = ExpressionBuilderContext.NormalizeIdentifier(kvp.Key);
                                            var cachedValueName = DefinitionCacheKeyForMethod(methodName!);
                                            var returnType = PrettyTypeName(overload.Item2.ReturnType);
                                            var privateMethodName = PrivateMethodNameFor(methodName!);
                                            writer.WriteLine(indentLevel, $"{cachedValueName} = new {lazy}<{returnType}>(this.{privateMethodName});");
                                        }
                                    }
                                }
                                indentLevel -= 1;
                            }
                            writer.WriteLine(indentLevel, "}");

                            var libraryToMemberNames = WriteLibraryMembers(writer, minimalGraph, libraryName, libraryNameToClassName!, indentLevel);
                            var invocationsTransformer = new InvocationsToMethodCallsTransformer(definitions, methods, interfaceTypes, libraryToMemberNames, libraryName);
                            foreach (var kvp in definitions.DefinitionsForLibrary(libraryName))
                            {
                                foreach (var overload in kvp.Value)
                                {
                                    WriteMemoizedInstanceMethod(className!, writer, indentLevel, invocationsTransformer, kvp.Key, overload);
                                    writer.WriteLine();
                                }
                            }
                            indentLevel -= 1;
                            writer.WriteLine(indentLevel, "}");
                        }
                        indentLevel -= 1;
                        if (!string.IsNullOrWhiteSpace(Namespace))
                        {
                            writer.WriteLine(indentLevel, "}");
                            indentLevel -= 1;
                        }
                    }
                }
                finally
                {
                    if (closeStream && stream != null)
                    {
                        stream.Close();
                    }
                }
            }
        }

        private bool IsDefinition((Type[], LambdaExpression) overload)
        {
            if (overload.Item2.Parameters.Count == 1
                && overload.Item2.Parameters[0].Type == typeof(RuntimeContext))
                return true;
            return false;
        }

        private Dictionary<string, string> WriteLibraryMembers(TextWriter writer,
            DirectedGraph dependencyGraph,
            string libraryName,
            Func<string, string> libraryNameToClassName,
            int indent)
        {
            var libToMemberNames = new Dictionary<string, string>();
            var node = dependencyGraph.Nodes[libraryName];
            var requiredLibraries = node.ForwardEdges?
                .Select(edge => edge.ToId)
                .Except(new[] { dependencyGraph.EndNode.NodeId })
                .Distinct();
            if (requiredLibraries != null)
            {

                writer.WriteLine(indent, "#region Dependencies");
                writer.WriteLine();

                foreach (var dependentLibrary in requiredLibraries)
                {
                    var typeName = libraryNameToClassName(dependentLibrary);
                    var memberName = typeName;
                    libToMemberNames.Add(dependentLibrary, memberName);
                    writer.WriteLine(indent, $"public {typeName} {memberName} {{ get; }}");
                }
                writer.WriteLine();
                writer.WriteLine(indent, "#endregion");
                writer.WriteLine();
            }
            return libToMemberNames;
        }

        private IList<DirectedGraphNode> DetermineBuildOrder(DirectedGraph minimalGraph)
        {
            var sorted = minimalGraph.TopologicalSort()
                .Where(n => n.NodeId != minimalGraph.StartNode.NodeId
                    && n.NodeId != minimalGraph.EndNode.NodeId)
                .ToList();
            return sorted;
        }

        private string DefinitionCacheKeyForMethod(string methodName)
        {
            if (methodName[0] == '@')
                return "__" + methodName.Substring(1);
            else return "__" + methodName;
        }
        private string PrivateMethodNameFor(string methodName) => methodName + "_Value";

        private void WriteMemoizedInstanceMethod(string className, TextWriter writer, int indentLevel,
            InvocationsToMethodCallsTransformer invocationsTransformer,
            string cqlName,
            (Type[], LambdaExpression) overload)
        {
            var methodName = ExpressionBuilderContext.NormalizeIdentifier(cqlName);
            var returnType = PrettyTypeName(overload.Item2.ReturnType);


            var parameters = overload.Item2.Parameters
                .Skip(1) // skip runtimeContext
                .Select(p => p.Name);

            var parameterFinder = new ParameterFinder();
            parameterFinder.Visit(overload.Item2.Body);
            var parametersInBody = parameterFinder.Names;

            var vng = new VariableNameGenerator(parameters.Concat(parametersInBody), "_");
            var visitedBody = Transform(overload.Item2.Body,
                invocationsTransformer,
                new RedundantCastsTransformer(),
                new BlockTransformer(vng, parameters)
            );

            if (IsDefinition(overload))
            {
                var cachedValueName = DefinitionCacheKeyForMethod(methodName!);
                var privateMethodName = PrivateMethodNameFor(methodName!);
                writer.WriteLine(indentLevel, $"private {returnType} {privateMethodName}()");
                if (visitedBody is BlockExpression)
                    WriteExpression(className, methodName!, writer, indentLevel, visitedBody, true);
                else
                {
                    writer.WriteLine(indentLevel, "{");
                    WriteExpression(className, methodName!, writer, indentLevel + 1, visitedBody, true);
                    writer.WriteLine(indentLevel, "}");
                }
                writer.WriteLine();
                writer.WriteLine(indentLevel, $"[CqlDeclaration(\"{cqlName}\")]");
                if (overload.Item2.ReturnType == typeof(CqlValueSet))
                {
                    if (overload.Item2.Body is NewExpression @new)
                    {
                        var arg = @new.Arguments[0];
                        if (arg is ConstantExpression constant)
                        {
                            if (constant.Value is string valueSetId)
                            {
                                writer.WriteLine(indentLevel, $"[CqlValueSet(\"{valueSetId}\")]");
                            }
                        }
                    }
                }
                writer.WriteLine(indentLevel, $"public {returnType} {methodName}() => {cachedValueName}.Value;");
            }
            else
            {
                var parameterString = string.Join(", ", overload.Item2.Parameters
                    .Skip(1) // skip runtimeContext
                    .Select(p => $"{PrettyTypeName(p.Type)} {PrefixKeywords(p.Name)}"));
                writer.WriteLine(indentLevel, $"[CqlDeclaration(\"{cqlName}\")]");
                writer.WriteLine(indentLevel, $"public {returnType} {methodName}({parameterString})");
                if (visitedBody is BlockExpression)
                    WriteExpression(className, methodName!, writer, indentLevel, visitedBody, true);
                else
                {
                    writer.WriteLine(indentLevel, "{");
                    WriteExpression(className, methodName!, writer, indentLevel + 1, visitedBody, true);
                    writer.WriteLine(indentLevel, "}");
                }
                writer.WriteLine();
            }

        }


        private void WriteUsings(TextWriter writer)
        {
            foreach (var @using in Usings)
            {
                writer.WriteLine($"using {@using};");
            }
            foreach (var @using in AliasedUsings)
            {
                writer.WriteLine($"using {@using.Item1} = {@using.Item2};");
            }
        }

        private int WriteTupleType(TextWriter writer, int indentLevel, Type tupleType)
        {
            writer.WriteLine();
            writer.WriteLine(indentLevel, $"[System.CodeDom.Compiler.GeneratedCode(\"{Product}\", \"{Version}\")]");
            writer.WriteLine(indentLevel, $"public class {tupleType.Name}: {PrettyTypeName(tupleType.BaseType)}");
            writer.WriteLine(indentLevel, "{");

            indentLevel++;
            foreach (var property in tupleType.GetProperties())
            {
                var normalizedName = ExpressionBuilderContext.NormalizeIdentifier(property.Name);
                var propertyType = PrettyTypeName(property.PropertyType);
                var cqlDeclarationAttribute = property.GetCustomAttribute<CqlDeclarationAttribute>();
                if (cqlDeclarationAttribute != null)
                {
                    writer.WriteLine(indentLevel, $"[CqlDeclaration(\"{cqlDeclarationAttribute.Name}\")]");
                }
                writer.WriteLine(indentLevel, $"public {propertyType} {normalizedName} {{ get; set; }}");
            }
            indentLevel--;
            writer.WriteLine(indentLevel, $"}}");
            return indentLevel;
        }

        private string WriteConstantValue(Type constantType, object value)
        {
            if (value == default)
            {
                if (constantType.IsValueType)
                    return "default";
                else return "null";
            }
            else
            {
                if (constantType.IsEnum)
                    return $"{constantType.Namespace}.{constantType.Name}.{value}";
                if (constantType == typeof(string))
                    return $"\"{value!}\"";
                else if (constantType == typeof(bool))
                    return value.ToString()!.ToLowerInvariant();
                else if (constantType == typeof(bool?))
                    return value?.ToString()!.ToLowerInvariant() ?? "null";
                else if (constantType == typeof(Uri))
                    return $"new Uri(\"{value!}\")";
                else if (typeof(Type).IsAssignableFrom(constantType))
                {
                    return $"typeof({PrettyTypeName((Type)value)})";
                }
                else
                {
                    var str = value.ToString();
                    return str;
                }

            }

        }

        private string PrettyTypeName(Type type)
        {

            string typeName = type.Name;
            if (type == typeof(int))
                return "int";
            else if (type == typeof(bool))
                return "bool";
            else if (type == typeof(decimal))
                return "decimal";
            else if (type == typeof(float))
                return "float";
            else if (type == typeof(double))
                return "double";
            else if (type == typeof(string))
                return "string";
            else if (type == typeof(object))
                return "object";
            if (type.IsGenericType)
            {
                if (type.IsGenericTypeDefinition == false && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    typeName = $"{PrettyTypeName(Nullable.GetUnderlyingType(type))}?";
                }
                else
                {
                    if (type.IsGenericType)
                    {
                        var tildeIndex = type.Name.IndexOf('`');
                        var rootName = type.Name.Substring(0, tildeIndex);
                        var genericArgumentNames = type.GetGenericArguments()
                            .Select(arg => PrettyTypeName(arg));
                        var prettyName = $"{rootName}<{string.Join(",", genericArgumentNames)}>";
                        typeName = prettyName;
                    }
                }
            }
            if (type.IsNested)
            {
                typeName = $"{PrettyTypeName(type.DeclaringType)}.{typeName}";
            }
            if (Ambiguities.Contains(type) || typeName.StartsWith("Tuple_"))
            {
                return $"{type.Namespace}.{typeName}";
            }
            else if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return $"{PrettyTypeName(elementType)}[]";
            }
            else return typeName;
        }

        private System.Linq.Expressions.Expression Transform(System.Linq.Expressions.Expression body, params ExpressionVisitor[] visitors)
        {
            foreach (var visitor in visitors)
            {
                //var visitedBody = overload.Item2.Body;
                body = visitor.Visit(body);
            }
            return body;
        }


        private void WriteExpression(string className, string methodName, TextWriter writer, int indentLevel, System.Linq.Expressions.Expression expression, bool @return)
        {
            if (expression is BlockExpression block)
            {
                writer.WriteLine(indentLevel, "{");
                foreach (var blockStatement in block.Expressions)
                {
                    if (blockStatement is BinaryExpression be && be.Left is ParameterExpression localVariable)
                    {
                        if (be.Right is LambdaExpression lambda)
                        {
                            if (lambda.Body is BlockExpression lambdaBlock)
                            {
                                var lambdaParameters = string.Join(", ", lambda.Parameters.Select(p => ToCode(indentLevel + 1, p, false)));
                                var funcType = PrettyTypeName(lambda.Type);
                                writer.WriteLine(indentLevel + 1, $"{funcType} {localVariable.Name} = ({lambdaParameters}) => ");
                                WriteExpression(className, methodName, writer, indentLevel + 1, lambdaBlock, true);
                                writer.WriteLine(";");
                            }
                            else
                            {
                                var declaration = $"{PrettyTypeName(lambda.Type)} {localVariable.Name}";
                                var value = ToCode(indentLevel + 1, be.Right, false);
                                var assignment = $"{declaration} = {value};";
                                writer.WriteLine(indentLevel + 1, assignment);
                            }
                            continue;
                        }
                        writer.WriteLine(indentLevel + 1, ToCode(indentLevel + 1, blockStatement, false));
                        continue;
                    }
                    WriteExpression(className, methodName, writer, indentLevel + 1, blockStatement, false);
                }
                writer.WriteLine();
                writer.Write(indentLevel, "}");
            }
            else if (expression is ConditionalExpression conditional)
            {
                WriteConditional(className, methodName, writer, indentLevel, conditional, true, @return);
            }
            else if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Throw)
            {
                var asString = ToCode(indentLevel, expression, false);
                writer.WriteLine(indentLevel, $"{asString};");
            }
            else
            {
                var asString = ToCode(indentLevel, expression, false);
                if (@return)
                {
                    writer.WriteLine(indentLevel, $"return {asString};");
                }
                else
                    writer.Write(indentLevel, asString);
            }
        }

        private readonly MethodInfo PropertyOrDefaultMethod = typeof(RuntimeExtensions).GetMethod(nameof(RuntimeExtensions.PropertyOrDefault));

        private string ToCode(int indent, System.Linq.Expressions.Expression expression, bool leadingIndent = true)
        {
            var leadingIndentString = string.Empty;
            if (leadingIndent)
                leadingIndentString = IndentString(indent);
            switch (expression)
            {
                case ConstantExpression constant:
                    if (constant.Type == typeof(decimal))
                    {
                        var code = $"{constant.Value}m";
                        return code;
                    }
                    else if (constant.Type == typeof(decimal?))
                    {
                        if (constant.Value == null)
                            return "null";
                        else
                        {
                            var code = $"{constant.Value}m";
                            return code;
                        }
                    }
                    else if (constant.Type == typeof(PropertyInfo))
                    {
                        if (constant.Value != null && constant.Value is PropertyInfo propertyInfo)
                        {
                            var declaringType = PrettyTypeName(propertyInfo.DeclaringType);
                            var code = $"typeof({declaringType}).GetProperty(\"{propertyInfo.Name}\")";
                            return code;
                        }
                    }
                    break;
                case NewExpression @new:
                    if (@new.Arguments.Count > 0)
                    {
                        var newSb = new StringBuilder();
                        newSb.Append($"{leadingIndentString}new {PrettyTypeName(@new.Type)}({ToCode(indent + 1, @new.Arguments[0], false)}");
                        if (@new.Arguments.Count > 1)
                        {
                            newSb.AppendLine(", ");
                            for (int i = 1; i < @new.Arguments.Count - 1; i++)
                            {
                                var argument = @new.Arguments[i];
                                newSb.Append(ToCode(indent + 1, argument));
                                newSb.AppendLine(", ");
                            }
                            newSb.Append(ToCode(indent + 1, @new.Arguments[@new.Arguments.Count - 1]));
                        }
                        newSb.Append(")");
                        return newSb.ToString();
                    }
                    else
                        break;
                case MethodCallExpression call:
                    if (call.Method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), true))
                    {
                        var thisArgument = call.Arguments[0];
                        if (call.Method.IsGenericMethod && call.Method.GetGenericMethodDefinition() == PropertyOrDefaultMethod)
                        {
                            var @object = $"{Parenthesize(ToCode(indent + 1, thisArgument, false))}";
                            if (call.Arguments[1] is LambdaExpression lambda
                                && lambda.Body is MemberExpression memberAccess)
                            {
                                var memberName = PrefixKeywords(memberAccess.Member.Name);
                                var nullCoalesce = $"{@object}?.{memberName}";
                                return nullCoalesce;
                            }
                            else throw new InvalidOperationException("Expected lambda argument with a member expression body");
                        }
                        else
                        {
                            var @object = $"{Parenthesize(ToCode(indent + 1, thisArgument, false))}.";
                            if (call.Arguments.Count > 1)
                            {
                                var sb = new StringBuilder();
                                sb.Append(leadingIndentString);
                                sb.Append($"{@object}{PrettyMethodName(call.Method)}({ToCode(indent + 1, call.Arguments[1], false)}");
                                if (call.Arguments.Count > 2)
                                {
                                    sb.AppendLine(", ");
                                    for (int i = 2; i < call.Arguments.Count - 1; i++)
                                    {
                                        var argument = call.Arguments[i];
                                        sb.Append(IndentString(indent + 1));
                                        sb.Append(ToCode(0, argument));
                                        sb.AppendLine(", ");
                                    }
                                    sb.Append(ToCode(indent + 1, call.Arguments[call.Arguments.Count - 1]));
                                }
                                sb.Append(")");
                                return sb.ToString();
                            }
                            else
                            {
                                return $"{leadingIndentString}{@object}{PrettyMethodName(call.Method)}()";
                            }
                        }
                    }
                    else if (call.Arguments.Count > 0)
                    {
                        var sb = new StringBuilder();
                        sb.Append(leadingIndentString);
                        var @object = string.Empty;
                        if (call.Object != null)
                        {
                            @object = $"{Parenthesize(ToCode(indent, call.Object, false))}.";
                        }
                        else if (call.Method.IsStatic)
                        {
                            @object = $"{PrettyTypeName(call.Method.DeclaringType)}.";
                        }
                        var firstArgument = ToCode(indent + 1, call.Arguments[0], false);
                        sb.Append($"{@object}{PrettyMethodName(call.Method)}({firstArgument}");
                        if (call.Arguments.Count > 1)
                        {
                            sb.AppendLine(", ");
                            for (int i = 1; i < call.Arguments.Count - 1; i++)
                            {
                                var argument = call.Arguments[i];
                                var argumentCode = ToCode(indent + 1, argument);
                                sb.Append(argumentCode);
                                sb.AppendLine(", ");
                            }
                            sb.Append(ToCode(indent + 1, call.Arguments[call.Arguments.Count - 1]));
                        }
                        sb.Append(")");
                        return sb.ToString();
                    }
                    else
                        break;
                case LambdaExpression lambda:
                    var lambdaParameters = $"({string.Join(", ", lambda.Parameters.Select(p => p.Name))})";
                    var lambdaBody = ToCode(indent + 1, lambda.Body, lambda.Body is BlockExpression);
                    var lambdaSb = new StringBuilder();
                    lambdaSb.Append(leadingIndentString);
                    lambdaSb.Append(lambdaParameters);
                    if (lambda.Body is BlockExpression)
                        lambdaSb.AppendLine(" => ");
                    else lambdaSb.Append(" => ");
                    lambdaSb.Append(lambdaBody);
                    return lambdaSb.ToString();
                case BinaryExpression binary:
                    {
                        var @operator = BinaryOperatorFor(binary.NodeType);
                        if (@operator != null)
                        {
                            if (binary.NodeType == ExpressionType.Assign &&
                                binary.Left is ParameterExpression parameter)
                            {
                                string typeDeclaration = "var";
                                if (binary.Right is DefaultExpression
                                    || (binary.Right is ConstantExpression ce && ce.Value == null)
                                    || (binary.Right.NodeType == ExpressionType.Convert && binary.Right is UnaryExpression rightUnary))
                                {
                                    typeDeclaration = PrettyTypeName(binary.Left.Type);
                                }
                                var right = ToCode(indent, binary.Right, false);
                                var assignment = $"{typeDeclaration} {parameter.Name} = {right};";
                                return assignment;
                            }
                            else if (binary.NodeType == ExpressionType.Coalesce &&
                                binary.Left.NodeType == ExpressionType.Convert
                                    && (binary.Right.NodeType == ExpressionType.Constant
                                        || binary.Right.NodeType == ExpressionType.Default))
                            {
                                // usually something like:
                                // (bool?)(x == null) ?? false;
                                // this can be simplified to just:
                                // x == null
                                // C# can figure this one out
                                var unary = (UnaryExpression)binary.Left;
                                var unaryToCode = ToCode(indent, unary.Operand, false);
                                return unaryToCode;
                            }
                            else
                            {
                                var left = ToCode(indent, binary.Left, false);
                                var right = ToCode(indent, binary.Right, false);
                                var binaryString = $"({left} {@operator} {right})";
                                return binaryString;
                            }
                        }
                        break;
                    }
                case UnaryExpression unary:
                    {
                        switch (unary.NodeType)
                        {
                            case ExpressionType.Convert:
                                {
                                    var removeCast = true;
                                    if (unary.Type.IsAssignableFrom(unary.Operand.Type) == false)
                                        removeCast = false;
                                    if (unary.Method != null)
                                        removeCast = false;
                                    if (unary.Type.IsGenericType
                                        && unary.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                                    {
                                        if (unary.Operand.NodeType == ExpressionType.Constant
                                            || unary.Operand.NodeType == ExpressionType.Default)
                                            removeCast = false;
                                        if (unary.Operand is BinaryExpression be && be.NodeType == ExpressionType.Equal
                                            && (be.Right.NodeType == ExpressionType.Constant
                                            || be.Right.NodeType == ExpressionType.Default))
                                        {
                                            removeCast = false;
                                        }
                                    }
                                    else
                                    {
                                        if (unary.Type != typeof(object))
                                        {
                                            removeCast = false;
                                        }
                                    }
                                    var operand = ToCode(indent, unary.Operand, false);
                                    if (removeCast)
                                    {
                                        return operand;
                                    }
                                    else
                                    {
                                        var typeName = PrettyTypeName(unary.Type);
                                        var code = $"(({typeName}){operand})";
                                        return code;
                                    }
                                }
                            case ExpressionType.TypeAs:
                                {
                                    var operand = ToCode(indent, unary.Operand, false);
                                    var typeName = PrettyTypeName(unary.Type);
                                    var code = $"({operand} as {typeName})";
                                    return code;
                                }

                            default: break;
                        }
                        break;
                    }
                case NewArrayExpression newArray:
                    if (newArray.NodeType == ExpressionType.NewArrayInit)
                    {
                        var newArraySb = new StringBuilder();
                        newArraySb.Append(leadingIndentString);
                        if (newArray.Expressions.Count > 0)
                        {
                            var arrayType = PrettyTypeName(newArray.Type);
                            newArraySb.AppendLine($"new {arrayType}");
                            var braceIndent = IndentString(indent);
                            newArraySb.Append(braceIndent);
                            newArraySb.AppendLine("{");
                            foreach (var expr in newArray.Expressions)
                            {
                                var exprCode = ToCode(indent + 1, expr, true);
                                newArraySb.Append(exprCode);
                                newArraySb.AppendLine(",");
                            }
                            newArraySb.Append(braceIndent);
                            newArraySb.Append("}");
                            return newArraySb.ToString();
                        }
                    }
                    else if (newArray.NodeType == ExpressionType.NewArrayBounds)
                    {
                        var newArraySb = new StringBuilder();
                        newArraySb.Append(leadingIndentString);
                        var arrayType = PrettyTypeName(newArray.Type.GetElementType());
                        var size = ToCode(0, newArray.Expressions[0], false);
                        newArraySb.AppendLine($"new {arrayType}[{size}]");
                        return newArraySb.ToString();
                    }
                    break;
                case MemberExpression me:
                    {
                        var @object = ToCode(0, me.Expression);
                        var memberName = PrefixKeywords(me.Member.Name);
                        var nullCoalesce = $"{@object}?.{memberName}";
                        return nullCoalesce;
                    }
                case MemberInitExpression memberInit:
                    var memberInitSb = new StringBuilder();
                    memberInitSb.Append(leadingIndentString);
                    if (memberInit.Bindings.Count > 0)
                    {
                        var arrayType = PrettyTypeName(memberInit.Type);
                        memberInitSb.AppendLine($"new {arrayType}");
                        var braceIndent = IndentString(indent);
                        memberInitSb.Append(braceIndent);
                        memberInitSb.AppendLine("{");
                        var braceIndentPlusOne = IndentString(indent + 1);
                        foreach (var binding in memberInit.Bindings)
                        {
                            if (binding is MemberAssignment assignment)
                            {
                                var memberName = assignment.Member.Name;
                                if (memberName == "ValueElement")
                                {
                                }
                                var assignmentCode = ToCode(indent + 1, assignment.Expression, false);
                                memberInitSb.Append(braceIndentPlusOne);
                                memberInitSb.Append($"{memberName} = {assignmentCode}");
                                memberInitSb.AppendLine(",");
                            }
                            else throw new NotImplementedException();
                        }
                        memberInitSb.Append(braceIndent);
                        memberInitSb.Append("}");
                        return memberInitSb.ToString();
                    }
                    break;
                case LabelExpression label:
                    var defaultValueCode = ToCode(indent, label.DefaultValue, false);
                    var @return = $"{leadingIndentString}return {defaultValueCode};";
                    return @return;
                case ConditionalExpression ce:
#if DEBUG
                    var original = $"{leadingIndentString}({expression.ToReadableString(Settings)})";
#endif
                    var conditionalSb = new StringBuilder();
                    conditionalSb.Append("(");
                    var test = ToCode(indent, ce.Test, false);
                    conditionalSb.AppendLine($"({test})");
                    var ifTrue = $"({ToCode(indent + 2, ce.IfTrue, false)})";

                    var ifFalse = $"({ToCode(indent + 2, ce.IfFalse, false)})";
                    conditionalSb.AppendLine($"{IndentString(indent + 1)}? {ifTrue}");
                    conditionalSb.AppendLine($"{IndentString(indent + 1)}: {ifFalse})");
                    return conditionalSb.ToString();
                case TypeBinaryExpression typeBinary:
                    if (typeBinary.NodeType == ExpressionType.TypeIs)
                    {
                        var left = ToCode(indent, typeBinary.Expression, false);
                        var type = PrettyTypeName(typeBinary.TypeOperand);
                        var @is = $"{left} is {type}";
                        return @is;
                    }
                    break;
                default:
                    break;
            }

            var defaultValue = $"{leadingIndentString}{expression.ToReadableString(Settings)}";
            return defaultValue;

        }

        private string? BinaryOperatorFor(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.AddChecked:
                    return "+=";
                case ExpressionType.And:
                    return "&";
                case ExpressionType.AndAlso:
                    return "&&";
                case ExpressionType.AndAssign:
                    return "&=";
                case ExpressionType.Assign:
                    return "=";
                case ExpressionType.Coalesce:
                    return "??";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.DivideAssign:
                    return "/=";
                case ExpressionType.Equal:
                    return "==";
                case ExpressionType.ExclusiveOr:
                    return "^^";
                case ExpressionType.ExclusiveOrAssign:
                    return "^^=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LeftShift:
                    return "<<";
                case ExpressionType.LeftShiftAssign:
                    return "<<=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.Modulo:
                    return "%";
                case ExpressionType.ModuloAssign:
                    return "%=";
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return "*";
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                    return "*=";
                case ExpressionType.NotEqual:
                    return "!=";
                case ExpressionType.Or:
                    return "|";
                case ExpressionType.OrAssign:
                    return "|=";
                case ExpressionType.OrElse:
                    return "||";
                case ExpressionType.RightShift:
                    return ">>";
                case ExpressionType.RightShiftAssign:
                    return ">>=";
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return "-";
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                    return "-=";
                case ExpressionType.TypeAs:
                    return "as";
                case ExpressionType.TypeIs:
                    return "is";
                default:
                    return null;
            }
        }

        private static string IndentString(int indent)
        {
            return string.Join(string.Empty, Enumerable.Range(0, indent).Select(i => "\t"));
        }

        private string PrettyMethodName(MethodInfo method)
        {
            if (method.IsGenericMethod)
            {
                var genericArgs = string.Join(", ", method.GetGenericArguments().Select(a => PrettyTypeName(a)));
                return $"{method.Name}<{genericArgs}>";
            }
            return method.Name;
        }

        private void WriteConditional(string className, string methodName, TextWriter writer, int indentLevel, ConditionalExpression conditional, bool indentLeadingIf, bool @return)
        {
            if (indentLeadingIf)
                writer.Write(indentLevel, "if (");
            else
                writer.Write(0, "if (");
            var test = ToCode(indentLevel + 1, conditional.Test, false);
            writer.Write(test);
            writer.WriteLine(0, ")");
            if (conditional.IfTrue is BlockExpression)
            {
                WriteExpression(className, methodName, writer, indentLevel, conditional.IfTrue, @return);
            }
            else
            {
                var parameterFinder = new ParameterFinder();
                parameterFinder.Visit(conditional.IfTrue);
                var parametersInBody = parameterFinder.Names;
                var vng = new VariableNameGenerator(parametersInBody, "__");
                var newBlock = Transform(conditional.IfTrue,
                    new RedundantCastsTransformer(),
                    new BlockTransformer(vng, parametersInBody));
                WriteExpression(className, methodName, writer, indentLevel + 1, newBlock, @return);
                writer.WriteLine();
            }
            if (conditional.IfFalse is BlockExpression)
            {
                writer.WriteLine(indentLevel, "else");
            }
            else
            {
                writer.Write(indentLevel, "else ");
            }
            if (conditional.IfFalse is ConditionalExpression elseIf)
            {
                WriteConditional(className, methodName, writer, indentLevel, elseIf, false, @return);
            }
            else
            {
                writer.WriteLine();
                if (conditional.IfFalse is BlockExpression)
                {
                    WriteExpression(className, methodName, writer, indentLevel, conditional.IfFalse, @return);
                }
                else
                {
                    var parameterFinder = new ParameterFinder();
                    parameterFinder.Visit(conditional.IfFalse);
                    var parametersInBody = parameterFinder.Names;
                    var vng = new VariableNameGenerator(parametersInBody, "__");
                    var newBlock = Transform(conditional.IfFalse,
                        new RedundantCastsTransformer(),
                        new BlockTransformer(vng, parametersInBody));
                    WriteExpression(className, methodName, writer, indentLevel + 1, newBlock, @return);
                    writer.WriteLine();
                }
            }
        }

        private string Parenthesize(string term)
        {
            if (term.Contains(" "))
                return $"({term})";
            else return term;
        }

        public string PrefixKeywords(string symbol)
        {
            var keyword = SyntaxFacts.GetKeywordKind(symbol);
            if (keyword != SyntaxKind.None)
            {
                symbol = $"@{symbol}";
            }
            return symbol;
        }

        private static string AccessModifier(AccessModifier modifier)
        {
            switch (modifier)
            {
                case CodeGeneration.AccessModifier.Public:
                    return "public";
                case CodeGeneration.AccessModifier.Private:
                    return "private";
                case CodeGeneration.AccessModifier.Internal:
                    return "internal";
                case CodeGeneration.AccessModifier.Protected:
                    return "protected";
                case CodeGeneration.AccessModifier.ProtectedInternal:
                    return "protected internal";
                default:
                    throw new ArgumentException("Invalid access modifier", nameof(modifier));
            }
        }

    }

    public static class TextWriterExtensions
    {
        public const int SpacesPerIndentLevel = 4;
        public static readonly string Indent = string.Join(string.Empty,
            Enumerable.Range(0, SpacesPerIndentLevel).Select(i => ' '));
        public static void WriteLine(this TextWriter writer, int indent, string text)
        {
            for (int i = 0; i < indent * SpacesPerIndentLevel; i++)
                writer.Write(' ');
            writer.WriteLine(text);
        }
        public static void Write(this TextWriter writer, int indent, string text)
        {
            for (int i = 0; i < indent * SpacesPerIndentLevel; i++)
                writer.Write(' ');
            writer.Write(text);
        }

    }
}
