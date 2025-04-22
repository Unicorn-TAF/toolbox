// --- Analysis Tab Specific Storage ---
let aggregatedFailures = new Map(); // Key: Normalized error message, Value: count
let allTrxData = []; // Stores { fileName, runStartTime, runEndTime, tests: [{...testData, testListId}], testListMap: {id->name} }
let suiteColorMap = new Map();
let nextSuiteColorIndex = 0;

// --- Analysis Tab Constants ---
const VIS_PALETTE = ['#1CCEED', '#1C4AED', '#1C8CED', '#1CEDC7', '#311CED', '#419EF0', '#44676E', '#8A1CED', '#1CE5ED', '#1CADEF', '#87CEEB', '#3CB371', '#FFD700', '#FFA07A', '#9370DB'];
const VIS_FAIL_COLOR = '#F04141';
const MIN_TEST_WIDTH_PX = 2;

// --- Initialization flags ---
let analysisTabInitialized = false;
let efficiencyTabInitialized = false; // <-- New flag for Efficiency Tab

// --- DOMContentLoaded ---
document.addEventListener('DOMContentLoaded', () => {
    // --- Tab Navigation Logic ---
    const tabsContainer = document.querySelector('.tabs');
    const contentContainer = document.getElementById('content');
    if (tabsContainer && contentContainer) {
        const tabLinks = tabsContainer.querySelectorAll('a[data-target]');
        const contentSections = contentContainer.querySelectorAll('.tab-content');

        function updateActiveTab(path) {
            let targetId = 'stats';
            switch (path) {
                case '/coverage': targetId = 'coverage'; break;
                case '/analysis': targetId = 'analysis'; break;
                case '/efficiency': targetId = 'efficiency'; break; // <-- Route for efficiency
                case '/': default: targetId = 'stats'; break;
            }
            console.log(`Updating tabs for path: ${path}, Target ID: ${targetId}`);

            tabLinks.forEach(link => link.classList.toggle('active', link.getAttribute('data-target') === targetId));
            contentSections.forEach(section => section.style.display = section.id === targetId ? 'block' : 'none');

            // --- Initialize specific tabs only when they become active ---
            if (targetId === 'analysis' && !analysisTabInitialized) { // Only init if not already done
                initializeAnalysisTab();
            } else if (targetId === 'efficiency' && !efficiencyTabInitialized) { // Only init if not already done
                initializeEfficiencyTab(); // <-- Call initialization for efficiency tab
            }
        }

        // Initial tab setup based on URL
        updateActiveTab(window.location.pathname);

        // Handle clicks on tab links
        tabsContainer.addEventListener('click', (event) => {
            const link = event.target.closest('a[data-target]');
            if (link) {
                event.preventDefault();
                const targetPath = link.getAttribute('href');
                // Only push state if the path actually changes
                if (window.location.pathname !== targetPath) {
                    history.pushState({ path: targetPath }, '', targetPath);
                }
                updateActiveTab(targetPath);
            }
        });

        // Handle browser back/forward buttons
        window.addEventListener('popstate', (event) => {
            updateActiveTab(event.state ? event.state.path : window.location.pathname);
        });
    } else {
        console.error("Could not find tabs container or content container.");
    }

    // --- Trigger initializations if the relevant tab is already visible ---
    // (This handles the case where the page loads directly on a specific tab)
    if (document.getElementById('analysis')?.style.display !== 'none' && !analysisTabInitialized) {
        initializeAnalysisTab();
    }
    if (document.getElementById('efficiency')?.style.display !== 'none' && !efficiencyTabInitialized) {
        initializeEfficiencyTab();
    }
}); // End DOMContentLoaded


// ========================================================================
// --- Analysis Tab Logic (Self-contained) ---
// ========================================================================
function initializeAnalysisTab() {
    if (analysisTabInitialized) return;
    console.log("Initializing Analysis Tab...");

    // DOM Elements specific to Analysis Tab
    const loadTrxButton = document.getElementById('loadTrxButton');
    const trxFileLoader = document.getElementById('trxFileLoader');
    const resultsTable = document.getElementById('resultsTable');
    const resultsTableBody = document.getElementById('resultsTableBody');
    const topFailsListContainer = document.getElementById('topFailsList');
    const loadStatusSpan = document.getElementById('loadStatus');
    const visualizeButton = document.getElementById('visualizeButton');
    const visualizationModal = document.getElementById('visualizationModal');
    const modalCloseButton = visualizationModal?.querySelector('.modal-close-button');
    const searchButton = document.getElementById('searchFailsButton');

    if (!loadTrxButton || !trxFileLoader || !resultsTable || !resultsTableBody || !topFailsListContainer || !loadStatusSpan || !visualizeButton || !visualizationModal || !modalCloseButton || !searchButton) {
        console.error("Analysis tab elements (including visualization & search) not all found.");
        // Don't mark as initialized if elements are missing, but allow other tabs to init
        return;
    }

    // Event Listeners for Analysis Tab
    loadTrxButton.addEventListener('click', () => trxFileLoader.click());
    trxFileLoader.addEventListener('change', handleTrxFileLoad); // Use named function

    const resultsTableHead = resultsTable.querySelector('thead');
    if (resultsTableHead) {
        resultsTableHead.addEventListener('click', handleSortTable); // Use named function
    } else {
        console.error("Could not find table header (thead) for sorting.");
    }

    visualizeButton.disabled = true; // Disable initially
    visualizeButton.addEventListener('click', openVisualizationModal);
    modalCloseButton.addEventListener('click', closeVisualizationModal);
    visualizationModal.addEventListener('click', (event) => {
        // Close modal if clicking outside the content area
        if (event.target === visualizationModal) {
            closeVisualizationModal();
        }
    });

    // Add listener for dynamic fail count clicks
    topFailsListContainer.addEventListener('click', handleFailCountClick);

    // Add listener for search
    searchButton.addEventListener('click', handleSearchFails);

    analysisTabInitialized = true;
    console.log("Analysis Tab Initialized.");
}

function handleTrxFileLoad(event) {
    const files = event.target.files;
    const resultsTableBody = document.getElementById('resultsTableBody');
    const topFailsListContainer = document.getElementById('topFailsList');
    const loadStatusSpan = document.getElementById('loadStatus');
    const visualizeButton = document.getElementById('visualizeButton');
    const resultsTable = document.getElementById('resultsTable');

    if (!files || files.length === 0 || !resultsTableBody || !topFailsListContainer || !loadStatusSpan || !visualizeButton || !resultsTable) {
        console.warn("Elements missing for file processing.");
        return;
    }

    // Reset UI elements
    resultsTableBody.innerHTML = '';
    topFailsListContainer.innerHTML = '';
    aggregatedFailures.clear();
    allTrxData = []; // Clear visualization data
    suiteColorMap.clear();
    nextSuiteColorIndex = 0;
    loadStatusSpan.textContent = '';
    visualizeButton.disabled = true;
    visualizeButton.classList.remove('gradient-active');
    resultsTable.querySelectorAll('thead th').forEach(th => {
        th.classList.remove('sorted-asc', 'sorted-desc');
        delete th.dataset.sortDirection;
    });
    // Reset search fields
    const failFilterTypeSelect = document.getElementById('failFilterType');
    const failSearchCriteriaInput = document.getElementById('failSearchCriteria');
    if (failFilterTypeSelect) failFilterTypeSelect.value = 'None';
    if (failSearchCriteriaInput) failSearchCriteriaInput.value = '';

    loadStatusSpan.textContent = `Loading ${files.length} file(s)...`;

    const fileReadPromises = Array.from(files).map(file => processTrxFile(file));

    Promise.all(fileReadPromises)
        .then(() => {
            console.log("All files processed.");
            displayTopFails();
            // Enable visualize button only if there's valid data with time ranges
            const hasVisData = allTrxData.some(d => d.runStartTime && d.runEndTime && d.tests.some(t => t.startTime && t.endTime));
            visualizeButton.disabled = !hasVisData;
            if (!visualizeButton.disabled) {
                visualizeButton.classList.add('gradient-active');
            } else {
                visualizeButton.classList.remove('gradient-active');
            }
            loadStatusSpan.textContent = `${files.length} file(s) processed.`;
        })
        .catch(error => {
            console.error("Error processing files:", error);
            loadStatusSpan.textContent = `Error loading. Check console.`;
            visualizeButton.disabled = true; // Keep disabled on error
            visualizeButton.classList.remove('gradient-active');
        })
        .finally(() => {
            // Clear the file input value so the 'change' event fires even if the same file is selected again
            event.target.value = '';
        });
}

function handleSortTable(event) {
    const header = event.target.closest('th[data-sort-key]');
    const resultsTable = document.getElementById('resultsTable');
    if (header && resultsTable) {
        sortTableByColumn(resultsTable, header);
    }
}

function handleFailCountClick(event) {
    if (event.target.matches('.fail-count[data-action="show-fails"]')) {
        const message = event.target.dataset.message;
        const failFilterTypeSelect = document.getElementById('failFilterType');
        const failSearchCriteriaInput = document.getElementById('failSearchCriteria');
        const searchFailsButton = document.getElementById('searchFailsButton');

        if (failFilterTypeSelect && failSearchCriteriaInput && searchFailsButton) {
            failFilterTypeSelect.value = 'ErrorMessage'; // Set filter type
            failSearchCriteriaInput.value = message;     // Set search text
            searchFailsButton.click();                   // Trigger search
            console.log(`Filtering results for failure message: "${message}"`);
        } else {
            console.warn("Could not find fail filter elements to perform action.");
        }
    }
}

function handleSearchFails() {
    const filterType = document.getElementById('failFilterType')?.value ?? 'None';
    const criteria = document.getElementById('failSearchCriteria')?.value?.trim() ?? '';
    const resultsTableBody = document.getElementById('resultsTableBody');
    console.log(`Searching fails with type: ${filterType}, criteria: "${criteria}"`);

    if (!resultsTableBody || !allTrxData) return;

    // Determine which files match the criteria
    const matchingFileNames = new Set();
    let requiresFullScan = false;

    if (filterType !== 'None' && criteria !== '') {
        requiresFullScan = true; // Need to iterate through allTrxData
        let regex = null;
        if (filterType === 'ErrorMessageRegex' || filterType === 'TestNameRegex') { // Add regex for test name if needed
            try {
                regex = new RegExp(criteria, 'i'); // Case-insensitive regex
            } catch (e) {
                console.error("Invalid Regular Expression:", e);
                alert("Invalid Regular Expression provided.");
                return; // Stop search if regex is invalid
            }
        }

        allTrxData.forEach(trxFile => {
            let fileMatches = false;
            if (filterType === 'TestName') {
                fileMatches = trxFile.tests.some(test => test.testName?.toLowerCase().includes(criteria.toLowerCase()));
            } else if (filterType === 'TestNameRegex' && regex) {
                fileMatches = trxFile.tests.some(test => regex.test(test.testName || ''));
            } else if (filterType === 'ErrorMessage') {
                fileMatches = trxFile.tests.some(test =>
                    test.outcome === 'Failed' && test.errorMessage?.toLowerCase().includes(criteria.toLowerCase())
                );
            } else if (filterType === 'ErrorMessageRegex' && regex) {
                fileMatches = trxFile.tests.some(test =>
                    test.outcome === 'Failed' && regex.test(test.errorMessage || '')
                );
            }
            // Add other filter types here if needed

            if (fileMatches) {
                matchingFileNames.add(trxFile.fileName);
            }
        });
    }

    // Filter rows in the summary table
    Array.from(resultsTableBody.rows).forEach(row => {
        // Skip error rows from filtering logic based on content
        if (row.classList.contains('error-row')) {
            row.style.display = ''; // Always show error rows
            return;
        }

        const fileNameCell = row.cells[0];
        if (!fileNameCell) return; // Skip rows without a filename cell

        const fileName = fileNameCell.textContent.trim();

        if (!requiresFullScan) {
            // No filter applied or criteria empty, show all non-error rows
            row.style.display = '';
        } else {
            // Filter based on the matching filenames found
            if (matchingFileNames.has(fileName)) {
                row.style.display = ''; // Show matching rows
            } else {
                row.style.display = 'none'; // Hide non-matching rows
            }
        }
    });

    console.log(`Search complete. Showing ${requiresFullScan ? matchingFileNames.size : 'all'} file(s).`);
}


// --- Analysis Tab Helper Functions ---
function processTrxFile(file) {
    return new Promise((resolve) => {
        const reader = new FileReader();
        let gridRowDisplayed = false; // Track if a row (summary or error) has been added

        reader.onload = (e) => {
            let trxFileDataForVis = {
                fileName: file.name,
                runStartTime: null,
                runEndTime: null,
                tests: [],
                testListMap: new Map()
            };
            let visualizationDataPossible = true;
            let fileHasValidTestsForVis = false;

            try {
                const xmlContent = e.target.result;
                const parser = new DOMParser();
                const xmlDoc = parser.parseFromString(xmlContent, "text/xml");
                const parseError = xmlDoc.querySelector("parsererror");

                if (parseError) {
                    console.error(`Error parsing TRX file "${file.name}":`, parseError.textContent);
                    displayErrorRow(file.name, "XML Parse Error");
                    gridRowDisplayed = true;
                    resolve(); return;
                }

                // Extract summary first for the table
                const summary = extractTrxSummary(xmlDoc);
                if (summary) {
                    displayTrxSummary(file.name, summary);
                    gridRowDisplayed = true;
                } else {
                    console.warn(`Could not extract summary from "${file.name}". Displaying error row.`);
                    displayErrorRow(file.name, "Summary Missing/Invalid");
                    gridRowDisplayed = true;
                    visualizationDataPossible = false; // Can't visualize if basic summary is missing
                }

                // --- Extract data needed for Visualization and Search ---
                const timesNode = xmlDoc.getElementsByTagName("Times")[0];
                if (timesNode) {
                    trxFileDataForVis.runStartTime = parseDateTimeSafe(timesNode.getAttribute("start"));
                    trxFileDataForVis.runEndTime = parseDateTimeSafe(timesNode.getAttribute("finish"));
                    if (!trxFileDataForVis.runStartTime || !trxFileDataForVis.runEndTime) {
                        console.warn(`File "${file.name}" has invalid or missing run start/finish times.`);
                        visualizationDataPossible = false;
                    }
                } else {
                    console.warn(`File "${file.name}" missing Times node.`);
                    visualizationDataPossible = false;
                }

                const testListsContainer = xmlDoc.getElementsByTagName("TestLists")[0];
                if (testListsContainer) {
                    const testListNodes = testListsContainer.getElementsByTagName("TestList");
                    for (let i = 0; i < testListNodes.length; i++) {
                        const id = testListNodes[i].getAttribute("id");
                        const name = testListNodes[i].getAttribute("name");
                        if (id && name) trxFileDataForVis.testListMap.set(id, name);
                    }
                } else {
                    console.warn(`File "${file.name}" missing TestLists node for visualization suite mapping.`);
                    // Don't necessarily fail visualization just for this
                }

                const unitTestResults = xmlDoc.getElementsByTagName("UnitTestResult");
                for (let i = 0; i < unitTestResults.length; i++) {
                    const result = unitTestResults[i];
                    const outcome = result.getAttribute("outcome");
                    const testName = result.getAttribute("testName");
                    const testListId = result.getAttribute("testListId");
                    const startTimeStr = result.getAttribute("startTime");
                    const endTimeStr = result.getAttribute("endTime");
                    const durationStr = result.getAttribute("duration");

                    let errorMessage = null;
                    let stackTrace = null;

                    // Extract failure info for both Top Fails and Search
                    if (outcome === "Failed") {
                        const errorInfo = result.getElementsByTagName("ErrorInfo")[0];
                        const messageElement = errorInfo?.getElementsByTagName("Message")[0];
                        const stackTraceElement = errorInfo?.getElementsByTagName("StackTrace")[0];
                        errorMessage = messageElement?.textContent?.trim() || null;
                        stackTrace = stackTraceElement?.textContent?.trim() || null;
                        if (errorMessage) {
                            // Use a consistent representation for aggregation
                            let normalizedMessage = errorMessage;
                            // Optional: Normalize further (e.g., remove GUIDs, timestamps if needed)
                            aggregatedFailures.set(normalizedMessage, (aggregatedFailures.get(normalizedMessage) || 0) + 1);
                        }
                    }

                    // Collect data for visualization if possible
                    if (visualizationDataPossible) {
                        const testData = {
                            testName: testName || `Unnamed Test ${i + 1}`,
                            startTime: parseDateTimeSafe(startTimeStr),
                            endTime: parseDateTimeSafe(endTimeStr),
                            durationMs: durationStr ? parseTimespan(durationStr) : 0,
                            outcome: outcome || "Unknown",
                            testListId: testListId,
                            errorMessage: errorMessage, // Store for search/tooltip
                            stackTrace: stackTrace    // Store for search/tooltip
                        };

                        // Attempt to derive missing start/end times from duration
                        if (!testData.startTime && testData.endTime && testData.durationMs > 0) {
                            testData.startTime = new Date(testData.endTime.getTime() - testData.durationMs);
                        } else if (testData.startTime && !testData.endTime && testData.durationMs > 0) {
                            testData.endTime = new Date(testData.startTime.getTime() + testData.durationMs);
                        }

                        // Add to vis data only if times are valid
                        if (testData.startTime && testData.endTime && testData.startTime < testData.endTime) {
                            trxFileDataForVis.tests.push(testData);
                            fileHasValidTestsForVis = true;
                        } else {
                            // console.warn(`Skipping test "${testData.testName}" in file "${file.name}" for visualization due to invalid times/duration.`);
                        }
                    } else {
                        // Still collect basic test info for search even if visualization isn't possible
                        const minimalTestData = {
                            testName: testName || `Unnamed Test ${i + 1}`,
                            outcome: outcome || "Unknown",
                            errorMessage: errorMessage,
                            stackTrace: stackTrace,
                            // No times needed if only for search
                        };
                        trxFileDataForVis.tests.push(minimalTestData);
                    }
                }

                // Add the collected file data (for vis and/or search)
                // Only add if it has tests, even if times were invalid for vis
                if (trxFileDataForVis.tests.length > 0) {
                    allTrxData.push(trxFileDataForVis);
                    if (!visualizationDataPossible) {
                        console.warn(`File "${file.name}" added for search/summary, but skipped for visualization due to missing/invalid run times.`);
                    } else if (!fileHasValidTestsForVis) {
                        console.warn(`File "${file.name}" added for search/summary; had run times but no valid *test* timings for visualization.`);
                    }
                } else {
                    console.warn(`File "${file.name}" had no UnitTestResult entries.`)
                }

                resolve();

            } catch (error) {
                console.error(`Critical error processing file "${file.name}":`, error);
                if (!gridRowDisplayed) { // Only display if no other row was added
                    displayErrorRow(file.name, "Processing Error");
                }
                // Add minimal entry to allTrxData so the file row isn't hidden by search
                allTrxData.push({ fileName: file.name, tests: [], testListMap: new Map(), runStartTime: null, runEndTime: null });
                resolve(); // Resolve anyway
            }
        };
        reader.onerror = (e) => {
            console.error(`Error reading file "${file.name}":`, e);
            displayErrorRow(file.name, "File Read Error");
            // Add minimal entry to allTrxData so the file row isn't hidden by search
            allTrxData.push({ fileName: file.name, tests: [], testListMap: new Map(), runStartTime: null, runEndTime: null });
            resolve();
        };
        reader.readAsText(file);
    });
}

function extractTrxSummary(xmlDoc) {
    try {
        const timesNode = xmlDoc.getElementsByTagName("Times")[0];
        const summaryNode = xmlDoc.getElementsByTagName("ResultSummary")[0];
        const countersNode = summaryNode ? summaryNode.getElementsByTagName("Counters")[0] : null;
        const resultsNode = xmlDoc.getElementsByTagName("Results")[0]; // Used for raw duration

        const testListsContainer = xmlDoc.getElementsByTagName("TestLists")[0];
        const testLists = testListsContainer ? testListsContainer.getElementsByTagName("TestList") : [];

        if (!timesNode || !summaryNode || !countersNode) {
            console.warn("TRX file missing essential elements for summary grid (Times, ResultSummary, Counters). Will return null.");
            return null;
        }

        const startTimeStr = timesNode.getAttribute("start");
        const finishTimeStr = timesNode.getAttribute("finish");
        let totalDurationMin = 0;
        if (startTimeStr && finishTimeStr) {
            const startMs = Date.parse(startTimeStr);
            const finishMs = Date.parse(finishTimeStr);
            if (!isNaN(startMs) && !isNaN(finishMs) && finishMs >= startMs) {
                totalDurationMin = (finishMs - startMs) / (1000 * 60);
            } else { console.warn("Invalid start/finish times found:", startTimeStr, finishTimeStr); }
        } else { console.warn("Missing start or finish time attributes."); }

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

        // Use the counters directly if available
        const totalTests = Number.parseInt(countersNode.getAttribute("total") || "0", 10) || 0;
        const executedTests = Number.parseInt(countersNode.getAttribute("executed") || "0", 10) || 0;
        const passedTests = Number.parseInt(countersNode.getAttribute("passed") || "0", 10) || 0;
        const failedTests = Number.parseInt(countersNode.getAttribute("failed") || "0", 10) || 0;
        // Consolidate various 'skipped' categories
        const errorTests = Number.parseInt(countersNode.getAttribute("error") || "0", 10) || 0;
        const timeoutTests = Number.parseInt(countersNode.getAttribute("timeout") || "0", 10) || 0;
        const abortedTests = Number.parseInt(countersNode.getAttribute("aborted") || "0", 10) || 0;
        const inconclusiveTests = Number.parseInt(countersNode.getAttribute("inconclusive") || "0", 10) || 0;
        const passedButRunAbortedTests = Number.parseInt(countersNode.getAttribute("passedButRunAborted") || "0", 10) || 0;
        const notRunnableTests = Number.parseInt(countersNode.getAttribute("notRunnable") || "0", 10) || 0;
        const notExecutedTests = Number.parseInt(countersNode.getAttribute("notExecuted") || "0", 10) || 0;
        const disconnectedTests = Number.parseInt(countersNode.getAttribute("disconnected") || "0", 10) || 0;
        const warningTests = Number.parseInt(countersNode.getAttribute("warning") || "0", 10) || 0;
        const completedTests = Number.parseInt(countersNode.getAttribute("completed") || "0", 10) || 0;
        const inProgressTests = Number.parseInt(countersNode.getAttribute("inProgress") || "0", 10) || 0;
        const pendingTests = Number.parseInt(countersNode.getAttribute("pending") || "0", 10) || 0;

        // Define 'Skipped' more broadly: Not Passed, Not Failed, Not Error/Timeout/Aborted
        // This aims to capture tests explicitly marked as skipped or inconclusive.
        // Note: 'notExecuted' might overlap with 'pending' or 'notRunnable'. The exact definition can be debated.
        // Let's use a common definition: Not executed or inconclusive.
        const skippedTests = (notExecutedTests + inconclusiveTests + pendingTests + notRunnableTests);
        // Let's count Fails more broadly too, including errors/timeouts/aborts
        const robustFailedTests = failedTests + errorTests + timeoutTests + abortedTests;

        const suitesCount = testLists.length;

        if (!testListsContainer && suitesCount === 0) { // Only warn if count is zero AND container missing
            console.warn("Missing <TestLists> node, cannot accurately calculate Suite Count (assuming 0).");
        }

        // Basic sanity check on counters (optional)
        // if (executedTests !== (passedTests + failedTests + skippedTests + errorTests + timeoutTests + abortedTests + inconclusiveTests)) {
        //     console.warn(`TRX counter mismatch in file: Executed (${executedTests}) vs Sum of detailed outcomes.`);
        // }

        return {
            totalDurationMin: totalDurationMin,
            rawDurationMin: rawDurationMin,
            suitesCount: suitesCount,
            testsCount: totalTests,
            failedTests: robustFailedTests, // Use the broader definition
            skippedTests: skippedTests // Use the broader definition
        };
    } catch (error) {
        console.error("Error extracting summary data from TRX structure:", error);
        return null;
    }
}


function parseTimespan(timeSpan) { if (!timeSpan || typeof timeSpan !== 'string') return 0; try { const parts = timeSpan.split(':'); if (parts.length !== 3) throw new Error("Invalid format"); const secondsParts = parts[2].split('.'); const hours = parseInt(parts[0], 10); const minutes = parseInt(parts[1], 10); const seconds = parseInt(secondsParts[0], 10); const fractionString = (secondsParts[1] || "0").padEnd(7, '0'); const milliseconds = parseFloat("0." + fractionString.substring(0, 7)) * 1000; if (isNaN(hours) || isNaN(minutes) || isNaN(seconds) || isNaN(milliseconds)) throw new Error("Contains non-numeric parts"); return (hours * 3600 + minutes * 60 + seconds) * 1000 + Math.round(milliseconds); } catch (e) { console.warn(`Could not parse timespan: ${timeSpan}`, e); return 0; } }
function parseDateTimeSafe(dateTimeString) { if (!dateTimeString) return null; try { const date = new Date(dateTimeString); return isNaN(date.getTime()) ? null : date; } catch (e) { console.warn(`Could not parse date-time string: ${dateTimeString}`, e); return null; } }

function displayTrxSummary(fileName, summary) {
    const resultsTableBody = document.getElementById('resultsTableBody');
    if (!resultsTableBody) { console.error("Cannot find resultsTableBody."); return; }
    const row = resultsTableBody.insertRow();
    row.insertCell().textContent = fileName;
    row.insertCell().textContent = summary.totalDurationMin.toFixed(1);
    row.insertCell().textContent = summary.rawDurationMin.toFixed(1);
    row.insertCell().textContent = summary.suitesCount;
    row.insertCell().textContent = summary.testsCount;
    row.insertCell().textContent = summary.failedTests;
    row.insertCell().textContent = summary.skippedTests;
    // Add tooltips for more precision or detail
    row.cells[0].title = fileName;
    row.cells[1].title = `Total Duration: ${summary.totalDurationMin.toFixed(3)} min`;
    row.cells[2].title = `Sum of Test Durations: ${summary.rawDurationMin.toFixed(3)} min`;
    row.cells[3].title = `${summary.suitesCount} Suites`;
    row.cells[4].title = `${summary.testsCount} Total Tests Defined`;
    row.cells[5].title = `${summary.failedTests} Failed (includes errors/timeouts)`;
    row.cells[6].title = `${summary.skippedTests} Skipped/Inconclusive/NotRun`;
}

function displayErrorRow(fileName, errorType) {
    const resultsTableBody = document.getElementById('resultsTableBody');
    if (!resultsTableBody) { console.error("Cannot find resultsTableBody."); return; }
    const row = resultsTableBody.insertRow();
    row.classList.add('error-row');
    const cellFileName = row.insertCell();
    cellFileName.textContent = fileName;
    const cellError = row.insertCell();
    cellError.textContent = `Error: ${errorType}`;
    cellError.colSpan = 6; // Span across remaining columns
    cellError.title = `File: ${fileName}\nError: ${errorType}`;
}

function displayTopFails() {
    const topFailsListContainer = document.getElementById('topFailsList');
    if (!topFailsListContainer) { console.error("Cannot find topFailsList container."); return; }

    const sortedFails = Array.from(aggregatedFailures.entries())
        .sort(([, countA], [, countB]) => countB - countA); // Sort descending by count

    const top5Fails = sortedFails.slice(0, 5);
    topFailsListContainer.innerHTML = ''; // Clear previous entries

    if (top5Fails.length === 0) {
        topFailsListContainer.innerHTML = '<p style="padding: 10px; color: #555; text-align: center;">No failures found in loaded files.</p>';
        return;
    }

    top5Fails.forEach(([message, count]) => {
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
        countSpan.textContent = count;
        countSpan.dataset.action = 'show-fails'; // Action for click handler
        countSpan.dataset.message = message;      // Store message for filtering

        failItemDiv.appendChild(messageBoxDiv);
        failItemDiv.appendChild(countSpan);
        topFailsListContainer.appendChild(failItemDiv);
    });
}

function sortTableByColumn(table, headerCell) {
    const tableBody = table.querySelector('tbody');
    if (!tableBody) { console.error("Table body not found for sorting."); return; }

    const sortKey = headerCell.dataset.sortKey;
    const sortType = headerCell.dataset.sortType || 'text';
    const currentDirection = headerCell.dataset.sortDirection || 'desc'; // Default desc? Or maybe none? Let's start with 'desc' toggle
    const newDirection = currentDirection === 'asc' ? 'desc' : 'asc';

    const headerCells = Array.from(headerCell.parentElement.children);
    const columnIndex = headerCells.indexOf(headerCell);
    if (columnIndex === -1) { console.error("Could not determine column index for sorting."); return; }

    // Get only non-error rows for sorting
    const rowsToSort = Array.from(tableBody.querySelectorAll('tr:not(.error-row)'));
    // Keep track of error rows and their original positions relative to each other
    const errorRows = Array.from(tableBody.querySelectorAll('tr.error-row'));

    rowsToSort.sort((rowA, rowB) => {
        const cellA = rowA.children[columnIndex];
        const cellB = rowB.children[columnIndex];
        // Handle potential missing cells gracefully
        const valueA = cellA ? cellA.textContent.trim() : '';
        const valueB = cellB ? cellB.textContent.trim() : '';

        let compareResult = 0;
        switch (sortType) {
            case 'number':
                const numA = parseFloat(valueA) || 0; // Default to 0 if parse fails
                const numB = parseFloat(valueB) || 0;
                compareResult = numA - numB;
                break;
            case 'text':
            default:
                // Use localeCompare for better string sorting (handles numbers within strings)
                compareResult = valueA.localeCompare(valueB, undefined, { sensitivity: 'base', numeric: true });
                break;
        }
        // Apply direction
        return newDirection === 'asc' ? compareResult : -compareResult;
    });

    // Update header styles
    headerCells.forEach(th => {
        if (th.dataset.sortKey) { // Only apply to sortable headers
            th.classList.remove('sorted-asc', 'sorted-desc');
            delete th.dataset.sortDirection;
        }
    });
    headerCell.classList.add(newDirection === 'asc' ? 'sorted-asc' : 'sorted-desc');
    headerCell.dataset.sortDirection = newDirection;

    // Re-append sorted rows first, then error rows
    // Note: This might change the relative order of error rows if that matters.
    // A more complex approach would be needed to preserve original relative error row positions.
    tableBody.innerHTML = ''; // Clear body
    rowsToSort.forEach(row => tableBody.appendChild(row));
    errorRows.forEach(row => tableBody.appendChild(row));
}


// --- Visualization Functions (Require Analysis Tab context) ---
function openVisualizationModal() {
    const modal = document.getElementById('visualizationModal');
    const container = document.getElementById('timelineContainer');
    const axisHeader = document.getElementById('timelineAxisHeader');
    const clickInfo = document.getElementById('timelineClickInfo');
    const clickMarker = document.getElementById('timelineClickMarkerLine');

    const validDataForVis = allTrxData.filter(d => d.runStartTime && d.runEndTime && d.tests.some(t => t.startTime && t.endTime));

    if (!modal || validDataForVis.length === 0) {
        console.warn("Cannot open visualization: Modal element not found or no data with valid time ranges loaded.");
        alert("No data with valid time ranges loaded for visualization.");
        return;
    }
    // Reset previous state
    if (container) container.innerHTML = '';
    if (axisHeader) axisHeader.innerHTML = '';
    if (clickInfo) clickInfo.innerHTML = 'Click on timeline to see tests';
    if (clickMarker) clickMarker.style.display = 'none'; // Hide marker initially


    modal.style.display = 'block';
    // Use requestAnimationFrame to ensure the modal is visible before generating complex content
    requestAnimationFrame(() => generateVisualization(validDataForVis)); // Pass only valid data
}

function closeVisualizationModal() {
    const modal = document.getElementById('visualizationModal');
    if (!modal) return;
    modal.style.display = 'none';
    // Clean up content and listeners to prevent memory leaks or interference
    const container = document.getElementById('timelineContainer');
    const axisHeader = document.getElementById('timelineAxisHeader');
    const clickInfo = document.getElementById('timelineClickInfo');
    const clickMarker = document.getElementById('timelineClickMarkerLine');
    if (container) container.innerHTML = '';
    if (axisHeader) axisHeader.innerHTML = '';
    if (clickInfo) clickInfo.innerHTML = 'Click on timeline to see tests';
    if (clickMarker) clickMarker.style.display = 'none';
    removeTimelineListeners(); // Crucial for cleanup
}

// --- Visualization Generation ---
function generateVisualization(visData) { // Accept filtered data
    const container = document.getElementById('timelineContainer');
    const axisHeader = document.getElementById('timelineAxisHeader');
    const clickInfo = document.getElementById('timelineClickInfo');
    const containerWrapper = document.getElementById('timelineContainerWrapper');

    if (!container || !axisHeader || !clickInfo || !containerWrapper) {
        console.error("Timeline elements not found for generation."); return;
    }
    if (visData.length === 0) { // Check the passed data
        container.innerHTML = '<p style="text-align: center; padding: 20px; color: #555;">No valid visualization data loaded.</p>'; return;
    }

    // Clear previous content
    container.innerHTML = '';
    axisHeader.innerHTML = '';
    clickInfo.innerHTML = 'Click on timeline to see tests';
    suiteColorMap.clear(); // Reset suite colors for each generation
    nextSuiteColorIndex = 0;

    // Calculate overall time range from the VALID data
    let overallMinTime = Infinity;
    let overallMaxTime = -Infinity;

    visData.forEach(trx => {
        // Use run times if available and valid
        const runStart = trx.runStartTime?.getTime();
        const runEnd = trx.runEndTime?.getTime();
        if (runStart && runStart < overallMinTime) overallMinTime = runStart;
        if (runEnd && runEnd > overallMaxTime) overallMaxTime = runEnd;

        // Include test times as well to ensure full range
        trx.tests.forEach(test => {
            const testStart = test.startTime?.getTime();
            const testEnd = test.endTime?.getTime();
            if (testStart && testStart < overallMinTime) overallMinTime = testStart;
            if (testEnd && testEnd > overallMaxTime) overallMaxTime = testEnd;
        });
    });

    if (overallMinTime === Infinity || overallMaxTime === -Infinity || overallMinTime >= overallMaxTime) {
        console.error("Could not determine valid time range for visualization.", { min: overallMinTime, max: overallMaxTime });
        container.innerHTML = '<p style="text-align: center; padding: 20px; color: #900;">Error calculating time range from loaded data.</p>';
        return;
    }

    const overallDuration = overallMaxTime - overallMinTime;
    if (overallDuration <= 0) {
        console.error("Invalid duration calculated for visualization.");
        container.innerHTML = '<p style="text-align: center; padding: 20px; color: #900;">Error: Invalid time duration calculated.</p>';
        return;
    }

    // Use clientWidth which accounts for padding/border of the scrollable wrapper
    // Recalculate width here as it might change if window resized while modal closed
    const containerWidth = containerWrapper.clientWidth;
    if (containerWidth <= 0) {
        console.error("Container width is invalid for visualization.");
        container.innerHTML = '<p style="text-align: center; padding: 20px; color: #900;">Error: Cannot get container width for rendering.</p>';
        // Try again shortly?
        // setTimeout(() => generateVisualization(visData), 100);
        return;
    }

    // --- Draw Axis ---
    drawTimelineAxis(axisHeader, overallMinTime, overallMaxTime, containerWidth);

    // --- Draw Rows and Tests ---
    const rowHeight = 25; // pixels
    const rowMargin = 2;  // pixels
    const underlineHeight = 2; // pixels
    let totalHeight = 0;

    visData.forEach((trx, fileIndex) => { // Iterate over the valid data
        const rowDiv = document.createElement('div');
        rowDiv.className = 'timeline-row';
        rowDiv.style.position = 'absolute'; // Use absolute positioning for performance
        rowDiv.style.top = `${fileIndex * (rowHeight + rowMargin)}px`;
        rowDiv.style.left = '0';
        rowDiv.style.right = '0'; // Stretch full width
        rowDiv.style.height = `${rowHeight}px`;
        rowDiv.dataset.fileName = trx.fileName; // Store filename for potential use

        let currentSuiteId = null;
        let currentSuiteStartXPercent = -1;
        let currentSuiteColorClass = '';
        let currentSuiteEndXPercent = -1; // Track the end of the current suite's underline

        // Filter only tests with valid times for this specific file for visualization
        const testsToVisualize = trx.tests.filter(t => t.startTime && t.endTime && t.startTime < t.endTime);
        // Sort tests within the file by start time for correct rendering order
        const sortedTests = testsToVisualize.sort((a, b) => (a.startTime.getTime()) - (b.startTime.getTime()));

        sortedTests.forEach((test) => {
            const testStartTime = test.startTime.getTime();
            const testEndTime = test.endTime.getTime();
            const relativeStartMs = testStartTime - overallMinTime;
            const durationMs = testEndTime - testStartTime;

            const leftPercent = (relativeStartMs / overallDuration) * 100;
            let widthPercent = (durationMs / overallDuration) * 100;

            // Basic validation for position/width
            if (leftPercent < 0 || leftPercent > 100 || isNaN(leftPercent) || isNaN(widthPercent) || durationMs <= 0) return;

            // Ensure minimum visible width
            const minWidthPercent = (MIN_TEST_WIDTH_PX / containerWidth) * 100;
            widthPercent = Math.max(widthPercent, minWidthPercent);

            // Prevent overflow beyond 100%
            if (leftPercent + widthPercent > 100.1) { // Allow slight overflow due to rounding
                widthPercent = Math.max(0, 100 - leftPercent);
            }
            if (widthPercent <= 0) return; // Skip if width becomes zero or negative

            const testEndXPercent = leftPercent + widthPercent;

            // --- Suite Underline Logic ---
            const suiteId = test.testListId || `unknown_suite_${fileIndex}`; // Handle tests without a list ID
            const suiteName = trx.testListMap.get(test.testListId) || 'Unknown Suite';
            const colorClass = getSuiteColorClass(suiteId); // Get color class based on ID

            if (suiteId !== currentSuiteId) {
                // Finish previous suite's underline if one was active and had width
                if (currentSuiteId !== null && currentSuiteStartXPercent >= 0 && currentSuiteEndXPercent > currentSuiteStartXPercent) {
                    drawSuiteUnderline(rowDiv, currentSuiteStartXPercent, currentSuiteEndXPercent, currentSuiteColorClass, rowHeight, underlineHeight, suiteId);
                }
                // Start new suite tracking
                currentSuiteId = suiteId;
                currentSuiteStartXPercent = leftPercent;
                currentSuiteEndXPercent = testEndXPercent; // Initial end for the new suite
                currentSuiteColorClass = colorClass;
            } else {
                // Extend the end position for the current suite if this test ends later
                currentSuiteEndXPercent = Math.max(currentSuiteEndXPercent, testEndXPercent);
            }

            // --- Draw Test Square ---
            const square = document.createElement('div');
            square.className = 'test-square';
            square.style.left = `${leftPercent}%`;
            square.style.width = `${widthPercent}%`;

            // Store data attributes for tooltip/info
            square.dataset.testName = test.testName;
            square.dataset.startTime = test.startTime.toISOString();
            square.dataset.endTime = test.endTime.toISOString();
            square.dataset.outcome = test.outcome;
            square.dataset.fileName = trx.fileName;
            square.dataset.duration = durationMs;
            square.dataset.suiteName = suiteName;
            // Optionally add error message for tooltip if failed
            if (test.outcome === 'Failed' && test.errorMessage) {
                square.dataset.error = test.errorMessage.substring(0, 200) + (test.errorMessage.length > 200 ? '...' : ''); // Truncate long errors
            }

            if (test.outcome === 'Failed') {
                square.classList.add('color-fail');
            } else {
                square.classList.add(colorClass); // Use the potentially generated suiteId color
            }

            rowDiv.appendChild(square);
        }); // End loop through tests

        // Draw underline for the very last suite in the row if it exists
        if (currentSuiteId !== null && currentSuiteStartXPercent >= 0 && currentSuiteEndXPercent > currentSuiteStartXPercent) {
            drawSuiteUnderline(rowDiv, currentSuiteStartXPercent, currentSuiteEndXPercent, currentSuiteColorClass, rowHeight, underlineHeight, currentSuiteId);
        }


        container.appendChild(rowDiv);
        totalHeight = (fileIndex + 1) * (rowHeight + rowMargin);
    }); // End loop through files

    // Set the minimum height of the container to hold all rows
    container.style.minHeight = `${totalHeight}px`;

    // Add interactive listeners AFTER drawing is complete
    // Pass the data used for this visualization instance
    addTimelineListeners(containerWrapper, container, overallMinTime, overallDuration, containerWidth, visData);
}

// Helper to draw suite underlines
function drawSuiteUnderline(rowDiv, startPercent, endPercent, colorClass, rowHeight, underlineHeight, suiteId) {
    const underline = document.createElement('div');
    underline.className = `suite-underline`; // Base class
    underline.style.left = `${startPercent}%`;
    underline.style.width = `${endPercent - startPercent}%`;
    underline.style.top = `${rowHeight - underlineHeight}px`; // Position at the bottom
    // Use the actual color from the palette for the background
    const colorIdx = suiteColorMap.get(suiteId) ?? 0;
    underline.style.backgroundColor = VIS_PALETTE[colorIdx % VIS_PALETTE.length];
    underline.title = `Suite ID: ${suiteId}`; // Add tooltip for suite ID
    rowDiv.appendChild(underline);
}


function getSuiteColorClass(suiteId) {
    if (!suiteId) { // Handle cases where suiteId might be null or undefined
        const defaultIndex = nextSuiteColorIndex++ % VIS_PALETTE.length;
        // console.warn("Suite ID missing, assigning default color index:", defaultIndex);
        return `color-${defaultIndex}`;
    }
    if (!suiteColorMap.has(suiteId)) {
        suiteColorMap.set(suiteId, nextSuiteColorIndex);
        nextSuiteColorIndex++; // Increment global index only for new suites
    }
    const colorIndex = suiteColorMap.get(suiteId) % VIS_PALETTE.length; // Wrap index when getting color
    return `color-${colorIndex}`;
}

function drawTimelineAxis(axisHeader, minTime, maxTime, containerWidth) {
    axisHeader.innerHTML = ''; // Clear previous axis
    const duration = maxTime - minTime;
    if (duration <= 0 || containerWidth <= 0) return; // Guard against invalid input

    const startTime = new Date(minTime);
    const endTime = new Date(maxTime);

    // Determine appropriate tick intervals (e.g., seconds, minutes, hours)
    let tickIntervalMs;
    const durationSec = duration / 1000;
    const durationMin = durationSec / 60;
    const durationHr = durationMin / 60;

    // Rough guide for tick density
    const minTickGapPx = 100; // Minimum pixels between major ticks
    const maxTicks = Math.floor(containerWidth / minTickGapPx);

    // Simple logic: Adjust based on total duration
    if (durationHr > maxTicks * 2) tickIntervalMs = 3600 * 1000 * Math.ceil(durationHr / maxTicks); // Multiple hours
    else if (durationHr > 2) tickIntervalMs = 3600 * 1000; // Hourly
    else if (durationMin > maxTicks * 2) tickIntervalMs = 60 * 1000 * Math.ceil(durationMin / maxTicks); // Multiple minutes
    else if (durationMin > 5) tickIntervalMs = 60 * 1000; // Minutely
    else if (durationSec > maxTicks * 2) tickIntervalMs = 1000 * Math.ceil(durationSec / maxTicks); // Multiple seconds
    else tickIntervalMs = 1000; // Default to seconds for short durations

    // Start Time Label (Always show)
    const startLabel = createTimeLabel(formatDateTime(startTime), 0, 'translateX(0)');
    startLabel.title = startTime.toISOString();
    axisHeader.appendChild(startLabel);

    // End Time Label (Always show)
    const endLabel = createTimeLabel(formatDateTime(endTime), 100, 'translateX(-100%)');
    endLabel.title = endTime.toISOString();
    axisHeader.appendChild(endLabel);

    // Generate ticks based on interval
    const firstTickTime = Math.ceil(minTime / tickIntervalMs) * tickIntervalMs;
    for (let tickTime = firstTickTime; tickTime < maxTime; tickTime += tickIntervalMs) {
        const percent = ((tickTime - minTime) / duration) * 100;

        // Avoid placing ticks too close to the edges where start/end labels are
        if (percent < 2 || percent > 98) continue;

        const tickDate = new Date(tickTime);
        const labelText = formatTime(tickDate); // Show only time for intermediate ticks

        const label = createTimeLabel(labelText, percent);
        label.title = formatDateTime(tickDate); // Full timestamp on hover
        axisHeader.appendChild(label);

        // Optional: Add vertical grid lines (lightly dotted)
        const gridLine = document.createElement('div');
        gridLine.style.position = 'absolute';
        gridLine.style.left = `${percent}%`;
        gridLine.style.top = '0';
        gridLine.style.bottom = '0'; // Stretch across axis header height
        gridLine.style.borderLeft = '1px dotted #ddd';
        gridLine.style.width = '1px';
        gridLine.style.zIndex = '-1'; // Behind labels
        axisHeader.appendChild(gridLine);
    }
}

function createTimeLabel(text, percent, transform = 'translateX(-50%)') {
    const label = document.createElement('span');
    label.className = 'time-label';
    label.textContent = text;
    label.style.left = `${percent}%`;
    label.style.transform = transform; // Center by default, override for edges
    return label;
}


// Listener Management (Store listener references to allow removal)
let timelineMouseMoveListener = null;
let timelineClickListener = null;
let timelineMouseOutListener = null;
let squareMouseOverListener = null;
let squareMouseOutListener = null;

// Modified to accept visData used for the current view
function addTimelineListeners(containerWrapper, container, overallMinTime, overallDuration, containerWidth, visData) {
    const tooltip = document.getElementById('timelineTooltip');
    const clickInfo = document.getElementById('timelineClickInfo');
    const cursorLine = document.getElementById('timelineCursorLine');
    const clickMarker = document.getElementById('timelineClickMarkerLine');

    if (!tooltip || !clickInfo || !cursorLine || !clickMarker || !containerWrapper || !container || !visData) {
        console.error("Cannot add timeline listeners: Required elements or visData missing.");
        return;
    }

    removeTimelineListeners(); // Ensure no duplicate listeners exist

    let isOverSquare = false; // Track if mouse is currently over a test square

    // --- Mouse Move Listener (on the wrapper for coordinate calculations) ---
    timelineMouseMoveListener = (event) => {
        const rect = containerWrapper.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top; // Y position relative to wrapper

        // Check if cursor is within the bounds of the timeline wrapper
        if (x >= 0 && x <= containerWidth && y >= 0 && y <= containerWrapper.clientHeight) {
            const timeRatio = Math.max(0, Math.min(1, x / containerWidth)); // Clamp ratio
            const currentTime = new Date(overallMinTime + (overallDuration * timeRatio));

            // Update cursor line position
            cursorLine.style.left = `${x}px`;
            cursorLine.style.display = 'block';

            // Update tooltip only if NOT over a test square
            if (!isOverSquare) {
                tooltip.textContent = formatDateTime(currentTime);
                // Position tooltip near cursor
                positionTooltip(tooltip, event);
                tooltip.style.display = 'block';
            }
        } else {
            // Cursor moved outside the timeline bounds
            cursorLine.style.display = 'none';
            if (!isOverSquare) { // Hide generic tooltip if not over a square
                tooltip.style.display = 'none';
            }
        }
    };

    // --- Click Listener (on the wrapper) ---
    timelineClickListener = (event) => {
        const rect = containerWrapper.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const timeRatio = Math.max(0, Math.min(1, x / containerWidth)); // Clamp ratio

        if (x >= 0 && x <= containerWidth) { // Check click is within horizontal bounds
            const clickTimeMs = overallMinTime + (overallDuration * timeRatio);

            // Update click marker position
            clickMarker.style.left = `${x}px`;
            clickMarker.style.display = 'block';

            // Find tests active at the clicked time using the passed visData
            clickInfo.innerHTML = `<strong>Tests active around ${formatDateTime(new Date(clickTimeMs))}:</strong><br>`;
            let testsAtTime = [];
            visData.forEach(trx => { // Use the filtered visData
                trx.tests.forEach(test => {
                    // Ensure test has valid times before checking
                    if (test.startTime && test.endTime) {
                        const start = test.startTime.getTime();
                        const end = test.endTime.getTime();
                        // Check if click time falls within the test's start (inclusive) and end (exclusive) time
                        if (clickTimeMs >= start && clickTimeMs < end) {
                            testsAtTime.push(`<span>${trx.fileName}:</span> ${test.testName} (<em>${test.outcome}</em>)`);
                        }
                    }
                });
            });

            clickInfo.innerHTML += testsAtTime.length > 0 ? testsAtTime.join('<br>') : '<em>No tests active at this exact time.</em>';
            clickInfo.scrollTop = 0; // Scroll info box to top

        } else {
            // Click was outside horizontal bounds (e.g., padding)
            // clickInfo.innerHTML = 'Click on timeline to see tests'; // Keep existing info?
            // clickMarker.style.display = 'none'; // Keep marker? User decision. Let's hide it.
            clickMarker.style.display = 'none';
        }
    };

    // --- Mouse Out Listener (on the wrapper) ---
    timelineMouseOutListener = (event) => {
        // Check if the mouse is leaving the wrapper element entirely
        if (!containerWrapper.contains(event.relatedTarget)) {
            cursorLine.style.display = 'none';
            if (!isOverSquare) { // Hide generic tooltip if not over a square
                tooltip.style.display = 'none';
            }
        }
    };

    // --- Mouse Over Listener (delegated on the container holding squares) ---
    squareMouseOverListener = (event) => {
        const square = event.target.closest('.test-square');
        if (square) {
            isOverSquare = true; // Mouse is now over a square
            tooltip.style.display = 'block'; // Ensure tooltip is visible

            // Format test details for tooltip
            let testDetails = `${square.dataset.testName}\n` +
                `Suite: ${square.dataset.suiteName}\n` +
                `File: ${square.dataset.fileName}\n` +
                `Outcome: ${square.dataset.outcome}\n` +
                `Start: ${formatTime(new Date(square.dataset.startTime))}\n` + // Use formatTime for brevity
                `End: ${formatTime(new Date(square.dataset.endTime))}\n` +
                `Duration: ${formatDuration(parseInt(square.dataset.duration, 10))}`;

            if (square.dataset.error) {
                testDetails += `\nError: ${square.dataset.error}`;
            }

            tooltip.textContent = testDetails;
            // Position tooltip near cursor
            positionTooltip(tooltip, event);
        }
    };

    // --- Mouse Out Listener (delegated on the container holding squares) ---
    squareMouseOutListener = (event) => {
        const square = event.target.closest('.test-square');
        if (square) {
            isOverSquare = false; // Mouse has left a square

            // Hide tooltip only if the mouse is moving outside the container OR onto a non-square element within the container
            if (!containerWrapper.contains(event.relatedTarget) || !event.relatedTarget?.closest('.test-square')) {
                // Check if the cursor is still inside the main wrapper to potentially show the time tooltip again
                const rect = containerWrapper.getBoundingClientRect();
                const currentX = event.clientX - rect.left; // Use current event's clientX
                if (currentX >= 0 && currentX <= containerWidth) {
                    // Cursor is still inside wrapper, but not over a square -> show time tooltip
                    const timeRatio = Math.max(0, Math.min(1, currentX / containerWidth)); // Clamp
                    const currentTime = new Date(overallMinTime + (overallDuration * timeRatio));
                    tooltip.textContent = formatDateTime(currentTime);
                    positionTooltip(tooltip, event);
                    tooltip.style.display = 'block'; // Make sure it's visible
                } else {
                    // Cursor moved outside wrapper completely
                    tooltip.style.display = 'none';
                }
            }
            // If moving to another square, the squareMouseOverListener for the *new* square will handle the tooltip update.
        }
    };

    // Attach listeners
    containerWrapper.addEventListener('mousemove', timelineMouseMoveListener);
    containerWrapper.addEventListener('click', timelineClickListener);
    containerWrapper.addEventListener('mouseout', timelineMouseOutListener);
    container.addEventListener('mouseover', squareMouseOverListener); // Use event delegation
    container.addEventListener('mouseout', squareMouseOutListener);  // Use event delegation

    console.log("Timeline listeners added.");
}

function positionTooltip(tooltip, event) {
    // Position tooltip slightly offset from the mouse cursor
    // Add logic to prevent tooltip from going off-screen
    const offsetX = 15;
    const offsetY = 15;
    let x = event.clientX + offsetX;
    let y = event.clientY + offsetY;

    // Get viewport dimensions
    const vw = Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0);
    const vh = Math.max(document.documentElement.clientHeight || 0, window.innerHeight || 0);

    // Get tooltip dimensions (need to display it briefly to measure)
    const displayBefore = tooltip.style.display;
    tooltip.style.visibility = 'hidden'; // Keep space but don't show
    tooltip.style.display = 'block';
    const ttRect = tooltip.getBoundingClientRect();
    tooltip.style.display = displayBefore; // Restore original display
    tooltip.style.visibility = 'visible';

    // Adjust position if tooltip overflows viewport
    if (x + ttRect.width > vw) {
        x = event.clientX - ttRect.width - offsetX; // Position to the left
    }
    if (y + ttRect.height > vh) {
        y = event.clientY - ttRect.height - offsetY; // Position above
    }

    // Ensure tooltip doesn't go off the top/left edges either
    if (x < 0) x = offsetX;
    if (y < 0) y = offsetY;


    tooltip.style.left = `${x}px`;
    tooltip.style.top = `${y}px`;
}

function removeTimelineListeners() {
    const containerWrapper = document.getElementById('timelineContainerWrapper');
    const container = document.getElementById('timelineContainer');
    const tooltip = document.getElementById('timelineTooltip');
    const cursorLine = document.getElementById('timelineCursorLine');
    const clickMarker = document.getElementById('timelineClickMarkerLine');

    // Remove listeners if they exist
    if (containerWrapper) {
        if (timelineMouseMoveListener) containerWrapper.removeEventListener('mousemove', timelineMouseMoveListener);
        if (timelineClickListener) containerWrapper.removeEventListener('click', timelineClickListener);
        if (timelineMouseOutListener) containerWrapper.removeEventListener('mouseout', timelineMouseOutListener);
    }
    if (container) {
        if (squareMouseOverListener) container.removeEventListener('mouseover', squareMouseOverListener);
        if (squareMouseOutListener) container.removeEventListener('mouseout', squareMouseOutListener);
    }

    // Nullify references
    timelineMouseMoveListener = null;
    timelineClickListener = null;
    timelineMouseOutListener = null;
    squareMouseOverListener = null;
    squareMouseOutListener = null;

    // Hide visual elements controlled by listeners
    if (tooltip) tooltip.style.display = 'none';
    if (cursorLine) cursorLine.style.display = 'none';
    if (clickMarker) clickMarker.style.display = 'none';

    // console.log("Timeline listeners removed."); // Optional: Log removal
}

// --- Date/Time Formatting Helpers (Used by Analysis/Visualization) ---
function formatDateTime(date) {
    if (!date || isNaN(date.getTime())) return "Invalid Date";
    // Adjust options for a clearer format, ensure 24hr format
    return date.toLocaleDateString(undefined, { year: 'numeric', month: '2-digit', day: '2-digit' }) + ' ' +
        date.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit' }); // en-GB for HH:MM:SS
}

function formatTime(date) {
    if (!date || isNaN(date.getTime())) return "Invalid Time";
    return date.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit' }); // en-GB for HH:MM:SS
}

function formatDuration(milliseconds) {
    if (isNaN(milliseconds) || milliseconds < 0) return "N/A";
    if (milliseconds < 1000) return `${milliseconds.toFixed(0)} ms`;
    let totalSeconds = milliseconds / 1000;
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = (totalSeconds % 60);

    let parts = [];
    if (hours > 0) parts.push(`${hours}h`);
    if (minutes > 0) parts.push(`${minutes}m`);
    if (seconds > 0 || parts.length === 0) { // Show seconds if > 0 or if it's the only unit
        // Show decimal only if duration < 1 min OR if there are hours/mins already
        const decimalPlaces = (totalSeconds < 60 && hours === 0 && minutes === 0) ? 1 : 0;
        parts.push(`${seconds.toFixed(decimalPlaces)}s`);
    }

    return parts.join(' ');
}
// ========================================================================
// --- END: Analysis Tab Logic ---
// ========================================================================


// ========================================================================
// --- AT Efficiency Tab Logic (Self-Contained Module/IIFE Pattern) ---
// ========================================================================
async function initializeEfficiencyTab() {
    if (efficiencyTabInitialized) return;
    console.log("Initializing AT Efficiency Tab...");

    // Dynamically import the calculator only when needed
    let RoiCalculator;
    try {
        const module = await import('/js/roi-calculator.js');
        RoiCalculator = module.RoiCalculator;
        if (!RoiCalculator) {
            throw new Error("RoiCalculator class not found in the imported module.");
        }
    } catch (error) {
        console.error("Failed to load roi-calculator.js:", error);
        // Display error message on the tab
        const resultsDiv = document.getElementById('efficiency-results');
        if (resultsDiv) {
            resultsDiv.innerHTML = '<p style="color: red; text-align: center; padding: 20px;">Error: Could not load calculation module. Please check console.</p>';
        }
        return; // Stop initialization
    }


    // --- DOM Elements specific to Efficiency Tab ---
    const efficiencySection = document.getElementById('efficiency');
    if (!efficiencySection) {
        console.error("Efficiency section not found.");
        return;
    }
    const manualTestTimeInput = efficiencySection.querySelector('#manualTestTime');
    const failInvestigationTimeInput = efficiencySection.querySelector('#failInvestigationTime');
    const frameworkCreationTimeInput = efficiencySection.querySelector('#frameworkCreationTime');
    const testCreationTimeInput = efficiencySection.querySelector('#testCreationTime');
    const testFixTimeInput = efficiencySection.querySelector('#testFixTime');
    const atFailPercentInput = efficiencySection.querySelector('#atFailPercent');
    const devFailPercentInput = efficiencySection.querySelector('#devFailPercent');
    const extraHrsPerDayInput = efficiencySection.querySelector('#extraHrsPerDay');
    const buildsPerWeekInput = efficiencySection.querySelector('#buildsPerWeek');
    const teamSizeInput = efficiencySection.querySelector('#teamSize');
    const predictionLengthInput = efficiencySection.querySelector('#predictionLength');
    const makeForecastButton = efficiencySection.querySelector('#makeForecastButton');
    const roiResultDisplay = efficiencySection.querySelector('#roiResult');
    const roiWeekSpan = efficiencySection.querySelector('#roiWeek');
    const roiChartCanvas = efficiencySection.querySelector('#roiChartCanvas');
    const efficiencyChartCanvas = efficiencySection.querySelector('#efficiencyChartCanvas');
    const roiTableBody = efficiencySection.querySelector('#roiTableBody');

    // Basic check for essential elements
    if (!makeForecastButton || !roiChartCanvas || !efficiencyChartCanvas || !roiTableBody || !roiWeekSpan || !roiResultDisplay) {
        console.error("Essential elements for AT Efficiency tab are missing.");
        return; // Stop initialization
    }

    // --- Chart instances (scoped to this initialization function) ---
    let roiChart = null;
    let efficiencyChart = null;

    // --- Helper function to read input values ---
    function getRoiInputs() {
        // Provide default values (0) if parsing fails or elements are missing
        const parseOrDefault = (element, defaultValue = 0) => {
            // Allow zero as a valid input
            if (element && element.value === "0") return 0;
            // Try parsing, fallback to default
            const value = parseFloat(element?.value);
            return isNaN(value) || value < 0 ? defaultValue : value; // Treat negative as invalid, use default
        };

        return {
            ManualTestExecutionHrs: parseOrDefault(manualTestTimeInput, 10.0) / 60.0, // Convert mins to hours
            FailAnalyzeHrs: parseOrDefault(failInvestigationTimeInput, 5.0) / 60.0,  // Convert mins to hours
            TafCreationHrs: parseOrDefault(frameworkCreationTimeInput, 80.0),
            AutoTestCreationHrs: parseOrDefault(testCreationTimeInput, 8.0),
            AutoTestFixHrs: parseOrDefault(testFixTimeInput, 2.0),
            AtFailsRatio: parseOrDefault(atFailPercentInput, 2.5) / 100.0,       // Convert % to ratio
            DevFailsRatio: parseOrDefault(devFailPercentInput, 2.5) / 100.0,      // Convert % to ratio
            NonDevHrs: parseOrDefault(extraHrsPerDayInput, 0.5),
            BuildsPerPeriod: parseOrDefault(buildsPerWeekInput, 5),               // Assuming Period = Week
            TeamCount: parseOrDefault(teamSizeInput, 4),
            PeriodsCount: parseOrDefault(predictionLengthInput, 54)
        };
    }

    // --- Helper function to format numbers for table ---
    function formatNumber(num, digits = 1) {
        if (typeof num !== 'number' || isNaN(num)) {
            return '--';
        }
        // Use Intl.NumberFormat for locale-aware formatting if needed
        return num.toFixed(digits);
    }

    // --- Function to update the ROI table ---
    function updateRoiTable(forecastData) {
        if (!roiTableBody) return;
        roiTableBody.innerHTML = ''; // Clear previous data

        if (!forecastData || !forecastData.series || forecastData.series.length === 0) {
            const row = roiTableBody.insertRow();
            const cell = row.insertCell();
            cell.colSpan = 8; // Match number of columns
            cell.textContent = 'No forecast data generated.';
            cell.style.textAlign = 'center';
            cell.style.fontStyle = 'italic';
            return;
        }

        forecastData.series.forEach((entry, index) => {
            const weekNumber = index + 1;
            const row = roiTableBody.insertRow();
            row.insertCell().textContent = weekNumber;
            row.insertCell().textContent = formatNumber(entry.TotalTests, 0); // Total tests (no decimals)
            row.insertCell().textContent = formatNumber(entry.TotalRuns, 0); // Total runs
            row.insertCell().textContent = formatNumber(entry.HrsToWriteTests, 1); // Hrs to write tests
            row.insertCell().textContent = formatNumber(entry.MtManDaysSavedPerRun, 2); // MT saved/run (days)
            row.insertCell().textContent = formatNumber(entry.MtManDaysSavedTotal, 1); // MT saved total (days)
            row.insertCell().textContent = formatNumber(entry.AtManDaysTotal, 1); // AT spent total (days)
            row.insertCell().textContent = formatNumber(entry.ROI, 1) + "%"; // ROI
        });
    }

    // --- Function to update the Charts ---
    function updateCharts(forecastData) {
        if (!forecastData || !forecastData.series || forecastData.series.length === 0) {
            // Optionally clear charts or show a message if no data
            if (roiChart) roiChart.destroy();
            if (efficiencyChart) efficiencyChart.destroy();
            roiChart = null;
            efficiencyChart = null;
            console.warn("No data to update charts.");
            // Clear canvas visually
            roiChartCanvas?.getContext('2d').clearRect(0, 0, roiChartCanvas.width, roiChartCanvas.height);
            efficiencyChartCanvas?.getContext('2d').clearRect(0, 0, efficiencyChartCanvas.width, efficiencyChartCanvas.height);
            return;
        }

        const labels = forecastData.series.map((_, index) => `Week ${index + 1}`);
        const roiValues = forecastData.series.map(entry => entry.ROI);
        const mtSavedValues = forecastData.series.map(entry => entry.MtManDaysSavedTotal);
        const atSpentValues = forecastData.series.map(entry => entry.AtManDaysTotal);
        // zeroReachIndex from calculator is 1-based week number where ROI becomes >= 0
        // Convert to 0-based index for array access
        const zeroReachArrayIndex = forecastData.zeroReachIndex > 0 ? forecastData.zeroReachIndex - 1 : -1;

        // --- ROI Chart ---
        if (roiChart) {
            roiChart.destroy(); // Destroy previous instance
        }
        const roiCtx = roiChartCanvas?.getContext('2d');
        if (roiCtx) {
            roiChart = new Chart(roiCtx, {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: [{
                        label: 'ROI (%)',
                        data: roiValues,
                        borderColor: 'rgb(217, 83, 79)', // Reddish
                        backgroundColor: 'rgba(217, 83, 79, 0.1)',
                        tension: 0.1,
                        fill: false,
                        pointRadius: 1, // Smaller points
                        pointHitRadius: 5,
                        borderWidth: 1.5
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false, // Crucial for resizing within flex container
                    scales: {
                        y: {
                            beginAtZero: false, // ROI can be negative
                            title: { display: true, text: 'ROI (%)' }
                        },
                        x: {
                            title: { display: true, text: 'Week' }
                        }
                    },
                    plugins: {
                        tooltip: {
                            mode: 'index',
                            intersect: false,
                            callbacks: {
                                label: function (context) {
                                    let label = context.dataset.label || '';
                                    if (label) { label += ': '; }
                                    if (context.parsed.y !== null) {
                                        label += context.parsed.y.toFixed(1) + '%';
                                    }
                                    return label;
                                }
                            }
                        },
                        legend: { display: false }, // Hide legend if only one dataset
                        annotation: { // Add annotation for ROI > 0
                            annotations: {
                                zeroLine: { // Line at ROI = 0
                                    type: 'line',
                                    yMin: 0,
                                    yMax: 0,
                                    borderColor: 'rgb(75, 192, 192)',
                                    borderWidth: 1,
                                    borderDash: [5, 5],
                                    label: {
                                        content: 'Break-even (ROI = 0%)',
                                        position: 'start',
                                        enabled: true,
                                        font: { size: 10 },
                                        backgroundColor: 'rgba(255, 255, 255, 0.5)'
                                    }
                                },
                                // Conditionally add ROI > 0 marker only if index is valid
                                ...(zeroReachArrayIndex !== -1 && zeroReachArrayIndex < labels.length && {
                                    roiPositiveMarker: {
                                        type: 'point',
                                        xValue: labels[zeroReachArrayIndex],
                                        yValue: roiValues[zeroReachArrayIndex],
                                        backgroundColor: 'rgba(92, 184, 92, 0.8)', // Greenish
                                        radius: 5,
                                        borderColor: 'rgb(76, 174, 76)',
                                        borderWidth: 1
                                    },
                                    roiPositiveLabel: {
                                        type: 'label',
                                        xValue: labels[zeroReachArrayIndex],
                                        yValue: roiValues[zeroReachArrayIndex],
                                        content: [`ROI Positive`, `Week ${zeroReachArrayIndex + 1}`],
                                        font: { size: 10 },
                                        position: 'center', // Adjust as needed
                                        yAdjust: -15, // Adjust label position slightly above point
                                        //xAdjust: 10,
                                        backgroundColor: 'rgba(255, 255, 255, 0.6)'
                                    }
                                })
                            }
                        }
                    }
                }
            });
        } else { console.error("ROI Chart canvas context not found."); }

        // --- Efficiency Chart (Man-days) ---
        if (efficiencyChart) {
            efficiencyChart.destroy(); // Destroy previous instance
        }
        const efficiencyCtx = efficiencyChartCanvas?.getContext('2d');
        if (efficiencyCtx) {
            efficiencyChart = new Chart(efficiencyCtx, {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: [
                        {
                            label: 'MT Saved (Total Days)',
                            data: mtSavedValues,
                            borderColor: 'rgb(92, 184, 92)', // Greenish
                            backgroundColor: 'rgba(92, 184, 92, 0.1)',
                            tension: 0.1,
                            fill: true, // Fill area under MT saved
                            pointRadius: 1,
                            pointHitRadius: 5,
                            borderWidth: 1.5
                        },
                        {
                            label: 'AT Spent (Total Days)',
                            data: atSpentValues,
                            borderColor: 'rgb(2, 117, 216)', // Blueish
                            backgroundColor: 'rgba(2, 117, 216, 0.1)',
                            tension: 0.1,
                            fill: true, // Fill area under AT spent
                            pointRadius: 1,
                            pointHitRadius: 5,
                            borderWidth: 1.5
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false, // Crucial for resizing within flex container
                    scales: {
                        y: {
                            beginAtZero: true,
                            title: { display: true, text: 'Cumulative Man-Days' },
                            stacked: false // Lines should not be stacked
                        },
                        x: {
                            title: { display: true, text: 'Week' }
                        }
                    },
                    plugins: {
                        tooltip: {
                            mode: 'index',
                            intersect: false,
                            callbacks: {
                                label: function (context) {
                                    let label = context.dataset.label || '';
                                    if (label) { label += ': '; }
                                    if (context.parsed.y !== null) {
                                        label += context.parsed.y.toFixed(1) + ' days';
                                    }
                                    return label;
                                }
                            }
                        },
                        legend: { display: true, position: 'bottom' }, // Show legend
                        annotation: { // Add annotation for crossover point
                            annotations: {
                                // Conditionally add crossover marker only if index is valid
                                ...(zeroReachArrayIndex !== -1 && zeroReachArrayIndex < labels.length && {
                                    crossoverMarker: {
                                        type: 'point',
                                        xValue: labels[zeroReachArrayIndex],
                                        // Place marker roughly where lines cross visually (average y-value at that point)
                                        yValue: (mtSavedValues[zeroReachArrayIndex] + atSpentValues[zeroReachArrayIndex]) / 2,
                                        backgroundColor: 'rgba(240, 173, 78, 0.8)', // Orangish
                                        radius: 5,
                                        borderColor: 'rgb(236, 151, 31)',
                                        borderWidth: 1
                                    },
                                    crossoverLabel: {
                                        type: 'label',
                                        xValue: labels[zeroReachArrayIndex],
                                        yValue: (mtSavedValues[zeroReachArrayIndex] + atSpentValues[zeroReachArrayIndex]) / 2,
                                        content: [`Break-even`, `(MT Saved ≥ AT Spent)`, `Week ${zeroReachArrayIndex + 1}`],
                                        font: { size: 10 },
                                        position: 'center', // Adjust as needed
                                        yAdjust: 15, // Below point
                                        backgroundColor: 'rgba(255, 255, 255, 0.6)'
                                    }
                                })
                            }
                        }
                    }
                }
            });
        } else { console.error("Efficiency Chart canvas context not found."); }
    }

    // --- Main function to trigger calculation and updates ---
    function runForecastAndUpdateUI() {
        console.log("Running forecast...");
        const inputs = getRoiInputs();

        // Basic input validation
        if (inputs.TeamCount <= 0 || inputs.PeriodsCount <= 0 || inputs.ManualTestExecutionHrs <= 0) {
            console.warn("Invalid inputs: Team size, prediction length, and manual test time must be positive.");
            // Clear previous results and show error message
            if (roiResultDisplay) roiResultDisplay.innerHTML = '<span style="color: red;">Invalid input values detected (e.g., Team Size, Prediction Length, Manual Test Time must be > 0).</span>';
            if (roiWeekSpan) roiWeekSpan.textContent = '--';
            if (roiTableBody) roiTableBody.innerHTML = ''; // Clear table
            // Clear charts
            if (roiChart) roiChart.destroy(); roiChart = null;
            if (efficiencyChart) efficiencyChart.destroy(); efficiencyChart = null;
            roiChartCanvas?.getContext('2d').clearRect(0, 0, roiChartCanvas.width, roiChartCanvas.height);
            efficiencyChartCanvas?.getContext('2d').clearRect(0, 0, efficiencyChartCanvas.width, efficiencyChartCanvas.height);
            return;
        }
        // Restore default message if inputs are okay now
        if (roiResultDisplay) roiResultDisplay.innerHTML = 'Test automation will self-sustain by <strong id="roiWeek">--</strong> week';


        const calculator = new RoiCalculator(inputs);
        const forecast = calculator.calculateForecast();

        console.log("Forecast calculated:", forecast);

        // Update ROI Result text (Need to re-find the span as innerHTML was replaced)
        const currentRoiWeekSpan = document.getElementById('roiWeek');
        if (currentRoiWeekSpan) {
            if (forecast.zeroReachIndex > 0) { // zeroReachIndex is 1-based index in the series array
                currentRoiWeekSpan.textContent = forecast.zeroReachIndex;
                currentRoiWeekSpan.style.color = '#5cb85c'; // Greenish for success
            } else {
                currentRoiWeekSpan.textContent = 'Never';
                currentRoiWeekSpan.style.color = '#d9534f'; // Reddish if never reached
            }
        }

        // Update Table and Charts
        updateRoiTable(forecast);
        updateCharts(forecast); // Pass the same forecast data
    }

    // --- Event Listener for the Button ---
    makeForecastButton.addEventListener('click', runForecastAndUpdateUI);

    // --- Initial Run (Optional) ---
    // Uncomment the line below if you want the forecast to run automatically when the tab is loaded
    // runForecastAndUpdateUI();

    efficiencyTabInitialized = true;
    console.log("AT Efficiency Tab Initialized.");
}
// ========================================================================
// --- END: AT Efficiency Tab Logic ---
// ========================================================================