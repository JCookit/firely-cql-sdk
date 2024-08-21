using Hl7.Cql.Abstractions;
using Hl7.Cql.Elm;
using Hl7.Cql.Model;
using Hl7.Cql.Primitives;
using Hl7.Cql.Runtime;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.TransactSql.ScriptDom;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

using static Hl7.Cql.Compiler.SqlExpressionBuilder;

using BinaryExpression = Microsoft.SqlServer.TransactSql.ScriptDom.BinaryExpression;
using Expression = System.Linq.Expressions.Expression;

namespace Hl7.Cql.Compiler
{
    internal class SqlExpression
    {
        public enum FragmentTypes
        {
            // a select statement that returns a list
            SelectStatement,
            // a select statement that returns a single row
            SelectStatementScalar,
            // a boolen expression wrapped in a select statement (IIF pattern)
            SelectBooleanExpression,
        }

        /// <summary>
        /// true IFF this fragment is a SELECT statement (right now, i think this is true always)
        /// </summary>
        public bool IsSelectStatement => true;

        public TSqlFragment SqlFragment { get;init; }

        public FragmentTypes FragmentType { get;init; }

        public string? CqlContext { get; init; }

        // TODO: hijacking .net type system to describe the types here, in a limited way.  Only use the ones that there is a clear 1:1 correspondence
        // bool, int, DateTime --- primitives
        // Patient, Condition, etc. --- FHIR types
        // CqlCode
        // TODO:  should IEnumerable be used here or is it implicit with SelectStatement vs SelectStatementScalar
        public Type DataType { get; init; }  

        public SqlExpression(TSqlFragment sqlFragment, FragmentTypes fragmentType, Type dataType, string? cqlContext = null)
        {
            SqlFragment = sqlFragment;
            FragmentType = fragmentType;
            DataType = dataType;
            CqlContext = cqlContext;
        }
    }

    internal class SqlExpressionBuilder : ExpressionBuilderBase<SqlExpressionBuilder, SqlExpression>
    {
        public SqlExpressionBuilder(Library library, TypeManager typeManager, ILogger<SqlExpressionBuilder> builderLogger)
            : base(library, typeManager, builderLogger)
        {
        }

        public override DefinitionDictionary<SqlExpression> Build()
        {
            var definitions = new DefinitionDictionary<SqlExpression>();
            var localLibraryIdentifiers = new Dictionary<string, string>();

            var version = this.Library.identifier!.version;
            if (string.IsNullOrWhiteSpace(version))
                version = "1.0.0";

            if (!string.IsNullOrWhiteSpace(this.Library.identifier!.id))
            {
                var nav = ThisLibraryKey;
                if (this.Library.includes != null)
                {
                    foreach (var def in this.Library!.includes!)
                    {
                        var alias = !string.IsNullOrWhiteSpace(def.localIdentifier)
                            ? def.localIdentifier!
                            : def.path!;

                        var libNav = def.NameAndVersion();
                        if (libNav != null)
                        {
                            localLibraryIdentifiers.Add(alias, libNav);
                        }
                        else throw new InvalidOperationException($"Include {def.localId} does not have a well-formed name and version");
                    }
                }

                if (this.Library.valueSets != null)
                {
                    //foreach (var def in Library.valueSets!)
                    //{
                    //    var ctor = typeof(CqlValueSet).GetConstructor(new[] { typeof(string), typeof(string) }) ?? throw new InvalidOperationException("CqlValueSet type requires a constructor with two string parameters.");
                    //    var @new = Expression.New(ctor, Expression.Constant(def.id, typeof(string)), Expression.Constant(def.version, typeof(string)));
                    //    var contextParameter = Expression.Parameter(typeof(CqlContext), "context");
                    //    var lambda = Expression.Lambda(@new, contextParameter);
                    //    definitions.Add(ThisLibraryKey, def.name!, lambda);
                    //}
                }

                //var codeCtor = typeof(CqlCode).GetConstructor(new Type[]
                //{
                //    typeof(string),
                //    typeof(string),
                //    typeof(string),
                //    typeof(string)
                //})!;
                var codeSystemUrls = Library.codeSystems?
                    .ToDictionary(cs => cs.name, cs => cs.id) ?? new Dictionary<string, string>();
                var codesByName = new Dictionary<string, CqlCode>();
                var codesByCodeSystemName = new Dictionary<string, List<CqlCode>>();
                if (this.Library.codes != null)
                {
                    foreach (var code in Library.codes)
                    {
                        if (code.codeSystem == null)
                            throw new InvalidOperationException("Code definition has a null codeSystem node.");
                        if (!codeSystemUrls.TryGetValue(code.codeSystem.name, out var csUrl))
                            throw new InvalidOperationException($"Undefined code system {code.codeSystem.name!}");
                        var existingCode = codesByName.Values.SingleOrDefault(c => c.code == code.id && c.system == csUrl);
                        if (existingCode != null)
                            throw new InvalidOperationException($"Duplicate code detected: {code.id} from {code.codeSystem.name} ({csUrl})");
                        var systemCode = new CqlCode(code.id, csUrl, null, null);
                        codesByName.Add(code.name, systemCode);
                        if (!codesByCodeSystemName.TryGetValue(code.codeSystem!.name!, out var codings))
                        {
                            codings = new List<CqlCode>();
                            codesByCodeSystemName.Add(code.codeSystem!.name!, codings);
                        }
                        codings.Add(systemCode);

                        SqlExpression codeSqlExpression = BuildSelectForCode(systemCode);
                        definitions.Add(ThisLibraryKey, code.name!, codeSqlExpression);

                        //var newCodingExpression = Expression.New(codeCtor,
                        //    Expression.Constant(code.id),
                        //    Expression.Constant(csUrl),
                        //    Expression.Constant(null, typeof(string)),
                        //    Expression.Constant(null, typeof(string))!
                        //);
                        //var contextParameter = Expression.Parameter(typeof(CqlContext), "context");
                        //var lambda = Expression.Lambda(newCodingExpression, contextParameter);
                        //definitions.Add(ThisLibraryKey, code.name!, lambda);
                    }
                }

                if (this.Library.codeSystems != null)
                {
                    foreach (var codeSystem in Library.codeSystems)
                    {
                        if (codesByCodeSystemName.TryGetValue(codeSystem.name, out var codes))
                        {
                            var codesystemSqlExpression = BuildSelectForCodeSystem(codes);

                            definitions.Add(ThisLibraryKey, codeSystem.name, codesystemSqlExpression);

                            //var initMembers = codes
                            //    .Select(coding =>
                            //        Expression.New(codeCtor,
                            //            Expression.Constant(coding.code),
                            //            Expression.Constant(coding.system),
                            //            Expression.Constant(null, typeof(string)),
                            //            Expression.Constant(null, typeof(string))
                            //        ))
                            //    .ToArray();
                            //var arrayOfCodesInitializer = Expression.NewArrayInit(typeof(CqlCode), initMembers);
                            //var contextParameter = Expression.Parameter(typeof(CqlContext), "context");
                            //var lambda = Expression.Lambda(arrayOfCodesInitializer, contextParameter);
                            //definitions.Add(ThisLibraryKey, codeSystem.name, lambda);
                        }
                        else
                        {
                            throw new NotImplementedException("Empty codesystem declared");

                            //var newArray = Expression.NewArrayBounds(typeof(CqlCode), Expression.Constant(0, typeof(int)));
                            //var contextParameter = Expression.Parameter(typeof(CqlContext), "context");
                            //var lambda = Expression.Lambda(newArray, contextParameter);
                            //definitions.Add(ThisLibraryKey, codeSystem.name, lambda);
                        }
                    }
                }

                if (this.Library.concepts != null)
                {
                    //var conceptCtor = typeof(CqlConcept).GetConstructor(new[] { typeof(IEnumerable<CqlCode>), typeof(string) });
                    //foreach (var concept in Library.concepts)
                    //{
                    //    if (concept.code.Length > 0)
                    //    {
                    //        var initMembers = new Expression[concept.code.Length];
                    //        for (int i = 0; i < concept.code.Length; i++)
                    //        {
                    //            var codeRef = concept.code[i];
                    //            if (!codesByName.TryGetValue(codeRef.name, out var systemCode))
                    //                throw new InvalidOperationException($"Code {codeRef.name} in concept {concept.name} is not defined.");
                    //            initMembers[i] = Expression.New(codeCtor,
                    //                    Expression.Constant(systemCode.code),
                    //                    Expression.Constant(systemCode.system),
                    //                    Expression.Constant(null, typeof(string)),
                    //                    Expression.Constant(null, typeof(string))
                    //            );
                    //        }
                    //        var arrayOfCodesInitializer = Expression.NewArrayInit(typeof(CqlCode), initMembers);
                    //        var asEnumerable = Expression.TypeAs(arrayOfCodesInitializer, typeof(IEnumerable<CqlCode>));
                    //        var display = Expression.Constant(concept.display, typeof(string));
                    //        var newConcept = Expression.New(conceptCtor!, asEnumerable, display);
                    //        var contextParameter = Expression.Parameter(typeof(CqlContext), "context");
                    //        var lambda = Expression.Lambda(newConcept, contextParameter);
                    //        definitions.Add(ThisLibraryKey, concept.name, lambda);
                    //    }
                    //    else
                    //    {
                    //        var newArray = Expression.NewArrayBounds(typeof(CqlCode), Expression.Constant(0, typeof(int)));
                    //        var contextParameter = Expression.Parameter(typeof(CqlContext), "context");
                    //        var lambda = Expression.Lambda(newArray, contextParameter);
                    //        definitions.Add(ThisLibraryKey, concept.name, lambda);
                    //    }
                    //}
                }

                if (this.Library.parameters != null)
                {
                    foreach (var parameter in Library.parameters ?? Enumerable.Empty<Hl7.Cql.Elm.ParameterDef>())
                    {
                        if (definitions.ContainsKey(null, parameter.name!))
                            throw new InvalidOperationException($"There is already a definition named {parameter.name}");

                        if (parameter.@default != null)
                        {
                            var defaultValue = TranslateExpression(
                                parameter.@default, 
                                new SqlExpressionBuilderContext(this, localLibraryIdentifiers, definitions, 
                                "unfiltered"));
                            definitions.Add(ThisLibraryKey, parameter.name!, defaultValue);
                        }
                        else
                        {
                            throw new NotImplementedException("Parameter without default value");
                        }
                        //    var contextParameter = Expression.Parameter(typeof(CqlContext), "context");
                        //    var buildContext = new ExpressionBuilderContext(this,
                        //        contextParameter,
                        //        definitions,
                        //        localLibraryIdentifiers);

                        //    Expression? defaultValue = null;
                        //    if (parameter.@default != null)
                        //        defaultValue = Expression.TypeAs(TranslateExpression(parameter.@default, buildContext), typeof(object));
                        //    else defaultValue = Expression.Constant(null, typeof(object));

                        //    var resolveParam = Expression.Call(
                        //        contextParameter,
                        //        typeof(CqlContext).GetMethod(nameof(CqlContext.ResolveParameter))!,
                        //        Expression.Constant(Library.NameAndVersion),
                        //        Expression.Constant(parameter.name),
                        //        defaultValue
                        //    );

                        //    var parameterType = TypeManager.TypeFor(parameter.parameterTypeSpecifier!, buildContext);
                        //    var cast = Expression.Convert(resolveParam, parameterType);
                        //    // e.g. (bundle, context) => context.Parameters["Measurement Period"]
                        //    var lambda = Expression.Lambda(cast, contextParameter);

                        //    definitions.Add(ThisLibraryKey, parameter.name!, lambda);
                    }
                }

                foreach (var def in this.Library.statements ?? Enumerable.Empty<Hl7.Cql.Elm.ExpressionDef>())
                {
                    if (def.expression != null)
                    {
                        var context = def.context;

                        //var contextParameter = Expression.Parameter(typeof(CqlContext), "context");
                        var buildContext = new SqlExpressionBuilderContext(this,
                            localLibraryIdentifiers,
                            definitions,
                            context);

                        if (string.IsNullOrWhiteSpace(def.name))
                        {
                            var message = $"Definition with local ID {def.localId} does not have a name.  This is not allowed.";
                            buildContext.LogError(message, def);
                            throw new InvalidOperationException(message);
                        }
                        //var customKey = $"{nav}.{def.name}";
                        //Type[] functionParameterTypes = Type.EmptyTypes;
                        //var parameters = new[] { buildContext.RuntimeContextParameter };
                        var function = def as FunctionDef;

                        // jeffcou:  believe this section is for functions with parameters

                        //if (function != null && function.operand != null)
                        //{
                        //    functionParameterTypes = new Type[function.operand!.Length];
                        //    int i = 0;
                        //    foreach (var operand in function.operand!)
                        //    {
                        //        if (operand.operandTypeSpecifier != null)
                        //        {
                        //            var operandType = TypeManager.TypeFor(operand.operandTypeSpecifier, buildContext)!;
                        //            var opName = ExpressionBuilderContext.NormalizeIdentifier(operand.name);
                        //            var parameter = Expression.Parameter(operandType, opName);
                        //            buildContext.Operands.Add(operand.name!, parameter);
                        //            functionParameterTypes[i] = parameter.Type;
                        //            i += 1;
                        //        }
                        //        else throw new InvalidOperationException($"Operand for function {def.name} is missing its {nameof(operand.operandTypeSpecifier)} property");
                        //    }

                        //    parameters = parameters
                        //        .Concat(buildContext.Operands.Values)
                        //        .ToArray();
                        //    if (CustomImplementations.TryGetValue(customKey, out var factory) && factory != null)
                        //    {
                        //        var customLambda = factory(parameters);
                        //        definitions.Add(ThisLibraryKey, def.name, functionParameterTypes, customLambda);
                        //        continue;
                        //    }
                        //    else if (function?.external ?? false)
                        //    {
                        //        var message = $"{customKey} is declared external, but {nameof(CustomImplementations)} does not define this function.";
                        //        buildContext.LogError(message, def);
                        //        if (Settings.AllowUnresolvedExternals)
                        //        {
                        //            var returnType = TypeManager.TypeFor(def, buildContext, throwIfNotFound: true)!;
                        //            var paramTypes = new[] { typeof(CqlContext) }
                        //                .Concat(functionParameterTypes)
                        //                .ToArray();
                        //            var notImplemented = NotImplemented(customKey, paramTypes, returnType, buildContext);
                        //            definitions.Add(ThisLibraryKey, def.name, paramTypes, notImplemented);
                        //            continue;
                        //        }
                        //        else throw new InvalidOperationException(message);
                        //    }

                        //}

                        // create SELECT statment

                        try
                        {
                            buildContext.LogInfo($"Building SQL expression for {def.name}");

                            buildContext = buildContext.Deeper(def);

                            var bodyExpression = TranslateExpression(def.expression, buildContext);

                            definitions.Add(ThisLibraryKey, def.name, new Type[0], bodyExpression);
                        }
                        catch (Exception e)
                        {
                            buildContext.LogError($"Failed to create SQL expression for {def.name} --- {e.Message}", def);
                        }
                        
                        //var lambda = Expression.Lambda(bodyExpression, parameters);
                        //if (function?.operand != null && definitions.ContainsKey(ThisLibraryKey, def.name, functionParameterTypes))
                        //{
                        //    var ops = function.operand
                        //        .Where(op => op.operandTypeSpecifier != null && op.operandTypeSpecifier.resultTypeName != null)
                        //        .Select(op => $"{op.name} {op.operandTypeSpecifier!.resultTypeName!}");
                        //    var message = $"Function {def.name}({string.Join(", ", ops)}) skipped; another function matching this signature already exists.";
                        //    buildContext.LogWarning(message, def);
                        //}
                        //else
                        //{
                        //    foreach (var annotation in def.annotation?.OfType<Annotation>() ?? Enumerable.Empty<Annotation>())
                        //    {
                        //        foreach (var tag in annotation.t ?? Enumerable.Empty<Tag>())
                        //        {
                        //            var name = tag.name;
                        //            if (!string.IsNullOrWhiteSpace(name))
                        //            {
                        //                var value = tag.value ?? string.Empty;
                        //                definitions.AddTag(ThisLibraryKey, def.name, functionParameterTypes ?? new Type[0], name, value);

                        //            }
                        //        }
                        //    }
                        //    definitions.Add(ThisLibraryKey, def.name, functionParameterTypes, lambda);
                        //}
                    }
                    else
                    {
                        throw new InvalidOperationException($"Definition {def.name} does not have an expression property");
                    }
                }
                return definitions;
            }
            else
            {
                throw new InvalidOperationException("This package does not have a name and version.");
            }
        }

        private static List<Identifier> cqlCodeColumns = new List<Identifier>()
            {
                new Identifier { Value = "code" },
                new Identifier { Value = "codesystem" },
                new Identifier { Value = "display" },
                new Identifier { Value = "ver" },
            };

        private SqlExpression BuildSelectForCodeSystem(List<CqlCode> codes)
        {
            InlineDerivedTable inlineTable = new InlineDerivedTable
            {
                Alias = new Identifier { Value = "codes" }
            };

            // sigh.  readonly property
            cqlCodeColumns.ForEach(e => inlineTable.Columns.Add(e));

            foreach (var code in codes)
            {
                var rowValue = new RowValue
                {
                    ColumnValues =
                    {
                        new StringLiteral { Value = code.code },
                        new StringLiteral { Value = code.system },
                        code.display != null ? new StringLiteral { Value = code.display } : new NullLiteral(),
                        code.version != null ? new StringLiteral { Value = code.version } : new NullLiteral()
                    }
                };
                inlineTable.RowValues.Add(rowValue);
            }

            var select = new SelectStatement
            {
                QueryExpression = new QuerySpecification
                {
                    SelectElements =
                    {
                        new SelectStarExpression()
                    },
                    FromClause = new FromClause
                    {
                        TableReferences =
                        {
                            inlineTable
                        }
                    }
                }
            };

            return new SqlExpression(
                select, 
                SqlExpression.FragmentTypes.SelectStatement, 
                typeof(IEnumerable<CqlCode>));
        }

        private SqlExpression BuildSelectForCode(CqlCode systemCode)
        {
            var codes = BuildSelectForCodeSystem(new List<CqlCode> { systemCode });

            // since this represents a scalar code, rewrap with type of CqlCode
            return new SqlExpression(codes.SqlFragment, SqlExpression.FragmentTypes.SelectStatement, typeof(CqlCode));
        }

        private SqlExpression TranslateExpression(Element op, SqlExpressionBuilderContext ctx)
        {
            ctx = ctx.Deeper(op);
            SqlExpression? result = null;
            switch (op)
            {
                case Abs abs:
                    // result = Abs(abs, ctx);
                    break;
                case Add add:
                    result = Add(add, ctx);
                    break;
                case After after:
                    result = After(after, ctx);
                    break;
                case AliasRef ar:
                    result = AliasRef(ar, ctx);
                    break;
                case AllTrue alt:
                    // result = AllTrue(alt, ctx);
                    break;
                case And and:
                    result = And(and, ctx);
                    break;
                case As @as:
                    result = As(@as, ctx);
                    break;
                case AnyTrue ate:
                    // result = AnyTrue(ate, ctx);
                    break;
                case AnyInValueSet avs:
                    // result = AnyInValueSet(avs, ctx);
                    break;
                case Avg avg:
                    // result = Avg(avg, ctx);
                    break;
                case Before before:
                    result = Before(before, ctx);
                    break;
                case CalculateAgeAt caa:
                    result = CalculateAgeAt(caa, ctx);
                    break;
                case CalculateAge ca:
                    // result = CalculateAge(ca, ctx);
                    break;
                case Case ce:
                    // result = Case(ce, ctx);
                    break;
                case Ceiling ceil:
                    // result = Ceiling(ceil, ctx);
                    break;
                case Coalesce cle:
                    // result = Coalesce(cle, ctx);
                    break;
                case CodeRef cre:
                    result = CodeRef(cre, ctx);
                    break;
                case CodeSystemRef csr:
                    // result = CodeSystemRef(csr, ctx);
                    break;
                case Collapse col:
                    // result = Collapse(col, ctx);
                    break;
                case Combine com:
                    // result = Combine(com, ctx);
                    break;
                case Concatenate cctn:
                    // result = Concatenate(cctn, ctx);
                    break;
                case ConceptRef cr:
                    // result = ConceptRef(cr, ctx);
                    break;
                case Contains ct:
                    // result = Contains(ct, ctx);
                    break;
                case ConvertQuantity cqe:
                    // result = ConvertQuantity(cqe, ctx);
                    break;
                case ConvertsToBoolean ce:
                    // result = ConvertsToBoolean(ce, ctx);
                    break;
                case ConvertsToDate ce:
                    // result = ConvertsToDate(ce, ctx);
                    break;
                case ConvertsToDateTime ce:
                    // result = ConvertsToDateTime(ce, ctx);
                    break;
                case ConvertsToDecimal ce:
                    // result = ConvertsToDecimal(ce, ctx);
                    break;
                case ConvertsToLong ce:
                    // result = ConvertsToLong(ce, ctx);
                    break;
                case ConvertsToInteger ce:
                    // result = ConvertsToInteger(ce, ctx);
                    break;
                case ConvertsToQuantity ce:
                    // result = ConvertsToQuantity(ce, ctx);
                    break;
                case ConvertsToString ce:
                    // result = ConvertsToString(ce, ctx);
                    break;
                case ConvertsToTime ce:
                    // result = ConvertsToTime(ce, ctx);
                    break;
                case Count ce:
                    result = Count(ce, ctx);
                    break;
                case DateFrom dfe:
                    // result = DateFrom(dfe, ctx);
                    break;
                case Elm.DateTime dt:
                    result = DateTime(dt, ctx);
                    break;
                case Date d:
                    result = Date(d, ctx);
                    break;
                case DateTimeComponentFrom dtcf:
                    // result = DateTimeComponentFrom(dtcf, ctx);
                    break;
                case Descendents desc:
                    // result = Descendents(desc, ctx);
                    break;
                case DifferenceBetween dbe:
                    // result = DifferenceBetween(dbe, ctx);
                    break;
                case Distinct distinct:
                    // result = Distinct(distinct, ctx);
                    break;
                case Divide divide:
                    result = Divide(divide, ctx);
                    break;
                case DurationBetween dbe:
                    // result = DurationBetween(dbe, ctx);
                    break;
                case End e:
                    result = End(e, ctx);
                    break;
                case Ends e:
                    // result = Ends(e, ctx);
                    break;
                case EndsWith e:
                    // result = EndsWith(e, ctx);
                    break;
                case Equal eq:
                    result = Equal(eq, ctx);
                    break;
                case Equivalent eqv:
                    // result = Equivalent(eqv, ctx);
                    break;
                case Except ex:
                    // result = Except(ex, ctx);
                    break;
                case Exists ex:
                    result = Exists(ex, ctx);
                    break;
                case Exp exe:
                    // result = Exp(exe, ctx);
                    break;
                case Expand expand:
                    // result = Expand(expand, ctx);
                    break;
                case ExpandValueSet evs:
                    // result = ExpandValueSet(evs, ctx);
                    break;
                case FunctionRef fre:
                    result = FunctionRef(fre, ctx);
                    break;
                case ExpressionRef ere:
                    result = ExpressionRef(ere, ctx);
                    break;
                case First first:
                    // result = First(first, ctx);
                    break;
                case Flatten fl:
                    // result = Flatten(fl, ctx);
                    break;
                case Floor floor:
                    // result = Floor(floor, ctx);
                    break;
                case GeometricMean gme:
                    // result = GeometricMean(gme, ctx);
                    break;
                case Greater gtr:
                    result = Greater(gtr, ctx);
                    break;
                case GreaterOrEqual gtre:
                    result = GreaterOrEqual(gtre, ctx);
                    break;
                case HighBoundary hb:
                    // result = HighBoundary(hb, ctx);
                    break;
                case IdentifierRef ire:
                    // result = IdentifierRef(ire, ctx);
                    break;
                case If @if:
                    // result = If(@if, ctx);
                    break;
                case Implies implies:
                    // result = Implies(implies, ctx);
                    break;
                case Includes inc:
                    // result = Includes(inc, ctx);
                    break;
                case IncludedIn ii:
                    // result = IncludedIn(ii, ctx);
                    break;
                case Indexer idx:
                    // result = Indexer(idx, ctx);
                    break;
                case IndexOf io:
                    // result = IndexOf(io, ctx);
                    break;
                case Instance ine:
                    // result = Instance(ine, ctx);
                    break;
                case Intersect ise:
                    // result = Intersect(ise, ctx);
                    break;
                case Interval ie:
                    result = Intervalresult(ie, ctx);
                    break;
                case InValueSet inv:
                    // result = InValueSet(inv, ctx);
                    break;
                case In @in:
                    result = In(@in, ctx);
                    break;
                case Is @is:
                    // result = Is(@is, ctx);
                    break;
                case IsFalse @isn:
                    // result = IsFalse(@isn, ctx);
                    break;
                case IsNull @isn:
                    // result = IsNull(@isn, ctx);
                    break;
                case IsTrue @isn:
                    // result = IsTrue(@isn, ctx);
                    break;
                case Last last:
                    // result = Last(last, ctx);
                    break;
                case LastPositionOf lpo:
                    // result = LastPositionOf(lpo, ctx);
                    break;
                case Length len:
                    // result = Length(len, ctx);
                    break;
                case Less less:
                    result = Less(less, ctx);
                    break;
                case LessOrEqual lesse:
                    result = LessOrEqual(lesse, ctx);
                    break;
                case List list:
                    // result = List(list, ctx);
                    break;
                case Elm.Literal lit:
                    result = Literal(lit, ctx);
                    break;
                case Ln ln:
                    // result = Ln(ln, ctx);
                    break;
                case Log log:
                    // result = Log(log, ctx);
                    break;
                case LowBoundary lb:
                    // result = LowBoundary(lb, ctx);
                    break;
                case Lower e:
                    // result = Lower(e, ctx);
                    break;
                case Matches e:
                    // result = Matches(e, ctx);
                    break;
                case Max max:
                    // result = Max(max, ctx);
                    break;
                case MaxValue max:
                    // result = MaxValue(max, ctx);
                    break;
                case Median med:
                    // result = Median(med, ctx);
                    break;
                case Meets meets:
                    // result = Meets(meets, ctx);
                    break;
                case MeetsBefore meets:
                    // result = MeetsBefore(meets, ctx);
                    break;
                case MeetsAfter meets:
                    // result = MeetsAfter(meets, ctx);
                    break;
                case Message msg:
                    // result = Message(msg, ctx);
                    break;
                case Min min:
                    // result = Min(min, ctx);
                    break;
                case MinValue min:
                    // result = MinValue(min, ctx);
                    break;
                case Mode mode:
                    // result = Mode(mode, ctx);
                    break;
                case Modulo mod:
                    // result = Modulo(mod, ctx);
                    break;
                case Multiply mul:
                    result = Multiply(mul, ctx);
                    break;
                case Negate neg:
                    // result = Negate(neg, ctx);
                    break;
                case Not not:
                    // result = Not(not, ctx);
                    break;
                case NotEqual ne:
                    result = NotEqual(ne, ctx);
                    break;
                case Now now:
                    // result = Now(now, ctx);
                    break;
                case Null @null:
                    // result = Null(@null, ctx);
                    break;
                case OperandRef ore:
                    // result = OperandRef(ore, ctx);
                    break;
                case Or or:
                    result = Or(or, ctx);
                    break;
                case Overlaps ole:
                    // result = Overlaps(ole, ctx);
                    break;
                case OverlapsAfter ola:
                    // result = OverlapsAfter(ola, ctx);
                    break;
                case OverlapsBefore olb:
                    // result = OverlapsBefore(olb, ctx);
                    break;
                case ParameterRef pre:
                    result = ParameterRef(pre, ctx);
                    break;
                case PointFrom pf:
                    // result = PointFrom(pf, ctx);
                    break;
                case PopulationStdDev pstd:
                    // result = PopulationStdDev(pstd, ctx);
                    break;
                case PopulationVariance pvar:
                    // result = PopulationVariance(pvar, ctx);
                    break;
                case PositionOf po:
                    // result = PositionOf(po, ctx);
                    break;
                case Power pow:
                    // result = Power(pow, ctx);
                    break;
                case Precision pre:
                    // result = Precision(pre, ctx);
                    break;
                case Predecessor prd:
                    // result = Predecessor(prd, ctx);
                    break;
                case Product prod:
                    // result = Product(prod, ctx);
                    break;
                case ProperContains pc:
                    // result = ProperContains(pc, ctx);
                    break;
                case ProperIn pi:
                    // result = ProperIn(pi, ctx);
                    break;
                case ProperIncludes pi:
                    // result = ProperIncludes(pi, ctx);
                    break;
                case ProperIncludedIn pie:
                    // result = ProperIncludedIn(pie, ctx);
                    break;
                case Property pe:
                    result = Property(pe, ctx);
                    break;
                case Quantity qua:
                    result = Quantity(qua, ctx);
                    break;
                case Query qe:
                    result = Query(qe, ctx);
                    break;
                case QueryLetRef qlre:
                    // result = QueryLetRef(qlre, ctx);
                    break;
                case Ratio re:
                    // result = Ratio(re, ctx);
                    break;
                case ReplaceMatches e:
                    // result = ReplaceMatches(e, ctx);
                    break;
                case Retrieve re:
                    result = Retrieve(re, ctx);
                    break;
                case Round rnd:
                    // result = Round(rnd, ctx);
                    break;
                case SameAs sa:
                    // result = SameAs(sa, ctx);
                    break;
                case SameOrAfter soa:
                    // result = SameOrAfter(soa, ctx);
                    break;
                case SameOrBefore sob:
                    // result = SameOrBefore(sob, ctx);
                    break;
                case SingletonFrom sf:
                    result = SingletonFrom(sf, ctx);
                    break;
                case Slice slice:
                    // result = Slice(slice, ctx);
                    break;
                case Split split:
                    // result = Split(split, ctx);
                    break;
                case Substring e:
                    // result = Substring(e, ctx);
                    break;
                case Subtract sub:
                    result = Subtract(sub, ctx);
                    break;
                case Successor suc:
                    // result = Successor(suc, ctx);
                    break;
                case Sum sum:
                    // result = Sum(sum, ctx);
                    break;
                case Starts starts:
                    // result = Starts(starts, ctx);
                    break;
                case Start start:
                    result = Start(start, ctx);
                    break;
                case StartsWith e:
                    // result = StartsWith(e, ctx);
                    break;
                case StdDev stddev:
                    // result = StdDev(stddev, ctx);
                    break;
                case Time time:
                    // result = Time(time, ctx);
                    break;
                case TimeOfDay tod:
                    // result = TimeOfDay(tod, ctx);
                    break;
                case TimezoneOffsetFrom tofe:
                    // result = TimezoneOffsetFrom(tofe, ctx);
                    break;
                case ToBoolean e:
                    // result = ToBoolean(e, ctx);
                    break;
                case ToConcept tc:
                    // result = ToConcept(tc, ctx);
                    break;
                case ToDateTime tdte:
                    result = ToDateTime(tdte, ctx);
                    break;
                case ToDate tde:
                    // result = ToDate(tde, ctx);
                    break;
                case Today today:
                    // result = Today(today, ctx);
                    break;
                case ToDecimal tde:
                    result = ToDecimal(tde, ctx);
                    break;
                case ToInteger tde:
                    // result = ToInteger(tde, ctx);
                    break;
                case ToList tle:
                    result = ToList(tle, ctx);
                    break;
                case ToLong toLong:
                    // result = ToLong(toLong, ctx);
                    break;
                case ToQuantity tq:
                    // result = ToQuantity(tq, ctx);
                    break;
                case ToString e:
                    // result = ToString(e, ctx);
                    break;
                case ToTime e:
                    // result = ToTime(e, ctx);
                    break;
                case Truncate trunc:
                    // result = Truncate(trunc, ctx);
                    break;
                case TruncatedDivide div:
                    // result = TruncatedDivide(div, ctx);
                    break;
                case Elm.Tuple tu:
                    // result = Tuple(tu, ctx);
                    break;
                case Union ue:
                    // result = Union(ue, ctx);
                    break;
                case ValueSetRef vsre:
                    // result = ValueSetRef(vsre, ctx);
                    break;
                case Variance variance:
                    // result = Variance(variance, ctx);
                    break;
                case Upper e:
                    // result = Upper(e, ctx);
                    break;
                case Width width:
                    // result = Width(width, ctx);
                    break;
                case Xor xor:
                    // result = Xor(xor, ctx);
                    break;
            }

            if (result == null)
                throw new InvalidOperationException($"Expression {op.GetType().FullName} is not implemented.");

            //foreach (var visitor in ExpressionMutators)
            //{
            //    if (visitor != null)
            //        expression = visitor.Mutate(expression!, op, ctx);
            //}

            return result!;
        }

        private SqlExpression? ToDateTime(ToDateTime tdte, SqlExpressionBuilderContext ctx)
        {
            // HACK:  assume noop
            return TranslateExpression(tdte.operand, ctx);
        }

        private SqlExpression? CalculateAgeAt(CalculateAgeAt caa, SqlExpressionBuilderContext ctx)
        {
            var birthdate = TranslateExpression(caa.operand[0], ctx);
            var asOf = TranslateExpression(caa.operand[1], ctx);
            var precision = caa.precision;

            if (precision != DateTimePrecision.Year)
            {
                // don't know anything but years yet
                throw new NotImplementedException();
            }

            var (birthDateUnwrapped, birthDateFrom) = UnwrapScalarSelectElement(birthdate);
            var (asOfUnwrapped, asOfFrom) = UnwrapScalarSelectElement(asOf);

            // create
            // SELECT select DATEDIFF(YEAR, birthDate, asOf) - 
            //   CASE WHEN(MONTH(birthDate) > MONTH(asOf)) OR (MONTH(birthDate) = MONTH(asOf) AND DAY(birthDate) > DAY(asOf))
            //     THEN 1
            //     ELSE 0
            //   END
            var selectStatement = new SelectStatement
            {
                QueryExpression = new QuerySpecification
                {
                    SelectElements =
                    {
                        new SelectScalarExpression
                        {
                            Expression = new BinaryExpression
                            {
                                BinaryExpressionType = BinaryExpressionType.Subtract,
                                FirstExpression = new FunctionCall
                                {
                                    FunctionName = new Identifier { Value = "DATEDIFF" },
                                    Parameters =
                                    {
                                        new ColumnReferenceExpression { MultiPartIdentifier = new MultiPartIdentifier { Identifiers = { new Identifier { Value = "YEAR" , QuoteType = QuoteType.NotQuoted } } } },
                                        birthDateUnwrapped,
                                        asOfUnwrapped
                                    }
                                },
                                SecondExpression = new SearchedCaseExpression
                                {
                                    WhenClauses =
                                    {
                                        new SearchedWhenClause
                                        {
                                            WhenExpression = new BooleanBinaryExpression
                                            {
                                                BinaryExpressionType = BooleanBinaryExpressionType.Or,
                                                FirstExpression = new BooleanParenthesisExpression
                                                { 
                                                    Expression = new BooleanComparisonExpression
                                                    {
                                                        ComparisonType = BooleanComparisonType.GreaterThan,
                                                        FirstExpression = new FunctionCall
                                                        {
                                                            FunctionName = new Identifier { Value = "MONTH" },
                                                            Parameters = { birthDateUnwrapped }
                                                        },
                                                        SecondExpression = new FunctionCall
                                                        {
                                                            FunctionName = new Identifier { Value = "MONTH" },
                                                            Parameters = { asOfUnwrapped }
                                                        }
                                                    }
                                                },
                                                SecondExpression = new BooleanParenthesisExpression
                                                {
                                                    Expression = new BooleanBinaryExpression
                                                    {
                                                        BinaryExpressionType = BooleanBinaryExpressionType.And,
                                                        FirstExpression = new BooleanComparisonExpression
                                                        {
                                                            ComparisonType = BooleanComparisonType.Equals,
                                                            FirstExpression = new FunctionCall
                                                            {
                                                                FunctionName = new Identifier { Value = "MONTH" },
                                                                Parameters = { birthDateUnwrapped }
                                                            },
                                                            SecondExpression = new FunctionCall
                                                            {
                                                                FunctionName = new Identifier { Value = "MONTH" },
                                                                Parameters = { asOfUnwrapped }
                                                            }
                                                        },
                                                        SecondExpression = new BooleanComparisonExpression
                                                        {
                                                            ComparisonType = BooleanComparisonType.GreaterThan,
                                                            FirstExpression = new FunctionCall
                                                            {
                                                                FunctionName = new Identifier { Value = "DAY" },
                                                                Parameters = { birthDateUnwrapped }
                                                            },
                                                            SecondExpression = new FunctionCall
                                                            {
                                                                FunctionName = new Identifier { Value = "DAY" },
                                                                Parameters = { asOfUnwrapped }
                                                            }
                                                        }
                                                    }
                                                }
                                            },
                                            ThenExpression = new IntegerLiteral { Value = "1" }
                                        }
                                    },
                                    ElseExpression = new IntegerLiteral { Value = "0" }
                                }
                            },
                            ColumnName = new IdentifierOrValueExpression { Identifier = new Identifier { Value = ResultColumnName } }
                        }
                    },
                    FromClause = ReconcileScalarFromClauses(birthDateFrom, asOfFrom)
                }
            };

            // HACK -- if in filtered context, add the context column
            if (InFilteredContext(ctx))
            {
                var querySpecification = FindQuerySpecification(selectStatement);
                querySpecification.SelectElements.Add(new SelectScalarExpression
                {
                    // TODO: table name here
                    Expression = BuildColumnReference(ContextColumnName),
                });
            }

            return new SqlExpression(
                selectStatement,
                SqlExpression.FragmentTypes.SelectStatementScalar,
                typeof(int));
        }

        private SqlExpression? AliasRef(AliasRef ar, SqlExpressionBuilderContext ctx)
        {
            // HACK
            // this shows up when it is a reference to the Result column in the where clause of a query
            // so the right thing to do is construct a SELECT Result statement

            var selectStatement = new SelectStatement
            {
                QueryExpression = new QuerySpecification
                {
                    SelectElements =
                    {
                        new SelectScalarExpression
                        {
                            Expression = BuildColumnReference(ResultColumnName),
                            ColumnName = new IdentifierOrValueExpression { Identifier = new Identifier { Value = ResultColumnName } }
                        }
                    },
                }
            };

            return new SqlExpression(
                selectStatement,
                SqlExpression.FragmentTypes.SelectStatement,
                typeof(object));
        }

        private SqlExpression? Start(Start start, SqlExpressionBuilderContext ctx)
            => BuildIntervalPartQuery(start.operand, "low", ctx);

        private SqlExpression? End(End e, SqlExpressionBuilderContext ctx) 
            => BuildIntervalPartQuery(e.operand, "hi", ctx);

        private SqlExpression BuildIntervalPartQuery(Hl7.Cql.Elm.Expression elmOperand, string intervalPart, SqlExpressionBuilderContext ctx)
        {
            var operand = TranslateExpression(elmOperand, ctx);

            // create SELECT TOP 1 [hi] FROM <operand>
            var selectStatement = new SelectStatement
            {
                QueryExpression = new QuerySpecification
                {
                    SelectElements =
                    {
                        new SelectScalarExpression
                        {
                            Expression = BuildColumnReference(SourceTableAlias, intervalPart),
                            ColumnName = new IdentifierOrValueExpression 
                            { 
                                Identifier = new Identifier { Value = ResultColumnName, QuoteType = QuoteType.SquareBracket } 
                            }
                        }
                    },
                    FromClause = new FromClause
                    {
                        TableReferences =
                        {
                            new QueryDerivedTable
                            {
                                QueryExpression = FindQuerySpecification(operand),
                                Alias = new Identifier { Value = SourceTableAlias, QuoteType = QuoteType.SquareBracket }
                            }
                        }
                    },
                    TopRowFilter = new TopRowFilter
                    {
                        Expression = new IntegerLiteral { Value = "1" }
                    }
                }
            };

            return new SqlExpression(
                selectStatement,
                SqlExpression.FragmentTypes.SelectStatement,
                typeof(CqlDateTime));
        }

        private SqlExpression? ParameterRef(ParameterRef pre, SqlExpressionBuilderContext ctx)
        {
            return BuildSqlFunctionReference(pre.name, pre, ctx);
        }

        private SqlExpression? Count(Count ce, SqlExpressionBuilderContext ctx)
        {
            var sourceExpression = TranslateExpression(ce.source, ctx);
            var sourceSelect = sourceExpression.SqlFragment as SelectStatement ?? throw new InvalidOperationException();

            // there are 3 cases:
            // 1. unfiltered context counting an unfiltered context.  returns a single scalar
            //    - that's just a Count(1) of the subquery
            // 2. unfiltered context counting a filtered context.  returns a single scalar (the count of top-level items in the filtered context)
            //    - that's a Count(DISTINCT _Context) of the subquery
            // 3. filtered context counting a filtered context.  returns a list of scalars
            //    - does a COUNT(1) GroupBy _Context

            var baseQuery = FindQuerySpecification(sourceSelect);
            var firstTableReference = baseQuery.FromClause.TableReferences.Single();

            var functionName = (firstTableReference as SchemaObjectFunctionTableReference)?.SchemaObject?.BaseIdentifier?.Value;

            // HACK
            // look up the from clause of the sourceExpression
            // - does it match a known def?
            // - if it does, what is the context of that def?
            //   - is it something other than unfiltered
            bool countingFilteredDefine = 
                !String.IsNullOrEmpty(functionName)
                && ctx.Definitions.TryGetValue(this.ThisLibraryKey, functionName, out var definition)
                && !String.Equals(definition?.CqlContext, "unfiltered", StringComparison.InvariantCultureIgnoreCase);
            bool isFilteredContext = InFilteredContext(ctx);

            // used for case 1 & 3
            SelectScalarExpression count1 = new SelectScalarExpression
            {
                Expression = new FunctionCall
                {
                    FunctionName = new Identifier { Value = "COUNT" },
                    Parameters = { new IntegerLiteral { Value = "1" } },
                },
                ColumnName = new IdentifierOrValueExpression { Identifier = new Identifier { Value = ResultColumnName, QuoteType = QuoteType.SquareBracket } }
            };
            // used for case 2
            SelectScalarExpression countDistinctContext = new SelectScalarExpression
            {
                Expression = new FunctionCall
                {
                    FunctionName = new Identifier { Value = "COUNT" },
                    UniqueRowFilter = UniqueRowFilter.Distinct,
                    Parameters = { BuildColumnReference(SourceTableAlias, ContextColumnName) },
                },
                ColumnName = new IdentifierOrValueExpression { Identifier = new Identifier { Value = ResultColumnName, QuoteType = QuoteType.SquareBracket } }
            };

            // base query 
            var select = new SelectStatement
            {
                QueryExpression = new QuerySpecification
                {
                    SelectElements =
                    {
                        !isFilteredContext && countingFilteredDefine
                                ? countDistinctContext   // case 2
                                : count1,                // case 1 & 3
                    },
                    FromClause = new FromClause
                    {
                        TableReferences =
                        {
                            new QueryDerivedTable
                            {
                                QueryExpression = sourceSelect.QueryExpression,
                                Alias = new Identifier { Value = SourceTableAlias, QuoteType = QuoteType.SquareBracket }
                            }
                        }
                    },
                }
            };

            // for case 3, add a groupby
            if (isFilteredContext && countingFilteredDefine)
            {
                // add Context column and groupby

                var querySpec = FindQuerySpecification(select);

                querySpec.SelectElements.Add(new SelectScalarExpression
                {
                    Expression = BuildColumnReference(SourceTableAlias, ContextColumnName),
                });

                querySpec.GroupByClause = new GroupByClause
                {
                    GroupingSpecifications =
                    {
                        new ExpressionGroupingSpecification
                        {
                            Expression = BuildColumnReference(SourceTableAlias, ContextColumnName)
                        }
                    }
                };
            }

            return new SqlExpression(
                select, 
                SqlExpression.FragmentTypes.SelectStatementScalar, 
                typeof(long),
                ctx.CqlContext);
        }

        private SqlExpression? SingletonFrom(SingletonFrom sf, SqlExpressionBuilderContext ctx)
        {
            var listExpression = TranslateExpression(sf.operand, ctx);
            var listSelect = listExpression.SqlFragment as SelectStatement ?? throw new InvalidOperationException();

            // wrap in SELECT TOP(1) * FROM <listexpression>
            var select = new SelectStatement
            {
                QueryExpression = new QuerySpecification
                {
                    SelectElements =
                    {
                        new SelectStarExpression()
                    },
                    FromClause = new FromClause
                    {
                        TableReferences =
                        {
                            new QueryDerivedTable
                            {
                                QueryExpression = listSelect.QueryExpression,
                                Alias = new Identifier { Value = UnusedTableName, QuoteType = QuoteType.SquareBracket }
                            }
                        }
                    },
                    TopRowFilter = new TopRowFilter
                    {
                        Expression = new IntegerLiteral { Value = "1" }
                    }
                }
            };

            if (InFilteredContext(ctx))
            {
                // remove the top 1 
                var querySpec = FindQuerySpecification(select);
                querySpec.TopRowFilter = null;
            }

            return new SqlExpression(
                select, 
                SqlExpression.FragmentTypes.SelectStatement, 
                listExpression.DataType,
                ctx.CqlContext);
        }

        private bool InFilteredContext(SqlExpressionBuilderContext ctx)
        {
            return !String.IsNullOrEmpty(ctx.CqlContext)
                && !String.Equals(ctx.CqlContext, "unfiltered", StringComparison.InvariantCultureIgnoreCase);
        }

        private SqlExpression? Exists(Exists ex, SqlExpressionBuilderContext ctx)
        {
            var existsExpression = TranslateExpression(ex.operand, ctx);
            var existsSelect = existsExpression.SqlFragment as SelectStatement ?? throw new InvalidOperationException();

            // wrap the given select in a pattern
            // SELECT IIF( (SELECT COUNT(1) AS Result FROM (<existsexpression>) AS UNUSED)> 0,1,0) FROM (SELECT NULL AS unused_column) AS UNUSED

            var newSelect = new SelectStatement
            {
                QueryExpression = new QuerySpecification
                {
                    SelectElements =
                    {
                        new SelectScalarExpression
                        {
                            Expression = new IIfCall
                            {
                                Predicate = new BooleanComparisonExpression
                                {
                                    FirstExpression = new ScalarSubquery
                                    {
                                        QueryExpression = new QuerySpecification
                                        {
                                            SelectElements =
                                            {
                                                new SelectScalarExpression
                                                {
                                                    Expression = new FunctionCall
                                                    {
                                                        FunctionName = new Identifier { Value = "COUNT" },
                                                        Parameters = { new IntegerLiteral { Value = "1" } }
                                                    }
                                                }
                                            },
                                            FromClause = new FromClause
                                            {
                                                TableReferences =
                                                {
                                                    new QueryDerivedTable
                                                    {
                                                        QueryExpression = existsSelect.QueryExpression,
                                                        Alias = new Identifier { Value = UnusedTableName, QuoteType = QuoteType.SquareBracket }
                                                    }
                                                }
                                            }
                                        }
                                    },
                                    ComparisonType = BooleanComparisonType.GreaterThan,
                                    SecondExpression = new IntegerLiteral { Value = "0" }
                                },
                                ThenExpression = new IntegerLiteral { Value = "1" },
                                ElseExpression = new IntegerLiteral { Value = "0" }
                            },
                            ColumnName = new IdentifierOrValueExpression { Identifier = new Identifier { Value = ResultColumnName, QuoteType = QuoteType.SquareBracket } }
                        }
                    },
                    FromClause = NullFromClause()
                }
            };

            return new SqlExpression(
                newSelect, 
                SqlExpression.FragmentTypes.SelectStatementScalar, 
                typeof(bool),
                ctx.CqlContext);
        }

        /// <summary>
        /// builds a select statement returning a scalar interval tuple
        /// </summary>
        /// <param name="ie"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private SqlExpression? Intervalresult(Interval ie, SqlExpressionBuilderContext ctx)
        {
            var lowExpression = TranslateExpression(ie.low, ctx);
            var hiExpression = TranslateExpression(ie.high, ctx);
            var lowClosed = ie.lowClosed;
            var hiClosed = ie.highClosed;

            // TODO disabling this check for now because type is not being full tracked
            //if (lowExpression.DataType != hiExpression.DataType)
            //    throw new InvalidOperationException("low and hi of Interval must be same type");

            var (lowTuple, lowFrom) = UnwrapScalarSelectElements(lowExpression);
            var (hiTuple, hiFrom) = UnwrapScalarSelectElements(hiExpression);

            if (lowTuple.Count > 1 || hiTuple.Count > 1)
                throw new InvalidOperationException();

            var intervalTuple = new ScalarTuple
            {
                DataType = typeof(CqlInterval<>).MakeGenericType(lowExpression.DataType)
            };

            // TODO: consider populating .net type and reflecting in order to build tuple
            intervalTuple.Values.Add("low", lowTuple.SingleValue);
            intervalTuple.Values.Add("hi", hiTuple.SingleValue);
            intervalTuple.Values.Add("lowClosed", new IntegerLiteral { Value = lowClosed ? "1" : "0" });
            intervalTuple.Values.Add("hiClosed", new IntegerLiteral { Value = hiClosed ? "1" : "0" });

            // stategy - build a scalar select which returns tuple of the interval type.  similar to cqlcode
            // (can this be genericized?)

            var fromClause = ReconcileScalarFromClauses(lowFrom, hiFrom);

            return WrapInSelectScalarExpression(intervalTuple, fromClause, ctx);
        }

        // returns a select statement wrapping BooleanExpression
        private SqlExpression? BooleanComparisonOperator(Elm.BinaryExpression booleanExpression, BooleanComparisonType bct, SqlExpressionBuilderContext ctx)
        {
            // unwrap both sides from their Selects, then re-wrap any scalar expression on either side

            var originalLhs = TranslateExpression(booleanExpression.operand[0], ctx);
            var (unwrappedLhs, lhsFrom) = UnwrapScalarSelectElement(originalLhs);

            var originalRhs = TranslateExpression(booleanExpression.operand[1], ctx);
            var (unwrappedRhs, rhsFrom) = UnwrapScalarSelectElement(originalRhs);

            ScalarExpression newLhs = originalLhs.FragmentType == SqlExpression.FragmentTypes.SelectStatementScalar && !InFilteredContext(ctx)
                    ? WrapInSelectScalarExpression(unwrappedLhs, lhsFrom, ctx, true).SqlFragment as ScalarSubquery ?? throw new InvalidOperationException()
                    : unwrappedLhs;
            ScalarExpression newRhs = originalRhs.FragmentType == SqlExpression.FragmentTypes.SelectStatementScalar && !InFilteredContext(ctx)
                    ? WrapInSelectScalarExpression(unwrappedRhs, rhsFrom, ctx, true).SqlFragment as ScalarSubquery ?? throw new InvalidOperationException()
                    : unwrappedRhs;

            var binaryExpression = new BooleanComparisonExpression
            {
                FirstExpression = newLhs,
                ComparisonType = bct,
                SecondExpression = newRhs,
            };

            return WrapBooleanExpressionInSelectStatement(
                binaryExpression, 
                ReconcileScalarFromClauses(lhsFrom, rhsFrom),
                ctx);
        }

        private SqlExpression? After(After after, SqlExpressionBuilderContext ctx) =>
            BooleanComparisonOperator(after, BooleanComparisonType.GreaterThan, ctx);

        private SqlExpression? Before(Before before, SqlExpressionBuilderContext ctx) =>
            BooleanComparisonOperator(before, BooleanComparisonType.LessThan, ctx);

        private SqlExpression? Greater(Greater greater, SqlExpressionBuilderContext ctx) =>
            BooleanComparisonOperator(greater, BooleanComparisonType.GreaterThan, ctx);

        private SqlExpression? GreaterOrEqual(GreaterOrEqual greaterOrEqual, SqlExpressionBuilderContext ctx) =>
            BooleanComparisonOperator(greaterOrEqual, BooleanComparisonType.GreaterThanOrEqualTo, ctx);

        private SqlExpression? Less(Less less, SqlExpressionBuilderContext ctx) =>
            BooleanComparisonOperator(less, BooleanComparisonType.LessThan, ctx);

        private SqlExpression? LessOrEqual(LessOrEqual lessOrEqual, SqlExpressionBuilderContext ctx) =>
            BooleanComparisonOperator(lessOrEqual, BooleanComparisonType.LessThanOrEqualTo, ctx);

        // this only works for the same types as SQL right now
        private SqlExpression? Equal(Equal eq, SqlExpressionBuilderContext ctx) =>
            BooleanComparisonOperator(eq, BooleanComparisonType.Equals, ctx);

        private SqlExpression? NotEqual(NotEqual neq, SqlExpressionBuilderContext ctx) =>
            BooleanComparisonOperator(neq, BooleanComparisonType.NotEqualToExclamation, ctx);

        // returns BooleanExpression; expects Select operands
        private SqlExpression? In(Elm.In inExpression, SqlExpressionBuilderContext ctx)
        {
            // unwrap both sides from their Selects, then re-wrap any scalar expression on either side

            var originalLhs = TranslateExpression(inExpression.operand[0], ctx);
            var (unwrappedLhs, lhsFrom) = UnwrapScalarSelectElements(originalLhs);

            var originalRhs = TranslateExpression(inExpression.operand[1], ctx);
            var (unwrappedRhs, rhsFrom) = UnwrapScalarSelectElements(originalRhs);

            // "lhs in rhs" is equivalent to
            // "(lhs > rhs.start AND lhs < rhs.end)" (or >= <= depending on open/closed)
            // (and assuming multiple evaluations of lhs/rhs are ok.  No side effects, right?)
            //
            // the dynamic version is
            // (rhs.lowClosed = 0 and lhs > rhs.low or lhs >= rhs.low) and (rhs.hiClosed = 0 and lhs < rhs.hi or lhs <= rhs.hi)

            // assuming rhs is an interval --- what if it is a columnref that is an interval?
            if (unwrappedRhs.DataType.GetGenericTypeDefinition() != typeof(CqlInterval<>))
                throw new InvalidOperationException();

            // assuming lhs is a single value 
            var lhsSingleValue = unwrappedLhs.SingleValue;

            // rewrap rhs parts as a scalarsubquery
            var rhsLow = WrapInSelectScalarExpression(unwrappedRhs.Values["low"], rhsFrom, ctx, true);
            var rhsHi = WrapInSelectScalarExpression(unwrappedRhs.Values["hi"], rhsFrom, ctx, true);

            BooleanExpression resultExpression;

            // TODO:  this works if the interval is a literal
            // if it's a columnref, we'll just hardcode to true for now.   But to fix it right, it needs to generate conditional
            if (unwrappedRhs.Values["lowClosed"] is Microsoft.SqlServer.TransactSql.ScriptDom.Literal)
            {
                bool lowClosed = SqlIntegerScalarExpressionToBoolean(unwrappedRhs.Values["lowClosed"]);
                bool hiClosed = SqlIntegerScalarExpressionToBoolean(unwrappedRhs.Values["hiClosed"]);

                resultExpression = new BooleanParenthesisExpression
                {
                    Expression = new BooleanBinaryExpression
                    {
                        FirstExpression = new BooleanComparisonExpression
                        {
                            FirstExpression = lhsSingleValue,
                            ComparisonType = lowClosed ? BooleanComparisonType.GreaterThanOrEqualTo : BooleanComparisonType.GreaterThan,
                            SecondExpression = rhsLow.SqlFragment as ScalarSubquery ?? throw new InvalidOperationException(),
                        },
                        BinaryExpressionType = BooleanBinaryExpressionType.And,
                        SecondExpression = new BooleanComparisonExpression
                        {
                            FirstExpression = lhsSingleValue,
                            ComparisonType = hiClosed ? BooleanComparisonType.LessThanOrEqualTo : BooleanComparisonType.LessThan,
                            SecondExpression = rhsHi.SqlFragment as ScalarSubquery ?? throw new InvalidOperationException(),
                        }
                    }
                };


            }
            else
            {
                var rhsLowClosed = WrapInSelectScalarExpression(unwrappedRhs.Values["lowClosed"], rhsFrom, ctx, true);
                var rhsHiClosed = WrapInSelectScalarExpression(unwrappedRhs.Values["hiClosed"], rhsFrom, ctx, true);

                // the dynamic version is

                // ((rhs.lowClosed = 0 and lhs > rhs.low) or (rhs.lowClosed = 1 and lhs >= rhs.low)) and
                // ((rhs.hiClosed = 0 and lhs < rhs.hi) or (rhs.hiClosed = 1 and lhs <= rhs.hi))
                resultExpression = new BooleanParenthesisExpression
                {
                    Expression = new BooleanBinaryExpression
                    {
                        // ((rhs.lowClosed = 0 and lhs > rhs.low) or (rhs.lowClosed = 1 and lhs >= rhs.low))
                        FirstExpression = new BooleanParenthesisExpression
                        {
                            Expression = new BooleanBinaryExpression
                            {
                                //(rhs.lowClosed = 0 and lhs > rhs.low)
                                FirstExpression = new BooleanParenthesisExpression
                                {
                                    Expression = new BooleanBinaryExpression
                                    {
                                        //rhs.lowClosed = 0
                                        FirstExpression = new BooleanComparisonExpression
                                        {
                                            FirstExpression = rhsLowClosed.SqlFragment as ScalarSubquery ?? throw new InvalidOperationException(),
                                            ComparisonType = BooleanComparisonType.Equals,
                                            SecondExpression = new IntegerLiteral { Value = "0" },
                                        },
                                        BinaryExpressionType = BooleanBinaryExpressionType.And,
                                        //lhs > rhs.low
                                        SecondExpression = new BooleanComparisonExpression
                                        {
                                            FirstExpression = lhsSingleValue,
                                            ComparisonType = BooleanComparisonType.GreaterThan,
                                            SecondExpression = rhsLow.SqlFragment as ScalarSubquery ?? throw new InvalidOperationException(),
                                        }
                                    }
                                },
                                BinaryExpressionType = BooleanBinaryExpressionType.Or,
                                //(rhs.lowClosed = 1 and lhs >= rhs.low)
                                SecondExpression = new BooleanParenthesisExpression
                                {
                                    Expression = new BooleanBinaryExpression
                                    {
                                        //rhs.lowClosed = 1
                                        FirstExpression = new BooleanComparisonExpression
                                        {
                                            FirstExpression = rhsLowClosed.SqlFragment as ScalarSubquery ?? throw new InvalidOperationException(),
                                            ComparisonType = BooleanComparisonType.Equals,
                                            SecondExpression = new IntegerLiteral { Value = "1" },
                                        },
                                        BinaryExpressionType = BooleanBinaryExpressionType.And,
                                        //lhs >= rhs.low
                                        SecondExpression = new BooleanComparisonExpression
                                        {
                                            FirstExpression = lhsSingleValue,
                                            ComparisonType = BooleanComparisonType.GreaterThanOrEqualTo,
                                            SecondExpression = rhsLow.SqlFragment as ScalarSubquery ?? throw new InvalidOperationException(),
                                        }
                                    }
                                }
                            }
                        },
                        BinaryExpressionType = BooleanBinaryExpressionType.And,
                        //((rhs.hiClosed = 0 and lhs < rhs.hi) or (rhs.hiClosed = 1 and lhs <= rhs.hi))
                        SecondExpression = new BooleanParenthesisExpression
                        {
                            Expression = new BooleanBinaryExpression
                            {
                                //(rhs.hiClosed = 0 and lhs < rhs.hi)
                                FirstExpression = new BooleanParenthesisExpression
                                {
                                    Expression = new BooleanBinaryExpression
                                    {
                                        //rhs.hiClosed = 0
                                        FirstExpression = new BooleanComparisonExpression
                                        {
                                            FirstExpression = rhsHiClosed.SqlFragment as ScalarSubquery ?? throw new InvalidOperationException(),
                                            ComparisonType = BooleanComparisonType.Equals,
                                            SecondExpression = new IntegerLiteral { Value = "0" },
                                        },
                                        BinaryExpressionType = BooleanBinaryExpressionType.And,
                                        //lhs < rhs.hi
                                        SecondExpression = new BooleanComparisonExpression
                                        {
                                            FirstExpression = lhsSingleValue,
                                            ComparisonType = BooleanComparisonType.LessThan,
                                            SecondExpression = rhsHi.SqlFragment as ScalarSubquery ?? throw new InvalidOperationException(),
                                        }
                                    }
                                },
                                BinaryExpressionType = BooleanBinaryExpressionType.Or,
                                //(rhs.hiClosed = 1 and lhs <= rhs.hi)
                                SecondExpression = new BooleanParenthesisExpression
                                {
                                    Expression = new BooleanBinaryExpression
                                    {
                                        //rhs.hiClosed = 1
                                        FirstExpression = new BooleanComparisonExpression
                                        {
                                            FirstExpression = rhsHiClosed.SqlFragment as ScalarSubquery ?? throw new InvalidOperationException(),
                                            ComparisonType = BooleanComparisonType.Equals,
                                            SecondExpression = new IntegerLiteral { Value = "1" },
                                        },
                                        BinaryExpressionType = BooleanBinaryExpressionType.And,
                                        //lhs <= rhs.hi
                                        SecondExpression = new BooleanComparisonExpression
                                        {
                                            FirstExpression = lhsSingleValue,
                                            ComparisonType = BooleanComparisonType.LessThanOrEqualTo,
                                            SecondExpression = rhsHi.SqlFragment as ScalarSubquery ?? throw new InvalidOperationException(),
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }

            return WrapBooleanExpressionInSelectStatement(
                resultExpression,
                ReconcileScalarFromClauses(lhsFrom, rhsFrom),
                ctx);
        }

        private bool SqlIntegerScalarExpressionToBoolean(ScalarExpression scalarExpression)
        {
            if (scalarExpression is IntegerLiteral integerLiteral)
                return Int32.Parse(integerLiteral.Value, System.Globalization.CultureInfo.InvariantCulture) != 0;
            else
                throw new InvalidOperationException();
        }

        private SqlExpression? BooleanOperator(Elm.BinaryExpression binaryExpression, BooleanBinaryExpressionType bbet, SqlExpressionBuilderContext ctx)
        {
            var (lhs, lhsFrom) = UnwrapBooleanExpression(TranslateExpression(binaryExpression.operand[0], ctx));
            var (rhs, rhsFrom) = UnwrapBooleanExpression(TranslateExpression(binaryExpression.operand[1], ctx));

            return WrapBooleanExpressionInSelectStatement(
                new BooleanBinaryExpression
                {
                    FirstExpression = lhs,
                    BinaryExpressionType = bbet,
                    SecondExpression = rhs,
                },
                ReconcileScalarFromClauses(lhsFrom, rhsFrom),
                ctx);
        }

        // returns a boolean expression; expects BooleanExpression operands
        private SqlExpression? And(And and, SqlExpressionBuilderContext ctx) =>
            BooleanOperator(and, BooleanBinaryExpressionType.And, ctx);

        private SqlExpression? Or(Or or, SqlExpressionBuilderContext ctx) =>
            BooleanOperator(or, BooleanBinaryExpressionType.Or, ctx);


        // create a subselect expression that builds a datetime2
        private SqlExpression? DateTime(Elm.DateTime dt, SqlExpressionBuilderContext ctx)
        {
            var (year, fromYear) = UnwrapScalarSelectElement(TranslateExpression(dt.year, ctx));
            var (month, fromMonth) = UnwrapScalarSelectElement(TranslateExpression(dt.month, ctx));
            var (day, fromDay) = UnwrapScalarSelectElement(TranslateExpression(dt.day, ctx));
            var (hour, fromHour) = UnwrapScalarSelectElement(TranslateExpression(dt.hour, ctx));
            var (minute, fromMinute) = UnwrapScalarSelectElement(TranslateExpression(dt.minute, ctx));
            var (second, fromSecond) = UnwrapScalarSelectElement(TranslateExpression(dt.second, ctx));
            var (millisecond, fromMillisecond) = UnwrapScalarSelectElement(TranslateExpression(dt.millisecond, ctx));


            var functionExpression = new FunctionCall
            {
                FunctionName = new Identifier { Value = "DATETIME2FROMPARTS" },
                Parameters =
                {
                    year,
                    month,
                    day,
                    hour,
                    minute,
                    second,
                    millisecond,
                    new IntegerLiteral { Value = "7" }, // precision hardcoded
                }
            };

            FromClause combinedFrom = ReconcileScalarFromClauses(fromYear, fromMonth, fromDay, fromHour, fromMinute, fromSecond, fromMillisecond);

            return WrapInSelectScalarExpression(functionExpression, combinedFrom, ctx);
        }

        private SqlExpression? Date(Date d, SqlExpressionBuilderContext ctx)
        {
            var (year, fromYear) = UnwrapScalarSelectElement(TranslateExpression(d.year, ctx));
            var (month, fromMonth) = UnwrapScalarSelectElement(TranslateExpression(d.month, ctx));
            var (day, fromDay) = UnwrapScalarSelectElement(TranslateExpression(d.day, ctx));

            var functionExpression = new FunctionCall
            {
                FunctionName = new Identifier { Value = "DATEFROMPARTS" },
                Parameters =
                {
                    year,
                    month,
                    day,
                }
            };

            FromClause combinedFrom = ReconcileScalarFromClauses(fromYear, fromMonth, fromDay);

            return WrapInSelectScalarExpression(functionExpression, combinedFrom, ctx);
        }

        private SqlExpression? Quantity(Quantity qua, SqlExpressionBuilderContext ctx)
        {
            ScalarTuple tuple = new ScalarTuple
            {
                DataType = typeof(CqlQuantity),
                Values =
                {
                    { "value", new NumericLiteral { Value = qua.value.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                    { "units", new StringLiteral { Value = qua.unit } }
                }
            };

            return WrapInSelectScalarExpression(
                tuple, 
                null, 
                ctx);
        }

        // returns SelectStatement
        // TODO: returns a fully formed select with table name, which might not be relevant to all contexts?
        private SqlExpression? Property(Property pe, SqlExpressionBuilderContext ctx)
        {
            SelectScalarExpression? selectScalarExpression = null;

            string sourceTableTypeName;
            Identifier sourceTableIdentifier;
            FromClause fromClause;
            string? elmElementType = null; // TODO not sure we can always figure this out?  but works in some cases

            if (!String.IsNullOrEmpty(pe.scope))
            {
                // I think? this is the case where the property source is the result of a Retrieve.   Don't know if this is always true
                var sourceTable = ctx.GetScope(pe.scope) ?? throw new InvalidOperationException();
                sourceTableTypeName = sourceTable.Type.Name;
                sourceTableIdentifier = sourceTable.SqlExpression as Identifier ?? throw new InvalidOperationException();
                elmElementType = (sourceTable.ElmExpression as Elm.Retrieve)?.dataType.Name;

                fromClause = new FromClause
                {
                    TableReferences =
                            {
                                new NamedTableReference
                                {
                                    SchemaObject = new SchemaObjectName
                                    {
                                        Identifiers =
                                        {
                                            sourceTableIdentifier
                                        }
                                    }
                                }
                            }
                };
            }
            else
            {
                // TODO this seems to be a pattern
                // this means the source is the result of another expression
                var sourceExpression = TranslateExpression(pe.source, ctx);
                sourceTableTypeName = sourceExpression.DataType.Name;

                // sigh.  HACK that happens for patient.birthDate.value, where the source table 
                if (sourceTableTypeName == "Object")
                {
                    sourceTableTypeName = "Patient";
                }

                sourceTableIdentifier = new Identifier { Value = sourceTableTypeName, QuoteType = QuoteType.SquareBracket };

                // the from clause is the same as the one from the source expression
                fromClause = FindTableReference(sourceExpression.SqlFragment);
            }

            // map the property name to a column name -- TODO: should be a fancier lookup
            switch (sourceTableTypeName.ToLowerInvariant())
            {
                case "condition":
                    {
                        switch (pe.path)
                        {
                            case "onset":
                                selectScalarExpression = BuildSimpleColumnReference(sourceTableIdentifier, "onsetDateTime");
                                break;
                        }
                        break;
                    }
                case "patient":
                    {
                        switch (pe.path)
                        {
                            case "birthDate":
                            case "birthDate.value":  // HACK
                                selectScalarExpression = BuildSimpleColumnReference(sourceTableIdentifier, "birthDate");
                                break;
                        }
                        break;
                    }
                case "procedure":
                    {
                        switch (pe.path)
                        {
                            case "performed":   // TODO: hack because performed is a range
                                selectScalarExpression = new SelectScalarExpression
                                {
                                    Expression = new FunctionCall
                                    {
                                        FunctionName = new Identifier { Value = "JSON_VALUE" },
                                        Parameters =
                                        {
                                            new ColumnReferenceExpression
                                            {
                                                MultiPartIdentifier = new MultiPartIdentifier
                                                {
                                                    Identifiers =
                                                    {
                                                        sourceTableIdentifier,
                                                        new Identifier { Value = "performedPeriod_string", QuoteType = QuoteType.SquareBracket }
                                                    }
                                                }
                                            },
                                            new StringLiteral { Value = "$.end" }
                                        }
                                    },
                                    ColumnName = new IdentifierOrValueExpression
                                    {
                                        Identifier = new Identifier { Value = ResultColumnName, QuoteType = QuoteType.SquareBracket }
                                    }
                                };
                                break;
                            case "status":
                                selectScalarExpression = BuildSimpleColumnReference(sourceTableIdentifier, "status");
                                break;
                        }
                        break;
                    }
            }

            if (selectScalarExpression == null)
                throw new NotImplementedException();

            var sqlSelect = new SelectStatement
            {
                QueryExpression = new QuerySpecification
                {
                    SelectElements =
                        {
                            selectScalarExpression,
                        },
                    FromClause = fromClause
                }
            };

            // if this is a filtered context, then also add a context column
            // TODO right now other code assumes that the property column is _first_ (because it is unwrapped in other places, like where clauses)
            // but in cases where this SELECT is used as-in, we need to add the context column
            var context = FindElmContext(ctx);
            if (elmElementType != null
                && context != null
                && FhirSqlTableMap.TryGetValue(elmElementType, out var sqlTableEntry)
                && sqlTableEntry.ContextIdentifierExpression != null
                && sqlTableEntry.ContextIdentifierExpression.TryGetValue(context.ToLowerInvariant(), out var contextIdentifierExpression))
            {
                // if there needs to be a context column, add it
                (sqlSelect.QueryExpression as QuerySpecification ?? throw new InvalidOperationException())?.SelectElements.Add(new SelectScalarExpression
                {
                    Expression = contextIdentifierExpression,
                    ColumnName = new IdentifierOrValueExpression
                    {
                        Identifier = new Identifier { Value = ContextColumnName, QuoteType = QuoteType.SquareBracket }
                    }
                });
            }

            return new SqlExpression(
                sqlSelect,
                SqlExpression.FragmentTypes.SelectStatement,
                typeof(object),      // TODO: infer type?;
                ctx.CqlContext);

            // TODO this is possibly duplicative, but worry about combining later
            static SelectScalarExpression BuildSimpleColumnReference(Identifier sourceTableIdentifier, string destinationcolumnName)
            {
                return new SelectScalarExpression
                {
                    Expression = new ColumnReferenceExpression
                    {
                        MultiPartIdentifier = new MultiPartIdentifier
                        {
                            Identifiers =
                                {
                                    sourceTableIdentifier,
                                    new Identifier { Value = destinationcolumnName, QuoteType = QuoteType.SquareBracket }
                                }
                        }
                    },
                    ColumnName = new IdentifierOrValueExpression
                    {
                        Identifier = new Identifier { Value = ResultColumnName, QuoteType = QuoteType.SquareBracket }
                    }
                };
            }
        }

        private SqlExpression? As(As @as, SqlExpressionBuilderContext ctx)
        {
            // TODO: make this no-op for now

            return TranslateExpression(@as.operand, ctx);
        }

        private SqlExpression? FunctionRef(FunctionRef fre, SqlExpressionBuilderContext ctx)
        {
            if (StringComparer.InvariantCultureIgnoreCase.Compare(fre.libraryName, "FHIRHelpers") == 0)
            {
                if (StringComparer.InvariantCultureIgnoreCase.Compare(fre.name, "ToDateTime") == 0
                ||  StringComparer.InvariantCultureIgnoreCase.Compare(fre.name, "ToDate") == 0)
                {
                    // TODO: no-op the ToDateTime function since we'll assume it already is a datetime
                    return TranslateExpression(fre.operand[0], ctx);
                }
                else if (StringComparer.InvariantCultureIgnoreCase.Compare(fre.name, "ToString") == 0)
                {
                    // also assume this is a no-op
                    return TranslateExpression(fre.operand[0], ctx);
                }
            }
            throw new NotImplementedException();
        }

        private SqlExpression? ToList(ToList tle, SqlExpressionBuilderContext ctx)
        {
            // big hack -- everything is a list already?
            return TranslateExpression(tle.operand, ctx);
        }

        /// <summary>
        /// </summary>
        /// <param name="cre"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private SqlExpression? CodeRef(CodeRef cre, SqlExpressionBuilderContext ctx)
        {
            var querySpecification = new QuerySpecification
            {
                FromClause = new FromClause
                {
                    TableReferences =
                        {
                            new SchemaObjectFunctionTableReference
                            {
                                SchemaObject = new SchemaObjectName
                                {
                                    Identifiers =
                                    {
                                        new Identifier { Value = ScopedSymbolsContext.NormalizeIdentifier(cre.name), QuoteType = QuoteType.SquareBracket }
                                    }
                                }
                            }
                        }
                },
                TopRowFilter = new TopRowFilter
                {
                    Expression = new IntegerLiteral { Value = "1" }
                }
            };

            // readonly list property.  Add the cqlcode columns
            cqlCodeColumns.ForEach(
                e =>
                querySpecification.SelectElements.Add(new SelectScalarExpression
                {
                    Expression = new ColumnReferenceExpression
                    {
                        MultiPartIdentifier = new MultiPartIdentifier
                        {
                            Identifiers =
                            {
                                e
                            }
                        }
                    }
                }));

            return new SqlExpression(
                new SelectStatement
                {
                    QueryExpression = querySpecification,
                },
                SqlExpression.FragmentTypes.SelectStatementScalar,
                typeof(CqlCode),
                ctx.CqlContext);
        }

        private SqlExpression? Query(Query query, SqlExpressionBuilderContext ctx)
        {
            if (query?.source?.Length == 0)
                throw new NotSupportedException("Queries must define at least 1 source");
            else if (query!.source!.Length == 1)
                return SingleSourceQuery(query, ctx);
            else
                return MultiSourceQuery(query, ctx);
        }

        // returns a SelectStatement
        private SqlExpression? SingleSourceQuery(Query query, SqlExpressionBuilderContext ctx)
        {
            var querySource = query.source![0];

            var querySourceAlias = querySource.alias;

            if (string.IsNullOrWhiteSpace(querySource.alias))
                throw new ArgumentException("Only aliased query sources are supported.", nameof(query));

            if (querySource.expression == null)
                throw new ArgumentException("Query sources must have an expression", nameof(query));

            // fully formed SELECT * with table (and _for now_ no where clause)
            var sourceExpression = TranslateExpression(querySource.expression!, ctx);
            var sourceSelect = sourceExpression.FragmentType == SqlExpression.FragmentTypes.SelectStatement
                ? sourceExpression.SqlFragment as SelectStatement ?? throw new InvalidOperationException()
                : throw new InvalidOperationException();

            // TODO:  assume this is a SelectStatement

            // this seems hacky but for now it works.   get the table alias in the from clause (which should be there for both table and TVFunction) and make it available to the where below
            var firstTableReference = FindTableReference(sourceSelect)?.TableReferences?[0] ?? throw new InvalidOperationException();

            TableReferenceWithAlias fromClauseTableReference;
            // more hack -- if the from clause is a join here, it means (only?) that it was a joined with a code table.  in that case, take the FirstTableReference of the join
            switch (firstTableReference)
            {
                case TableReferenceWithAlias tableReferenceWithAlias:
                    fromClauseTableReference = tableReferenceWithAlias;
                    break;
                case QualifiedJoin qualifiedJoin:
                    fromClauseTableReference = qualifiedJoin.FirstTableReference as TableReferenceWithAlias ?? throw new InvalidOperationException();
                    break;
                default:
                    throw new InvalidOperationException();
            }
            Identifier fromClauseTableAlias = fromClauseTableReference.Alias ?? throw new InvalidOperationException();

            // further modification necessary?

            //var isSingle = false;
            //// promote single objects into enumerables so where works
            //if (!IsOrImplementsIEnumerableOfT(source.Type))
            //{
            //    var arrayInit = Expression.NewArrayInit(source.Type, source);
            //    source = arrayInit;
            //    isSingle = true;
            //}
            //Type elementType = TypeResolver.GetListElementType(@return.Type, @throw: true)!;

            //// handle with/such-that
            //if (query.relationship != null)
            //{
            //    foreach (var relationship in query.relationship ?? Enumerable.Empty<RelationshipClause>())
            //    {
            //        var selectManyLambda = WithToSelectManyBody(querySourceAlias!, elementType, relationship, ctx);

            //        var selectManyCall = OperatorBinding.Bind(CqlOperator.SelectMany, ctx.RuntimeContextParameter,
            //            @return, selectManyLambda);
            //        if (relationship is Without)
            //        {
            //            var callExcept = OperatorBinding.Bind(CqlOperator.ListExcept, ctx.RuntimeContextParameter,
            //                @return, selectManyCall);
            //            @return = callExcept;
            //        }
            //        else
            //        {
            //            @return = selectManyCall;
            //        }
            //    }
            //}
            // The element type may have changed
            //elementType = TypeResolver.GetListElementType(@return.Type, @throw: true)!;

            if (query.where != null)
            {
                var parameterName = ExpressionBuilderContext.NormalizeIdentifier(querySourceAlias)
                    ?? throw new NotImplementedException();
                //    var whereLambdaParameter = Expression.Parameter(elementType, parameterName);
                //    if (querySourceAlias == "ItemOnLine")
                //    {
                //    }

                // TODO: lifted this pattern from Retrieve; unclear if Query follows same
                Type sourceElementType;
                if (query.resultTypeSpecifier == null)
                {
                    if (string.IsNullOrWhiteSpace(query.resultTypeName.Name))
                        throw new ArgumentException("If a Query lacks a ResultTypeSpecifier it must have a ResultTypeName", nameof(query));
                    string cqlQueryResultType = query.resultTypeName.Name;

                    sourceElementType = TypeResolver.ResolveType(cqlQueryResultType) ?? throw new InvalidOperationException();
                }
                else
                {
                    if (query.resultTypeSpecifier is Elm.ListTypeSpecifier listTypeSpecifier)
                    {
                        //cqlQueryResultType = listTypeSpecifier.elementType is Elm.NamedTypeSpecifier nts ? nts.name.Name : null;
                        sourceElementType = TypeManager.TypeFor(listTypeSpecifier.elementType, ctx);
                    }
                    else throw new NotImplementedException($"Sources with type {query.resultTypeSpecifier.GetType().Name} are not implemented.");
                }

                // create a new scope with the given alias, a table reference, and the FHIR type of the table
                Identifier tableIdentifier = fromClauseTableAlias;  // see above
                var scopes = new[]
                {
                    new KeyValuePair<string, ScopedSqlExpression>(
                        querySourceAlias!,
                        new ScopedSqlExpression(tableIdentifier, querySource.expression, sourceElementType))
                };
                var subContext = ctx.WithScopes(scopes);

                //    if (query.let != null)
                //    {
                //        var letScopes = new KeyValuePair<string, ScopedExpression>[query.let.Length];
                //        for (int i = 0; i < query.let.Length; i++)
                //        {
                //            var let = query.let[i];
                //            var expression = TranslateExpression(let.expression!, subContext);
                //            letScopes[i] = new KeyValuePair<string, ScopedExpression>(let.identifier!, new ScopedExpression(expression, let.expression!));
                //        }
                //        subContext = subContext.WithScopes(letScopes);
                //    }

                var whereExpression = TranslateExpression(query.where, subContext);
                // TODO: how to reconcile the fromClause in the where 
                var (whereBody, whereFrom) = UnwrapBooleanExpression(whereExpression);

                var querySpecification = FindQuerySpecification(sourceSelect);

                querySpecification.WhereClause = new WhereClause
                {
                    SearchCondition = whereBody
                };


                // TODO:  join with existing query (possibly adding WHERE clause or AND with existing)

                //    var whereLambda = System.Linq.Expressions.Expression.Lambda(whereBody, whereLambdaParameter);
                //    var callWhere = OperatorBinding.Bind(CqlOperator.Where, ctx.RuntimeContextParameter, @return, whereLambda);
                //    @return = callWhere;
            }

            //if (query.@return != null)
            //{
            //    var parameterName = ExpressionBuilderContext.NormalizeIdentifier(querySourceAlias)
            //    ?? TypeNameToIdentifier(elementType, ctx);


            //    var selectLambdaParameter = Expression.Parameter(elementType, parameterName);

            //    var scopes = new[] { new KeyValuePair<string, ScopedExpression>(querySourceAlias!, new ScopedExpression(selectLambdaParameter, query.@return)) };
            //    var subContext = ctx.WithScopes(scopes);

            //    if (query.let != null)
            //    {
            //        for (int i = 0; i < query.let.Length; i++)
            //        {
            //            var let = query.let[i];
            //            var expression = TranslateExpression(let.expression!, subContext);
            //            subContext = subContext.WithScopes(new KeyValuePair<string, ScopedExpression>(let.identifier!, new ScopedExpression(expression, let.expression!)));
            //        }
            //    }
            //    var selectBody = TranslateExpression(query.@return.expression!, subContext);
            //    var selectLambda = Expression.Lambda(selectBody, selectLambdaParameter);
            //    var callSelect = OperatorBinding.Bind(CqlOperator.Select, ctx.RuntimeContextParameter, @return, selectLambda);
            //    @return = callSelect;
            //}

            //if (query.aggregate != null)
            //{
            //    var parameterName = ExpressionBuilderContext.NormalizeIdentifier(querySourceAlias)
            //    ?? TypeNameToIdentifier(elementType, ctx);
            //    var sourceAliasParameter = Expression.Parameter(elementType, parameterName);
            //    var resultAlias = query.aggregate.identifier!;
            //    Type? resultType = null;
            //    if (query.aggregate.resultTypeSpecifier != null)
            //    {
            //        resultType = TypeManager.TypeFor(query.aggregate.resultTypeSpecifier, ctx);
            //    }
            //    else if (!string.IsNullOrWhiteSpace(query.aggregate.resultTypeName.Name!))
            //    {
            //        resultType = TypeResolver.ResolveType(query.aggregate.resultTypeName.Name!);
            //    }
            //    if (resultType == null)
            //    {
            //        throw new InvalidOperationException($"Could not resolve aggregate query result type for query {query.localId} at {query.locator}");
            //    }
            //    var resultParameter = Expression.Parameter(resultType, resultAlias);
            //    var scopes = new[]
            //    {
            //            new KeyValuePair < string, ScopedExpression > (querySourceAlias !, new ScopedExpression(sourceAliasParameter, query)),
            //            new KeyValuePair < string, ScopedExpression > (resultAlias !, new ScopedExpression(resultParameter, query.aggregate))
            //        };
            //    var subContext = ctx.WithScopes(scopes);
            //    if (query.let != null)
            //    {
            //        for (int i = 0; i < query.let.Length; i++)
            //        {
            //            var let = query.let[i];
            //            var expression = TranslateExpression(let.expression!, subContext);
            //            subContext = subContext.WithScopes(new KeyValuePair<string, ScopedExpression>(let.identifier!, new ScopedExpression(expression, let.expression!)));
            //        }
            //    }
            //    var startingValue = TranslateExpression(query.aggregate.starting!, subContext);

            //    var lambdaBody = TranslateExpression(query.aggregate.expression!, subContext);
            //    var lambda = Expression.Lambda(lambdaBody, resultParameter, sourceAliasParameter);
            //    var aggregateCall = OperatorBinding.Bind(CqlOperator.Aggregate, subContext.RuntimeContextParameter, @return, lambda, startingValue);
            //    @return = aggregateCall;
            //}


            ////[System.Xml.Serialization.XmlIncludeAttribute(typeof(ByExpression))]
            ////[System.Xml.Serialization.XmlIncludeAttribute(typeof(ByColumn))]
            ////[System.Xml.Serialization.XmlIncludeAttribute(typeof(ByDirection))]
            //if (query.sort != null && query.sort.by != null && query.sort.by.Length > 0)
            //{
            //    foreach (var by in query.sort.by)
            //    {
            //        ListSortDirection order = ExtensionMethods.ListSortOrder(by.direction);
            //        if (by is ByExpression byExpression)
            //        {
            //            var parameterName = "@this";
            //            var returnElementType = TypeResolver.GetListElementType(@return.Type, true)!;
            //            var sortMemberParameter = Expression.Parameter(returnElementType, parameterName);
            //            var subContext = ctx.WithImpliedAlias(parameterName!, sortMemberParameter, byExpression.expression);
            //            var sortMemberExpression = TranslateExpression(byExpression.expression, subContext);
            //            var lambdaBody = Expression.Convert(sortMemberExpression, typeof(object));
            //            var sortLambda = System.Linq.Expressions.Expression.Lambda(lambdaBody, sortMemberParameter);
            //            var sort = OperatorBinding.Bind(CqlOperator.SortBy, ctx.RuntimeContextParameter,
            //                @return, sortLambda, Expression.Constant(order, typeof(ListSortDirection)));
            //            @return = sort;
            //        }
            //        else if (by is ByColumn byColumn)
            //        {
            //            var parameterName = "@this";
            //            var returnElementType = TypeResolver.GetListElementType(@return.Type, true)!;
            //            var sortMemberParameter = Expression.Parameter(returnElementType, parameterName);
            //            var pathMemberType = TypeManager.TypeFor(byColumn, ctx);
            //            if (pathMemberType == null)
            //            {
            //                var msg = $"Type specifier {by.resultTypeName} at {by.locator ?? "unknown"} could not be resolved.";
            //                ctx.LogError(msg);
            //                throw new InvalidOperationException(msg);
            //            }
            //            var pathExpression = PropertyHelper(sortMemberParameter, byColumn.path, pathMemberType!, ctx);
            //            var lambdaBody = Expression.Convert(pathExpression, typeof(object));
            //            var sortLambda = System.Linq.Expressions.Expression.Lambda(lambdaBody, sortMemberParameter);
            //            var sort = OperatorBinding.Bind(CqlOperator.SortBy, ctx.RuntimeContextParameter,
            //                @return, sortLambda, Expression.Constant(order, typeof(ListSortDirection)));
            //            @return = sort;
            //        }
            //        else
            //        {
            //            var sort = OperatorBinding.Bind(CqlOperator.Sort, ctx.RuntimeContextParameter,
            //                @return, Expression.Constant(order, typeof(ListSortDirection)));
            //            @return = sort;
            //        }
            //    }
            //}

            //if (isSingle)
            //{
            //    var callSingle = OperatorBinding.Bind(CqlOperator.Single, ctx.RuntimeContextParameter, @return);
            //    @return = callSingle;
            //}

            return sourceExpression;

            //return @return;
        }

        /// <summary>
        /// Given a SqlExpression, find the QuerySpecification inside
        /// Unwraps parenthesis.  Throws on error.
        /// </summary>
        /// <param name="sqlExpression"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private QuerySpecification FindQuerySpecification(SqlExpression sqlExpression)
        {
            var sqlFragment = sqlExpression.SqlFragment;
            if (sqlFragment is SelectStatement selectStatement)
            {
                return FindQuerySpecification(selectStatement);
            }
            else
                throw new InvalidOperationException();
        }

        private QuerySpecification FindQuerySpecification(SelectStatement selectStatement)
        {
            var queryExpression = selectStatement.QueryExpression;
            while (queryExpression is QueryParenthesisExpression queryParenthesisExpression)
                queryExpression = queryParenthesisExpression.QueryExpression;
            return queryExpression as QuerySpecification ?? throw new InvalidOperationException();
        }

        protected string TypeNameToIdentifier(Type type, SqlExpressionBuilderContext? ctx)
        {
            var typeName = type.Name.ToLowerInvariant();
            if (type.IsGenericType)
            {
                var genericTypeNames = string.Join("_", type.GetGenericArguments().Select(t => TypeNameToIdentifier(t, null)));
                var tick = typeName.IndexOf('`');
                if (tick > -1)
                    typeName = typeName[..tick];
                var fullName = $"{typeName}_{genericTypeNames}";
                typeName = fullName;
            }

            if (ctx != null)
            {
                int i = 1;
                var uniqueTypeName = typeName;
                while (ctx.HasScope(uniqueTypeName))
                {
                    uniqueTypeName = $"{typeName}{i}";
                    i++;
                }
                typeName = uniqueTypeName;
            }

            return ScopedSymbolsContext.NormalizeIdentifier(typeName!)!;
        }



        private SqlExpression? MultiSourceQuery(Query query, SqlExpressionBuilderContext ctx)
        {
            throw new NotImplementedException();
        }

        // returns a SelectStatement
        private SqlExpression? Retrieve(Retrieve retrieve, SqlExpressionBuilderContext ctx)
        {
            Type sourceElementType;
            string? cqlRetrieveResultType;

            // SingletonFrom does not have this specified; in this case use DataType instead
            if (retrieve.resultTypeSpecifier == null)
            {
                if (string.IsNullOrWhiteSpace(retrieve.dataType.Name))
                    throw new ArgumentException("If a Retrieve lacks a ResultTypeSpecifier it must have a DataType", nameof(retrieve));
                cqlRetrieveResultType = retrieve.dataType.Name;

                sourceElementType = TypeResolver.ResolveType(cqlRetrieveResultType) ?? throw new InvalidOperationException();
            }
            else
            {
                if (retrieve.resultTypeSpecifier is Elm.ListTypeSpecifier listTypeSpecifier)
                {
                    cqlRetrieveResultType = listTypeSpecifier.elementType is Elm.NamedTypeSpecifier nts ? nts.name.Name : null;
                    sourceElementType = TypeManager.TypeFor(listTypeSpecifier.elementType, ctx);
                }
                else throw new NotImplementedException($"Sources with type {retrieve.resultTypeSpecifier.GetType().Name} are not implemented.");
            }

            if (!FhirSqlTableMap.TryGetValue(cqlRetrieveResultType!, out var sqlTableEntry))
            {
                throw new InvalidOperationException($"Could not find SQL table mapping for type {cqlRetrieveResultType}");
            }

            List<SelectElement> selectElements = new List<SelectElement>();
            List<TableReference> tableReferences = new List<TableReference>();
            //WhereClause? whereClause = null;
            //ScalarExpression? codeColumnExpression = sqlTableEntry.DefaultCodingCodeExpression;

            // TODO: for now select * --- maybe will have to shape the result later
            selectElements.Add(new SelectStarExpression
            {
                Qualifier = new MultiPartIdentifier
                {
                    Identifiers =
                    {
                        new Identifier { Value = SourceTableAlias, QuoteType = QuoteType.SquareBracket }
                    }
                }
            });

            string? context = FindElmContext(ctx);
            if (context != null
                && sqlTableEntry.ContextIdentifierExpression != null
                && sqlTableEntry.ContextIdentifierExpression.TryGetValue(context.ToLowerInvariant(), out var contextIdentifierExpression))
            {
                // if there needs to be a context column, add it
                selectElements.Add(new SelectScalarExpression
                {
                    Expression = contextIdentifierExpression,
                    ColumnName = new IdentifierOrValueExpression
                    {
                        Identifier = new Identifier { Value = ContextColumnName, QuoteType = QuoteType.SquareBracket }
                    }
                });
            }

            TableReference sourceTableReference = new NamedTableReference
            {
                SchemaObject = new SchemaObjectName
                {
                    Identifiers =
                                {
                                    new Identifier { Value = sqlTableEntry.SqlTableName, QuoteType = QuoteType.SquareBracket }
                                }
                },
                Alias = new Identifier { Value = SourceTableAlias, QuoteType = QuoteType.SquareBracket }  // TODO: should this be a dynamic name?  see joins below
            };


            //Expression? codeProperty;

            var hasCodePropertySpecified = sourceElementType != null && retrieve.codeProperty != null;
            var isDefaultCodeProperty = retrieve.codeProperty is null ||
                (cqlRetrieveResultType is not null &&
                 modelMapping.TryGetValue(cqlRetrieveResultType, out ClassInfo? classInfo) &&
                 classInfo.primaryCodePath == retrieve.codeProperty);

            if (hasCodePropertySpecified && !isDefaultCodeProperty)
            {
                throw new NotImplementedException();

                //var codePropertyInfo = TypeResolver.GetProperty(sourceElementType!, retrieve.codeProperty!);
                //codeProperty = Expression.Constant(codePropertyInfo, typeof(PropertyInfo));
            }
            else
            {
                //codeProperty = Expression.Constant(null, typeof(PropertyInfo));
            }

            if (retrieve.codes != null)
            {
                if (retrieve.codes is ValueSetRef valueSetRef)
                {
                    //if (string.IsNullOrWhiteSpace(valueSetRef.name))
                    //    throw new ArgumentException($"The ValueSetRef at {valueSetRef.locator} is missing a name.", nameof(retrieve));
                    //var valueSet = InvokeDefinitionThroughRuntimeContext(valueSetRef.name!, valueSetRef!.libraryName, typeof(CqlValueSet), ctx);
                    //var call = OperatorBinding.Bind(CqlOperator.Retrieve, ctx.RuntimeContextParameter,
                    //    Expression.Constant(sourceElementType, typeof(Type)), valueSet, codeProperty!);
                    //return call;

                    throw new NotImplementedException();
                }
                else
                {
                    // In this construct, instead of querying a value set, we're testing resources
                    // against a list of codes, e.g., as defined by the code from or codesystem construct

                    // this should return a subselect that can be used in a where or join clause
                    SqlExpression codeExpression = TranslateExpression(retrieve.codes, ctx);
                    SelectStatement codeSelect =
                        codeExpression.IsSelectStatement
                        ? codeExpression.SqlFragment as SelectStatement ?? throw new InvalidOperationException()
                        : throw new InvalidOperationException();
                    QuerySpecification codeQuery = codeSelect.QueryExpression as QuerySpecification ?? throw new InvalidOperationException();

                    tableReferences.Add(new QualifiedJoin
                    {
                        FirstTableReference = sourceTableReference,
                        SecondTableReference = new QueryDerivedTable
                        {
                            QueryExpression = codeQuery,
                            Alias = new Identifier { Value = "codeTable" }
                        },
                        SearchCondition = new BooleanBinaryExpression
                        {
                            BinaryExpressionType = BooleanBinaryExpressionType.And,
                            FirstExpression = new BooleanComparisonExpression
                            {
                                ComparisonType = BooleanComparisonType.Equals,
                                FirstExpression = sqlTableEntry.DefaultCodingCodeExpression,
                                SecondExpression = BuildColumnReference("codeTable", "code") 
                            },
                            SecondExpression = new BooleanComparisonExpression
                            {
                                ComparisonType = BooleanComparisonType.Equals,
                                FirstExpression = sqlTableEntry.DefaultCodingCodeSystemExpression,
                                SecondExpression = BuildColumnReference("codeTable", "codesystem")
                            }
                        }
                    });


                    //var call = OperatorBinding.Bind(CqlOperator.Retrieve, ctx.RuntimeContextParameter,
                    //    Expression.Constant(sourceElementType, typeof(Type)), codes, codeProperty!);
                    //return call;
                }
            }
            else
            {
                // think this is the unfiltered case - everything from the table

                // create WHERE 1=1 clause so there is always a where clause
                //whereClause = new BooleanComparisonExpression
                //{
                //    ComparisonType = BooleanComparisonType.Equals,
                //    FirstExpression = new IntegerLiteral { Value = "1" },
                //    SecondExpression = new IntegerLiteral { Value = "1" }
                //};

                tableReferences.Add(sourceTableReference);
            }

            // map type to query clause and from clause
            SelectStatement select = new SelectStatement
            {
                QueryExpression = new QuerySpecification
                {
                    FromClause = new FromClause(),
                }
            };

            // gotta copy 'cause the SQL dom list properties are read-only
            IList<SelectElement> se = ((QuerySpecification)(select.QueryExpression)).SelectElements;
            selectElements.ForEach(e => se.Add(e));

            IList<TableReference> tr = ((QuerySpecification)(select.QueryExpression)).FromClause.TableReferences;
            tableReferences.ForEach(t => tr.Add(t));

            return new SqlExpression(
                select,
                SqlExpression.FragmentTypes.SelectStatement,
                sourceElementType!,
                ctx.CqlContext);

        }

        private string? FindElmContext(SqlExpressionBuilderContext ctx)
        {
            // TODO: this assumes that the interesting context is on the ExpressionDef predecessor.  Always true?
            for (int i = ctx.Predecessors.Count-1; i >=0; i--)
            {
                if (ctx.Predecessors[i] is Elm.ExpressionDef expressionDef)
                {
                    return expressionDef.context;
                }
            }

            return null;
        }

        public class FhirSqlTableMapEntry
        {
            public string SqlTableName { get; init; } = String.Empty;

            // how to find the default code property into sql (typically a column reference)
            public ScalarExpression? DefaultCodingCodeExpression { get; init; } = null;
            public ScalarExpression? DefaultCodingCodeSystemExpression { get; init; } = null;

            // dictionary that maps
            // the current elm context -> the sql expression (if applicable) that will produce an identity of that context
            // TODO: unclear what do do with the table qualification so far
            public Dictionary<string, ScalarExpression>? ContextIdentifierExpression { get; init; } = null;
        }

        /// <summary>
        /// for now, hard coded metadata about the FHIR tables. 
        /// this should be data-driven, and also may have some dynamic components 
        /// </summary>
        public Dictionary<string, FhirSqlTableMapEntry> FhirSqlTableMap { get; } = new Dictionary<string, FhirSqlTableMapEntry>
        {
            {
                "{http://hl7.org/fhir}Patient",
                new FhirSqlTableMapEntry
                {
                    SqlTableName = "patient",
                    ContextIdentifierExpression = new Dictionary<string, ScalarExpression>
                    {
                        // a Patient object in a Patient context (just the id column)
                        {
                            "patient",
                            BuildColumnReference(SourceTableAlias, "id")
                        },
                    }
                }
            },
            {
                "{http://hl7.org/fhir}Encounter",
                new FhirSqlTableMapEntry 
                { 
                    SqlTableName = "encounter" 
                }
            },            
            {
                "{http://hl7.org/fhir}Procedure",
                new FhirSqlTableMapEntry
                {
                    SqlTableName = "procedure",
                    // TODO: figure out what to do with table identifier; probably need to make this unique (ie dynamically generated)
                    DefaultCodingCodeExpression =                             
                        new FunctionCall
                            {
                                FunctionName = new Identifier { Value = "JSON_VALUE" },
                                Parameters =
                                {
                                    BuildColumnReference(SourceTableAlias, "code_string"),
                                    new StringLiteral { Value = "$.coding[0].code" }
                                }
                            }, //BuildColumnReference(SourceTableAlias, "code_coding_code"),
                    DefaultCodingCodeSystemExpression =
                        new FunctionCall
                            {
                                FunctionName = new Identifier { Value = "JSON_VALUE" },
                                Parameters =
                                {
                                    BuildColumnReference(SourceTableAlias, "code_string"),
                                    new StringLiteral { Value = "$.coding[0].system" }
                                }
                            }, //BuildColumnReference(SourceTableAlias, "code_coding_system"),
                    ContextIdentifierExpression = new Dictionary<string, ScalarExpression>
                    {
                        // a Condition object in a Patient context (extract the patient id)
                        {
                            "patient",
                            new FunctionCall
                            {
                                FunctionName = new Identifier { Value = "JSON_VALUE" },
                                Parameters = 
                                {
                                    BuildColumnReference(SourceTableAlias, "subject_string"),
                                    new StringLiteral { Value = "$.id" }
                                }
                            }
                        }
                    }
                }
            },
            {
                "{http://hl7.org/fhir}Condition",
                new FhirSqlTableMapEntry
                {
                    SqlTableName = "condition",
                    DefaultCodingCodeExpression = 
                        new FunctionCall
                            {
                                FunctionName = new Identifier { Value = "JSON_VALUE" },
                                Parameters =
                                {
                                    BuildColumnReference(SourceTableAlias, "code_string"),
                                    new StringLiteral { Value = "$.coding[0].code" }
                                }
                            },
                    DefaultCodingCodeSystemExpression = 
                        new FunctionCall
                            {
                                FunctionName = new Identifier { Value = "JSON_VALUE" },
                                Parameters =
                                {
                                    BuildColumnReference(SourceTableAlias, "code_string"),
                                    new StringLiteral { Value = "$.coding[0].system" }
                                }
                            },
                    ContextIdentifierExpression = new Dictionary<string, ScalarExpression>
                    {
                        // a Condition object in a Patient context (extract the patient id)
                        {
                            "patient",
                            new FunctionCall
                            {
                                FunctionName = new Identifier { Value = "JSON_VALUE" },
                                Parameters =
                                {
                                    BuildColumnReference(SourceTableAlias, "subject_string"),
                                    new StringLiteral { Value = "$.id" }
                                }
                            }
                        }
                    }
                }
            },
            {
                "{http://hl7.org/fhir}Observation",
                new FhirSqlTableMapEntry
                {
                    SqlTableName = "observation",
                    DefaultCodingCodeExpression = 
                        new FunctionCall
                            {
                                FunctionName = new Identifier { Value = "JSON_VALUE" },
                                Parameters =
                                {
                                    BuildColumnReference(SourceTableAlias, "code_string"),
                                    new StringLiteral { Value = "$.coding[0].code" }
                                }
                            },
                    DefaultCodingCodeSystemExpression = 
                        new FunctionCall
                            {
                                FunctionName = new Identifier { Value = "JSON_VALUE" },
                                Parameters =
                                {
                                    BuildColumnReference(SourceTableAlias, "code_string"),
                                    new StringLiteral { Value = "$.coding[0].system" }
                                }
                            },
                    ContextIdentifierExpression = new Dictionary<string, ScalarExpression>
                    {
                        // a Condition object in a Patient context (extract the patient id)
                        {
                            "patient",
                            new FunctionCall
                            {
                                FunctionName = new Identifier { Value = "JSON_VALUE" },
                                Parameters =
                                {
                                    BuildColumnReference(SourceTableAlias, "subject_string"),
                                    new StringLiteral { Value = "$.id" }
                                }
                            }
                        }
                    }
                }
            },
        };


        // returns a selectstatement, which could be a selectscalarexpression or selectstarexpression
        private SqlExpression? ExpressionRef(ExpressionRef ere, SqlExpressionBuilderContext ctx)
        {
            return BuildSqlFunctionReference(ere.name, ere, ctx);
        }

        private SqlExpression BuildSqlFunctionReference(string functionName, Elm.Expression ere, SqlExpressionBuilderContext ctx)
        {
            functionName = ScopedSymbolsContext.NormalizeIdentifier(functionName) ?? throw new InvalidOperationException();

            // is this a scalar or list function?
            // scalar hardcodes to a column called 'result'
            // a list (currently) does select *
            bool isScalar = (ere.resultTypeSpecifier == null || !(ere.resultTypeSpecifier is Elm.ListTypeSpecifier));

            // seems to be a massive HACK
            // if it otherwise looks like a scalar, it _might_ be Patient
            bool isHackyPatientQuery = false;
            if (isScalar 
                && ere.resultTypeSpecifier == null
                && ere.resultTypeName == null
                && String.Equals(functionName, "Patient", StringComparison.InvariantCultureIgnoreCase))
            {
                isHackyPatientQuery = true;
            }

            FromClause fromClause = new FromClause
            {
                TableReferences =
                {
                    new SchemaObjectFunctionTableReference
                    {
                        SchemaObject = new SchemaObjectName
                        {
                            Identifiers =
                            {
                                new Identifier { Value = functionName, QuoteType = QuoteType.SquareBracket }
                            }
                        },
                        Alias = new Identifier { Value = functionName, QuoteType = QuoteType.SquareBracket },
                    }
                }
            };

            if (isScalar && !isHackyPatientQuery)
            {
                // if it's a tuple type, then add all the columns; else just add Result
                // TODO: for now, hardcode Interval but eventually this has to be generic

                if (ere.resultTypeSpecifier is Elm.IntervalTypeSpecifier intervalTypeSpecifier)
                {
                    var intervalTuple = new ScalarTuple
                    {
                        DataType = typeof(CqlInterval<>) // TODO: extract base type
                    };

                    intervalTuple.Values.Add("low", BuildColumnReference(functionName, "low"));
                    intervalTuple.Values.Add("hi", BuildColumnReference(functionName, "hi"));
                    intervalTuple.Values.Add("lowClosed", BuildColumnReference(functionName, "lowClosed"));
                    intervalTuple.Values.Add("hiClosed", BuildColumnReference(functionName, "hiClosed"));

                    return WrapInSelectScalarExpression(intervalTuple, fromClause, ctx);
                }
                else if (ere.resultTypeSpecifier == null
                    && ere.resultTypeName != null
                    && ere.resultTypeName.Name.StartsWith("{http://hl7.org/fhir}"))
                {
                    // TODO:  hack --- if here, it means the result is a single value of a FHIR resource type
                    var fhirType = TypeResolver.ResolveType(ere.resultTypeName.Name) ?? throw new InvalidOperationException();

                    //
                    // create a SELECT TOP(1) * from function

                    QuerySpecification querySpecification = new QuerySpecification
                    {
                        FromClause = fromClause,
                        TopRowFilter = new TopRowFilter
                        {
                            Expression = new IntegerLiteral { Value = "1" }
                        }
                    };

                    querySpecification.SelectElements.Add(new SelectStarExpression
                    {
                        Qualifier = new MultiPartIdentifier
                        {
                            Identifiers =
                            {
                                new Identifier { Value = functionName, QuoteType = QuoteType.SquareBracket }
                            }
                        }
                    });

                    // wrap in queryexpression
                    var queryExpression = new QueryParenthesisExpression
                    {
                        QueryExpression = querySpecification
                    };

                    // wrap in select statement
                    return new SqlExpression(
                        new SelectStatement
                        {
                            QueryExpression = queryExpression
                        },
                        SqlExpression.FragmentTypes.SelectStatementScalar, fhirType, ctx.CqlContext); // TODO: type needs to be passed in
                }
                else
                {
                    // better be a primitive type (or it should have been handled above)
                    var primitiveType = ere.resultTypeName != null ? TypeResolver.ResolveType(ere.resultTypeName.Name) : null;

                    if (primitiveType == typeof(CqlQuantity))
                    {
                        var intervalTuple = new ScalarTuple
                        {
                            DataType = typeof(CqlQuantity) 
                        };

                        intervalTuple.Values.Add("value", BuildColumnReference(functionName, "value"));
                        intervalTuple.Values.Add("units", BuildColumnReference(functionName, "units"));

                        return WrapInSelectScalarExpression(intervalTuple, fromClause, ctx);
                    }
                    else
                    {
                        return WrapInSelectScalarExpression(
                            BuildColumnReference(functionName, ResultColumnName),
                            fromClause,
                            ctx);
                    }
                }
            }
            else
            {
                return new SqlExpression(
                    new SelectStatement
                    {
                        QueryExpression = new QueryParenthesisExpression
                        {
                            QueryExpression = new QuerySpecification
                            {
                                SelectElements =
                                {
                                    new SelectStarExpression
                                    {
                                        Qualifier = new MultiPartIdentifier
                                        {
                                            Identifiers =
                                            {
                                                new Identifier { Value = functionName, QuoteType = QuoteType.SquareBracket }
                                            }
                                        }
                                    }
                                },
                                FromClause = fromClause
                            },
                        },
                    },
                    SqlExpression.FragmentTypes.SelectStatement,
                    typeof(object),   // TODO: infer type
                    ctx.CqlContext);
            }
        }

        // returns SelectStatement
        private SqlExpression? Literal(Elm.Literal lit, SqlExpressionBuilderContext ctx)
        {
            Microsoft.SqlServer.TransactSql.ScriptDom.Literal? result = null;

            // TODO: would this need to force type in some cases?
            switch (lit.valueType.Name.ToLowerInvariant())
            {
                case "{urn:hl7-org:elm-types:r1}integer":
                    // TODO:  validate?
                    result = new IntegerLiteral { Value = lit.value };
                    break;
                case "{urn:hl7-org:elm-types:r1}decimal":
                    result = new NumericLiteral { Value = lit.value };
                    break;

                case "{urn:hl7-org:elm-types:r1}any":
                case "{urn:hl7-org:elm-types:r1}date":
                case "{urn:hl7-org:elm-types:r1}datetime":
                case "{urn:hl7-org:elm-types:r1}quantity":
                case "{urn:hl7-org:elm-types:r1}long":
                case "{urn:hl7-org:elm-types:r1}boolean":
                    result = new IntegerLiteral
                    {
                        Value =
                        lit.value.ToLowerInvariant() switch
                        {
                            "true" => "1",
                            "false" => "0",
                            _ => throw new InvalidOperationException()
                        }
                    };
                    break;

                case "{urn:hl7-org:elm-types:r1}string":
                    result = new StringLiteral { Value = lit.value };
                    break;
                case "{urn:hl7-org:elm-types:r1}ratio":
                case "{urn:hl7-org:elm-types:r1}code":
                case "{urn:hl7-org:elm-types:r1}codesystem":
                case "{urn:hl7-org:elm-types:r1}concept":
                case "{urn:hl7-org:elm-types:r1}time":
                case "{urn:hl7-org:elm-types:r1}valueset":
                case "{urn:hl7-org:elm-types:r1}vocabulary":

                default:
                    throw new NotImplementedException();
            }

            return WrapInSelectScalarExpression(result, null, ctx);
        }

        // TODO: bad things would happen if user tokens collided with these
        private const string UnusedTableName = "_UNUSED";
        private const string UnusedColumnName = "_unused_column";
        private const string ResultColumnName = "_Result";
        private const string SourceTableAlias = "_sourceTable"; // TODO: this is used everywhere but probably shouldn't be
        private const string ContextColumnName = "_Context";

        private FromClause NullFromClause()
        {
            return new FromClause
            {
                TableReferences =
                {
                    new QueryDerivedTable
                    {
                        QueryExpression = new QuerySpecification
                        {
                            SelectElements =
                                        {
                                            new SelectScalarExpression
                                            {
                                                Expression = new NullLiteral(),
                                                ColumnName = new IdentifierOrValueExpression
                                                {
                                                    Identifier = new Identifier { Value = UnusedColumnName, QuoteType = QuoteType.SquareBracket }
                                                }
                                            }
                                        }
                        },
                        Alias = new Identifier { Value = UnusedTableName, QuoteType = QuoteType.SquareBracket }
                    }
                }
            };
        }

        private SqlExpression? Add(Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx)
            => BinaryScalarExpression(BinaryExpressionType.Add, binaryExpression, ctx);
        private SqlExpression? Subtract(Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx)
            => BinaryScalarExpression(BinaryExpressionType.Subtract, binaryExpression, ctx);
        private SqlExpression? Multiply(Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx)
            => BinaryScalarExpression(BinaryExpressionType.Multiply, binaryExpression, ctx);
        private SqlExpression? Divide(Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx)
            => BinaryScalarExpression(BinaryExpressionType.Divide, binaryExpression, ctx);


        // returns a SelectStatement
        private SqlExpression BinaryScalarExpression(BinaryExpressionType binType, Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx)
        {
            if (binaryExpression.operand.Count() != 2)
                throw new InvalidOperationException();

            var op1 = TranslateExpression(binaryExpression.operand[0], ctx);
            var op2 = TranslateExpression(binaryExpression.operand[1], ctx);
            ScalarExpression fragment;

            // do special things if dealing with dates; for now just check the type of first arg
            var op1Type = binaryExpression.operand[0].resultTypeName != null 
                ? TypeResolver.ResolveType(binaryExpression.operand[0].resultTypeName.Name) 
                : null;

            if (op1Type != null
                && (op1Type == typeof(CqlDate) || op1Type == typeof(CqlDateTime)))
            {
                // this just became date math --- second param better be Quantity and this better be + or -
                // (cql->elm might enforce this, not sure)

                var op2Type = TypeResolver.ResolveType(binaryExpression.operand[1].resultTypeName.Name) ?? throw new InvalidOperationException();
                if (op2Type != typeof(CqlQuantity) || (binType != BinaryExpressionType.Add && binType != BinaryExpressionType.Subtract))
                    throw new InvalidOperationException();

                // expression becomes
                // case (select top(1) units from op2) 
                // when 'years' THEN DATEADD(year, (select top(1) value from op2), op1) end
                // when 'months' THEN DATEADD(month, (select top(1) value from op2), op1) end
                // etc

                fragment = new SimpleCaseExpression
                {
                    InputExpression = new ScalarSubquery
                    {
                        QueryExpression = new QuerySpecification
                        {
                            SelectElements =
                            {
                                new SelectScalarExpression
                                {
                                    Expression = BuildColumnReference("op2", "units")
                                }
                            },
                            FromClause = new FromClause
                            {
                                TableReferences =
                            {
                                new QueryDerivedTable
                                {
                                    QueryExpression = (op2.SqlFragment as SelectStatement ?? throw new InvalidOperationException())?.QueryExpression,
                                    Alias = new Identifier { Value = "op2", QuoteType = QuoteType.SquareBracket }
                                }
                            }
                            }
                        }
                    },
                    WhenClauses =
                    {
                        BuildWhenClause(binType, op1, op2, "years", "year"),
                        BuildWhenClause(binType, op1, op2, "months", "month"),
                        BuildWhenClause(binType, op1, op2, "weeks", "week"),
                        BuildWhenClause(binType, op1, op2, "days", "day"),
                    }
                };
            }
            else
            {
                fragment = new BinaryExpression
                {
                    BinaryExpressionType = binType,
                    FirstExpression = new ScalarSubquery
                    {
                        QueryExpression = new QueryParenthesisExpression
                        {
                            QueryExpression = (op1.SqlFragment as SelectStatement ?? throw new InvalidOperationException())?.QueryExpression
                        }
                    },
                    SecondExpression = new ScalarSubquery
                    {
                        QueryExpression = new QueryParenthesisExpression
                        {
                            QueryExpression = (op2.SqlFragment as SelectStatement ?? throw new InvalidOperationException())?.QueryExpression
                        }
                    }
                };
            }
            return WrapInSelectScalarExpression(fragment, null, ctx);


            //var (lhs, lhsFrom) = UnwrapScalarSelectElement(TranslateExpression(binaryExpression.operand[0], ctx));
            //var (rhs, rhsFrom) = UnwrapScalarSelectElement(TranslateExpression(binaryExpression.operand[1], ctx));

            //ScalarExpression fragment = new BinaryExpression
            //{
            //    BinaryExpressionType = binType,
            //    FirstExpression = lhs,
            //    SecondExpression = rhs
            //};

            //if (NeedsParenthesis(binaryExpression, ctx.Parent))
            //{
            //    var parenthesis = new ParenthesisExpression
            //    {
            //        Expression = fragment
            //    };

            //    fragment = parenthesis;
            //}
            //return WrapInSelectScalarExpression(fragment, ReconcileScalarFromClauses(lhsFrom, rhsFrom), ctx);

            static SimpleWhenClause BuildWhenClause(
                BinaryExpressionType binType,
                SqlExpression op1,
                SqlExpression op2,
                string cqlUnit,
                string sqlDatePart)
            {
                return new SimpleWhenClause
                {
                    WhenExpression = new StringLiteral { Value = cqlUnit },
                    ThenExpression = new FunctionCall
                    {
                        FunctionName = new Identifier { Value = "DATEADD" },
                        Parameters =
                        {
                            // datepart
                            new ColumnReferenceExpression
                            {
                                MultiPartIdentifier = new MultiPartIdentifier
                                {
                                    Identifiers =
                                    {
                                        new Identifier { Value = sqlDatePart }
                                    }
                                }
                            },
                            // number
                            new ScalarSubquery
                            {
                                QueryExpression = new QuerySpecification
                                {
                                    SelectElements =
                                    {
                                        new SelectScalarExpression
                                        {
                                            Expression = binType == BinaryExpressionType.Add
                                                        ? BuildColumnReference("op2", "value")
                                                        : new Microsoft.SqlServer.TransactSql.ScriptDom.UnaryExpression
                                                        {
                                                            UnaryExpressionType = UnaryExpressionType.Negative,
                                                            Expression = BuildColumnReference("op2", "value")
                                                        }
                                        }
                                    },
                                    FromClause = new FromClause
                                    {
                                        TableReferences =
                                        {
                                            new QueryDerivedTable
                                            {
                                                QueryExpression = (op2.SqlFragment as SelectStatement ?? throw new InvalidOperationException())?.QueryExpression,
                                                Alias = new Identifier { Value = "op2", QuoteType = QuoteType.SquareBracket }
                                            }
                                        }
                                    }
                                }
                            },
                            // date
                            new ScalarSubquery
                            {
                                QueryExpression = new QuerySpecification
                                {
                                    SelectElements =
                                    {
                                        new SelectScalarExpression
                                        {
                                            Expression = BuildColumnReference("op1", ResultColumnName)
                                        }
                                    },
                                    FromClause = new FromClause
                                    {
                                        TableReferences =
                                        {
                                            new QueryDerivedTable
                                            {
                                                QueryExpression = (op1.SqlFragment as SelectStatement ?? throw new InvalidOperationException())?.QueryExpression,
                                                Alias = new Identifier { Value = "op1", QuoteType = QuoteType.SquareBracket }
                                            }
                                        }
                                    }
                                }
                            },
                        }
                    }
                };
            }
        }

        private static ColumnReferenceExpression BuildColumnReference(string tableName, string columnName)
        {
            return new ColumnReferenceExpression
            {
                MultiPartIdentifier = new MultiPartIdentifier
                {
                    Identifiers =
                    {
                        new Identifier { Value = tableName, QuoteType = QuoteType.SquareBracket },
                        new Identifier { Value = columnName, QuoteType = QuoteType.SquareBracket }
                    }
                }
            };
        }

        // HACK - overload which does not require table name -- really shouldn't use this
        private static ColumnReferenceExpression BuildColumnReference(string columnName)
        {
            return new ColumnReferenceExpression
            {
                MultiPartIdentifier = new MultiPartIdentifier
                {
                    Identifiers =
                    {
                        new Identifier { Value = columnName, QuoteType = QuoteType.SquareBracket }
                    }
                }
            };
        }


        private FromClause ReconcileScalarFromClauses(params FromClause[] froms)
        {
            if (froms.Length == 0)
                throw new InvalidOperationException();

            // loop through the given froms and build a join expression
            // TODO: only handles scalar (literal or TV function) case.  
            // Will have to handle lists eventually

            // build a tree out of the froms; suppress duplicate nulls (used in literals)

            bool hasLiteralScalar = false;

            List<FromClause> fromList = froms.ToList();

            // remove the empties first
            for (int i = fromList.Count() - 1; i >= 0; i--)
            {
                FromClause from = fromList[i];
                if (from == null || from.TableReferences.Count == 0)
                {
                    fromList.RemoveAt(i);
                    continue;
                }
            }

            // search through the from clauses (backwards); remember if any are literal scalar, and remove them
            for (int i = fromList.Count() - 1; i >= 0; i--)
            {
                FromClause from = fromList[i];

                if (from.TableReferences.Count == 1)
                {
                    var tableReference = from.TableReferences[0];
                    if (tableReference is QueryDerivedTable queryDerivedTable)
                    {
                        if (queryDerivedTable.QueryExpression is QuerySpecification querySpecification)
                        {
                            if (querySpecification.SelectElements.Count == 1)
                            {
                                var selectElement = querySpecification.SelectElements[0];
                                if (selectElement is SelectScalarExpression selectScalarExpression)
                                {
                                    if (selectScalarExpression.Expression is NullLiteral)
                                    {
                                        hasLiteralScalar = true;
                                        fromList.RemoveAt(i);

                                        continue;
                                    }
                                }
                            }
                        }
                    }

                    // also attempt to diagnose duplicates and remove this one if a dupe is found (yay n^2)
                    for (int j = i - 1; j >= 0; j--)
                    {
                        FromClause fromClauseOther = froms[j];
                        if (fromClauseOther.TableReferences.Count > 1)
                            throw new InvalidOperationException();   // TODO: what does this mean?

                        TableReference tableReferenceOther = fromClauseOther.TableReferences[0];
                        if (AreEqualTableReferences(tableReference, tableReferenceOther))
                        {
                            fromList.RemoveAt(i);
                            break;
                        }  
                    }
                }
                else
                {
                    // TODO: there shouldn't be any cases of multiple TableReferences, right?
                    throw new InvalidOperationException();
                }
            }

            FromClause result;
            bool haveAddedFirst = false;

            if (hasLiteralScalar)
            {
                result = NullFromClause();
                haveAddedFirst = true;
            }
            else
            {
                result = new FromClause();
            }

            // now loop through any remaining TV function references and build joins
            foreach (var from in fromList)
            {
                if (from.TableReferences.Count == 1)
                {
                    var tableReference = from.TableReferences[0];
                    if (haveAddedFirst)
                    {
                        // TODO: this needs to get a lot more complicated with other table joins
                        // TODO: do we need to allow for cases of both scalar and set joins?  need an example

                        var existingTable = result.TableReferences[0];
                        result.TableReferences.RemoveAt(0);

                        result.TableReferences.Add(new UnqualifiedJoin
                        {
                            FirstTableReference = existingTable,
                            SecondTableReference = tableReference,
                            UnqualifiedJoinType = UnqualifiedJoinType.CrossApply
                        });
                    }
                    else
                    {
                        result.TableReferences.Add(tableReference);
                    }
                    haveAddedFirst = true;
                }
                else
                {
                    // duplicate bulletproofing to above
                    throw new InvalidOperationException();
                }
            }
            return result;
        }

        private bool AreEqualTableReferences(TableReference tableReference, TableReference tableReferenceOther)
        {
            // do some basic checks to see if the table references are the same
            // handles tables, functions and simple subselects
            if (tableReference is NamedTableReference namedTableReference
                && tableReferenceOther is NamedTableReference namedTableReferenceOther)
            {
                return namedTableReference.SchemaObject.BaseIdentifier.Value
                    == namedTableReferenceOther.SchemaObject.BaseIdentifier.Value;
            }
            else if (tableReference is SchemaObjectFunctionTableReference schemaObjectFunctionTableReference
                && tableReferenceOther is SchemaObjectFunctionTableReference schemaObjectFunctionTableReferenceOther)
            {
                return schemaObjectFunctionTableReference.SchemaObject.BaseIdentifier.Value
                    == schemaObjectFunctionTableReferenceOther.SchemaObject.BaseIdentifier.Value;
            }
            else if (tableReference is QueryDerivedTable queryDerivedTable
                && tableReferenceOther is QueryDerivedTable queryDerivedTableOther)
            {
                var querySpecification = queryDerivedTable.QueryExpression as QuerySpecification ?? throw new InvalidOperationException();
                var querySpecificationOther = queryDerivedTableOther.QueryExpression as QuerySpecification ?? throw new InvalidOperationException();
           
                // TODO: could do some more comparisons on the select elements, but in practice this should be good enough for now
                if (querySpecification.SelectElements.Count != querySpecificationOther.SelectElements.Count)
                    return false;

                if (querySpecification.FromClause.TableReferences.Count != 1
                    || querySpecificationOther.FromClause.TableReferences.Count != 1)
                    return false;

                return AreEqualTableReferences(
                    querySpecification.FromClause.TableReferences[0], 
                    querySpecificationOther.FromClause.TableReferences[0]);
            }

            return false;
        }

        private FromClause FindTableReference(TSqlFragment sqlFragment)
        {
            if (sqlFragment is SelectStatement selectStatment)
            {
                var querySpecification = FindQuerySpecification(selectStatment);

                return querySpecification.FromClause;
            }
            else
                throw new InvalidOperationException();
        }

        internal class ScalarTuple
        {
            public Dictionary<string, ScalarExpression> Values { get; init; } = new Dictionary<string, ScalarExpression>();
            public Type DataType { get; init; } = typeof(object);

            public int Count => Values.Count;

            /// <summary>
            /// A handy shortcut for returning a single value if the tuple only has one
            /// (or at least the 'Result' column --- a filtered context will create a second column)
            /// </summary>
            public ScalarExpression SingleValue
            {
                get
                {
                    if (!Values.TryGetValue(ResultColumnName, out var value))
                        throw new InvalidOperationException();
                    return value;
                }
            }
        }

        // scalars need to be wrapped in full select statements (and correspondingly unwrapped for complex expressions)
        // when unwrapping, the return would be the inner expression(s) as well as the tables
        // and then when wrapping, tables are added
        //
        //
        // NOTE:  an explicit TOP 1 is added to indicate a scalar.   (unless in filtered context!)
        private SqlExpression WrapInSelectScalarExpression(
            ScalarTuple scalarTuple, 
            FromClause? fromClause,
            SqlExpressionBuilderContext ctx,
            bool createScalarSubquery = false)
        {
            // build the basic query specification
            QuerySpecification querySpecification = new QuerySpecification
            {
                FromClause = fromClause,
                TopRowFilter = InFilteredContext(ctx) 
                    ? null 
                    : new TopRowFilter
                {
                    Expression = new IntegerLiteral { Value = "1" }
                }
            };

            // add the select elements
            foreach (var field in scalarTuple.Values)
            {
                querySpecification.SelectElements.Add(new SelectScalarExpression
                {
                    Expression = field.Value,
                    ColumnName = new IdentifierOrValueExpression
                    {
                        Identifier = new Identifier { Value = field.Key, QuoteType = QuoteType.SquareBracket }
                    }
                });
            }

            // wrap in queryexpression
            var queryExpression = new QueryParenthesisExpression
            {
                QueryExpression = querySpecification
            };

            // wrap in select statement
            if (createScalarSubquery)
            {
                return new SqlExpression(
                    new ScalarSubquery
                    {
                        QueryExpression = queryExpression
                    },
                    SqlExpression.FragmentTypes.SelectStatementScalar, scalarTuple.DataType,
                    ctx.CqlContext);
            }
            else
            {
                return new SqlExpression(
                    new SelectStatement
                    {
                        QueryExpression = queryExpression
                    },
                    SqlExpression.FragmentTypes.SelectStatementScalar, scalarTuple.DataType,  // TODO: type needs to be passed in
                    ctx.CqlContext); 
            }
        }

        // helper method when the scalar is just one field (TODO: is this still useful?)
        private SqlExpression WrapInSelectScalarExpression(
            ScalarExpression scalar, 
            FromClause? fromClause, 
            SqlExpressionBuilderContext ctx, 
            bool createScalarSubquery = false)
        {
            return WrapInSelectScalarExpression(
                new ScalarTuple
                {
                    Values = { { ResultColumnName, scalar } },
                    DataType = typeof(object) // TODO: type needs to be inferred?
                },
                fromClause,
                ctx,
                createScalarSubquery);
        }

        // expressions are (always?) wrapped as a SelectElement (a SelectScalarExpression)
        // but if used in a larger expression, need to be unwrapped
        private (ScalarExpression, FromClause) UnwrapScalarSelectElement(SqlExpression sqlFragment)
        {
            if (sqlFragment.IsSelectStatement
                && sqlFragment.SqlFragment is SelectStatement selectStatment)
            {
                  var querySpecification = FindQuerySpecification(selectStatment);

                // TODO: strong assumption here is that the 0th element is the one we want
                // to be complete, it is the one marked "_Result" or a tuple if we get that fancy
                // there also could be a Context column, but that also should be last
                SelectScalarExpression selectScalarExpression = querySpecification.SelectElements[0] as SelectScalarExpression ?? throw new InvalidOperationException();

                return (selectScalarExpression.Expression, querySpecification.FromClause);
            }
            else
                throw new InvalidOperationException();
        }

        private (ScalarTuple, FromClause) UnwrapScalarSelectElements(SqlExpression sqlFragment)
        {
            if (sqlFragment.IsSelectStatement
                && sqlFragment.SqlFragment is SelectStatement selectStatment)
            {
                var querySpecification = FindQuerySpecification(selectStatment);

                ScalarTuple result = new()
                {
                    DataType = sqlFragment.DataType
                };
                foreach (var selectElement in querySpecification.SelectElements)
                {
                    SelectScalarExpression selectScalarExpression = selectElement as SelectScalarExpression ?? throw new InvalidOperationException();

                    result.Values.Add(
                        selectScalarExpression.ColumnName?.Value ?? throw new InvalidOperationException(),
                        selectScalarExpression.Expression);
                }

                return (result, querySpecification.FromClause);
            }
            else
                throw new InvalidOperationException();
        }

        private (BooleanExpression, FromClause) UnwrapBooleanExpression(SqlExpression sqlFragment)
        {
            var (scalarExpression, fromClause) = UnwrapScalarSelectElement(sqlFragment);

            // the scalarExpression should be an IIF call.  Extract the Predicate

            if (scalarExpression is IIfCall iifCall)
            {
                return (iifCall.Predicate, fromClause);
            }
            else 
            {
                // we assume that the scalar is a boolean (could be literal, could be columnref, etc), and that the value is 1 for true and 0 for false
                return (new BooleanComparisonExpression
                {
                    ComparisonType = BooleanComparisonType.Equals,
                    FirstExpression = scalarExpression,
                    SecondExpression = new IntegerLiteral { Value = "1" }
                },
                fromClause);
            }
        }

        // returns a SelectStatement
        private SqlExpression? ToDecimal(ToDecimal tde, SqlExpressionBuilderContext ctx)
        {
            var (expression, fromClause) = UnwrapScalarSelectElement(TranslateExpression(tde.operand, ctx));

            var fragment = new CastCall
            {
                DataType = new SqlDataTypeReference
                {
                    SqlDataTypeOption = SqlDataTypeOption.Decimal
                },
                Parameter = new ParenthesisExpression
                {
                    Expression = expression
                }
            };

            return WrapInSelectScalarExpression(fragment, fromClause, ctx);
        }

        /// <summary>
        /// Tries to determine if the current expression needs parenthesis.
        /// TODO:  this could be not worth it; maybe sub expressions always get parenthesis --- but was unsure if SQL has some maximum nesting depth 
        /// and it is necessary to minimize
        /// </summary>
        /// <param name="current"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool NeedsParenthesis(Element current, Element? parent)
        {
            bool result = false;

            switch (current)
            {
                case Elm.Add:
                case Elm.Subtract:
                    switch (parent)
                    {
                        case Elm.Multiply:
                        case Elm.Divide:
                            result = true;
                            break;
                    }
                    break;

                case Elm.Multiply:
                case Elm.Divide:
                    break;
                default:
                    throw new NotImplementedException();

            }
            return result;
        }

        private SqlExpression WrapBooleanExpressionInSelectStatement(ScalarExpression scalarExpression, FromClause fromClause, SqlExpressionBuilderContext ctx)
        {
            return WrapBooleanExpressionInSelectStatement(
                new BooleanComparisonExpression
                {
                    ComparisonType = BooleanComparisonType.Equals,
                    FirstExpression = scalarExpression,
                    SecondExpression = new IntegerLiteral { Value = "1" }
                },
                fromClause,
                ctx);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="booleanExpression"></param>
        /// <param name="fromClause"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private SqlExpression WrapBooleanExpressionInSelectStatement(BooleanExpression booleanExpression, FromClause fromClause, SqlExpressionBuilderContext ctx)
        {
            // TODO:  add top 1?

            var booleanSelectStatement = new SelectStatement
            {
                QueryExpression = new QuerySpecification
                {
                    SelectElements =
                    {
                        new SelectScalarExpression
                        {
                            Expression = new IIfCall
                            {
                                Predicate = booleanExpression,
                                ThenExpression = new IntegerLiteral { Value = "1" },
                                ElseExpression = new IntegerLiteral { Value = "0" }
                            },
                            ColumnName = new IdentifierOrValueExpression
                            {
                                Identifier = new Identifier { Value  = ResultColumnName, QuoteType = QuoteType.SquareBracket }
                            }
                        }
                    },
                    FromClause = fromClause
                }
            };

            // HACK -- if in filtered context, add the context column
            if (InFilteredContext(ctx))
            {
                var querySpecification = FindQuerySpecification(booleanSelectStatement);
                querySpecification.SelectElements.Add(new SelectScalarExpression
                {
                    // TODO: table name here
                    Expression = BuildColumnReference(ContextColumnName),
                });
            }

            return new SqlExpression(
                booleanSelectStatement,
                SqlExpression.FragmentTypes.SelectBooleanExpression,
                typeof(bool),
                ctx.CqlContext);
        }

    }

}