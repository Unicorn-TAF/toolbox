import { RoiInputs } from '../models/roi.js';

// Constants specific to ROI calculation
const DAY_HOURS: number = 8;
const WEEK_DAYS: number = 5;

// Represents a single point in the forecast series
export class RoiEntry {
    TotalTests: number;
    TotalTestsRunnable: number;
    TotalRuns: number;
    HrsToWriteTests: number;
    MtManDaysSavedPerRun: number;
    MtManDaysSavedTotal: number;
    AtManDaysTotal: number;
    ROI: number; // Percentage

    constructor(
        totalTests: number = 0,
        totalRuns: number = 0,
        hrsToWriteTests: number = 0,
        mtManDaysSavedPerRun: number = 0,
        mtManDaysSavedTotal: number = 0,
        atManDaysTotal: number = 0
    ) {
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
        } else if (atManDaysTotal <= 0) {
            this.ROI = -100; // Default to -100% if no cost incurred yet
        } else {
            this.ROI = ((mtManDaysSavedTotal - atManDaysTotal) * 100) / atManDaysTotal;
        }

    }
}

// Represents the entire forecast result
export class RoiForecast {
    series: RoiEntry[];
    zeroReachIndex: number; // 1-based index of the week when ROI >= 0

    constructor() {
        // Start with initial empty entry (Week 0 state)
        this.series = [new RoiEntry()];
        this.zeroReachIndex = -1;
    }

    addEntry(entry: RoiEntry): void {
        this.series.push(entry);
        // Update zeroReachIndex only if it hasn't been found yet and current ROI is >= 0
        // Ensure we don't count the initial Week 0 entry for the index
        if (this.zeroReachIndex === -1 && entry.ROI >= 0 && this.series.length > 1) {
            this.zeroReachIndex = this.series.length - 1; // -1 because series includes Week 0
        }
    }
}

// The main calculator class
export class RoiCalculator {
    private readonly _inputs: RoiInputs;
    private readonly _leadHours: number; // Calculated lead hours per person per day

    constructor(inputs: RoiInputs) {
        // Input validation could be added here
        this._inputs = inputs;
        this._leadHours = this.getLeadHours();
    }

    private getLeadHours(): number {
        // Simple lead hours calculation based on team size (per person per day)
        if (this._inputs.TeamCount > 10) return 4 / DAY_HOURS; // 4 hrs/day / 8 hrs/day
        if (this._inputs.TeamCount > 5) return 2 / DAY_HOURS;
        if (this._inputs.TeamCount > 1) return 1 / DAY_HOURS;
        return 0;
    }

    // Calculates the state for the *next* week based on the *last* week's state
    private calculateRoiEntry(last: RoiEntry, timeToDevelopTafRemainingInWeek: number): RoiEntry {
        const totalWeekManHrs = this._inputs.TeamCount * DAY_HOURS * WEEK_DAYS;
        const lastTotalTestsRunnable = Math.max(0, Math.floor(last.TotalTestsRunnable));

        const failedTestsInPeriodByAtReasons = lastTotalTestsRunnable * this._inputs.AtFailsRatio * this._inputs.BuildsPerPeriod;
        const failedTestsInPeriodByDevReasons = lastTotalTestsRunnable * this._inputs.DevFailsRatio * this._inputs.BuildsPerPeriod;

        const hrsToFixAutoTestsPerWeek = failedTestsInPeriodByAtReasons * this._inputs.AutoTestFixHrs;
        const hrsToAnalyzeDevFailsPerWeek = failedTestsInPeriodByDevReasons * this._inputs.FailAnalyzeHrs;

        // Total lead hours for the whole team per week
        const leadHrsPerWeek = this._leadHours * DAY_HOURS * this._inputs.TeamCount * WEEK_DAYS;
        const otherManHrsPerWeek = this._inputs.NonDevHrs * this._inputs.TeamCount * WEEK_DAYS;


        // Calculate hours spent on non-test-writing activities this week
        const nonTestWritingHrs = hrsToFixAutoTestsPerWeek
            + hrsToAnalyzeDevFailsPerWeek
            + otherManHrsPerWeek
            + leadHrsPerWeek
            + timeToDevelopTafRemainingInWeek; // TAF hours spent THIS week

        // Calculate hours available for writing new tests
        const manHrsToWriteTests = Math.max(0, totalWeekManHrs - nonTestWritingHrs);

        // Calculate how many new tests were written
        const writtenTests = (this._inputs.AutoTestCreationHrs > 0)
            ? Math.max(0, manHrsToWriteTests / this._inputs.AutoTestCreationHrs)
            : 0;

        // --- Update Totals ---
        const totalTests = last.TotalTests + writtenTests;
        const totalRuns = last.TotalRuns + this._inputs.BuildsPerPeriod;

        // AT Man Days spent = Previous total + (Total hours this week / Hours per day)
        // The cost is the full team effort for the week.
        const atManDaysThisWeek = totalWeekManHrs / DAY_HOURS;
        const atManDaysTotal = last.AtManDaysTotal + atManDaysThisWeek;

        // --- MT Savings Calculation ---
        // Savings per run based on *last* week's runnable tests
        const mtManHrsSavedPerRun = lastTotalTestsRunnable * this._inputs.ManualTestExecutionHrs;
        const mtManDaysSavedPerRun = mtManHrsSavedPerRun / DAY_HOURS;

        // Total savings this week = (Savings per run * # Runs) + (Time saved analyzing dev fails)
        const mtManDaysSavedThisWeek = (this._inputs.BuildsPerPeriod * mtManDaysSavedPerRun) + (hrsToAnalyzeDevFailsPerWeek / DAY_HOURS);
        const mtManDaysSavedTotal = last.MtManDaysSavedTotal + mtManDaysSavedThisWeek;

        return new RoiEntry(
            totalTests,
            totalRuns,
            manHrsToWriteTests,
            mtManDaysSavedPerRun, // Saved per run *this* week (based on last week's tests)
            mtManDaysSavedTotal,  // Cumulative savings
            atManDaysTotal        // Cumulative cost
        );
    }

    // Calculates the forecast for the specified number of periods (weeks)
    public calculateForecast(): RoiForecast {
        const forecast = new RoiForecast();
        let timeToDevelopTafRemainingTotal = this._inputs.TafCreationHrs; // Total TAF hours needed
        const manHrsPerWeek = this._inputs.TeamCount * DAY_HOURS * WEEK_DAYS;

        for (let i = 0; i < this._inputs.PeriodsCount; i++) {
            // Get the state from the *end* of the previous week (or initial state)
            const last = forecast.series[forecast.series.length - 1];

            // Determine how many TAF hours are spent *this* specific week
            // Can't spend more TAF hours than remaining, or more than total available hours
            const tafHrsThisWeek = Math.min(timeToDevelopTafRemainingTotal, manHrsPerWeek);

            let roiEntry: RoiEntry;

            // If TAF development consumes the entire week's effort or more
            if (tafHrsThisWeek >= manHrsPerWeek) {
                // No tests written, no MT savings generated this week (only analysis savings), cost increases
                const hrsToAnalyzeDevFailsPerWeek = (Math.max(0, Math.floor(last.TotalTestsRunnable)) * this._inputs.DevFailsRatio * this._inputs.BuildsPerPeriod) * this._inputs.FailAnalyzeHrs;
                const mtManDaysSavedThisWeek = hrsToAnalyzeDevFailsPerWeek / DAY_HOURS;
                const atManDaysTotal = last.AtManDaysTotal + (manHrsPerWeek / DAY_HOURS);

                roiEntry = new RoiEntry(
                    last.TotalTests, // No new tests
                    last.TotalRuns + this._inputs.BuildsPerPeriod, // Runs still happen
                    0, // 0 hours spent writing tests
                    0, // 0 MT savings *per run* this week (as no new tests contribute)
                    last.MtManDaysSavedTotal + mtManDaysSavedThisWeek, // Cumulative savings increase only by analysis time saved
                    atManDaysTotal // Cost increases by full week
                );
                timeToDevelopTafRemainingTotal -= manHrsPerWeek; // Deduct a full week's worth of potential TAF effort
            } else {
                // Calculate normally, passing only the TAF hours spent *this week*
                // These TAF hours reduce the time available for writing tests this week
                roiEntry = this.calculateRoiEntry(last, tafHrsThisWeek);
                timeToDevelopTafRemainingTotal = 0; // All remaining TAF hours are consumed this week
            }

            forecast.addEntry(roiEntry);
        }

        forecast.series.shift(); // Remove the initial Week 0 entry before returning
        return forecast;
    }
}