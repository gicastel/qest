using System.CommandLine;
using System.IO;
using System.Linq;

namespace qest.Commands
{
    internal static class Options
    {
        internal static Option<DirectoryInfo?> FolderOption()
        {
            Option<DirectoryInfo?> folderOption = new(
                name: "--folder",
                description: "The destination folder for the templates.",
                parseArgument: result =>
                {
                    if (result.Tokens.Any())
                    {
                        DirectoryInfo dir = new(result.Tokens.First().Value);
                        if (dir.Exists)
                        {
                            return dir;
                        }
                    }

                    result.ErrorMessage = "Please specify a valid folder.";
                    return null;
                });
            folderOption.Arity = ArgumentArity.ZeroOrOne;
            folderOption.AddAlias("-d");

            return folderOption;
        }


        internal static Option<FileInfo?> FileOption()
        {
            Option<FileInfo?> fileOption = new(
                name: "--file",
                description: "A YML test file.",
                parseArgument: result =>
                {
                    if (result.Tokens.Any())
                    {
                        FileInfo file = new(result.Tokens.First().Value);
                        if (file.Exists)
                            return file;
                    }

                    result.ErrorMessage = "Please specify a YML file.";
                    return null;
                });
            fileOption.Arity = ArgumentArity.ZeroOrOne;
            fileOption.AddAlias("-f");

            return fileOption;
        }

        internal static Option<string> ConnectionStringOption()
        {
            Option<string> tcsOption = new(
               name: "--tcs",
               description: "MSSQL Server target connectionstring."
           );
            tcsOption.IsRequired = true;
            tcsOption.Arity = ArgumentArity.ExactlyOne;
            tcsOption.AddAlias("-c");

            return tcsOption;
        }
    }
}
