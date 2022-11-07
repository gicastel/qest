using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using qest.Models;
using qest.Connectors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace qest.Commands
{

    internal sealed class GenerateCommand : AsyncCommand<GenerateCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Folder containing test files")]
            [CommandOption("-d|--folder <FOLDER>")]
            [DefaultValue("templates")]
            public string Folder { get; init; }

            [Description("Target connection string")]
            [CommandOption("-c|--tcs <TARGETCONNECTIONSTRING>")]
            public string ConnectionString { get; init; }

            public override ValidationResult Validate()
            {
                DirectoryInfo folderToLoad = new DirectoryInfo(Folder);
                if (!folderToLoad.Exists)
                    folderToLoad.Create();

                var result = Validators.ValidateConnectionString(ConnectionString);
                
                return result;
            }
        }

        internal const string parametersQuery = @"
                 SELECT
                    SCHEMA_NAME(SCHEMA_ID) AS [Schema],
                    o.name AS [ObjectName],
                    p.parameter_id AS [ParameterID],
                    p.name AS [ParameterName],
                    TYPE_NAME(p.user_type_id) AS [ParameterDataType],
                    p.is_output AS [IsOutputParameter]
                FROM sys.objects AS o
                INNER JOIN sys.parameters AS p
                    ON o.OBJECT_ID = P.OBJECT_ID
                WHERE
                    o.type = 'P'
                ORDER BY
                    [Schema],
                    o.name,
                    p.parameter_id
                ";

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var sqlConnection = new SqlConnection(settings.ConnectionString);
            DirectoryInfo folder = new DirectoryInfo(settings.Folder);

            try
            {
                await sqlConnection.OpenAsync();

                SqlCommand parametersList = new(parametersQuery, sqlConnection);
                using var reader = parametersList.ExecuteReader();

                await reader.ReadAsync();

                string currentSchema = reader.GetString(0);
                string currentSp = reader.GetString(1);

                Test currentTest = GenerateNewTest(currentSchema, currentSp);

                while (await reader.ReadAsync())
                {
                    string rowSchema = reader.GetString(0);
                    string rowSp = reader.GetString(1);

                    if (currentSchema != rowSchema || currentSp != rowSp)
                    {
                        await Utils.SafeWriteYamlAsync(folder!, currentTest);

                        currentSchema = rowSchema;
                        currentSp = rowSp;

                        currentTest = GenerateNewTest(rowSchema, rowSp);
                    }

                    var currentStep = currentTest.Steps.First();

                    var parameterName = reader.GetString(3);
                    var parameterType = reader.GetString(4);
                    var isOutput = reader.GetBoolean(5);

                    if (isOutput)
                    {
                        currentStep.Results.OutputParameters ??= new();
                        var outputPar = new OutputParameter();
                        outputPar.Name = parameterName[1..];
                        outputPar.Value = "?";
                        outputPar.Type = MsSqlConnector.MapStringToQestType(parameterType);

                        currentStep.Results.OutputParameters.Add(outputPar);
                    }
                    else
                    {
                        currentStep.Command.Parameters!.Add(parameterName[1..], parameterType);
                    }
                }

                await Utils.SafeWriteYamlAsync(folder!, currentTest);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                return 1;
            }

            return 0;
        }

        private static Test GenerateNewTest(string schemaName, string spName)
        {
            Test currentTest = new();
            currentTest.Steps = new();
            TestStep currentStep = new();
            currentTest.Steps.Add(currentStep);
            TestCommand currentCommand = new();
            currentCommand.Parameters = new();
            currentStep.Command = currentCommand;
            currentStep.Results = new ResultGroup();

            string completeName = $"{schemaName}.{spName}";
            currentTest.Name = $"{completeName}";
            currentStep.Name = $"Template for {completeName} test";
            currentCommand.CommandText = $"[{schemaName}].[{spName}]";

            return currentTest;
        }
    }
}
