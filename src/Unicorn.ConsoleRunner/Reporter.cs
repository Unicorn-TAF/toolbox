using System;
using System.Text;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.ConsoleRunner
{
    internal static class Reporter
    {
        private static readonly string Delimiter = new string('-', Console.WindowWidth);

        internal static void ReportResults(LaunchOutcome outcome)
        {
            StringBuilder header = new StringBuilder()
                .AppendLine("\n\n")
                .AppendLine(Delimiter)
                .AppendLine($"Tests run {outcome.RunStatus}")
                .AppendLine();

            Console.ForegroundColor = outcome.RunStatus.Equals(Status.Passed) ?
                ConsoleColor.Green :
                ConsoleColor.Red;

            Console.Write(header.ToString());

            int passedTests = 0;
            int skippedTests = 0;
            int failedTests = 0;

            foreach (SuiteOutcome suiteOutcome in outcome.SuitesOutcomes)
            {
                failedTests += suiteOutcome.FailedTests;
                skippedTests += suiteOutcome.SkippedTests;
                passedTests += suiteOutcome.PassedTests;
            }

            Console.ForegroundColor = passedTests == 0 ? ConsoleColor.DarkGray : ConsoleColor.DarkGreen;
            Console.Write($"Passed tests: {passedTests}    ");

            Console.ForegroundColor = failedTests == 0 ? ConsoleColor.DarkGray : ConsoleColor.DarkRed;
            Console.Write($"Failed tests: {failedTests}    ");

            Console.ForegroundColor = skippedTests == 0 ? ConsoleColor.DarkGray : ConsoleColor.DarkYellow;
            Console.WriteLine($"Skipped tests: {skippedTests}");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($" Total tests: {passedTests + failedTests + skippedTests}");
        }

        internal static void ReportHeader(string assemblyPath)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;

            Console.WriteLine(ResourceAsciiLogo.Logo);
            Console.WriteLine();

            Console.WriteLine(Delimiter);
            Console.WriteLine();
            Console.WriteLine("Tests assembly: " + assemblyPath);
            Console.WriteLine();
            Console.WriteLine(Config.GetInfo());
            Console.WriteLine(Delimiter);
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
