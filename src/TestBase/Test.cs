using System.Data;
using System.Data.SqlClient;

namespace TestBase
{
    public class Test
    {
        public string Name { get; set; }
        public List<Script>? Before { get; set; }
        public SqlConnection? Connection { get; set; }
        public TestCommand? Command { get; set; }
        public ResultGroup Results { get; set; }
        public List<TestAssert>? Asserts { get; set; }
        public List<Script>? After { get; set; }

        private bool IsPass { get; set; }
        private List<(string Message, bool? IsError)> Report { get; set; }

        public Test(string name)
        {
            Name = name;
        }

        public Test()
        {

        }

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
                            string data = item.Compact();
                            if (data.Length > 0)
                            {
                                ReportAdd("Loading data...");
                                var dataprep = new SqlCommand(data, Connection, loadData);
                                dataprep.ExecuteNonQuery();
                                ReportAdd("Loaded data", false);
                            }
                        }
                        loadData.Commit();
                    }
                    catch (Exception ex)
                    {
                        loadData.Rollback();
                        throw;
                    }
                }

                SqlCommand testCmd = new(Command.CommandText, Connection);
                testCmd.CommandType = CommandType.StoredProcedure;

                // add input pars
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
                            var outPar = testCmd.Parameters.Add($"@{output.Name}", SqlDbType.Int);
                            outPar.Direction = ParameterDirection.Output;
                            outPars.Add(outPar);
                        }

                    // add rc if exists
                    if (Results.ReturnCode != null)
                    {
                        var rc = Results.ReturnCode;
                        var outPar = testCmd.Parameters.Add($"@{rc.Name}", SqlDbType.Int);
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

                        for (int i = 0; i < resultSets.Count; i++)
                        {
                            DataTable dataTable = new(resultSets[i].Name);
                            for (int f = 0; f < resultSets[i].ResultsetDefinition.Count; f++)
                            {
                                var field = resultSets[i].ResultsetDefinition[f];
                                switch (field.FieldType)
                                {
                                    case SqlDbType.Int:
                                        dataTable.Columns.Add(field.FieldName, typeof(Int32));
                                        break;
                                    case SqlDbType.NVarChar:
                                        dataTable.Columns.Add(field.FieldName, typeof(String));
                                        break;
                                    case SqlDbType.DateTime:
                                        dataTable.Columns.Add(field.FieldName, typeof(DateTime));
                                        break;
                                    case SqlDbType.Bit:
                                        dataTable.Columns.Add(field.FieldName, typeof(bool));
                                        break;
                                    case SqlDbType.DateTimeOffset:
                                        dataTable.Columns.Add(field.FieldName, typeof(DateTimeOffset));
                                        break;
                                    case SqlDbType.TinyInt:
                                        dataTable.Columns.Add(field.FieldName, typeof(Byte));
                                        break;
                                    case SqlDbType.Date:
                                        dataTable.Columns.Add(field.FieldName, typeof(DateTime));
                                        break;
                                    case SqlDbType.Float:
                                        dataTable.Columns.Add(field.FieldName, typeof(Double));
                                        break;
                                    case SqlDbType.Time:
                                        dataTable.Columns.Add(field.FieldName, typeof(TimeSpan));
                                        break;
                                }
                            }

                            while (reader.Read())
                            {
                                DataRow row = dataTable.NewRow();
                                for (int f = 0; f < resultSets[i].ResultsetDefinition.Count; f++)
                                {
                                    var field = resultSets[i].ResultsetDefinition[f];
                                    if (reader.IsDBNull(f))
                                        row[field.FieldName] = DBNull.Value;
                                    else
                                    {
                                        switch (field.FieldType)
                                        {
                                            case SqlDbType.Int:
                                                row[field.FieldName] = reader.GetInt32(f);
                                                break;
                                            case SqlDbType.NVarChar:
                                                row[field.FieldName] = reader.GetString(f);
                                                break;
                                            case SqlDbType.DateTime:
                                                row[field.FieldName] = reader.GetDateTime(f);
                                                break;
                                            case SqlDbType.Bit:
                                                row[field.FieldName] = reader.GetSqlBoolean(f);
                                                break;
                                            case SqlDbType.DateTimeOffset:
                                                row[field.FieldName] = reader.GetDateTimeOffset(f);
                                                break;
                                            case SqlDbType.TinyInt:
                                                row[field.FieldName] = reader.GetByte(f);
                                                break;
                                            case SqlDbType.Date:
                                                row[field.FieldName] = reader.GetDateTime(f);
                                                break;
                                            case SqlDbType.Float:
                                                row[field.FieldName] = reader.GetDouble(f);
                                                break;
                                            case SqlDbType.Time:
                                                row[field.FieldName] = reader.GetTimeSpan(f);
                                                break;
                                        }
                                    }
                                }
                                dataTable.Rows.Add(row);
                            }
                            dataTables.Add(dataTable);
                            reader.NextResult();
                        }

                        foreach (var expected in Results.ResultSets)
                        {
                            bool error = false;

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

                            ReportAdd($"Result {expected.Name}: {expected.Value}", false);
                        }
                    }

                    if (Results.ReturnCode != null)
                    {
                        var expected = Results.ReturnCode;
                        ReportAdd($"Checking Return Value: {expected.Name}");
                        var currRes = outPars.Where(p => p.ParameterName == $"@{expected.Name}").FirstOrDefault();
                        if (currRes == null)
                        {
                            ReportAdd($"Return Code {expected.Name} not found", true);
                        }
                        else
                        {
                            if (expected.Value != Convert.ToInt32(currRes.Value))
                            {
                                ReportAdd($"Return Code {expected.Name}: {currRes.Value} != {expected.Value}", true);
                            }
                            else
                                ReportAdd($"Return Code {expected.Name}: {expected.Value}", false);
                        }
                    }
                }

                // asserts
                if (Asserts != null)
                {
                    foreach (var assert in Asserts)
                    {
                        SqlCommand cmd = new SqlCommand(assert.SqlQuery, Connection);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            bool pass = false;
                            switch (assert.ScalarType)
                            {
                                case SqlDbType.Int:
                                    pass = Convert.ToInt32(assert.ScalarValue) == Convert.ToInt32(result);
                                    break;
                                case SqlDbType.NVarChar:
                                    pass = Convert.ToString(assert.ScalarValue) == Convert.ToString(result);
                                    break;
                                case SqlDbType.SmallInt:
                                    pass = Convert.ToByte(assert.ScalarValue) == Convert.ToByte(result);
                                    break;
                                case SqlDbType.Bit:
                                    pass = Convert.ToBoolean(assert.ScalarValue) == Convert.ToBoolean(result);
                                    break;
                            }
                            ReportAdd($"Assert {assert.SqlQuery}: {result} == {assert.ScalarValue}", !pass);
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
                    foreach (var del in After)
                    {
                        string cmd = del.Compact();
                        ReportAdd("Deleting data...");
                        var delete = new SqlCommand(cmd, Connection);
                        delete.ExecuteNonQuery();
                        ReportAdd("Deleted data", false);
                    }
                }
                Connection.Close();
            }

            IsPass = !Report.Where(m => m.IsError.HasValue && m.IsError.Value).Any();

            return IsPass;
        }

        private void ReportAdd(string message, bool? isError = null)
        {
            message = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " " + message;
            Report.Add((message, isError));

            if (isError.HasValue)
                Console.ForegroundColor = isError.Value? ConsoleColor.Red : ConsoleColor.Green;
            
            Console.WriteLine(message);
            Console.ForegroundColor= ConsoleColor.White;
        }
    }
}
