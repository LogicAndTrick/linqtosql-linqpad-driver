using LINQPad.Extensibility.DataContext;
using System.Xml.Linq;

namespace LogicAndTrick.LinqToSQL2.LINQPadDriver
{
	class ConnectionProperties
	{
		public IConnectionInfo ConnectionInfo { get; private set; }

		XElement DriverData => ConnectionInfo.DriverData;

		public ConnectionProperties (IConnectionInfo cxInfo)
		{
			ConnectionInfo = cxInfo;
            cxInfo.DatabaseInfo.Provider = "System.Data.SqlClient";
        }
	}
}