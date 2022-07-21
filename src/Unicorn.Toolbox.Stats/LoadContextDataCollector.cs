#if NET || NETCOREAPP
using System.Reflection;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.Toolbox.Stats
{
    public class LoadContextDataCollector : IDataCollector
    {
        private readonly bool _considerParameterization;

        public LoadContextDataCollector(bool considerParameterization)
        {
            _considerParameterization = considerParameterization;
        }

        public IOutcome CollectData(Assembly assembly)
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
                        data.AddSuiteData(CollectorUtilities.GetSuiteInfo(parameterizedSuite, _considerParameterization));

                        if (!_considerParameterization)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    var nonParameterizedSuite = assembly.CreateInstance(suiteType.FullName);
                    data.AddSuiteData(CollectorUtilities.GetSuiteInfo(nonParameterizedSuite, _considerParameterization));
                }
            }

            return data;
        }
    }
}
#endif