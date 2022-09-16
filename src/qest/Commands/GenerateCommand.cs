using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
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

                Test currentTest = new();
                currentTest.Name = $"{currentSchema}.{currentSp}";
                currentTest.Command = new();
                currentTest.Command.Parameters = new List<TestCommandParameter>();
                currentTest.Command.CommandText = $"EXEC {currentTest.Name}";
                currentTest.Results = new ResultGroup();
                currentTest.Results.OutputParameters = new List<OutputParameter>();

                while (await reader.ReadAsync())
                {
                    string rowSchema = reader.GetString(0);
                    string rowSp = reader.GetString(1);

                    if (currentSchema != rowSchema || currentSp != rowSp)
                    {
                        await SafeWriteYamlAsync(folder!, currentTest);

                        currentSchema = rowSchema;
                        currentSp = rowSp;

                        currentTest = new();
                        currentTest.Name = $"{currentSchema}.{currentSp}";
                        currentTest.Command = new();
                        currentTest.Command.Parameters = new List<TestCommandParameter>();
                        currentTest.Command.CommandText = $"EXEC {currentTest.Name}";
                        currentTest.Results = new ResultGroup();
                        currentTest.Results.OutputParameters = new List<OutputParameter>();
                    }

                    var parameterName = reader.GetString(3);
                    var parameterType = reader.GetString(4);
                    var isOutput = reader.GetBoolean(5);

                    if (isOutput)
                    {
                        var outputPar = new OutputParameter();
                        outputPar.Name = parameterName[1..];
                        outputPar.Value = "?";
                        outputPar.Type = Test.MapType(parameterType);

                        currentTest.Results.OutputParameters.Add(outputPar);
                    }
                    else
                    {
                        var inputPar = new TestCommandParameter();
                        inputPar.Name = parameterName[1..];
                        inputPar.Type = Test.MapType(parameterType);

                        currentTest.Command.Parameters.Add(inputPar);
                    }
                }

                await SafeWriteYamlAsync(folder!, currentTest);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }

        static async Task SafeWriteYamlAsync(DirectoryInfo folder, Test testTemplate)
        {
            FileInfo output = new(Path.Combine(folder.Name, $"{testTemplate.Name}.yml"));

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            try
            {
                using var stream = new StreamWriter(output.FullName);
                string yaml = serializer.Serialize(testTemplate);
                await stream.WriteAsync(yaml);

                Console.WriteLine($"Created template {output.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating template for {output.Name}: {ex.Message}");
            }
        }
    }
}
