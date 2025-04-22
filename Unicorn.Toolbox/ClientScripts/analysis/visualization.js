"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.removeTimelineListeners = removeTimelineListeners;
exports.openVisualizationModal = openVisualizationModal;
exports.closeVisualizationModal = closeVisualizationModal;
exports.initializeVisualizationModalControls = initializeVisualizationModalControls;
var formatters_1 = require("../utils/formatters");
// --- Module Constants ---
var VIS_PALETTE = ['#1CCEED', '#1C4AED', '#1C8CED', '#1CEDC7', '#311CED', '#419EF0', '#44676E', '#8A1CED', '#1CE5ED', '#1CADEF', '#87CEEB', '#3CB371', '#FFD700', '#FFA07A', '#9370DB'];
var VIS_FAIL_COLOR = '#F04141';
var MIN_TEST_WIDTH_PX = 2;
// --- Module State ---
var suiteColorMap = new Map(); // Maps suiteId to color index
var nextSuiteColorIndex = 0;
var isOverSquare = false; // Track if mouse is currently over a test square
var timelineMouseMoveListener = null;
var timelineClickListener = null;
var timelineMouseOutListener = null;
var squareMouseOverListener = null;
var squareMouseOutListener = null;
function getVisualizationElements() {
    var modal = document.getElementById('visualizationModal');
    return {
        modal: modal,
        modalCloseButton: modal === null || modal === void 0 ? void 0 : modal.querySelector('.modal-close-button'),
        containerWrapper: document.getElementById('timelineContainerWrapper'),
        container: document.getElementById('timelineContainer'),
        axisHeader: document.getElementById('timelineAxisHeader'),
        tooltip: document.getElementById('timelineTooltip'),
        clickInfo: document.getElementById('timelineClickInfo'),
        cursorLine: document.getElementById('timelineCursorLine'),
        clickMarker: document.getElementById('timelineClickMarkerLine'),
    };
}
// --- Helper Functions ---
function getSuiteColorClass(suiteId) {
    var _a;
    var safeSuiteId = suiteId !== null && suiteId !== void 0 ? suiteId : "unknown_suite_".concat(suiteColorMap.size); // Handle null/undefined
    if (!suiteColorMap.has(safeSuiteId)) {
        suiteColorMap.set(safeSuiteId, nextSuiteColorIndex);
        nextSuiteColorIndex++; // Increment global index only for new suites
    }
    var colorIndex = ((_a = suiteColorMap.get(safeSuiteId)) !== null && _a !== void 0 ? _a : 0) % VIS_PALETTE.length; // Wrap index
    return "color-".concat(colorIndex);
}
function createTimeLabel(text, percent, transform) {
    if (transform === void 0) { transform = 'translateX(-50%)'; }
    var label = document.createElement('span');
    label.className = 'time-label';
    label.textContent = text;
    label.style.left = "".concat(percent, "%");
    label.style.transform = transform; // Center by default, override for edges
    return label;
}
function drawTimelineAxis(axisHeader, minTime, maxTime, containerWidth) {
    axisHeader.innerHTML = ''; // Clear previous axis
    var duration = maxTime - minTime;
    if (duration <= 0 || containerWidth <= 0)
        return; // Guard against invalid input
    var startTime = new Date(minTime);
    var endTime = new Date(maxTime);
    // Determine appropriate tick intervals (e.g., seconds, minutes, hours)
    var tickIntervalMs;
    var durationSec = duration / 1000;
    var durationMin = durationSec / 60;
    var durationHr = durationMin / 60;
    var minTickGapPx = 80; // Minimum pixels between major ticks (reduced slightly)
    var maxTicks = Math.max(1, Math.floor(containerWidth / minTickGapPx)); // Ensure at least 1 tick calculation
    // More robust interval calculation
    if (durationHr / maxTicks > 2)
        tickIntervalMs = 3600 * 1000 * Math.ceil(durationHr / maxTicks); // Multiple hours
    else if (durationHr > 2)
        tickIntervalMs = 3600 * 1000; // Hourly
    else if (durationMin / maxTicks > 2)
        tickIntervalMs = 60 * 1000 * Math.ceil(durationMin / maxTicks); // Multiple minutes
    else if (durationMin > 1)
        tickIntervalMs = 60 * 1000; // Minutely
    else if (durationSec / maxTicks > 2)
        tickIntervalMs = 1000 * Math.ceil(durationSec / maxTicks); // Multiple seconds
    else
        tickIntervalMs = 1000; // Default to seconds for short durations
    // Start Time Label (Always show)
    var startLabel = createTimeLabel((0, formatters_1.formatDateTime)(startTime), 0, 'translateX(0)');
    startLabel.title = startTime.toISOString();
    axisHeader.appendChild(startLabel);
    // End Time Label (Always show)
    var endLabel = createTimeLabel((0, formatters_1.formatDateTime)(endTime), 100, 'translateX(-100%)');
    endLabel.title = endTime.toISOString();
    axisHeader.appendChild(endLabel);
    // Generate ticks based on interval
    var firstTickTime = Math.ceil(minTime / tickIntervalMs) * tickIntervalMs;
    for (var tickTime = firstTickTime; tickTime < maxTime; tickTime += tickIntervalMs) {
        var percent = ((tickTime - minTime) / duration) * 100;
        // Avoid placing ticks too close to the edges where start/end labels are
        if (percent < 5 || percent > 95)
            continue; // Increased margin slightly
        var tickDate = new Date(tickTime);
        var labelText = (0, formatters_1.formatTime)(tickDate); // Show only time for intermediate ticks
        var label = createTimeLabel(labelText, percent);
        label.title = (0, formatters_1.formatDateTime)(tickDate); // Full timestamp on hover
        axisHeader.appendChild(label);
    }
}
function drawSuiteUnderline(rowDiv, startPercent, endPercent, suiteId, rowHeight, underlineHeight) {
    var _a;
    if (endPercent <= startPercent)
        return; // Don't draw zero/negative width lines
    var underline = document.createElement('div');
    underline.className = "suite-underline";
    underline.style.left = "".concat(startPercent, "%");
    underline.style.width = "".concat(endPercent - startPercent, "%");
    underline.style.top = "".concat(rowHeight - underlineHeight, "px"); // Position at the bottom
    underline.style.height = "".concat(underlineHeight, "px");
    // Use the actual color from the palette for the background
    var colorIdx = (_a = suiteColorMap.get(suiteId)) !== null && _a !== void 0 ? _a : 0; // Default to 0 if somehow missing
    underline.style.backgroundColor = VIS_PALETTE[colorIdx % VIS_PALETTE.length];
    underline.title = "Suite ID: ".concat(suiteId); // Add tooltip for suite ID
    rowDiv.appendChild(underline);
}
function positionTooltip(tooltip, event) {
    var offsetX = 15;
    var offsetY = 15;
    var x = event.clientX + offsetX;
    var y = event.clientY + offsetY;
    var vw = window.innerWidth;
    var vh = window.innerHeight;
    // Make tooltip temporarily visible but off-screen to measure
    tooltip.style.visibility = 'hidden';
    tooltip.style.display = 'block';
    var ttRect = tooltip.getBoundingClientRect();
    tooltip.style.display = 'none'; // Hide again before positioning
    tooltip.style.visibility = 'visible';
    // Adjust position if tooltip overflows viewport
    if (x + ttRect.width > vw) {
        x = event.clientX - ttRect.width - offsetX; // Position to the left
    }
    if (y + ttRect.height > vh) {
        y = event.clientY - ttRect.height - offsetY; // Position above
    }
    // Prevent going off top/left edges
    x = Math.max(offsetX, x);
    y = Math.max(offsetY, y);
    tooltip.style.left = "".concat(x, "px");
    tooltip.style.top = "".concat(y, "px");
}
// --- Visualization Generation ---
function generateVisualization(elements, visData) {
    var container = elements.container, axisHeader = elements.axisHeader, clickInfo = elements.clickInfo, containerWrapper = elements.containerWrapper;
    if (!container || !axisHeader || !clickInfo || !containerWrapper) {
        console.error("Timeline elements not found for generation.");
        return;
    }
    if (visData.length === 0) {
        container.innerHTML = '<p style="text-align: center; padding: 20px; color: #555;">No valid visualization data loaded.</p>';
        return;
    }
    // Clear previous content
    container.innerHTML = '';
    axisHeader.innerHTML = '';
    clickInfo.innerHTML = 'Click on timeline to see tests';
    suiteColorMap.clear(); // Reset suite colors for each generation
    nextSuiteColorIndex = 0;
    isOverSquare = false; // Reset state
    // Calculate overall time range from the VALID data
    var overallMinTime = Infinity;
    var overallMaxTime = -Infinity;
    visData.forEach(function (trx) {
        var _a, _b;
        var runStart = (_a = trx.runStartTime) === null || _a === void 0 ? void 0 : _a.getTime();
        var runEnd = (_b = trx.runEndTime) === null || _b === void 0 ? void 0 : _b.getTime();
        if (runStart && runStart < overallMinTime)
            overallMinTime = runStart;
        if (runEnd && runEnd > overallMaxTime)
            overallMaxTime = runEnd;
        trx.tests.forEach(function (test) {
            var _a, _b;
            var testStart = (_a = test.startTime) === null || _a === void 0 ? void 0 : _a.getTime();
            var testEnd = (_b = test.endTime) === null || _b === void 0 ? void 0 : _b.getTime();
            if (testStart && testStart < overallMinTime)
                overallMinTime = testStart;
            if (testEnd && testEnd > overallMaxTime)
                overallMaxTime = testEnd;
        });
    });
    if (overallMinTime === Infinity || overallMaxTime === -Infinity || overallMinTime >= overallMaxTime) {
        console.error("Could not determine valid time range.", { min: overallMinTime, max: overallMaxTime });
        container.innerHTML = '<p class="error-message">Error calculating time range.</p>';
        return;
    }
    var overallDuration = overallMaxTime - overallMinTime;
    var containerWidth = containerWrapper.clientWidth;
    if (overallDuration <= 0 || containerWidth <= 0) {
        console.error("Invalid duration or container width.", { duration: overallDuration, width: containerWidth });
        container.innerHTML = '<p class="error-message">Invalid duration or width.</p>';
        return;
    }
    // --- Draw Axis ---
    drawTimelineAxis(axisHeader, overallMinTime, overallMaxTime, containerWidth);
    // --- Draw Rows and Tests ---
    var rowHeight = 25; // pixels
    var rowMargin = 2; // pixels
    var underlineHeight = 2; // pixels
    var totalHeight = 0;
    visData.forEach(function (trx, fileIndex) {
        var rowDiv = document.createElement('div');
        rowDiv.className = 'timeline-row';
        rowDiv.style.top = "".concat(fileIndex * (rowHeight + rowMargin), "px");
        rowDiv.dataset.fileName = trx.fileName;
        var currentSuiteId = null;
        var currentSuiteStartXPercent = -1;
        var currentSuiteEndXPercent = -1; // Track the end of the current suite's underline
        var testsToVisualize = trx.tests
            .filter(function (t) { return t.startTime && t.endTime && t.startTime < t.endTime; })
            .sort(function (a, b) { return (a.startTime.getTime()) - (b.startTime.getTime()); }); // Non-null assertion okay due to filter
        testsToVisualize.forEach(function (test) {
            // Assertions okay due to filter
            var testStartTime = test.startTime.getTime();
            var testEndTime = test.endTime.getTime();
            var relativeStartMs = testStartTime - overallMinTime;
            var durationMs = testEndTime - testStartTime;
            var leftPercent = (relativeStartMs / overallDuration) * 100;
            var widthPercent = (durationMs / overallDuration) * 100;
            if (leftPercent < 0 || leftPercent > 100 || isNaN(leftPercent) || isNaN(widthPercent) || durationMs <= 0)
                return;
            var minWidthPercent = (MIN_TEST_WIDTH_PX / containerWidth) * 100;
            widthPercent = Math.max(widthPercent, minWidthPercent);
            widthPercent = Math.min(widthPercent, 100 - leftPercent); // Prevent overflow
            if (widthPercent <= 0)
                return;
            var testEndXPercent = leftPercent + widthPercent;
            var suiteId = test.testListId; // May be null
            var suiteName = test.testListId ? (trx.testListMap.get(test.testListId) || "Suite ".concat(test.testListId)) : 'Unknown Suite';
            // --- Suite Underline Logic ---
            if (suiteId !== currentSuiteId) {
                // Finish previous suite's underline
                if (currentSuiteId !== null && currentSuiteStartXPercent >= 0 && currentSuiteEndXPercent > currentSuiteStartXPercent) {
                    drawSuiteUnderline(rowDiv, currentSuiteStartXPercent, currentSuiteEndXPercent, currentSuiteId, rowHeight, underlineHeight);
                }
                // Start new suite tracking
                currentSuiteId = suiteId; // Can be null
                currentSuiteStartXPercent = leftPercent;
                currentSuiteEndXPercent = testEndXPercent;
            }
            else {
                // Extend the end position for the current suite
                currentSuiteEndXPercent = Math.max(currentSuiteEndXPercent, testEndXPercent);
            }
            // --- Draw Test Square ---
            var square = document.createElement('div');
            square.className = 'test-square';
            square.style.left = "".concat(leftPercent, "%");
            square.style.width = "".concat(widthPercent, "%");
            // Store data attributes
            square.dataset.testName = test.testName;
            square.dataset.startTime = test.startTime.toISOString();
            square.dataset.endTime = test.endTime.toISOString();
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
        if (currentSuiteId !== null && currentSuiteStartXPercent >= 0 && currentSuiteEndXPercent > currentSuiteStartXPercent) {
            drawSuiteUnderline(rowDiv, currentSuiteStartXPercent, currentSuiteEndXPercent, currentSuiteId, rowHeight, underlineHeight);
        }
        container.appendChild(rowDiv);
        totalHeight += (rowHeight + rowMargin);
    }); // End loop through files
    // Set the minimum height of the container to hold all rows
    container.style.minHeight = "".concat(totalHeight, "px");
    // Add interactive listeners AFTER drawing is complete
    addTimelineListeners(elements, overallMinTime, overallDuration, containerWidth, visData);
}
// --- Listener Management ---
function addTimelineListeners(elements, overallMinTime, overallDuration, containerWidth, visData) {
    var containerWrapper = elements.containerWrapper, container = elements.container, tooltip = elements.tooltip, clickInfo = elements.clickInfo, cursorLine = elements.cursorLine, clickMarker = elements.clickMarker;
    if (!containerWrapper || !container || !tooltip || !clickInfo || !cursorLine || !clickMarker) {
        console.error("Cannot add timeline listeners: Required elements missing.");
        return;
    }
    removeTimelineListeners(); // Ensure no duplicate listeners
    // --- Mouse Move Listener ---
    timelineMouseMoveListener = function (event) {
        var rect = containerWrapper.getBoundingClientRect();
        var x = event.clientX - rect.left;
        var y = event.clientY - rect.top;
        if (x >= 0 && x <= containerWidth && y >= 0 && y <= containerWrapper.clientHeight) {
            var timeRatio = Math.max(0, Math.min(1, x / containerWidth));
            var currentTime = new Date(overallMinTime + (overallDuration * timeRatio));
            cursorLine.style.left = "".concat(x, "px");
            cursorLine.style.display = 'block';
            if (!isOverSquare) {
                tooltip.textContent = (0, formatters_1.formatDateTime)(currentTime);
                positionTooltip(tooltip, event);
                tooltip.style.display = 'block';
            }
        }
        else {
            cursorLine.style.display = 'none';
            if (!isOverSquare)
                tooltip.style.display = 'none';
        }
    };
    // --- Click Listener ---
    timelineClickListener = function (event) {
        var rect = containerWrapper.getBoundingClientRect();
        var x = event.clientX - rect.left;
        if (x < 0 || x > containerWidth) { // Click outside bounds
            clickMarker.style.display = 'none';
            return;
        }
        ;
        var timeRatio = Math.max(0, Math.min(1, x / containerWidth));
        var clickTimeMs = overallMinTime + (overallDuration * timeRatio);
        clickMarker.style.left = "".concat(x, "px");
        clickMarker.style.display = 'block';
        clickInfo.innerHTML = "<strong>Tests active around ".concat((0, formatters_1.formatDateTime)(new Date(clickTimeMs)), ":</strong><br>");
        var testsAtTime = [];
        visData.forEach(function (trx) {
            trx.tests.forEach(function (test) {
                if (test.startTime && test.endTime) {
                    var start = test.startTime.getTime();
                    var end = test.endTime.getTime();
                    if (clickTimeMs >= start && clickTimeMs < end) {
                        testsAtTime.push("<span>".concat(trx.fileName, ":</span> ").concat(test.testName, " (<em>").concat(test.outcome, "</em>)"));
                    }
                }
            });
        });
        clickInfo.innerHTML += testsAtTime.length > 0 ? testsAtTime.join('<br>') : '<em>No tests active at this exact time.</em>';
        clickInfo.scrollTop = 0;
    };
    // --- Mouse Out Listener (Wrapper) ---
    timelineMouseOutListener = function (event) {
        if (!containerWrapper.contains(event.relatedTarget)) {
            cursorLine.style.display = 'none';
            if (!isOverSquare)
                tooltip.style.display = 'none';
        }
    };
    // --- Mouse Over Listener (Squares - Delegated) ---
    squareMouseOverListener = function (event) {
        var target = event.target;
        var square = target.closest('.test-square');
        if (square === null || square === void 0 ? void 0 : square.dataset) { // Check dataset exists
            isOverSquare = true;
            tooltip.style.display = 'block'; // Make sure it's visible
            var _a = square.dataset, testName = _a.testName, suiteName = _a.suiteName, fileName = _a.fileName, outcome = _a.outcome, startTime = _a.startTime, endTime = _a.endTime, duration = _a.duration, error = _a.error;
            var testDetails = "".concat(testName, "\n") +
                "Suite: ".concat(suiteName || 'N/A', "\n") +
                "File: ".concat(fileName, "\n") +
                "Outcome: ".concat(outcome, "\n") +
                "Start: ".concat(startTime ? (0, formatters_1.formatTime)(new Date(startTime)) : 'N/A', "\n") +
                "End: ".concat(endTime ? (0, formatters_1.formatTime)(new Date(endTime)) : 'N/A', "\n") +
                "Duration: ".concat(duration ? (0, formatters_1.formatDuration)(parseInt(duration, 10)) : 'N/A');
            if (error) {
                testDetails += "\nError: ".concat(error);
            }
            tooltip.textContent = testDetails;
            positionTooltip(tooltip, event);
        }
    };
    // --- Mouse Out Listener (Squares - Delegated) ---
    squareMouseOutListener = function (event) {
        var target = event.target;
        var square = target.closest('.test-square');
        var relatedTarget = event.relatedTarget;
        if (square) {
            isOverSquare = false;
            // Hide tooltip only if moving outside the main wrapper OR to a non-square element within the container
            if (!containerWrapper.contains(relatedTarget) || !(relatedTarget === null || relatedTarget === void 0 ? void 0 : relatedTarget.closest('.test-square'))) {
                // Check if the cursor is still inside the wrapper bounds
                var rect = containerWrapper.getBoundingClientRect();
                var currentX = event.clientX - rect.left;
                if (currentX >= 0 && currentX <= containerWidth && currentX <= rect.width /* check actual width */) {
                    // Cursor still inside wrapper -> show time tooltip
                    var timeRatio = Math.max(0, Math.min(1, currentX / containerWidth));
                    var currentTime = new Date(overallMinTime + (overallDuration * timeRatio));
                    tooltip.textContent = (0, formatters_1.formatDateTime)(currentTime);
                    positionTooltip(tooltip, event); // Reposition based on current event
                    tooltip.style.display = 'block'; // Ensure visible
                }
                else {
                    // Cursor moved outside wrapper
                    tooltip.style.display = 'none';
                }
            }
            // If moving to another square, its mouseover will handle the tooltip.
        }
    };
    // Attach listeners
    containerWrapper.addEventListener('mousemove', timelineMouseMoveListener);
    containerWrapper.addEventListener('click', timelineClickListener);
    containerWrapper.addEventListener('mouseout', timelineMouseOutListener);
    container.addEventListener('mouseover', squareMouseOverListener); // Use event delegation
    container.addEventListener('mouseout', squareMouseOutListener); // Use event delegation
    console.log("Timeline listeners added.");
}
function removeTimelineListeners() {
    var elements = getVisualizationElements();
    var containerWrapper = elements.containerWrapper, container = elements.container, tooltip = elements.tooltip, cursorLine = elements.cursorLine, clickMarker = elements.clickMarker;
    // Remove listeners if they exist and element exists
    if (containerWrapper) {
        if (timelineMouseMoveListener)
            containerWrapper.removeEventListener('mousemove', timelineMouseMoveListener);
        if (timelineClickListener)
            containerWrapper.removeEventListener('click', timelineClickListener);
        if (timelineMouseOutListener)
            containerWrapper.removeEventListener('mouseout', timelineMouseOutListener);
    }
    if (container) {
        if (squareMouseOverListener)
            container.removeEventListener('mouseover', squareMouseOverListener);
        if (squareMouseOutListener)
            container.removeEventListener('mouseout', squareMouseOutListener);
    }
    // Nullify references
    timelineMouseMoveListener = null;
    timelineClickListener = null;
    timelineMouseOutListener = null;
    squareMouseOverListener = null;
    squareMouseOutListener = null;
    // Hide visual elements
    if (tooltip)
        tooltip.style.display = 'none';
    if (cursorLine)
        cursorLine.style.display = 'none';
    if (clickMarker)
        clickMarker.style.display = 'none';
    console.log("Timeline listeners removed.");
}
// --- Public Modal Functions ---
function openVisualizationModal(allTrxDataForVis) {
    var elements = getVisualizationElements();
    var modal = elements.modal, container = elements.container, axisHeader = elements.axisHeader, clickInfo = elements.clickInfo, clickMarker = elements.clickMarker;
    // Filter for data actually usable in visualization *before* deciding to open
    var validDataForVis = allTrxDataForVis.filter(function (d) {
        return d.runStartTime && d.runEndTime && d.tests.some(function (t) { return t.startTime && t.endTime && t.startTime < t.endTime; });
    });
    if (!modal || !container || !axisHeader || !clickInfo || !clickMarker) {
        console.error("Cannot open visualization: Modal or essential child elements not found.");
        return;
    }
    if (validDataForVis.length === 0) {
        console.warn("Cannot open visualization: No data with valid time ranges available.");
        alert("No data with valid time ranges loaded for visualization.");
        return;
    }
    // Reset previous state
    container.innerHTML = '';
    axisHeader.innerHTML = '';
    clickInfo.innerHTML = 'Click on timeline to see tests';
    clickMarker.style.display = 'none';
    suiteColorMap.clear();
    nextSuiteColorIndex = 0;
    isOverSquare = false;
    modal.style.display = 'block';
    // Use rAF to ensure modal is visible and dimensions are calculated before drawing
    requestAnimationFrame(function () { return generateVisualization(elements, validDataForVis); }); // Pass valid data
}
function closeVisualizationModal() {
    var elements = getVisualizationElements();
    var modal = elements.modal, container = elements.container, axisHeader = elements.axisHeader, clickInfo = elements.clickInfo, clickMarker = elements.clickMarker;
    if (!modal)
        return;
    modal.style.display = 'none';
    removeTimelineListeners(); // Crucial for cleanup
    // Clear content
    if (container)
        container.innerHTML = '';
    if (axisHeader)
        axisHeader.innerHTML = '';
    if (clickInfo)
        clickInfo.innerHTML = 'Click on timeline to see tests';
    if (clickMarker)
        clickMarker.style.display = 'none';
    suiteColorMap.clear(); // Clear color map on close
}
// --- Initialization (Adding listeners to modal controls) ---
function initializeVisualizationModalControls() {
    var elements = getVisualizationElements();
    var modal = elements.modal, modalCloseButton = elements.modalCloseButton;
    if (modal && modalCloseButton) {
        modalCloseButton.addEventListener('click', closeVisualizationModal);
        modal.addEventListener('click', function (event) {
            // Close modal if clicking on the backdrop (modal element itself)
            if (event.target === modal) {
                closeVisualizationModal();
            }
        });
    }
    else {
        console.error("Could not initialize modal close controls.");
    }
}
//# sourceMappingURL=visualization.js.map