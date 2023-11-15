using Hl7.Cql.Abstractions;
using Hl7.Cql.Elm;
using Hl7.Cql.Model;
using Hl7.Cql.Primitives;
using Hl7.Cql.Runtime;

using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.TransactSql.ScriptDom;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using BinaryExpression = Microsoft.SqlServer.TransactSql.ScriptDom.BinaryExpression;
using Expression = System.Linq.Expressions.Expression;

namespace Hl7.Cql.Compiler
{
    internal class SqlExpressionBuilder : ExpressionBuilderBase<SqlExpressionBuilder, TSqlFragment>
    {
        protected internal override TypeResolver TypeResolver { get; }

        public SqlExpressionBuilder(Library library, TypeResolver typeResolver, ILogger<SqlExpressionBuilder> builderLogger)
            : base(library, builderLogger)
        {
            TypeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
        }

        public override DefinitionDictionary<TSqlFragment> Build()
        {
            var definitions = new DefinitionDictionary<TSqlFragment>();
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
                //var codeSystemUrls = Library.codeSystems?
                //    .ToDictionary(cs => cs.name, cs => cs.id) ?? new Dictionary<string, string>();
                //var codesByName = new Dictionary<string, CqlCode>();
                //var codesByCodeSystemName = new Dictionary<string, List<CqlCode>>();
                if (this.Library.codes != null)
                {
                    //foreach (var code in Library.codes)
                    //{
                    //    if (code.codeSystem == null)
                    //        throw new InvalidOperationException("Code definition has a null codeSystem node.");
                    //    if (!codeSystemUrls.TryGetValue(code.codeSystem.name, out var csUrl))
                    //        throw new InvalidOperationException($"Undefined code system {code.codeSystem.name!}");
                    //    var existingCode = codesByName.Values.SingleOrDefault(c => c.code == code.id && c.system == csUrl);
                    //    if (existingCode != null)
                    //        throw new InvalidOperationException($"Duplicate code detected: {code.id} from {code.codeSystem.name} ({csUrl})");
                    //    var systemCode = new CqlCode(code.id, csUrl, null, null);
                    //    codesByName.Add(code.name, systemCode);
                    //    if (!codesByCodeSystemName.TryGetValue(code.codeSystem!.name!, out var codings))
                    //    {
                    //        codings = new List<CqlCode>();
                    //        codesByCodeSystemName.Add(code.codeSystem!.name!, codings);
                    //    }
                    //    codings.Add(systemCode);

                    //    var newCodingExpression = Expression.New(codeCtor,
                    //        Expression.Constant(code.id),
                    //        Expression.Constant(csUrl),
                    //        Expression.Constant(null, typeof(string)),
                    //        Expression.Constant(null, typeof(string))!
                    //    );
                    //    var contextParameter = Expression.Parameter(typeof(CqlContext), "context");
                    //    var lambda = Expression.Lambda(newCodingExpression, contextParameter);
                    //    definitions.Add(ThisLibraryKey, code.name!, lambda);
                    //}
                }

                if (this.Library.codeSystems != null)
                {
                    //foreach (var codeSystem in Library.codeSystems)
                    //{
                    //    if (codesByCodeSystemName.TryGetValue(codeSystem.name, out var codes))
                    //    {
                    //        var initMembers = codes
                    //            .Select(coding =>
                    //                Expression.New(codeCtor,
                    //                    Expression.Constant(coding.code),
                    //                    Expression.Constant(coding.system),
                    //                    Expression.Constant(null, typeof(string)),
                    //                    Expression.Constant(null, typeof(string))
                    //                ))
                    //            .ToArray();
                    //        var arrayOfCodesInitializer = Expression.NewArrayInit(typeof(CqlCode), initMembers);
                    //        var contextParameter = Expression.Parameter(typeof(CqlContext), "context");
                    //        var lambda = Expression.Lambda(arrayOfCodesInitializer, contextParameter);
                    //        definitions.Add(ThisLibraryKey, codeSystem.name, lambda);
                    //    }
                    //    else
                    //    {
                    //        var newArray = Expression.NewArrayBounds(typeof(CqlCode), Expression.Constant(0, typeof(int)));
                    //        var contextParameter = Expression.Parameter(typeof(CqlContext), "context");
                    //        var lambda = Expression.Lambda(newArray, contextParameter);
                    //        definitions.Add(ThisLibraryKey, codeSystem.name, lambda);
                    //    }
                    //}
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
                            buildContext = buildContext.Deeper(def);

                            TSqlFragment queryExpression = TranslateExpression(def.expression, buildContext);

                            var bodyExpression = WrapWithSelect(queryExpression, buildContext);

                            definitions.Add(ThisLibraryKey, def.name, new Type[0], bodyExpression);
                        }
                        catch
                        {
                            buildContext.LogError("Failed to create SQL expression", def);
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

        private TSqlFragment WrapWithSelect(TSqlFragment queryExpression, SqlExpressionBuilderContext context)
        {
            TSqlFragment? select = null;

            // does the queryExpression have literals?
            // create a wrapping scalar expression
            if (ExpressionHasLiterals(queryExpression))
            {
                var selectQueryExpression = new SelectScalarExpression
                {
                    Expression = queryExpression as ScalarExpression ?? throw new ArgumentException(),
                    ColumnName = new IdentifierOrValueExpression
                    {
                        Identifier = new Identifier { Value = "Result" }
                    }
                };

                select = new SelectStatement
                {
                    QueryExpression = new QueryParenthesisExpression
                    {
                        QueryExpression = new QuerySpecification
                        {
                            SelectElements = 
                            {
                                selectQueryExpression
                            },
                            FromClause = new FromClause
                            {
                                TableReferences =
                                {
                                    context.OutputContext.FromTables
                                }
                            }
                        },
                    }
                };
            }

            if (select == null)
            {
                throw new NotImplementedException();
            }

            return select;
        }

        private bool ExpressionHasLiterals(TSqlFragment queryExpression)
        {
            return true; // TODO: implement -- walk the tree and look for literals
        }

        private TSqlFragment TranslateExpression(Element op, SqlExpressionBuilderContext ctx)
        {
            ctx = ctx.Deeper(op);
            TSqlFragment? result = null;
            switch (op)
            {
                case Abs abs:
                    // result = Abs(abs, ctx);
                    break;
                case Add add:
                    result = Add(add, ctx);
                    break;
                case After after:
                    // result = After(after, ctx);
                    break;
                case AliasRef ar:
                    // result = AliasRef(ar, ctx);
                    break;
                case AllTrue alt:
                    // result = AllTrue(alt, ctx);
                    break;
                case And and:
                    // result = And(and, ctx);
                    break;
                case As @as:
                    // result = As(@as, ctx);
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
                    // result = Before(before, ctx);
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
                    // result = CodeRef(cre, ctx);
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
                    // result = Count(ce, ctx);
                    break;
                case DateFrom dfe:
                    // result = DateFrom(dfe, ctx);
                    break;
                case Elm.DateTime dt:
                    // result = DateTime(dt, ctx);
                    break;
                case Date d:
                    // result = Date(d, ctx);
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
                    // result = Exists(ex, ctx);
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
                    // result = FunctionRef(fre, ctx);
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
                    // result = Greater(gtr, ctx);
                    break;
                case GreaterOrEqual gtre:
                    // result = GreaterOrEqual(gtre, ctx);
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
                    // result = Intervalresult(ie, ctx);
                    break;
                case InValueSet inv:
                    // result = InValueSet(inv, ctx);
                    break;
                case In @in:
                    // result = In(@in, ctx);
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
                    // result = Less(less, ctx);
                    break;
                case LessOrEqual lesse:
                    // result = LessOrEqual(lesse, ctx);
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
                    // result = Or(or, ctx);
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
                    // result = Property(pe, ctx);
                    break;
                case Quantity qua:
                    // result = Quantity(qua, ctx);
                    break;
                case Query qe:
                    // result = Query(qe, ctx);
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
                    // result = SingletonFrom(sf, ctx);
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
                    // result = ToList(tle, ctx);
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

        private TSqlFragment? Retrieve(Retrieve retrieve, SqlExpressionBuilderContext ctx)
        {
            throw new NotImplementedException();

            //Type? sourceElementType;
            //string? cqlRetrieveResultType;

            //// SingletonFrom does not have this specified; in this case use DataType instead
            //if (retrieve.resultTypeSpecifier == null)
            //{
            //    if (string.IsNullOrWhiteSpace(retrieve.dataType.Name))
            //        throw new ArgumentException("If a Retrieve lacks a ResultTypeSpecifier it must have a DataType", nameof(retrieve));
            //    cqlRetrieveResultType = retrieve.dataType.Name;

            //    sourceElementType = TypeResolver.ResolveType(cqlRetrieveResultType);
            //}
            //else
            //{
            //    if (retrieve.resultTypeSpecifier is Elm.ListTypeSpecifier listTypeSpecifier)
            //    {
            //        cqlRetrieveResultType = listTypeSpecifier.elementType is Elm.NamedTypeSpecifier nts ? nts.name.Name : null;
            //        sourceElementType = TypeManager.TypeFor(listTypeSpecifier.elementType, ctx);
            //    }
            //    else throw new NotImplementedException($"Sources with type {retrieve.resultTypeSpecifier.GetType().Name} are not implemented.");
            //}

            //Expression? codeProperty;

            //var hasCodePropertySpecified = sourceElementType != null && retrieve.codeProperty != null;
            //var isDefaultCodeProperty = retrieve.codeProperty is null ||
            //    (cqlRetrieveResultType is not null &&
            //     modelMapping.TryGetValue(cqlRetrieveResultType, out ClassInfo? classInfo) &&
            //     classInfo.primaryCodePath == retrieve.codeProperty);

            //if (hasCodePropertySpecified && !isDefaultCodeProperty)
            //{
            //    var codePropertyInfo = TypeResolver.GetProperty(sourceElementType!, retrieve.codeProperty!);
            //    codeProperty = Expression.Constant(codePropertyInfo, typeof(PropertyInfo));
            //}
            //elsef
            //{
            //    codeProperty = Expression.Constant(null, typeof(PropertyInfo));
            //}

            //if (retrieve.codes != null)
            //{
            //    if (retrieve.codes is ValueSetRef valueSetRef)
            //    {
            //        if (string.IsNullOrWhiteSpace(valueSetRef.name))
            //            throw new ArgumentException($"The ValueSetRef at {valueSetRef.locator} is missing a name.", nameof(retrieve));
            //        var valueSet = InvokeDefinitionThroughRuntimeContext(valueSetRef.name!, valueSetRef!.libraryName, typeof(CqlValueSet), ctx);
            //        var call = OperatorBinding.Bind(CqlOperator.Retrieve, ctx.RuntimeContextParameter,
            //            Expression.Constant(sourceElementType, typeof(Type)), valueSet, codeProperty!);
            //        return call;
            //    }
            //    else
            //    {
            //        // In this construct, instead of querying a value set, we're testing resources
            //        // against a list of codes, e.g., as defined by the code from or codesystem construct
            //        var codes = TranslateExpression(retrieve.codes, ctx);
            //        var call = OperatorBinding.Bind(CqlOperator.Retrieve, ctx.RuntimeContextParameter,
            //            Expression.Constant(sourceElementType, typeof(Type)), codes, codeProperty!);
            //        return call;
            //    }
            //}
            //else
            //{
            //    var call = OperatorBinding.Bind(CqlOperator.Retrieve, ctx.RuntimeContextParameter,
            //        Expression.Constant(sourceElementType, typeof(Type)), Expression.Constant(null, typeof(CqlValueSet)), codeProperty!);
            //    return call;
            //}
        }
        

        private TSqlFragment? ExpressionRef(ExpressionRef ere, SqlExpressionBuilderContext ctx)
        {
            string functionName = ere.name;

            ctx.OutputContext.AddJoinFunctionReference(functionName, functionName);

            var referenceExpression = new ColumnReferenceExpression
            {
                MultiPartIdentifier = new MultiPartIdentifier
                {
                    Identifiers =
                    {
                        new Identifier { Value = functionName},
                        new Identifier { Value = "Result" }
                    }
                }
            };

            return referenceExpression;
        }

        private TSqlFragment? Literal(Elm.Literal lit, SqlExpressionBuilderContext ctx)
        {
            Microsoft.SqlServer.TransactSql.ScriptDom.Literal? result = null;

            // TODO: would this need to force type in some cases?
            switch (lit.valueType.Name.ToLowerInvariant())
            {
                case "{urn:hl7-org:elm-types:r1}integer":
                    // TODO:  validate?
                    result = new IntegerLiteral { Value = lit.value };
                    break;

                case "{urn:hl7-org:elm-types:r1}any":
                case "{urn:hl7-org:elm-types:r1}date":
                case "{urn:hl7-org:elm-types:r1}datetime":
                case "{urn:hl7-org:elm-types:r1}quantity":
                case "{urn:hl7-org:elm-types:r1}long":
                case "{urn:hl7-org:elm-types:r1}boolean":
                case "{urn:hl7-org:elm-types:r1}string":
                case "{urn:hl7-org:elm-types:r1}decimal":
                    result = new NumericLiteral { Value = lit.value };
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

            return result;
        }

        private TSqlFragment? Add(Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx) 
            => BinaryScalarExpression(BinaryExpressionType.Add, binaryExpression, ctx);
        private TSqlFragment? Subtract(Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx) 
            => BinaryScalarExpression(BinaryExpressionType.Subtract, binaryExpression, ctx);
        private TSqlFragment? Multiply(Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx) 
            => BinaryScalarExpression(BinaryExpressionType.Multiply, binaryExpression, ctx);
        private TSqlFragment? Divide(Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx) 
            => BinaryScalarExpression(BinaryExpressionType.Divide, binaryExpression, ctx);


        private TSqlFragment BinaryScalarExpression(BinaryExpressionType binType, Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx)
        {
            ScalarExpression? lhs = TranslateExpression(binaryExpression.operand[0], ctx) as ScalarExpression;
            ScalarExpression? rhs = TranslateExpression(binaryExpression.operand[1], ctx) as ScalarExpression;

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
            return fragment;
        }

        private TSqlFragment? ToDecimal(ToDecimal tde, SqlExpressionBuilderContext ctx)
        {
            var fragment = new CastCall
            {
                DataType = new SqlDataTypeReference
                {
                    SqlDataTypeOption = SqlDataTypeOption.Decimal
                },
                Parameter = new ParenthesisExpression
                {
                    Expression = TranslateExpression(tde.operand, ctx) as ScalarExpression
                }
            };

            return fragment;
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

    }
}