using System;
using Microsoft.Data.SqlClient;
using LINQPad.Extensibility.DataContext;

namespace LogicAndTrick.LinqToSQL2.LINQPadDriver
{
    public static class Extensions
    {
        public static string GetConnectionString(this IConnectionInfo conn)
        {
            var builder = new SqlConnectionStringBuilder(conn.DatabaseInfo.CustomCxString)
            {
                ApplicationName = "LINQPad"
            };
            if (string.IsNullOrWhiteSpace(conn.DatabaseInfo.CustomCxString))
            {
                // upgrade old version data
                builder.DataSource = conn.DatabaseInfo.Server;
                builder.InitialCatalog = conn.DatabaseInfo.Database;
                if (string.IsNullOrWhiteSpace(conn.DatabaseInfo.UserName))
                {
                    builder.IntegratedSecurity = true;
                    builder.Authentication = SqlAuthenticationMethod.NotSpecified;
                }
                else
                {
                    builder.IntegratedSecurity = false;
                    builder.Authentication = SqlAuthenticationMethod.SqlPassword;
                    builder.UserID = conn.DatabaseInfo.UserName;
                    builder.Password = conn.DatabaseInfo.Password;
                }
                builder.TrustServerCertificate = true;
                builder.Encrypt = true;
            }
            return builder.ConnectionString;
        }
    }
}
