using Hl7.Cql.Abstractions;
using Hl7.Cql.Elm;
using Hl7.Cql.Model;
using Hl7.Cql.Primitives;
using Hl7.Cql.Runtime;

using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.TransactSql.ScriptDom;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
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
        public SqlExpressionBuilder(Library library, TypeManager typeManager, ILogger<SqlExpressionBuilder> builderLogger)
            : base(library, typeManager, builderLogger)
        {
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

                        TSqlFragment codeSqlExpression = BuildSelectForCode(systemCode);
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
                            TSqlFragment codesystemSqlExpression = BuildSelectForCodeSystem(codes);

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

        private TSqlFragment BuildSelectForCodeSystem(List<CqlCode> codes)
        {
            InlineDerivedTable inlineTable = new InlineDerivedTable
            {
                Columns =
                {
                    new Identifier { Value = "code" },
                    new Identifier { Value = "codesystem" },
                    new Identifier { Value = "display" },
                    new Identifier { Value = "ver" },
                },
                Alias = new Identifier { Value = "codes" }
            };

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

            return select;
        }

        private TSqlFragment BuildSelectForCode(CqlCode systemCode)
        {
            return BuildSelectForCodeSystem(new List<CqlCode> { systemCode });
        }

        private TSqlFragment WrapWithSelect(TSqlFragment queryExpression, SqlExpressionBuilderContext context, bool createScalarSubquery = false)
        {
            TSqlFragment? select = null;

            switch(queryExpression)
            {
                case SelectStatement selectStatement:
                    select = selectStatement;
                    break;
                case SelectScalarExpression selectScalarExpression:
                    select = Wrap(
                        selectScalarExpression.Expression,
                        context,
                        createScalarSubquery);
                    break;

                case ScalarExpression scalarExpression:
                    select = Wrap(
                        scalarExpression,
                        context,
                        createScalarSubquery);
                    break;
            }

            if (select == null)
            {
                throw new NotImplementedException();
            }

            return select;

            static TSqlFragment Wrap(ScalarExpression scalarExpression, SqlExpressionBuilderContext context, bool createScalarSubquery)
            {
                TSqlFragment? select;
                var selectQuerySpecification = new QueryParenthesisExpression
                {
                    QueryExpression = new QuerySpecification
                    {
                        SelectElements =
                        {
                            new SelectScalarExpression
                            {
                                Expression = scalarExpression,
                                ColumnName = new IdentifierOrValueExpression
                                {
                                    Identifier = new Identifier { Value = "Result" }
                                }
                            }
                        },
                        FromClause = context.OutputContext.FromClause
                    },
                };

                if (!createScalarSubquery)
                {
                    select = new SelectStatement
                    {
                        QueryExpression = selectQuerySpecification
                    };
                }
                else
                {
                    select = new ScalarSubquery
                    {
                        QueryExpression = selectQuerySpecification
                    };
                }

                return select;
            }
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
                    // result = Count(ce, ctx);
                    break;
                case DateFrom dfe:
                    // result = DateFrom(dfe, ctx);
                    break;
                case Elm.DateTime dt:
                    result = DateTime(dt, ctx);
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

        // returns a boolean expression
        private TSqlFragment? After(After after, SqlExpressionBuilderContext ctx)
        {
            // TODO: this is weird because it's likely a columnref but will it be wrapped as a Select? and what does that mean
            var lhs = TranslateExpression(after.operand[0], ctx);

            // TODO:  unwrap SelectStatement (and rewrap as ScalarSubquery)
            var (rhs, rhsFrom) = UnwrapScalarSelectElement(TranslateExpression(after.operand[1], ctx));

            var binaryExpression = new BooleanComparisonExpression
            {
                FirstExpression = lhs as ScalarExpression ?? throw new InvalidOperationException(),
                ComparisonType = BooleanComparisonType.GreaterThan,
                SecondExpression = rhs
            };

            return binaryExpression;
        }

        // returns BooleanExpression
        private TSqlFragment? Before(Before before, SqlExpressionBuilderContext ctx)
        {
            // TODO: see notes from After

            var lhs = TranslateExpression(before.operand[0], ctx);

            var (rhs, rhsFrom) = UnwrapScalarSelectElement(TranslateExpression(after.operand[1], ctx));

            var binaryExpression = new BooleanComparisonExpression
            {
                FirstExpression = lhs as ScalarExpression ?? throw new InvalidOperationException(),
                ComparisonType = BooleanComparisonType.LessThan,
                SecondExpression = rhs as ScalarExpression ?? throw new InvalidOperationException()
            };

            return binaryExpression;
        }

        // returns a boolean expression
        private TSqlFragment? And(And and, SqlExpressionBuilderContext ctx)
        {
            var lhs = TranslateExpression(and.operand[0], ctx);
            var rhs = TranslateExpression(and.operand[1], ctx);

            return new BooleanBinaryExpression
            {
                FirstExpression = lhs as BooleanExpression ?? throw new InvalidOperationException(),
                BinaryExpressionType = BooleanBinaryExpressionType.And,
                SecondExpression = rhs as BooleanExpression ?? throw new InvalidOperationException()
            };
        }


        // create a subselect expression that builds a datetime2
        private TSqlFragment? DateTime(Elm.DateTime dt, SqlExpressionBuilderContext ctx)
        {
            var functionExpression = new FunctionCall
            {
                FunctionName = new Identifier { Value = "DATETIME2FROMPARTS" },
                Parameters =
                {
                    TranslateExpression(dt.year, ctx) as ScalarExpression ?? throw new InvalidOperationException(),
                    TranslateExpression(dt.month, ctx) as ScalarExpression ?? throw new InvalidOperationException(),
                    TranslateExpression(dt.day, ctx) as ScalarExpression ?? throw new InvalidOperationException(),
                    TranslateExpression(dt.hour, ctx) as ScalarExpression ?? throw new InvalidOperationException(),
                    TranslateExpression(dt.minute, ctx) as ScalarExpression ?? throw new InvalidOperationException(),
                    TranslateExpression(dt.second, ctx) as ScalarExpression ?? throw new InvalidOperationException(),
                    TranslateExpression(dt.millisecond, ctx) as ScalarExpression ?? throw new InvalidOperationException(),
                    new IntegerLiteral { Value = "7" }, // precision hardcoded
                }
            };

            return WrapWithSelect(functionExpression, ctx, true);
        }

        // returns ColumnReference?
        // TODO: can this be a full SelectStatement? to be consistent
        private TSqlFragment? Property(Property pe, SqlExpressionBuilderContext ctx)
        {
            ColumnReferenceExpression? columnReferenceExpression = null;

            var sourceTable = ctx.GetScope(pe.scope) ?? throw new InvalidOperationException(); 
            var sourceTableType = sourceTable.Type;
            var sourceTableIdentifier = sourceTable.SqlExpression as Identifier ?? throw new InvalidOperationException();

            // map the property name to a column name -- TODO: should be a lookup
            switch (sourceTableType.Name.ToLowerInvariant())
            {
                case "condition":
                    {
                        switch (pe.path)
                        {
                            case "onset":
                                columnReferenceExpression = new ColumnReferenceExpression
                                {
                                    MultiPartIdentifier = new MultiPartIdentifier
                                    {
                                        Identifiers =
                                        {
                                            sourceTableIdentifier,
                                            new Identifier { Value = "onsetDateTime" }
                                        }
                                    }
                                };
                                break;
                        }
                        break;
                    }
            }

            if (columnReferenceExpression == null)
                throw new NotImplementedException();

            return columnReferenceExpression;
        }

        private TSqlFragment? As(As @as, SqlExpressionBuilderContext ctx)
        {
            // TODO: make this no-op for now

            return TranslateExpression(@as.operand, ctx);
        }

        private TSqlFragment? FunctionRef(FunctionRef fre, SqlExpressionBuilderContext ctx)
        {
            if (StringComparer.InvariantCultureIgnoreCase.Compare(fre.libraryName, "FHIRHelpers") == 0)
            {
                if (StringComparer.InvariantCultureIgnoreCase.Compare(fre.name, "ToDateTime") == 0)
                {
                    // TODO: no-op the ToDateTime function since we'll assume it already is a datetime
                    return TranslateExpression(fre.operand[0], ctx);

                }
            }
            throw new NotImplementedException();
        }

        private TSqlFragment? ToList(ToList tle, SqlExpressionBuilderContext ctx)
        {
            // big hack -- everything is a list already
            return TranslateExpression(tle.operand, ctx);
        }

        /// <summary>
        /// Return a QueryExpression (expected to be integrated into another statement)
        /// </summary>
        /// <param name="cre"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private TSqlFragment? CodeRef(CodeRef cre, SqlExpressionBuilderContext ctx)
        {
            var select = new QuerySpecification
            {
                SelectElements =
                {
                    new SelectStarExpression()
                },
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

            return select;
        }

        private TSqlFragment? Query(Query query, SqlExpressionBuilderContext ctx)
        {
            if (query?.source?.Length == 0)
                throw new NotSupportedException("Queries must define at least 1 source");
            else if (query!.source!.Length == 1)
                return SingleSourceQuery(query, ctx);
            else
                return MultiSourceQuery(query, ctx);
        }

        // returns a SelectStatement
        private TSqlFragment? SingleSourceQuery(Query query, SqlExpressionBuilderContext ctx)
        {
            var querySource = query.source![0];

            var querySourceAlias = querySource.alias;

            if (string.IsNullOrWhiteSpace(querySource.alias))
                throw new ArgumentException("Only aliased query sources are supported.", nameof(query));

            if (querySource.expression == null)
                throw new ArgumentException("Query sources must have an expression", nameof(query));

            // fully formed SELECT * with table (and _for now_ no where clause)
            var source = TranslateExpression(querySource.expression!, ctx);


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
                Identifier tableIdentifier = new Identifier { Value = "sourceTable" };  // TODO: this should be the same as what was used in the retrieve
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

                var whereBody = TranslateExpression(query.where, subContext);

                var sourceSelect = source as SelectStatement ?? throw new InvalidOperationException();
                var queryExpression = sourceSelect.QueryExpression as QuerySpecification ?? throw new InvalidOperationException();

                queryExpression.WhereClause = new WhereClause
                {
                    SearchCondition = whereBody as BooleanExpression ?? throw new InvalidOperationException()
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

            return source;

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



        private TSqlFragment? MultiSourceQuery(Query query, SqlExpressionBuilderContext ctx)
        {
            throw new NotImplementedException();
        }

        // returns a SelectStatement
        private TSqlFragment? Retrieve(Retrieve retrieve, SqlExpressionBuilderContext ctx)
        {
            Type? sourceElementType;
            string? cqlRetrieveResultType;

            // SingletonFrom does not have this specified; in this case use DataType instead
            if (retrieve.resultTypeSpecifier == null)
            {
                if (string.IsNullOrWhiteSpace(retrieve.dataType.Name))
                    throw new ArgumentException("If a Retrieve lacks a ResultTypeSpecifier it must have a DataType", nameof(retrieve));
                cqlRetrieveResultType = retrieve.dataType.Name;

                sourceElementType = TypeResolver.ResolveType(cqlRetrieveResultType);
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
            ScalarExpression? codeColumnExpression = sqlTableEntry.DefaultCodingCodeExpression;

            // TODO: for now select * --- maybe will have to shape the result later
            selectElements.Add(new SelectStarExpression());

            TableReference sourceTableReference = new NamedTableReference
            {
                SchemaObject = new SchemaObjectName
                {
                    Identifiers =
                                {
                                    new Identifier { Value = sqlTableEntry.SqlTableName }
                                }
                },
                Alias = new Identifier { Value = "sourceTable" }  // TODO: should this be a dynamic name?  see joins below
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
                    QuerySpecification codeSelect = TranslateExpression(retrieve.codes, ctx) as QuerySpecification ?? throw new InvalidOperationException();

                    tableReferences.Add(new QualifiedJoin
                    {
                        FirstTableReference = sourceTableReference,
                        SecondTableReference = new QueryDerivedTable
                        {
                            QueryExpression = codeSelect,
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
                    //WhereClause = whereClause
                }
            };

            // gotta copy 'cause the SQL dom list properties are read-only
            IList<SelectElement> se = ((QuerySpecification)(select.QueryExpression)).SelectElements;
            selectElements.ForEach(e => se.Add(e));

            IList<TableReference> tr = ((QuerySpecification)(select.QueryExpression)).FromClause.TableReferences;
            tableReferences.ForEach(t => tr.Add(t));

            return select;
        }

        public class FhirSqlTableMapEntry
        {
            public string SqlTableName { get; init; } = String.Empty;

            // how to find the default code property into sql (typically a column reference)
            public ScalarExpression? DefaultCodingCodeExpression { get; init; } = null;
            public ScalarExpression? DefaultCodingCodeSystemExpression { get; init; } = null;
        }

        /// <summary>
        /// for now, harded metadata about the FHIR tables. 
        /// this should be data-driven, and also may have some dynamic components 
        /// </summary>
        public Dictionary<string, FhirSqlTableMapEntry> FhirSqlTableMap { get; } = new Dictionary<string, FhirSqlTableMapEntry>
        {
            { "{http://hl7.org/fhir}Patient", new FhirSqlTableMapEntry { SqlTableName = "patient" } },
            { "{http://hl7.org/fhir}Encounter", new FhirSqlTableMapEntry { SqlTableName = "encounter" } },
            { "{http://hl7.org/fhir}Condition", 
                new FhirSqlTableMapEntry
                {
                    SqlTableName = "condition", 
                    DefaultCodingCodeExpression = new ColumnReferenceExpression 
                    { 
                        MultiPartIdentifier = new MultiPartIdentifier 
                        { 
                            // TODO: figure out what to do with table identifier; probably need to make this unique (ie dynamicly generated)
                            Identifiers = { new Identifier { Value = "sourceTable" }, new Identifier { Value = "code_coding_code"  } }
                        }
                    },
                    DefaultCodingCodeSystemExpression = new ColumnReferenceExpression
                    {
                        MultiPartIdentifier = new MultiPartIdentifier
                        {
                            Identifiers = { new Identifier { Value = "sourceTable" }, new Identifier { Value = "code_coding_system" } }
                        }
                    }
                }
            },
            { "{http://hl7.org/fhir}Observation", 
                new FhirSqlTableMapEntry
                {
                    SqlTableName = "observation",
                    DefaultCodingCodeExpression = new ColumnReferenceExpression
                    {
                        MultiPartIdentifier = new MultiPartIdentifier
                        { 
                            // TODO: figure out what to do with table identifier; probably need to make this unique (ie dynamicly generated)
                            Identifiers = { new Identifier { Value = "sourceTable" }, new Identifier { Value = "code_coding_code"  } }
                        }
                    },
                    DefaultCodingCodeSystemExpression = new ColumnReferenceExpression
                    {
                        MultiPartIdentifier = new MultiPartIdentifier
                        {
                            Identifiers = { new Identifier { Value = "sourceTable" }, new Identifier { Value = "code_coding_system" } }
                        }
                    }
                }
            },
        };


        // returns a selectstatement, which could be a selectscalarexpression or selectstarexpression
        // also adds a table reference to the context (which later becomes a join)
        private TSqlFragment? ExpressionRef(ExpressionRef ere, SqlExpressionBuilderContext ctx)
        {
            string functionName = ere.name;
            ctx.OutputContext.AddJoinFunctionReference(functionName, functionName);

            // is this a scalar or list function?
            // scalar hardcodes to a column called 'result'
            // a list (currently) does select *
            bool isScalar = (ere.resultTypeSpecifier == null || !(ere.resultTypeSpecifier is Elm.ListTypeSpecifier));

            if (isScalar)
            {
                return WrapInSelectScalarExpression(new ColumnReferenceExpression
                {
                    MultiPartIdentifier = new MultiPartIdentifier
                    {
                        Identifiers =
                        {
                            new Identifier { Value = functionName},
                            new Identifier { Value = "Result" }
                        }
                    }
                },
                ctx.OutputContext.FromClause);
            }
            else
            {
                return new SelectStatement
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

                            FromClause = ctx.OutputContext.FromClause
                        },
                    },
                };

            }
        }

        // returns SelectStatement
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
                case "{urn:hl7-org:elm-types:r1}decimal":
                    result = new NumericLiteral { Value = lit.value };
                    break;

                case "{urn:hl7-org:elm-types:r1}any":
                case "{urn:hl7-org:elm-types:r1}date":
                case "{urn:hl7-org:elm-types:r1}datetime":
                case "{urn:hl7-org:elm-types:r1}quantity":
                case "{urn:hl7-org:elm-types:r1}long":
                case "{urn:hl7-org:elm-types:r1}boolean":
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

            return WrapInSelectScalarExpression(result, ctx.OutputContext.FromClause);
        }



        private TSqlFragment? Add(Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx) 
            => BinaryScalarExpression(BinaryExpressionType.Add, binaryExpression, ctx);
        private TSqlFragment? Subtract(Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx) 
            => BinaryScalarExpression(BinaryExpressionType.Subtract, binaryExpression, ctx);
        private TSqlFragment? Multiply(Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx) 
            => BinaryScalarExpression(BinaryExpressionType.Multiply, binaryExpression, ctx);
        private TSqlFragment? Divide(Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx) 
            => BinaryScalarExpression(BinaryExpressionType.Divide, binaryExpression, ctx);


        // returns a SelectStatement
        private TSqlFragment BinaryScalarExpression(BinaryExpressionType binType, Elm.BinaryExpression binaryExpression, SqlExpressionBuilderContext ctx)
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
            return WrapInSelectScalarExpression(fragment, ReconcileFromClauses(lhsFrom, rhsFrom));
        }

        private FromClause ReconcileFromClauses(FromClause lhsFrom, FromClause rhsFrom)
        {
            // TODO: for now, just return the first
            return lhsFrom;
        }

        // expressions are (always?) wrapped as a SelectElement (a SelectScalarExpression)
        // but if used in a larger expression, need to be unwrapped
        private (ScalarExpression, FromClause) UnwrapScalarSelectElement(TSqlFragment sqlFragment)
        {
            if (sqlFragment is SelectStatement selectStatment)
            {
                QueryExpression inner = selectStatment.QueryExpression;

                // unwrap brackets
                while (inner is QueryParenthesisExpression queryParenthesisExpression)
                    inner = queryParenthesisExpression.QueryExpression;
                
                QuerySpecification querySpecification = inner as QuerySpecification ?? throw new InvalidOperationException();
                SelectScalarExpression selectScalarExpression = querySpecification.SelectElements[0] as SelectScalarExpression ?? throw new InvalidOperationException();

                return (selectScalarExpression.Expression, querySpecification.FromClause);
            }
            else
                throw new InvalidOperationException();
        }

        // scalars need to be wrapped in full select statements (and correspondingly unwrapped for complex expressions)
        // when unwrapping, the return would be the inner expression as well as the tables
        // and then when wrapping, tables are added
        // TODO: consider default FromClause (with unused table) - this is used for literals; right now caller must be explicit
        //
        // TODO:  YOU ARE HERE --- handle wrapping as a scalarsubquery
        private TSqlFragment WrapInSelectScalarExpression(ScalarExpression scalar, FromClause fromClause, bool createScalarSubquery)
        {
            return new SelectStatement
            {
                QueryExpression = new QueryParenthesisExpression
                {
                    QueryExpression = new QuerySpecification
                    {
                        SelectElements =
                        {
                            new SelectScalarExpression
                            {
                                Expression = scalar
                            }
                        },
                        FromClause = fromClause
                    },
                },
            };
        }

        // returns a SelectStatement
        private TSqlFragment? ToDecimal(ToDecimal tde, SqlExpressionBuilderContext ctx)
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

    }
}