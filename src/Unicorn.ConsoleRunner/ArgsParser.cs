using System;
using System.Linq;

namespace Unicorn.ConsoleRunner
{
    internal class ArgsParser
    {
        private const string ConstTestAssembly = "--test-assembly";
        private const string ConstConfiguration = "--config";
        private const string ConstTrx = "--trx";
        private const string ConstHelp = "--help";
        private const string ConstNoLogo = "--no-logo";

        public ArgsParser(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelpText();
                throw new ArgumentException("Required arguments were not specified");
            }

            if (args[0].Equals(ConstHelp))
            {
                PrintHelpText();
                return;
            }

            AssemblyPath = GetArgument(args, ConstTestAssembly, true);
            ConfigPath = GetArgument(args, ConstConfiguration, false);
            TrxFileName = GetArgument(args, ConstTrx, false);

            NoLogo = args.Contains(ConstNoLogo);
        }

        internal string AssemblyPath { get; }

        internal string ConfigPath { get; }

        internal string TrxFileName { get; } = null;

        internal bool NoLogo { get; } = false;

        private void PrintHelpText()
        {
            Console.WriteLine("Required arguments:");
            Console.WriteLine($"    {ConstTestAssembly}   Path to dll with tests");
            Console.WriteLine("");
            Console.WriteLine("Optional arguments:");
            Console.WriteLine($"    {ConstConfiguration}          Path to config (if absent default configuration is used)");
            Console.WriteLine($"    {ConstTrx}             Path to generate TRX (if absent TRX is not generated)");
            Console.WriteLine($"    {ConstNoLogo}         Do not display logo before tests run");
            Console.WriteLine("");
        }

        private string GetArgument(string[] args, string argName, bool required)
        {
            string argument = args.FirstOrDefault(a => a.StartsWith(argName));

            if (!string.IsNullOrEmpty(argument))
            {
                if (argument.Contains("="))
                {
                    return argument.Split('=')[1];
                }

                int valueIndex = Array.IndexOf(args, argument) + 1;

                if (args.Length == valueIndex || args[valueIndex].StartsWith("-"))
                {
                    throw new ArgumentException($"Argument '{argName}' value missed");
                }

                return args[valueIndex];
            }

            if (required)
            {
                throw new ArgumentException($"Required argument '{argName}' missed");
            }

            return null;
        }
    }
}
