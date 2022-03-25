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

namespace Unicorn.TestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class UnicrornTestExecutor : ITestExecutor
    {
        internal const string ExecutorUriString = "executor://UnicornTestExecutor/v2";

        private const string Prefix = "Unicorn Adapter: ";

        private const string RunStart = Prefix + "test run starting";
        private const string RunComplete = Prefix + "test run complete";
        private const string RunInitFailed = Prefix + "test run initialization failed";
        private const string RunnerError = Prefix + "runner error";
        private const string NonVsRunDisabled = Prefix + "only run from Visual Studio is supported, exiting...";

        internal static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        private bool runCancelled;

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            if (string.IsNullOrEmpty(runContext.SolutionDirectory))
            {
                frameworkHandle.SendMessage(TestMessageLevel.Warning, NonVsRunDisabled);
                return;
            }

            frameworkHandle.SendMessage(TestMessageLevel.Informational, RunStart);
            runCancelled = false;

            var runDir = ExecutorUtilities.PrepareRunDirectory(runContext.SolutionDirectory);
            ExecutorUtilities.CopyDeploymentItems(runContext, runDir, frameworkHandle);

            foreach (var source in sources)
            {
                ExecutorUtilities.CopySourceFilesToRunDir(Path.GetDirectoryName(source), runDir);

                try
                {
                    Environment.CurrentDirectory = runDir;
                    var newSource = source.Replace(Path.GetDirectoryName(source), runDir);

                    //var runner = new TafEngine.TestsRunner(newSource, false);
                    //runner.RunTests();
                    //var outcome = runner.Outcome;

                    //if (!outcome.RunInitialized)
                    //{
                    //    frameworkHandle.SendMessage(TestMessageLevel.Error, 
                    //        RunInitFailed + Environment.NewLine + outcome.RunnerException);
                    //}
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
            runCancelled = false;

            var runDir = ExecutorUtilities.PrepareRunDirectory(runContext.SolutionDirectory);
            ExecutorUtilities.CopyDeploymentItems(runContext, runDir, frameworkHandle);

            foreach (var source in sources)
            {
                ExecutorUtilities.CopySourceFilesToRunDir(Path.GetDirectoryName(source), runDir);
                var masks = tests.Select(t => t.FullyQualifiedName).ToArray();

                try
                {
                    Environment.CurrentDirectory = runDir;
                    var newSource = source.Replace(Path.GetDirectoryName(source), runDir);

                    LaunchOutcome outcome = RunTests(newSource, masks);

                    if (!outcome.RunInitialized)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Error,
                            RunInitFailed + Environment.NewLine + outcome.RunnerException);

                        foreach (TestCase test in tests)
                        {
                            SkipTest(test, frameworkHandle);
                        }
                    }
                    else
                    {
                        foreach (TestCase test in tests)
                        {
                            var outcomes = outcome.SuitesOutcomes.SelectMany(so => so.TestsOutcomes).Where(to => to.FullMethodName.Equals(test.FullyQualifiedName));

                            if (outcomes.Any())
                            {
                                var unicornOutcome = outcomes.First();
                                var testResult = GetTestResultFromOutcome(unicornOutcome, test);
                                frameworkHandle.RecordResult(testResult);
                            }
                            else
                            {
                                SkipTest(test, frameworkHandle);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error, 
                        RunnerError + Environment.NewLine + ex);

                    foreach (TestCase test in tests)
                    {
                        SkipTest(test, frameworkHandle);
                    }
                }
            }

            frameworkHandle.SendMessage(TestMessageLevel.Informational, RunComplete);
        }

        public void Cancel() =>
            runCancelled = true;

#if NETFRAMEWORK
        private LaunchOutcome RunTests(string assemblyPath, string[] testsMasks)
        {
            LaunchOutcome outcome = null;

            AppDomain unicornDomain = AppDomain.CreateDomain("Unicorn.ConsoleRunner AppDomain");

            try
            {
                string pathToDll = Assembly.GetExecutingAssembly().Location;

                AppDomainRunner runner = (AppDomainRunner)unicornDomain
                    .CreateInstanceFromAndUnwrap(pathToDll, typeof(AppDomainRunner).FullName);

                outcome = runner.RunTests(assemblyPath, testsMasks);
            }
            finally
            {
                AppDomain.Unload(unicornDomain);
            }

            return outcome;
        }
#endif

#if NET || NETCOREAPP
        private LaunchOutcome RunTests(string assemblyPath, string[] testsMasks)
        {
            string contextDirectory = Path.GetDirectoryName(assemblyPath);
            LaunchOutcome outcome = null;
            UnicornAssemblyLoadContext runnerContext = new UnicornAssemblyLoadContext(contextDirectory);

            try
            {
                runnerContext.Initialize(typeof(ITestRunner));

                AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
                Assembly testAssembly = runnerContext.GetAssembly(assemblyName);

                Type runnerType = runnerContext.GetAssemblyContainingType(typeof(ContextRunner))
                    .GetTypes()
                    .First(t => t.Name.Equals(typeof(ContextRunner).Name));

                ITestRunner runner = Activator.CreateInstance(runnerType, testAssembly, testsMasks) as ITestRunner;

                IOutcome ioutcome = runner.RunTests();

                // Outcome transition between load contexts.
                byte[] bytes = SerializeOutcome(ioutcome);
                outcome = DeserializeOutcome(bytes);
            }
            finally
            {
                //runnerContext.Unload();
            }

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

        //private void FailTest(TestCase test, Exception ex, IFrameworkHandle frameworkHandle)
        //{
        //    var testResult = new TestResult(test)
        //    {
        //        ComputerName = Environment.MachineName,
        //        Outcome = TestOutcome.Failed,
        //        ErrorMessage = ex.Message,
        //        ErrorStackTrace = ex.StackTrace
        //    };

        //    frameworkHandle.RecordResult(testResult);
        //}

        private void SkipTest(TestCase test, IFrameworkHandle frameworkHandle)
        {
            var testResult = new TestResult(test)
            {
                ComputerName = Environment.MachineName,
                Outcome = TestOutcome.Skipped
            };

            frameworkHandle.RecordResult(testResult);
        }

        private TestResult GetTestResultFromOutcome(Taf.Core.Testing.TestOutcome outcome, TestCase testCase)
        {
            var testResult = new TestResult(testCase);
            testResult.ComputerName = Environment.MachineName;

            switch (outcome.Result)
            {
                case Taf.Core.Testing.Status.Passed:
                    testResult.Outcome = TestOutcome.Passed;
                    testResult.Duration = outcome.ExecutionTime;
                    break;
                case Taf.Core.Testing.Status.Failed:
                    testResult.Outcome = TestOutcome.Failed;
                    testResult.ErrorMessage = outcome.Exception.Message;
                    testResult.ErrorStackTrace = outcome.Exception.StackTrace;
                    testResult.Duration = outcome.ExecutionTime;
                    break;
                case Taf.Core.Testing.Status.Skipped:
                    testResult.Outcome = TestOutcome.Skipped;
                    break;
                default:
                    testResult.Outcome = TestOutcome.None;
                    break;
            }

            return testResult;
        }
    }
}
