using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using qest.Models;

namespace qest.Connectors
{
    public class MsSqlConnector : IConnector
    {
        private SqlConnection Connection;
        private Test currentTest;
        public MsSqlConnector()
        {
        }

        public void SetConnectionString(string connectionString)
        {
            this.Connection = new SqlConnection(connectionString);
        }

        public async Task LoadData(Test test)
        {
            currentTest = test;

            try
            {
                await Connection.OpenAsync();

                if (currentTest.Before is not null)
                {
                    await RunScriptAsync(currentTest.Before);
                }

                foreach (var testCase in currentTest.Steps)
                {
                    RunTestStep(testCase);
                }
            }
            catch (Exception ex)
            {
                this.currentTest.ResultException = ex;
            }
            finally
            {
                if (currentTest.After is not null)
                {
                    await RunScriptAsync(currentTest.After);
                }

                await Connection.CloseAsync();
            }
        }

        private void RunTestStep(TestStep step)
        {
            SqlCommand testCmd = new(step.Command.CommandText, Connection);
            testCmd.CommandType = CommandType.StoredProcedure;

            // add input pars
            if (step.Command.Parameters != null)
                foreach (var input in step.Command.Parameters)
                {
                    testCmd.Parameters.AddWithValue($"@{input.Key}", input.Value?.ReplaceVarsInParameter(currentTest.Variables));
                }

            // add output pars
            List<SqlParameter> outPars = new();
            if (step.Results != null)
            {
                if (step.Results.OutputParameters != null)
                    foreach (var output in step.Results.OutputParameters)
                    {
                        var outPar = testCmd.Parameters.Add($"@{output.Name}", MapQestTypeToSqlType(output.Type));
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

            try
            {
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
                            try
                            {
                                resultSet.CreateDataTable();

                                while (reader.Read())
                                {
                                    DataRow row = resultSet.Result!.NewRow();

                                    for (int f = 0; f < resultSet.Columns.Count; f++)
                                    {
                                        var column = resultSet.Columns[f];

                                        if (reader.IsDBNull(f))
                                            row[column.Name] = DBNull.Value;
                                        else
                                            row[column.Name] = Convert.ChangeType(reader.GetValue(f), Utils.MapQestTypeToInternal(column.Type));
                                    }
                                    resultSet.Result.Rows.Add(row);
                                }
                            }
                            catch (Exception ex)
                            {
                                resultSet.ResultException = ex;
                            }
                            reader.NextResult();
                        }
                    }

                    reader.Close();

                    //output parameters
                    if (step.Results.OutputParameters != null)
                    {
                        foreach (var expected in step.Results.OutputParameters)
                        {
                            try
                            {
                                var currRes = outPars.Where(p => p.ParameterName == $"@{expected.Name}").FirstOrDefault();
                                if (currRes is not null)
                                    expected.Result = currRes.Value;
                            }
                            catch (Exception ex)
                            {
                                expected.ResultException = ex;
                            }
                        }
                    }

                    //returncode
                    if (step.Results.ReturnCode.HasValue)
                    {
                        try
                        {
                            var rcPar = outPars.Where(p => p.ParameterName == $"@rc").FirstOrDefault();
                            if (rcPar is not null)
                                step.Results.ReturnCodeResult = Convert.ToInt32(rcPar.Value);
                        }
                        catch (Exception ex)
                        {
                            step.Results.ReturnCodeResultException = ex;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                step.Command.ResultException = ex;
                return;
            }

            // asserts
            if (step.Asserts != null)
            {
                foreach (var assert in step.Asserts)
                {
                    try
                    {
                        var assertSqlQuery = assert.SqlQuery.ReplaceVars(currentTest.Variables);
                        var assertScalarValue = assert.ScalarValue.ReplaceVarsInParameter(currentTest.Variables);

                        SqlCommand cmd = new(assertSqlQuery, Connection);
                        var result = cmd.ExecuteScalar();
                        if (result is not null)
                            assert.Result = result;
                    }
                    catch (Exception ex)
                    {
                        assert.ResultException = ex;
                    }
                }
            }
        }

        private async Task RunScriptAsync(Scripts scripts)
        {
            var transaction = Connection.BeginTransaction();
            try
            {
                foreach (var item in scripts)
                {
                    string script = item.Compact(currentTest.Variables);
                    if (script.Length > 0)
                    {
                        var cmd = new SqlCommand(script, Connection, transaction);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                scripts.ResultException = ex;
            }
        }

        public static SqlDbType MapQestTypeToSqlType(qestType type) => type switch
        {
            qestType.Bit => SqlDbType.Bit,
            qestType.TinyInt => SqlDbType.TinyInt,
            qestType.SmallInt => SqlDbType.SmallInt,
            qestType.BigInt => SqlDbType.BigInt,
            qestType.Float => SqlDbType.Float,
            qestType.Int => SqlDbType.Int,
            qestType.NVarChar => SqlDbType.NVarChar,
            qestType.DateTime => SqlDbType.DateTime,
            qestType.DateTime2 => SqlDbType.DateTime2,
            qestType.Date => SqlDbType.Date,
            qestType.DateTimeOffset => SqlDbType.DateTimeOffset,
            qestType.Time => SqlDbType.Time,
            qestType.Real => SqlDbType.Real,
            qestType.Decimal => SqlDbType.Decimal,
            qestType.Money => SqlDbType.Decimal,

            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Type not expected: {type}"),
        };

        public static qestType MapStringToQestType(string type) => type.ToLower() switch
        {
            "bit" => qestType.Bit,
            "tinyint" => qestType.TinyInt,
            "smallint" => qestType.SmallInt,
            "bigint" => qestType.BigInt,
            "float" => qestType.Float,
            "int" => qestType.Int,
            "nvarchar" => qestType.NVarChar,
            "datetime" => qestType.DateTime,
            "datetime2" => qestType.DateTime2,
            "date" => qestType.Date,
            "datetimeoffset" => qestType.DateTimeOffset,
            "time" => qestType.Time,
            "real" => qestType.Real,
            "decimal" => qestType.Decimal,
            "money" => qestType.Money,

            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Type not expected: {type}"),
        };


    }
}