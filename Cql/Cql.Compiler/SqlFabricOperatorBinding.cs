using Hl7.Cql.Abstractions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hl7.Cql.Compiler
{
    internal abstract class SQLFabricOperatorBinding : IOperatorBinding<TSqlFragment>
    {
        public TSqlFragment Bind(CqlOperator @operator, TSqlFragment context, params TSqlFragment[] parameters)
        {
            throw new System.NotImplementedException();
        }
    }
}
