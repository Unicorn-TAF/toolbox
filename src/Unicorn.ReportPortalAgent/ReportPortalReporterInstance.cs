using System;
using System.Reflection;
using Unicorn.Taf.Core.Logging;
using Unicorn.Taf.Core.Steps;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.ReportPortalAgent
{
    /// <summary>
    /// Report portal reporter instance. Contains subscriptions to corresponding Unicorn events.
    /// </summary>
    public sealed class ReportPortalReporterInstance : IDisposable
    {
        private readonly ReportPortalListener _listener;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportPortalReporterInstance"/> class.<br/>
        /// New RP launch is started automatically with automatic subscription to all test events.
        /// </summary>
        public ReportPortalReporterInstance() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportPortalReporterInstance"/> class 
        /// based on existing launch ID and with automatic subscription to all test events.<br/>
        /// If ID is null, then starts new launch on RP.
        /// </summary>
        /// <param name="existingLaunchId">existing launch ID</param>
        public ReportPortalReporterInstance(string existingLaunchId)
        {
            _listener = new ReportPortalListener();

            if (!string.IsNullOrEmpty(existingLaunchId))
            {
                _listener.ExistingLaunchId = existingLaunchId;
            }

            _listener.StartRun();

            Test.OnTestStart += _listener.StartSuiteMethod;
            Test.OnTestFinish += _listener.FinishSuiteMethod;
            Test.OnTestSkip += SkipSuiteMethod;

            SuiteMethod.OnSuiteMethodStart += _listener.StartSuiteMethod;
            SuiteMethod.OnSuiteMethodFinish += _listener.FinishSuiteMethod;

            TestSuite.OnSuiteStart += _listener.StartSuite;
            TestSuite.OnSuiteFinish += _listener.FinishSuite;

            StepEvents.OnStepStart += ReportInfo;
        }

        /// <summary>
        /// Starts RP Suite record for given <see cref="TestSuite"/>.
        /// </summary>
        /// <param name="suite">suite instance</param>
        public void StartSuite(TestSuite suite) =>
            _listener.StartSuite(suite);

        /// <summary>
        /// Finishes RP Suite record for given <see cref="TestSuite"/>.
        /// </summary>
        /// <param name="suite">suite instance</param>
        public void FinishSuite(TestSuite suite) =>
            _listener.FinishSuite(suite);

        /// <summary>
        /// Starts RP record for given <see cref="SuiteMethod"/>.
        /// Parent suite record should be started before.
        /// </summary>
        /// <param name="suiteMethod">suite method instance</param>
        public void StartSuiteMethod(SuiteMethod suiteMethod) =>
            _listener.StartSuiteMethod(suiteMethod);

        /// <summary>
        /// Finishes RP record for given <see cref="SuiteMethod"/>.
        /// </summary>
        /// <param name="suiteMethod">suite method instance</param>
        public void FinishSuiteMethod(SuiteMethod suiteMethod) =>
            _listener.FinishSuiteMethod(suiteMethod);

        /// <summary>
        /// Reports logs to current executing suite method.
        /// </summary>
        /// <param name="method">method itself</param>
        /// <param name="arguments">method arguments</param>
        public void ReportInfo(MethodBase method, object[] arguments) =>
            _listener.ReportTestMessage(
                LogLevel.Info, 
                StepsUtilities.GetStepInfo(method, arguments));

        /// <summary>
        /// Sets defect type to set for skipped tests in report portal.
        /// </summary>
        /// <param name="defectType">report portal defect type ID</param>
        public void SetSkippedTestsDefectType(string defectType) =>
            _listener.SkippedTestDefectType = defectType;

        /// <summary>
        /// Unsubscribes from events and finishes launch if it is not external.
        /// </summary>
        public void Dispose()
        {
            if (_listener != null)
            {
                // Need to finish it even if the launch is external!
                _listener.FinishRun();

                Test.OnTestStart -= _listener.StartSuiteMethod;
                Test.OnTestFinish -= _listener.FinishSuiteMethod;
                Test.OnTestSkip -= SkipSuiteMethod;

                SuiteMethod.OnSuiteMethodStart -= _listener.StartSuiteMethod;
                SuiteMethod.OnSuiteMethodFinish -= _listener.FinishSuiteMethod;

                TestSuite.OnSuiteStart -= _listener.StartSuite;
                TestSuite.OnSuiteFinish -= _listener.FinishSuite;

                StepEvents.OnStepStart -= ReportInfo;
            }
        }

        private void SkipSuiteMethod(SuiteMethod suiteMethod)
        {
            StartSuiteMethod(suiteMethod);
            FinishSuiteMethod(suiteMethod);
        }
    }
}
