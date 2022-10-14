using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unicorn.Taf.Api;

#if NET || NETCOREAPP
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace Unicorn.Toolbox.Stats
{
    public class StatsCollector
    {
        private string _assemblyFile;
        private bool _considerParameterization;

        public StatsCollector()
        {
        }

        public AutomationData Data { get; protected set; }

        public string AssemblyFile { get; protected set; }

        public string AssemblyProps { get; protected set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GetTestsStatistics(string fileName, bool considerParameterization)
        {
            _assemblyFile = fileName;
            _considerParameterization = considerParameterization;
            AssemblyFile = Path.GetFileName(fileName);
            AssemblyName aName = AssemblyName.GetAssemblyName(fileName);
            AssemblyProps = $"{aName.Name} version={aName.Version}";
            Data = new AutomationData();

#if NETFRAMEWORK

            string assemblyDir = Path.GetDirectoryName(_assemblyFile);

            using (var collector = new UnicornAppDomainIsolation<AppDomainCollector>(assemblyDir))
            {
                Data = collector.Instance.GetTestsStatistics(_assemblyFile, _considerParameterization);
            }
#endif

#if NET || NETCOREAPP

            string contextDirectory = Path.GetDirectoryName(_assemblyFile);

            UnicornAssemblyLoadContext collectorContext = new UnicornAssemblyLoadContext(contextDirectory);
            collectorContext.Initialize(typeof(ITestRunner));
            collectorContext.LoadAssemblyFrom(typeof(LoadContextCollector).Assembly.Location);
            AssemblyName assemblyName = AssemblyName.GetAssemblyName(_assemblyFile);
            Assembly testAssembly = collectorContext.GetAssembly(assemblyName);

            Type collectorType = collectorContext.GetAssemblyContainingType(typeof(LoadContextCollector))
                .GetTypes()
                .First(t => t.Name.Equals(typeof(LoadContextCollector).Name));

            IDataCollector collector = Activator
                .CreateInstance(collectorType, new object[] { _considerParameterization }) as IDataCollector;
            IOutcome ioutcome = collector.CollectData(testAssembly);

            // Outcome transition between load contexts.
            byte[] bytes = SerializeOutcome(ioutcome);
            Data = DeserializeOutcome(bytes);

            collectorContext.Unload();
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
