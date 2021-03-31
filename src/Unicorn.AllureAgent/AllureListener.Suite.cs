using Allure.Commons;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.AllureAgent
{
    /// <summary>
    /// Allure listener, which handles reporting stuff for all test items.
    /// </summary>
    public partial class AllureListener
    {
        private TestSuite testSuite = null;

        static AllureListener()
        {
            try
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                string path = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));

                var json = Path.Combine(path, "allureConfig.json");
                Environment.SetEnvironmentVariable("ALLURE_CONFIG", json);

                var directory = AllureLifecycle.Instance.AllureConfiguration.Directory;

                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }

                Directory.CreateDirectory(directory);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in initialization." + Environment.NewLine + e);
            }
        }

        internal void StartSuite(TestSuite suite)
        {
            try
            {
                var name = suite.Outcome.Name;

                if (!string.IsNullOrEmpty(suite.Outcome.DataSetName))
                {
                    name += "[" + suite.Outcome.DataSetName + "]";
                }

                var result = new TestResultContainer
                {
                    uuid = suite.Outcome.Id.ToString(),
                    name = name
                };

                // adding description to suite
                var description = new StringBuilder();

                foreach (var key in suite.Metadata.Keys)
                {
                    var value = suite.Metadata[key];
                    var appendString =
                        value.StartsWith("http", StringComparison.InvariantCultureIgnoreCase) ?
                        $"[{key}]({value})" :
                        $"{key}: {value}";

                    description.AppendLine(appendString);
                }

                result.description = description.ToString();

                testSuite = suite;
                AllureLifecycle.Instance.StartTestContainer(result);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in StartSuite '{0}'." + Environment.NewLine + e, suite.Outcome.Name);
            }
        }

        internal void FinishSuite(TestSuite suite)
        {
            try
            {
                AllureLifecycle.Instance.StopTestContainer(suite.Outcome.Id.ToString());
                AllureLifecycle.Instance.WriteTestContainer(suite.Outcome.Id.ToString());
                testSuite = null;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in FinishSuite '{0}'." + Environment.NewLine + e, suite.Outcome.Name);
            }
        }
    }
}
