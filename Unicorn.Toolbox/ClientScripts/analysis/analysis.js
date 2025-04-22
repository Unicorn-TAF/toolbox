"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g = Object.create((typeof Iterator === "function" ? Iterator : Object).prototype);
    return g.next = verb(0), g["throw"] = verb(1), g["return"] = verb(2), typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (g && (g = 0, op[0] && (_ = 0)), _) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.initializeAnalysisTab = initializeAnalysisTab;
var formatters_1 = require("../utils/formatters");
var visualization_1 = require("./visualization"); // Import modal functions
// --- Module State ---
var analysisTabInitialized = false;
var aggregatedFailures = new Map(); // Key: Normalized error message, Value: count
var allTrxData = []; // Stores processed data for all loaded files
// --- DOM Element Fetching ---
function getAnalysisElements() {
    var getElement = function (id) { return document.getElementById(id); };
    return {
        loadTrxButton: getElement('loadTrxButton'),
        trxFileLoader: getElement('trxFileLoader'),
        resultsTable: getElement('resultsTable'),
        resultsTableBody: getElement('resultsTableBody'),
        topFailsListContainer: getElement('topFailsList'),
        loadStatusSpan: getElement('loadStatus'),
        visualizeButton: getElement('visualizeButton'),
        searchButton: getElement('searchFailsButton'),
        failFilterTypeSelect: getElement('failFilterType'),
        failSearchCriteriaInput: getElement('failSearchCriteria'),
    };
}
// --- TRX Parsing and Processing ---
function extractTrxSummary(xmlDoc) {
    var _a, _b;
    try {
        var timesNode = xmlDoc.getElementsByTagName("Times")[0];
        var summaryNode = xmlDoc.getElementsByTagName("ResultSummary")[0];
        var countersNode_1 = summaryNode === null || summaryNode === void 0 ? void 0 : summaryNode.getElementsByTagName("Counters")[0];
        var resultsNode = xmlDoc.getElementsByTagName("Results")[0];
        var testListsContainer = xmlDoc.getElementsByTagName("TestLists")[0];
        var testLists = testListsContainer ? testListsContainer.getElementsByTagName("TestList") : [];
        if (!timesNode || !summaryNode || !countersNode_1) {
            console.warn("TRX file missing essential elements for summary (Times, ResultSummary, Counters).");
            return null;
        }
        var startTimeStr = timesNode.getAttribute("start");
        var finishTimeStr = timesNode.getAttribute("finish");
        var totalDurationMin = 0;
        var startMs = (_a = (0, formatters_1.parseDateTimeSafe)(startTimeStr)) === null || _a === void 0 ? void 0 : _a.getTime();
        var finishMs = (_b = (0, formatters_1.parseDateTimeSafe)(finishTimeStr)) === null || _b === void 0 ? void 0 : _b.getTime();
        if (startMs && finishMs && finishMs >= startMs) {
            totalDurationMin = (finishMs - startMs) / (1000 * 60);
        }
        else {
            console.warn("Invalid/missing start/finish times:", startTimeStr, finishTimeStr);
        }
        var rawDurationMin = 0;
        if (resultsNode) {
            var unitTestResults = resultsNode.getElementsByTagName("UnitTestResult");
            var rawDurationMs = 0;
            for (var i = 0; i < unitTestResults.length; i++) {
                var durationStr = unitTestResults[i].getAttribute("duration");
                if (durationStr) {
                    rawDurationMs += (0, formatters_1.parseTimespan)(durationStr);
                }
            }
            rawDurationMin = rawDurationMs / (1000 * 60);
        }
        else {
            console.warn("Missing <Results> node, cannot calculate Raw Duration.");
        }
        var getCounter = function (name) { return parseInt((countersNode_1 === null || countersNode_1 === void 0 ? void 0 : countersNode_1.getAttribute(name)) || "0", 10) || 0; };
        var totalTests = getCounter("total");
        var failedTests = getCounter("failed");
        var errorTests = getCounter("error");
        var timeoutTests = getCounter("timeout");
        var abortedTests = getCounter("aborted");
        var inconclusiveTests = getCounter("inconclusive");
        var notExecutedTests = getCounter("notExecuted");
        var pendingTests = getCounter("pending");
        var notRunnableTests = getCounter("notRunnable");
        // Define 'Skipped' and 'Failed' broadly
        var skipped = notExecutedTests + inconclusiveTests + pendingTests + notRunnableTests;
        var robustFailed = failedTests + errorTests + timeoutTests + abortedTests;
        var suitesCount = testLists.length;
        return {
            totalDurationMin: totalDurationMin,
            rawDurationMin: rawDurationMin,
            suitesCount: suitesCount,
            testsCount: totalTests,
            failedTests: robustFailed,
            skippedTests: skipped
        };
    }
    catch (error) {
        console.error("Error extracting summary data:", error);
        return null;
    }
}
// Modify processTrxFile function signature and return type
function processTrxFile(file) {
    return __awaiter(this, void 0, void 0, function () {
        return __generator(this, function (_a) {
            return [2 /*return*/, new Promise(function (resolve) {
                    var reader = new FileReader();
                    reader.onload = function (e) {
                        var _a, _b, _c, _d, _e;
                        var result = {
                            fileName: file.name,
                            summary: null,
                            data: null
                        };
                        try {
                            var xmlContent = (_a = e.target) === null || _a === void 0 ? void 0 : _a.result;
                            if (!xmlContent)
                                throw new Error("File content is empty.");
                            var parser = new DOMParser();
                            var xmlDoc = parser.parseFromString(xmlContent, "text/xml");
                            var parseError = xmlDoc.querySelector("parsererror");
                            if (parseError) {
                                console.error("Error parsing TRX file \"".concat(file.name, "\":"), parseError.textContent);
                                result.errorType = "XML Parse Error";
                                resolve(result);
                                return;
                            }
                            // *** Extract Summary HERE ***
                            result.summary = extractTrxSummary(xmlDoc); // Pass the XMLDocument
                            // Initialize data structure (even if summary failed, we might get tests)
                            var currentTrxData_1 = {
                                fileName: file.name,
                                runStartTime: null,
                                runEndTime: null,
                                tests: [],
                                testListMap: new Map()
                            };
                            // Extract Times
                            var timesNode = xmlDoc.getElementsByTagName("Times")[0];
                            if (timesNode) {
                                currentTrxData_1.runStartTime = (0, formatters_1.parseDateTimeSafe)(timesNode.getAttribute("start"));
                                currentTrxData_1.runEndTime = (0, formatters_1.parseDateTimeSafe)(timesNode.getAttribute("finish"));
                            } // Continue even if timesNode is missing
                            // Extract TestLists
                            var testListsContainer = xmlDoc.getElementsByTagName("TestLists")[0];
                            testListsContainer === null || testListsContainer === void 0 ? void 0 : testListsContainer.querySelectorAll("TestList").forEach(function (listNode) {
                                var id = listNode.getAttribute("id");
                                var name = listNode.getAttribute("name");
                                if (id && name)
                                    currentTrxData_1.testListMap.set(id, name);
                            });
                            // Extract Test Results (same logic as before)
                            var unitTestResults = xmlDoc.getElementsByTagName("UnitTestResult");
                            for (var i = 0; i < unitTestResults.length; i++) {
                                var testResultNode = unitTestResults[i];
                                var outcome = testResultNode.getAttribute("outcome") || "Unknown";
                                var testName = testResultNode.getAttribute("testName") || "Unnamed Test ".concat(i + 1);
                                var testListId = testResultNode.getAttribute("testListId");
                                var startTime = (0, formatters_1.parseDateTimeSafe)(testResultNode.getAttribute("startTime"));
                                var endTime = (0, formatters_1.parseDateTimeSafe)(testResultNode.getAttribute("endTime"));
                                var durationStr = testResultNode.getAttribute("duration");
                                var durationMs = durationStr ? (0, formatters_1.parseTimespan)(durationStr) : 0;
                                if (!endTime && startTime && durationMs > 0) {
                                    endTime = new Date(startTime.getTime() + durationMs);
                                }
                                var errorMessage = null;
                                var stackTrace = null;
                                if (outcome === "Failed") {
                                    var errorInfo = testResultNode.getElementsByTagName("ErrorInfo")[0];
                                    errorMessage = ((_c = (_b = errorInfo === null || errorInfo === void 0 ? void 0 : errorInfo.getElementsByTagName("Message")[0]) === null || _b === void 0 ? void 0 : _b.textContent) === null || _c === void 0 ? void 0 : _c.trim()) || null;
                                    stackTrace = ((_e = (_d = errorInfo === null || errorInfo === void 0 ? void 0 : errorInfo.getElementsByTagName("StackTrace")[0]) === null || _d === void 0 ? void 0 : _d.textContent) === null || _e === void 0 ? void 0 : _e.trim()) || null;
                                    if (errorMessage) {
                                        var normalizedMessage = errorMessage; // Placeholder for potential normalization
                                        aggregatedFailures.set(normalizedMessage, (aggregatedFailures.get(normalizedMessage) || 0) + 1);
                                    }
                                }
                                var testData = {
                                    testName: testName,
                                    startTime: startTime,
                                    endTime: endTime,
                                    durationMs: durationMs,
                                    outcome: outcome,
                                    testListId: testListId,
                                    errorMessage: errorMessage,
                                    stackTrace: stackTrace,
                                    suiteName: testListId ? (currentTrxData_1.testListMap.get(testListId) || "Suite ".concat(testListId)) : 'Unknown Suite'
                                };
                                currentTrxData_1.tests.push(testData);
                            }
                            result.data = currentTrxData_1; // Assign the processed data
                            resolve(result);
                        }
                        catch (error) {
                            console.error("Critical error processing file \"".concat(file.name, "\":"), error);
                            result.errorType = error.message || "Processing Error";
                            resolve(result);
                        }
                    };
                    reader.onerror = function (e) {
                        console.error("Error reading file \"".concat(file.name, "\":"), e);
                        resolve({ fileName: file.name, summary: null, data: null, errorType: "File Read Error" });
                    };
                    reader.readAsText(file);
                })];
        });
    });
}
// --- UI Display Functions ---
function displayTrxSummaryRow(elements, fileName, summary) {
    var tableBody = elements.resultsTableBody;
    if (!tableBody)
        return;
    var row = tableBody.insertRow();
    row.insertCell().textContent = fileName;
    row.insertCell().textContent = summary.totalDurationMin.toFixed(1);
    row.insertCell().textContent = summary.rawDurationMin.toFixed(1);
    row.insertCell().textContent = summary.suitesCount.toString();
    row.insertCell().textContent = summary.testsCount.toString();
    row.insertCell().textContent = summary.failedTests.toString();
    row.insertCell().textContent = summary.skippedTests.toString();
    // Add tooltips
    row.cells[0].title = fileName;
    row.cells[1].title = "Total Duration: ".concat(summary.totalDurationMin.toFixed(3), " min");
    row.cells[2].title = "Sum of Test Durations: ".concat(summary.rawDurationMin.toFixed(3), " min");
    row.cells[5].title = "".concat(summary.failedTests, " Failed (includes errors/timeouts/aborts)");
    row.cells[6].title = "".concat(summary.skippedTests, " Skipped/Inconclusive/NotRun");
}
function displayErrorRow(elements, fileName, errorType) {
    var tableBody = elements.resultsTableBody;
    if (!tableBody)
        return;
    var row = tableBody.insertRow();
    row.classList.add('error-row');
    row.insertCell().textContent = fileName;
    var cellError = row.insertCell();
    cellError.textContent = "Error: ".concat(errorType);
    cellError.colSpan = 6; // Span across remaining columns
    cellError.title = "File: ".concat(fileName, "\nError Type: ").concat(errorType);
}
function displayTopFails(elements) {
    var container = elements.topFailsListContainer;
    if (!container)
        return;
    var sortedFails = Array.from(aggregatedFailures.entries())
        .map(function (_a) {
        var message = _a[0], count = _a[1];
        return ({ message: message, count: count });
    })
        .sort(function (a, b) { return b.count - a.count; }); // Sort descending by count
    var top5Fails = sortedFails.slice(0, 5);
    container.innerHTML = ''; // Clear previous entries
    if (top5Fails.length === 0) {
        container.innerHTML = '<p class="no-data-message">No failures found in loaded files.</p>';
        return;
    }
    top5Fails.forEach(function (_a) {
        var message = _a.message, count = _a.count;
        var failItemDiv = document.createElement('div');
        failItemDiv.className = 'fail-item';
        failItemDiv.title = "Count: ".concat(count, "\nMessage:\n").concat(message);
        var messageBoxDiv = document.createElement('div');
        messageBoxDiv.className = 'fail-message-box';
        var preElement = document.createElement('pre');
        preElement.textContent = message;
        messageBoxDiv.appendChild(preElement);
        var countSpan = document.createElement('span');
        countSpan.className = 'fail-count';
        countSpan.textContent = count.toString();
        countSpan.dataset.action = 'show-fails'; // Action for click handler
        countSpan.dataset.message = message; // Store message for filtering
        failItemDiv.appendChild(messageBoxDiv);
        failItemDiv.appendChild(countSpan);
        container.appendChild(failItemDiv);
    });
}
// --- Sorting ---
function sortTableByColumn(table, headerCell) {
    var _a;
    var tableBody = table.tBodies[0]; // Use tBodies[0] for better compatibility
    if (!tableBody) {
        console.error("Table body not found for sorting.");
        return;
    }
    var sortKey = headerCell.dataset.sortKey;
    var sortType = headerCell.dataset.sortType || 'text';
    // Ensure sortKey exists before proceeding
    if (!sortKey) {
        console.warn("Header cell clicked has no data-sort-key attribute.");
        return;
    }
    var currentDirection = headerCell.dataset.sortDirection || 'none'; // Start with 'none'
    // Cycle: none -> asc -> desc -> none ? Or just asc <-> desc? Let's do asc <-> desc
    var newDirection = currentDirection === 'asc' ? 'desc' : 'asc';
    var headerCells = Array.from(((_a = headerCell.parentElement) === null || _a === void 0 ? void 0 : _a.children) || []);
    var columnIndex = headerCells.indexOf(headerCell);
    if (columnIndex === -1) {
        console.error("Could not determine column index.");
        return;
    }
    // Separate rows
    var rowsToSort = [];
    var errorRows = [];
    tableBody.querySelectorAll('tr').forEach(function (row) {
        if (row.classList.contains('error-row')) {
            errorRows.push(row);
        }
        else {
            rowsToSort.push(row);
        }
    });
    rowsToSort.sort(function (rowA, rowB) {
        var _a, _b, _c, _d;
        var cellA = rowA.cells[columnIndex];
        var cellB = rowB.cells[columnIndex];
        var valueA = (_b = (_a = cellA === null || cellA === void 0 ? void 0 : cellA.textContent) === null || _a === void 0 ? void 0 : _a.trim()) !== null && _b !== void 0 ? _b : '';
        var valueB = (_d = (_c = cellB === null || cellB === void 0 ? void 0 : cellB.textContent) === null || _c === void 0 ? void 0 : _c.trim()) !== null && _d !== void 0 ? _d : '';
        var compareResult = 0;
        switch (sortType) {
            case 'number':
                var numA = parseFloat(valueA) || 0;
                var numB = parseFloat(valueB) || 0;
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
    headerCells.forEach(function (th) {
        if (th !== headerCell && th.dataset.sortKey) { // Reset others
            th.classList.remove('sorted-asc', 'sorted-desc');
            th.dataset.sortDirection = 'none';
        }
    });
    headerCell.classList.remove('sorted-asc', 'sorted-desc');
    headerCell.classList.add(newDirection === 'asc' ? 'sorted-asc' : 'sorted-desc');
    headerCell.dataset.sortDirection = newDirection;
    // Re-append rows
    // tableBody.innerHTML = ''; // Avoid innerHTML for performance if table is large
    rowsToSort.forEach(function (row) { return tableBody.appendChild(row); }); // Re-appending moves them
    errorRows.forEach(function (row) { return tableBody.appendChild(row); }); // Append error rows at the end
}
// --- Event Handlers ---
// Modify handleTrxFileLoad to use the new structure
function handleTrxFileLoad(event, elements) {
    return __awaiter(this, void 0, void 0, function () {
        var input, files, results, filesProcessedCount, hasVisData;
        return __generator(this, function (_a) {
            switch (_a.label) {
                case 0:
                    input = event.target;
                    files = input.files;
                    if (!files || files.length === 0 || !elements.resultsTableBody /*...other checks...*/) {
                        if (input)
                            input.value = '';
                        return [2 /*return*/];
                    }
                    // --- Reset UI and State --- (Keep this)
                    elements.loadStatusSpan.textContent = "Loading ".concat(files.length, " file(s)...");
                    return [4 /*yield*/, Promise.all(Array.from(files).map(function (file) { return processTrxFile(file); }))];
                case 1:
                    results = _a.sent();
                    filesProcessedCount = 0;
                    allTrxData = []; // Reset allTrxData before populating
                    // Process results and display rows
                    results.forEach(function (result) {
                        if (!result)
                            return; // Should not happen with promise handling, but good practice
                        if (result.errorType) {
                            displayErrorRow(elements, result.fileName, result.errorType);
                            // Add minimal entry for error files so they aren't hidden by search
                            allTrxData.push({ fileName: result.fileName, tests: [], runStartTime: null, runEndTime: null, testListMap: new Map() });
                        }
                        else if (result.summary && result.data) {
                            // Display summary row using extracted summary
                            displayTrxSummaryRow(elements, result.fileName, result.summary);
                            allTrxData.push(result.data); // Add the full data for vis/search
                            filesProcessedCount++;
                        }
                        else if (result.data) {
                            // Case where summary failed but tests were processed
                            displayErrorRow(elements, result.fileName, "Summary Missing/Invalid");
                            allTrxData.push(result.data); // Still add data for search/vis
                            // filesProcessedCount++; // Arguably not 'fully' successful
                        }
                        else {
                            // Fallback error case
                            displayErrorRow(elements, result.fileName, "Unknown Processing Issue");
                            allTrxData.push({ fileName: result.fileName, tests: [], runStartTime: null, runEndTime: null, testListMap: new Map() });
                        }
                    });
                    console.log("".concat(filesProcessedCount, " of ").concat(files.length, " files processed successfully."));
                    displayTopFails(elements);
                    hasVisData = allTrxData.some(function (d) {
                        return d.runStartTime && d.runEndTime && d.tests.some(function (t) { return t.startTime && t.endTime && t.startTime < t.endTime; });
                    });
                    elements.visualizeButton.disabled = !hasVisData; // Non-null assertion ok due to initial check
                    elements.visualizeButton.classList.toggle('gradient-active', hasVisData);
                    elements.loadStatusSpan.textContent = "".concat(files.length, " file(s) processed.");
                    // Clear the file input value
                    input.value = '';
                    return [2 /*return*/];
            }
        });
    });
}
function handleSortTable(event, elements) {
    var target = event.target;
    var header = target.closest('th[data-sort-key]');
    if (header && elements.resultsTable) {
        sortTableByColumn(elements.resultsTable, header);
    }
}
function handleFailCountClick(event, elements) {
    var target = event.target;
    if (target.matches('.fail-count[data-action="show-fails"]')) {
        var message = target.dataset.message;
        var failFilterTypeSelect = elements.failFilterTypeSelect, failSearchCriteriaInput = elements.failSearchCriteriaInput, searchButton = elements.searchButton;
        if (message && failFilterTypeSelect && failSearchCriteriaInput && searchButton) {
            failFilterTypeSelect.value = 'ErrorMessage';
            failSearchCriteriaInput.value = message;
            searchButton.click(); // Trigger the search handler
            console.log("Filtering results for failure message: \"".concat(message, "\""));
        }
        else {
            console.warn("Could not find fail filter elements or message to perform action.");
        }
    }
}
function handleSearchFails(elements) {
    var _a, _b, _c, _d, _e;
    var filterType = (_b = (_a = elements.failFilterTypeSelect) === null || _a === void 0 ? void 0 : _a.value) !== null && _b !== void 0 ? _b : 'None';
    var criteria = (_e = (_d = (_c = elements.failSearchCriteriaInput) === null || _c === void 0 ? void 0 : _c.value) === null || _d === void 0 ? void 0 : _d.trim().toLowerCase()) !== null && _e !== void 0 ? _e : '';
    var tableBody = elements.resultsTableBody;
    console.log("Searching fails with type: ".concat(filterType, ", criteria: \"").concat(criteria, "\""));
    if (!tableBody)
        return;
    var requiresFilter = filterType !== 'None' && criteria !== '';
    var regex = null;
    if (requiresFilter && (filterType === 'ErrorMessageRegex' || filterType === 'TestNameRegex')) {
        try {
            regex = new RegExp(criteria, 'i'); // Case-insensitive regex
        }
        catch (e) {
            console.error("Invalid Regular Expression:", e);
            alert("Invalid Regular Expression provided.");
            return; // Stop search
        }
    }
    var matchingFileNames = new Set();
    if (requiresFilter) {
        allTrxData.forEach(function (trxFile) {
            var fileMatches = trxFile.tests.some(function (test) {
                var _a, _b, _c, _d;
                var testNameLower = ((_a = test.testName) === null || _a === void 0 ? void 0 : _a.toLowerCase()) || '';
                var errorMessageLower = ((_b = test.errorMessage) === null || _b === void 0 ? void 0 : _b.toLowerCase()) || '';
                switch (filterType) {
                    case 'TestName': return testNameLower.includes(criteria);
                    case 'TestNameRegex': return (_c = regex === null || regex === void 0 ? void 0 : regex.test(testNameLower)) !== null && _c !== void 0 ? _c : false;
                    case 'ErrorMessage': return test.outcome === 'Failed' && errorMessageLower.includes(criteria);
                    case 'ErrorMessageRegex': return test.outcome === 'Failed' && ((_d = regex === null || regex === void 0 ? void 0 : regex.test(errorMessageLower)) !== null && _d !== void 0 ? _d : false);
                    default: return false;
                }
            });
            if (fileMatches) {
                matchingFileNames.add(trxFile.fileName);
            }
        });
    }
    // Filter rows in the summary table
    var visibleRows = 0;
    tableBody.querySelectorAll('tr').forEach(function (row) {
        var _a, _b;
        if (row.classList.contains('error-row')) {
            row.style.display = ''; // Always show error rows
            visibleRows++;
            return;
        }
        var fileNameCell = row.cells[0];
        if (!fileNameCell)
            return; // Should not happen normally
        var fileName = (_b = (_a = fileNameCell.textContent) === null || _a === void 0 ? void 0 : _a.trim()) !== null && _b !== void 0 ? _b : '';
        if (!requiresFilter || matchingFileNames.has(fileName)) {
            row.style.display = ''; // Show row
            visibleRows++;
        }
        else {
            row.style.display = 'none'; // Hide row
        }
    });
    console.log("Search complete. Showing ".concat(visibleRows, " file(s) (including errors)."));
}
// --- Initialization Function ---
function initializeAnalysisTab() {
    if (analysisTabInitialized)
        return;
    console.log("Initializing Analysis Tab...");
    var elements = getAnalysisElements();
    // Check for essential elements
    if (!elements.loadTrxButton || !elements.trxFileLoader || !elements.resultsTable || !elements.resultsTableBody || !elements.topFailsListContainer || !elements.loadStatusSpan || !elements.visualizeButton || !elements.searchButton) {
        console.error("Analysis tab essential elements not all found.");
        return; // Stop initialization if critical elements are missing
    }
    // --- Add Event Listeners ---
    elements.loadTrxButton.addEventListener('click', function () { var _a; return (_a = elements.trxFileLoader) === null || _a === void 0 ? void 0 : _a.click(); });
    elements.trxFileLoader.addEventListener('change', function (e) { return handleTrxFileLoad(e, elements); });
    elements.searchButton.addEventListener('click', function () { return handleSearchFails(elements); });
    // Add sorting listener to table header
    var tableHead = elements.resultsTable.querySelector('thead');
    if (tableHead) {
        tableHead.addEventListener('click', function (e) { return handleSortTable(e, elements); });
    }
    else {
        console.error("Could not find table header (thead) for sorting.");
    }
    // Add delegated listener for fail count clicks
    elements.topFailsListContainer.addEventListener('click', function (e) { return handleFailCountClick(e, elements); });
    // Visualization Button Listener
    elements.visualizeButton.disabled = true; // Disable initially
    elements.visualizeButton.addEventListener('click', function () { return (0, visualization_1.openVisualizationModal)(allTrxData); });
    // Initialize controls for the modal (close button, backdrop click)
    (0, visualization_1.initializeVisualizationModalControls)();
    analysisTabInitialized = true;
    console.log("Analysis Tab Initialized.");
}
//# sourceMappingURL=analysis.js.map