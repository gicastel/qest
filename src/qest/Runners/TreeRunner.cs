using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using qest.Models;
using Spectre.Console;

namespace qest.Runners
{
    public class TreeRunner
    {
        private readonly Test Test;
        private readonly SqlConnection Connection;
        private readonly TreeNode Node;
        private bool Pass;

        private const string objectStyle = "blue";
        private const string errorStyle = "bold red";
        private const string okStyle = "green";

        public TreeRunner(Test _test, SqlConnection _connection, TreeNode _node)
        {
            Test = _test;
            Connection = _connection;
            Node = _node;
            Pass = true;
        }

        public async Task<bool> RunAsync()
        {
            Pass = true;

            if (Test.Steps == null)
            {
                Node.AddNode("FATAL: TestCases null".EscapeAndAddStyles(errorStyle));
                return false;
            }

            try
            {
                await Connection.OpenAsync();

                if (Test.Before is not null)
                {
                    string objName = "Before".EscapeAndAddStyles(objectStyle);
                    var beforeNode = Node.AddNode($"Scripts: {objName}");
                    await RunScriptAsync(Test.Before);
                    beforeNode.AddNode("OK".EscapeAndAddStyles(okStyle));
                }

                foreach (var testCase in Test.Steps)
                {
                    var testCaseNode = Node.AddNode($"Step: {testCase.Name.EscapeAndAddStyles(objectStyle)}");
                    RunTestStep(testCase, Test.Variables, testCaseNode);
                }
            }
            catch (Exception ex)
            {
               Node.AddNode($"FATAL: {ex}".EscapeAndAddStyles(errorStyle));
            }
            finally
            {
                if (Test.After is not null)
                {
                    string objName = "After".EscapeAndAddStyles(objectStyle);
                    var afterNode = Node.AddNode($"Scripts: {objName}");
                    await RunScriptAsync(Test.After);
                    afterNode.AddNode("OK".EscapeAndAddStyles(okStyle));
                }

                await Connection.CloseAsync();
            }

            if (Pass)
                Node.AddNode($"{Test.Name}: OK".EscapeAndAddStyles($"bold {okStyle}"));
            else
                Node.AddNode($"{Test.Name}: KO!".EscapeAndAddStyles(errorStyle));

            return Pass;
        }

        private void RunTestStep(TestStep step, Dictionary<string, object>? variables, TreeNode testCaseNode)
        {
            SqlCommand testCmd = new(step.Command.CommandText, Connection);
            testCmd.CommandType = CommandType.StoredProcedure;

            // add input pars
            if (step.Command.Parameters != null)
                foreach (var input in step.Command.Parameters)
                {
                    testCmd.Parameters.AddWithValue($"@{input.Key}", input.Value?.ReplaceVarsInParameter(variables));
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
                    var resultSetsNode = testCaseNode.AddNode("Result Sets");

                    List<DataTable> dataTables = new();
                    Dictionary<string, TreeNode> dataSetNodes = new();

                    foreach (var resultSet in resultSets)
                    {
                        var rs_node = resultSetsNode.AddNode($"{resultSet.Name}".EscapeAndAddStyles(objectStyle));
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
                        dataSetNodes.Add(resultSet.Name, rs_node);
                        reader.NextResult();
                    }

                    foreach (var expected in step.Results.ResultSets)
                    {
                        var currRes = dataTables.Where(d => d.TableName == expected.Name).FirstOrDefault();
                        var currNode = dataSetNodes[expected.Name];

                        if (currRes == null)
                        {
                            currNode.AddNode($"Not found!".EscapeAndAddStyles(errorStyle));
                            Pass = false;
                            continue;
                        }

                        if (expected.RowNumber != null)
                        {
                            if (expected.RowNumber != currRes.Rows.Count)
                            {
                                currNode.AddNode($"Row number: {currRes.Rows.Count} != {expected.RowNumber}".EscapeAndAddStyles(errorStyle));
                                Pass = false;
                                continue;
                            }
                        }
                        
                        currNode.AddNode($"OK".EscapeAndAddStyles(okStyle));
                    }
                }

                reader.Close();

                //output parameters
                if (step.Results.OutputParameters != null)
                {
                    var outputParsNode = testCaseNode.AddNode("Output Parameters");
                    foreach (var expected in step.Results.OutputParameters)
                    {
                        var currRes = outPars.Where(p => p.ParameterName == $"@{expected.Name}").FirstOrDefault();
                        var currNode = outputParsNode.AddNode($"{expected.Name}".EscapeAndAddStyles(objectStyle));

                        if (currRes == null)
                        {
                            currNode.AddNode("Not found!".EscapeAndAddStyles(errorStyle));
                            Pass = false;
                            continue;
                        }

                        if (expected.Value != null)
                        {
                            if (!currRes.Value.Equals(currRes.Value))
                            {
                                currNode.AddNode($"{currRes.Value} != {expected.Value}".EscapeAndAddStyles(errorStyle));
                                Pass = false;
                                continue;
                            }
                        }

                        currNode.AddNode($"{expected.Value} == {currRes.Value}".EscapeAndAddStyles(okStyle));
                    }
                }

                //returncode
                if (step.Results.ReturnCode.HasValue)
                {
                    var rcNode = testCaseNode.AddNode("[blue]Return Code[/]");
                    var expected = step.Results.ReturnCode.Value;
                    var rcPar = outPars.Where(p => p.ParameterName == $"@rc").FirstOrDefault();
                    if (rcPar == null)
                    {
                        rcNode.AddNode("Not found!".EscapeAndAddStyles(errorStyle));
                        Pass = false;
                    }
                    else
                    {
                        if (expected != Convert.ToInt32(rcPar.Value))
                        {
                            rcNode.AddNode($"{rcPar.Value} != {expected}".EscapeAndAddStyles(errorStyle));
                            Pass = false;
                        }
                        else
                            rcNode.AddNode($"{expected} == {expected}".EscapeAndAddStyles(okStyle));
                    }
                }
            }

            // asserts
            if (step.Asserts != null)
            {
                var assertsNode = testCaseNode.AddNode("Asserts");
                foreach (var assert in step.Asserts)
                {
                    var assertSqlQuery = assert.SqlQuery.ReplaceVars(Test.Variables);                    
                    var assertScalarValue = assert.ScalarValue.ReplaceVarsInParameter(Test.Variables);

                    var currNode = assertsNode.AddNode(assertSqlQuery.EscapeAndAddStyles(objectStyle));

                    SqlCommand cmd = new(assertSqlQuery, Connection);
                    var result = cmd.ExecuteScalar();
                    if (result is null)
                    {
                        currNode.AddNode($"NULL != {assertScalarValue}".EscapeAndAddStyles(errorStyle));
                        Pass = false;
                    }
                    else
                    {
                        bool convertOk = false;
                        var scalarType = Utils.MapType(assert.ScalarType);
                        convertOk = Convert.ChangeType(assertScalarValue, scalarType).Equals(Convert.ChangeType(result, scalarType));

                        if (convertOk)
                            currNode.AddNode($"{result} == {assertScalarValue}".EscapeAndAddStyles(okStyle));
                        else
                        {
                            currNode.AddNode($"{result} != {assertScalarValue}".EscapeAndAddStyles(errorStyle));
                            Pass = false;
                        }
                    }
                }
            }
        }


        private async Task RunScriptAsync(List<Script> scripts)
        {
            var transaction = Connection.BeginTransaction();
            try
            {
                foreach (var item in scripts)
                {
                    string script = item.Compact(Test.Variables);
                    if (script.Length > 0)
                    {
                        var cmd = new SqlCommand(script, Connection, transaction);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}