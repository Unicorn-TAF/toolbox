using System;
using System.IO;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Engine.Configuration;
using Unicorn.Taf.Core.Utility;

namespace Unicorn.ConsoleRunner
{
    public class Program
    {

        private Program()
        {
        }

        public static void Main(string[] args)
        {
            Program app = new Program();

            ArgsParser parser = new ArgsParser();
            parser.ParseArguments(args);

            app.Run(parser.AssemblyPath, parser.PropertiesPath, parser.TrxFileName);
        }

        private void Run(string assemblyPath, string propertiesPath, string trxFileName)
        {
            Config.FillFromFile(propertiesPath);
            Reporter.ReportHeader(assemblyPath);
            LaunchOutcome outcome = null;
            AppDomain unicornDomain = AppDomain.CreateDomain("Unicorn app domain");

            try
            {
                string testAssemblyDir = Path.GetDirectoryName(assemblyPath);
                Type runnerType = typeof(IsolatedTestsRunner);
                string pathToDll = Path.Combine(testAssemblyDir, runnerType.Assembly.GetName().Name + ".dll");

                IsolatedTestsRunner myObject = (IsolatedTestsRunner)unicornDomain
                    .CreateInstanceFromAndUnwrap(pathToDll, runnerType.FullName);

                outcome = myObject.RunTests(assemblyPath, propertiesPath);

                if (!string.IsNullOrEmpty(trxFileName))
                {
                    new TrxCreator().GenerateTrxFile(outcome, trxFileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running tests ({ex.Message})");
            }

            AppDomain.Unload(unicornDomain);

            if (outcome != null)
            {
                Reporter.ReportResults(outcome);
            }
        }
    }
}
