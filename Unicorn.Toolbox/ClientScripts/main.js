"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
// Import initialization functions for each tab
var analysis_1 = require("./analysis/analysis");
var efficiency_1 = require("./efficiency/efficiency");
// --- State ---
var analysisTabInitialized = false;
var efficiencyTabInitialized = false;
// --- DOM Element Safe Access ---
function getElement(selector) {
    return document.querySelector(selector);
}
// --- Tab Navigation Logic ---
function setupTabs() {
    var tabsContainer = getElement('.tabs');
    var contentContainer = getElement('#content'); // Can be <main>
    if (!tabsContainer || !contentContainer) {
        console.error("Could not find tabs container or content container.");
        return;
    }
    var tabLinks = tabsContainer.querySelectorAll('a[data-target]');
    var contentSections = contentContainer.querySelectorAll('.tab-content');
    function updateActiveTab(path) {
        var targetId = 'stats'; // Default tab
        switch (path) {
            case '/coverage':
                targetId = 'coverage';
                break;
            case '/analysis':
                targetId = 'analysis';
                break;
            case '/efficiency':
                targetId = 'efficiency';
                break;
            case '/':
            default:
                targetId = 'stats';
                break;
        }
        console.log("Updating tabs for path: ".concat(path, ", Target ID: ").concat(targetId));
        // Update link styles and section visibility
        tabLinks.forEach(function (link) { return link.classList.toggle('active', link.dataset.target === targetId); });
        contentSections.forEach(function (section) { return section.style.display = section.id === targetId ? 'flex' : 'none'; }); // Use flex for tab content
        // --- Initialize specific tabs only when they become active ---
        try {
            if (targetId === 'analysis' && !analysisTabInitialized) {
                (0, analysis_1.initializeAnalysisTab)();
                analysisTabInitialized = true; // Mark as initialized after call
            }
            else if (targetId === 'efficiency' && !efficiencyTabInitialized) {
                // Initialize efficiency tab (async function)
                (0, efficiency_1.initializeEfficiencyTab)().then(function () {
                    efficiencyTabInitialized = true; // Mark as initialized after promise resolves
                }).catch(function (error) {
                    console.error("Failed to initialize Efficiency Tab:", error);
                    // Optionally display an error message within the efficiency tab section
                });
            }
            // Add initialization logic for other tabs (stats, coverage) here if needed
        }
        catch (error) {
            console.error("Error initializing tab \"".concat(targetId, "\":"), error);
            // Optionally display a general error or log more details
        }
    }
    // --- Event Listeners ---
    // Initial tab setup based on URL
    updateActiveTab(window.location.pathname);
    // Handle clicks on tab links
    tabsContainer.addEventListener('click', function (event) {
        var link = event.target.closest('a[data-target]');
        if (link && link.dataset.target) { // Ensure link and target exist
            event.preventDefault();
            var targetPath = link.getAttribute('href');
            if (targetPath && window.location.pathname !== targetPath) {
                // Use pushState for SPA-like navigation
                history.pushState({ path: targetPath }, '', targetPath);
                updateActiveTab(targetPath);
            }
            else if (targetPath) {
                // If clicking the already active tab, ensure it's initialized (e.g., after a refresh)
                updateActiveTab(targetPath);
            }
        }
    });
    // Handle browser back/forward buttons
    window.addEventListener('popstate', function (event) {
        var _a, _b;
        // Use event.state?.path or fallback to current pathname
        var path = (_b = (_a = event.state) === null || _a === void 0 ? void 0 : _a.path) !== null && _b !== void 0 ? _b : window.location.pathname;
        updateActiveTab(path);
    });
}
// --- Main Initialization ---
document.addEventListener('DOMContentLoaded', function () {
    console.log("DOM fully loaded and parsed.");
    setupTabs();
    // --- Trigger initializations if the relevant tab is already visible ---
    // (This handles the case where the page loads directly on a specific tab,
    // e.g., after a refresh or direct link)
    var activeSection = document.querySelector('.tab-content[style*="display: flex"]');
    if (activeSection) {
        try {
            if (activeSection.id === 'analysis' && !analysisTabInitialized) {
                (0, analysis_1.initializeAnalysisTab)();
                analysisTabInitialized = true;
            }
            else if (activeSection.id === 'efficiency' && !efficiencyTabInitialized) {
                (0, efficiency_1.initializeEfficiencyTab)().then(function () {
                    efficiencyTabInitialized = true;
                }).catch(function (error) { return console.error("Failed to initialize Efficiency Tab on direct load:", error); });
            }
            // Add checks for other tabs if they have initializers
        }
        catch (error) {
            console.error("Error initializing active tab \"".concat(activeSection.id, "\" on load:"), error);
        }
    }
});
//# sourceMappingURL=main.js.map