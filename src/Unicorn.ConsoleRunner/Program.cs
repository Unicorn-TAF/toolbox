using System;
using System.IO;
using System.Linq;
using System.Reflection;
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

#if NETFRAMEWORK
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
#endif

#if NETCOREAPP || NET
        private void Run(string assemblyPath, string propertiesPath, string trxFileName)
        {
            string testAssemblyDir = Path.GetDirectoryName(assemblyPath);
            var alc = new RunnerAssemblyLoadContext(testAssemblyDir);
            alc.Initialize();
            alc.Run(assemblyPath, propertiesPath, trxFileName);
            //Config.FillFromFile(propertiesPath);
            //Reporter.ReportHeader(assemblyPath);
            //LaunchOutcome outcome = null;

            //try
            //{
            //    string testAssemblyDir = Path.GetDirectoryName(assemblyPath);
            //    Type runnerType = typeof(IsolatedTestsRunner);

            //    RunnerAssemblyLoadContext rlc = new RunnerAssemblyLoadContext(testAssemblyDir);
                
            //    Assembly unicornCore = rlc.Assemblies
            //        .First(a => a.GetName().Name.Equals(runnerType.Assembly.GetName().Name));

            //    IsolatedTestsRunner runner = (IsolatedTestsRunner)unicornCore.CreateInstance(runnerType.FullName);

            //    outcome = runner.RunTests(assemblyPath, propertiesPath);

            //    if (!string.IsNullOrEmpty(trxFileName))
            //    {
            //        new TrxCreator().GenerateTrxFile(outcome, trxFileName);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error running tests ({ex.Message})");
            //}

            //if (outcome != null)
            //{
            //    Reporter.ReportResults(outcome);
            //}
        }
#endif
    }
}
