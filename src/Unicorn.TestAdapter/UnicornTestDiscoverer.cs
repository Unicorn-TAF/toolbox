using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Unicorn.Taf.Api;

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
                    logger?.SendMessage(TestMessageLevel.Error, 
                        Prefix + $"error discovering {source} source: {ex.Message}");
                }
            }

            logger?.SendMessage(TestMessageLevel.Informational, Prefix + "test discovery complete");
        }

        private static void DiscoverAssembly(string source, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            List<TestInfo> testsInfos = GetTestsInfo(source);

            logger?.SendMessage(TestMessageLevel.Informational, 
                $"Source: {Path.GetFileName(source)} (found {testsInfos.Count} tests)");

            foreach (TestInfo testInfo in testsInfos)
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

                if (testInfo.TestParametersCount > 0)
                {
                    testcase.Traits.Add(new Trait("Parameters", testInfo.TestParametersCount.ToString()));
                }

                discoverySink.SendTestCase(testcase);
            }
        }

#if NETFRAMEWORK
        private static List<TestInfo> GetTestsInfo(string source)
        {
            AppDomain unicornDomain = AppDomain.CreateDomain("Unicorn.TestAdapter AppDomain");

            try
            {
                string pathToDll = Assembly.GetExecutingAssembly().Location;

                AppDomainObserver observer = (AppDomainObserver)unicornDomain
                    .CreateInstanceFromAndUnwrap(pathToDll, typeof(AppDomainObserver).FullName);

                return observer.GetTests(source);
            }
            finally
            {
                AppDomain.Unload(unicornDomain);
            }
        }
#endif

#if NETCOREAPP || NET
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static List<TestInfo> GetTestsInfo(string source)
        {
            UnicornAssemblyLoadContext observerContext = new UnicornAssemblyLoadContext(Path.GetDirectoryName(source));

            observerContext.Initialize(typeof(IDataCollector));

            Type observerType = observerContext.GetAssemblyContainingType(typeof(LoadContextObserver))
                .GetTypes()
                .First(t => t.Name.Equals(typeof(LoadContextObserver).Name));

            IDataCollector observer = Activator.CreateInstance(observerType) as IDataCollector;

            AssemblyName assemblyName = AssemblyName.GetAssemblyName(source);
            Assembly testAssembly = observerContext.GetAssembly(assemblyName);

            IOutcome iTestInfo = observer.CollectData(testAssembly);

            //Outcome transition between load contexts.
            byte[] bytes = SerializeInfo(iTestInfo);
            ObserverOutcome outcome = DeserializeInfo(bytes);

            return outcome.TestInfoList;
        }

#pragma warning disable SYSLIB0011 // Type or member is obsolete
        private static byte[] SerializeInfo(IOutcome outcome)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, outcome);
                return ms.ToArray();
            }
        }

        private static ObserverOutcome DeserializeInfo(byte[] bytes)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.Write(bytes, 0, bytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                return new BinaryFormatter().Deserialize(memStream) as ObserverOutcome;
            }
        }
#pragma warning restore SYSLIB0011 // Type or member is obsolete

#endif
    }
}