using System;
using Unicorn.Taf.Core.Engine;

namespace Unicorn.ConsoleRunner
{
    public class IsolatedTestsRunner : MarshalByRefObject
    {
        public LaunchOutcome RunTests(string assemblyPath, string configPath)
        {
            var runner = new TestsRunner(assemblyPath, configPath);
            runner.RunTests();
            return runner.Outcome;
        }
    }
}
