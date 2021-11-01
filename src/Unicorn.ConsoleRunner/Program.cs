using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Engine.Configuration;
using Unicorn.Taf.Core.Testing;
using Unicorn.Taf.Core.Utility;

namespace Unicorn.ConsoleRunner
{
    public class Program
    {
        private const string ConstTestAssembly = "--test-assembly";
        private const string ConstConfiguration = "--config";
        private const string ConstTrx = "--trx";
        private const string ConstHelp = "--help";
        private readonly string _delimiter = new string('-', Console.WindowWidth);

        private Program()
        {
        }

        public static void Main(string[] args)
        {
            var app = new Program();

            if (args.Length == 0)
            {
                app.PrintHelpText();
                throw new ArgumentException("Required arguments were not specified");
            }

            if (args[0].Equals(ConstHelp))
            {
                app.PrintHelpText();
                return;
            }

            string assemblyPath;
            string propertiesPath;
            string trxFileName = null;

            var assemblyArgs = args.Where(a => a.Trim().StartsWith(ConstTestAssembly));

            if (assemblyArgs.Any())
            {
                assemblyPath = assemblyArgs.First().Trim().Split('=')[1].Trim();
            }
            else
            {
                app.PrintHelpText();
                throw new ArgumentException($"'{ConstTestAssembly}' argument was not specified");
            }

            var configArgs = args.Where(a => a.Trim().StartsWith(ConstConfiguration));

            if (configArgs.Any())
            {
                propertiesPath = configArgs.First().Trim().Split('=')[1].Trim();
            }
            else
            {
                app.PrintHelpText();
                throw new ArgumentException($"'{ConstConfiguration}' argument was not specified");
            }

            var trxArgs = args.Where(a => a.Trim().StartsWith(ConstTrx));

            if (trxArgs.Any())
            {
                trxFileName = trxArgs.First().Trim().Split('=')[1].Trim();
            }

            app.Run(assemblyPath, propertiesPath, trxFileName);
        }

        private void Run(string assemblyPath, string propertiesPath, string trxFileName)
        {
            var assemblyUri = Path.IsPathRooted(assemblyPath) ?
                new Uri(assemblyPath, UriKind.Absolute) :
                new Uri(assemblyPath, UriKind.Relative);

            var configUri = Path.IsPathRooted(propertiesPath) ?
                new Uri(propertiesPath, UriKind.Absolute) :
                new Uri(propertiesPath, UriKind.Relative);

            Config.FillFromFile(configUri.IsAbsoluteUri ? configUri.AbsolutePath : configUri.ToString());
            ReportHeader(assemblyPath);

            LaunchOutcome outcome = null;

            try
            {
                var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                using (var executor = new UnicornAppDomainIsolation<IsolatedTestsRunner>(location))
                {
                    outcome = executor.Instance.RunTests(
                        assemblyUri.IsAbsoluteUri ? assemblyUri.AbsolutePath : assemblyUri.ToString(),
                        configUri.IsAbsoluteUri ? configUri.AbsolutePath : configUri.ToString());
                }

                if (!string.IsNullOrEmpty(trxFileName))
                {
                    new TrxCreator().GenerateTrxFile(outcome, trxFileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running tests ({ex.Message})");
            }

            ReportResults(outcome);
        }

        private void ReportResults(LaunchOutcome outcome)
        {
            var header = new StringBuilder()
                .AppendLine("\n\n")
                .AppendLine(_delimiter)
                .AppendLine($"Tests run {outcome.RunStatus}")
                .AppendLine();

            Console.ForegroundColor = outcome.RunStatus.Equals(Status.Passed) ? 
                ConsoleColor.Green : 
                ConsoleColor.Red;

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

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write($"Passed tests: {passedTests}    ");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write($"Failed tests: {failedTests}    ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Skipped tests: {skippedTests}");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($" Total tests: {passedTests + failedTests + skippedTests}");
        }

        private void ReportHeader(string assemblyPath)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;

            Console.WriteLine(ResourceAsciiLogo.Logo);
            Console.WriteLine();

            Console.WriteLine(_delimiter);
            Console.WriteLine();
            Console.WriteLine("Tests assembly: " + assemblyPath);
            Console.WriteLine();
            Console.WriteLine(Config.GetInfo());
            Console.WriteLine(_delimiter);
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
        }

        private void PrintHelpText()
        {
            var help = new StringBuilder()
                .AppendLine("Required arguments:")
                .AppendLine($"    {ConstTestAssembly}=PATH_TO_TEST_ASSEMBLY")
                .AppendLine($"    {ConstConfiguration}=PATH_TO_CONFIGURATION_FILE")
                .AppendLine()
                .AppendLine("Optional arguments:")
                .AppendLine($"    {ConstTrx}=TRX_FILE_NAME        trx is not generated by default");


            Console.WriteLine(help.ToString());
        }
    }
}
