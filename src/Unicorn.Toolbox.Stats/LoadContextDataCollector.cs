#if NET || NETCOREAPP
using System.Reflection;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.Toolbox.Stats;

public sealed class LoadContextDataCollector : IDataCollector
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
                    var parameterizedSuiteInstance = assembly
                        .CreateInstance(
                        suiteType.FullName,
                        true,
                        BindingFlags.Default,
                        null,
                        parametersSet.Parameters.ToArray(),
                        null,
                        null);

                    TestSuite parameterizedTestSuite = new TestSuite(parameterizedSuiteInstance);
                    parameterizedTestSuite.Outcome.DataSetName = parametersSet.Name;
                    data.AddSuiteData(CollectorUtilities.GetSuiteInfo(
                        parameterizedTestSuite, 
                        parameterizedSuiteInstance, 
                        _considerParameterization));

                    if (!_considerParameterization)
                    {
                        break;
                    }
                }
            }
            else
            {
                var nonParameterizedSuiteInstance = assembly.CreateInstance(suiteType.FullName);
                var nonParameterizedSuite = new TestSuite(nonParameterizedSuiteInstance);
                data.AddSuiteData(CollectorUtilities.GetSuiteInfo(
                    nonParameterizedSuite, 
                    nonParameterizedSuiteInstance, 
                    _considerParameterization));
            }
        }

        return data;
    }
}
#endif