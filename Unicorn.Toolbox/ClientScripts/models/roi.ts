export interface RoiInputs {
    ManualTestExecutionHrs: number;
    FailAnalyzeHrs: number;
    TafCreationHrs: number;
    AutoTestCreationHrs: number;
    AutoTestFixHrs: number;
    AtFailsRatio: number;
    DevFailsRatio: number;
    NonDevHrs: number;
    BuildsPerPeriod: number;
    TeamCount: number;
    PeriodsCount: number;
}

// Defined in roi-calculator.ts but can be exported from here too if desired
// export interface RoiEntry { ... }
// export interface RoiForecast { ... }