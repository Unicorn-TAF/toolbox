"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.parseDateTimeSafe = parseDateTimeSafe;
exports.parseTimespan = parseTimespan;
exports.formatDateTime = formatDateTime;
exports.formatTime = formatTime;
exports.formatDuration = formatDuration;
exports.formatNumberForDisplay = formatNumberForDisplay;
/**
 * Safely parses a date-time string.
 * @param dateTimeString The string to parse.
 * @returns A Date object or null if parsing fails or input is null/undefined.
 */
function parseDateTimeSafe(dateTimeString) {
    if (!dateTimeString)
        return null;
    try {
        var date = new Date(dateTimeString);
        return isNaN(date.getTime()) ? null : date;
    }
    catch (e) {
        console.warn("Could not parse date-time string: ".concat(dateTimeString), e);
        return null;
    }
}
/**
 * Parses a .NET TimeSpan string (HH:MM:SS.fffffff) into milliseconds.
 * @param timeSpan The TimeSpan string.
 * @returns Duration in milliseconds, or 0 if parsing fails.
 */
function parseTimespan(timeSpan) {
    if (!timeSpan || typeof timeSpan !== 'string')
        return 0;
    try {
        var parts = timeSpan.split(':');
        if (parts.length !== 3)
            throw new Error("Invalid format");
        var secondsParts = parts[2].split('.');
        var hours = parseInt(parts[0], 10);
        var minutes = parseInt(parts[1], 10);
        var seconds = parseInt(secondsParts[0], 10);
        // Handle fractional seconds more robustly (up to 7 digits)
        var fractionString = (secondsParts[1] || "0").padEnd(7, '0').substring(0, 7);
        var millisecondsFraction = parseFloat("0." + fractionString) * 1000;
        if (isNaN(hours) || isNaN(minutes) || isNaN(seconds) || isNaN(millisecondsFraction)) {
            throw new Error("Contains non-numeric parts");
        }
        return (hours * 3600 + minutes * 60 + seconds) * 1000 + Math.round(millisecondsFraction);
    }
    catch (e) {
        console.warn("Could not parse timespan: ".concat(timeSpan), e);
        return 0;
    }
}
/**
 * Formats a Date object into a standard date and time string (e.g., MM/DD/YYYY HH:MM:SS).
 * Uses 24-hour format.
 */
function formatDateTime(date) {
    if (!date || isNaN(date.getTime()))
        return "Invalid Date";
    // Using en-GB locale often gives DD/MM/YYYY HH:MM:SS format
    return date.toLocaleDateString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' }) + ' ' +
        date.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false });
}
/**
 * Formats a Date object into a time string (HH:MM:SS).
 * Uses 24-hour format.
 */
function formatTime(date) {
    if (!date || isNaN(date.getTime()))
        return "Invalid Time";
    return date.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false });
}
/**
 * Formats a duration in milliseconds into a human-readable string (e.g., 1h 30m 15.5s, 500 ms).
 */
function formatDuration(milliseconds) {
    if (milliseconds === null || milliseconds === undefined || isNaN(milliseconds) || milliseconds < 0)
        return "N/A";
    if (milliseconds < 1000)
        return "".concat(milliseconds.toFixed(0), " ms");
    var totalSeconds = milliseconds / 1000;
    var hours = Math.floor(totalSeconds / 3600);
    var minutes = Math.floor((totalSeconds % 3600) / 60);
    var seconds = (totalSeconds % 60);
    var parts = [];
    if (hours > 0)
        parts.push("".concat(hours, "h"));
    if (minutes > 0)
        parts.push("".concat(minutes, "m"));
    // Show seconds if > 0 OR if it's the only unit (e.g., 1.2s)
    if (seconds > 0 || parts.length === 0) {
        // Show decimal only if total duration < 1 min OR if non-zero normally
        var decimalPlaces = (totalSeconds < 60 && hours === 0 && minutes === 0) ? 1 : 0;
        parts.push("".concat(seconds.toFixed(decimalPlaces), "s"));
    }
    return parts.join(' ');
}
/**
 * Formats a number for display, typically for tables.
 */
function formatNumberForDisplay(num, digits) {
    if (digits === void 0) { digits = 1; }
    if (typeof num !== 'number' || isNaN(num)) {
        return '--';
    }
    return num.toFixed(digits);
}
//# sourceMappingURL=formatters.js.map