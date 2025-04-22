import { TrxFileData, TestData, TrxSummary, AggregatedFailure } from '../models/trx.js';
import { parseDateTimeSafe, parseTimespan } from '../utils/formatters.js';
import { openVisualizationModal, closeVisualizationModal, initializeVisualizationModalControls } from './visualization.js'; // Import modal functions

// --- Module State ---
let analysisTabInitialized: boolean = false;
let aggregatedFailures: Map<string, number> = new Map(); // Key: Normalized error message, Value: count
let allTrxData: TrxFileData[] = []; // Stores processed data for all loaded files
let processedFileNames: Set<string> = new Set(); // Stores names of files processed in the current load

// --- DOM Element Fetching ---
function getAnalysisElements() {
    const getElement = <T extends HTMLElement>(id: string): T | null => document.getElementById(id) as T | null;
    return {
        loadTrxButton: getElement<HTMLButtonElement>('loadTrxButton'),
        trxFileLoader: getElement<HTMLInputElement>('trxFileLoader'),
        resultsTable: getElement<HTMLTableElement>('resultsTable'),
        resultsTableBody: getElement<HTMLTableSectionElement>('resultsTableBody'),
        topFailsListContainer: getElement<HTMLDivElement>('topFailsList'),
        loadStatusSpan: getElement<HTMLSpanElement>('loadStatus'),
        visualizeButton: getElement<HTMLButtonElement>('visualizeButton'),
        searchButton: getElement<HTMLButtonElement>('searchFailsButton'),
        failFilterTypeSelect: getElement<HTMLSelectElement>('failFilterType'),
        failSearchCriteriaInput: getElement<HTMLInputElement>('failSearchCriteria'),
    };
}

// --- TRX Parsing and Processing ---
function extractTrxSummary(xmlDoc: XMLDocument): TrxSummary | null {
    try {
        const timesNode = xmlDoc.getElementsByTagName("Times")[0];
        const summaryNode = xmlDoc.getElementsByTagName("ResultSummary")[0];
        const countersNode = summaryNode?.getElementsByTagName("Counters")[0];
        const resultsNode = xmlDoc.getElementsByTagName("Results")[0];
        const testListsContainer = xmlDoc.getElementsByTagName("TestLists")[0];
        const testLists = testListsContainer ? testListsContainer.getElementsByTagName("TestList") : [];

        if (!timesNode || !summaryNode || !countersNode) {
            console.warn("TRX file missing essential elements for summary (Times, ResultSummary, Counters).");
            return null;
        }

        const startTimeStr = timesNode.getAttribute("start");
        const finishTimeStr = timesNode.getAttribute("finish");
        let totalDurationMin = 0;
        const startMs = parseDateTimeSafe(startTimeStr)?.getTime();
        const finishMs = parseDateTimeSafe(finishTimeStr)?.getTime();

        if (startMs && finishMs && finishMs >= startMs) {
            totalDurationMin = (finishMs - startMs) / (1000 * 60);
        } else { console.warn("Invalid/missing start/finish times:", startTimeStr, finishTimeStr); }

        let rawDurationMin = 0;
        if (resultsNode) {
            const unitTestResults = resultsNode.getElementsByTagName("UnitTestResult");
            let rawDurationMs = 0;
            for (let i = 0; i < unitTestResults.length; i++) {
                const durationStr = unitTestResults[i].getAttribute("duration");
                if (durationStr) { rawDurationMs += parseTimespan(durationStr); }
            }
            rawDurationMin = rawDurationMs / (1000 * 60);
        } else { console.warn("Missing <Results> node, cannot calculate Raw Duration."); }

        const getCounter = (name: string): number => parseInt(countersNode?.getAttribute(name) || "0", 10) || 0;

        const totalTests = getCounter("total");
        const failedTests = getCounter("failed");
        const errorTests = getCounter("error");
        const timeoutTests = getCounter("timeout");
        const abortedTests = getCounter("aborted");
        const inconclusiveTests = getCounter("inconclusive");
        const notExecutedTests = getCounter("notExecuted");
        const pendingTests = getCounter("pending");
        const notRunnableTests = getCounter("notRunnable");


        // Define 'Skipped' and 'Failed' broadly
        const skipped = notExecutedTests + inconclusiveTests + pendingTests + notRunnableTests;
        const robustFailed = failedTests + errorTests + timeoutTests + abortedTests;
        const suitesCount = testLists.length;

        return {
            totalDurationMin: totalDurationMin,
            rawDurationMin: rawDurationMin,
            suitesCount: suitesCount,
            testsCount: totalTests,
            failedTests: robustFailed,
            skippedTests: skipped
        };
    } catch (error) {
        console.error("Error extracting summary data:", error);
        return null;
    }
}

interface ProcessedTrxResult {
    fileName: string;
    summary: TrxSummary | null;
    data: TrxFileData | null; // Can be null if only summary was possible or error occurred
    errorType?: string; // Optional error indicator
}

async function processTrxFile(file: File): Promise<ProcessedTrxResult> {
    return new Promise((resolve) => {
        const reader = new FileReader();

        reader.onload = (e) => {
            const result: ProcessedTrxResult = {
                fileName: file.name,
                summary: null,
                data: null
            };

            try {
                const xmlContent = e.target?.result as string;
                if (!xmlContent) throw new Error("File content is empty.");

                const parser = new DOMParser();
                const xmlDoc = parser.parseFromString(xmlContent, "text/xml");
                const parseError = xmlDoc.querySelector("parsererror");

                if (parseError) {
                    console.error(`Error parsing TRX file "${file.name}":`, parseError.textContent);
                    result.errorType = "XML Parse Error";
                    resolve(result);
                    return;
                }

                result.summary = extractTrxSummary(xmlDoc);

                const currentTrxData: TrxFileData = {
                    fileName: file.name,
                    runStartTime: null,
                    runEndTime: null,
                    tests: [],
                    testListMap: new Map()
                };

                const timesNode = xmlDoc.getElementsByTagName("Times")[0];
                if (timesNode) {
                    currentTrxData.runStartTime = parseDateTimeSafe(timesNode.getAttribute("start"));
                    currentTrxData.runEndTime = parseDateTimeSafe(timesNode.getAttribute("finish"));
                }

                const testListsContainer = xmlDoc.getElementsByTagName("TestLists")[0];
                testListsContainer?.querySelectorAll("TestList").forEach(listNode => {
                    const id = listNode.getAttribute("id");
                    const name = listNode.getAttribute("name");
                    if (id && name) currentTrxData.testListMap.set(id, name);
                });

                const unitTestResults = xmlDoc.getElementsByTagName("UnitTestResult");
                for (let i = 0; i < unitTestResults.length; i++) {
                    const testResultNode = unitTestResults[i];
                    const outcome = testResultNode.getAttribute("outcome") || "Unknown";
                    const testName = testResultNode.getAttribute("testName") || `Unnamed Test ${i + 1}`;
                    const testListId = testResultNode.getAttribute("testListId");
                    const startTime = parseDateTimeSafe(testResultNode.getAttribute("startTime"));
                    let endTime = parseDateTimeSafe(testResultNode.getAttribute("endTime"));
                    const durationStr = testResultNode.getAttribute("duration");
                    const durationMs = durationStr ? parseTimespan(durationStr) : 0;

                    if (!endTime && startTime && durationMs > 0) {
                        endTime = new Date(startTime.getTime() + durationMs);
                    }

                    let errorMessage: string | null = null;
                    let stackTrace: string | null = null;

                    if (outcome === "Failed") {
                        const errorInfo = testResultNode.getElementsByTagName("ErrorInfo")[0];
                        errorMessage = errorInfo?.getElementsByTagName("Message")[0]?.textContent?.trim() || null;
                        stackTrace = errorInfo?.getElementsByTagName("StackTrace")[0]?.textContent?.trim() || null;
                        if (errorMessage) {
                            const normalizedMessage = errorMessage;
                            aggregatedFailures.set(normalizedMessage, (aggregatedFailures.get(normalizedMessage) || 0) + 1);
                        }
                    }

                    const testData: TestData = {
                        testName: testName,
                        startTime: startTime,
                        endTime: endTime,
                        durationMs: durationMs,
                        outcome: outcome,
                        testListId: testListId,
                        errorMessage: errorMessage,
                        stackTrace: stackTrace,
                        suiteName: testListId ? (currentTrxData.testListMap.get(testListId) || `Suite ${testListId}`) : 'Unknown Suite'
                    };
                    currentTrxData.tests.push(testData);
                }

                result.data = currentTrxData;
                resolve(result);

            } catch (error: any) {
                console.error(`Critical error processing file "${file.name}":`, error);
                result.errorType = error.message || "Processing Error";
                resolve(result);
            }
        };

        reader.onerror = (e) => {
            console.error(`Error reading file "${file.name}":`, e);
            resolve({ fileName: file.name, summary: null, data: null, errorType: "File Read Error" });
        };

        reader.readAsText(file);
    });
}

// --- UI Display Functions ---
function displayTrxSummaryRow(elements: ReturnType<typeof getAnalysisElements>, fileName: string, summary: TrxSummary): void {
    const tableBody = elements.resultsTableBody;
    if (!tableBody) return;
    const row = tableBody.insertRow();
    row.insertCell().textContent = fileName;
    row.insertCell().textContent = summary.totalDurationMin.toFixed(1);
    row.insertCell().textContent = summary.rawDurationMin.toFixed(1);
    row.insertCell().textContent = summary.suitesCount.toString();
    row.insertCell().textContent = summary.testsCount.toString();
    row.insertCell().textContent = summary.failedTests.toString();
    row.insertCell().textContent = summary.skippedTests.toString();
    row.cells[0].title = fileName;
    row.cells[1].title = `Total Duration: ${summary.totalDurationMin.toFixed(3)} min`;
    row.cells[2].title = `Sum of Test Durations: ${summary.rawDurationMin.toFixed(3)} min`;
    row.cells[5].title = `${summary.failedTests} Failed (includes errors/timeouts/aborts)`;
    row.cells[6].title = `${summary.skippedTests} Skipped/Inconclusive/NotRun`;
}

function displayErrorRow(elements: ReturnType<typeof getAnalysisElements>, fileName: string, errorType: string): void {
    const tableBody = elements.resultsTableBody;
    if (!tableBody) return;
    const row = tableBody.insertRow();
    row.classList.add('error-row');
    row.insertCell().textContent = fileName;
    const cellError = row.insertCell();
    cellError.textContent = `Error: ${errorType}`;
    cellError.colSpan = 6;
    cellError.title = `File: ${fileName}\nError Type: ${errorType}`;
}

function displaySkippedDuplicateRow(elements: ReturnType<typeof getAnalysisElements>, fileName: string): void {
    const tableBody = elements.resultsTableBody;
    if (!tableBody) return;
    const row = tableBody.insertRow();
    row.classList.add('skipped-row'); // Add a class for potential styling
    row.insertCell().textContent = fileName;
    const cellInfo = row.insertCell();
    cellInfo.textContent = 'Skipped (duplicate name in this load)';
    cellInfo.colSpan = 6;
    cellInfo.title = `File: ${fileName}\nSkipped because another file with the same name was already selected in this batch.`;
}


function displayTopFails(elements: ReturnType<typeof getAnalysisElements>): void {
    const container = elements.topFailsListContainer;
    if (!container) return;

    const sortedFails: AggregatedFailure[] = Array.from(aggregatedFailures.entries())
        .map(([message, count]) => ({ message, count }))
        .sort((a, b) => b.count - a.count); // Sort descending by count

    const top5Fails = sortedFails.slice(0, 5);
    container.innerHTML = ''; // Clear previous entries

    if (top5Fails.length === 0) {
        container.innerHTML = '<p class="no-data-message">No failures found in loaded files.</p>';
        return;
    }

    top5Fails.forEach(({ message, count }) => {
        const failItemDiv = document.createElement('div');
        failItemDiv.className = 'fail-item';
        failItemDiv.title = `Count: ${count}\nMessage:\n${message}`;

        const messageBoxDiv = document.createElement('div');
        messageBoxDiv.className = 'fail-message-box';
        const preElement = document.createElement('pre');
        preElement.textContent = message;
        messageBoxDiv.appendChild(preElement);

        const countSpan = document.createElement('span');
        countSpan.className = 'fail-count';
        countSpan.textContent = count.toString();
        countSpan.dataset.action = 'show-fails';
        countSpan.dataset.message = message;

        failItemDiv.appendChild(messageBoxDiv);
        failItemDiv.appendChild(countSpan);
        container.appendChild(failItemDiv);
    });
}

// --- Sorting ---
function sortTableByColumn(table: HTMLTableElement, headerCell: HTMLTableCellElement): void {
    const tableBody = table.tBodies[0];
    if (!tableBody) { console.error("Table body not found for sorting."); return; }

    const sortKey = headerCell.dataset.sortKey;
    const sortType = headerCell.dataset.sortType || 'text';
    if (!sortKey) { console.warn("Header cell clicked has no data-sort-key attribute."); return; }

    const currentDirection = headerCell.dataset.sortDirection || 'none';
    const newDirection = currentDirection === 'asc' ? 'desc' : 'asc';

    const headerCells = Array.from(headerCell.parentElement?.children || []) as HTMLTableCellElement[];
    const columnIndex = headerCells.indexOf(headerCell);
    if (columnIndex === -1) { console.error("Could not determine column index."); return; }

    // Separate rows
    const rowsToSort: HTMLTableRowElement[] = [];
    const otherRows: HTMLTableRowElement[] = []; // error-row, skipped-row
    tableBody.querySelectorAll('tr').forEach(row => {
        if (row.classList.contains('error-row') || row.classList.contains('skipped-row')) {
            otherRows.push(row);
        } else {
            rowsToSort.push(row);
        }
    });


    rowsToSort.sort((rowA, rowB) => {
        const cellA = rowA.cells[columnIndex];
        const cellB = rowB.cells[columnIndex];
        const valueA = cellA?.textContent?.trim() ?? '';
        const valueB = cellB?.textContent?.trim() ?? '';

        let compareResult = 0;
        switch (sortType) {
            case 'number':
                const numA = parseFloat(valueA) || 0;
                const numB = parseFloat(valueB) || 0;
                compareResult = numA - numB;
                break;
            case 'text':
            default:
                compareResult = valueA.localeCompare(valueB, undefined, { sensitivity: 'base', numeric: true });
                break;
        }
        return newDirection === 'asc' ? compareResult : -compareResult;
    });

    // Update header styles
    headerCells.forEach(th => {
        if (th !== headerCell && th.dataset.sortKey) {
            th.classList.remove('sorted-asc', 'sorted-desc');
            th.dataset.sortDirection = 'none';
        }
    });
    headerCell.classList.remove('sorted-asc', 'sorted-desc');
    headerCell.classList.add(newDirection === 'asc' ? 'sorted-asc' : 'sorted-desc');
    headerCell.dataset.sortDirection = newDirection;

    // Re-append rows
    rowsToSort.forEach(row => tableBody.appendChild(row)); // Re-appending moves them
    otherRows.forEach(row => tableBody.appendChild(row)); // Append error/skipped rows at the end
}


// --- Event Handlers ---
async function handleTrxFileLoad(event: Event, elements: ReturnType<typeof getAnalysisElements>): Promise<void> {
    const input = event.target as HTMLInputElement;
    const files = input.files;

    if (!files || files.length === 0 || !elements.resultsTableBody || !elements.topFailsListContainer || !elements.loadStatusSpan || !elements.visualizeButton) {
        console.warn("File input event triggered, but no files or essential elements found.");
        if (input) input.value = ''; // Clear input even if no files selected
        return;
    }

    // --- *** CLEAR PREVIOUS STATE AND UI HERE *** ---
    console.log("New file load requested. Clearing previous results...");
    allTrxData = [];
    aggregatedFailures.clear();
    processedFileNames.clear(); // Clear the set for the new load operation
    elements.resultsTableBody.innerHTML = ''; // Clear summary table
    elements.topFailsListContainer.innerHTML = '<p class="no-data-message">Load TRX files to see top failures.</p>'; // Clear top fails
    elements.visualizeButton.disabled = true; // Disable visualize button
    elements.visualizeButton.classList.remove('gradient-active');
    elements.loadStatusSpan.textContent = ''; // Clear status message

    // --- Filter out duplicate filenames within this specific load ---
    const filesToProcess: File[] = [];
    const fileNamesInBatch = new Set<string>(); // Track names *just* within this batch

    for (const file of Array.from(files)) {
        if (fileNamesInBatch.has(file.name)) {
            console.warn(`Skipping duplicate file in this selection: ${file.name}`);
            displaySkippedDuplicateRow(elements, file.name); // Show in UI it was skipped
        } else {
            filesToProcess.push(file);
            fileNamesInBatch.add(file.name);
            processedFileNames.add(file.name); // Add to the main set for this load session
        }
    }

    if (filesToProcess.length === 0) {
        elements.loadStatusSpan.textContent = `No new files to process (duplicates skipped).`;
        input.value = ''; // Clear the file input
        return;
    }

    elements.loadStatusSpan.textContent = `Loading ${filesToProcess.length} file(s)...`;

    // Process unique files
    const results: ProcessedTrxResult[] = await Promise.all(filesToProcess.map(file => processTrxFile(file)));

    let filesProcessedCount = 0;

    // Process results and display rows
    results.forEach(result => {
        if (!result) return;

        if (result.errorType) {
            displayErrorRow(elements, result.fileName, result.errorType);
            // Add minimal entry for error files so they aren't hidden by search
            allTrxData.push({ fileName: result.fileName, tests: [], runStartTime: null, runEndTime: null, testListMap: new Map() });

        } else if (result.summary && result.data) {
            displayTrxSummaryRow(elements, result.fileName, result.summary);
            allTrxData.push(result.data);
            filesProcessedCount++;
        } else if (result.data) {
            displayErrorRow(elements, result.fileName, "Summary Missing/Invalid");
            allTrxData.push(result.data);
        } else {
            displayErrorRow(elements, result.fileName, "Unknown Processing Issue");
            allTrxData.push({ fileName: result.fileName, tests: [], runStartTime: null, runEndTime: null, testListMap: new Map() });
        }
    });

    console.log(`${filesProcessedCount} of ${filesToProcess.length} files processed successfully.`);
    displayTopFails(elements);

    // Enable visualize button logic
    const hasVisData = allTrxData.some(d =>
        d.runStartTime && d.runEndTime && d.tests.some(t => t.startTime && t.endTime && t.startTime < t.endTime)
    );
    elements.visualizeButton.disabled = !hasVisData;
    elements.visualizeButton.classList.toggle('gradient-active', hasVisData);

    elements.loadStatusSpan.textContent = `${filesToProcess.length} file(s) processed (${files.length - filesToProcess.length} duplicates skipped).`;

    // Clear the file input value *after* processing
    input.value = '';
}


function handleSortTable(event: MouseEvent, elements: ReturnType<typeof getAnalysisElements>): void {
    const target = event.target as HTMLElement;
    const header = target.closest<HTMLTableCellElement>('th[data-sort-key]');
    if (header && elements.resultsTable) {
        sortTableByColumn(elements.resultsTable, header);
    }
}

function handleFailCountClick(event: MouseEvent, elements: ReturnType<typeof getAnalysisElements>): void {
    const target = event.target as HTMLElement;
    if (target.matches('.fail-count[data-action="show-fails"]')) {
        const message = target.dataset.message;
        const { failFilterTypeSelect, failSearchCriteriaInput, searchButton } = elements;

        if (message && failFilterTypeSelect && failSearchCriteriaInput && searchButton) {
            failFilterTypeSelect.value = 'ErrorMessage';
            failSearchCriteriaInput.value = message;
            searchButton.click();
            console.log(`Filtering results for failure message: "${message}"`);
        } else {
            console.warn("Could not find fail filter elements or message to perform action.");
        }
    }
}

function handleSearchFails(elements: ReturnType<typeof getAnalysisElements>): void {
    const filterType = elements.failFilterTypeSelect?.value ?? 'None';
    const criteria = elements.failSearchCriteriaInput?.value?.trim().toLowerCase() ?? '';
    const tableBody = elements.resultsTableBody;
    console.log(`Searching fails with type: ${filterType}, criteria: "${criteria}"`);

    if (!tableBody) return;

    const requiresFilter = filterType !== 'None' && criteria !== '';
    let regex: RegExp | null = null;

    if (requiresFilter && (filterType === 'ErrorMessageRegex' || filterType === 'TestNameRegex')) {
        try {
            regex = new RegExp(criteria, 'i'); // Case-insensitive regex
        } catch (e) {
            console.error("Invalid Regular Expression:", e);
            alert("Invalid Regular Expression provided.");
            return;
        }
    }

    let matchingFileNames = new Set<string>();
    if (requiresFilter) {
        allTrxData.forEach(trxFile => {
            // Ensure file data exists before checking tests
            if (!trxFile || !trxFile.tests) return;

            const fileMatches = trxFile.tests.some(test => {
                // Add null/undefined checks for robustness
                const testNameLower = test.testName?.toLowerCase() || '';
                const errorMessageLower = test.errorMessage?.toLowerCase() || '';

                switch (filterType) {
                    case 'TestName': return testNameLower.includes(criteria);
                    case 'TestNameRegex': return regex?.test(testNameLower) ?? false;
                    case 'ErrorMessage': return test.outcome === 'Failed' && errorMessageLower.includes(criteria);
                    case 'ErrorMessageRegex': return test.outcome === 'Failed' && (regex?.test(errorMessageLower) ?? false);
                    default: return false;
                }
            });
            if (fileMatches) {
                matchingFileNames.add(trxFile.fileName);
            }
        });
    }


    // Filter rows in the summary table
    let visibleRows = 0;
    tableBody.querySelectorAll('tr').forEach(row => {
        // Always show error and skipped rows regardless of filter
        if (row.classList.contains('error-row') || row.classList.contains('skipped-row')) {
            row.style.display = '';
            visibleRows++;
            return;
        }

        const fileNameCell = row.cells[0];
        if (!fileNameCell) return;

        const fileName = fileNameCell.textContent?.trim() ?? '';

        if (!requiresFilter || matchingFileNames.has(fileName)) {
            row.style.display = '';
            visibleRows++;
        } else {
            row.style.display = 'none';
        }
    });

    console.log(`Search complete. Showing ${visibleRows} row(s) (including errors/skipped).`);
}


// --- Initialization Function ---
export function initializeAnalysisTab(): void {
    if (analysisTabInitialized) {
        console.log("Analysis Tab already initialized. Skipping.");
        return;
    }
    console.log("Initializing Analysis Tab...");

    const elements = getAnalysisElements();

    if (!elements.loadTrxButton || !elements.trxFileLoader || !elements.resultsTable || !elements.resultsTableBody || !elements.topFailsListContainer || !elements.loadStatusSpan || !elements.visualizeButton || !elements.searchButton || !elements.failFilterTypeSelect || !elements.failSearchCriteriaInput) {
        console.error("Analysis tab essential elements not all found.");
        return;
    }

    elements.loadTrxButton.addEventListener('click', () => elements.trxFileLoader?.click());
    elements.trxFileLoader.addEventListener('change', (e) => handleTrxFileLoad(e, elements));
    elements.searchButton.addEventListener('click', () => handleSearchFails(elements));

    const tableHead = elements.resultsTable.querySelector('thead');
    if (tableHead) {
        tableHead.addEventListener('click', (e) => handleSortTable(e as MouseEvent, elements));
    } else {
        console.error("Could not find table header (thead) for sorting.");
    }

    elements.topFailsListContainer.addEventListener('click', (e) => handleFailCountClick(e as MouseEvent, elements));

    elements.visualizeButton.disabled = true;
    elements.visualizeButton.addEventListener('click', () => {
        if (allTrxData.length > 0) {
            openVisualizationModal(allTrxData)
        } else {
            console.warn("Visualize button clicked but no data available.");
        }
    });

    initializeVisualizationModalControls();

    analysisTabInitialized = true;
    console.log("Analysis Tab Initialized successfully.");
}