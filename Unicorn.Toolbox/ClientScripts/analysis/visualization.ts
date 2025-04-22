import { TrxFileData, TestData } from '../models/trx.js';
import { formatDateTime, formatTime, formatDuration } from '../utils/formatters.js';

// --- Module Constants ---
const VIS_PALETTE: ReadonlyArray<string> = ['#1CCEED', '#1C4AED', '#1C8CED', '#1CEDC7', '#311CED', '#419EF0', '#44676E', '#8A1CED', '#1CE5ED', '#1CADEF', '#87CEEB', '#3CB371', '#FFD700', '#FFA07A', '#9370DB'];
const VIS_FAIL_COLOR: string = '#F04141';
const MIN_TEST_WIDTH_PX: number = 2;

// --- Module State ---
const suiteColorMap: Map<string, number> = new Map(); // Maps suiteId to color index
let nextSuiteColorIndex: number = 0;
let isOverSquare: boolean = false; // Track if mouse is currently over a test square

// --- Listener Management (Store listener references to allow removal) ---
// Define function types for clarity
type MouseMoveHandler = (event: MouseEvent) => void;
type ClickHandler = (event: MouseEvent) => void;
type MouseOutHandler = (event: MouseEvent) => void;
type MouseOverHandler = (event: MouseEvent) => void;

let timelineMouseMoveListener: MouseMoveHandler | null = null;
let timelineClickListener: ClickHandler | null = null;
let timelineMouseOutListener: MouseOutHandler | null = null;
let squareMouseOverListener: MouseOverHandler | null = null;
let squareMouseOutListener: MouseOutHandler | null = null;

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

// --- Helper Functions ---
function getSuiteColorClass(suiteId: string | null): string {
    // Handle null/undefined - Assign a consistent "unknown" index if needed elsewhere
    // but for class generation, we'll just use the logic below.
    const safeSuiteId = suiteId ?? `__unknown_suite_${suiteColorMap.size}`;

    if (!suiteColorMap.has(safeSuiteId)) {
        suiteColorMap.set(safeSuiteId, nextSuiteColorIndex);
        nextSuiteColorIndex++;
    }
    // The modulo operation ensures we wrap around the palette if we have more suites than colors
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

function drawTimelineAxis(axisHeader: HTMLDivElement, minTime: number, maxTime: number, containerWidth: number): void {
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

function drawSuiteUnderline(
    rowDiv: HTMLDivElement,
    startPercent: number,
    endPercent: number,
    suiteId: string | null, // Allow null suiteId
    rowHeight: number,
    underlineHeight: number
): void {
    if (endPercent <= startPercent) return;

    const underline = document.createElement('div');
    underline.className = `suite-underline`; // Base class
    underline.style.left = `${startPercent}%`;
    underline.style.width = `${endPercent - startPercent}%`;
    underline.style.top = `${rowHeight - underlineHeight}px`;
    underline.style.height = `${underlineHeight}px`;

    // --- MODIFICATION START ---
    // Get the same CSS color class used for the test squares
    const colorClass = getSuiteColorClass(suiteId);
    underline.classList.add(colorClass);
    // --- MODIFICATION END ---

    // Remove direct style setting:
    // const colorIdx = suiteColorMap.get(suiteId) ?? 0;
    // underline.style.backgroundColor = VIS_PALETTE[colorIdx % VIS_PALETTE.length];

    underline.title = `Suite ID: ${suiteId ?? 'Unknown'}`; // Add tooltip for suite ID
    rowDiv.appendChild(underline);
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

    if (x + ttRect.width > vw) {
        x = event.clientX - ttRect.width - offsetX;
    }
    if (y + ttRect.height > vh) {
        y = event.clientY - ttRect.height - offsetY;
    }

    x = Math.max(offsetX, x);
    y = Math.max(offsetY, y);

    tooltip.style.left = `${x}px`;
    tooltip.style.top = `${y}px`;
}

// --- Visualization Generation ---
function generateVisualization(elements: VisualizationElements, visData: TrxFileData[]): void {
    const { container, axisHeader, clickInfo, containerWrapper } = elements;

    if (!container || !axisHeader || !clickInfo || !containerWrapper) {
        console.error("Timeline elements not found for generation."); return;
    }
    if (visData.length === 0) {
        container.innerHTML = '<p style="text-align: center; padding: 20px; color: #555;">No valid visualization data loaded.</p>'; return;
    }

    container.innerHTML = '';
    axisHeader.innerHTML = '';
    clickInfo.innerHTML = 'Click on timeline to see tests';
    suiteColorMap.clear();
    nextSuiteColorIndex = 0;
    isOverSquare = false;

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
        console.error("Could not determine valid time range.", { min: overallMinTime, max: overallMaxTime });
        container.innerHTML = '<p class="error-message">Error calculating time range.</p>'; return;
    }

    const overallDuration = overallMaxTime - overallMinTime;
    const containerWidth = containerWrapper.clientWidth;

    if (overallDuration <= 0 || containerWidth <= 0) {
        console.error("Invalid duration or container width.", { duration: overallDuration, width: containerWidth });
        container.innerHTML = '<p class="error-message">Invalid duration or width.</p>'; return;
    }

    drawTimelineAxis(axisHeader, overallMinTime, overallMaxTime, containerWidth);

    const rowHeight = 25;
    const rowMargin = 2;
    const underlineHeight = 2;
    let totalHeight = 0;

    visData.forEach((trx, fileIndex) => {
        const rowDiv = document.createElement('div');
        rowDiv.className = 'timeline-row';
        rowDiv.style.top = `${fileIndex * (rowHeight + rowMargin)}px`;
        rowDiv.dataset.fileName = trx.fileName;

        let currentSuiteId: string | null = null;
        let currentSuiteStartXPercent = -1;
        let currentSuiteEndXPercent = -1;

        const testsToVisualize = trx.tests
            .filter(t => t.startTime && t.endTime && t.startTime < t.endTime)
            .sort((a, b) => (a.startTime!.getTime()) - (b.startTime!.getTime()));

        testsToVisualize.forEach((test) => {
            const testStartTime = test.startTime!.getTime();
            const testEndTime = test.endTime!.getTime();
            const relativeStartMs = testStartTime - overallMinTime;
            const durationMs = testEndTime - testStartTime;

            const leftPercent = (relativeStartMs / overallDuration) * 100;
            let widthPercent = (durationMs / overallDuration) * 100;

            if (leftPercent < 0 || leftPercent > 100 || isNaN(leftPercent) || isNaN(widthPercent) || durationMs <= 0) return;

            const minWidthPercent = (MIN_TEST_WIDTH_PX / containerWidth) * 100;
            widthPercent = Math.max(widthPercent, minWidthPercent);
            widthPercent = Math.min(widthPercent, 100 - leftPercent);

            if (widthPercent <= 0) return;

            const testEndXPercent = leftPercent + widthPercent;
            const suiteId = test.testListId;
            const suiteName = test.testListId ? (trx.testListMap.get(test.testListId) || `Suite ${test.testListId}`) : 'Unknown Suite';

            // --- Suite Underline Logic ---
            if (suiteId !== currentSuiteId) {
                if (currentSuiteId !== null && currentSuiteStartXPercent >= 0 && currentSuiteEndXPercent > currentSuiteStartXPercent) {
                    // Draw previous underline
                    drawSuiteUnderline(rowDiv, currentSuiteStartXPercent, currentSuiteEndXPercent, currentSuiteId, rowHeight, underlineHeight);
                }
                // Start new suite tracking (allow null suiteId)
                currentSuiteId = suiteId;
                currentSuiteStartXPercent = leftPercent;
                currentSuiteEndXPercent = testEndXPercent;
            } else if (currentSuiteId === null && suiteId === null) {
                // Edge case: Consecutive tests with *no* suite ID. Extend the "null suite" underline.
                currentSuiteEndXPercent = Math.max(currentSuiteEndXPercent, testEndXPercent);
            }
            else {
                // Extend the end position for the current *non-null* suite
                if (currentSuiteId !== null) {
                    currentSuiteEndXPercent = Math.max(currentSuiteEndXPercent, testEndXPercent);
                }
            }

            // --- Draw Test Square ---
            const square = document.createElement('div');
            square.className = 'test-square';
            square.style.left = `${leftPercent}%`;
            square.style.width = `${widthPercent}%`;

            square.dataset.testName = test.testName;
            square.dataset.startTime = test.startTime!.toISOString();
            square.dataset.endTime = test.endTime!.toISOString();
            square.dataset.outcome = test.outcome;
            square.dataset.fileName = trx.fileName;
            square.dataset.duration = durationMs.toString();
            square.dataset.suiteName = suiteName;
            if (test.outcome === 'Failed' && test.errorMessage) {
                square.dataset.error = test.errorMessage.substring(0, 200) + (test.errorMessage.length > 200 ? '...' : '');
            }

            square.classList.add(test.outcome === 'Failed' ? 'color-fail' : getSuiteColorClass(suiteId));
            rowDiv.appendChild(square);
        }); // End loop through tests

        // Draw underline for the very last suite in the row
        // Check currentSuiteEndXPercent > currentSuiteStartXPercent to avoid drawing 0-width lines if a suite had no valid tests
        if (currentSuiteStartXPercent >= 0 && currentSuiteEndXPercent > currentSuiteStartXPercent) {
            drawSuiteUnderline(rowDiv, currentSuiteStartXPercent, currentSuiteEndXPercent, currentSuiteId, rowHeight, underlineHeight);
        }


        container.appendChild(rowDiv);
        totalHeight += (rowHeight + rowMargin);
    }); // End loop through files

    container.style.minHeight = `${totalHeight}px`;
    addTimelineListeners(elements, overallMinTime, overallDuration, containerWidth, visData);
}


// --- Listener Management ---
function addTimelineListeners(
    elements: VisualizationElements,
    overallMinTime: number,
    overallDuration: number,
    containerWidth: number,
    visData: TrxFileData[]
): void {
    const { containerWrapper, container, tooltip, clickInfo, cursorLine, clickMarker } = elements;
    if (!tooltip) { console.error("TOOLTIP ELEMENT NOT FOUND!"); return; }
    if (!containerWrapper || !container || !clickInfo || !cursorLine || !clickMarker) {
        console.error("Cannot add timeline listeners: Required elements missing."); return;
    }

    removeTimelineListeners(); // Ensure no duplicate listeners

    // --- Mouse Move Listener ---
    timelineMouseMoveListener = (event: MouseEvent) => {
        const rect = containerWrapper.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;

        if (x >= 0 && x <= containerWidth && y >= 0 && y <= containerWrapper.clientHeight) {
            const timeRatio = Math.max(0, Math.min(1, x / containerWidth));
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
        const rect = containerWrapper.getBoundingClientRect();
        const x = event.clientX - rect.left;
        if (x < 0 || x > containerWidth) {
            clickMarker.style.display = 'none';
            return;
        };

        const timeRatio = Math.max(0, Math.min(1, x / containerWidth));
        const clickTimeMs = overallMinTime + (overallDuration * timeRatio);

        clickMarker.style.left = `${x}px`;
        clickMarker.style.display = 'block';

        clickInfo.innerHTML = `<strong>Tests active around ${formatDateTime(new Date(clickTimeMs))}:</strong><br>`;
        let testsAtTime: string[] = [];
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
        if (!containerWrapper.contains(event.relatedTarget as Node)) {
            cursorLine.style.display = 'none';
            if (!isOverSquare) tooltip.style.display = 'none';
        }
    };

    // --- Mouse Over Listener (Squares - Delegated) ---
    squareMouseOverListener = (event: MouseEvent) => {
        const target = event.target as HTMLElement;
        const square = target.closest<HTMLElement>('.test-square');
        if (square?.dataset) {
            isOverSquare = true;
            tooltip.style.display = 'block';

            const { testName, suiteName, fileName, outcome, startTime, endTime, duration, error } = square.dataset;

            let testDetails = `${testName}\n` +
                `Suite: ${suiteName || 'N/A'}\n` +
                `File: ${fileName}\n` +
                `Outcome: ${outcome}\n` +
                `Start: ${startTime ? formatTime(new Date(startTime)) : 'N/A'}\n` +
                `End: ${endTime ? formatTime(new Date(endTime)) : 'N/A'}\n` +
                `Duration: ${duration ? formatDuration(parseInt(duration, 10)) : 'N/A'}`;
            if (error) {
                testDetails += `\nError: ${error}`;
            }

            tooltip.textContent = testDetails;
            positionTooltip(tooltip, event);
            tooltip.style.display = 'block';
        }
    };

    // --- Mouse Out Listener (Squares - Delegated) ---
    squareMouseOutListener = (event: MouseEvent) => {
        const target = event.target as HTMLElement;
        const square = target.closest<HTMLElement>('.test-square');
        const relatedTarget = event.relatedTarget as HTMLElement | null;

        if (square) {
            isOverSquare = false;
            if (!containerWrapper.contains(relatedTarget) || !relatedTarget?.closest('.test-square')) {
                const rect = containerWrapper.getBoundingClientRect();
                const currentX = event.clientX - rect.left;
                if (currentX >= 0 && currentX <= containerWidth && currentX <= rect.width) {
                    const timeRatio = Math.max(0, Math.min(1, currentX / containerWidth));
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
    container.addEventListener('mouseover', squareMouseOverListener);
    container.addEventListener('mouseout', squareMouseOutListener);

    console.log("Timeline listeners added.");
}

export function removeTimelineListeners(): void {
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

    console.log("Timeline listeners removed.");
}


// --- Public Modal Functions ---
export function openVisualizationModal(allTrxDataForVis: TrxFileData[]): void {
    const elements = getVisualizationElements();
    const { modal, container, axisHeader, clickInfo, clickMarker } = elements;

    const validDataForVis = allTrxDataForVis.filter(d =>
        d.runStartTime && d.runEndTime && d.tests.some(t => t.startTime && t.endTime && t.startTime < t.endTime)
    );

    if (!modal || !container || !axisHeader || !clickInfo || !clickMarker) {
        console.error("Cannot open visualization: Modal or essential child elements not found.");
        return;
    }

    if (validDataForVis.length === 0) {
        console.warn("Cannot open visualization: No data with valid time ranges available.");
        alert("No data with valid time ranges loaded for visualization.");
        return;
    }

    container.innerHTML = '';
    axisHeader.innerHTML = '';
    clickInfo.innerHTML = 'Click on timeline to see tests';
    clickMarker.style.display = 'none';
    suiteColorMap.clear();
    nextSuiteColorIndex = 0;
    isOverSquare = false;

    modal.style.display = 'block';
    requestAnimationFrame(() => generateVisualization(elements, validDataForVis));
}

export function closeVisualizationModal(): void {
    const elements = getVisualizationElements();
    const { modal, container, axisHeader, clickInfo, clickMarker } = elements;

    if (!modal) return;

    modal.style.display = 'none';
    removeTimelineListeners();

    if (container) container.innerHTML = '';
    if (axisHeader) axisHeader.innerHTML = '';
    if (clickInfo) clickInfo.innerHTML = 'Click on timeline to see tests';
    if (clickMarker) clickMarker.style.display = 'none';
    suiteColorMap.clear();
}

// --- Initialization (Adding listeners to modal controls) ---
export function initializeVisualizationModalControls(): void {
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