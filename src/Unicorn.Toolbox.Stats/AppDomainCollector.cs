#if NETFRAMEWORK
using System;
using System.Reflection;

namespace Unicorn.Toolbox.Stats
{
    public class AppDomainCollector : MarshalByRefObject
    {
        public AutomationData GetTestsStatistics(string assemblyPath, bool considerParameterization)
        {
            Assembly testsAssembly = Assembly.LoadFrom(assemblyPath);
            return AutomationDataCollector.CollectData(testsAssembly, considerParameterization);
        }
    }
}
#endif