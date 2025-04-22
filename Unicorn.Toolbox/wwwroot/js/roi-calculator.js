// js/roi-calculator.js

// Constants specific to ROI calculation
const DAY_HOURS = 8;
const WEEK_DAYS = 5;

// Equivalent of RoiEntry class
class RoiEntry {
    constructor(totalTests = 0, totalRuns = 0, hrsToWriteTests = 0, mtManDaysSavedPerRun = 0, mtManDaysSavedTotal = 0, atManDaysTotal = 0) {
        this.TotalTests = totalTests;
        this.TotalTestsRunnable = Math.floor(totalTests);
        this.TotalRuns = totalRuns;
        this.HrsToWriteTests = hrsToWriteTests;
        this.MtManDaysSavedPerRun = mtManDaysSavedPerRun;
        this.MtManDaysSavedTotal = mtManDaysSavedTotal;
        this.AtManDaysTotal = atManDaysTotal;
        this.ROI = (atManDaysTotal > 0) ? (mtManDaysSavedTotal - atManDaysTotal) * 100 / atManDaysTotal : -100;
    }
}

// Equivalent of RoiForecast class
class RoiForecast {
    constructor() {
        this.series = [new RoiEntry()]; // Start with initial empty entry
        this.zeroReachIndex = -1;
    }

    addEntry(entry) {
        this.series.push(entry);
        if (this.zeroReachIndex === -1 && entry.ROI > 0) {
            this.zeroReachIndex = this.series.length - 1;
        }
    }
}

// Equivalent of Calculator class
class RoiCalculator {
    constructor(inputs) {
        this._inputs = inputs;
        this._leadHours = this.getLeadHours();
    }

    getLeadHours() {
        if (this._inputs.TeamCount > 10) return 4;
        if (this._inputs.TeamCount > 5) return 2;
        if (this._inputs.TeamCount > 1) return 1;
        return 0;
    }

    calculateRoiEntry(last, timeToDevelopTafRemainingInWeek) {
        const totalWeekManHrs = this._inputs.TeamCount * DAY_HOURS * WEEK_DAYS;
        const lastTotalTestsRunnable = Math.max(0, Math.floor(last.TotalTestsRunnable));

        const failedTestsInPeriodByAtReasons = lastTotalTestsRunnable * this._inputs.AtFailsRatio * this._inputs.BuildsPerPeriod;
        const failedTestsInPeriodByDevReasons = lastTotalTestsRunnable * this._inputs.DevFailsRatio * this._inputs.BuildsPerPeriod;

        const hrsToFixAutoTestsPerWeek = failedTestsInPeriodByAtReasons * this._inputs.AutoTestFixHrs;
        const hrsToAnalyzeDevFailsPerWeek = failedTestsInPeriodByDevReasons * this._inputs.FailAnalyzeHrs;

        const otherManHrsPerWeek = this._inputs.NonDevHrs * this._inputs.TeamCount * WEEK_DAYS;
        const leadHrsPerWeek = this._leadHours * WEEK_DAYS; // Use weekly lead hours

        // Calculate available hours, ensuring it doesn't exceed total available, accounting for TAF hours for *this* week
        const nonTestWritingHrs = hrsToFixAutoTestsPerWeek + hrsToAnalyzeDevFailsPerWeek + otherManHrsPerWeek + leadHrsPerWeek + timeToDevelopTafRemainingInWeek;
        let manHrsToWriteTests = Math.max(0, totalWeekManHrs - nonTestWritingHrs);

        const writtenTests = (this._inputs.AutoTestCreationHrs > 0) ? Math.max(0, manHrsToWriteTests / this._inputs.AutoTestCreationHrs) : 0;

        const totalTests = last.TotalTests + writtenTests;
        const totalRuns = last.TotalRuns + this._inputs.BuildsPerPeriod;
        // AT Man Days spent = Previous total + (Total hours this week / Hours per day)
        const atManDaysTotal = last.AtManDaysTotal + (totalWeekManHrs / DAY_HOURS);

        // MT Savings Calculation
        const mtManDaysSavedPerRun = (lastTotalTestsRunnable * this._inputs.ManualTestExecutionHrs) / DAY_HOURS;
        const mtManDaysSavedTotal = last.MtManDaysSavedTotal + (this._inputs.BuildsPerPeriod * mtManDaysSavedPerRun) + (hrsToAnalyzeDevFailsPerWeek / DAY_HOURS);

        return new RoiEntry(totalTests, totalRuns, manHrsToWriteTests, mtManDaysSavedPerRun, mtManDaysSavedTotal, atManDaysTotal);
    }


    calculateForecast() {
        const forecast = new RoiForecast();
        let timeToDevelopTafTotal = this._inputs.TafCreationHrs; // Total TAF hours needed
        const manHrsPerWeek = this._inputs.TeamCount * DAY_HOURS * WEEK_DAYS;

        for (let i = 0; i < this._inputs.PeriodsCount; i++) {
            const last = forecast.series[forecast.series.length - 1];
            let roiEntry;

            // Determine how many TAF hours are spent *this week*
            const tafHrsThisWeek = Math.min(timeToDevelopTafTotal, manHrsPerWeek);

            // If TAF development consumes the entire week's effort
            if (tafHrsThisWeek >= manHrsPerWeek) {
                const atManDaysTotal = last.AtManDaysTotal + (manHrsPerWeek / DAY_HOURS);
                roiEntry = new RoiEntry(last.TotalTests, last.TotalRuns, 0, 0, last.MtManDaysSavedTotal, atManDaysTotal);
                timeToDevelopTafTotal -= manHrsPerWeek; // Deduct a full week's worth
            } else {
                // Calculate normally, passing only the TAF hours spent *this week*
                roiEntry = this.calculateRoiEntry(last, tafHrsThisWeek);
                timeToDevelopTafTotal = 0; // All TAF hours are now accounted for
            }

            forecast.addEntry(roiEntry);
        }

        forecast.series.shift(); // Remove the initial empty entry
        return forecast;
    }
}

// Export the necessary class
export { RoiCalculator, RoiEntry, RoiForecast }; // Export Entry/Forecast if needed elsewhere