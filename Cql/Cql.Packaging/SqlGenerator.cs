using Hl7.Cql.Runtime;
using Hl7.Cql.Graph;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using static System.Net.WebRequestMethods;

namespace Hl7.Cql.Compiler
{
    internal class SqlGenerator
    {
        internal string GenerateSql(
            DirectedGraph packageGraph, 
            TypeManager typeManager, 
            ILoggerFactory logFactory)
        {
            var generatorLogger = logFactory.CreateLogger<SqlGenerator>();

            DefinitionDictionary<SqlExpression> allFragments = CompileSql(packageGraph, typeManager, logFactory);

            return BuildSqlString(allFragments, generatorLogger);
        }

        private static DefinitionDictionary<SqlExpression> CompileSql(
            DirectedGraph packageGraph, 
            TypeManager typeManager, 
            ILoggerFactory logFactory)
        {
            var elmLibraries = packageGraph.Nodes.Values
                .Select(node => node.Properties?[Hl7.Cql.Elm.Library.LibraryNodeProperty] as Hl7.Cql.Elm.Library)
                .Where(p => p is not null)
                .Select(p => p!)
                .ToArray();

            var builderLogger = logFactory.CreateLogger<SqlExpressionBuilder>();
            var allFragments = new DefinitionDictionary<SqlExpression>();
            foreach (var library in elmLibraries)
            {
                // TODO: cheat and bypass FHIRHelpers 'cause it's not going to work yet anyway
                if (StringComparer.InvariantCultureIgnoreCase.Compare(library.NameAndVersion, "FHIRHelpers-4.0.001") == 0)
                    continue;

                builderLogger.LogInformation($"Building expressions for {library.NameAndVersion}");

                var builder = new SqlExpressionBuilder(library, typeManager, builderLogger);
                var sqlFragment = builder.Build();
                allFragments.Merge(sqlFragment);
            }

            return allFragments;
        }

        private string BuildSqlString(DefinitionDictionary<SqlExpression> all, ILogger<SqlGenerator> generatorLogger)
        {
            var generator = new SqlServerlessScriptGenerator();

            var writer = new StringWriter();

            // call the generator for each fragment, and insert GO inbetween
            // TODO:  is order going to be an issue?   i think so; with functions that depend on each other --- leverage the graph
            foreach (var library in all.Libraries)
            {
                foreach (var define in all.DefinitionsForLibrary(library))
                {
                    generatorLogger.LogInformation($"Generating SQL for {define.Key}");

                    // TODO:  what does this mean when there is more than one overload?
                    foreach (var fragment in define.Value)
                    {
                        string normalizedName = ExpressionBuilderContext.NormalizeIdentifier(define.Key) ?? throw new InvalidOperationException();

                        writer.WriteLine("--");
                        writer.WriteLine($"-- start {normalizedName}");
                        writer.WriteLine("--");
                        generator.GenerateScript(BuildDropFunction(normalizedName), writer);
                        writer.WriteLine();
                        writer.WriteLine("GO");

                        // should only get here with Select statements
                        if (fragment.Item2.IsSelectStatement)
                        {
                            generator.GenerateScript(WrapWithFunction(normalizedName, fragment.Item2.SqlFragment), writer);
                            writer.WriteLine("GO");
                        }
                        else
                            throw new InvalidOperationException();
                    }
                }
            }

            //generator.GenerateScript(BuildDropFunction("TestFunction"), writer);
            //writer.WriteLine();
            //writer.WriteLine("GO");
            //generator.GenerateScript(BuildTestData("TestFunction"), writer);
            //writer.WriteLine();
            //writer.WriteLine("GO");

            return writer.ToString();
        }

        private TSqlFragment BuildDropFunction(string functionName)
        {
            var dropFunction = new DropFunctionStatement
            {
                Objects = 
                {
                    new SchemaObjectName
                    {
                        Identifiers =
                        {
                            new Identifier { Value = functionName }
                        }
                    }
                }
            };

            return dropFunction;
        }


        private TSqlFragment BuildTestData(string functionName)
        {
            var select = new SelectStatement
            {
                QueryExpression = new QueryParenthesisExpression
                {
                    QueryExpression = new QuerySpecification
                    {
                        SelectElements =
                        {
                            new SelectScalarExpression
                            {
                                Expression = new BinaryExpression
                                {
                                    BinaryExpressionType = BinaryExpressionType.Add,
                                    FirstExpression = new IntegerLiteral { Value = "1" },
                                    SecondExpression = new IntegerLiteral { Value = "1" }
                                },
                                ColumnName = new IdentifierOrValueExpression
                                {
                                    Identifier = new Identifier { Value = "Result" }
                                }
                            },
                        },
                        FromClause = new FromClause
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
                                                    Identifier = new Identifier { Value = "unused_column" }
                                                }
                                            }
                                        }
                                    },
                                    Alias = new Identifier { Value = "UNUSED" }
                                }
                            }
                        }
                    }
                }
            };

            CreateFunctionStatement function = WrapWithFunction(functionName, select);

            return function;
        }

        private static CreateFunctionStatement WrapWithFunction(string functionName, TSqlFragment selectStatement)
        {
            return new CreateFunctionStatement
            {
                Name = new SchemaObjectName
                {
                    Identifiers =
                    {
                        new Identifier { Value = functionName }
                    }
                },
                ReturnType = new SelectFunctionReturnType
                {
                    SelectStatement = selectStatement as SelectStatement ?? throw new ArgumentException("Must be a select statement", nameof(selectStatement))
                }
            };
        }
    }
}
