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
            new Program().Run(parser.AssemblyPath, parser.PropertiesPath, parser.TrxFileName);
        }

        private void Run(string assemblyPath, string propertiesPath, string trxFileName)
        {
            Config.FillFromFile(propertiesPath);
            Reporter.ReportHeader(assemblyPath);
            
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

#if NETFRAMEWORK
        private LaunchOutcome ExecuteTests(string assemblyPath, string propertiesPath)
        {
            LaunchOutcome outcome = null;
            AppDomain unicornDomain = AppDomain.CreateDomain("Unicorn.ConsoleRunner AppDomain");

            try
            {
                string pathToDll = Assembly.GetExecutingAssembly().Location;

                AppDomainRunner myObject = (AppDomainRunner)unicornDomain
                    .CreateInstanceFromAndUnwrap(pathToDll, typeof(AppDomainRunner).FullName);

                outcome = myObject.RunTests(assemblyPath, propertiesPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running tests ({ex.Message})");
            }

            AppDomain.Unload(unicornDomain);

            return outcome;
        }
#endif

#if NETCOREAPP || NET
        private LaunchOutcome ExecuteTests(string assemblyPath, string propertiesPath)
        {
            string contextDirectory = Path.GetDirectoryName(assemblyPath);
            LaunchOutcome outcome = null;
            UnicornAssemblyLoadContext runnerContext = new UnicornAssemblyLoadContext(contextDirectory);

            try
            {
                runnerContext.Initialize(typeof(ITestRunner));

                AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
                Assembly testAssembly = runnerContext.GetAssembly(assemblyName);

                Type runnerType = runnerContext.GetAssemblyContainingType(typeof(TestsRunner))
                    .GetTypes()
                    .First(t => t.Name.Equals(typeof(TestsRunner).Name));

                ITestRunner runner = Activator.CreateInstance(runnerType, testAssembly, propertiesPath) as ITestRunner;

                IOutcome ioutcome = runner.RunTests();

                // Outcome transition between load contexts.
                byte[] bytes = SerializeOutcome(ioutcome);
                outcome = DeserializeOutcome(bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running tests ({ex.Message})");
            }

            //runnerContext.Unload();

            return outcome;
        }

        private byte[] SerializeOutcome(IOutcome outcome)
        {
            BinaryFormatter bf = new BinaryFormatter();

            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, outcome);
                return ms.ToArray();
            }
        }

        private LaunchOutcome DeserializeOutcome(byte[] bytes)
        {
            BinaryFormatter binForm = new BinaryFormatter();

            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.Write(bytes, 0, bytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                return binForm.Deserialize(memStream) as LaunchOutcome;
            }
        }
#endif
    }
}
