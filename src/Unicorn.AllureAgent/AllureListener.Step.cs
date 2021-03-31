using Allure.Commons;
using System;
using System.Reflection;
using Unicorn.Taf.Core.Steps;

namespace Unicorn.AllureAgent
{
    /// <summary>
    /// Allure listener, which handles reporting stuff for all test items.
    /// </summary>
    public partial class AllureListener
    {
        private string stepGuid = null;

        internal void StartStep(MethodBase method, object[] arguments)
        {
            try
            {
                stepGuid = Guid.NewGuid().ToString();

                var result = new StepResult()
                {
                    name = StepsUtilities.GetStepInfo(method, arguments),
                    status = Status.passed
                };

                AllureLifecycle.Instance.StartStep(testGuid, stepGuid, result);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in StartStep '{0}'." + Environment.NewLine + e);
            }
        }

        internal void FinishStep(MethodBase method, object[] arguments)
        {
            try
            {
                AllureLifecycle.Instance.StopStep(stepGuid);
                stepGuid = null;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in FinishStep" + Environment.NewLine + e);
            }
        }
    }
}
