"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.RoiCalculator = exports.RoiForecast = exports.RoiEntry = void 0;
// Constants specific to ROI calculation
var DAY_HOURS = 8;
var WEEK_DAYS = 5;
// Represents a single point in the forecast series
var RoiEntry = /** @class */ (function () {
    function RoiEntry(totalTests, totalRuns, hrsToWriteTests, mtManDaysSavedPerRun, mtManDaysSavedTotal, atManDaysTotal) {
        if (totalTests === void 0) { totalTests = 0; }
        if (totalRuns === void 0) { totalRuns = 0; }
        if (hrsToWriteTests === void 0) { hrsToWriteTests = 0; }
        if (mtManDaysSavedPerRun === void 0) { mtManDaysSavedPerRun = 0; }
        if (mtManDaysSavedTotal === void 0) { mtManDaysSavedTotal = 0; }
        if (atManDaysTotal === void 0) { atManDaysTotal = 0; }
        this.TotalTests = totalTests;
        // Ensure runnable tests are non-negative integers
        this.TotalTestsRunnable = Math.max(0, Math.floor(totalTests));
        this.TotalRuns = totalRuns;
        this.HrsToWriteTests = hrsToWriteTests;
        this.MtManDaysSavedPerRun = mtManDaysSavedPerRun;
        this.MtManDaysSavedTotal = mtManDaysSavedTotal;
        this.AtManDaysTotal = atManDaysTotal;
        // Calculate ROI: (Benefit - Cost) / Cost * 100
        // Benefit = mtManDaysSavedTotal, Cost = atManDaysTotal
        // Handle division by zero or negative cost (results in -100% ROI initially)
        this.ROI = (atManDaysTotal > 0) ? ((mtManDaysSavedTotal - atManDaysTotal) * 100) / atManDaysTotal : (mtManDaysSavedTotal > 0 ? Infinity : -100);
        // Avoid division by zero if AT cost is 0 but MT savings exist (infinite ROI conceptually)
        if (atManDaysTotal === 0 && mtManDaysSavedTotal > 0) {
            this.ROI = Infinity; // Or handle as a very large number if needed for charting
        }
        else if (atManDaysTotal <= 0) {
            this.ROI = -100; // Default to -100% if no cost incurred yet
        }
        else {
            this.ROI = ((mtManDaysSavedTotal - atManDaysTotal) * 100) / atManDaysTotal;
        }
    }
    return RoiEntry;
}());
exports.RoiEntry = RoiEntry;
// Represents the entire forecast result
var RoiForecast = /** @class */ (function () {
    function RoiForecast() {
        // Start with initial empty entry (Week 0 state)
        this.series = [new RoiEntry()];
        this.zeroReachIndex = -1;
    }
    RoiForecast.prototype.addEntry = function (entry) {
        this.series.push(entry);
        // Update zeroReachIndex only if it hasn't been found yet and current ROI is >= 0
        // Ensure we don't count the initial Week 0 entry for the index
        if (this.zeroReachIndex === -1 && entry.ROI >= 0 && this.series.length > 1) {
            this.zeroReachIndex = this.series.length - 1; // -1 because series includes Week 0
        }
    };
    return RoiForecast;
}());
exports.RoiForecast = RoiForecast;
// The main calculator class
var RoiCalculator = /** @class */ (function () {
    function RoiCalculator(inputs) {
        // Input validation could be added here
        this._inputs = inputs;
        this._leadHours = this.getLeadHours();
    }
    RoiCalculator.prototype.getLeadHours = function () {
        // Simple lead hours calculation based on team size (per person per day)
        if (this._inputs.TeamCount > 10)
            return 4 / DAY_HOURS; // 4 hrs/day / 8 hrs/day
        if (this._inputs.TeamCount > 5)
            return 2 / DAY_HOURS;
        if (this._inputs.TeamCount > 1)
            return 1 / DAY_HOURS;
        return 0;
    };
    // Calculates the state for the *next* week based on the *last* week's state
    RoiCalculator.prototype.calculateRoiEntry = function (last, timeToDevelopTafRemainingInWeek) {
        var totalWeekManHrs = this._inputs.TeamCount * DAY_HOURS * WEEK_DAYS;
        var lastTotalTestsRunnable = Math.max(0, Math.floor(last.TotalTestsRunnable));
        var failedTestsInPeriodByAtReasons = lastTotalTestsRunnable * this._inputs.AtFailsRatio * this._inputs.BuildsPerPeriod;
        var failedTestsInPeriodByDevReasons = lastTotalTestsRunnable * this._inputs.DevFailsRatio * this._inputs.BuildsPerPeriod;
        var hrsToFixAutoTestsPerWeek = failedTestsInPeriodByAtReasons * this._inputs.AutoTestFixHrs;
        var hrsToAnalyzeDevFailsPerWeek = failedTestsInPeriodByDevReasons * this._inputs.FailAnalyzeHrs;
        // Total lead hours for the whole team per week
        var leadHrsPerWeek = this._leadHours * DAY_HOURS * this._inputs.TeamCount * WEEK_DAYS;
        var otherManHrsPerWeek = this._inputs.NonDevHrs * this._inputs.TeamCount * WEEK_DAYS;
        // Calculate hours spent on non-test-writing activities this week
        var nonTestWritingHrs = hrsToFixAutoTestsPerWeek
            + hrsToAnalyzeDevFailsPerWeek
            + otherManHrsPerWeek
            + leadHrsPerWeek
            + timeToDevelopTafRemainingInWeek; // TAF hours spent THIS week
        // Calculate hours available for writing new tests
        var manHrsToWriteTests = Math.max(0, totalWeekManHrs - nonTestWritingHrs);
        // Calculate how many new tests were written
        var writtenTests = (this._inputs.AutoTestCreationHrs > 0)
            ? Math.max(0, manHrsToWriteTests / this._inputs.AutoTestCreationHrs)
            : 0;
        // --- Update Totals ---
        var totalTests = last.TotalTests + writtenTests;
        var totalRuns = last.TotalRuns + this._inputs.BuildsPerPeriod;
        // AT Man Days spent = Previous total + (Total hours this week / Hours per day)
        // The cost is the full team effort for the week.
        var atManDaysThisWeek = totalWeekManHrs / DAY_HOURS;
        var atManDaysTotal = last.AtManDaysTotal + atManDaysThisWeek;
        // --- MT Savings Calculation ---
        // Savings per run based on *last* week's runnable tests
        var mtManHrsSavedPerRun = lastTotalTestsRunnable * this._inputs.ManualTestExecutionHrs;
        var mtManDaysSavedPerRun = mtManHrsSavedPerRun / DAY_HOURS;
        // Total savings this week = (Savings per run * # Runs) + (Time saved analyzing dev fails)
        var mtManDaysSavedThisWeek = (this._inputs.BuildsPerPeriod * mtManDaysSavedPerRun) + (hrsToAnalyzeDevFailsPerWeek / DAY_HOURS);
        var mtManDaysSavedTotal = last.MtManDaysSavedTotal + mtManDaysSavedThisWeek;
        return new RoiEntry(totalTests, totalRuns, manHrsToWriteTests, mtManDaysSavedPerRun, // Saved per run *this* week (based on last week's tests)
        mtManDaysSavedTotal, // Cumulative savings
        atManDaysTotal // Cumulative cost
        );
    };
    // Calculates the forecast for the specified number of periods (weeks)
    RoiCalculator.prototype.calculateForecast = function () {
        var forecast = new RoiForecast();
        var timeToDevelopTafRemainingTotal = this._inputs.TafCreationHrs; // Total TAF hours needed
        var manHrsPerWeek = this._inputs.TeamCount * DAY_HOURS * WEEK_DAYS;
        for (var i = 0; i < this._inputs.PeriodsCount; i++) {
            // Get the state from the *end* of the previous week (or initial state)
            var last = forecast.series[forecast.series.length - 1];
            // Determine how many TAF hours are spent *this* specific week
            // Can't spend more TAF hours than remaining, or more than total available hours
            var tafHrsThisWeek = Math.min(timeToDevelopTafRemainingTotal, manHrsPerWeek);
            var roiEntry = void 0;
            // If TAF development consumes the entire week's effort or more
            if (tafHrsThisWeek >= manHrsPerWeek) {
                // No tests written, no MT savings generated this week (only analysis savings), cost increases
                var hrsToAnalyzeDevFailsPerWeek = (Math.max(0, Math.floor(last.TotalTestsRunnable)) * this._inputs.DevFailsRatio * this._inputs.BuildsPerPeriod) * this._inputs.FailAnalyzeHrs;
                var mtManDaysSavedThisWeek = hrsToAnalyzeDevFailsPerWeek / DAY_HOURS;
                var atManDaysTotal = last.AtManDaysTotal + (manHrsPerWeek / DAY_HOURS);
                roiEntry = new RoiEntry(last.TotalTests, // No new tests
                last.TotalRuns + this._inputs.BuildsPerPeriod, // Runs still happen
                0, // 0 hours spent writing tests
                0, // 0 MT savings *per run* this week (as no new tests contribute)
                last.MtManDaysSavedTotal + mtManDaysSavedThisWeek, // Cumulative savings increase only by analysis time saved
                atManDaysTotal // Cost increases by full week
                );
                timeToDevelopTafRemainingTotal -= manHrsPerWeek; // Deduct a full week's worth of potential TAF effort
            }
            else {
                // Calculate normally, passing only the TAF hours spent *this week*
                // These TAF hours reduce the time available for writing tests this week
                roiEntry = this.calculateRoiEntry(last, tafHrsThisWeek);
                timeToDevelopTafRemainingTotal = 0; // All remaining TAF hours are consumed this week
            }
            forecast.addEntry(roiEntry);
        }
        forecast.series.shift(); // Remove the initial Week 0 entry before returning
        return forecast;
    };
    return RoiCalculator;
}());
exports.RoiCalculator = RoiCalculator;
//# sourceMappingURL=roi-calculator.js.map