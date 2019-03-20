using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Unicorn.Taf.Core.Engine;

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

                var testCoordinatesProvider = new TestCoordinatesProvider(source);

                foreach (var info in infos)
                {
                    var coordinates = testCoordinatesProvider.GetNavigationData(info.ClassName, info.MethodName);

                    var testcase = new TestCase(info.FullName, UnicrornTestExecutor.ExecutorUri, source)
                    {
                        DisplayName = info.DisplayName,
                        CodeFilePath = coordinates.FilePath,
                        LineNumber = coordinates.LineNumber
                };

                    discoverySink.SendTestCase(testcase);
                }
            }

            logger?.SendMessage(TestMessageLevel.Informational, "Unicorn Adapter: Test discovery complete");
        }
    }
}