using System;
using System.IO;
using System.Linq;
using System.Reflection;

#if NET || NETCOREAPP
using System.Runtime.Loader;
using System.Runtime.Serialization.Formatters.Binary;
using Unicorn.Taf.Api;
#endif

namespace Unicorn.Toolbox.Analysis
{
    public class Analyzer
    {
        private readonly string _assemblyFile;
        private readonly bool _considerParameterization;

        public Analyzer(string fileName, bool considerParameterization)
        {
            _assemblyFile = fileName;
            _considerParameterization = considerParameterization;
            AssemblyFileName = Path.GetFileName(fileName);
            TestsAssemblyName = AssemblyName.GetAssemblyName(fileName).FullName;
            Data = new AutomationData();
        }

        public AutomationData Data { get; protected set; }

        public string AssemblyFileName { get; protected set; }

        public string TestsAssemblyName { get; protected set; }

        public void GetTestsStatistics()
        {
#if NETFRAMEWORK

            AppDomain unicornDomain = AppDomain.CreateDomain("Unicorn.ConsoleRunner AppDomain");

            try
            {
                string pathToDll = Assembly.GetExecutingAssembly().Location;

                AppDomainDataCollector collector = (AppDomainDataCollector)unicornDomain
                    .CreateInstanceFromAndUnwrap(pathToDll, typeof(AppDomainDataCollector).FullName);

                Data = collector.GetTestsStatistics(_assemblyFile, _considerParameterization);
            }
            finally
            {
                AppDomain.Unload(unicornDomain);
            }
#endif

#if NET || NETCOREAPP

            string contextDirectory = Path.GetDirectoryName(_assemblyFile);

            UnicornAssemblyLoadContext collectorContext = new UnicornAssemblyLoadContext(contextDirectory);
            collectorContext.Initialize(typeof(ITestRunner));
            collectorContext.LoadAssemblyFrom(Assembly.GetExecutingAssembly().Location);
            AssemblyName assemblyName = AssemblyName.GetAssemblyName(_assemblyFile);
            Assembly testAssembly = collectorContext.GetAssembly(assemblyName);

            Type collectorType = collectorContext.GetAssemblyContainingType(typeof(LoadContextDataCollector))
                .GetTypes()
                .First(t => t.Name.Equals(typeof(LoadContextDataCollector).Name));

            IDataCollector collector = Activator.CreateInstance(collectorType, new object[] {_considerParameterization }) as IDataCollector;
            IOutcome ioutcome = collector.CollectData(testAssembly);

            // Outcome transition between load contexts.
            byte[] bytes = SerializeOutcome(ioutcome);
            Data = DeserializeOutcome(bytes);

            //Data = new GetTestsStatisticsWorkerNetCore().GetTestsStatistics(_assemblyFile, _considerParameterization);
#endif
        }

#if NET || NETCOREAPP
        private static byte[] SerializeOutcome(IOutcome outcome)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, outcome);
                return ms.ToArray();
            }
        }

        private static AutomationData DeserializeOutcome(byte[] bytes)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.Write(bytes, 0, bytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                return new BinaryFormatter().Deserialize(memStream) as AutomationData;
            }
        }
#endif
    }
}
