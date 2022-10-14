using System.Reflection;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.Toolbox.Stats
{
    public static class AutomationDataCollector
    {
        public static AutomationData CollectData(Assembly assembly, bool considerParameterization)
        {
            var data = new AutomationData();
            var allSuites = TestsObserver.ObserveTestSuites(assembly);

            foreach (var suiteType in allSuites)
            {
                if (AdapterUtilities.IsSuiteParameterized(suiteType))
                {
                    foreach (var parametersSet in AdapterUtilities.GetSuiteData(suiteType))
                    {
                        var parameterizedSuite = assembly
                            .CreateInstance(
                            suiteType.FullName,
                            true,
                            BindingFlags.Default,
                            null,
                            parametersSet.Parameters.ToArray(),
                            null,
                            null);

                        ((TestSuite)parameterizedSuite).Outcome.DataSetName = parametersSet.Name;
                        data.AddSuiteData(CollectorUtilities.GetSuiteInfo(parameterizedSuite, considerParameterization));

                        if (!considerParameterization)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    var nonParameterizedSuite = assembly.CreateInstance(suiteType.FullName);
                    data.AddSuiteData(CollectorUtilities.GetSuiteInfo(nonParameterizedSuite, considerParameterization));
                }
            }

            return data;
        }
    }
}
