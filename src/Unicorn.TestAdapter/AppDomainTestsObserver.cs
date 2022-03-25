#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Reflection;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing;
using Unicorn.Taf.Core.Testing.Attributes;

namespace Unicorn.TestAdapter
{
    /// <summary>
    /// Provides with ability to get <see cref="TestInfo"/> data from specified assembly in separate AppDomain.
    /// </summary>
    public class AppDomainTestsObserver : MarshalByRefObject
    {
        /// <summary>
        /// Gets list of <see cref="ITestInfo"/> from specified assembly in separate AppDomain.
        /// </summary>
        /// <param name="assembly">test assembly file</param>
        /// <returns>test info list</returns>
        public List<ITestInfo> GetTests(string assembly)
        {
            var testsAssembly = Assembly.LoadFrom(assembly);
            var tests = TestsObserver.ObserveTests(testsAssembly);
            var infos = new List<ITestInfo>();

            foreach (var unicornTest in tests)
            {
                var testAttribute = unicornTest
                    .GetCustomAttribute(typeof(TestAttribute), true) as TestAttribute;

                if (testAttribute != null)
                {
                    infos.Add(new TestInfo(unicornTest));
                }
            }

            return infos;
        }
    }
}
#endif