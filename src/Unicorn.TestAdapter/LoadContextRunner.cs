#if NET || NETCOREAPP
using System.Reflection;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core;
using Unicorn.Taf.Core.Engine;

namespace Unicorn.TestAdapter
{
    /// <summary>
    /// Provides ability to run unicorn tests in dedicated AppDomain.
    /// </summary>
    public class LoadContextRunner : ITestRunner
    {
        private readonly Assembly _testsAssembly;
        private readonly string[] _testsMasks;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadContextRunner"/> class based on assembly and tests masks.
        /// </summary>
        /// <param name="assembly">assembly to run on</param>
        /// <param name="testsMasks">masks of tests to be run</param>
        public LoadContextRunner(Assembly assembly, string[] testsMasks)
        {
            _testsAssembly = assembly;
            _testsMasks = testsMasks;
        }

        /// <summary>
        /// Runs tests from specified assembly and specified configuration.
        /// </summary>
        /// <param name="assemblyPath">assembly file path</param>
        /// <param name="configPath">configuration file path</param>
        /// <returns>outcome of tests run</returns>
        public IOutcome RunTests()
        {
            Config.SetTestsMasks(_testsMasks);
            var runner = new TestsRunner(_testsAssembly, false);
            runner.RunTests();
            return runner.Outcome;
        }
    }
}
#endif