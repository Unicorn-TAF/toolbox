using System;
using System.Text;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core;
using Unicorn.Taf.Core.Testing;
using System.Linq;

namespace Unicorn.ConsoleRunner
{
    internal static class Reporter
    {
        private static readonly string Delimiter = new string('-', Console.WindowWidth);

        internal static void ReportResults(LaunchOutcome outcome)
        {
            TimeSpan ts = DateTime.Now - outcome.StartTime;

            StringBuilder header = new StringBuilder()
                .AppendLine("\n\n")
                .AppendLine(Delimiter)
                .AppendFormat("Tests run {0}", outcome.RunStatus)
                .AppendFormat(" (duration {0}h {1}m {2}s {3}ms)\n", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds)
                .AppendLine();

            Console.ForegroundColor = outcome.RunStatus.Equals(Status.Passed) ? 
                ConsoleColor.Green :
                ConsoleColor.Red;

            Console.Write(header.ToString());

            int passedTests = outcome.SuitesOutcomes.Sum(o => o.PassedTests);
            int skippedTests = outcome.SuitesOutcomes.Sum(o => o.SkippedTests);
            int failedTests = outcome.SuitesOutcomes.Sum(o => o.FailedTests);

            Console.ForegroundColor = passedTests == 0 ? 
                ConsoleColor.DarkGray : 
                ConsoleColor.DarkGreen;

            Console.Write($"Passed tests: {passedTests}    ");

            Console.ForegroundColor = failedTests == 0 ? 
                ConsoleColor.DarkGray : 
                ConsoleColor.DarkRed;

            Console.Write($"Failed tests: {failedTests}    ");

            Console.ForegroundColor = skippedTests == 0 ? 
                ConsoleColor.DarkGray : 
                ConsoleColor.DarkYellow;

            Console.WriteLine($"Skipped tests: {skippedTests}");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"-----> Total: {passedTests + failedTests + skippedTests}");
        }

        internal static void ReportHeader(string assemblyPath, bool hideLogo)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            
            if (!hideLogo)
            {
                Console.WriteLine(ResourceAsciiLogo.Logo);
                Console.WriteLine();

                Console.WriteLine(Delimiter);
                Console.WriteLine();
            }
            
            Console.WriteLine("Tests assembly: " + assemblyPath);
            Console.WriteLine();
            Console.WriteLine(Config.GetInfo());
            Console.WriteLine(Delimiter);
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
