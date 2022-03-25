#if NET || NETCOREAPP
using System;
using System.Collections.Generic;
using System.Reflection;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing;
using Unicorn.Taf.Core.Testing.Attributes;

namespace Unicorn.TestAdapter
{
    public class ContextInfoObserver : ITestInfoCollector
    {
        public List<ISuiteInfo> CollectSuitesInfo(Assembly assembly) =>
            throw new NotImplementedException();

        /// <summary>
        /// Gets list of <see cref="ITestInfo"/> from specified assembly in separate AppDomain.
        /// </summary>
        /// <param name="assembly">test assembly file</param>
        /// <returns>test info list</returns>
        public List<ITestInfo> CollectTestsInfo(Assembly assembly)
        {
            IEnumerable<MethodInfo> tests = TestsObserver.ObserveTests(assembly);
            List<ITestInfo> infos = new List<ITestInfo>();

            foreach (var unicornTest in tests)
            {
                TestAttribute testAttribute = unicornTest.GetCustomAttribute<TestAttribute>(true);

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