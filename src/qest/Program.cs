// See https://aka.ms/new-console-template for more information

using System.Data.SqlClient;
using TestBase;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


if (args.Length == 0)
{
    Console.WriteLine("----------------------------------------------------------------------");
    Console.WriteLine("Welcome!");
    Console.WriteLine("Available parameters:");
    Console.WriteLine("--file       A YAML test file                                Optional*");
    Console.WriteLine("--folder     A folder containing YAML test files             Optional*");

    Console.WriteLine("--tcs        MSSQL Server target connectionstring            Required");
    Console.WriteLine();
    Console.WriteLine("*One of --file or --folder is Required.");
    Environment.Exit(1);
}

List<Test> TestCollection = new List<Test>();
List<string> pars = args.ToList();

if (pars.Contains("--file"))
{
    int idx = pars.IndexOf("--file") + 1;
    if (idx > pars.Count || !File.Exists(pars[idx]))
    {
        // if par index out of range or file not exists
        ExitParNotValid("--file");
    }
    else
        TestCollection.AddRange(SafeReadYaml(pars[idx]));

}
else if (pars.Contains("--folder"))
{
    int idx = pars.IndexOf("--folder") + 1;
    if (idx > pars.Count || !Directory.Exists(pars[idx]))
    {
        // if par index out of range or file not exists
        ExitParNotValid("--folder");
    }
    else
        foreach (var item in Directory.GetFiles(pars[idx], "*.yml"))
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

string ConnectionString = "";

if (pars.Contains("--tcs"))
{
    int idx = pars.IndexOf("--tcs") + 1;
    if (idx < pars.Count)
        ConnectionString = pars[idx];
}

if (ConnectionString.Length == 0)
    ExitParNotValid("--tcs");

var sqlConnection = new SqlConnection(ConnectionString);

foreach (var test in TestCollection)
{
    test.Connection = sqlConnection;
    bool pass = test.Run();
    if (!pass)
        Environment.Exit(1);
}
Environment.Exit(0);

List<Test> SafeReadYaml(string fname)
{
    List<Test> list = new List<Test>();

    var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    try
    {
        using var stream = new StreamReader(fname);
        string yaml = stream.ReadToEnd();
        list.AddRange(deserializer.Deserialize<List<Test>>(yaml));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deserializing {fname}: {ex.Message}");
    }
    return list;
}

void ExitParNotValid(string par)
{
    Console.WriteLine($"Parameter {par} not valid.");
    Environment.Exit(1);
}