/**
 * Creates a debounced function that delays invoking func until after wait milliseconds have elapsed
 * since the last time the debounced function was invoked. The func is invoked with the last arguments provided.
 *
 * @param func The function to debounce.
 * @param waitFor The number of milliseconds to delay.
 * @returns Returns the new debounced function.
 *
 * Example Usage:
 * const debouncedLayout = debounce(() => {
 *   console.log("Window resized!");
 *   // Perform layout calculation here
 * }, 250);
 * window.addEventListener('resize', debouncedLayout);
 */
export function debounce<T extends (...args: any[]) => void>(
    func: T,
    waitFor: number
): (...args: Parameters<T>) => void {
    let timeoutId: ReturnType<typeof setTimeout> | null = null;

    // Return a function that takes the same arguments as the original function
    return (...args: Parameters<T>): void => {
        // If there's an existing timeout, clear it
        if (timeoutId !== null) {
            clearTimeout(timeoutId);
        }

        // Set a new timeout to run the original function after 'waitFor' ms
        timeoutId = setTimeout(() => {
            // Clear the timeoutId before running the function,
            // so the next call can start a new timeout
            timeoutId = null;
            func(...args); // Call the original function with the last provided arguments
        }, waitFor);
    };
}

// You can add other utility functions here if needed later.