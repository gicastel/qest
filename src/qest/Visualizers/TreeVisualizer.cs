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

                root.AddNode(testNode);

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
                testNode.AddExceptionNode(Test.ResultException);
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

                if (testCaseNode.Nodes.Count > 0)
                    testNode.AddNode(testCaseNode);
            }

            if (Test.After is not null)
            {
                EvaluateScript("After", Test.After, testNode);
            }

            if (Pass)
                testNode.AddNode("OK".EscapeAndAddStyles($"bold {okStyle}"));
            else
                testNode.AddNode("KO!".EscapeAndAddStyles(errorStyle));
            
        }


        private void EvaluateTestStep(TestStep step, Dictionary<string, object>? variables, TreeNode testCaseNode)
        {

            var commandNode = NewMarkupTreeNode("Command");
            string actualCommand = $"{step.Command.CommandText} {string.Join(", ", step.Command.ActualParameters)}";
            commandNode.AddNode(actualCommand.EscapeAndAddStyles(objectStyle_l3));
            if (!step.Command.Result)
            {
                commandNode.AddExceptionNode(step.Command.ResultException);
                testCaseNode.AddNode(commandNode);
                Pass = false;
                return;
            }
            testCaseNode.AddOutputNode(commandNode, Verbose);

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
                            currentResultNode.AddExceptionNode(expectedResult.ResultException);
                            resultSetsNode.AddNode(currentResultNode);
                            Pass = false;
                            continue;
                        }

                        if (currentResult == null)
                        {
                            currentResultNode.AddNode($"Not found".EscapeAndAddStyles(errorStyle));
                            resultSetsNode.AddNode(currentResultNode);
                            Pass = false;
                            continue;
                        }

                        if (expectedResult.RowNumber != null)
                        {
                            if (expectedResult.RowNumber != currentResult.Rows.Count)
                            {
                                currentResultNode.AddNode($"Rows: {currentResult.Rows.Count} != {expectedResult.RowNumber}".EscapeAndAddStyles(errorStyle));
                                resultSetsNode.AddNode(currentResultNode);
                                Pass = false;
                                continue;
                            }
                        }

                        currentResultNode.AddOutputNode(NewMarkupTreeNode("OK".EscapeAndAddStyles(okStyle)), Verbose);
                        resultSetsNode.AddOutputNode(currentResultNode, Verbose);
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
                            testCaseNode.AddNode(outputParametersNode.AddNode(currentResultNode));
                            Pass = false;
                            continue;
                        }

                        if (expectedResult.Value is not null)
                        {
                            var parameterType = Utils.MapQestTypeToInternal(expectedResult.Type);
                            if (!Convert.ChangeType(expectedResult.Value, parameterType).Equals(Convert.ChangeType(currentResult, parameterType)))
                            {
                                currentResultNode.AddNode($"{currentResult} != {expectedResult.Value}".EscapeAndAddStyles(errorStyle));
                                testCaseNode.AddNode(outputParametersNode.AddNode(currentResultNode));
                                Pass = false;
                                continue;
                            }
                        }

                        currentResultNode.AddOutputNode(NewMarkupTreeNode($"{expectedResult.Value} == {currentResult}".EscapeAndAddStyles(okStyle)), Verbose);
                        outputParametersNode.AddOutputNode(currentResultNode, Verbose);
                    }

                    if (outputParametersNode.Nodes.Count > 0)
                        testCaseNode.AddNode(outputParametersNode);
                }

                //returncode
                if (step.Results.ReturnCode.HasValue)
                {
                    var rcNode = NewMarkupTreeNode("Return Code".EscapeAndAddStyles(objectStyle_l3));
                    var expectedResult = step.Results.ReturnCode.Value;
                    var currentResult = step.Results.ReturnCodeResult;

                    if (step.Results.ReturnCodeResultException is not null)
                    {
                        rcNode.AddExceptionNode(step.Results.ReturnCodeResultException);
                        testCaseNode.AddNode(rcNode);
                        Pass = false;
                    }
                    else
                    {

                        if (!currentResult.HasValue)
                        {
                            rcNode.AddNode("Null output".EscapeAndAddStyles(errorStyle));
                            testCaseNode.AddNode(rcNode);
                            Pass = false;
                        }
                        else
                        {
                            if (expectedResult != Convert.ToInt32(currentResult.Value))
                            {
                                rcNode.AddNode($"{currentResult.Value} != {expectedResult}".EscapeAndAddStyles(errorStyle));
                                testCaseNode.AddNode(rcNode);
                                Pass = false;
                            }
                            else
                            {
                                rcNode.AddNode($"{expectedResult} == {currentResult.Value}".EscapeAndAddStyles(okStyle));
                                testCaseNode.AddOutputNode(rcNode, Verbose);
                            }
                        }
                    }
                }
            }

            // asserts
            if (step.Asserts != null)
            {
                var assertsNode = NewMarkupTreeNode("Asserts");
                foreach (var assert in step.Asserts)
                {
                    var assertSqlQuery = assert.SqlQuery.ReplaceVars(Test.Variables);
                    var assertScalarValue = assert.ScalarValue.ReplaceVarsInParameter(Test.Variables);
                    var currentResult = assert.Result;
                    var currentResultNode = NewMarkupTreeNode(assertSqlQuery.EscapeAndAddStyles(objectStyle_l3));

                    if (assert.ResultException is not null)
                    {
                        currentResultNode.AddExceptionNode(assert.ResultException);
                        assertsNode.AddNode(currentResultNode);
                        Pass = false;
                        continue;
                    }

                    if (currentResult is null)
                    {
                        currentResultNode.AddNode($"NULL != {assertScalarValue}".EscapeAndAddStyles(errorStyle));
                        assertsNode.AddNode(currentResultNode);
                        Pass = false;
                    }
                    else
                    {
                        bool convertOk = false;
                        var scalarType = Utils.MapQestTypeToInternal(assert.ScalarType);
                        convertOk = Convert.ChangeType(assertScalarValue, scalarType).Equals(Convert.ChangeType(currentResult, scalarType));

                        if (convertOk)
                        {
                            currentResultNode.AddNode($"{currentResult} == {assertScalarValue}".EscapeAndAddStyles(okStyle));
                            assertsNode.AddOutputNode(currentResultNode, Verbose);
                        }
                        else
                        {
                            currentResultNode.AddNode($"{currentResult} != {assertScalarValue}".EscapeAndAddStyles(errorStyle));
                            assertsNode.AddNode(currentResultNode);
                            Pass = false;
                        }
                    }
                }

                if (assertsNode.Nodes.Count > 0)
                    testCaseNode.AddNode(assertsNode);
            }
        }

        private void EvaluateScript(string scope, Scripts scripts, TreeNode root)
        {
            string objName = scope.EscapeAndAddStyles(objectStyle_l2);
            var scriptsNode = new TreeNode(new Markup($"Scripts: {objName}"));

            if (scripts.Result)
            {
                scriptsNode.AddNode("OK".EscapeAndAddStyles(okStyle));
                root.AddOutputNode(scriptsNode, Verbose);
            }
            else
            {
                scriptsNode.AddExceptionNode(scripts.ResultException!);
                root.AddNode(scriptsNode);
                Pass = false;
            }
        }

        private TreeNode NewMarkupTreeNode(string markup) => new TreeNode(new Markup(markup));
    }

    file static class ExtensionMethods
    {
        internal static TreeNode AddOutputNode(this TreeNode parentNode, TreeNode childNode, bool verbose)
        {
             if (verbose)
                parentNode.AddNode(childNode);
            
            return parentNode;
        }

        internal static TreeNode AddExceptionNode(this TreeNode currentNode, Exception ex, string errorStyle = "bold red")
        {
            currentNode.AddNode($"Exception".EscapeAndAddStyles(errorStyle));
            currentNode.AddNode(ex.ToString().EscapeAndAddStyles(errorStyle));
            return currentNode;
        }
    }

}