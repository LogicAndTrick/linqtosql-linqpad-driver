using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using LINQPad;
using LINQPad.Extensibility.DataContext;

namespace LogicAndTrick.LinqToSQL2.LINQPadDriver
{
    public class GenericLinqToSqlDriver : StaticDataContextDriver
    {
        public override string Name => "LINQ to SQL (or compatible) for SQL Server";
        public override string Author => "Logic & Trick";

        public override string GetConnectionDescription(IConnectionInfo cxInfo)
        {
            return $"{cxInfo.DatabaseInfo.Server}.{cxInfo.DatabaseInfo.Database}";
        }

        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
        {
            return new ConnectionDialog(cxInfo).ShowDialog() == true;
        }

        public override ParameterDescriptor[] GetContextConstructorParameters(IConnectionInfo r) => new[]
        {
            new ParameterDescriptor("connection", "System.Data.IDbConnection")
        };

        public override object[] GetContextConstructorArguments(IConnectionInfo cxInfo)
        {
            return new object[]
            {
                GetIDbConnection(cxInfo)
            };
        }

        public override IEnumerable<string> GetAssembliesToAdd(IConnectionInfo cxInfo) => new[]
        {
            "SD.Tools.LinqToSQL2.dll",
            "System.Data.SqlClient.dll",
            "Microsoft.SqlServer.Types.dll"
        };

        public override IEnumerable<string> GetNamespacesToAdd(IConnectionInfo cxInfo) => new string[3]
        {
            "System.Data.Linq",
            "System.Data.Linq.SqlClient",
            "System.Data.SqlClient"
        };

        public override IDbConnection GetIDbConnection(IConnectionInfo cxInfo)
        {
            return new SqlConnection(cxInfo.DatabaseInfo.GetCxString());
        }

        public override List<ExplorerItem> GetSchema(IConnectionInfo cxInfo, Type customType)
        {
            return new ExplorerItem[0].ToList();
        }

        public override ICustomMemberProvider GetCustomDisplayMemberProvider(object objectToWrite)
        {
            if (objectToWrite == null) return null;
            if (EntityMemberProvider.AppliesTo(objectToWrite)) return new EntityMemberProvider(objectToWrite);
            return null;
        }

        public override void TearDownContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager, object[] constructorArguments)
        {
            try
            {
                using (var idbConnection = (SqlConnection)GetIDbConnection(cxInfo)) SqlConnection.ClearPool(idbConnection);
            }
            catch
            {
                //
            }
        }

        public override void ClearConnectionPools(IConnectionInfo c)
        {
            try
            {
                using (var idbConnection = (SqlConnection)GetIDbConnection(c)) SqlConnection.ClearPool(idbConnection);
            }
            catch
            {
                //
            }
        }
    }
}