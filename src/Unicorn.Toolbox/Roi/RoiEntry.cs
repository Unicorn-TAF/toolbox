namespace Unicorn.Toolbox.Roi
{
    public sealed class RoiEntry
    {
        public RoiEntry()
        {
            TotalTests = 0;
            TotalTestsRunnable = 0;
            TotalRuns = 0;
            HrsToWriteTests = 0;
            MtManDaysSavedPerRun = 0;
            MtManDaysSavedTotal = 0;
            AtManDaysTotal = 0;
            ROI = -100;
        }

        public RoiEntry(double totalTests, int totalRuns, double hrsToWriteTests, double mtManDaysSavedPerRun, double mtManDaysSavedTotal, double atManDaysTotal)
        {
            TotalTests = totalTests;
            TotalTestsRunnable = (int)totalTests;
            TotalRuns = totalRuns;
            HrsToWriteTests = hrsToWriteTests;
            MtManDaysSavedPerRun = mtManDaysSavedPerRun;
            MtManDaysSavedTotal = mtManDaysSavedTotal;
            AtManDaysTotal = atManDaysTotal;
            ROI = (MtManDaysSavedTotal - AtManDaysTotal) * 100d / AtManDaysTotal;
        }

        public double TotalTests { get; }

        public int TotalTestsRunnable { get; }

        public int TotalRuns { get; }

        public double HrsToWriteTests { get; }

        public double MtManDaysSavedPerRun { get; }

        public double MtManDaysSavedTotal { get; }

        public double AtManDaysTotal { get; }

        public double ROI { get; }

        public override string ToString() =>
            $"{TotalTestsRunnable}\t{TotalTests:F1}\t{TotalRuns}\t{HrsToWriteTests:F1}\t{MtManDaysSavedPerRun:F1}\t{MtManDaysSavedTotal:F1}\t{AtManDaysTotal:F1}\t{ROI:F1}%";
    }

}


