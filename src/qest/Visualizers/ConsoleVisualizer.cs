using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using qest.Models;
using Spectre.Console;

namespace qest.Visualizers
{
    public class ConsoleVisualizer<T> : IVisualizer where T : IConnector, new()
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

        private const string consoleSeparator = " [bold]/[/] ";

        private List<string> LogHierarchy;

        private T DbConnector;

        public ConsoleVisualizer(List<Test> testCollection, string connectionString)
        {
            TestCollection = testCollection;
            Pass = true;

            DbConnector = new();
            DbConnector.SetConnectionString(connectionString);

            LogHierarchy = new();
        }

        public async Task<int> RunAllAsync(bool verbose)
        {
            Verbose = verbose;

            int exitCode = 0;

            AnsiConsole.MarkupLine($"{TestCollection.Count} tests loaded");

            foreach (var currentTest in TestCollection)
            {
                LogHierarchy.Add($"{currentTest.Name}".EscapeAndAddStyles($"bold {objectStyle_l1}"));

                Test = currentTest;

                bool pass = await RunSingleTestAsync();

                LogHierarchy.RemoveLast();

                if (!pass)
                {
                    exitCode = 1;
                    break;
                }
            }

            return exitCode;
        }

        private async Task<bool> RunSingleTestAsync()
        {
            await DbConnector.LoadData(Test);
            Pass = true;

            if (Test.ResultException is not null)
            {
                LogException(Test.ResultException);
                //return false;
            }

            if (Test.Before is not null)
            {
                EvaluateScript("Before", Test.Before);
            }

            foreach (var testCase in Test.Steps)
            {
                LogHierarchy.Add($"{testCase.Name.EscapeAndAddStyles(objectStyle_l2)}");
                EvaluateTestStep(testCase, Test.Variables);
                LogHierarchy.RemoveLast();
            }

            if (Test.After is not null)
            {
                EvaluateScript("After", Test.After);
            }

            if (Pass)
                LogConsole("OK".EscapeAndAddStyles($"bold {okStyle}"), true);
            else
                LogConsole("KO!".EscapeAndAddStyles(errorStyle), true);

            return Pass;
        }


        private void EvaluateTestStep(TestStep step, Dictionary<string, object>? variables)
        {

            LogHierarchy.Add("Command");
            string actualCommand = $"{step.Command.CommandText} {string.Join(", ", step.Command.ActualParameters)}";
            if (!step.Command.Result)
            {
                LogConsoleError(actualCommand.EscapeAndAddStyles(objectStyle_l3));
                LogException(step.Command.ResultException);
                LogHierarchy.RemoveLast();
                Pass = false;
                return;
            }
            LogConsole(actualCommand.EscapeAndAddStyles(objectStyle_l3), Verbose);
            LogHierarchy.RemoveLast();

            if (step.Results != null)
            {
                // resultsets
                if (step.Results.ResultSets != null)
                {
                    LogHierarchy.Add("Result Sets");

                    foreach (var expectedResult in step.Results.ResultSets)
                    {
                        LogHierarchy.Add(expectedResult.Name.EscapeAndAddStyles(objectStyle_l3));

                        var currentResult = expectedResult.Result;

                        if (expectedResult.ResultException is not null)
                        {
                            LogException(expectedResult.ResultException);
                            Pass = false;
                            continue;
                        }

                        if (currentResult == null)
                        {
                            LogConsoleError($"Not found".EscapeAndAddStyles(errorStyle));
                            Pass = false;
                            LogHierarchy.RemoveLast();
                            continue;
                        }

                        if (expectedResult.RowNumber != null)
                        {
                            if (expectedResult.RowNumber != currentResult.Rows.Count)
                            {
                                LogConsoleError($"Rows: {currentResult.Rows.Count} != {expectedResult.RowNumber}".EscapeAndAddStyles(errorStyle));
                                Pass = false;
                                LogHierarchy.RemoveLast();
                                continue;
                            }
                        }

                        if (expectedResult.Data is not null)
                        {

                            // check expected values
                            int i = 0;

                            foreach (var expectedResultLine in expectedResult.Data.ReadLine())
                            {
                                var expectedRowWithSubstitutions = expectedResultLine.ReplaceVars(variables);
                                var expectedRow = expectedRowWithSubstitutions.Split(expectedResult.Data.Separator ?? ";");
                                var currentRow = currentResult.Rows[i];

                                LogHierarchy.Add($"{i + 1}".EscapeMarkup());

                                for (int j = 0; j < expectedRow.Length; j++)
                                {
                                    var currentValue = currentRow[j];
                                    var converter = TypeDescriptor.GetConverter(currentValue.GetType());
                                    var expectedValue = converter.ConvertFromString(null, CultureInfo.InvariantCulture, expectedRow[j]);

                                    if (!expectedValue.Equals(currentValue))
                                    {
                                        LogConsoleError($"{expectedResult.Columns[j].Name}: {currentValue} != {expectedValue}".EscapeAndAddStyles(errorStyle));
                                        Pass = false;
                                    }
                                    else
                                    {
                                        LogConsole($"{expectedResult.Columns[j].Name}: {currentValue}".EscapeAndAddStyles(okStyle), Verbose);
                                    }
                                }

                                i++;
                                    
                                LogHierarchy.RemoveLast(); 
                            }
                        }

                        if (Pass)
                            LogConsole("OK".EscapeAndAddStyles(okStyle));
                        
                        LogHierarchy.RemoveLast();
                    }
                    LogHierarchy.RemoveLast();
                }

                //output parameters
                if (step.Results.OutputParameters is not null)
                {
                    LogHierarchy.Add("Output Parameters");
                    foreach (var expectedResult in step.Results.OutputParameters)
                    {
                        var currentResult = expectedResult.Result;
                        LogHierarchy.Add($"{expectedResult.Name}".EscapeAndAddStyles(objectStyle_l3));

                        if (currentResult is null)
                        {
                            LogConsoleError("Null output".EscapeAndAddStyles(errorStyle));
                            Pass = false;
                            LogHierarchy.RemoveLast();
                            continue;
                        }

                        if (expectedResult.Value is not null)
                        {
                            var parameterType = Utils.MapQestTypeToInternal(expectedResult.Type);
                            if (!Convert.ChangeType(expectedResult.Value, parameterType).Equals(Convert.ChangeType(currentResult, parameterType)))
                            {
                                LogConsoleError($"{currentResult} != {expectedResult.Value}".EscapeAndAddStyles(errorStyle));
                                Pass = false;
                                LogHierarchy.RemoveLast();
                                continue;
                            }
                        }

                        LogConsole($"{expectedResult.Value} == {currentResult}".EscapeAndAddStyles(okStyle));
                        LogHierarchy.RemoveLast();

                    }
                    LogHierarchy.RemoveLast();
                }

                //returncode
                if (step.Results.ReturnCode.HasValue)
                {
                    LogHierarchy.Add("Return Code".EscapeAndAddStyles(objectStyle_l3));
                    var expectedResult = step.Results.ReturnCode.Value;
                    var currentResult = step.Results.ReturnCodeResult;

                    if (step.Results.ReturnCodeResultException is not null)
                    {
                        LogException(step.Results.ReturnCodeResultException);
                        Pass = false;
                    }
                    else
                    {

                        if (!currentResult.HasValue)
                        {
                            LogConsoleError("Null output".EscapeAndAddStyles(errorStyle));
                            Pass = false;
                        }
                        else
                        {
                            if (expectedResult != Convert.ToInt32(currentResult.Value))
                            {
                                LogConsoleError($"{currentResult.Value} != {expectedResult}".EscapeAndAddStyles(errorStyle));
                                Pass = false;
                            }
                            else
                                LogConsole($"{expectedResult} == {currentResult.Value}".EscapeAndAddStyles(okStyle));
                        }
                    }
                    LogHierarchy.RemoveLast();
                }
            }

            // asserts
            if (step.Asserts != null)
            {
                LogHierarchy.Add("Asserts");
                foreach (var assert in step.Asserts)
                {
                    var assertSqlQuery = assert.SqlQuery.ReplaceVars(Test.Variables);
                    var assertScalarValue = assert.ScalarValue.ReplaceVarsInParameter(Test.Variables);
                    var currentResult = assert.Result;
                    LogHierarchy.Add(assertSqlQuery.EscapeAndAddStyles(objectStyle_l3));

                    if (assert.ResultException is not null)
                    {
                        LogException(assert.ResultException);
                        Pass = false;
                        LogHierarchy.RemoveLast();
                        continue;
                    }

                    if (currentResult is null)
                    {
                        LogConsoleError($"NULL != {assertScalarValue}".EscapeAndAddStyles(errorStyle));
                        Pass = false;
                    }
                    else
                    {
                        bool convertOk = false;
                        var scalarType = Utils.MapQestTypeToInternal(assert.ScalarType);
                        convertOk = Convert.ChangeType(assertScalarValue, scalarType).Equals(Convert.ChangeType(currentResult, scalarType));

                        if (convertOk)
                            LogConsole($"{currentResult} == {assertScalarValue}".EscapeAndAddStyles(okStyle));
                        else
                        {
                            LogConsoleError($"{currentResult} != {assertScalarValue}".EscapeAndAddStyles(errorStyle));
                            Pass = false;
                        }
                    }
                    LogHierarchy.RemoveLast();
                }
                LogHierarchy.RemoveLast();
            }
        }

        private void EvaluateScript(string scope, Scripts scripts)
        {
            string objName = scope.EscapeAndAddStyles(objectStyle_l2);
            LogHierarchy.Add(objName);

            if (scripts.Result)
            {
                if (Verbose)
                    for (int i = 0; i < scripts.ActualScripts?.Count; i++)
                    {
                        string? item = scripts.ActualScripts[i];
                        LogConsole(item.EscapeAndAddStyles(objectStyle_l3));
                    }

                LogConsole("OK".EscapeAndAddStyles(okStyle));
            }
            else
            {
                for (int i = 0; i < scripts.ActualScripts?.Count; i++)
                {
                    string? item = scripts.ActualScripts[i];
                    LogConsoleError(item.EscapeAndAddStyles(objectStyle_l3));
                }

                LogException(scripts.ResultException!);
                Pass = false;
            }
            LogHierarchy.RemoveLast();
        }

        private void LogException(Exception ex)
        {
            string exTitle = "Exception".EscapeAndAddStyles(errorStyle);
            string prfx = string.Join(consoleSeparator, LogHierarchy);
            AnsiConsole.MarkupLine($"{prfx}{consoleSeparator}{exTitle}");
            AnsiConsole.WriteException(ex);
        }

        private void LogConsole(string message, bool forceOutput = false)
        {
            if (Verbose || forceOutput)
            {
                string prfx = string.Join(consoleSeparator, LogHierarchy);
                AnsiConsole.MarkupLine($"{prfx}{consoleSeparator}{message}");
            }
        }

        private void LogConsoleError(string message) => LogConsole(message, true);

    }

    file static class ExtensionMethods
    {
        internal static void RemoveLast<T>(this List<T> list)
        {
            if (list.Count > 0)
                list.RemoveAt(list.Count - 1);
            else
                throw new System.NotSupportedException();
        }
    }
}