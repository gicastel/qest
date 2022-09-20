using qest.Commands;
using System.CommandLine;
using System.Threading.Tasks;

namespace qest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            RootCommand rootCommand = new("Simple, cross platform, command line tool to test MSSQL procedures");

            rootCommand.AddCommand(CreateRunCommand());
            rootCommand.AddCommand(CreateGenerateCommand());

            await rootCommand.InvokeAsync(args);
        }

        internal static Command CreateRunCommand()
        {
            Command runCommand = new("run", "Run tests defined in YML files.");

            var tcs = Options.ConnectionStringOption();
            var folder = Options.FolderOption();
            var file = Options.FileOption();
          
            runCommand.AddOption(tcs);
            runCommand.AddOption(folder);
            runCommand.AddOption(file);

            runCommand.SetHandler((file, folder, tcs) =>
                {
                    RunTests.Run(file, folder, tcs);
                },
                file, folder, tcs);

            return runCommand;
        }


        internal static Command CreateGenerateCommand()
        {
            var tcs = Options.ConnectionStringOption();
            var folder = Options.FolderOption();
            folder.SetDefaultValue("templates");

            Command generateCommand = new("generate", "Generates YML templates from Stored Procedures.");

            generateCommand.AddOption(tcs);
            generateCommand.AddOption(folder);

            generateCommand.SetHandler(async(folder, tcs) =>
            {
                await Generate.Run(folder, tcs);
            },
                folder, tcs);

            return generateCommand;
        }

    }
}