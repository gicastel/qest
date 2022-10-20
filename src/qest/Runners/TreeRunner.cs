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

        private const string errorStyles = "bold,red";

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
                Node.AddNode("FATAL: TestCases null".EscapeAndAddStyles(errorStyles));
                return false;
            }

            try
            {
                await Connection.OpenAsync();

                if (Test.Before is not null)
                {
                    var beforeNode = Node.AddNode("[blue]Before[/] scripts");
 
                    beforeNode.AddNode("Running...");
                    await RunScriptAsync(Test.Before, Test.Variables);
                    beforeNode.AddNode("[green]Done![/]");
                }


                foreach (var testCase in Test.Steps)
                {
                    var testCaseNode = Node.AddNode($"Step: [blue]{testCase.Name}[/]");
                    RunTestStep(testCase, Test.Variables, testCaseNode);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.Markup($"FATAL: {ex}".EscapeAndAddStyles(errorStyles));
            }
            finally
            {
                if (Test.After is not null)
                {
                    var afterNode = Node.AddNode($"[blue]After[/] scripts");
 
                    afterNode.AddNode("Running...");
                    await RunScriptAsync(Test.After, Test.Variables);
                    afterNode.AddNode("[green]Done![/]");
                }

                await Connection.CloseAsync();
            }

            if (Pass)
                Node.AddNode($"{Test.Name}: OK".EscapeAndAddStyles("green,bold"));
            else
                Node.AddNode($"{Test.Name}: KO!".EscapeAndAddStyles(errorStyles));

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
                    var resultSetsNode = testCaseNode.AddNode("[blue]Result Sets[/]");

                    List<DataTable> dataTables = new();
                    Dictionary<string, TreeNode> dataSetNodes = new();

                    foreach (var resultSet in resultSets)
                    {
                        var rs_node = resultSetsNode.AddNode($"{resultSet.Name}".EscapeAndAddStyles("blue"));
                        rs_node.AddNode("Loading...");

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
                        rs_node.AddNode("Loaded...");
                        dataSetNodes.Add(resultSet.Name, rs_node);
                        reader.NextResult();
                    }

                    foreach (var expected in step.Results.ResultSets)
                    {
                        var currRes = dataTables.Where(d => d.TableName == expected.Name).FirstOrDefault();
                        var currNode = dataSetNodes[expected.Name];
                        currNode.AddNode($"Checking...");

                        if (currRes == null)
                        {
                            currNode.AddNode($"Not found!".EscapeAndAddStyles(errorStyles));
                            Pass = false;
                            continue;
                        }

                        if (expected.RowNumber != null)
                        {
                            if (expected.RowNumber != currRes.Rows.Count)
                            {
                                currNode.AddNode($"Row number: {currRes.Rows.Count} != {expected.RowNumber}".EscapeAndAddStyles(errorStyles));
                                Pass = false;
                                continue;
                            }
                        }
                        
                        currNode.AddNode($"[green]OK[/]");
                    }
                }

                reader.Close();

                //output parameters
                if (step.Results.OutputParameters != null)
                {
                    var outputParsNode = testCaseNode.AddNode("[blue]Output Parameters[/]");
                    foreach (var expected in step.Results.OutputParameters)
                    {
                        var currRes = outPars.Where(p => p.ParameterName == $"@{expected.Name}").FirstOrDefault();
                        var currNode = outputParsNode.AddNode($"{expected.Name}".EscapeAndAddStyles("blue"));

                        currNode.AddNode("Checking...");
                        if (currRes == null)
                        {
                            currNode.AddNode("Not found!".EscapeAndAddStyles(errorStyles));
                            Pass = false;
                            continue;
                        }

                        if (expected.Value != null)
                        {
                            if (!currRes.Value.Equals(currRes.Value))
                            {
                                currNode.AddNode($"{currRes.Value} != {expected.Value}".EscapeAndAddStyles(errorStyles));
                                Pass = false;
                                continue;
                            }
                        }

                        currNode.AddNode($"{expected.Value} == {currRes.Value}".EscapeAndAddStyles("green"));
                    }
                }

                //returncode
                if (step.Results.ReturnCode.HasValue)
                {
                    var rcNode = testCaseNode.AddNode("[blue]Return Code[/]");
                    var expected = step.Results.ReturnCode.Value;
                    var rcPar = outPars.Where(p => p.ParameterName == $"@rc").FirstOrDefault();
                    rcNode.AddNode($"Checking...");
                    if (rcPar == null)
                    {
                        rcNode.AddNode("Not found!".EscapeAndAddStyles(errorStyles));
                        Pass = false;
                    }
                    else
                    {
                        if (expected != Convert.ToInt32(rcPar.Value))
                        {
                            rcNode.AddNode($"{rcPar.Value} != {expected}".EscapeAndAddStyles(errorStyles));
                            Pass = false;
                        }
                        else
                            rcNode.AddNode($"{expected} == {expected}".EscapeAndAddStyles("green"));
                    }
                }
            }

            // asserts
            if (step.Asserts != null)
            {
                var assertsNode = testCaseNode.AddNode("[blue]Asserts[/]");
                foreach (var assert in step.Asserts)
                {
                    var assertSqlQuery = assert.SqlQuery.ReplaceVars(Test.Variables);                    
                    var assertScalarValue = assert.ScalarValue.ReplaceVarsInParameter(Test.Variables);

                    var currNode = assertsNode.AddNode(assertSqlQuery.EscapeAndAddStyles("blue"));

                    SqlCommand cmd = new(assertSqlQuery, Connection);
                    var result = cmd.ExecuteScalar();
                    if (result is null)
                    {
                        currNode.AddNode($"NULL != {assertScalarValue}".EscapeAndAddStyles(errorStyles));
                        Pass = false;
                    }
                    else
                    {
                        bool convertOk = false;
                        var scalarType = Utils.MapType(assert.ScalarType);
                        convertOk = Convert.ChangeType(assertScalarValue, scalarType).Equals(Convert.ChangeType(result, scalarType));

                        if (convertOk)
                            currNode.AddNode($"{result} == {assertScalarValue}".EscapeAndAddStyles("green"));
                        else
                        {
                            currNode.AddNode($"{result} != {assertScalarValue}".EscapeAndAddStyles(errorStyles));
                            Pass = false;
                        }
                    }
                }
            }
        }


        private async Task RunScriptAsync(List<Script> scripts, Dictionary<string, object>? variables)
        {
            var transaction = Connection.BeginTransaction();
            try
            {
                foreach (var item in scripts)
                {
                    string script = item.Compact(variables);
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