using System.Text;

namespace Unicorn.Toolbox.Roi
{
    public sealed class RoiInputs
    {
        public double ManualTestExecutionHrs { get; set; } = 10 / 60d; // 10 min

        public double FailAnalyzeHrs { get; set; } = 5 / 60d; // ~5 min

        public double TafCreationHrs { get; set; } = 80;

        public double AutoTestCreationHrs { get; set; } = 8;

        public double AutoTestFixHrs { get; set; } = 2;

        public double AtFailsRatio { get; set; } = 0.025; // 2.5%

        public double DevFailsRatio { get; set; } = 0.025; // 25%

        public double NonDevHrs { get; set; } = 0.5;

        public int BuildsPerPeriod { get; set; } = 5;

        public int TeamCount { get; set; } = 4;

        public double AtVsMtSalaryRatio { get; set; } = 1.5;

        public int PeriodsCount { get; set; } = 54;

        public override string ToString() =>
            new StringBuilder()
            .AppendFormat("mt-exec: {0:F1}    ", ManualTestExecutionHrs)
            .AppendFormat("fail-analyze: {0:F1}    ", FailAnalyzeHrs)
            .AppendFormat("taf-create: {0:F1}    ", TafCreationHrs)
            .AppendFormat("at-create: {0:F1}    ", AutoTestCreationHrs)
            .AppendFormat("at-fix: {0:F1}    ", AutoTestFixHrs)
            .AppendFormat("at-fails-pct: {0:F1}    ", AtFailsRatio * 100)
            .AppendFormat("dev-fails-pct: {0:F1}    ", DevFailsRatio * 100)
            .AppendFormat("other-time: {0:F1}    ", NonDevHrs)
            .AppendFormat("week-builds: {0}    ", BuildsPerPeriod)
            .AppendFormat("at-team: {0}", TeamCount)
            .ToString();
    }
}
