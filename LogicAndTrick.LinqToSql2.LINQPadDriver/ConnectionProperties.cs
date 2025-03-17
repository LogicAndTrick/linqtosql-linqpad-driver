using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using LINQPad.Extensibility.DataContext;
using Microsoft.Data.SqlClient;

namespace LogicAndTrick.LinqToSQL2.LINQPadDriver
{
	public class ConnectionProperties : INotifyPropertyChanged
	{
        private readonly SqlConnectionStringBuilder _builder;
        private readonly IConnectionInfo _cxInfo;

        public ICustomTypeInfo CustomTypeInfo => _cxInfo.CustomTypeInfo;

        public bool IsProduction
        {
            get => _cxInfo.IsProduction;
            set
            {
                if (value == _cxInfo.IsProduction) return;
                _cxInfo.IsProduction = value;
                OnPropertyChanged();
            }
        }

        public bool Persist
        {
            get => _cxInfo.Persist;
            set
            {
                if (value == _cxInfo.Persist) return;
                _cxInfo.Persist = value;
                OnPropertyChanged();
            }
        }

        public string DisplayName
        {
            get => _cxInfo.DisplayName;
            set
            {
                if (value == _cxInfo.DisplayName) return;
                _cxInfo.DisplayName = value;
                OnPropertyChanged();
            }
        }

        public string Server
        {
            get => _builder.DataSource;
            set
            {
                if (value == _builder.DataSource) return;
                _builder.DataSource = value;
                OnPropertyChanged();
            }
        }

        public string Database
        {
            get => _builder.InitialCatalog;
            set
            {
                if (value == _builder.InitialCatalog) return;
                _builder.InitialCatalog = value;
                OnPropertyChanged();
            }
        }

        private static readonly List<string> AuthMethodNames = new List<string>()
        {
            "Windows",
            SqlAuthenticationMethod.SqlPassword.ToString(),
            SqlAuthenticationMethod.ActiveDirectoryPassword.ToString(),
            SqlAuthenticationMethod.ActiveDirectoryIntegrated.ToString(),
            SqlAuthenticationMethod.ActiveDirectoryInteractive.ToString(),
        };

        public List<string> AuthNames => AuthMethodNames;

        public bool DisplayUsername => _builder.Authentication == SqlAuthenticationMethod.SqlPassword || _builder.Authentication == SqlAuthenticationMethod.ActiveDirectoryPassword || _builder.Authentication == SqlAuthenticationMethod.ActiveDirectoryInteractive;
        public bool DisplayPassword => _builder.Authentication == SqlAuthenticationMethod.SqlPassword || _builder.Authentication == SqlAuthenticationMethod.ActiveDirectoryPassword;

        public string AuthenticationMethod
        {
            get => _builder.IntegratedSecurity ? "Windows" : _builder.Authentication.ToString();
            set
            {
                if (value == AuthenticationMethod) return;
                if (value == "Windows")
                {
                    _builder.IntegratedSecurity = true;
                    _builder.Authentication = SqlAuthenticationMethod.NotSpecified;
                }
                else
                {
                    _builder.IntegratedSecurity = false;
                    _builder.Authentication = (SqlAuthenticationMethod) System.Enum.Parse(typeof(SqlAuthenticationMethod), value);
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayUsername));
                OnPropertyChanged(nameof(DisplayPassword));
            }
        }

        public string UserName
        {
            get => _builder.UserID;
            set
            {
                if (value == _builder.UserID) return;
                _builder.UserID = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _cxInfo.DatabaseInfo.Password;
            set
            {
                if (value == _cxInfo.DatabaseInfo.Password) return;
                _cxInfo.DatabaseInfo.Password = value;
                OnPropertyChanged();
            }
        }

        public bool Trust
        {
            get => _builder.TrustServerCertificate;
            set
            {
                if (value == _builder.TrustServerCertificate) return;
                _builder.TrustServerCertificate = value;
                OnPropertyChanged();
            }
        }

        public bool Encrypt
        {
            get => _builder.Encrypt;
            set
            {
                if (value == _builder.Encrypt) return;
                _builder.Encrypt = value;
                OnPropertyChanged();
            }
        }

        public string PreviewConnectionString
        {
            get
            {
                var b = new SqlConnectionStringBuilder(_builder.ConnectionString);
                if (DisplayPassword) b.Password = "*****";
                return b.ConnectionString;
            }
        }

        public ConnectionProperties(IConnectionInfo cxInfo)
        {
            cxInfo.DatabaseInfo.Provider = "Microsoft.Data.SqlClient";

            var connStr = cxInfo.DatabaseInfo.CustomCxString;
            _builder = new SqlConnectionStringBuilder(connStr);

            if (!string.IsNullOrWhiteSpace(cxInfo.DatabaseInfo.Server))
            {
                // upgrade old version data
                _builder.DataSource = cxInfo.DatabaseInfo.Server;
                _builder.InitialCatalog = cxInfo.DatabaseInfo.Database;
                if (string.IsNullOrWhiteSpace(cxInfo.DatabaseInfo.UserName))
                {
                    _builder.IntegratedSecurity = true;
                    _builder.Authentication = SqlAuthenticationMethod.NotSpecified;
                }
                else
                {
                    _builder.IntegratedSecurity = false;
                    _builder.Authentication = SqlAuthenticationMethod.SqlPassword;
                    _builder.UserID = cxInfo.DatabaseInfo.UserName;
                    _builder.Password = cxInfo.DatabaseInfo.Password;
                }
            }

            if (string.IsNullOrWhiteSpace(connStr))
            {
                // set defaults
                _builder.IntegratedSecurity = true;
                _builder.TrustServerCertificate = true;
                _builder.Encrypt = true;
            }

            _cxInfo = cxInfo;
            _cxInfo.DriverData.SetElementValue("Version", 2);

            PropertyChanged += (sender, e) => CommitChanges();
            CommitChanges();
        }

        private void CommitChanges()
        {
            _cxInfo.DatabaseInfo.CustomCxString = _builder.ConnectionString;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreviewConnectionString)));
        }
    }
}