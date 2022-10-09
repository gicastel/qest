using qest.Commands;
using Spectre.Console.Cli;
using System.Threading.Tasks;

namespace qest
{
    internal class Program
    {
        const string ExampleCs = "\"Server=WOPR;Initial Catalog=qest;User=StephenFalken;Password=JOSHUA\"";
        static async Task Main(string[] args)
        {
            var app = new CommandApp();
            
            app.Configure(config => 
            {                
                config.SetApplicationName("qest");

                config.AddCommand<RunCommand>("run")
                    .WithDescription("Run tests defined in YML files.")
                    .WithExample(new string[] {"run", "--file", "test.yml", "--tcs", ExampleCs })
                    .WithExample(new string[] {"run", "--folder", "./tests", "--tcs", ExampleCs });

                config.AddCommand<GenerateCommand>("generate")
                    .WithDescription("Generates YML templates from Stored Procedures.")
                    .WithExample(new string[] {"generate", "--folder", "./templates", "--tcs", ExampleCs });
            }
            );

            await app.RunAsync(args);
        }
    }
}