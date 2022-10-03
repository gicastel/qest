using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TestBase;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace qest.Commands
{
    internal static class Generate
    {
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

        internal const string yamlSchema = 
            @"# yaml-language-server: $schema=https://raw.githubusercontent.com/Geims83/qest/main/docs/yamlSchema.json";

        internal static async Task Run(DirectoryInfo? folder, string tcs)
        {
            var sqlConnection = new SqlConnection(tcs);

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
                        await SafeWriteYamlAsync(folder!, currentTest);

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
                        outputPar.Type = Test.MapType(parameterType);

                        currentStep.Results.OutputParameters.Add(outputPar);
                    }
                    else
                    {
                        currentStep.Command.Parameters!.Add(parameterName[1..], parameterType);
                    }
                }

                await SafeWriteYamlAsync(folder!, currentTest);

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
                Console.ResetColor();
            }
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

        static async Task SafeWriteYamlAsync(DirectoryInfo folder, Test testTemplate)
        {
            FileInfo output = new(Path.Combine(folder.Name, $"{testTemplate.Name}.yml"));

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            try
            {
                await using var stream = new StreamWriter(output.FullName, false);

                string yaml = serializer.Serialize(new Test[] {testTemplate});
                await stream.WriteLineAsync(yamlSchema);
                await stream.WriteAsync(yaml);
                
                Console.WriteLine($"Created template {output.Name}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error creating template {output.Name}: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
