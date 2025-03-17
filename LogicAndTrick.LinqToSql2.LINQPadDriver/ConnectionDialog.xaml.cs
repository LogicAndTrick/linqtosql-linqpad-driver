using System;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.IO;

using LINQPad.Extensibility.DataContext;

namespace LogicAndTrick.LinqToSQL2.LINQPadDriver
{
	public partial class ConnectionDialog : Window
	{
        private readonly IConnectionInfo _cxInfo;

        public ConnectionDialog(IConnectionInfo cxInfo)
		{
            _cxInfo = cxInfo;
            DataContext = new ConnectionProperties(cxInfo);
            InitializeComponent();
            TxtPassword.Password = _cxInfo.DatabaseInfo.Password;
        }

        private void BrowseAssembly(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Choose custom assembly",
                DefaultExt = ".dll"
            };

            if (dialog.ShowDialog() == true)
            {
                _cxInfo.CustomTypeInfo.CustomAssemblyPath = dialog.FileName;
                AutoChooseType();
            }
		}

        private void AutoChooseType()
        {
            var assemPath = _cxInfo.CustomTypeInfo.CustomAssemblyPath;
            if (assemPath.Length == 0) return;

            if (!File.Exists(assemPath)) return;

            try
            {
                var customTypes = _cxInfo.CustomTypeInfo.GetCustomTypesInAssembly("System.Data.Linq.DataContext");
                if (customTypes.Length == 1) _cxInfo.CustomTypeInfo.CustomTypeName = customTypes[0];
            }
            catch
            {
                // What a shame
            }
        }

        private void ChooseType(object sender, RoutedEventArgs e)
        {
            var assemPath = _cxInfo.CustomTypeInfo.CustomAssemblyPath;
            if (assemPath.Length == 0)
            {
                MessageBox.Show("First enter a path to an assembly.");
                return;
            }

            if (!File.Exists(assemPath))
            {
                MessageBox.Show("File '" + assemPath + "' does not exist.");
                return;
            }

            string[] customTypes;
            try
            {
                customTypes = _cxInfo.CustomTypeInfo.GetCustomTypesInAssembly("System.Data.Linq.DataContext");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error obtaining custom types: " + ex.Message);
                return;
            }
            if (customTypes.Length == 0)
            {
                MessageBox.Show("There are no public types in that assembly.");
                return;
            }

            var result = (string) LINQPad.Extensibility.DataContext.UI.Dialogs.PickFromList("Choose Custom Type", customTypes.OfType<object>().ToArray());
            if (result != null) _cxInfo.CustomTypeInfo.CustomTypeName = result;
        }

        private void ClickOk(object sender, RoutedEventArgs e)
        {
            _cxInfo.DatabaseInfo.Password = TxtPassword.Password;
            DialogResult = true;
        }

        private void ClickTest(object sender, RoutedEventArgs e)
        {
            _cxInfo.DatabaseInfo.Password = TxtPassword.Password;
            try
            {
                if (SelectOne() == 1) MessageBox.Show("Connection successful.");
                else throw new Exception("Connection worked, but returned an unexpected result.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed: " + ex.Message);
            }
        }

        private int SelectOne()
        {
            using (var connection = new SqlConnection(_cxInfo.GetConnectionString()))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT 1";
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
    }
}