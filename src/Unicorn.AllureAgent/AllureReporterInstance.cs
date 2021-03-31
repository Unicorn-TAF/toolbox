using System;
using Unicorn.Taf.Core.Steps;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.AllureAgent
{
    /// <summary>
    /// Allure reporter instance. Contains subscriptions to corresponding Unicorn events.
    /// </summary>
    public sealed class AllureReporterInstance : IDisposable
    {
        private readonly AllureListener _listener;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllureReporterInstance"/> class.<br/>
        /// Automatic subscribtion to all test events.
        /// </summary>
        public AllureReporterInstance()
        {
            _listener = new AllureListener();

            Test.OnTestStart += _listener.StartSuiteMethod;
            Test.OnTestFinish += _listener.FinishSuiteMethod;
            Test.OnTestSkip += _listener.SkipSuiteMethod;

            SuiteMethod.OnSuiteMethodStart += _listener.StartSuiteMethod;
            SuiteMethod.OnSuiteMethodFinish += _listener.FinishSuiteMethod;

            TestSuite.OnSuiteStart += _listener.StartSuite;
            TestSuite.OnSuiteFinish += _listener.FinishSuite;

            StepsEvents.OnStepStart += _listener.StartStep;
            StepsEvents.OnStepFinish += _listener.FinishStep;
        }

        /// <summary>
        /// Unsubscribes from events.
        /// </summary>
        public void Dispose()
        {
            Test.OnTestStart -= _listener.StartSuiteMethod;
            Test.OnTestFinish -= _listener.FinishSuiteMethod;
            Test.OnTestSkip -= _listener.SkipSuiteMethod;

            SuiteMethod.OnSuiteMethodStart -= _listener.StartSuiteMethod;
            SuiteMethod.OnSuiteMethodFinish -= _listener.FinishSuiteMethod;

            TestSuite.OnSuiteStart -= _listener.StartSuite;
            TestSuite.OnSuiteFinish -= _listener.FinishSuite;

            StepsEvents.OnStepStart -= _listener.StartStep;
            StepsEvents.OnStepFinish -= _listener.FinishStep;
        }
    }
}
