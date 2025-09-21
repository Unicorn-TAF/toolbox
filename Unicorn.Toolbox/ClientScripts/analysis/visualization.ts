import { TrxFileData, TestData } from '../models/trx.js';
import { formatDateTime, formatTime, formatDuration } from '../utils/formatters.js';
import { debounce } from '../utils/debounce.js'; // Assuming you have/create a debounce utility

// --- Module Constants ---
const VIS_PALETTE: ReadonlyArray<string> = ['#1CCEED', '#1C4AED', '#1C8CED', '#1CEDC7', '#311CED', '#419EF0', '#44676E', '#8A1CED', '#1CE5ED', '#1CADEF', '#87CEEB', '#3CB371', '#FFD700', '#FFA07A', '#9370DB'];
const VIS_FAIL_COLOR: string = '#F04141';
const MIN_TEST_WIDTH_PX: number = 2;
const RESIZE_DEBOUNCE_MS = 150; // Debounce time for resize handling

// --- Module State ---
const suiteColorMap: Map<string, number> = new Map(); // Maps suiteId to color index
let nextSuiteColorIndex: number = 0;
let isOverSquare: boolean = false;

// --- Cache State ---
interface ProcessedVisStructure {
    rows: HTMLDivElement[]; // Rows containing unpositioned squares/underlines
    overallMinTime: number;
    overallMaxTime: number;
    sourceDataRef: TrxFileData[]; // Reference to the data used for this cache
}
let cachedVisStructure: ProcessedVisStructure | null = null;

// --- Listener Management ---
type MouseMoveHandler = (event: MouseEvent) => void;
type ClickHandler = (event: MouseEvent) => void;
type MouseOutHandler = (event: MouseEvent) => void;
type MouseOverHandler = (event: MouseEvent) => void;

let timelineMouseMoveListener: MouseMoveHandler | null = null;
let timelineClickListener: ClickHandler | null = null;
let timelineMouseOutListener: MouseOutHandler | null = null;
let squareMouseOverListener: MouseOverHandler | null = null;
let squareMouseOutListener: MouseOutHandler | null = null;

// --- Resize Observer ---
let resizeObserver: ResizeObserver | null = null;
let debouncedLayout: (() => void) | null = null;


// --- DOM Element Fetching (Type-safe) ---
interface VisualizationElements {
    modal: HTMLDivElement | null;
    modalCloseButton: HTMLSpanElement | null;
    containerWrapper: HTMLDivElement | null;
    container: HTMLDivElement | null;
    axisHeader: HTMLDivElement | null;
    tooltip: HTMLDivElement | null;
    clickInfo: HTMLDivElement | null;
    cursorLine: HTMLDivElement | null;
    clickMarker: HTMLDivElement | null;
}

function getVisualizationElements(): VisualizationElements {
    const modal = document.getElementById('visualizationModal') as HTMLDivElement | null;
    return {
        modal: modal,
        modalCloseButton: modal?.querySelector('.modal-close-button') as HTMLSpanElement | null,
        containerWrapper: document.getElementById('timelineContainerWrapper') as HTMLDivElement | null,
        container: document.getElementById('timelineContainer') as HTMLDivElement | null,
        axisHeader: document.getElementById('timelineAxisHeader') as HTMLDivElement | null,
        tooltip: document.getElementById('timelineTooltip') as HTMLDivElement | null,
        clickInfo: document.getElementById('timelineClickInfo') as HTMLDivElement | null,
        cursorLine: document.getElementById('timelineCursorLine') as HTMLDivElement | null,
        clickMarker: document.getElementById('timelineClickMarkerLine') as HTMLDivElement | null,
    };
}

// --- Helper Functions (getSuiteColorClass, createTimeLabel, positionTooltip remain the same) ---
function getSuiteColorClass(suiteId: string | null): string {
    const safeSuiteId = suiteId ?? `__unknown_suite_${suiteColorMap.size}`;
    if (!suiteColorMap.has(safeSuiteId)) {
        suiteColorMap.set(safeSuiteId, nextSuiteColorIndex);
        nextSuiteColorIndex++;
    }
    const colorIndex = (suiteColorMap.get(safeSuiteId) ?? 0) % VIS_PALETTE.length;
    return `color-${colorIndex}`;
}

function createTimeLabel(text: string, percent: number, transform: string = 'translateX(-50%)'): HTMLSpanElement {
    const label = document.createElement('span');
    label.className = 'time-label';
    label.textContent = text;
    label.style.left = `${percent}%`;
    label.style.transform = transform;
    return label;
}

function positionTooltip(tooltip: HTMLDivElement, event: MouseEvent): void {
    const offsetX = 15;
    const offsetY = 15;
    let x = event.clientX + offsetX;
    let y = event.clientY + offsetY;
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    tooltip.style.visibility = 'hidden';
    tooltip.style.display = 'block';
    const ttRect = tooltip.getBoundingClientRect();
    tooltip.style.display = 'none';
    tooltip.style.visibility = 'visible';
    if (x + ttRect.width > vw) x = event.clientX - ttRect.width - offsetX;
    if (y + ttRect.height > vh) y = event.clientY - ttRect.height - offsetY;
    x = Math.max(offsetX, x);
    y = Math.max(offsetY, y);
    tooltip.style.left = `${x}px`;
    tooltip.style.top = `${y}px`;
}


// --- Visualization Generation Logic ---

/**
 * Processes the raw data and creates the basic, unpositioned DOM structure.
 * This result is cached.
 */
function processVisualizationData(visData: TrxFileData[]): ProcessedVisStructure | null {
    console.log("Processing visualization data...");
    suiteColorMap.clear(); // Reset suite colors for each processing run
    nextSuiteColorIndex = 0;

    let overallMinTime = Infinity;
    let overallMaxTime = -Infinity;

    visData.forEach(trx => {
        const runStart = trx.runStartTime?.getTime();
        const runEnd = trx.runEndTime?.getTime();
        if (runStart && runStart < overallMinTime) overallMinTime = runStart;
        if (runEnd && runEnd > overallMaxTime) overallMaxTime = runEnd;
        trx.tests.forEach(test => {
            const testStart = test.startTime?.getTime();
            const testEnd = test.endTime?.getTime();
            if (testStart && testStart < overallMinTime) overallMinTime = testStart;
            if (testEnd && testEnd > overallMaxTime) overallMaxTime = testEnd;
        });
    });

    if (overallMinTime === Infinity || overallMaxTime === -Infinity || overallMinTime >= overallMaxTime) {
        console.error("Could not determine valid time range for processing.", { min: overallMinTime, max: overallMaxTime });
        return null;
    }

    const processedRows: HTMLDivElement[] = [];
    const rowHeight = 25; // Keep fixed row height definition

    visData.forEach((trx) => {
        const rowDiv = document.createElement('div');
        rowDiv.className = 'timeline-row';
        // DO NOT set top style here, layoutVisualization will handle vertical positioning if needed,
        // or CSS can handle it based on order. For now, order is enough.
        rowDiv.dataset.fileName = trx.fileName;

        let currentSuiteId: string | null = null;
        let currentSuiteStartTimeMs = -1; // Track start time for underline calc
        let currentSuiteEndTimeMs = -1; // Track end time for underline calc


        const testsToVisualize = trx.tests
            .filter(t => t.startTime && t.endTime && t.startTime < t.endTime)
            .sort((a, b) => (a.startTime!.getTime()) - (b.startTime!.getTime()));

        testsToVisualize.forEach((test) => {
            const testStartTimeMs = test.startTime!.getTime();
            const testEndTimeMs = test.endTime!.getTime();
            const durationMs = testEndTimeMs - testStartTimeMs;

            if (durationMs <= 0) return; // Skip invalid duration tests

            const suiteId = test.testListId;
            const suiteName = test.testListId ? (trx.testListMap.get(test.testListId) || `Suite ${test.testListId}`) : 'Unknown Suite';

            // --- Suite Underline Tracking ---
            if (suiteId !== currentSuiteId) {
                // Finish previous suite's underline
                if (currentSuiteId !== null && currentSuiteStartTimeMs >= 0 && currentSuiteEndTimeMs > currentSuiteStartTimeMs) {
                    // Create underline element but don't position yet
                    const underline = createSuiteUnderlineElement(currentSuiteStartTimeMs, currentSuiteEndTimeMs, currentSuiteId, rowHeight);
                    if (underline) rowDiv.appendChild(underline);
                }
                // Start new suite tracking
                currentSuiteId = suiteId;
                currentSuiteStartTimeMs = testStartTimeMs;
                currentSuiteEndTimeMs = testEndTimeMs;
            } else {
                // Extend the end time for the current suite
                currentSuiteEndTimeMs = Math.max(currentSuiteEndTimeMs, testEndTimeMs);
                // Also ensure start time is the minimum (handles out-of-order tests within a suite if sorting failed)
                currentSuiteStartTimeMs = Math.min(currentSuiteStartTimeMs, testStartTimeMs);
            }


            // --- Create Test Square Element (Unpositioned) ---
            const square = document.createElement('div');
            square.className = 'test-square';

            // Store data attributes needed for layout
            square.dataset.testName = test.testName;
            square.dataset.startTimeMs = testStartTimeMs.toString();
            square.dataset.endTimeMs = testEndTimeMs.toString();
            square.dataset.outcome = test.outcome;
            square.dataset.fileName = trx.fileName;
            square.dataset.durationMs = durationMs.toString();
            square.dataset.suiteName = suiteName;
            if (test.outcome === 'Failed' && test.errorMessage) {
                square.dataset.error = test.errorMessage.substring(0, 200) + (test.errorMessage.length > 200 ? '...' : '');
            }

            // Add color class
            square.classList.add(test.outcome === 'Failed' ? 'color-fail' : getSuiteColorClass(suiteId));
            rowDiv.appendChild(square);
        }); // End loop through tests

        // Create underline for the very last suite in the row
        if (currentSuiteStartTimeMs >= 0 && currentSuiteEndTimeMs > currentSuiteStartTimeMs) {
            const underline = createSuiteUnderlineElement(currentSuiteStartTimeMs, currentSuiteEndTimeMs, currentSuiteId, rowHeight);
            if (underline) rowDiv.appendChild(underline);
        }


        processedRows.push(rowDiv);
    }); // End loop through files

    return {
        rows: processedRows,
        overallMinTime,
        overallMaxTime,
        sourceDataRef: visData // Store reference to original data
    };
}

/** Helper to create the underline element */
function createSuiteUnderlineElement(startTimeMs: number, endTimeMs: number, suiteId: string | null, rowHeight: number): HTMLDivElement | null {
    const underlineHeight = 2;
    const underline = document.createElement('div');
    underline.className = `suite-underline`; // Base class
    underline.style.top = `${rowHeight - underlineHeight}px`; // Vertical position is fixed
    underline.style.height = `${underlineHeight}px`;

    // Store times needed for layout
    underline.dataset.startTimeMs = startTimeMs.toString();
    underline.dataset.endTimeMs = endTimeMs.toString();

    const colorClass = getSuiteColorClass(suiteId);
    underline.classList.add(colorClass);

    underline.title = `Suite ID: ${suiteId ?? 'Unknown'}`;
    return underline;
}


/**
 * Takes processed structure and lays it out based on current container width.
 * Draws axis, positions elements, sets height, adds listeners.
 */
function layoutVisualization(
    processedStructure: ProcessedVisStructure,
    elements: VisualizationElements
): void {
    const { container, axisHeader, clickInfo, containerWrapper } = elements;
    const { rows, overallMinTime, overallMaxTime } = processedStructure;

    if (!container || !axisHeader || !clickInfo || !containerWrapper || !rows) {
        console.error("Timeline elements or processed structure missing for layout."); return;
    }
    console.log("Laying out visualization...");

    // --- Clear Existing Layout ---
    container.innerHTML = ''; // Clear previous squares/rows
    axisHeader.innerHTML = ''; // Clear previous axis
    removeTimelineListeners(); // Remove old listeners before adding new ones

    // --- Calculate Dimensions ---
    const containerWidth = containerWrapper.clientWidth;
    const overallDuration = overallMaxTime - overallMinTime;

    if (overallDuration <= 0 || containerWidth <= 0) {
        console.error("Invalid duration or container width for layout.", { duration: overallDuration, width: containerWidth });
        container.innerHTML = '<p class="error-message">Invalid duration or width.</p>'; return;
    }

    // --- Draw Axis ---
    drawTimelineAxis(axisHeader, overallMinTime, overallMaxTime, containerWidth);

    // --- Position Elements and Append Rows ---
    const rowHeight = 25;
    const rowMargin = 2;
    let totalHeight = 0;

    rows.forEach((rowDiv, index) => {
        // Set vertical position (can also be done via CSS flex/grid on container)
        rowDiv.style.top = `${index * (rowHeight + rowMargin)}px`;

        // Position test squares within this row
        rowDiv.querySelectorAll<HTMLDivElement>('.test-square').forEach(square => {
            const startMs = parseFloat(square.dataset.startTimeMs || '0');
            const endMs = parseFloat(square.dataset.endTimeMs || '0');
            const durationMs = endMs - startMs;

            if (startMs >= overallMinTime && durationMs > 0) {
                const relativeStartMs = startMs - overallMinTime;
                const leftPercent = (relativeStartMs / overallDuration) * 100;
                let widthPercent = (durationMs / overallDuration) * 100;

                const minWidthPercent = (MIN_TEST_WIDTH_PX / containerWidth) * 100;
                widthPercent = Math.max(widthPercent, minWidthPercent);
                widthPercent = Math.min(widthPercent, 100 - leftPercent); // Prevent overflow

                if (widthPercent > 0 && leftPercent >= 0 && leftPercent <= 100) {
                    square.style.left = `${leftPercent}%`;
                    square.style.width = `${widthPercent}%`;
                    square.style.display = ''; // Ensure visible
                } else {
                    square.style.display = 'none'; // Hide if invalid position/width
                }
            } else {
                square.style.display = 'none'; // Hide if invalid times
            }
        });

        // Position suite underlines within this row
        rowDiv.querySelectorAll<HTMLDivElement>('.suite-underline').forEach(underline => {
            const startMs = parseFloat(underline.dataset.startTimeMs || '0');
            const endMs = parseFloat(underline.dataset.endTimeMs || '0');
            const durationMs = endMs - startMs;

            if (startMs >= overallMinTime && durationMs > 0) {
                const relativeStartMs = startMs - overallMinTime;
                const leftPercent = (relativeStartMs / overallDuration) * 100;
                let widthPercent = (durationMs / overallDuration) * 100;

                widthPercent = Math.min(widthPercent, 100 - leftPercent); // Prevent overflow

                if (widthPercent > 0 && leftPercent >= 0 && leftPercent <= 100) {
                    underline.style.left = `${leftPercent}%`;
                    underline.style.width = `${widthPercent}%`;
                    underline.style.display = ''; // Ensure visible
                } else {
                    underline.style.display = 'none';
                }
            } else {
                underline.style.display = 'none';
            }
        });

        container.appendChild(rowDiv); // Add the fully processed row
        totalHeight += (rowHeight + rowMargin);
    });

    // Set container height
    container.style.minHeight = `${totalHeight}px`;

    // --- Add Interactive Listeners ---
    // Pass current dimensions/times needed by listeners
    addTimelineListeners(elements, overallMinTime, overallDuration, containerWidth, processedStructure.sourceDataRef);
}

// --- drawTimelineAxis (No changes needed) ---
function drawTimelineAxis(axisHeader: HTMLDivElement, minTime: number, maxTime: number, containerWidth: number): void {
    // ... (implementation remains the same)
    axisHeader.innerHTML = '';
    const duration = maxTime - minTime;
    if (duration <= 0 || containerWidth <= 0) return;
    const startTime = new Date(minTime);
    const endTime = new Date(maxTime);
    let tickIntervalMs: number;
    const durationSec = duration / 1000;
    const durationMin = durationSec / 60;
    const durationHr = durationMin / 60;
    const minTickGapPx = 80;
    const maxTicks = Math.max(1, Math.floor(containerWidth / minTickGapPx));
    if (durationHr / maxTicks > 2) tickIntervalMs = 3600 * 1000 * Math.ceil(durationHr / maxTicks);
    else if (durationHr > 2) tickIntervalMs = 3600 * 1000;
    else if (durationMin / maxTicks > 2) tickIntervalMs = 60 * 1000 * Math.ceil(durationMin / maxTicks);
    else if (durationMin > 1) tickIntervalMs = 60 * 1000;
    else if (durationSec / maxTicks > 2) tickIntervalMs = 1000 * Math.ceil(durationSec / maxTicks);
    else tickIntervalMs = 1000;
    const startLabel = createTimeLabel(formatDateTime(startTime), 0, 'translateX(0)');
    startLabel.title = startTime.toISOString();
    axisHeader.appendChild(startLabel);
    const endLabel = createTimeLabel(formatDateTime(endTime), 100, 'translateX(-100%)');
    endLabel.title = endTime.toISOString();
    axisHeader.appendChild(endLabel);
    const firstTickTime = Math.ceil(minTime / tickIntervalMs) * tickIntervalMs;
    for (let tickTime = firstTickTime; tickTime < maxTime; tickTime += tickIntervalMs) {
        const percent = ((tickTime - minTime) / duration) * 100;
        if (percent < 5 || percent > 95) continue;
        const tickDate = new Date(tickTime);
        const labelText = formatTime(tickDate);
        const label = createTimeLabel(labelText, percent);
        label.title = formatDateTime(tickDate);
        axisHeader.appendChild(label);
    }
}

// --- Listener Management (addTimelineListeners, removeTimelineListeners) ---
// Make sure addTimelineListeners receives necessary current context
function addTimelineListeners(
    elements: VisualizationElements,
    overallMinTime: number,
    overallDuration: number,
    containerWidth: number, // Pass current width
    visData: TrxFileData[] // Pass original data reference for click info
): void {
    const { containerWrapper, container, tooltip, clickInfo, cursorLine, clickMarker } = elements;
    if (!tooltip) { console.error("TOOLTIP ELEMENT NOT FOUND!"); return; }
    if (!containerWrapper || !container || !clickInfo || !cursorLine || !clickMarker) {
        console.error("Cannot add timeline listeners: Required elements missing."); return;
    }

    removeTimelineListeners(); // Ensure no duplicate listeners (redundant check, but safe)

    // --- Mouse Move Listener ---
    timelineMouseMoveListener = (event: MouseEvent) => {
        const rect = containerWrapper.getBoundingClientRect(); // Get current rect
        const currentContainerWidth = rect.width; // Use current width
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;

        if (x >= 0 && x <= currentContainerWidth && y >= 0 && y <= containerWrapper.clientHeight) {
            const timeRatio = Math.max(0, Math.min(1, x / currentContainerWidth));
            const currentTime = new Date(overallMinTime + (overallDuration * timeRatio));

            cursorLine.style.left = `${x}px`;
            cursorLine.style.display = 'block';

            if (!isOverSquare) {
                tooltip.textContent = formatDateTime(currentTime);
                positionTooltip(tooltip, event);
                tooltip.style.display = 'block';
            }
        } else {
            cursorLine.style.display = 'none';
            if (!isOverSquare) tooltip.style.display = 'none';
        }
    };

    // --- Click Listener ---
    timelineClickListener = (event: MouseEvent) => {
        const rect = containerWrapper.getBoundingClientRect(); // Get current rect
        const currentContainerWidth = rect.width; // Use current width
        const x = event.clientX - rect.left;
        if (x < 0 || x > currentContainerWidth) {
            clickMarker.style.display = 'none';
            return;
        };

        const timeRatio = Math.max(0, Math.min(1, x / currentContainerWidth));
        const clickTimeMs = overallMinTime + (overallDuration * timeRatio);

        clickMarker.style.left = `${x}px`;
        clickMarker.style.display = 'block';

        clickInfo.innerHTML = `<strong>Tests active around ${formatDateTime(new Date(clickTimeMs))}:</strong><br>`;
        let testsAtTime: string[] = [];
        // Use the passed visData reference here
        visData.forEach(trx => {
            trx.tests.forEach(test => {
                if (test.startTime && test.endTime) {
                    const start = test.startTime.getTime();
                    const end = test.endTime.getTime();
                    if (clickTimeMs >= start && clickTimeMs < end) {
                        testsAtTime.push(`<span>${trx.fileName}:</span> ${test.testName} (<em>${test.outcome}</em>)`);
                    }
                }
            });
        });

        clickInfo.innerHTML += testsAtTime.length > 0 ? testsAtTime.join('<br>') : '<em>No tests active at this exact time.</em>';
        clickInfo.scrollTop = 0;
    };

    // --- Mouse Out Listener (Wrapper) ---
    timelineMouseOutListener = (event: MouseEvent) => {
        // ... (implementation remains the same)
        if (!containerWrapper.contains(event.relatedTarget as Node)) {
            cursorLine.style.display = 'none';
            if (!isOverSquare) tooltip.style.display = 'none';
        }
    };

    // --- Mouse Over/Out Listeners (Squares - Delegated) ---
    squareMouseOverListener = (event: MouseEvent) => {
        // ... (implementation remains the same - positioning uses event coords)
        const target = event.target as HTMLElement;
        const square = target.closest<HTMLElement>('.test-square');
        if (square?.dataset) {
            isOverSquare = true;
            tooltip.style.display = 'block';
            const { testName, suiteName, fileName, outcome, startTimeMs, endTimeMs, durationMs, error } = square.dataset;
            let testDetails = `${testName}\n` +
                `Suite: ${suiteName || 'N/A'}\n` +
                `File: ${fileName}\n` +
                `Outcome: ${outcome}\n` +
                `Start: ${startTimeMs ? formatTime(new Date(parseInt(startTimeMs, 10))) : 'N/A'}\n` + // Parse from dataset
                `End: ${endTimeMs ? formatTime(new Date(parseInt(endTimeMs, 10))) : 'N/A'}\n` +       // Parse from dataset
                `Duration: ${durationMs ? formatDuration(parseInt(durationMs, 10)) : 'N/A'}`;
            if (error) testDetails += `\nError: ${error}`;
            tooltip.textContent = testDetails;
            positionTooltip(tooltip, event);
            tooltip.style.display = 'block';
        }
    };
    squareMouseOutListener = (event: MouseEvent) => {
        // ... (uses event coords and needs currentContainerWidth for time display)
        const target = event.target as HTMLElement;
        const square = target.closest<HTMLElement>('.test-square');
        const relatedTarget = event.relatedTarget as HTMLElement | null;

        if (square) {
            isOverSquare = false;
            if (!containerWrapper.contains(relatedTarget) || !relatedTarget?.closest('.test-square')) {
                const rect = containerWrapper.getBoundingClientRect();
                const currentContainerWidth = rect.width; // Need current width
                const currentX = event.clientX - rect.left;
                if (currentX >= 0 && currentX <= currentContainerWidth && currentX <= rect.width) {
                    const timeRatio = Math.max(0, Math.min(1, currentX / currentContainerWidth));
                    const currentTime = new Date(overallMinTime + (overallDuration * timeRatio));
                    tooltip.textContent = formatDateTime(currentTime);
                    positionTooltip(tooltip, event);
                    tooltip.style.display = 'block';
                } else {
                    tooltip.style.display = 'none';
                }
            }
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

function removeTimelineListeners(): void {
    // ... (implementation remains the same)
    const elements = getVisualizationElements();
    const { containerWrapper, container, tooltip, cursorLine, clickMarker } = elements;
    if (containerWrapper) {
        if (timelineMouseMoveListener) containerWrapper.removeEventListener('mousemove', timelineMouseMoveListener);
        if (timelineClickListener) containerWrapper.removeEventListener('click', timelineClickListener);
        if (timelineMouseOutListener) containerWrapper.removeEventListener('mouseout', timelineMouseOutListener);
    }
    if (container) {
        if (squareMouseOverListener) container.removeEventListener('mouseover', squareMouseOverListener);
        if (squareMouseOutListener) container.removeEventListener('mouseout', squareMouseOutListener);
    }
    timelineMouseMoveListener = null;
    timelineClickListener = null;
    timelineMouseOutListener = null;
    squareMouseOverListener = null;
    squareMouseOutListener = null;
    if (tooltip) tooltip.style.display = 'none';
    if (cursorLine) cursorLine.style.display = 'none';
    if (clickMarker) clickMarker.style.display = 'none';
    // console.log("Timeline listeners removed."); // Less noisy logging
}

// --- Public Modal Functions ---

export function openVisualizationModal(allTrxDataForVis: TrxFileData[]): void {
    const elements = getVisualizationElements();
    const { modal, container, axisHeader, clickInfo, clickMarker, containerWrapper } = elements;

    const validDataForVis = allTrxDataForVis.filter(d =>
        d.runStartTime && d.runEndTime && d.tests.some(t => t.startTime && t.endTime && t.startTime < t.endTime)
    );

    if (!modal || !container || !axisHeader || !clickInfo || !clickMarker || !containerWrapper) {
        console.error("Cannot open visualization: Modal or essential child elements not found.");
        return;
    }

    if (validDataForVis.length === 0) {
        console.warn("Cannot open visualization: No data with valid time ranges available.");
        alert("No data with valid time ranges loaded for visualization.");
        return;
    }

    // Reset UI elements specific to layout
    container.innerHTML = '';
    axisHeader.innerHTML = '';
    clickInfo.innerHTML = 'Click on timeline to see tests';
    clickMarker.style.display = 'none';
    isOverSquare = false;

    modal.style.display = 'block';

    // --- Caching Logic ---
    // Check if the data is the same as the cached data (simple reference check)
    if (cachedVisStructure && cachedVisStructure.sourceDataRef === validDataForVis) {
        console.log("Using cached visualization structure.");
        // Use cache, just run layout
        requestAnimationFrame(() => layoutVisualization(cachedVisStructure!, elements));
    } else {
        console.log("No valid cache found or data changed. Processing data...");
        // Process data, store in cache, then layout
        const newStructure = processVisualizationData(validDataForVis);
        if (newStructure) {
            cachedVisStructure = newStructure; // Update cache
            requestAnimationFrame(() => layoutVisualization(cachedVisStructure!, elements));
        } else {
            // Handle processing error
            container.innerHTML = '<p class="error-message">Failed to process visualization data.</p>';
            cachedVisStructure = null; // Ensure cache is cleared on error
        }
    }

    // --- Add Resize Observer ---
    setupResizeObserver(elements);
}

export function closeVisualizationModal(): void {
    const elements = getVisualizationElements();
    const { modal, container, axisHeader, clickInfo, clickMarker } = elements;

    if (!modal) return;

    modal.style.display = 'none';
    removeTimelineListeners();
    teardownResizeObserver(); // Disconnect observer

    // Clear dynamic content, but cache remains
    if (container) container.innerHTML = '';
    if (axisHeader) axisHeader.innerHTML = '';
    if (clickInfo) clickInfo.innerHTML = 'Click on timeline to see tests';
    if (clickMarker) clickMarker.style.display = 'none';
    // suiteColorMap is reset during processing, not on close
}

// --- Resize Handling ---
function setupResizeObserver(elements: VisualizationElements): void {
    const { containerWrapper } = elements;
    if (!containerWrapper) return;

    // Create debounced layout function only once
    if (!debouncedLayout) {
        debouncedLayout = debounce(() => {
            if (cachedVisStructure && elements.modal?.style.display === 'block') {
                console.log("Resize detected, relayout visualization...");
                layoutVisualization(cachedVisStructure, elements);
            }
        }, RESIZE_DEBOUNCE_MS);
    }


    // Ensure existing observer is disconnected before creating a new one
    teardownResizeObserver();

    resizeObserver = new ResizeObserver(debouncedLayout);
    resizeObserver.observe(containerWrapper);
    console.log("Resize observer setup.");
}

function teardownResizeObserver(): void {
    if (resizeObserver) {
        resizeObserver.disconnect();
        resizeObserver = null;
        console.log("Resize observer disconnected.");
    }
    // Keep debouncedLayout function reference for next setup
}


// --- Cache Invalidation ---
/** Call this function when the underlying TRX data changes (e.g., new files loaded) */
export function invalidateVisualizationCache(): void {
    console.log("Invalidating visualization cache.");
    cachedVisStructure = null;
}

// --- Initialization (Adding listeners to modal controls) ---
export function initializeVisualizationModalControls(): void {
    // ... (implementation remains the same)
    const elements = getVisualizationElements();
    const { modal, modalCloseButton } = elements;
    if (modal && modalCloseButton) {
        modalCloseButton.addEventListener('click', closeVisualizationModal);
        modal.addEventListener('click', (event) => {
            if (event.target === modal) {
                closeVisualizationModal();
            }
        });
    } else {
        console.error("Could not initialize modal close controls.");
    }
}

// --- Debounce Utility (Example - place in utils/debounce.ts) ---
/*
export function debounce<T extends (...args: any[]) => void>(
    func: T,
    waitFor: number
): (...args: Parameters<T>) => void {
    let timeoutId: ReturnType<typeof setTimeout> | null = null;

    return (...args: Parameters<T>): void => {
        if (timeoutId !== null) {
            clearTimeout(timeoutId);
        }
        timeoutId = setTimeout(() => func(...args), waitFor);
    };
}
*/