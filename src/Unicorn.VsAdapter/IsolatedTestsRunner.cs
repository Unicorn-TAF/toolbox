using System;
using Unicorn.Taf.Core.Engine;

namespace Unicorn.TestAdapter
{
    public class IsolatedTestsRunner : MarshalByRefObject
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

        public LaunchOutcome RunTests(string source, string[] testsMasks, TestsRecorder recorder)
        {
            Configuration.SetTestsMasks(testsMasks);

            var runner = new TestsRunner(source, false);
            runner.RunTests();
            return runner.Outcome;
        }
    }
}
