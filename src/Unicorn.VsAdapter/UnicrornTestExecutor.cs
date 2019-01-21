using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Unicorn.Core.Engine;

namespace Unicorn.TestAdapter
{
    [ExtensionUri(UnicrornTestExecutor.ExecutorUriString)]
    public class UnicrornTestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://UnicornTestExecutor/v1";
        public static readonly Uri ExecutorUri = new Uri(UnicrornTestExecutor.ExecutorUriString);
        private bool m_cancelled;

        public void RunTests(IEnumerable<string> sources, IRunContext runContext,
            IFrameworkHandle frameworkHandle)
        {
            foreach (var source in sources)
            {
                LaunchOutcome outcome;

                TestsRunner runner = new TestsRunner(source, false);
                runner.RunTests();

                using (var loader = new UnicornAppDomainIsolation<RunTestsWorker>(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
                {
                    outcome = loader.Instance.RunTests(source);
                }
            }
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext,
               IFrameworkHandle frameworkHandle)
        {
            m_cancelled = false;

            LaunchOutcome outcome;

            using (var loader = new UnicornAppDomainIsolation<RunTestsWorker>(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {
                outcome = loader.Instance.RunTests(tests.First().Source, tests.Select(t => t.FullyQualifiedName).ToArray());
            }

            foreach (TestCase test in tests)
            {
                if (m_cancelled)
                {
                    break;
                }

                var testResult = new TestResult(test);
                var correspondingUnicornOutcome = outcome.SuitesOutcomes.SelectMany(so => so.TestsOutcomes).First(to => to.FullMethodName.Equals(test.FullyQualifiedName));
                testResult.Outcome = GetTestCaseResult(correspondingUnicornOutcome);
                frameworkHandle.RecordResult(testResult);
            }
        }


        private TestOutcome GetTestCaseResult(Unicorn.Core.Testing.Tests.TestOutcome outcome)
        {
            switch (outcome.Result)
            {
                case Unicorn.Core.Testing.Tests.Status.Passed:
                    return TestOutcome.Passed;
                case Unicorn.Core.Testing.Tests.Status.Failed:
                    return TestOutcome.Failed;
                case Unicorn.Core.Testing.Tests.Status.Skipped:
                    return TestOutcome.Skipped;
                default:
                    return TestOutcome.None;
            }
        }

        public void Cancel() =>
            m_cancelled = true;
    }

    public class RunTestsWorker : MarshalByRefObject
    {
        public LaunchOutcome RunTests(string source)
        {
            var runner = new TestsRunner(source, false);
            runner.RunTests();
            return runner.Outcome;
        }

        public LaunchOutcome RunTests(string source, string[] testsMasks)
        {
            Configuration.SetTestsMasks(testsMasks);
            var runner = new TestsRunner(source, false);
            runner.RunTests();
            return runner.Outcome;
        }
    }
}
