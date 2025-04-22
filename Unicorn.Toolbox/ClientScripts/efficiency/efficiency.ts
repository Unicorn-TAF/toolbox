// ClientScripts/efficiency/efficiency.ts

// Import the TYPE from the module, can keep the name RoiCalculator here
import type { RoiCalculator as RoiCalculatorType, RoiForecast, RoiEntry } from './roi-calculator.js';
import type { RoiInputs } from '../models/roi.js'; // Assuming models/roi.ts also changed to roi.js
import { formatNumberForDisplay } from '../utils/formatters.js'; // Assuming utils/formatters.ts changed to formatters.js

// --- Import ONLY Types for Chart.js/Annotation ---
import type { Chart as ChartJS, ChartConfiguration, TooltipItem, Point } from 'chart.js';
import type { AnnotationOptions, LineAnnotationOptions, PointAnnotationOptions, LabelAnnotationOptions } from 'chartjs-plugin-annotation';

// --- Declare Global Chart Variable ---
declare var Chart: typeof ChartJS;

// --- Type alias for annotations ---
type ChartAnnotations = {
    [key: string]: AnnotationOptions;
};

// --- Module State ---
let efficiencyTabInitialized: boolean = false; // <-- ADD flag HERE
let roiChart: ChartJS | null = null;
let efficiencyChart: ChartJS | null = null;
let DynamicallyLoadedRoiCalculator: any;

// --- DOM Element Fetching (Type-safe) ---
function getEfficiencyElements() {
    // ... (Remains the same) ...
    const section = document.getElementById('efficiency');
    if (!section) return null;

    const getInputElement = (id: string): HTMLInputElement | null => section.querySelector<HTMLInputElement>(`#${id}`);
    const getCanvasElement = (id: string): HTMLCanvasElement | null => section.querySelector<HTMLCanvasElement>(`#${id}`);
    const getElement = <T extends HTMLElement>(id: string): T | null => section.querySelector<T>(`#${id}`);

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
        makeForecastButton: getElement<HTMLButtonElement>('makeForecastButton'),
        roiResultDisplay: getElement<HTMLDivElement>('roiResult'),
        roiWeekSpan: getElement<HTMLElement>('roiWeek'),
        roiChartCanvas: getCanvasElement('roiChartCanvas'),
        efficiencyChartCanvas: getCanvasElement('efficiencyChartCanvas'),
        roiTableBody: getElement<HTMLTableSectionElement>('roiTableBody')
    };
}

// --- Input Reading ---
function getRoiInputs(elements: ReturnType<typeof getEfficiencyElements>): RoiInputs | null {
    // ... (Remains the same) ...
    if (!elements) return null;

    const parseOrDefault = (element: HTMLInputElement | null, defaultValue: number = 0): number => {
        if (element && element.value === "0") return 0;
        const value = parseFloat(element?.value ?? '');
        return isNaN(value) || value < 0 ? defaultValue : value;
    };

    const teamSize = parseOrDefault(elements.teamSizeInput, 0);
    const predictionLength = parseOrDefault(elements.predictionLengthInput, 0);
    const manualTime = parseOrDefault(elements.manualTestTimeInput, 0);

    if (teamSize <= 0 || predictionLength <= 0 || manualTime <= 0) {
        console.warn("Invalid inputs: Team size, prediction length, and manual test time must be positive.");
        if (elements.roiResultDisplay) {
            const strongTag = elements.roiResultDisplay.querySelector('strong');
            elements.roiResultDisplay.innerHTML = `<span style="color: red;">Invalid inputs (Team Size, Prediction Length, Manual Test Time must be > 0).</span>`;
            if (strongTag) elements.roiResultDisplay.appendChild(strongTag);
        }
        return null;
    }
    const currentRoiWeekSpan = document.getElementById('roiWeek');
    if (elements.roiResultDisplay && currentRoiWeekSpan) {
        elements.roiResultDisplay.textContent = 'Test automation will self-sustain by ';
        elements.roiResultDisplay.appendChild(currentRoiWeekSpan);
        currentRoiWeekSpan.textContent = '--';
        currentRoiWeekSpan.style.color = '';
        elements.roiResultDisplay.append(' week');
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
        TeamCount: teamSize,
        PeriodsCount: predictionLength
    };
}

// --- UI Update Functions ---
function updateRoiTable(tableBody: HTMLTableSectionElement | null, forecastData: RoiForecast | null): void {
    // ... (Remains the same) ...
    if (!tableBody) return;
    tableBody.innerHTML = '';

    if (!forecastData || !forecastData.series || forecastData.series.length === 0) {
        const row = tableBody.insertRow();
        const cell = row.insertCell();
        cell.colSpan = 8;
        cell.textContent = 'No forecast data generated or invalid inputs.';
        cell.style.textAlign = 'center';
        cell.style.fontStyle = 'italic';
        return;
    }

    forecastData.series.forEach((entry, index) => {
        const weekNumber = index + 1;
        const row = tableBody.insertRow();
        row.insertCell().textContent = weekNumber.toString();
        row.insertCell().textContent = formatNumberForDisplay(entry.TotalTests, 0);
        row.insertCell().textContent = formatNumberForDisplay(entry.TotalRuns, 0);
        row.insertCell().textContent = formatNumberForDisplay(entry.HrsToWriteTests, 1);
        row.insertCell().textContent = formatNumberForDisplay(entry.MtManDaysSavedPerRun, 2);
        row.insertCell().textContent = formatNumberForDisplay(entry.MtManDaysSavedTotal, 1);
        row.insertCell().textContent = formatNumberForDisplay(entry.AtManDaysTotal, 1);
        const roiText = isFinite(entry.ROI) ? formatNumberForDisplay(entry.ROI, 1) + "%" : "∞ %";
        row.insertCell().textContent = roiText;
    });
}

function updateCharts(
    roiCanvas: HTMLCanvasElement | null,
    efficiencyCanvas: HTMLCanvasElement | null,
    forecastData: RoiForecast | null
): void {
    // ... (Chart update logic remains the same, using global Chart) ...
    // Destroy existing charts first
    if (roiChart) { roiChart.destroy(); roiChart = null; }
    if (efficiencyChart) { efficiencyChart.destroy(); efficiencyChart = null; }

    const clearCanvas = (canvas: HTMLCanvasElement | null) => {
        const ctx = canvas?.getContext('2d');
        if (ctx && canvas) { ctx.clearRect(0, 0, canvas.width, canvas.height); }
    }

    if (!forecastData || !forecastData.series || forecastData.series.length === 0 || !roiCanvas || !efficiencyCanvas) {
        clearCanvas(roiCanvas);
        clearCanvas(efficiencyCanvas);
        console.warn("No data or canvas elements to update charts.");
        return;
    }

    const labels = forecastData.series.map((_, index) => `Week ${index + 1}`);
    const roiValues = forecastData.series.map(entry => isFinite(entry.ROI) ? entry.ROI : null);
    const mtSavedValues = forecastData.series.map(entry => entry.MtManDaysSavedTotal);
    const atSpentValues = forecastData.series.map(entry => entry.AtManDaysTotal);
    const zeroReachArrayIndex = forecastData.zeroReachIndex > 0 ? forecastData.zeroReachIndex - 1 : -1;

    // Build ROI Annotations Object
    const roiAnnotations: ChartAnnotations = {
        zeroLine: {
            type: 'line',
            yMin: 0, yMax: 0, borderColor: 'rgb(75, 192, 192)', borderWidth: 1, borderDash: [5, 5],
            label: { content: 'Break-even (ROI = 0%)', position: 'start', font: { size: 10 }, backgroundColor: 'rgba(255, 255, 255, 0.5)' }
        }
    };
    if (zeroReachArrayIndex !== -1 && zeroReachArrayIndex < labels.length) {
        const roiAtBreakEven = forecastData.series[zeroReachArrayIndex].ROI;
        roiAnnotations.roiPositiveMarker = {
            type: 'point', xValue: labels[zeroReachArrayIndex], yValue: isFinite(roiAtBreakEven) ? roiAtBreakEven : 0,
            backgroundColor: 'rgba(92, 184, 92, 0.8)', radius: 5, borderColor: 'rgb(76, 174, 76)', borderWidth: 1
        };
        roiAnnotations.roiPositiveLabel = {
            type: 'label', xValue: labels[zeroReachArrayIndex], yValue: isFinite(roiAtBreakEven) ? roiAtBreakEven : 0,
            content: [`ROI ≥ 0`, `Week ${zeroReachArrayIndex + 1}`], font: { size: 10 }, yAdjust: -15
        };
    }

    // ROI Chart Config
    const roiCtx = roiCanvas.getContext('2d');
    if (roiCtx) {
        const roiConfig: ChartConfiguration<'line', (number | Point | null)[], string> = {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'ROI (%)', data: roiValues, borderColor: 'rgb(217, 83, 79)', backgroundColor: 'rgba(217, 83, 79, 0.1)',
                    tension: 0.1, fill: false, pointRadius: 1, pointHitRadius: 5, borderWidth: 1.5
                }]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                scales: { y: { beginAtZero: false, title: { display: true, text: 'ROI (%)' } }, x: { title: { display: true, text: 'Week' } } },
                plugins: {
                    tooltip: { mode: 'index', intersect: false, callbacks: { label: (context: TooltipItem<'line'>) => `ROI: ${context.parsed.y !== null ? context.parsed.y.toFixed(1) + '%' : 'N/A'}` } },
                    legend: { display: false },
                    annotation: { annotations: roiAnnotations }
                }
            }
        };
        roiChart = new Chart(roiCtx, roiConfig); // Use global Chart
    }

    // Build Efficiency Annotations Object
    const efficiencyAnnotations: ChartAnnotations = {};
    if (zeroReachArrayIndex !== -1 && zeroReachArrayIndex < labels.length) {
        const yValueCrossover = (mtSavedValues[zeroReachArrayIndex] + atSpentValues[zeroReachArrayIndex]) / 2;
        efficiencyAnnotations.crossoverMarker = {
            type: 'point', xValue: labels[zeroReachArrayIndex], yValue: yValueCrossover,
            backgroundColor: 'rgba(240, 173, 78, 0.8)', radius: 5, borderColor: 'rgb(236, 151, 31)', borderWidth: 1
        };
        efficiencyAnnotations.crossoverLabel = {
            type: 'label', xValue: labels[zeroReachArrayIndex], yValue: yValueCrossover,
            content: [`Break-even`, `(Saved ≥ Spent)`, `Week ${zeroReachArrayIndex + 1}`], font: { size: 10 },
            yAdjust: 15, backgroundColor: 'rgba(255, 255, 255, 0.6)'
        };
    }

    // Efficiency Chart Config
    const efficiencyCtx = efficiencyCanvas.getContext('2d');
    if (efficiencyCtx) {
        const efficiencyConfig: ChartConfiguration<'line', number[], string> = {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { label: 'MT Saved (Total Days)', data: mtSavedValues, borderColor: 'rgb(92, 184, 92)', backgroundColor: 'rgba(92, 184, 92, 0.1)', tension: 0.1, fill: true, pointRadius: 1, pointHitRadius: 5, borderWidth: 1.5 },
                    { label: 'AT Spent (Total Days)', data: atSpentValues, borderColor: 'rgb(2, 117, 216)', backgroundColor: 'rgba(2, 117, 216, 0.1)', tension: 0.1, fill: true, pointRadius: 1, pointHitRadius: 5, borderWidth: 1.5 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                scales: { y: { beginAtZero: true, title: { display: true, text: 'Cumulative Man-Days' }, stacked: false }, x: { title: { display: true, text: 'Week' } } },
                plugins: {
                    tooltip: { mode: 'index', intersect: false, callbacks: { label: (context: TooltipItem<'line'>) => `${context.dataset.label}: ${context.parsed.y.toFixed(1)} days` } },
                    legend: { display: true, position: 'bottom' },
                    annotation: { annotations: efficiencyAnnotations }
                }
            }
        };
        efficiencyChart = new Chart(efficiencyCtx, efficiencyConfig); // Use global Chart
    }
}


// --- Main Forecast Execution ---
function runForecastAndUpdateUI(elements: ReturnType<typeof getEfficiencyElements>): void {
    console.log("Running forecast...");
    if (!elements) {
        console.error("Cannot run forecast: Elements not found.");
        return;
    }

    const inputs = getRoiInputs(elements);

    if (!inputs) {
        updateRoiTable(elements.roiTableBody, null);
        updateCharts(elements.roiChartCanvas, elements.efficiencyChartCanvas, null);
        return;
    }

    // Ensure the calculator class has been loaded before using it
    if (!DynamicallyLoadedRoiCalculator) {
        console.error("ROI Calculator class has not been loaded yet.");
        // Optionally display an error message to the user
        return;
    }

    // Use the RENAMED module-scoped variable to instantiate
    const calculator = new DynamicallyLoadedRoiCalculator(inputs);
    const forecast = calculator.calculateForecast();
    console.log("Forecast calculated:", forecast);

    // ... (Rest of UI update remains the same) ...
    const currentRoiWeekSpan = document.getElementById('roiWeek');
    if (currentRoiWeekSpan) {
        if (forecast.zeroReachIndex > 0) {
            currentRoiWeekSpan.textContent = forecast.zeroReachIndex.toString();
            currentRoiWeekSpan.style.color = '#5cb85c';
        } else {
            currentRoiWeekSpan.textContent = 'Never';
            currentRoiWeekSpan.style.color = '#d9534f';
        }
    }
    updateRoiTable(elements.roiTableBody, forecast);
    updateCharts(elements.roiChartCanvas, elements.efficiencyChartCanvas, forecast);
}

// --- Initialization Function ---
export async function initializeEfficiencyTab(): Promise<void> {
    // --- Check if already initialized HERE ---
    if (efficiencyTabInitialized) {
        console.log("Efficiency Tab already initialized. Skipping.");
        return;
    }
    console.log("Initializing AT Efficiency Tab...");

    // Dynamic Import for Calculator
    try {
        const module = await import('./roi-calculator.js');
        DynamicallyLoadedRoiCalculator = module.RoiCalculator;
        if (!DynamicallyLoadedRoiCalculator) throw new Error("RoiCalculator class not found");
        console.log("ROI Calculator loaded successfully.");
    } catch (error) {
        console.error("Failed to load roi-calculator.js:", error);
        const resultsDiv = document.getElementById('efficiency-results');
        if (resultsDiv) resultsDiv.innerHTML = '<p style="color: red; text-align: center; padding: 20px;">Error loading calc module.</p>';
        return; // Stop initialization if calculator fails
    }

    const elements = getEfficiencyElements();
    console.log("Efficiency tab elements queried:", elements);
    if (!elements?.makeForecastButton || !elements.roiChartCanvas || !elements.efficiencyChartCanvas || !elements.roiTableBody) {
        console.error("Essential elements for AT Efficiency tab are missing. Check IDs.", {
            makeForecastButton: !!elements?.makeForecastButton,
            roiChartCanvas: !!elements?.roiChartCanvas,
            efficiencyChartCanvas: !!elements?.efficiencyChartCanvas,
            roiTableBody: !!elements?.roiTableBody
        });
        const efficiencySection = document.getElementById('efficiency');
        if (efficiencySection) efficiencySection.innerHTML = '<p style="color: red; padding: 20px;">Error initializing tab elements.</p>';
        return; // Stop initialization if elements are missing
    }

    console.log("Adding forecast button listener.");
    elements.makeForecastButton.addEventListener('click', () => runForecastAndUpdateUI(elements));

    // --- Mark as initialized at the END ---
    efficiencyTabInitialized = true;
    console.log("AT Efficiency Tab Initialized successfully.");
}