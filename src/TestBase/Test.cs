using System.Data;
using System.Data.SqlClient;

namespace TestBase
{
    public class Test
    {
        public string Name { get; set; }
        public List<Script>? Before { get; set; }
        public SqlConnection? Connection { get; set; }
        public TestCommand Command { get; set; }
        public ResultGroup Results { get; set; }
        public List<Assert>? Asserts { get; set; }
        public List<Script>? After { get; set; }

        private bool IsPass { get; set; }
        private List<(string Message, bool? IsError)>? Report { get; set; }

        public bool Run()
        {
            Report = new();
            IsPass = true;

            ReportAdd($"-----------------------------------------------------------------------------------");
            ReportAdd($"Running Test: {this.Name}");

            if (Connection == null)
            {
                ReportAdd($"FATAL: SQL Connection null", true);
                return false;
            }

            if (Command == null)
            {
                ReportAdd($"FATAL: Test Command null", true);
                return false;
            }

            try
            {
                Connection.Open();

                // data prep
                if (Before != null)
                {
                    SqlTransaction loadData = Connection.BeginTransaction();
                    try
                    {
                        foreach (var item in Before)
                        {
                            string script = item.Compact();
                            if (script.Length > 0)
                            {
                                ReportAdd("Running Before scripts...");
                                var cmd = new SqlCommand(script, Connection, loadData);
                                cmd.ExecuteNonQuery();
                                ReportAdd("Completed.", false);
                            }
                        }
                        loadData.Commit();
                    }
                    catch
                    {
                        loadData.Rollback();
                        throw;
                    }
                }

                SqlCommand testCmd = new(Command.CommandText, Connection);
                testCmd.CommandType = CommandType.StoredProcedure;

                // add input pars
                if (Command.Parameters != null)
                    foreach (var input in Command.Parameters)
                    {
                        testCmd.Parameters.AddWithValue($"@{input.Name}", input.Value);
                    }

                // add output pars
                List<SqlParameter> outPars = new();
                if (Results != null)
                {
                    if (Results.OutputParameters != null)
                        foreach (var output in Results.OutputParameters)
                        {
                            var outPar = testCmd.Parameters.Add($"@{output.Name}", output.Type);
                            outPar.Direction = ParameterDirection.Output;
                            outPars.Add(outPar);
                        }

                    // add rc if exists
                    if (Results.ReturnCode.HasValue)
                    {
                        var rc = Results.ReturnCode;
                        var outPar = testCmd.Parameters.Add($"@rc", SqlDbType.Int);
                        outPar.Direction = ParameterDirection.ReturnValue;
                        outPars.Add(outPar);
                    }
                }

                using var reader = testCmd.ExecuteReader();

                if (Results != null)
                {
                    if (Results.ResultSets != null)
                    {
                        var resultSets = Results.ResultSets;

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

                        foreach (var expected in Results.ResultSets)
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

                    if (Results.OutputParameters != null)
                    {
                        foreach (var expected in Results.OutputParameters)
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

                    if (Results.ReturnCode.HasValue)
                    {
                        var expected = Results.ReturnCode.Value;
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
                if (Asserts != null)
                {
                    foreach (var assert in Asserts)
                    {
                        SqlCommand cmd = new(assert.SqlQuery, Connection);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            bool pass = false;
                            var scalarType = Utils.MapType(assert.ScalarType);
                            pass = Convert.ChangeType(assert.ScalarValue, scalarType).Equals(Convert.ChangeType(result, scalarType));

                            if (pass)
                                ReportAdd($"Assert {assert.SqlQuery}: {result} == {assert.ScalarValue}", false);
                            else
                                ReportAdd($"Assert {assert.SqlQuery}: {result} != {assert.ScalarValue}", true);
                        }
                        else
                            ReportAdd($"Assert {assert.SqlQuery}: Result NULL", true);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportAdd($"FATAL: {ex.Message}", true);
            }
            finally
            {
                if (After != null)
                {
                    foreach (var script in After)
                    {
                        string cmd = script.Compact();
                        ReportAdd("Running After scripts...");
                        var delete = new SqlCommand(cmd, Connection);
                        delete.ExecuteNonQuery();
                        ReportAdd("Completed.", false);
                    }
                }
                Connection.Close();
            }

            IsPass = !Report.Where(m => m.IsError.HasValue && m.IsError.Value).Any();

            ReportAdd($"Test {Name}: {(IsPass ? "OK":"KO")}", !IsPass);

            return IsPass;
        }

        private void ReportAdd(string message, bool? isError = null)
        {
            message = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " " + message;
            Report.Add((message, isError));

            if (isError.HasValue)
                Console.ForegroundColor = isError.Value? ConsoleColor.Red : ConsoleColor.Green;
            
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
