using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Engine.Configuration;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.ConsoleRunner
{
    public class Program
    {
        private const string ConstTestAssembly = "testAssembly";
        private const string ConstConfiguration = "configuration";
        private static readonly string Delimiter = new string('-', 123);

        protected Program()
        {
        }

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelpText();
                throw new ArgumentException("Required parameters were not specified");
            }

            string assemblyPath = null;
            string propertiesPath = null;

            var assemblyArgs = args.Where(a => a.Trim().StartsWith(ConstTestAssembly));

            if (assemblyArgs.Any())
            {
                assemblyPath = assemblyArgs.First().Trim().Split('=')[1].Trim();
            }
            else
            {
                PrintHelpText();
                throw new ArgumentException($"'{ConstTestAssembly}' parameter was not specified");
            }

            var configArgs = args.Where(a => a.Trim().StartsWith(ConstConfiguration));

            if (configArgs.Any())
            {
                propertiesPath = configArgs.First().Trim().Split('=')[1].Trim();
            }
            else
            {
                PrintHelpText();
                throw new ArgumentException($"'{ConstConfiguration}' parameter was not specified");
            }

            Uri assemblyUri = Path.IsPathRooted(assemblyPath) ? 
                new Uri(assemblyPath, UriKind.Absolute) : 
                new Uri(assemblyPath, UriKind.Relative);

            Uri configUri = Path.IsPathRooted(propertiesPath) ?
                new Uri(propertiesPath, UriKind.Absolute) :
                new Uri(propertiesPath, UriKind.Relative);

            Config.FillFromFile(configUri.AbsolutePath);
            ReportHeader(assemblyPath);

            LaunchOutcome outcome = null;

            try
            {
                using (var executor = new UnicornAppDomainIsolation<IsolatedTestsRunner>(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
                {
                    outcome = executor.Instance.RunTests(assemblyUri.AbsolutePath, configUri.AbsolutePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running tests ({ex.Message})");
            }

            ReportResults(outcome);
        }

        private static void ReportResults(LaunchOutcome outcome)
        {
            StringBuilder header = new StringBuilder();

            header.AppendLine().AppendLine().AppendLine().AppendLine()
                .AppendLine(Delimiter).AppendLine()
                .AppendLine($"Tests run {outcome.RunStatus}").AppendLine();

            var color = outcome.RunStatus.Equals(Status.Passed) ? ConsoleColor.Green : ConsoleColor.Red;
            Console.ForegroundColor = color;

            Console.Write(header.ToString());

            int passedTests = 0;
            int skippedTests = 0;
            int failedTests = 0;

            foreach (var suiteOutcome in outcome.SuitesOutcomes)
            {
                failedTests += suiteOutcome.FailedTests;
                skippedTests += suiteOutcome.SkippedTests;
                passedTests += suiteOutcome.PassedTests;
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Passed tests: {passedTests}    Skipped tests: {skippedTests}    Failed tests: {failedTests}");
        }

        private static void ReportHeader(string assemblyPath)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;

            Console.WriteLine(ResourceAsciiLogo.Logo);
            Console.WriteLine();

            Console.WriteLine(Delimiter);
            Console.WriteLine();
            Console.WriteLine("Configuration");
            Console.WriteLine();
            Console.WriteLine("Tests assembly: " + assemblyPath);
            Console.WriteLine(Config.GetInfo());
            Console.WriteLine(Delimiter);
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void PrintHelpText()
        {
            Console.WriteLine("Please specify necessary parameters to run:");
            Console.WriteLine($"{ConstTestAssembly}=<test_assembly_path>");
            Console.WriteLine($"{ConstConfiguration}=<configuration_file_path>");
        }
    }
}
