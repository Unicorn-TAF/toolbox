using Allure.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.AllureAgent
{
    /// <summary>
    /// Allure listener, which handles reporting stuff for all test items.
    /// </summary>
    public partial class AllureListener
    {
        private string testGuid = null;

        internal void StartSuiteMethod(SuiteMethod suiteMethod)
        {
            try
            {
                var labels = new List<Label>()
                {
                    Label.Owner(suiteMethod.Outcome.Author),
                    Label.Host(Environment.MachineName),
                    Label.Suite(testSuite.Outcome.Name),
                    Label.TestClass(testSuite.GetType().Name),
                    Label.Package(testSuite.GetType().Namespace)
                };

                foreach (var tag in testSuite.Tags)
                {
                    labels.Add(Label.Feature(tag));
                }

                if (suiteMethod.MethodType.Equals(SuiteMethodType.Test))
                {
                    labels.AddRange((suiteMethod as Test)
                        .Categories
                        .Select(c => new Label() { name = "Category", value = c }));
                }

                var result = new TestResult
                {
                    uuid = suiteMethod.Outcome.Id.ToString(),
                    name = suiteMethod.Outcome.Title,
                    fullName = suiteMethod.Outcome.FullMethodName,
                    labels = labels
                };

                testGuid = suiteMethod.Outcome.Id.ToString();
                AllureLifecycle.Instance.StartTestCase(testSuite.Outcome.Id.ToString(), result);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in StartSuiteMethod '{0}'." + Environment.NewLine + e, suiteMethod.Outcome.Title);
            }
        }

        internal void FinishSuiteMethod(SuiteMethod suiteMethod)
        {
            try
            {
                var uuid = suiteMethod.Outcome.Id.ToString();

                switch (suiteMethod.Outcome.Result)
                {
                    case Taf.Core.Testing.Status.Failed:
                        FailTest(suiteMethod, uuid);
                        break;
                    case Taf.Core.Testing.Status.Skipped:
                        AllureLifecycle.Instance.UpdateTestCase(uuid, r =>
                        {
                            r.status = Allure.Commons.Status.skipped;
                        });
                        break;
                    default:
                        AllureLifecycle.Instance.UpdateTestCase(uuid, r => r.status = Allure.Commons.Status.passed);
                        break;
                }


                testGuid = null;
                AllureLifecycle.Instance.StopTestCase(uuid);
                AllureLifecycle.Instance.WriteTestCase(uuid);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in FinishSuiteMethod '{0}'." + Environment.NewLine + e, suiteMethod.Outcome.Title);
            }
        }

        internal void SkipSuiteMethod(SuiteMethod suiteMethod)
        {
            StartSuiteMethod(suiteMethod);
            FinishSuiteMethod(suiteMethod);
        }

        private void FailTest(SuiteMethod suiteMethod, string uuid)
        {
            var details = new StatusDetails()
            {
                message = suiteMethod.Outcome.Exception.Message,
                trace = suiteMethod.Outcome.Exception.StackTrace
            };

            AllureLifecycle.Instance.UpdateTestCase(uuid, r =>
            {
                if (r.steps.Any())
                {
                    r.steps.Last().status = Allure.Commons.Status.failed;
                }
            });

            var failed = suiteMethod.Outcome.Exception.GetType().Name
                .ToLowerInvariant().Contains("assert");

            var status = failed ? Allure.Commons.Status.failed : Allure.Commons.Status.broken;

            AllureLifecycle.Instance.UpdateTestCase(uuid, r =>
            {
                r.status = status;
                r.statusDetails = details;
            });

            if (suiteMethod.Outcome.Attachments.Any())
            {
                var attachments = new List<Allure.Commons.Attachment>();

                foreach (var a in suiteMethod.Outcome.Attachments)
                {
                    var attachment = new Allure.Commons.Attachment()
                    {
                        name = a.Name,
                        source = a.FilePath,
                        type = a.MimeType
                    };

                    attachments.Add(attachment);
                }

                AllureLifecycle.Instance.UpdateTestCase(uuid, r => r.attachments = attachments);
            }
        }
    }
}
