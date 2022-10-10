using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using YamlDotNet.Serialization;

namespace qest.Models
{
    public class Test
    {
        public string Name { get; set; }
        public Scripts? Before { get; set; }
        [YamlIgnore]
        public SqlConnection? Connection { get; set; }
        public List<TestStep> Steps { get; set; }
        public Scripts? After { get; set; }
        public Dictionary<string, object>? Variables { get; set; }
        [YamlIgnore]
        private bool Pass;

        public async Task<bool> RunAsync()
        {
            Logger($"Running Test: {this.Name}", customStyle: "bold,blue");
            Pass = true;

            if (Connection == null)
            {
                Logger($"FATAL: SQL Connection null", true, "bold");
                return false;
            }

            if (Steps == null)
            {
                Logger($"FATAL: TestCases null", true, "bold");
                return false;
            }

            try
            {
                await Connection.OpenAsync();

                await Before?.RunAsync(this, "Before");

                foreach (var testCase in Steps)
                {
                    RunTestStep(this, testCase);
                }
            }
            catch (Exception ex)
            {
                Logger($"FATAL: {ex}", true);
            }
            finally
            {
                await After?.RunAsync(this, "After");
                await Connection.CloseAsync();
            }

            Logger($"Test {Name}: {(Pass ? "OK" : "KO")}", !Pass, "bold");

            return Pass;
        }

        private void RunTestStep(Test parentTest, TestStep step)
        {
            Logger($"Running Test {parentTest.Name} - Step: {step.Name}", customStyle: "blue");
            SqlCommand testCmd = new(step.Command.CommandText, Connection);
            testCmd.CommandType = CommandType.StoredProcedure;

            // add input pars
            if (step.Command.Parameters != null)
                foreach (var input in step.Command.Parameters)
                {
                    testCmd.Parameters.AddWithValue($"@{input.Key}", input.Value?.ReplaceVarsInParameter(Variables));
                }

            // add output pars
            List<SqlParameter> outPars = new();
            if (step.Results != null)
            {
                if (step.Results.OutputParameters != null)
                    foreach (var output in step.Results.OutputParameters)
                    {
                        var outPar = testCmd.Parameters.Add($"@{output.Name}", output.Type);
                        outPar.Direction = ParameterDirection.Output;
                        outPars.Add(outPar);
                    }

                // add rc if exists
                if (step.Results.ReturnCode.HasValue)
                {
                    var rc = step.Results.ReturnCode;
                    var outPar = testCmd.Parameters.Add($"@rc", SqlDbType.Int);
                    outPar.Direction = ParameterDirection.ReturnValue;
                    outPars.Add(outPar);
                }
            }

            using var reader = testCmd.ExecuteReader();

            if (step.Results != null)
            {
                // resultsets
                if (step.Results.ResultSets != null)
                {
                    var resultSets = step.Results.ResultSets;

                    List<DataTable> dataTables = new();

                    foreach (var resultSet in resultSets)
                    {
                        Logger($"Loading ResultSet: {resultSet.Name}");
                        DataTable dataTable = resultSet.GetDataTable();

                        while (reader.Read())
                        {
                            DataRow row = dataTable.NewRow();

                            for (int f = 0; f < resultSet.Columns.Count; f++)
                            {
                                var column = resultSet.Columns[f];

                                if (reader.IsDBNull(f))
                                    row[column.Name] = DBNull.Value;
                                else
                                    row[column.Name] = Convert.ChangeType(reader.GetValue(f), Utils.MapType(column.Type));
                            }
                            dataTable.Rows.Add(row);
                        }

                        dataTables.Add(dataTable);
                        reader.NextResult();
                    }

                    foreach (var expected in step.Results.ResultSets)
                    {
                        Logger($"Checking ResultSet: {expected.Name}");
                        var currRes = dataTables.Where(d => d.TableName == expected.Name).FirstOrDefault();

                        if (currRes == null)
                        {
                            Logger($"Resultset {expected.Name} not found", true);
                            continue;
                        }

                        if (expected.RowNumber != null)
                        {
                            if (expected.RowNumber != currRes.Rows.Count)
                            {
                                Logger($"ResultSet {expected.Name} row number: {currRes.Rows.Count} != {expected.RowNumber}", true);
                                continue;
                            }
                        }

                        Logger($"Result {expected.Name}: OK", false);
                    }
                }

                reader.Close();

                //output parameters
                if (step.Results.OutputParameters != null)
                {
                    foreach (var expected in step.Results.OutputParameters)
                    {
                        Logger($"Checking Output Parameter: {expected.Name}");
                        var currRes = outPars.Where(p => p.ParameterName == $"@{expected.Name}").FirstOrDefault();
                        if (currRes == null)
                        {
                            Logger($"Output Parameter {expected.Name} not found", true);
                            continue;
                        }

                        if (expected.Value != null)
                        {
                            if (!currRes.Value.Equals(currRes.Value))
                            {
                                Logger($"Ouput Parameter {expected.Name}: {currRes.Value} != {expected.Value}", true);
                                continue;
                            }
                        }

                        Logger($"Result {expected.Name}: {expected.Value} == {currRes.Value}", false);
                    }
                }

                //returncode
                if (step.Results.ReturnCode.HasValue)
                {
                    var expected = step.Results.ReturnCode.Value;
                    Logger($"Checking Return Code");
                    var rcPar = outPars.Where(p => p.ParameterName == $"@rc").FirstOrDefault();
                    if (rcPar == null)
                    {
                        Logger($"Return Code not found", true);
                    }
                    else
                    {
                        if (expected != Convert.ToInt32(rcPar.Value))
                            Logger($"Return Code: {rcPar.Value} != {expected}", true);
                        else
                            Logger($"Return Code: {expected} == {expected}", false);
                    }
                }
            }

            // asserts
            if (step.Asserts != null)
            {
                Logger($"Checking Asserts");
                foreach (var assert in step.Asserts)
                {
                    var assertSqlQuery = assert.SqlQuery.ReplaceVars(Variables);
                    var assertScalarValue = assert.ScalarValue.ReplaceVarsInParameter(Variables);
                    SqlCommand cmd = new(assertSqlQuery, Connection);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        bool pass = false;
                        var scalarType = Utils.MapType(assert.ScalarType);
                        pass = Convert.ChangeType(assertScalarValue, scalarType).Equals(Convert.ChangeType(result, scalarType));

                        if (pass)
                            Logger($"{assertSqlQuery}: {result} == {assertScalarValue}", false);
                        else
                            Logger($"{assertSqlQuery}: {result} != {assertScalarValue}", true);
                    }
                    else
                        Logger($"{assertSqlQuery}: Result NULL", true);
                }
            }
        }

        internal void Logger(string message, bool? isError = null, string? customStyle = null)
        {
            string pfx = $"[grey]{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}[/]";

            message = message.EscapeMarkup();

            if (customStyle is not null)
                foreach (string style in customStyle.Split(','))
                    message = $"[{style}]{message}[/]";

            if (isError.HasValue)
            {
                Pass = (Pass && !isError.Value);
                if (isError.Value)
                    message = $"{pfx} [red]{message}[/]";
                else
                    message = $"{pfx} [green]{message}[/]";
            }
            else
                message = $"{pfx} {message}";

            AnsiConsole.MarkupLine(message);
        }
    }
}
