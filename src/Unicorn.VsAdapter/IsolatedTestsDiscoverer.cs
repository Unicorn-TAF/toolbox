using System;
using System.Collections.Generic;
using System.Reflection;
using Unicorn.Core.Engine;
using Unicorn.Core.Testing.Tests.Attributes;

namespace Unicorn.TestAdapter
{
    public class IsolatedTestsDiscoverer : MarshalByRefObject
    {
#pragma warning disable S3885 // "Assembly.Load" should be used
        public List<UnicornTestInfo> GetTests(string source)
        {
            var infos = new List<UnicornTestInfo>();
            var testsAssembly = Assembly.LoadFrom(source);
            var unicornTests = TestsObserver.ObserveTests(testsAssembly);

            foreach (var unicornTest in unicornTests)
            {
                var methodName = unicornTest.Name;
                var className = unicornTest.DeclaringType.FullName;
                var testAttribute = unicornTest.GetCustomAttribute(typeof(TestAttribute), true) as TestAttribute;

                if (testAttribute != null)
                {
                    var name = string.IsNullOrEmpty(testAttribute.Description) ? unicornTest.Name : testAttribute.Description;
                    infos.Add(new UnicornTestInfo(AdapterUtilities.GetFullTestMethodName(unicornTest), name, methodName, className));
                }
            }

            return infos;
        }
#pragma warning restore S3885 // "Assembly.Load" should be used
    }
}
