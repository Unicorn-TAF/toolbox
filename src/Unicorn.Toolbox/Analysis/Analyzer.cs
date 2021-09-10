using System.IO;
using System.Reflection;
using Unicorn.Taf.Core.Engine;

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
            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            using (var loader = new UnicornAppDomainIsolation<GetTestsStatisticsWorker>(location))
            {
                Data = loader.Instance.GetTestsStatistics(_assemblyFile, _considerParameterization);
            }
        }
    }
}
