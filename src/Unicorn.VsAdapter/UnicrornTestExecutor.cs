using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Unicorn.Core.Engine;

namespace Unicorn.TestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class UnicrornTestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://UnicornTestExecutor/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);
        private bool m_cancelled;

        public void RunTests(IEnumerable<string> sources, IRunContext runContext,
            IFrameworkHandle frameworkHandle)
        {
            foreach (var source in sources)
            {
                LaunchOutcome outcome;

                TestsRunner runner = new TestsRunner(source, false);
                runner.RunTests();

                using (var isolation = new UnicornAppDomainIsolation<IsolatedTestsRunner>(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
                {
                    outcome = isolation.Instance.RunTests(source);
                }
            }
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext,
               IFrameworkHandle frameworkHandle)
        {
            m_cancelled = false;

            LaunchOutcome outcome = null;
            var recorder = new TestsRecorder(frameworkHandle);

            frameworkHandle.SendMessage(TestMessageLevel.Informational, "Unicorn Adapter: Test run starting");

            try
            {
                using (var loader = new UnicornAppDomainIsolation<IsolatedTestsRunner>(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
                {
                    outcome = loader.Instance.RunTests(tests.First().Source, tests.Select(t => t.FullyQualifiedName).ToArray());
                }
            }
            catch (Exception ex)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, $"Unicorn Adapter: error running tests ({ex.ToString()})");
            }

            foreach (TestCase test in tests)
            {
                if (m_cancelled)
                {
                    break;
                }

                var unicornOutcome = outcome.SuitesOutcomes.SelectMany(so => so.TestsOutcomes).First(to => to.FullMethodName.Equals(test.FullyQualifiedName));
                var testResult = GetTestResultFromOutcome(unicornOutcome, test);
                frameworkHandle.RecordResult(testResult);
            }

            frameworkHandle.SendMessage(TestMessageLevel.Informational, "Unicorn Adapter: Test run complete");
        }

        private TestResult GetTestResultFromOutcome(Core.Testing.Tests.TestOutcome outcome, TestCase testCase)
        {
            var testResult = new TestResult(testCase);
            testResult.ComputerName = Environment.MachineName;

            switch (outcome.Result)
            {
                case Core.Testing.Tests.Status.Passed:
                    testResult.Outcome = TestOutcome.Passed;
                    testResult.Duration = outcome.ExecutionTime;
                    break;
                case Core.Testing.Tests.Status.Failed:
                    testResult.Outcome = TestOutcome.Failed;
                    testResult.ErrorMessage = outcome.Exception.Message;
                    testResult.ErrorStackTrace = outcome.Exception.StackTrace;
                    testResult.Duration = outcome.ExecutionTime;
                    break;
                case Core.Testing.Tests.Status.Skipped:
                    testResult.Outcome = TestOutcome.Skipped;
                    break;
                default:
                    testResult.Outcome = TestOutcome.None;
                    break;
            }

            return testResult;
        }

        public void Cancel() =>
            m_cancelled = true;
    }
}
