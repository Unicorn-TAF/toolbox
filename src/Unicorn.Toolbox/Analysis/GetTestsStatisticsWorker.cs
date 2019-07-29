using System;
using System.Linq;
using System.Reflection;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing;
using Unicorn.Taf.Core.Testing.Attributes;

namespace Unicorn.Toolbox.Analysis
{
    public class GetTestsStatisticsWorker : MarshalByRefObject
    {
        private readonly Type baseSuiteType = typeof(TestSuite);

        public AutomationData GetTestsStatistics(string assemblyPath)
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
                        var parameterizedSuite = testsAssembly.CreateInstance(suiteType.FullName, true, BindingFlags.Default, null, parametersSet.Parameters.ToArray(), null, null);
                        ((TestSuite)parameterizedSuite).Outcome.DataSetName = parametersSet.Name;
                        data.AddSuiteData(GetSuiteInfo(parameterizedSuite));
                    }
                }
                else
                {
                    var nonParameterizedSuite = testsAssembly.CreateInstance(suiteType.FullName);
                    data.AddSuiteData(GetSuiteInfo(nonParameterizedSuite));
                }
            }

            return data;
        }

        private SuiteInfo GetSuiteInfo(object suiteInstance)
        {
            int inheritanceCounter = 0;
            var currentType = suiteInstance.GetType();

            while (!currentType.Equals(baseSuiteType) && inheritanceCounter++ < 50)
            {
                currentType = currentType.BaseType;
            }

            var testSuite = suiteInstance as TestSuite;

            var suiteName = testSuite.Outcome.Name;

            if (!string.IsNullOrEmpty(testSuite.Outcome.DataSetName))
            {
                suiteName += "[" + testSuite.Outcome.DataSetName + "]";
            }

            var suiteInfo = new SuiteInfo(suiteName, testSuite.Tags, testSuite.Metadata);

            var tests = suiteInstance.GetType().GetRuntimeMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null);

            foreach (var test in tests)
            {
                suiteInfo.TestsInfos.Add(GetTestInfo(test));
            }

            return suiteInfo;
        }

        private TestInfo GetTestInfo(MethodInfo testMethod)
        {
            var disabled = testMethod.GetCustomAttribute<DisabledAttribute>() != null;

            var authorAttribute = testMethod.GetCustomAttribute<AuthorAttribute>();
            var author = authorAttribute != null ? authorAttribute.Author : "No Author";

            var titleAttribute = testMethod.GetCustomAttribute<TestAttribute>();
            var title = string.IsNullOrEmpty(titleAttribute.Title) ? testMethod.Name : titleAttribute.Title;

            var categories = testMethod.GetCustomAttributes<CategoryAttribute>().Select(c => c.Category.ToUpper().Trim()).Where(c => !string.IsNullOrEmpty(c));

            return new TestInfo(title, author, disabled, categories);
        }
    }
}
