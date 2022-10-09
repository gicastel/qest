using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using qest.Models;
using Spectre.Console;
using Spectre.Console.Cli;
namespace qest.Commands
{

    internal sealed class RunCommand : Command<RunCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Test file")]
            [CommandOption("-f|--file <FILE>")]
            public string? File { get; init; }

            [Description("Folder containing test files")]
            [CommandOption("-d|--folder <FOLDER>")]
            public string? Folder { get; init; }

            [Description("Target connection string")]
            [CommandOption("-c|--tcs <TARGETCONNECTIONSTRING>")]
            public string ConnectionString { get; init; }

            public override ValidationResult Validate()
            {
                if (Folder is null && File is null)
                {
                    return ValidationResult.Error("One parameter between FILE or FOLDER must be supplied.");
                }

                List<Test> testsToRun = new();

                if (File is not null)
                {
                    FileInfo fileToLoad = new FileInfo(File);
                    if (!fileToLoad.Exists)
                        return ValidationResult.Error("File specified does not exist.");
                    else
                        testsToRun.AddRange(Utils.SafeReadYaml(fileToLoad));
                }

                if (Folder is not null)
                {
                    DirectoryInfo folderToLoad = new DirectoryInfo(Folder);
                    if (!folderToLoad.Exists)
                        return ValidationResult.Error("Folder specified does not exist.");
                    else
                        foreach (var fileToLoad in folderToLoad.EnumerateFiles().Where(f => f.Extension == ".yml" || f.Extension == ".yaml"))
                            testsToRun.AddRange(Utils.SafeReadYaml(fileToLoad));
                }

                if (!testsToRun.Any())
                    return ValidationResult.Error("No tests found in file or folder.");

                var result = Validators.ValidateConnectionString(ConnectionString);

                return result;
            }

        }

        private List<Test>? TestCollection;

        private List<(string Message, bool? IsError)>? Report;

        private Tree? TestTree;

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {

            TestCollection = new();
            Report = new();
            TestTree = new Tree("quest");


             if (settings.File is not null)
            {
                    FileInfo fileToLoad = new FileInfo(settings.File);

                TestCollection.AddRange(Utils.SafeReadYaml(fileToLoad));
            }
            else if (settings.Folder is not null)
            {
                DirectoryInfo folderToLoad = new DirectoryInfo(settings.Folder);
                foreach (var item in folderToLoad.EnumerateFiles().Where(f => f.Extension == ".yml" || f.Extension == ".yaml"))
                    TestCollection.AddRange(Utils.SafeReadYaml(item));
            }

            AnsiConsole.MarkupLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [green]{TestCollection.Count} tests loaded.[/]");
            
            var sqlConnection = new SqlConnection(settings.ConnectionString);


            foreach (var test in TestCollection)
            {
                AnsiConsole.Live(TestTree)
                .Start(ctx =>
                {
                    test.Connection = sqlConnection;
                    bool pass = test.Run(TestTree);
                });
            }

            return 0;
        }
    }
}
