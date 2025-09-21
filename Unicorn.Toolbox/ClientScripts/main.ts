// ClientScripts/main.ts

// Import initialization functions for each tab
import { initializeAnalysisTab } from './analysis/analysis.js';
import { initializeEfficiencyTab } from './efficiency/efficiency.js';

// --- REMOVE State Variables ---
// let analysisTabInitialized: boolean = false;
// let efficiencyTabInitialized: boolean = false;

// --- DOM Element Safe Access ---
function getElement<T extends HTMLElement>(selector: string): T | null {
    return document.querySelector<T>(selector);
}

// --- Tab Navigation Logic ---
function setupTabs(): void {
    const tabsContainer = getElement<HTMLDivElement>('.tabs');
    const contentContainer = getElement<HTMLElement>('#content');

    if (!tabsContainer || !contentContainer) {
        console.error("Could not find tabs container or content container.");
        return;
    }

    const tabLinks = tabsContainer.querySelectorAll<HTMLAnchorElement>('a[data-target]');
    const contentSections = contentContainer.querySelectorAll<HTMLElement>('.tab-content');

    function updateActiveTab(path: string): void {
        let targetId = 'stats';
        switch (path) {
            case '/template': targetId = 'template'; break;
            case '/analysis': targetId = 'analysis'; break;
            case '/efficiency': targetId = 'efficiency'; break;
            case '/': default: targetId = 'stats'; break;
        }
        console.log(`Updating tabs for path: ${path}, Target ID: ${targetId}`);

        // Update link styles and section visibility
        tabLinks.forEach(link => link.classList.toggle('active', link.dataset.target === targetId));
        contentSections.forEach(section => section.style.display = section.id === targetId ? 'flex' : 'none');

        // --- Initialize specific tabs only when they become active ---
        // --- REMOVE checks for initialization flags here ---
        try {
            if (targetId === 'analysis') {
                initializeAnalysisTab();
                // --- REMOVE flag setting ---
                // analysisTabInitialized = true;
            } else if (targetId === 'efficiency') {
                initializeEfficiencyTab().catch(error => { // Still catch errors from async init
                    console.error("Failed to initialize Efficiency Tab:", error);
                });
                // --- REMOVE flag setting ---
                // efficiencyTabInitialized = true;
            }
        } catch (error) {
            console.error(`Error initializing tab "${targetId}":`, error);
        }
    }

    // --- Event Listeners ---
    updateActiveTab(window.location.pathname); // Initial tab setup

    tabsContainer.addEventListener('click', (event: MouseEvent) => { // Handle clicks
        const link = (event.target as HTMLElement).closest<HTMLAnchorElement>('a[data-target]');
        if (link && link.dataset.target) {
            event.preventDefault();
            const targetPath = link.getAttribute('href');
            if (targetPath && window.location.pathname !== targetPath) {
                history.pushState({ path: targetPath }, '', targetPath);
                updateActiveTab(targetPath);
            } else if (targetPath) {
                updateActiveTab(targetPath); // Re-call to ensure initialization logic runs if needed
            }
        }
    });

    window.addEventListener('popstate', (event: PopStateEvent) => { // Handle back/forward
        const path = event.state?.path ?? window.location.pathname;
        updateActiveTab(path);
    });
}


// --- Main Initialization ---
document.addEventListener('DOMContentLoaded', () => {
    console.log("DOM fully loaded and parsed.");
    setupTabs();

    // --- Trigger initializations if the relevant tab is already visible ---
    // This logic can remain, as it ensures the correct initialize function
    // is called on direct load/refresh, and the function itself will handle
    // preventing re-initialization internally.
    const activeSection = document.querySelector<HTMLElement>('.tab-content[style*="display: flex"]');
    if (activeSection) {
        console.log(`Active section on load: ${activeSection.id}`); // Log which section is active
        try {
            if (activeSection.id === 'analysis') {
                initializeAnalysisTab(); // Call init
            } else if (activeSection.id === 'efficiency') {
                initializeEfficiencyTab().catch(error => console.error("Failed to initialize Efficiency Tab on direct load:", error)); // Call init
            }
        } catch (error) {
            console.error(`Error initializing active tab "${activeSection.id}" on load:`, error);
        }
    } else {
        console.log("No active section found on load based on style='display: flex'");
    }
});