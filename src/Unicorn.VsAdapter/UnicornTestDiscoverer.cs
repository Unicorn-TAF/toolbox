using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Unicorn.Core.Engine;

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
                List<UnicornTestInfo> infos;

                using (var discoverer = new UnicornAppDomainIsolation<IsolatedTestsDiscoverer>(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
                {
                    infos = discoverer.Instance.GetTests(source);
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
    }
}