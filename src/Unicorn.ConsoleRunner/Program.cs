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
        public static void Main(string[] args)
        {
            ArgsParser parser = new ArgsParser(args);
            new Program().Run(parser.AssemblyPath, parser.ConfigPath, parser.TrxFileName, parser.NoLogo);
        }

        private void Run(string assemblyPath, string propertiesPath, string trxFileName, bool hideLogo)
        {
            if (!string.IsNullOrEmpty(propertiesPath))
            {
                Config.FillFromFile(propertiesPath);
            }

            Reporter.ReportHeader(assemblyPath, hideLogo);

            try
            {
                LaunchOutcome outcome = ExecuteTests(assemblyPath, propertiesPath);

                if (outcome == null)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(trxFileName))
                {
                    new TrxCreator().GenerateTrxFile(outcome, trxFileName);
                }

                Reporter.ReportResults(outcome);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error running tests: {0}", ex);
            }
        }

#if NETFRAMEWORK
        private static LaunchOutcome ExecuteTests(string assemblyPath, string propertiesPath)
        {
            string assemblyDir = Path.GetDirectoryName(assemblyPath);

            using (var runner = new UnicornAppDomainIsolation<AppDomainRunner>(assemblyDir))
            {
                return runner.Instance.RunTests(assemblyPath, propertiesPath);
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

            try
            {
                // Outcome transition between load contexts.
                byte[] bytes = SerializeOutcome(ioutcome);
                return DeserializeOutcome(bytes);
            }
            finally
            {
                runnerContext.Unload();
            }
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
