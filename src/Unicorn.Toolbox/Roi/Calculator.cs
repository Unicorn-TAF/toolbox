namespace Unicorn.Toolbox.Roi
{
    public sealed class Calculator
    {
        private const int DayHours = 8;
        private const int WeekDays = 5;

        private readonly RoiInputs _inputs;
        private readonly double _leadHours;

        public Calculator(RoiInputs inputs)
        {
            _inputs = inputs;
            _leadHours = GetLeadHours();
        }

        public RoiForecast CalculateForecast()
        {
            RoiForecast forecast = new RoiForecast();
            double timeToDevelopTaf = _inputs.TafCreationHrs;

            for (int i = 0; i < _inputs.PeriodsCount; i++)
            {
                RoiEntry last = forecast.Series[forecast.Series.Count - 1];
                RoiEntry roiEntry;

                if (timeToDevelopTaf >= DayHours * WeekDays)
                {
                    double atManDaysTotal = last.AtManDaysTotal + WeekDays;
                    roiEntry = new RoiEntry(0, 0, 0, 0, 0, atManDaysTotal);
                    timeToDevelopTaf -= DayHours * WeekDays;
                }
                else
                {
                    roiEntry = CalculateRoiEntry(last, timeToDevelopTaf);
                    timeToDevelopTaf = 0;
                }

                forecast.AddEntry(roiEntry);
            }

            forecast.Series.RemoveAt(0);
            return forecast;
        }

        private RoiEntry CalculateRoiEntry(RoiEntry last, double timeToDevelopTaf)
        {
            // Total man*hours in period
            double totalWeekManHrs = _inputs.TeamCount * DayHours * WeekDays;

            // Total failed tests across the period
            int failedTestsInPeriodByAtReasons = (int)(last.TotalTestsRunnable * _inputs.AtFailsRatio * _inputs.BuildsPerPeriod);
            int failedTestsInPeriodByDevReasons = (int)(last.TotalTestsRunnable * _inputs.DevFailsRatio * _inputs.BuildsPerPeriod);

            double hrsToFixAutoTestsPerWeek = failedTestsInPeriodByAtReasons * _inputs.AutoTestFixHrs;
            double hrsToAnalyzeDevFailsPerWeek = failedTestsInPeriodByDevReasons * _inputs.FailAnalyzeHrs;

            double otherManHrsPerWeek = _inputs.NonDevHrs * _inputs.TeamCount * WeekDays;
            double leadHrs = _leadHours * WeekDays;

            double manHrsToWriteTests = totalWeekManHrs - hrsToFixAutoTestsPerWeek - hrsToAnalyzeDevFailsPerWeek - otherManHrsPerWeek - leadHrs - timeToDevelopTaf;

            double writtenTests = manHrsToWriteTests / _inputs.AutoTestCreationHrs;


            double totalTests = last.TotalTests + writtenTests;
            int totalRuns = last.TotalRuns + _inputs.BuildsPerPeriod;
            double atManDaysTotal = last.AtManDaysTotal + totalWeekManHrs / DayHours;
            double mtManDaysSavedPerRun = last.TotalTestsRunnable * _inputs.ManualTestExecutionHrs / DayHours;

            double mtManDaysSavedTotal = last.MtManDaysSavedTotal + _inputs.BuildsPerPeriod * mtManDaysSavedPerRun + hrsToAnalyzeDevFailsPerWeek;

            return new RoiEntry(totalTests, totalRuns, manHrsToWriteTests, mtManDaysSavedPerRun, mtManDaysSavedTotal, atManDaysTotal);
        }

        private double GetLeadHours()
        {
            if (_inputs.TeamCount > 10)
            {
                return 4;
            }
            else if (_inputs.TeamCount > 5)
            {
                return 2;
            }
            else if (_inputs.TeamCount > 1)
            {
                return 1;
            }

            return 0;
        }
    }
}

