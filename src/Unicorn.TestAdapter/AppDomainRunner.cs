#if NETFRAMEWORK
using System;
using System.Reflection;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core;
using Unicorn.Taf.Core.Engine;

namespace Unicorn.TestAdapter
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
        public LaunchOutcome RunTests(string assemblyPath, string[] testsMasks)
        {
            Config.SetTestsMasks(testsMasks);
            Assembly testAssembly = Assembly.LoadFrom(assemblyPath);
            var runner = new TestsRunner(testAssembly, false);
            runner.RunTests();
            return runner.Outcome;
        }
    }
}
#endif