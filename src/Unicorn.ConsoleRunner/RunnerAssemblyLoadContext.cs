#if NETCOREAPP || NET
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Engine.Configuration;
using Unicorn.Taf.Core.Utility;

namespace Unicorn.ConsoleRunner
{
    public class RunnerAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly List<Assembly> _loadedAssemblies;

        private readonly string _path;

        private Assembly UnicornAssembly => Assemblies.First(a => a.GetTypes().Any(t => t.Name.Equals(typeof(IsolatedTestsRunner).Name)));

        public RunnerAssemblyLoadContext(string path)
        {
            _path = path;

            _loadedAssemblies = new List<Assembly>();
        }

        public void Initialize()
        {
            foreach (string dll in Directory.EnumerateFiles(_path, "*.dll"))
            {
                _loadedAssemblies.Add(LoadFromAssemblyPath(dll));
            }
        }

        public Type GetImplementation<T>() =>
            UnicornAssembly
            .GetTypes()
            .First(t => t.Name.Equals(typeof(T).Name));

        public IsolatedTestsRunner GetRunner()
        {
            Type type = GetImplementation<IsolatedTestsRunner>();
            object instance = Activator.CreateInstance(type);
            return instance as IsolatedTestsRunner;
        }

        public void Run(string assemblyPath, string propertiesPath, string trxFileName)
        {
            Config.FillFromFile(propertiesPath);
            Reporter.ReportHeader(assemblyPath);
            LaunchOutcome outcome = null;

            try
            {
                //string testAssemblyDir = Path.GetDirectoryName(assemblyPath);

                IsolatedTestsRunner runner = GetRunner();

                outcome = runner.RunTests(assemblyPath, propertiesPath);

                if (!string.IsNullOrEmpty(trxFileName))
                {
                    new TrxCreator().GenerateTrxFile(outcome, trxFileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running tests ({ex.Message})");
            }

            if (outcome != null)
            {
                Reporter.ReportResults(outcome);
            }
        }

        //protected override Assembly Load(AssemblyName assemblyName) =>
        //    Assembly.Load(assemblyName);
    }
}
#endif
