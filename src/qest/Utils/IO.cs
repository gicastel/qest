using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using qest.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace qest
{
    internal static partial class Utils
    {
        internal static List<Test> SafeReadYaml(FileInfo file)
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
                Console.WriteLine($"Error deserializing {file.FullName}: {ex.Message}");
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
