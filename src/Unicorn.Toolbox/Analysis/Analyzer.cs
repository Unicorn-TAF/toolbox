using System.IO;
using System.Reflection;
using Unicorn.Taf.Core.Engine;

namespace Unicorn.Toolbox.Analysis
{
    public class Analyzer
    {
        private readonly string assemblyFile;

        public Analyzer(string fileName)
        {
            this.assemblyFile = fileName;
            this.AssemblyFileName = Path.GetFileName(fileName);
            this.TestsAssemblyName = AssemblyName.GetAssemblyName(fileName).FullName;
            this.Data = new AutomationData();
        }

        public AutomationData Data { get; protected set; }

        public string AssemblyFileName { get; protected set; }

        public string TestsAssemblyName { get; protected set; }

        public void GetTestsStatistics()
        {
            using (var loader = new UnicornAppDomainIsolation<GetTestsStatisticsWorker>(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {
                this.Data = loader.Instance.GetTestsStatistics(assemblyFile);
            }
        }
    }
}
