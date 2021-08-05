using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LINQPad;
using DateTime = System.DateTime;

namespace LogicAndTrick.LinqToSql2.LINQPadDriver
{
    public class QueryLogger : TextWriter
    {
        private readonly TextWriter _wrapped;
        private CleverCommand _cleverCommand;

        public QueryLogger(TextWriter wrapped)
        {
            _wrapped = wrapped;
        }

        public override void Write(char[] buffer, int index, int count) => CleverWrite(new string(buffer, index, count));
        public override void Write(string value) => CleverWrite(value);
        public override void WriteLine(string value) => CleverWrite(value + Environment.NewLine);
        public override void WriteLine(string format, params object[] arg) => CleverWrite(String.Format(format, arg) + Environment.NewLine);
        public override void WriteLine() => CleverWrite(Environment.NewLine);

        private void CleverWrite(string text)
        {
            var frame = new StackFrame(2);
            if (frame.GetMethod()?.Name == "LogCommand")
            {
                // We are coming from the LogCommand method, which has as well defined format, use the clever command
                if (_cleverCommand == null) _cleverCommand = new CleverCommand();
                _cleverCommand.Append(text);
                if (_cleverCommand.IsComplete)
                {
                    _wrapped.Write(_cleverCommand.ToString());
                    _cleverCommand = null;
                }
            }
            else
            {
                if (_cleverCommand != null)
                {
                    _wrapped.Write(_cleverCommand.ToString());
                    _cleverCommand = null;
                }

                _wrapped.Write(text);
            }
        }

        public override Encoding Encoding => _wrapped.Encoding;

        public void FormatCommand(IDbCommand cmd)
        {
#if NETCORE
            ExecutionEngine.CurrentlyExecutingCommand = cmd;
#endif
            if (!ExecutionEngine.SqlTranslationsEnabled) return;

            var sb = new StringBuilder();

            if (cmd.Parameters.Count > 0)
            {
                sb.AppendLine("-- Region Parameters");
                foreach (DbParameter parameter in cmd.Parameters) sb.AppendLine(FormatParameter(parameter));
                sb.AppendLine("-- EndRegion");
            }

            if (cmd.CommandType == CommandType.Text)
            {
                sb.AppendLine(cmd.CommandText);
            }
            else if (cmd.CommandType == CommandType.TableDirect)
            {
                sb.AppendLine($"SELECT * FROM {cmd.CommandText}");
            }
            else if (cmd.CommandType == CommandType.StoredProcedure)
            {
                sb.Append("EXEC ");
                var parms = cmd.Parameters.OfType<DbParameter>().ToList();
                var retPar = parms.FirstOrDefault(p => p.Direction == ParameterDirection.ReturnValue);
                if (retPar != null)
                {
                    sb.Append($"{retPar.ParameterName} = ");
                    parms.Remove(retPar);
                }
                sb.Append(cmd.CommandText);
                var parNames = parms.Select(x => $"{x.ParameterName} = {x.ParameterName}");
                sb.Append(String.Join(", ", parNames));
                sb.AppendLine();
            }

            sb.AppendLine("GO").AppendLine();
            _wrapped.Write(sb.ToString());
        }

        private static string FormatParameter(DbParameter parameter)
        {
            var par = parameter as SqlParameter;
            if (par == null) return $"DECLARE {parameter.ParameterName} {parameter.DbType} = {parameter.Value}";

            var formatType = par.SqlDbType.ToString().ToUpper();
            string formatValue;
            if (par.SqlValue != null && par.SqlValue != DBNull.Value)
            {
                switch (par.SqlDbType)
                {
                    case SqlDbType.Binary:
                    case SqlDbType.VarBinary:
                    case SqlDbType.Image:
                        formatValue = "NULL -- Binary data not included";
                        break;
                    case SqlDbType.Char:
                    case SqlDbType.NChar:
                    case SqlDbType.VarChar:
                    case SqlDbType.NVarChar:
                    case SqlDbType.Text:
                    case SqlDbType.NText:
                    case SqlDbType.UniqueIdentifier:
                    case SqlDbType.Xml:
                        formatValue = "'" + Convert.ToString(par.SqlValue).Replace("'", "''") + "'";
                        break;
                    case SqlDbType.Date:
                    case SqlDbType.DateTime:
                    case SqlDbType.DateTime2:
                    case SqlDbType.DateTimeOffset:
                    case SqlDbType.SmallDateTime:
                    case SqlDbType.Time:
                        switch (par.SqlValue)
                        {
                            case DateTime dt:
                                formatValue = "'" + dt.ToString(par.SqlDbType == SqlDbType.Date ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss.fff") + "'";
                                break;
                            case DateTimeOffset dto:
                                formatValue = "'" + dto.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
                                break;
                            case SqlDateTime sdt:
                                formatValue = "'" + sdt.Value.ToString(par.SqlDbType == SqlDbType.Date ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss.fff") + "'";
                                break;
                            default:
                                formatValue = "NULL";
                                break;
                        }

                        break;
                    default:
                        formatValue = Convert.ToString(par.SqlValue, CultureInfo.InvariantCulture);
                        break;
                }
            }
            else
            {
                formatValue = "NULL";
            }

            return $"DECLARE {par.ParameterName} {formatType} = {formatValue}";
        }

        private class CleverCommand
        {
            // LogCommand format:
            // 1. It prints the SQL statement first in a single WriteLine
            // 2. Then it prints each parameter in this format:
            //    -- {0}: {1} {2} (Size = {3}; Prec = {4}; Scale = {5}) [{6}]
            //    With these parameter arguments:
            //    name, direction, [sql] dbtype, size, precision, scale, [sql] value
            // 3. Finally it prints some information in this format:
            //    "-- Context: {0}({1}) Model: {2} Build: {3}"
            //    With these arguments:
            //    provider type name, sql mode, model type name, build version number
            // A full example looks like this:
            // SELECT ... FROM ... WHERE ID > @p0
            // -- @p0: Input Int (Size = -1; Prec = 0; Scale = 0) [99]
            // -- Context: SqlProvider(Sql2008) Model: AttributedMetaModel Build: 1.0 (placeholder)

            private static readonly Regex MatchParam = new Regex(@"^-- (.*?): ([a-z]*) (.*?) \(Size = (.*?); Prec = (.*?); Scale = (.*?)\) \[(.*?)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private static readonly Regex MatchContext = new Regex(@"-- Context: [a-z0-9]*\([a-z0-9]*\) Model: [a-z0-9]* Build: .*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private string _commandText;
            private readonly List<string> _parameters;
            public bool IsComplete { get; private set; }

            public CleverCommand()
            {
                _commandText = "";
                _parameters = new List<string>();
                IsComplete = false;
            }

            public void Append(string text)
            {
                var par = MatchParam.Match(text);
                var ctx = MatchContext.Match(text);
                if (par.Success)
                {
                    var name = par.Groups[1].Value;
                    // var direction = par.Groups[2].Value;
                    var type = par.Groups[3].Value.ToUpper();
                    var size = int.Parse(par.Groups[4].Value);
                    var prec = int.Parse(par.Groups[5].Value);
                    var scal = int.Parse(par.Groups[6].Value);
                    var value = par.Groups[7].Value;
                    _parameters.Add(FormatParameter(name, type, size, prec, scal, value));
                }
                else if (ctx.Success)
                {
                    IsComplete = true;
                }
                else
                {
                    _commandText += text;
                }
            }

            private static string FormatParameter(string name, string type, int size, int precision, int scale, string value)
            {
                var formatType = type;
                if (type == "VARCHAR" || type == "NVARCHAR" || type == "VARBINARY") formatType += size < 0 ? "(MAX)" : "(" + size + ")";
                else if (precision > 0 && scale >= 0) formatType += $"({precision}, {scale})";

                var formatValue = value;
                switch (type)
                {
                    case "VARCHAR":
                    case "NVARCHAR":
                    case "TEXT":
                    case "NTEXT":
                    case "CHAR":
                    case "NCHAR":
                    case "XML":
                    case "UNIQUEIDENTIFIER":
                        formatValue = "'" + value.Replace("'", "''") + "'";
                        break;
                    case "DATE":
                    case "DATETIME":
                    case "SMALLDATETIME":
                    case "DATETIME2":
                        if (DateTime.TryParse(value, out var dt)) formatValue = "'" + dt.ToString(type == "DATE" ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss.fff") + "'";
                        else formatValue = "'" + value.Replace("'", "''") + "'";
                        break;
                    case "DATETIMEOFFSET":
                        if (DateTime.TryParse(value, out var dateTimeOffset)) formatValue = "'" + dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
                        else formatValue = "'" + value.Replace("'", "''") + "'";
                        break;
                    case "BINARY":
                    case "VARBINARY":
                        formatValue = "NULL -- Binary data not included";
                        break;
                }

                return $"DECLARE {name} {formatType} = {formatValue}";
            }

            public override string ToString()
            {
                var text = new StringBuilder();
                if (_parameters.Any())
                {
                    text.AppendLine("-- Region Parameters");
                    foreach (var parameter in _parameters) text.AppendLine(parameter);
                    text.AppendLine("-- EndRegion");
                }
                text.AppendLine(_commandText.Trim());
                text.AppendLine("GO").AppendLine();
                return text.ToString();
            }
        }
    }
}
