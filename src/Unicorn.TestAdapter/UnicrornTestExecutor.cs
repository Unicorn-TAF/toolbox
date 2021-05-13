using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TafTests = Unicorn.Taf.Core.Testing;
using TafEngine = Unicorn.Taf.Core.Engine;

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
        private IFrameworkHandle fwHandle;
        private IEnumerable<TestCase> vsTests;

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

                    var runner = new TafEngine.TestsRunner(newSource, false);
                    runner.RunTests();
                    var outcome = runner.Outcome;

                    if (!outcome.RunInitialized)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Error, 
                            RunInitFailed + Environment.NewLine + outcome.RunnerException);
                    }
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
            fwHandle = frameworkHandle;
            vsTests = tests;

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

            TafTests.Test.OnTestStart += StartTestCase;
            TafTests.Test.OnTestFinish += FinishTestCase;
            TafTests.Test.OnTestSkip += SkipTestCase;

            foreach (var source in sources)
            {
                ExecutorUtilities.CopySourceFilesToRunDir(Path.GetDirectoryName(source), runDir);

                try
                {
                    Environment.CurrentDirectory = runDir;
                    var newSource = source.Replace(Path.GetDirectoryName(source), runDir);

                    var masks = tests.Select(t => t.FullyQualifiedName).ToArray();
                    TafEngine.Configuration.Config.SetTestsMasks(masks);

                    var runner = new TafEngine.TestsRunner(newSource, false);
                    runner.RunTests();
                    var outcome = runner.Outcome;

                    if (!outcome.RunInitialized)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Error, 
                            RunInitFailed + Environment.NewLine + outcome.RunnerException);
                    }
                }
                catch (Exception ex)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error, 
                        RunnerError + Environment.NewLine + ex);
                }
            }

            vsTests = null;
            fwHandle = null;
            frameworkHandle.SendMessage(TestMessageLevel.Informational, RunComplete);
        }

        public void Cancel() =>
            runCancelled = true;

        private void StartTestCase(TafTests.SuiteMethod suiteMethod)
        {
            var testCase = GetTestCaseOf(suiteMethod);
            fwHandle.RecordStart(testCase);
        }

        private void FinishTestCase(TafTests.SuiteMethod suiteMethod)
        {
            var testCase = GetTestCaseOf(suiteMethod);
            var testResult = GetTestResultFromOutcome(suiteMethod.Outcome, testCase);
            fwHandle.RecordEnd(testCase, testResult.Outcome);
            fwHandle.RecordResult(testResult);
        }

        private void SkipTestCase(TafTests.SuiteMethod suiteMethod)
        {
            var testCase = GetTestCaseOf(suiteMethod);
            var testResult = GetTestResultFromOutcome(suiteMethod.Outcome, testCase);
            fwHandle.RecordResult(testResult);
        }

        private TestResult GetTestResultFromOutcome(TafTests.TestOutcome outcome, TestCase testCase)
        {
            var testResult = new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                Duration = outcome.ExecutionTime
            };

            switch (outcome.Result)
            {
                case TafTests.Status.Passed:
                    testResult.Outcome = TestOutcome.Passed;
                    break;
                case TafTests.Status.Failed:
                    testResult.Outcome = TestOutcome.Failed;
                    testResult.ErrorMessage = outcome.Exception.Message;
                    testResult.ErrorStackTrace = outcome.Exception.StackTrace;
                    break;
                case TafTests.Status.Skipped:
                    testResult.Outcome = TestOutcome.Skipped;
                    break;
                default:
                    testResult.Outcome = TestOutcome.None;
                    break;
            }

            return testResult;
        }

        private TestCase GetTestCaseOf(TafTests.SuiteMethod suiteMethod) =>
            vsTests.First(t => t.FullyQualifiedName.Equals(suiteMethod.Outcome.FullMethodName));
    }
}
