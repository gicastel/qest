using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using qest.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace qest
{
    internal static class Utils
    {
        internal static Type MapType(SqlDbType type) => type switch
        {
            SqlDbType.Bit => typeof(bool),
            SqlDbType.TinyInt => typeof(byte),
            SqlDbType.SmallInt => typeof(short),
            SqlDbType.BigInt => typeof(long),
            SqlDbType.Float => typeof(double),
            SqlDbType.Int => typeof(int),
            SqlDbType.NVarChar => typeof(string),
            SqlDbType.DateTime => typeof(DateTime),
            SqlDbType.DateTime2 => typeof(DateTime),
            SqlDbType.Date => typeof(DateTime),
            SqlDbType.DateTimeOffset => typeof(DateTimeOffset),
            SqlDbType.Time => typeof(TimeSpan),
            SqlDbType.Real => typeof(float),
            SqlDbType.Decimal => typeof(decimal),
            SqlDbType.Money => typeof(decimal),

            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Type not expected: {type}"),
        };

        internal static SqlDbType MapSqlType(string type) => type switch
        {
            "bit" => SqlDbType.Bit,
            "tinyint" => SqlDbType.TinyInt,
            "smallint" => SqlDbType.SmallInt,
            "bigint" => SqlDbType.BigInt,
            "float" => SqlDbType.Float,
            "int" => SqlDbType.Int,
            "nvarchar" => SqlDbType.NVarChar,
            "datetime" => SqlDbType.DateTime,
            "datetime2" => SqlDbType.DateTime2,
            "date" => SqlDbType.Date,
            "datetimetoffset" => SqlDbType.DateTimeOffset,
            "time" => SqlDbType.Time,
            "real" => SqlDbType.Real,
            "decimal" => SqlDbType.Decimal,
            "money" => SqlDbType.Decimal,

            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Type not expected: {type}"),
        };
        internal static object? ReplaceVarsInParameter(this object value, Dictionary<string, object>? variables)
        {
            if (variables != null && value is string stringValue)
            {
                var result = stringValue.ReplaceVars(variables);
                return result != "NULL" ? result : null;
            }
            else
            {
                return value;
            }
        }

        internal static string ReplaceVars(this string value, Dictionary<string, object>? variables)
        {
            if (variables != null)
            {
                return variables.Aggregate(value, (acc, var) => acc.Replace($"{{{var.Key}}}", var.Value?.ToString() ?? "NULL"));
            }
            else
            {
                return value;
            }
        }

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
        
        internal const string ConnectionStringBarebone = "Server=;Initial Catalog=;User Id=;Password=;";

        internal const string yamlSchema = 
            @"# yaml-language-server: $schema=https://raw.githubusercontent.com/Geims83/qest/0.9.2/docs/yamlSchema.json";
        
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
