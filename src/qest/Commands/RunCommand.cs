using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using TestBase;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace qest.Commands
{
    internal static class RunTests
    {
        internal static void Run(FileInfo? file, DirectoryInfo? folder, string tcs)
        {
            List<Test> TestCollection = new List<Test>();

            if (file is not null)
            {
                TestCollection.AddRange(SafeReadYaml(file));
            }
            else if (folder is not null)
            {
                foreach (var item in folder.EnumerateFiles().Where(f => f.Extension == ".yml" || f.Extension == ".yaml"))
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
