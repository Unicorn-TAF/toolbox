#if NETFRAMEWORK
using System;
using System.Reflection;
using Unicorn.Taf.Core.Engine;

namespace Unicorn.ConsoleRunner
{
    /// <summary>
    /// Provides ability to run unicorn tests in dedicated AppDomain.
    /// </summary>
    public class AppDomainRunner : MarshalByRefObject
    {
        /// <summary>
        /// Runs tests from specified assembly and specified configuration.
        /// </summary>
        /// <param name="assemblyPath">assembly file path</param>
        /// <param name="configPath">configuration file path</param>
        /// <returns>outcome of tests run</returns>
        public LaunchOutcome RunTests(string assemblyPath, string configPath)
        {
            Assembly testAssembly = Assembly.LoadFrom(assemblyPath);

            TestsRunner runner = string.IsNullOrEmpty(configPath) ?
                new TestsRunner(testAssembly, false) :
                new TestsRunner(testAssembly, configPath);

            runner.RunTests();
            return runner.Outcome;
        }
    }
}
#endif