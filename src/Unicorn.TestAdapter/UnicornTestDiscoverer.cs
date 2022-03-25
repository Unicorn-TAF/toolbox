using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.TestAdapter
{
    [DefaultExecutorUri(UnicrornTestExecutor.ExecutorUriString)]
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [Category("managed")]
    public class UnicornTestDiscoverer : ITestDiscoverer
    {
        private const string Prefix = "Unicorn Adapter: ";

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            logger?.SendMessage(TestMessageLevel.Informational, Prefix + "test discovery starting");

            foreach (string source in sources)
            {
                try
                {
                    DiscoverAssembly(source, logger, discoverySink);
                }
                catch (Exception ex)
                {
                    logger?.SendMessage(TestMessageLevel.Error, Prefix + $"error discovering {source} source: {ex.Message}");
                }
            }

            logger?.SendMessage(TestMessageLevel.Informational, Prefix + "test discovery complete");
        }

        private void DiscoverAssembly(string source, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            List<ITestInfo> testsInfos = GetTestsInfo(source);

            logger?.SendMessage(TestMessageLevel.Informational, $"Source: {Path.GetFileName(source)} (found {testsInfos.Count} tests)");

            foreach (var testInfo in testsInfos)
            {
                string fullName = testInfo.ClassPath + "." + testInfo.MethodName;

                TestCase testcase = new TestCase(fullName, UnicrornTestExecutor.ExecutorUri, source)
                {
                    DisplayName = testInfo.MethodName,
                };

                if (!string.IsNullOrEmpty(testInfo.Author))
                {
                    testcase.Traits.Add(new Trait("Author", testInfo.Author));
                }

                if (testInfo.Categories.Any())
                {
                    testcase.Traits.Add(new Trait("Categories", string.Join(",", testInfo.Categories)));
                }

                discoverySink.SendTestCase(testcase);
            }
        }

#if NETFRAMEWORK
        private List<ITestInfo> GetTestsInfo(string source)
        {
            AppDomain unicornDomain = AppDomain.CreateDomain("Unicorn.TestAdapter AppDomain");

            try
            {
                AppDomainTestsObserver myObject = (AppDomainTestsObserver)unicornDomain.CreateInstanceFromAndUnwrap(
                    Assembly.GetExecutingAssembly().Location, typeof(AppDomainTestsObserver).FullName);

                return myObject.GetTests(source);
            }
            finally
            {
                AppDomain.Unload(unicornDomain);
            }
        }
#endif

#if NETCOREAPP || NET
        private List<ITestInfo> GetTestsInfo(string source)
        {
            UnicornAssemblyLoadContext observerContext = new UnicornAssemblyLoadContext(Path.GetDirectoryName(source));
            List<ITestInfo> testInfo = null;

            observerContext.Initialize(typeof(ITestInfoCollector));

            Type observerType = observerContext.GetAssemblyContainingType(typeof(ContextInfoObserver))
                .GetTypes()
                .First(t => t.Name.Equals(typeof(ContextInfoObserver).Name));

            ITestInfoCollector observer = Activator.CreateInstance(observerType) as ITestInfoCollector;

            AssemblyName assemblyName = AssemblyName.GetAssemblyName(source);
            Assembly testAssembly = observerContext.GetAssembly(assemblyName);

            List<ITestInfo> iTestInfo = observer.CollectTestsInfo(testAssembly);

            //Outcome transition between load contexts.
            List<byte[]> bytes = SerializeInfo(iTestInfo);
            testInfo = DeserializeInfo(bytes);

            //runnerContext.Unload();

            return testInfo;
        }

        private List<byte[]> SerializeInfo(List<ITestInfo> outcome)
        {
            List<byte[]> list = new List<byte[]>();

            BinaryFormatter bf = new BinaryFormatter();

            foreach(ITestInfo iti in outcome)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bf.Serialize(ms, iti);
                    list.Add(ms.ToArray());
                }
            }

            return list;
        }

        private List<ITestInfo> DeserializeInfo(List<byte[]> bytes)
        {
            List<ITestInfo> testInfo = new List<ITestInfo>();

            BinaryFormatter binForm = new BinaryFormatter();

            foreach (byte[] bite in bytes)
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    memStream.Write(bite, 0, bite.Length);
                    memStream.Seek(0, SeekOrigin.Begin);
                    testInfo.Add(binForm.Deserialize(memStream) as TestInfo);
                }
            }

            return testInfo;
        }
#endif
    }
}