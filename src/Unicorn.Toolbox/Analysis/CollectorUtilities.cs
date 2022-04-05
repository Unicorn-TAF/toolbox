using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unicorn.Taf.Core.Testing;
using Unicorn.Taf.Core.Testing.Attributes;

namespace Unicorn.Toolbox.Analysis
{
    internal static class CollectorUtilities
    {
        internal static SuiteInfo GetSuiteInfo(object suiteInstance, bool considerParameterization)
        {
            int inheritanceCounter = 0;
            var currentType = suiteInstance.GetType();

            Type _baseSuiteType = typeof(TestSuite);

            while (!currentType.Equals(_baseSuiteType) && inheritanceCounter++ < 50)
            {
                currentType = currentType.BaseType;
            }

            var testSuite = suiteInstance as TestSuite;

            var suiteName = testSuite.Outcome.Name;

            if (!string.IsNullOrEmpty(testSuite.Outcome.DataSetName) && considerParameterization)
            {
                suiteName += "[" + testSuite.Outcome.DataSetName + "]";
            }

            var suiteInfo = new SuiteInfo(suiteName, testSuite.Tags, testSuite.Metadata);

            var tests = suiteInstance.GetType()
                .GetRuntimeMethods()
                .Where(m => m.IsDefined(typeof(TestAttribute), true));

            foreach (var test in tests)
            {
                if (test.IsDefined(typeof(TestDataAttribute), true))
                {
                    var infos = GetTestsInfo(test, suiteInstance, considerParameterization);
                    suiteInfo.TestsInfos.AddRange(infos);
                }
                else
                {
                    suiteInfo.TestsInfos.Add(GetTestInfo(test));
                }
            }

            return suiteInfo;
        }

        private static TestInfo GetTestInfo(MethodInfo testMethod)
        {
            var disabled = testMethod.GetCustomAttribute<DisabledAttribute>() != null;

            var authorAttribute = testMethod.GetCustomAttribute<AuthorAttribute>();
            var author = authorAttribute != null ? authorAttribute.Author : "No Author";

            var titleAttribute = testMethod.GetCustomAttribute<TestAttribute>();
            var title = string.IsNullOrEmpty(titleAttribute.Title) ? testMethod.Name : titleAttribute.Title;

            var categories = testMethod.GetCustomAttributes<CategoryAttribute>().Select(c => c.Category.ToUpper().Trim()).Where(c => !string.IsNullOrEmpty(c));

            return new TestInfo(title, author, disabled, categories);
        }

        private static List<TestInfo> GetTestsInfo(MethodInfo testMethod, object suiteInstance, bool considerParameterization)
        {
            var infos = new List<TestInfo>();

            var disabled = testMethod.IsDefined(typeof(DisabledAttribute), true);

            var authorAttribute = testMethod.GetCustomAttribute<AuthorAttribute>();
            var author = authorAttribute != null ? authorAttribute.Author : "No Author";

            var titleAttribute = testMethod.GetCustomAttribute<TestAttribute>();
            var title = string.IsNullOrEmpty(titleAttribute.Title) ? testMethod.Name : titleAttribute.Title;

            var categories = testMethod
                .GetCustomAttributes<CategoryAttribute>()
                .Select(c => c.Category.ToUpper().Trim())
                .Where(c => !string.IsNullOrEmpty(c));

            var datasetsAttribute = testMethod.GetCustomAttribute<TestDataAttribute>();
            var dataSets = suiteInstance
                .GetType()
                .GetMethod(datasetsAttribute.Method)
                .Invoke(suiteInstance, null)
                as List<DataSet>;

            if (considerParameterization)
            {
                for (int i = 0; i < dataSets.Count; i++)
                {
                    infos.Add(new TestInfo($"{title} [{i}]", author, disabled, categories));
                }
            }
            else
            {
                infos.Add(new TestInfo(title, author, disabled, categories));
            }

            return infos;
        }
    }
}
