using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using qest.Models;
using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace qest
{
    internal static partial class Utils
    {
        internal static List<Test> SafeReadYaml(FileInfo file) => SafeReadYamlAsync(file).GetAwaiter().GetResult();
        internal static async Task<List<Test>> SafeReadYamlAsync(FileInfo file)
        {
            List<Test> list = new List<Test>();

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            try
            {
                using var stream = new StreamReader(file.FullName);
                string yaml = await stream.ReadToEndAsync();
                list.AddRange(deserializer.Deserialize<List<Test>>(yaml));
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"[bold red]Error deserializing {file.FullName}[/]");
                AnsiConsole.WriteException(ex);
            }
            return list;
        }

        internal static async Task SafeWriteYamlAsync(DirectoryInfo folder, Test testTemplate)
        {
            FileInfo output = new(Path.Combine(folder.Name, $"{testTemplate.Name}.yml"));

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            try
            {
                await using var stream = new StreamWriter(output.FullName, false);

                string yaml = serializer.Serialize(new Test[] { testTemplate });
                await stream.WriteLineAsync(yamlSchema);
                await stream.WriteAsync(yaml);
                AnsiConsole.MarkupLine($"Created Template: {folder.Name}/{testTemplate.Name.EscapeAndAddStyles("blue")}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"[bold red]Error serializing {output.FullName}[/]");
                AnsiConsole.WriteException(ex);
            }
        }
    }
}
