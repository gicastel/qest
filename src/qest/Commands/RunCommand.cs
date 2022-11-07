using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using qest.Models;
using qest.Connectors;
using qest.Visualizers;
using Spectre.Console;
using Spectre.Console.Cli;
namespace qest.Commands
{

    internal sealed class RunCommand : AsyncCommand<RunCommand.Settings>
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

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {

            List<Test> TestCollection = new();

            if (settings.File is not null)
            {
                FileInfo fileToLoad = new FileInfo(settings.File);

                TestCollection.AddRange(await Utils.SafeReadYamlAsync(fileToLoad));
            }
            else if (settings.Folder is not null)
            {
                DirectoryInfo folderToLoad = new DirectoryInfo(settings.Folder);
                foreach (var item in folderToLoad.EnumerateFiles().Where(f => f.Extension == ".yml" || f.Extension == ".yaml"))
                    TestCollection.AddRange(await Utils.SafeReadYamlAsync(item));
            }
          
            IVisualizer visualizer;
            visualizer = new ConsoleVisualizer<MsSqlConnector>(TestCollection, settings.ConnectionString);

            int exitCode = await visualizer.RunAllAsync();
            return exitCode;
        }
    }
}
