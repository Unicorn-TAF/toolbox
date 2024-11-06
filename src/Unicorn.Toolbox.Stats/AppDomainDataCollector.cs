using System;
using System.Reflection;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.Toolbox.Stats;

#pragma warning disable S3885 // "Assembly.Load" should be used
public class AppDomainDataCollector : MarshalByRefObject
{
    public AutomationData GetTestsStatistics(string assemblyPath, bool considerParameterization)
    {
        var data = new AutomationData();

        var testsAssembly = Assembly.LoadFrom(assemblyPath);
        var allSuites = TestsObserver.ObserveTestSuites(testsAssembly);

        foreach (var suiteType in allSuites)
        {
            if (AdapterUtilities.IsSuiteParameterized(suiteType))
            {
                foreach (var parametersSet in AdapterUtilities.GetSuiteData(suiteType))
                {
                    var parameterizedSuite = testsAssembly
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
                var nonParameterizedSuite = testsAssembly.CreateInstance(suiteType.FullName);
                data.AddSuiteData(CollectorUtilities.GetSuiteInfo(nonParameterizedSuite, considerParameterization));
            }
        }

        return data;
    }

    
}
#pragma warning restore S3885 // "Assembly.Load" should be used

