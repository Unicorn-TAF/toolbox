using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Unicorn.Taf.Core.Engine;

namespace Unicorn.TestAdapter
{
    [DefaultExecutorUri(UnicrornTestExecutor.ExecutorUriString)]
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    public class UnicornTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            logger?.SendMessage(TestMessageLevel.Informational, "Unicorn Adapter: Test discovery starting");

            foreach (string source in sources)
            {
                try
                {
                    DiscoverAssembly(source, logger, discoverySink);
                }
                catch (Exception ex)
                {
                    logger?.SendMessage(TestMessageLevel.Error, $"Unicorn Adapter: Error discovering {source} source: {ex.Message}");
                }
            }

            logger?.SendMessage(TestMessageLevel.Informational, "Unicorn Adapter: Test discovery complete");
        }

        private void DiscoverAssembly(string source, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            List<TestInfo> testsInfos;

            using (var discoverer = new UnicornAppDomainIsolation<IsolatedTestsInfoObserver>(Path.GetDirectoryName(source)))
            {
                testsInfos = discoverer.Instance.GetTests(source);
            }

            logger?.SendMessage(TestMessageLevel.Informational, $"Source: {Path.GetFileName(source)} (found {testsInfos.Count} tests)");

            var testCoordinatesProvider = new TestCoordinatesProvider(source);

            foreach (var testInfo in testsInfos)
            {
                var methodName = testInfo.MethodName;
                var className = testInfo.ClassName;
                var coordinates = testCoordinatesProvider.GetNavigationData(className, methodName);

                var testcase = new TestCase(testInfo.FullName, UnicrornTestExecutor.ExecutorUri, source)
                {
                    DisplayName = testInfo.DisplayName,
                    CodeFilePath = coordinates.FilePath,
                    LineNumber = coordinates.LineNumber,
                };

                discoverySink.SendTestCase(testcase);
            }
        }
    }
}