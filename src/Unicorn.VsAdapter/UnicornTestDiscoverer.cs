using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Unicorn.Core.Engine;
using Unicorn.Core.Testing.Tests.Attributes;

namespace Unicorn.TestAdapter
{
    [DefaultExecutorUri(UnicrornTestExecutor.ExecutorUriString)]
    [FileExtension(".dll")]
    public class UnicornTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            logger?.SendMessage(TestMessageLevel.Informational, "Unicorn Adapter: Test discovery starting");

            foreach (string source in sources)
            {
                List<TestInfo> infos;

                using (var loader = new UnicornAppDomainIsolation<GetTestsWorker>(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
                {
                    infos = loader.Instance.GetTests(source);
                }

                logger?.SendMessage(TestMessageLevel.Informational, $"Source: {source} (found {infos.Count} tests)");

                foreach (var info in infos)
                {
                    var testcase = new TestCase(info.FullName, UnicrornTestExecutor.ExecutorUri, source)
                    {
                        CodeFilePath = source,
                        DisplayName = info.DisplayName
                    };

                    discoverySink.SendTestCase(testcase);
                }
            }

            logger?.SendMessage(TestMessageLevel.Informational, "Unicorn Adapter: Test discovery complete");
        }

        public class GetTestsWorker : MarshalByRefObject
        {
            public List<TestInfo> GetTests(string source)
            {
                var infos = new List<TestInfo>();
                var testsAssembly = Assembly.LoadFrom(source);
                var unicornTests = TestsObserver.ObserveTests(testsAssembly);

                foreach (var unicornTest in unicornTests)
                {
                    var testAttribute = unicornTest.GetCustomAttribute(typeof(TestAttribute), true) as TestAttribute;

                    if (testAttribute != null)
                    {
                        var name = string.IsNullOrEmpty(testAttribute.Description) ? unicornTest.Name : testAttribute.Description;

                        infos.Add(new TestInfo(AdapterUtilities.GetFullTestMethodName(unicornTest), name));
                    }
                }

                return infos;
            }
        }
    }

    [Serializable]
    public class TestInfo
    {
        public TestInfo(string fullName, string displayName)
        {
            this.FullName = fullName;
            this.DisplayName = displayName;
        }

        public string FullName { get; protected set; }

        public string DisplayName { get; protected set; }
    }
}