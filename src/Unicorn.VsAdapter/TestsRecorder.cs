using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Unicorn.TestAdapter
{
    [Serializable]
    public class TestsRecorder
    {
        public IFrameworkHandle frameworkHandle;

        public TestsRecorder(IFrameworkHandle frameworkHandle)
        {
            this.frameworkHandle = frameworkHandle;
        }

        public void ReportTestStart(TestCase testCase)
        {
            frameworkHandle.RecordStart(testCase);
        }

        public void ReportTestEnd(TestCase testCase, TestOutcome outcome)
        {
            frameworkHandle.RecordEnd(testCase, outcome);
        }

        public void RecordResult(TestResult testResult)
        {
            frameworkHandle.RecordResult(testResult);
        }
    }
}
