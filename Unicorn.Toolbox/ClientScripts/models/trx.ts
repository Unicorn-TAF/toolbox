export interface TestData {
    testName: string;
    startTime: Date | null;
    endTime: Date | null;
    durationMs: number;
    outcome: string;
    testListId: string | null;
    errorMessage: string | null;
    stackTrace: string | null;
    // Can add suiteName here if preferred over getting from map later
    suiteName?: string; // Optional: store resolved suite name directly
}

export interface TrxFileData {
    fileName: string;
    runStartTime: Date | null;
    runEndTime: Date | null;
    tests: TestData[];
    testListMap: Map<string, string>; // Map<id, name>
}

export interface TrxSummary {
    totalDurationMin: number;
    rawDurationMin: number;
    suitesCount: number;
    testsCount: number;
    failedTests: number;
    skippedTests: number;
}

export interface AggregatedFailure {
    message: string;
    count: number;
}