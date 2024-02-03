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

        // TODO: hijacking .net type system to describe the types here, in a limited way.  Only use the ones that there is a clear 1:1 correspondence
        // bool, int, DateTime --- primitives
        // Patient, Condition, etc. --- FHIR types
        // CqlCode
        // TODO:  should IEnumerable be used here or is it implicit with SelectStatement vs SelectStatementScalar
        public Type DataType { get; init; }  

        public SqlExpression(TSqlFragment sqlFragment, FragmentTypes fragmentType, Type dataType)
        {
            SqlFragment = sqlFragment;
            FragmentType = fragmentType;
            DataType = dataType;
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
                    //foreach (var parameter in Library.parameters ?? Enumerable.Empty<elm.ParameterDef>())
                    //{
                    //    if (definitions.ContainsKey(null, parameter.name!))
                    //        throw new InvalidOperationException($"There is already a definition named {parameter.name}");

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
                    //}
                }

                foreach (var def in this.Library.statements ?? Enumerable.Empty<Hl7.Cql.Elm.ExpressionDef>())
                {
                    if (def.expression != null)
                    {
                        //var contextParameter = Expression.Parameter(typeof(CqlContext), "context");
                        var buildContext = new SqlExpressionBuilderContext(this,
                            localLibraryIdentifiers,
                            definitions);

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

            return new SqlExpression(select, SqlExpression.FragmentTypes.SelectStatement, typeof(IEnumerable<CqlCode>));
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
                    // result = AliasRef(ar, ctx);
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
                    // result = CalculateAgeAt(caa, ctx);
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
                    // result = End(e, ctx);
                    break;
                case Ends e:
                    // result = Ends(e, ctx);
                    break;
                case EndsWith e:
                    // result = EndsWith(e, ctx);
                    break;
                case Equal eq:
                    // result = Equal(eq, ctx);
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
                    // result = NotEqual(ne, ctx);
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
                    // result = ParameterRef(pre, ctx);
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
                    // result = Quantity(qua, ctx);
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
                    // result = Start(start, ctx);
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
                    // result = ToDateTime(tdte, ctx);
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

        private SqlExpression? Count(Count ce, SqlExpressionBuilderContext ctx)
        {
            // TODO YOU ARE HERE --- currently this works when in an unfiltered context, and counting something in an unfiltered context
            // the real trick is what happens when the thing being counted is, for example, Patient context
            // then, we have to make sure that a) the subquery has a property which allows it to be grouped, and b) this aggregate query does grouping
            //

            var sourceExpression = TranslateExpression(ce.source, ctx);
            var sourceSelect = sourceExpression.SqlFragment as SelectStatement ?? throw new InvalidOperationException();

            // wrap in SELECT COUNT(1) AS Result FROM <sourceSelect>
            var select = new SelectStatement
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
                                Parameters = {new IntegerLiteral { Value = "1" } },
                            },
                            ColumnName = new IdentifierOrValueExpression { Identifier = new Identifier { Value = ResultColumnName } }
                        }
                    },
                    FromClause = new FromClause
                    {
                        TableReferences =
                        {
                            new QueryDerivedTable
                            {
                                QueryExpression = sourceSelect.QueryExpression,
                                Alias = new Identifier { Value = UnusedTableName }
                            }
                        }
                    }
                }
            };

            return new SqlExpression(
                select, 
                SqlExpression.FragmentTypes.SelectStatementScalar, 
                typeof(long));
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
                                Alias = new Identifier { Value = UnusedTableName }
                            }
                        }
                    },
                    TopRowFilter = new TopRowFilter
                    {
                        Expression = new IntegerLiteral { Value = "1" }
                    }
                }
            };

            return new SqlExpression(select, SqlExpression.FragmentTypes.SelectStatement, listExpression.DataType);
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
                                                        Alias = new Identifier { Value = UnusedTableName }
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
                            ColumnName = new IdentifierOrValueExpression { Identifier = new Identifier { Value = ResultColumnName } }
                        }
                    },
                    FromClause = NullFromClause()
                }
            };

            return new SqlExpression(newSelect, SqlExpression.FragmentTypes.SelectStatementScalar, typeof(bool));
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

            if (lowExpression.DataType != hiExpression.DataType)
                throw new InvalidOperationException("low and hi of Interval must be same type");

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

            return WrapInSelectScalarExpression(intervalTuple, fromClause);
        }

        // returns a select statement wrapping BooleanExpression
        private SqlExpression? BooleanComparisonOperator(Elm.BinaryExpression booleanExpression, BooleanComparisonType bct, SqlExpressionBuilderContext ctx)
        {
            // unwrap both sides from their Selects, then re-wrap any scalar expression on either side

            var originalLhs = TranslateExpression(booleanExpression.operand[0], ctx);
            var (unwrappedLhs, lhsFrom) = UnwrapScalarSelectElement(originalLhs);

            var originalRhs = TranslateExpression(booleanExpression.operand[1], ctx);
            var (unwrappedRhs, rhsFrom) = UnwrapScalarSelectElement(originalRhs);

            ScalarExpression newLhs = originalLhs.FragmentType == SqlExpression.FragmentTypes.SelectStatementScalar
                    ? WrapInSelectScalarExpression(unwrappedLhs, lhsFrom, true).SqlFragment as ScalarSubquery ?? throw new InvalidOperationException()
                    : unwrappedLhs;
            ScalarExpression newRhs = originalRhs.FragmentType == SqlExpression.FragmentTypes.SelectStatementScalar
                    ? WrapInSelectScalarExpression(unwrappedRhs, rhsFrom, true).SqlFragment as ScalarSubquery ?? throw new InvalidOperationException()
                    : unwrappedRhs;

            var binaryExpression = new BooleanComparisonExpression
            {
                FirstExpression = newLhs,
                ComparisonType = bct,
                SecondExpression = newRhs,
            };

            return WrapBooleanExpressionInSelectStatement(
                binaryExpression, 
                ReconcileScalarFromClauses(lhsFrom, rhsFrom));
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

            // assuming rhs is an interval --- what if it is a columnref that is an interval?
            if (unwrappedRhs.DataType.GetGenericTypeDefinition() != typeof(CqlInterval<>))
                throw new InvalidOperationException();

            // assuming lhs is a single value
            var lhsSingleValue = unwrappedLhs.SingleValue;

            // rewrap rhs parts as a scalarsubquery
            var rhsLow = WrapInSelectScalarExpression(unwrappedRhs.Values["low"], rhsFrom, true);
            var rhsHi = WrapInSelectScalarExpression(unwrappedRhs.Values["hi"], rhsFrom, true);

            // TODO:  this works if the interval is a literal
            // if it's a columnref, we'll just hardcode to true for now.   But to fix it right, it needs to generate conditional
            // expression to do < or <= depending on whether the interval is open or closed
            bool lowClosed = true;
            bool hiClosed = true;
            if (unwrappedRhs.Values["lowClosed"] is Microsoft.SqlServer.TransactSql.ScriptDom.Literal)
            {
                lowClosed = SqlIntegerScalarExpressionToBoolean(unwrappedRhs.Values["lowClosed"]);
                hiClosed = SqlIntegerScalarExpressionToBoolean(unwrappedRhs.Values["hiClosed"]);
            }

            var andExpression = new BooleanParenthesisExpression
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

            return WrapBooleanExpressionInSelectStatement(
                andExpression,
                ReconcileScalarFromClauses(lhsFrom, rhsFrom));
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
                ReconcileScalarFromClauses(lhsFrom, rhsFrom));
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

            return WrapInSelectScalarExpression(functionExpression, combinedFrom);
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

            return WrapInSelectScalarExpression(functionExpression, combinedFrom);
        }


        // returns SelectStatement
        // TODO: returns a fully formed select with table name, which might not be relevant to all contexts?
        private SqlExpression? Property(Property pe, SqlExpressionBuilderContext ctx)
        {
            SelectScalarExpression? selectScalarExpression = null;

            Type sourceTableType;
            Identifier sourceTableIdentifier;
            FromClause fromClause;
            string? elmElementType = null; // TODO not sure we can always figure this out?  but works in some cases

            if (!String.IsNullOrEmpty(pe.scope))
            {
                // I think? this is the case where the property source is the result of a Retrieve.   Don't know if this is always true
                var sourceTable = ctx.GetScope(pe.scope) ?? throw new InvalidOperationException();
                sourceTableType = sourceTable.Type;
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
                sourceTableType = sourceExpression.DataType;
                sourceTableIdentifier = new Identifier { Value = sourceTableType.Name };

                // the from clause is the same as the one from the source expression
                fromClause = FindTableReference(sourceExpression.SqlFragment);
            }

            // map the property name to a column name -- TODO: should be a fancier lookup
            switch (sourceTableType.Name.ToLowerInvariant())
            {
                case "condition":
                    {
                        switch (pe.path)
                        {
                            case "onset":
                                string destinationcolumnName = "onsetDateTime";
                                selectScalarExpression = new SelectScalarExpression
                                {
                                    Expression = new ColumnReferenceExpression
                                    {
                                        MultiPartIdentifier = new MultiPartIdentifier
                                        {
                                            Identifiers =
                                            {
                                                sourceTableIdentifier,
                                                new Identifier { Value = destinationcolumnName }
                                            }
                                        }
                                    },
                                    ColumnName = new IdentifierOrValueExpression
                                    {
                                        Identifier = new Identifier { Value = ResultColumnName }
                                    }
                                };
                                break;
                        }
                        break;
                    }
                case "patient":
                    {
                        switch (pe.path)
                        {
                            case "birthDate":
                                string destinationcolumnName = "birthDate";
                                selectScalarExpression = new SelectScalarExpression
                                {
                                    Expression = new ColumnReferenceExpression
                                    {
                                        MultiPartIdentifier = new MultiPartIdentifier
                                        {
                                            Identifiers =
                                            {
                                                sourceTableIdentifier,
                                                new Identifier { Value = destinationcolumnName }
                                            }
                                        }
                                    },
                                    ColumnName = new IdentifierOrValueExpression
                                    {
                                        Identifier = new Identifier { Value = ResultColumnName }
                                    }
                                };
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

            // TODO YOU ARE HERE
            // somethis is still broken about this logic

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
                        Identifier = new Identifier { Value = ContextColumnName }
                    }
                });
            }

            return new SqlExpression(
                sqlSelect,
                SqlExpression.FragmentTypes.SelectStatement,
                typeof(object));  // TODO: infer type?;
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
            }
            throw new NotImplementedException();
        }

        private SqlExpression? ToList(ToList tle, SqlExpressionBuilderContext ctx)
        {
            // big hack -- everything is a list already?
            return TranslateExpression(tle.operand, ctx);
        }

        /// <summary>
        /// Return a QueryExpression (expected to be integrated into another statement)
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
                                        new Identifier { Value = ScopedSymbolsContext.NormalizeIdentifier(cre.name) }
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
                typeof(CqlCode));
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

                var queryExpression = sourceSelect.QueryExpression;

                while (queryExpression is QueryParenthesisExpression queryParenthesisExpression)
                    queryExpression = queryParenthesisExpression.QueryExpression;
                var querySpecification = queryExpression as QuerySpecification ?? throw new InvalidOperationException();

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
                        new Identifier { Value = SourceTableAlias }
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
                        Identifier = new Identifier { Value = ContextColumnName }
                    }
                });
            }

            TableReference sourceTableReference = new NamedTableReference
            {
                SchemaObject = new SchemaObjectName
                {
                    Identifiers =
                                {
                                    new Identifier { Value = sqlTableEntry.SqlTableName }
                                }
                },
                Alias = new Identifier { Value = SourceTableAlias }  // TODO: should this be a dynamic name?  see joins below
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
                                SecondExpression = new ColumnReferenceExpression
                                {
                                    MultiPartIdentifier = new MultiPartIdentifier
                                    {
                                        Identifiers =
                                        {
                                            new Identifier { Value = "codeTable" },
                                            new Identifier { Value = "code" }
                                        }
                                    }
                                }
                            },
                            SecondExpression = new BooleanComparisonExpression
                            {
                                ComparisonType = BooleanComparisonType.Equals,
                                FirstExpression = sqlTableEntry.DefaultCodingCodeSystemExpression,
                                SecondExpression = new ColumnReferenceExpression
                                {
                                    MultiPartIdentifier = new MultiPartIdentifier
                                    {
                                        Identifiers =
                                        {
                                            new Identifier { Value = "codeTable" },
                                            new Identifier { Value = "codesystem" }
                                        }
                                    }
                                }
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
                sourceElementType!);

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
                            new ColumnReferenceExpression
                            {
                                MultiPartIdentifier = new MultiPartIdentifier
                                {
                                    Identifiers =
                                    {
                                        new Identifier { Value = SourceTableAlias },
                                        new Identifier { Value = "id" }
                                    }
                                }
                            }
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
                "{http://hl7.org/fhir}Condition",
                new FhirSqlTableMapEntry
                {
                    SqlTableName = "condition",
                    DefaultCodingCodeExpression = new ColumnReferenceExpression
                    {
                        MultiPartIdentifier = new MultiPartIdentifier
                        { 
                            // TODO: figure out what to do with table identifier; probably need to make this unique (ie dynamicly generated)
                            Identifiers = { new Identifier { Value = SourceTableAlias }, new Identifier { Value = "code_coding_code"  } }
                        }
                    },
                    DefaultCodingCodeSystemExpression = new ColumnReferenceExpression
                    {
                        MultiPartIdentifier = new MultiPartIdentifier
                        {
                            Identifiers = { new Identifier { Value = SourceTableAlias }, new Identifier { Value = "code_coding_system" } }
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
                                    new ColumnReferenceExpression
                                    {
                                        MultiPartIdentifier = new MultiPartIdentifier
                                        {
                                            Identifiers =
                                            {
                                                new Identifier { Value = SourceTableAlias },
                                                new Identifier { Value = "subject_string" }
                                            }
                                        }
                                    },
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
                    DefaultCodingCodeExpression = new ColumnReferenceExpression
                    {
                        MultiPartIdentifier = new MultiPartIdentifier
                        { 
                            // TODO: figure out what to do with table identifier; probably need to make this unique (ie dynamicly generated)
                            Identifiers = { new Identifier { Value = SourceTableAlias }, new Identifier { Value = "code_coding_code"  } }
                        }
                    },
                    DefaultCodingCodeSystemExpression = new ColumnReferenceExpression
                    {
                        MultiPartIdentifier = new MultiPartIdentifier
                        {
                            Identifiers = { new Identifier { Value = SourceTableAlias }, new Identifier { Value = "code_coding_system" } }
                        }
                    }
                }
            },
        };


        // returns a selectstatement, which could be a selectscalarexpression or selectstarexpression
        private SqlExpression? ExpressionRef(ExpressionRef ere, SqlExpressionBuilderContext ctx)
        {
            string functionName = ere.name;

            // is this a scalar or list function?
            // scalar hardcodes to a column called 'result'
            // a list (currently) does select *
            bool isScalar = (ere.resultTypeSpecifier == null || !(ere.resultTypeSpecifier is Elm.ListTypeSpecifier));

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
                                new Identifier { Value = functionName }
                            }
                        },
                        Alias = new Identifier { Value = functionName },
                    }
                }
            };

            if (isScalar)
            {
                // if it's a tuple type, then add all the columns; else just add Result
                // TODO: for now, hardcode Interval but eventually this has to be generic

                if (ere.resultTypeSpecifier is Elm.IntervalTypeSpecifier intervalTypeSpecifier)
                {
                    var intervalTuple = new ScalarTuple
                    {
                        DataType = typeof(CqlInterval<>) // TODO: extract base type
                    };

                    intervalTuple.Values.Add("low", BuildColumnReferenceExpression(functionName, "low"));
                    intervalTuple.Values.Add("hi", BuildColumnReferenceExpression(functionName, "hi"));
                    intervalTuple.Values.Add("lowClosed", BuildColumnReferenceExpression(functionName, "lowClosed"));
                    intervalTuple.Values.Add("hiClosed", BuildColumnReferenceExpression(functionName, "hiClosed"));

                    return WrapInSelectScalarExpression(intervalTuple, fromClause);
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
                                new Identifier { Value = functionName }
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
                        SqlExpression.FragmentTypes.SelectStatementScalar, fhirType); // TODO: type needs to be passed in
                }
                else
                {
                    return WrapInSelectScalarExpression(
                        BuildColumnReferenceExpression(functionName, ResultColumnName),
                        fromClause);
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
                                            new Identifier { Value = functionName}
                                        }
                                    }
                                }
                            },

                                FromClause = fromClause
                            },
                        },
                    },
                    SqlExpression.FragmentTypes.SelectStatement,
                    typeof(object)); // TODO: infer type
            }


            ColumnReferenceExpression BuildColumnReferenceExpression(string functionName, string columnName)
            {
                return new ColumnReferenceExpression
                {
                    MultiPartIdentifier = new MultiPartIdentifier
                    {
                        Identifiers =
                            {
                                new Identifier { Value = functionName},
                                new Identifier { Value = columnName }
                            }
                    }
                };
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

            return WrapInSelectScalarExpression(result, NullFromClause());
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
                                                    Identifier = new Identifier { Value = UnusedColumnName }
                                                }
                                            }
                                        }
                        },
                        Alias = new Identifier { Value = UnusedTableName }
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
            var (lhs, lhsFrom) = UnwrapScalarSelectElement(TranslateExpression(binaryExpression.operand[0], ctx));
            var (rhs, rhsFrom) = UnwrapScalarSelectElement(TranslateExpression(binaryExpression.operand[1], ctx));

            ScalarExpression fragment = new BinaryExpression
            {
                BinaryExpressionType = binType,
                FirstExpression = lhs,
                SecondExpression = rhs
            };

            if (NeedsParenthesis(binaryExpression, ctx.Parent))
            {
                var parenthesis = new ParenthesisExpression
                {
                    Expression = fragment
                };

                fragment = parenthesis;
            }
            return WrapInSelectScalarExpression(fragment, ReconcileScalarFromClauses(lhsFrom, rhsFrom));
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

            // search through the from clauses (backwards); remember if any are literal scalar, and remove them
            for (int i = fromList.Count() - 1; i >= 0; i--)
            {
                FromClause from = froms[i];

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

        private FromClause FindTableReference(TSqlFragment sqlFragment)
        {
            if (sqlFragment is SelectStatement selectStatment)
            {
                QueryExpression inner = selectStatment.QueryExpression;

                // unwrap brackets
                while (inner is QueryParenthesisExpression queryParenthesisExpression)
                    inner = queryParenthesisExpression.QueryExpression;

                QuerySpecification querySpecification = inner as QuerySpecification ?? throw new InvalidOperationException();

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
            /// </summary>
            public ScalarExpression SingleValue
            {
                get
                {
                    if (Count != 1)
                        throw new InvalidOperationException();
                    return Values.Values.First();
                }
            }
        }

        // scalars need to be wrapped in full select statements (and correspondingly unwrapped for complex expressions)
        // when unwrapping, the return would be the inner expression(s) as well as the tables
        // and then when wrapping, tables are added
        //
        //
        // NOTE:  an explicit TOP 1 is added to indicate a scalar. 
        private SqlExpression WrapInSelectScalarExpression(ScalarTuple scalarTuple, FromClause fromClause, bool createScalarSubquery = false)
        {
            // build the basic query specification
            QuerySpecification querySpecification = new QuerySpecification
            {
                FromClause = fromClause,
                TopRowFilter = new TopRowFilter
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
                        Identifier = new Identifier { Value = field.Key }
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
                    SqlExpression.FragmentTypes.SelectStatementScalar, scalarTuple.DataType);
            }
            else
            {
                return new SqlExpression(
                    new SelectStatement
                    {
                        QueryExpression = queryExpression
                    },
                    SqlExpression.FragmentTypes.SelectStatementScalar, scalarTuple.DataType); // TODO: type needs to be passed in
            }
        }

        // helper method when the scalar is just one field (TODO: is this still useful?)
        private SqlExpression WrapInSelectScalarExpression(ScalarExpression scalar, FromClause fromClause, bool createScalarSubquery = false)
        {
            return WrapInSelectScalarExpression(
                new ScalarTuple
                {
                    Values = { { ResultColumnName, scalar } },
                    DataType = typeof(object) // TODO: type needs to be inferred?
                },
                fromClause,
                createScalarSubquery);
        }

        // expressions are (always?) wrapped as a SelectElement (a SelectScalarExpression)
        // but if used in a larger expression, need to be unwrapped
        private (ScalarExpression, FromClause) UnwrapScalarSelectElement(SqlExpression sqlFragment)
        {
            if (sqlFragment.IsSelectStatement
                && sqlFragment.SqlFragment is SelectStatement selectStatment)
            {
                QueryExpression inner = selectStatment.QueryExpression;

                // unwrap brackets
                while (inner is QueryParenthesisExpression queryParenthesisExpression)
                    inner = queryParenthesisExpression.QueryExpression;

                QuerySpecification querySpecification = inner as QuerySpecification ?? throw new InvalidOperationException();
                // TODO: strong assumption here is that the 0th element is the one we want
                // to be complete, it is the one marked "_Result" or a tuple if we get that fancy
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
                QueryExpression inner = selectStatment.QueryExpression;

                // unwrap brackets
                while (inner is QueryParenthesisExpression queryParenthesisExpression)
                    inner = queryParenthesisExpression.QueryExpression;

                QuerySpecification querySpecification = inner as QuerySpecification ?? throw new InvalidOperationException();

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

            return WrapInSelectScalarExpression(fragment, fromClause);
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

        private SqlExpression WrapBooleanExpressionInSelectStatement(ScalarExpression scalarExpression, FromClause fromClause)
        {
            return WrapBooleanExpressionInSelectStatement(
                new BooleanComparisonExpression
                {
                    ComparisonType = BooleanComparisonType.Equals,
                    FirstExpression = scalarExpression,
                    SecondExpression = new IntegerLiteral { Value = "1" }
                },
                fromClause);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="booleanExpression"></param>
        /// <param name="fromClause"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private SqlExpression WrapBooleanExpressionInSelectStatement(BooleanExpression booleanExpression, FromClause fromClause)
        {
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
                                Identifier = new Identifier { Value  = ResultColumnName }
                            }
                        }
                    },
                    FromClause = fromClause
                }
            };

            return new SqlExpression(
                booleanSelectStatement,
                SqlExpression.FragmentTypes.SelectBooleanExpression,
                typeof(bool));
        }

    }

}