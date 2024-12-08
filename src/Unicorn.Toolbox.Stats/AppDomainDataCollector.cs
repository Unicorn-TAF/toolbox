using System;
using System.Reflection;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.Toolbox.Stats;

#pragma warning disable S3885 // "Assembly.Load" should be used
public sealed class AppDomainDataCollector : MarshalByRefObject
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
                    var parameterizedSuiteInstance = testsAssembly
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
                        considerParameterization));

                    if (!considerParameterization)
                    {
                        break;
                    }
                }
            }
            else
            {
                var nonParameterizedSuiteInstance = testsAssembly.CreateInstance(suiteType.FullName);
                var nonParameterizedSuite = new TestSuite(nonParameterizedSuiteInstance);
                data.AddSuiteData(CollectorUtilities.GetSuiteInfo(
                    nonParameterizedSuite, 
                    nonParameterizedSuiteInstance, 
                    considerParameterization));
            }
        }

        return data;
    }

    
}
#pragma warning restore S3885 // "Assembly.Load" should be used

