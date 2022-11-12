using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using qest.Models;
using Spectre.Console;

namespace qest.Visualizers
{
    public class TreeVisualizer<T> : IVisualizer where T : IConnector, new()
    {
        private List<Test> TestCollection;
        private Test Test;
        private bool Pass;
        private bool Verbose;

        private const string objectStyle_l1 = "blue";
        private const string objectStyle_l2 = "cornflowerblue";
        private const string objectStyle_l3 = "steelblue1";

        private const string errorStyle = "bold red";
        private const string okStyle = "green";

        private T DbConnector;

        public TreeVisualizer(List<Test> testCollection, string connectionString)
        {
            TestCollection = testCollection;
            Pass = true;

            DbConnector = new();
            DbConnector.SetConnectionString(connectionString);
        }

        public async Task<int> RunAllAsync(bool verbose)
        {
            Verbose = verbose;
            int exitCode = 0;

            var root = new Tree($"{TestCollection.Count} tests loaded");

            foreach (var currentTest in TestCollection)
            {
                var testNode = new TreeNode(new Markup($"{currentTest.Name}".EscapeAndAddStyles($"bold {objectStyle_l1}")));

                Test = currentTest;

                await RunSingleTestAsync(testNode);

                if (!Pass)
                {
                    exitCode = 1;
                    break;
                }
            }

            AnsiConsole.Write(root);

            return exitCode;
        }

        private async Task RunSingleTestAsync(TreeNode testNode)
        {
            await DbConnector.LoadData(Test);
            Pass = true;

            if (Test.ResultException is not null)
            {
                AddExceptionNode(testNode, Test.ResultException);
                Pass = false;
                return;
            }

            if (Test.Before is not null)
            {
                EvaluateScript("Before", Test.Before, testNode);
            }

            foreach (var testCase in Test.Steps)
            {
                var testCaseNode = new TreeNode(new Markup($"Step: {testCase.Name.EscapeAndAddStyles(objectStyle_l2)}"));
                EvaluateTestStep(testCase, Test.Variables, testCaseNode);

            }

            if (Test.After is not null)
            {
                EvaluateScript("After", Test.After, testNode);
            }

            if (Pass)
                testNode.AddNode($"{Test.Name}: OK".EscapeAndAddStyles($"bold {okStyle}"));
            else
                testNode.AddNode($"{Test.Name}: KO!".EscapeAndAddStyles(errorStyle));
        }


        private void EvaluateTestStep(TestStep step, Dictionary<string, object>? variables, TreeNode testCaseNode)
        {
            if (!step.Command.Result)
            {
                var commandNode = testCaseNode.AddNode("Command");
                AddExceptionNode(commandNode, step.Command.ResultException);
                Pass = false;
                return;
            }

            if (step.Results != null)
            {
                // resultsets
                if (step.Results.ResultSets != null)
                {
                    var resultSetsNode = NewMarkupTreeNode("Result Sets");

                    foreach (var expectedResult in step.Results.ResultSets)
                    {
                        var currentResultNode = NewMarkupTreeNode(expectedResult.Name.EscapeAndAddStyles(objectStyle_l3));

                        var currentResult = expectedResult.Result;

                        if (expectedResult.ResultException is not null)
                        {
                            AddExceptionNode(currentResultNode, expectedResult.ResultException);
                            AddErrorNode(resultSetsNode, currentResultNode);
                            Pass = false;
                            continue;
                        }

                        if (currentResult == null)
                        {
                            currentResultNode.AddNode($"Not found".EscapeAndAddStyles(errorStyle));
                            AddErrorNode(resultSetsNode, currentResultNode);
                            Pass = false;
                            continue;
                        }

                        if (expectedResult.RowNumber != null)
                        {
                            if (expectedResult.RowNumber != currentResult.Rows.Count)
                            {
                                currentResultNode.AddNode($"Rows: {currentResult.Rows.Count} != {expectedResult.RowNumber}".EscapeAndAddStyles(errorStyle));
                                AddErrorNode(resultSetsNode, currentResultNode);
                                Pass = false;
                                continue;
                            }
                        }

                        AddNode(currentResultNode, NewMarkupTreeNode("OK".EscapeAndAddStyles(okStyle)));
                        AddNode(resultSetsNode, currentResultNode);
                    }

                    if (resultSetsNode.Nodes.Count > 0)
                        testCaseNode.AddNode(resultSetsNode);
                }

                //output parameters
                if (step.Results.OutputParameters is not null)
                {
                    var outputParametersNode = NewMarkupTreeNode("Output Parameters");
                    foreach (var expectedResult in step.Results.OutputParameters)
                    {
                        var currentResult = expectedResult.Result;
                        var currentResultNode = NewMarkupTreeNode($"{expectedResult.Name}".EscapeAndAddStyles(objectStyle_l3));

                        if (currentResult is null)
                        {
                            currentResultNode.AddNode("Null output".EscapeAndAddStyles(errorStyle));
                            Pass = false;
                            continue;
                        }

                        if (expectedResult.Value is not null)
                        {
                            var parameterType = Utils.MapQestTypeToInternal(expectedResult.Type);
                            if (!Convert.ChangeType(expectedResult.Value, parameterType).Equals(Convert.ChangeType(currentResult, parameterType)))
                            {
                                currentResultNode.AddNode($"{currentResult} != {expectedResult.Value}".EscapeAndAddStyles(errorStyle));
                                Pass = false;
                                continue;
                            }
                        }

                        AddNode(currentResultNode, NewMarkupTreeNode($"{expectedResult.Value} == {currentResult}".EscapeAndAddStyles(okStyle)));
                        AddNode(outputParametersNode, currentResultNode);
                    }
                }

                //returncode
                if (step.Results.ReturnCode.HasValue)
                {
                    var rcNode = testCaseNode.AddNode("Return Code".EscapeAndAddStyles(objectStyle_l3));
                    var expectedResult = step.Results.ReturnCode.Value;
                    var currentResult = step.Results.ReturnCodeResult;

                    if (step.Results.ReturnCodeResultException is not null)
                    {
                        AddExceptionNode(rcNode, step.Results.ReturnCodeResultException);
                        Pass = false;
                    }
                    else
                    {

                        if (!currentResult.HasValue)
                        {
                            rcNode.AddNode("Null output".EscapeAndAddStyles(errorStyle));
                            Pass = false;
                        }
                        else
                        {
                            if (expectedResult != Convert.ToInt32(currentResult.Value))
                            {
                                rcNode.AddNode($"{currentResult.Value} != {expectedResult}".EscapeAndAddStyles(errorStyle));
                                Pass = false;
                            }
                            else
                                rcNode.AddNode($"{expectedResult} == {currentResult.Value}".EscapeAndAddStyles(okStyle));
                        }
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
                    var currentResult = assert.Result;
                    var currentResultNode = assertsNode.AddNode(assertSqlQuery.EscapeAndAddStyles(objectStyle_l3));

                    if (assert.ResultException is not null)
                    {
                        AddExceptionNode(currentResultNode, assert.ResultException);
                        Pass = false;
                        continue;
                    }

                    if (currentResult is null)
                    {
                        currentResultNode.AddNode($"NULL != {assertScalarValue}".EscapeAndAddStyles(errorStyle));
                        Pass = false;
                    }
                    else
                    {
                        bool convertOk = false;
                        var scalarType = Utils.MapQestTypeToInternal(assert.ScalarType);
                        convertOk = Convert.ChangeType(assertScalarValue, scalarType).Equals(Convert.ChangeType(currentResult, scalarType));

                        if (convertOk)
                            currentResultNode.AddNode($"{currentResult} == {assertScalarValue}".EscapeAndAddStyles(okStyle));
                        else
                        {
                            currentResultNode.AddNode($"{currentResult} != {assertScalarValue}".EscapeAndAddStyles(errorStyle));
                            Pass = false;
                        }
                    }
                }
            }
        }

        private void EvaluateScript(string scope, Scripts scripts, TreeNode root)
        {
            string objName = scope.EscapeAndAddStyles(objectStyle_l2);
            var scriptsNode = new TreeNode(new Markup($"Scripts: {objName}"));

            if (scripts.Result)
            {
                scriptsNode.AddNode("OK".EscapeAndAddStyles(okStyle));
                AddNode(root, scriptsNode);
            }
            else
            {
                scriptsNode.AddNode(scripts.ResultException!.ToString().EscapeAndAddStyles(errorStyle));
                AddErrorNode(root, scriptsNode);
                Pass = false;
            }
        }

        private void AddExceptionNode(TreeNode currentNode, Exception ex)
        {
            currentNode.AddNode($"Exception".EscapeAndAddStyles(errorStyle));
            currentNode.AddNode(ex.ToString().EscapeAndAddStyles(errorStyle));
        }

        private void AddNode(TreeNode currentNode, TreeNode nodeToAdd, bool forceOutput = false)
        {
            if (Verbose || forceOutput)
                currentNode.AddNode(nodeToAdd);
        }

        private void AddErrorNode(TreeNode currentNode, TreeNode nodeToAdd) => AddNode(currentNode, nodeToAdd, true);

        private TreeNode NewMarkupTreeNode(string markup) => new TreeNode(new Markup(markup));
    }
}