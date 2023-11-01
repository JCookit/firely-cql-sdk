using Hl7.Cql.Runtime;
using Hl7.Cql.Graph;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Hl7.Cql.Compiler
{
    internal class SqlGenerator
    {
        internal string GenerateSql(DirectedGraph packageGraph, ILoggerFactory logFactory)
        {
            var generatorLogger = logFactory.CreateLogger<SqlGenerator>();

            DefinitionDictionary<TSqlFragment> allFragments = CompileSql(packageGraph, logFactory, generatorLogger);

            return BuildSqlString(allFragments, generatorLogger);
        }

        private static DefinitionDictionary<TSqlFragment> CompileSql(DirectedGraph packageGraph, ILoggerFactory logFactory, ILogger<SqlGenerator> generatorLogger)
        {
            var elmLibraries = packageGraph.Nodes.Values
                .Select(node => node.Properties?[Hl7.Cql.Elm.Library.LibraryNodeProperty] as Hl7.Cql.Elm.Library)
                .Where(p => p is not null)
                .Select(p => p!)
                .ToArray();

            var builderLogger = logFactory.CreateLogger<SqlExpressionBuilder>();
            var allFragments = new DefinitionDictionary<TSqlFragment>();
            foreach (var library in elmLibraries)
            {
                generatorLogger.LogInformation($"Building expressions for {library.NameAndVersion}");

                var builder = new SqlExpressionBuilder(library, builderLogger);
                var sqlFragment = builder.Build();
                allFragments.Merge(sqlFragment);
            }

            return allFragments;
        }

        private string BuildSqlString(DefinitionDictionary<TSqlFragment> all, ILogger<SqlGenerator> generatorLogger)
        {
            throw new NotImplementedException();
        }
    }
}
