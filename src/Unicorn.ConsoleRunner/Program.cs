using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core;
using Unicorn.Taf.Core.Engine;
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
            ArgsParser parser = new ArgsParser();
            parser.ParseArguments(args);
            new Program().Run(parser.AssemblyPath, parser.ConfigPath, parser.TrxFileName);
        }

        private void Run(string assemblyPath, string propertiesPath, string trxFileName)
        {
            if (!string.IsNullOrEmpty(propertiesPath))
            {
                Config.FillFromFile(propertiesPath);
            }

            Reporter.ReportHeader(assemblyPath);

            try
            {
                LaunchOutcome outcome = ExecuteTests(assemblyPath, propertiesPath);

                if (outcome != null)
                {
                    if (!string.IsNullOrEmpty(trxFileName))
                    {
                        new TrxCreator().GenerateTrxFile(outcome, trxFileName);
                    }

                    Reporter.ReportResults(outcome);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running tests ({ex.Message})");
            }
        }

#if NETFRAMEWORK
        private static LaunchOutcome ExecuteTests(string assemblyPath, string propertiesPath)
        {
            AppDomain unicornDomain = AppDomain.CreateDomain("Unicorn.ConsoleRunner AppDomain");

            try
            {
                string pathToDll = Assembly.GetExecutingAssembly().Location;

                AppDomainRunner runner = (AppDomainRunner)unicornDomain
                    .CreateInstanceFromAndUnwrap(pathToDll, typeof(AppDomainRunner).FullName);

                return runner.RunTests(assemblyPath, propertiesPath);
            }
            finally
            {
                AppDomain.Unload(unicornDomain);
            }
        }
#endif

#if NETCOREAPP || NET
        private static LaunchOutcome ExecuteTests(string assemblyPath, string propertiesPath)
        {
            string contextDirectory = Path.GetDirectoryName(assemblyPath);
            
            UnicornAssemblyLoadContext runnerContext = new UnicornAssemblyLoadContext(contextDirectory);
            runnerContext.Initialize(typeof(ITestRunner));

            AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
            Assembly testAssembly = runnerContext.GetAssembly(assemblyName);

            Type runnerType = runnerContext.GetAssemblyContainingType(typeof(TestsRunner))
                .GetTypes()
                .First(t => t.Name.Equals(typeof(TestsRunner).Name));

            ITestRunner runner = string.IsNullOrEmpty(propertiesPath) ?
                Activator.CreateInstance(runnerType, testAssembly, false) as ITestRunner :
                Activator.CreateInstance(runnerType, testAssembly, propertiesPath) as ITestRunner;

            IOutcome ioutcome = runner.RunTests();

            // Outcome transition between load contexts.
            byte[] bytes = SerializeOutcome(ioutcome);
            return DeserializeOutcome(bytes);
        }

#pragma warning disable SYSLIB0011 // Type or member is obsolete
        private static byte[] SerializeOutcome(IOutcome outcome)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, outcome);
                return ms.ToArray();
            }
        }

        private static LaunchOutcome DeserializeOutcome(byte[] bytes)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.Write(bytes, 0, bytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                return new BinaryFormatter().Deserialize(memStream) as LaunchOutcome;
            }
        }
#pragma warning restore SYSLIB0011 // Type or member is obsolete

#endif
    }
}
