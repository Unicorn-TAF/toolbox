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
        /// Initializes a new instance of the <see cref="ReportPortalReporterInstance"/> class.
        /// </summary>
        public ReportPortalReporterInstance() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportPortalReporterInstance"/> class based on existing launch ID.
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
            Test.OnTestSkip += _listener.SkipSuiteMethod;

            SuiteMethod.OnSuiteMethodStart += _listener.StartSuiteMethod;
            SuiteMethod.OnSuiteMethodFinish += _listener.FinishSuiteMethod;

            TestSuite.OnSuiteStart += _listener.StartSuite;
            TestSuite.OnSuiteFinish += _listener.FinishSuite;

            StepsEvents.OnStepStart += ReportInfo;
        }

        /// <summary>
        /// Reports logs to current executing suite method
        /// </summary>
        /// <param name="method">method itself</param>
        /// <param name="arguments">method arguments</param>
        public void ReportInfo(MethodBase method, object[] arguments) =>
            _listener.ReportTestMessage(
                LogLevel.Info, 
                StepsUtilities.GetStepInfo(method, arguments));

        /// <summary>
        /// Sets list of tags which are common for all suites and specific for the run
        /// </summary>
        /// <param name="tags">list of tags</param>
        public void SetCommonSuitesTags(params string[] tags) =>
            _listener.SetCommonSuitesTags(tags);

        /// <summary>
        /// Sets defect type to set for skipped tests in report portal.
        /// </summary>
        /// <param name="defectType">report portal defect type ID</param>
        public void SetSkippedTestsDefectType(string defectType) =>
            _listener.SkippedTestDefectType = defectType;

        /// <summary>
        /// Unsubscribe from events and finish launch if it is not external
        /// </summary>
        public void Dispose()
        {
            if (_listener != null)
            {
                // Need to finish it even if the launch is external!
                _listener.FinishRun();

                Test.OnTestStart -= _listener.StartSuiteMethod;
                Test.OnTestFinish -= _listener.FinishSuiteMethod;
                Test.OnTestSkip -= _listener.SkipSuiteMethod;

                SuiteMethod.OnSuiteMethodStart -= _listener.StartSuiteMethod;
                SuiteMethod.OnSuiteMethodFinish -= _listener.FinishSuiteMethod;

                TestSuite.OnSuiteStart -= _listener.StartSuite;
                TestSuite.OnSuiteFinish -= _listener.FinishSuite;

                StepsEvents.OnStepStart -= ReportInfo;
            }
        }
    }
}
