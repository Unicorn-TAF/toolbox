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
exports.initializeEfficiencyTab = initializeEfficiencyTab;
var roi_calculator_1 = require("./roi-calculator");
var formatters_1 = require("../utils/formatters");
var auto_1 = require("chart.js/auto");
// Import necessary types directly
var chartjs_plugin_annotation_1 = require("chartjs-plugin-annotation");
// Register the annotation plugin globally for Chart.js
auto_1.Chart.register(chartjs_plugin_annotation_1.default);
// --- Module State ---
var efficiencyTabInitialized = false;
var roiChart = null;
var efficiencyChart = null;
// --- DOM Element Fetching (Type-safe) ---
function getEfficiencyElements() {
    var section = document.getElementById('efficiency');
    if (!section)
        return null;
    var getInputElement = function (id) { return section.querySelector("#".concat(id)); };
    var getCanvasElement = function (id) { return section.querySelector("#".concat(id)); };
    var getElement = function (id) { return section.querySelector("#".concat(id)); };
    return {
        manualTestTimeInput: getInputElement('manualTestTime'),
        failInvestigationTimeInput: getInputElement('failInvestigationTime'),
        frameworkCreationTimeInput: getInputElement('frameworkCreationTime'),
        testCreationTimeInput: getInputElement('testCreationTime'),
        testFixTimeInput: getInputElement('testFixTime'),
        atFailPercentInput: getInputElement('atFailPercent'),
        devFailPercentInput: getInputElement('devFailPercent'),
        extraHrsPerDayInput: getInputElement('extraHrsPerDay'),
        buildsPerWeekInput: getInputElement('buildsPerWeek'),
        teamSizeInput: getInputElement('teamSize'),
        predictionLengthInput: getInputElement('predictionLength'),
        makeForecastButton: getElement('makeForecastButton'),
        roiResultDisplay: getElement('roiResult'),
        roiWeekSpan: getElement('roiWeek'), // Might be strong or span
        roiChartCanvas: getCanvasElement('roiChartCanvas'),
        efficiencyChartCanvas: getCanvasElement('efficiencyChartCanvas'),
        roiTableBody: getElement('#roiTableBody')
    };
}
// --- Input Reading ---
function getRoiInputs(elements) {
    if (!elements)
        return null;
    var parseOrDefault = function (element, defaultValue) {
        var _a;
        if (defaultValue === void 0) { defaultValue = 0; }
        if (element && element.value === "0")
            return 0;
        var value = parseFloat((_a = element === null || element === void 0 ? void 0 : element.value) !== null && _a !== void 0 ? _a : '');
        return isNaN(value) || value < 0 ? defaultValue : value;
    };
    // Basic validation check
    var teamSize = parseOrDefault(elements.teamSizeInput, 0);
    var predictionLength = parseOrDefault(elements.predictionLengthInput, 0);
    var manualTime = parseOrDefault(elements.manualTestTimeInput, 0);
    if (teamSize <= 0 || predictionLength <= 0 || manualTime <= 0) {
        console.warn("Invalid inputs: Team size, prediction length, and manual test time must be positive.");
        if (elements.roiResultDisplay) {
            var strongTag = elements.roiResultDisplay.querySelector('strong');
            elements.roiResultDisplay.innerHTML = "<span style=\"color: red;\">Invalid inputs (Team Size, Prediction Length, Manual Test Time must be > 0).</span>";
            if (strongTag)
                elements.roiResultDisplay.appendChild(strongTag);
        }
        return null; // Indicate invalid inputs
    }
    // Restore default message if inputs are okay now
    var currentRoiWeekSpan = document.getElementById('roiWeek');
    if (elements.roiResultDisplay && currentRoiWeekSpan) {
        elements.roiResultDisplay.textContent = 'Test automation will self-sustain by '; // Set text part first
        elements.roiResultDisplay.appendChild(currentRoiWeekSpan); // Append existing span
        currentRoiWeekSpan.textContent = '--'; // Reset span content
        currentRoiWeekSpan.style.color = ''; // Reset color
        elements.roiResultDisplay.append(' week'); // Add suffix text
    }
    return {
        ManualTestExecutionHrs: parseOrDefault(elements.manualTestTimeInput, 10.0) / 60.0,
        FailAnalyzeHrs: parseOrDefault(elements.failInvestigationTimeInput, 5.0) / 60.0,
        TafCreationHrs: parseOrDefault(elements.frameworkCreationTimeInput, 80.0),
        AutoTestCreationHrs: parseOrDefault(elements.testCreationTimeInput, 8.0),
        AutoTestFixHrs: parseOrDefault(elements.testFixTimeInput, 2.0),
        AtFailsRatio: parseOrDefault(elements.atFailPercentInput, 2.5) / 100.0,
        DevFailsRatio: parseOrDefault(elements.devFailPercentInput, 2.5) / 100.0,
        NonDevHrs: parseOrDefault(elements.extraHrsPerDayInput, 0.5),
        BuildsPerPeriod: parseOrDefault(elements.buildsPerWeekInput, 5),
        TeamCount: teamSize, // Use validated value
        PeriodsCount: predictionLength // Use validated value
    };
}
// --- UI Update Functions ---
function updateRoiTable(tableBody, forecastData) {
    if (!tableBody)
        return;
    tableBody.innerHTML = ''; // Clear previous data
    if (!forecastData || !forecastData.series || forecastData.series.length === 0) {
        var row = tableBody.insertRow();
        var cell = row.insertCell();
        cell.colSpan = 8; // Match number of columns
        cell.textContent = 'No forecast data generated or invalid inputs.';
        cell.style.textAlign = 'center';
        cell.style.fontStyle = 'italic';
        return;
    }
    forecastData.series.forEach(function (entry, index) {
        var weekNumber = index + 1; // Display 1-based week number
        var row = tableBody.insertRow();
        row.insertCell().textContent = weekNumber.toString();
        row.insertCell().textContent = (0, formatters_1.formatNumberForDisplay)(entry.TotalTests, 0);
        row.insertCell().textContent = (0, formatters_1.formatNumberForDisplay)(entry.TotalRuns, 0);
        row.insertCell().textContent = (0, formatters_1.formatNumberForDisplay)(entry.HrsToWriteTests, 1);
        row.insertCell().textContent = (0, formatters_1.formatNumberForDisplay)(entry.MtManDaysSavedPerRun, 2);
        row.insertCell().textContent = (0, formatters_1.formatNumberForDisplay)(entry.MtManDaysSavedTotal, 1);
        row.insertCell().textContent = (0, formatters_1.formatNumberForDisplay)(entry.AtManDaysTotal, 1);
        var roiText = isFinite(entry.ROI) ? (0, formatters_1.formatNumberForDisplay)(entry.ROI, 1) + "%" : "∞ %";
        row.insertCell().textContent = roiText;
    });
}
function updateCharts(roiCanvas, efficiencyCanvas, forecastData) {
    // Destroy existing charts first
    if (roiChart) {
        roiChart.destroy();
        roiChart = null;
    }
    if (efficiencyChart) {
        efficiencyChart.destroy();
        efficiencyChart = null;
    }
    var clearCanvas = function (canvas) {
        var ctx = canvas === null || canvas === void 0 ? void 0 : canvas.getContext('2d');
        if (ctx && canvas) {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
        }
    };
    if (!forecastData || !forecastData.series || forecastData.series.length === 0 || !roiCanvas || !efficiencyCanvas) {
        clearCanvas(roiCanvas);
        clearCanvas(efficiencyCanvas);
        console.warn("No data or canvas elements to update charts.");
        return;
    }
    var labels = forecastData.series.map(function (_, index) { return "Week ".concat(index + 1); });
    var roiValues = forecastData.series.map(function (entry) { return isFinite(entry.ROI) ? entry.ROI : null; });
    var mtSavedValues = forecastData.series.map(function (entry) { return entry.MtManDaysSavedTotal; });
    var atSpentValues = forecastData.series.map(function (entry) { return entry.AtManDaysTotal; });
    var zeroReachArrayIndex = forecastData.zeroReachIndex > 0 ? forecastData.zeroReachIndex - 1 : -1;
    // --- Build ROI Annotations Object ---
    var roiAnnotations = {
        // Base annotation (always present)
        zeroLine: {
            type: 'line', // Set type explicitly
            yMin: 0,
            yMax: 0,
            borderColor: 'rgb(75, 192, 192)',
            borderWidth: 1,
            borderDash: [5, 5],
            label: {
                content: 'Break-even (ROI = 0%)',
                position: 'start',
                font: { size: 10 },
                backgroundColor: 'rgba(255, 255, 255, 0.5)'
            }
        }
        // We don't use 'satisfies' here on the whole object literal
    };
    // Conditionally add point and label annotations
    if (zeroReachArrayIndex !== -1 && zeroReachArrayIndex < labels.length) {
        var roiAtBreakEven = forecastData.series[zeroReachArrayIndex].ROI;
        roiAnnotations.roiPositiveMarker = {
            type: 'point', // Set type explicitly
            xValue: labels[zeroReachArrayIndex],
            yValue: isFinite(roiAtBreakEven) ? roiAtBreakEven : 0,
            backgroundColor: 'rgba(92, 184, 92, 0.8)',
            radius: 5,
            borderColor: 'rgb(76, 174, 76)',
            borderWidth: 1
        };
        roiAnnotations.roiPositiveLabel = {
            type: 'label', // Set type explicitly
            xValue: labels[zeroReachArrayIndex],
            yValue: isFinite(roiAtBreakEven) ? roiAtBreakEven : 0,
            content: ["ROI \u2265 0", "Week ".concat(zeroReachArrayIndex + 1)],
            font: { size: 10 },
            yAdjust: -15
        };
    }
    // --- ROI Chart ---
    var roiCtx = roiCanvas === null || roiCanvas === void 0 ? void 0 : roiCanvas.getContext('2d');
    if (roiCtx) {
        var roiConfig = {
            type: 'line',
            data: {
                labels: labels,
                datasets: [ /* ... */]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                scales: { /* ... */},
                plugins: {
                    tooltip: { /* ... */},
                    legend: { /* ... */},
                    annotation: {
                        annotations: roiAnnotations // Pass the fully constructed object
                    }
                }
            }
        };
        roiChart = new auto_1.Chart(roiCtx, roiConfig);
    }
    // --- Build Efficiency Annotations Object ---
    var efficiencyAnnotations = {}; // Start empty
    // Conditionally add point and label annotations
    if (zeroReachArrayIndex !== -1 && zeroReachArrayIndex < labels.length) {
        var yValueCrossover = (mtSavedValues[zeroReachArrayIndex] + atSpentValues[zeroReachArrayIndex]) / 2;
        efficiencyAnnotations.crossoverMarker = {
            type: 'point', // Set type explicitly
            xValue: labels[zeroReachArrayIndex],
            yValue: yValueCrossover,
            backgroundColor: 'rgba(240, 173, 78, 0.8)',
            radius: 5, borderColor: 'rgb(236, 151, 31)', borderWidth: 1
        };
        efficiencyAnnotations.crossoverLabel = {
            type: 'label', // Set type explicitly
            xValue: labels[zeroReachArrayIndex],
            yValue: yValueCrossover,
            content: ["Break-even", "(Saved \u2265 Spent)", "Week ".concat(zeroReachArrayIndex + 1)],
            font: { size: 10 },
            yAdjust: 15, backgroundColor: 'rgba(255, 255, 255, 0.6)'
        };
    }
    // --- Efficiency Chart ---
    var efficiencyCtx = efficiencyCanvas === null || efficiencyCanvas === void 0 ? void 0 : efficiencyCanvas.getContext('2d');
    if (efficiencyCtx) {
        var efficiencyConfig = {
            type: 'line',
            data: {
                labels: labels,
                datasets: [ /* ... */]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                scales: { /* ... */},
                plugins: {
                    tooltip: { /* ... */},
                    legend: { /* ... */},
                    annotation: {
                        annotations: efficiencyAnnotations // Pass the fully constructed object
                    }
                }
            }
        };
        efficiencyChart = new auto_1.Chart(efficiencyCtx, efficiencyConfig);
    }
}
// --- Main Forecast Execution ---
function runForecastAndUpdateUI(elements) {
    console.log("Running forecast...");
    if (!elements) {
        console.error("Cannot run forecast: Elements not found.");
        return;
    }
    var inputs = getRoiInputs(elements);
    if (!inputs) {
        updateRoiTable(elements.roiTableBody, null);
        updateCharts(elements.roiChartCanvas, elements.efficiencyChartCanvas, null);
        return;
    }
    var calculator = new roi_calculator_1.RoiCalculator(inputs);
    var forecast = calculator.calculateForecast();
    console.log("Forecast calculated:", forecast);
    // Update ROI Result text
    var currentRoiWeekSpan = document.getElementById('roiWeek');
    if (currentRoiWeekSpan) {
        if (forecast.zeroReachIndex > 0) {
            currentRoiWeekSpan.textContent = forecast.zeroReachIndex.toString();
            currentRoiWeekSpan.style.color = '#5cb85c';
        }
        else {
            currentRoiWeekSpan.textContent = 'Never';
            currentRoiWeekSpan.style.color = '#d9534f';
        }
    }
    // Update Table and Charts
    updateRoiTable(elements.roiTableBody, forecast);
    updateCharts(elements.roiChartCanvas, elements.efficiencyChartCanvas, forecast);
}
// --- Initialization Function ---
function initializeEfficiencyTab() {
    return __awaiter(this, void 0, void 0, function () {
        var elements, efficiencySection;
        return __generator(this, function (_a) {
            if (efficiencyTabInitialized)
                return [2 /*return*/];
            console.log("Initializing AT Efficiency Tab...");
            elements = getEfficiencyElements();
            if (!(elements === null || elements === void 0 ? void 0 : elements.makeForecastButton) || !elements.roiChartCanvas || !elements.efficiencyChartCanvas || !elements.roiTableBody) {
                console.error("Essential elements for AT Efficiency tab are missing.");
                efficiencySection = document.getElementById('efficiency');
                if (efficiencySection) {
                    efficiencySection.innerHTML = '<p style="color: red; padding: 20px;">Error: Could not initialize Efficiency Tab. Required HTML elements are missing.</p>';
                }
                return [2 /*return*/]; // Stop initialization
            }
            // Add Event Listener
            elements.makeForecastButton.addEventListener('click', function () { return runForecastAndUpdateUI(elements); });
            // Optional: Run initial forecast on load? Decide if needed.
            // runForecastAndUpdateUI(elements);
            efficiencyTabInitialized = true;
            console.log("AT Efficiency Tab Initialized.");
            return [2 /*return*/];
        });
    });
}
//# sourceMappingURL=efficiency.js.map