#if NET || NETCOREAPP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing;
using Unicorn.Taf.Core.Testing.Attributes;

namespace Unicorn.Toolbox.Analysis
{
    public class GetTestsStatisticsWorkerNetCore
    {
        private readonly Type _baseSuiteType = typeof(TestSuite);

        public AutomationData GetTestsStatistics(string assemblyPath, bool considerParameterization)
        {
            var data = new AutomationData();

            var alc = new CustomAssemblyLoadContext(assemblyPath);

            alc.Resolving += (context, assemblyName) =>
            {
                var calc = context as CustomAssemblyLoadContext;
                return calc?.LoadFallback(assemblyName);
            };

            var testsAssembly = alc.LoadFromAssemblyPath(assemblyPath);
            //var proxy = asm.CreateInstance(typeof(Proxy).FullName);
            var asm = alc.Assemblies.First(a => a.GetName().Name.Equals("Unicorn.Taf.Core", StringComparison.InvariantCultureIgnoreCase));
            var allSuites = (IEnumerable<Type>)asm.GetType("TestsObserver").GetMethod("ObserveTestSuites").Invoke(null, new[] { testsAssembly });

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
                        data.AddSuiteData(GetSuiteInfo(parameterizedSuite, considerParameterization));

                        if (!considerParameterization)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    var nonParameterizedSuite = testsAssembly.CreateInstance(suiteType.FullName);
                    data.AddSuiteData(GetSuiteInfo(nonParameterizedSuite, considerParameterization));
                }
            }

            return data;
        }

        private SuiteInfo GetSuiteInfo(object suiteInstance, bool considerParameterization)
        {
            int inheritanceCounter = 0;
            var currentType = suiteInstance.GetType();

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

        private List<TestInfo> GetTestsInfo(MethodInfo testMethod, object suiteInstance, bool considerParameterization)
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


    internal class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;
        private readonly string _basePath;

        public CustomAssemblyLoadContext(string mainAssemblyToLoadPath)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
            _basePath = Path.GetDirectoryName(mainAssemblyToLoadPath);
        }

        protected override Assembly Load(AssemblyName name)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(name);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }

        /// <summary>
        /// Loads assemblies that are dependencies, and in the same folder as the parent assembly,
        /// but are not fully specified in parent assembly deps.json file. This happens when the
        /// dependencies reference in the csproj file has CopyLocal=false, and for example, the
        /// reference is a projectReference and has the same output directory as the parent.
        ///
        /// LoadFallback should be called via the CustomAssemblyLoadContext.Resolving callback when
        /// a dependent assembly of that referred to in a previous 'CustomAssemblyLoadContext.Load' call
        /// could not be loaded by CustomAssemblyLoadContext.Load nor by the default ALC; to which the
        /// runtime will fallback when CustomAssemblyLoadContext.Load fails (to let the default ALC
        /// load system assemblies).
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        public Assembly LoadFallback(AssemblyName name)
        {
            string assemblyPath = Path.Combine(_basePath, name.Name + ".dll");
            if (File.Exists(assemblyPath))
                return LoadFromAssemblyPath(assemblyPath);
            return null;
        }
    }
}
#endif