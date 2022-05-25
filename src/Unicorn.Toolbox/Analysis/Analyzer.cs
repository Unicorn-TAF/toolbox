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
        private string _assemblyFile;
        private bool _considerParameterization;

        public Analyzer()
        {
        }

        public AutomationData Data { get; protected set; }

        public string AssemblyFile { get; protected set; }

        public string AssemblyName { get; protected set; }

        public void GetTestsStatistics(string fileName, bool considerParameterization)
        {
            _assemblyFile = fileName;
            _considerParameterization = considerParameterization;
            AssemblyFile = Path.GetFileName(fileName);
            AssemblyName = System.Reflection.AssemblyName.GetAssemblyName(fileName).FullName;
            Data = new AutomationData();


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
            AssemblyName assemblyName = System.Reflection.AssemblyName.GetAssemblyName(_assemblyFile);
            Assembly testAssembly = collectorContext.GetAssembly(assemblyName);

            Type collectorType = collectorContext.GetAssemblyContainingType(typeof(LoadContextDataCollector))
                .GetTypes()
                .First(t => t.Name.Equals(typeof(LoadContextDataCollector).Name));

            IDataCollector collector = Activator.CreateInstance(collectorType, new object[] {_considerParameterization }) as IDataCollector;
            IOutcome ioutcome = collector.CollectData(testAssembly);

            // Outcome transition between load contexts.
            byte[] bytes = SerializeOutcome(ioutcome);
            Data = DeserializeOutcome(bytes);
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
