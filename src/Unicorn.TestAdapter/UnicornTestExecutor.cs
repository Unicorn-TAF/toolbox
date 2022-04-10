using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core.Engine;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnicornTest = Unicorn.Taf.Core.Testing;

namespace Unicorn.TestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class UnicornTestExecutor : ITestExecutor
    {
        internal const string ExecutorUriString = "executor://UnicornTestExecutor/v2";

        private const string Prefix = "Unicorn Adapter: ";

        private const string RunStart = Prefix + "test run starting";
        private const string RunComplete = Prefix + "test run complete";
        private const string RunInitFailed = Prefix + "test run initialization failed";
        private const string RunnerError = Prefix + "runner error";
        private const string NonVsRunDisabled = Prefix + "only run from Visual Studio is supported, exiting...";

        internal static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            if (string.IsNullOrEmpty(runContext.SolutionDirectory))
            {
                frameworkHandle.SendMessage(TestMessageLevel.Warning, NonVsRunDisabled);
                return;
            }

            frameworkHandle.SendMessage(TestMessageLevel.Informational, RunStart);

            var runDir = ExecutorUtilities.PrepareRunDirectory(runContext.SolutionDirectory);
            ExecutorUtilities.CopyDeploymentItems(runContext, runDir, frameworkHandle);

            foreach (var source in sources)
            {
                ExecutorUtilities.CopySourceFilesToRunDir(Path.GetDirectoryName(source), runDir);

                try
                {
                    var newSource = source.Replace(Path.GetDirectoryName(source), runDir);
                    RunTests(newSource, new string[0]);
                }
                catch (Exception ex)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error, 
                        RunnerError + Environment.NewLine + ex);
                }
            }

            frameworkHandle.SendMessage(TestMessageLevel.Informational, RunComplete);
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            if (string.IsNullOrEmpty(runContext.SolutionDirectory))
            {
                frameworkHandle.SendMessage(TestMessageLevel.Warning, NonVsRunDisabled);
                return;
            }

            var sources = tests.Select(t => t.Source).Distinct();

            frameworkHandle.SendMessage(TestMessageLevel.Informational, RunStart);

            var runDir = ExecutorUtilities.PrepareRunDirectory(runContext.SolutionDirectory);
            ExecutorUtilities.CopyDeploymentItems(runContext, runDir, frameworkHandle);

            foreach (var source in sources)
            {
                ExecutorUtilities.CopySourceFilesToRunDir(Path.GetDirectoryName(source), runDir);
                var masks = tests.Select(t => t.FullyQualifiedName).ToArray();

                try
                {
                    var newSource = source.Replace(Path.GetDirectoryName(source), runDir);
                    LaunchOutcome outcome = RunTests(newSource, masks);
                    ProcessLaunchOutcome(outcome, tests, frameworkHandle);
                }
                catch (Exception ex)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error, 
                        RunnerError + Environment.NewLine + ex);

                    foreach (TestCase test in tests)
                    {
                        ExecutorUtilities.SkipTest(test, ex.ToString(), frameworkHandle);
                    }
                }
            }

            frameworkHandle.SendMessage(TestMessageLevel.Informational, RunComplete);
        }

        private void ProcessLaunchOutcome(LaunchOutcome outcome, IEnumerable<TestCase> tests, IFrameworkHandle fwHandle)
        {
            if (!outcome.RunInitialized)
            {
                fwHandle.SendMessage(
                    TestMessageLevel.Error,
                    RunInitFailed + Environment.NewLine + outcome.RunnerException);

                foreach (TestCase test in tests)
                {
                    ExecutorUtilities.SkipTest(test, "Assembly initialization failed.\n" + outcome.RunnerException.ToString(), fwHandle);
                }
            }
            else
            {
                foreach (TestCase test in tests)
                {
                    var outcomes = outcome.SuitesOutcomes
                        .SelectMany(so => so.TestsOutcomes)
                        .Where(to => to.FullMethodName.Equals(test.FullyQualifiedName));

                    if (outcomes.Any())
                    {
                        foreach (var outcomeToRecord in outcomes)
                        {
                            var testResult = ExecutorUtilities.GetTestResultFromOutcome(outcomeToRecord, test);
                            fwHandle.RecordResult(testResult);
                        }
                    }
                    else
                    {
                        ExecutorUtilities.SkipTest(test, "Test was not executed, possibly it's disabled", fwHandle);
                    }
                }
            }
        }

        public void Cancel() 
        { 
        }

#if NETFRAMEWORK
        private LaunchOutcome RunTests(string assemblyPath, string[] testsMasks)
        {
            AppDomain unicornDomain = AppDomain.CreateDomain("Unicorn.TestAdapter AppDomain");

            try
            {
                string pathToDll = Assembly.GetExecutingAssembly().Location;

                AppDomainRunner runner = (AppDomainRunner)unicornDomain
                    .CreateInstanceFromAndUnwrap(pathToDll, typeof(AppDomainRunner).FullName);

                return runner.RunTests(assemblyPath, testsMasks);
            }
            finally
            {
                AppDomain.Unload(unicornDomain);
            }
        }
#endif

#if NET || NETCOREAPP
        private LaunchOutcome RunTests(string assemblyPath, string[] testsMasks)
        {
            string contextDirectory = Path.GetDirectoryName(assemblyPath);
            UnicornAssemblyLoadContext runnerContext = new UnicornAssemblyLoadContext(contextDirectory);
            runnerContext.Initialize(typeof(ITestRunner));

            AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
            Assembly testAssembly = runnerContext.GetAssembly(assemblyName);

            Type runnerType = runnerContext.GetAssemblyContainingType(typeof(LoadContextRunner))
                .GetTypes()
                .First(t => t.Name.Equals(typeof(LoadContextRunner).Name));

            ITestRunner runner = Activator.CreateInstance(runnerType, testAssembly, testsMasks) as ITestRunner;
            IOutcome ioutcome = runner.RunTests();

            // Outcome transition between load contexts.
            byte[] bytes = SerializeOutcome(ioutcome);
            return DeserializeOutcome(bytes);
        }

#pragma warning disable SYSLIB0011 // Type or member is obsolete
        private byte[] SerializeOutcome(IOutcome outcome)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, outcome);
                return ms.ToArray();
            }
        }

        private LaunchOutcome DeserializeOutcome(byte[] bytes)
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
