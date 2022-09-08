using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TestBase;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace qest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            Option<FileInfo?> fileOption = new (
               name: "--file",
               description: "A YML test file.",
                parseArgument: result =>
                {
                    if (result.Tokens.Any())
                    {
                        FileInfo file = new (result.Tokens.First().Value);
                        if (file.Exists)
                            return file;
                    }

                    result.ErrorMessage = "Please specify a YML file.";
                    return null;
                }
            );
            fileOption.Arity = ArgumentArity.ZeroOrOne;

            Option<DirectoryInfo?> folderOption = new (
                   name: "--folder",
                   description: "A folder containing YML test files.",
                   parseArgument: result =>
                    {
                        if (result.Tokens.Any())
                        {
                            DirectoryInfo dir = new (result.Tokens.First().Value);
                            if (dir.Exists)
                                return dir;
                        }

                        result.ErrorMessage = "Please specify a valid folder.";
                        return null;
                    });
            folderOption.Arity = ArgumentArity.ZeroOrOne;

            Option<string> tcsOption = new (
                   name: "--tcs",
                   description: "MSSQL Server target connectionstring"
            );
            tcsOption.IsRequired = true;
            tcsOption.Arity = ArgumentArity.ExactlyOne;

            RootCommand rootCommand = new ("Simple, cross platform, command line tool to test MSSQL procedures");

            rootCommand.AddOption(fileOption);
            rootCommand.AddOption(folderOption);
            rootCommand.AddOption(tcsOption);

            rootCommand.SetHandler((file, folder, tcs) =>
                {
                    Run(file, folder, tcs);
                },
                fileOption, folderOption, tcsOption);

            await rootCommand.InvokeAsync(args);
        }

        static void Run(FileInfo? file, DirectoryInfo? folder, string tcs)
        {
            List<Test> TestCollection = new List<Test>();

            if (file is not null)
            {
                TestCollection.AddRange(SafeReadYaml(file));
            }
            else if (folder is not null)
            {
                foreach (var item in folder.EnumerateFiles().Where(f => (f.Extension == ".yml") || (f.Extension == ".yaml")))
                    TestCollection.AddRange(SafeReadYaml(item));
            }
            else
            {
                Console.WriteLine("One parameter between --file or --folder is mandatory.");
                Environment.Exit(1);
            }

            if (TestCollection.Count == 0)
            {
                Console.WriteLine("No test loaded");
                Environment.Exit(1);
            }

            var sqlConnection = new SqlConnection(tcs);

            foreach (var test in TestCollection)
            {
                test.Connection = sqlConnection;
                bool pass = test.Run();
                if (!pass)
                    Environment.Exit(1);
            }
            Environment.Exit(0);
        }

        static List<Test> SafeReadYaml(FileInfo file)
        {
            List<Test> list = new List<Test>();

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            try
            {
                using var stream = new StreamReader(file.FullName);
                string yaml = stream.ReadToEnd();
                list.AddRange(deserializer.Deserialize<List<Test>>(yaml));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing {file.FullName}: {ex.Message}");
            }
            return list;
        }
    }
}