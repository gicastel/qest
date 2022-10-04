using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
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
        private List<(string Message, bool? IsError)>? Report { get; set; }

        public bool Run()
        {
            Report = new();

            ReportAdd($"-----------------------------------------------------------------------------------");
            ReportAdd($"Running Test: {this.Name}");

            if (Connection == null)
            {
                ReportAdd($"FATAL: SQL Connection null", true);
                return false;
            }

            if (Steps == null)
            {
                ReportAdd($"FATAL: TestCases null", true);
                return false;
            }

            try
            {
                Connection.Open();

                Before?.Run(this, "Before");

                foreach (var testCase in Steps)
                {
                    RunTestStep(this, testCase);
                }
            }
            catch (Exception ex)
            {
                ReportAdd($"FATAL: {ex}", true);
            }
            finally
            {
                After?.Run(this, "After");
                Connection.Close();
            }

            var isPass = !Report.Where(m => m.IsError.HasValue && m.IsError.Value).Any();

            ReportAdd($"Test {Name}: {(isPass ? "OK" : "KO")}", !isPass);

            return isPass;
        }

        private void RunTestStep(Test parentTest, TestStep step)
        {
            ReportAdd($"Running Test {parentTest.Name} - Step: {step.Name}");
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
                        ReportAdd($"Checking ResultSet: {expected.Name}");
                        var currRes = dataTables.Where(d => d.TableName == expected.Name).FirstOrDefault();

                        if (currRes == null)
                        {
                            ReportAdd($"Resultset {expected.Name} not found", true);
                            continue;
                        }

                        if (expected.RowNumber != null)
                        {
                            if (expected.RowNumber != currRes.Rows.Count)
                            {
                                ReportAdd($"ResultSet {expected.Name} row number: {currRes.Rows.Count} != {expected.RowNumber}", true);
                                continue;
                            }
                        }

                        ReportAdd($"Result {expected.Name}: OK", false);
                    }
                }

                reader.Close();

                //output parameters
                if (step.Results.OutputParameters != null)
                {
                    foreach (var expected in step.Results.OutputParameters)
                    {
                        ReportAdd($"Checking Output Parameter: {expected.Name}");
                        var currRes = outPars.Where(p => p.ParameterName == $"@{expected.Name}").FirstOrDefault();
                        if (currRes == null)
                        {
                            ReportAdd($"Output Parameter {expected.Name} not found", true);
                            continue;
                        }

                        if (expected.Value != null)
                        {
                            if (!currRes.Value.Equals(currRes.Value))
                            {
                                ReportAdd($"Ouput Parameter {expected.Name}: {currRes.Value} != {expected.Value}", true);
                                continue;
                            }
                        }

                        ReportAdd($"Result {expected.Name}: {expected.Value} == {currRes.Value}", false);
                    }
                }

                //returncode
                if (step.Results.ReturnCode.HasValue)
                {
                    var expected = step.Results.ReturnCode.Value;
                    ReportAdd($"Checking Return Code");
                    var rcPar = outPars.Where(p => p.ParameterName == $"@rc").FirstOrDefault();
                    if (rcPar == null)
                    {
                        ReportAdd($"Return Code not found", true);
                    }
                    else
                    {
                        if (expected != Convert.ToInt32(rcPar.Value))
                            ReportAdd($"Return Code: {rcPar.Value} != {expected}", true);
                        else
                            ReportAdd($"Return Code: {expected} == {expected}", false);
                    }
                }
            }

            // asserts
            if (step.Asserts != null)
            {
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
                            ReportAdd($"Assert {assertSqlQuery}: {result} == {assertScalarValue}", false);
                        else
                            ReportAdd($"Assert {assertSqlQuery}: {result} != {assertScalarValue}", true);
                    }
                    else
                        ReportAdd($"Assert {assertSqlQuery}: Result NULL", true);
                }
            }
        }

        internal void ReportAdd(string message, bool? isError = null)
        {
            message = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " " + message;
            Report.Add((message, isError));

            if (isError.HasValue)
                Console.ForegroundColor = isError.Value ? ConsoleColor.Red : ConsoleColor.Green;

            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static SqlDbType MapType(string type) => Utils.MapSqlType(type);
    }
}
