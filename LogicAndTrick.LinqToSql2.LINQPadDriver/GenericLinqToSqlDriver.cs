using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using LINQPad;
using LINQPad.Extensibility.DataContext;
using LogicAndTrick.LinqToSql2.LINQPadDriver;

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

        public override void InitializeContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
        {
            if (executionManager.SqlTranslationWriter == null || !ExecutionEngine.SqlTranslationsEnabled) return;
            var logger = new QueryLogger(executionManager.SqlTranslationWriter);
            try
            {
                // If we're using Albahari's fork, then we can use the more reliable CommandExecuting hook
                Action<IDbCommand> log = logger.FormatCommand;
                var currentType = context.GetType();
                while (currentType.Namespace != "System.Data.Linq") currentType = currentType.BaseType;
                currentType.Assembly.GetType("System.Data.Linq.DbEngines.SqlServer.SqlProvider")
                    .GetField("CommandExecuting", BindingFlags.Static | BindingFlags.Public)
                    .SetValue(null, log);
            }
            catch
            {
                // If that doesn't work, drop back to the default DataContext Log method
                context.GetType().GetProperty("Log").SetValue(context, logger);
            }
        }

        public override IEnumerable<string> GetAssembliesToAdd(IConnectionInfo cxInfo) => new[]
        {
            "*"
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