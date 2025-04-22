using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;

// --- Configuration ---
var config = new ConfigurationBuilder()
    .AddCommandLine(args)
    .Build();

string resultsDirRelative = config["path"] ?? "test-results"; // Relative path for local results
string port = config["port"] ?? "5080";
string url = $"http://localhost:{port}";

// --- PATHS ---
// Directory where the 'dotnet unicorn-toolbox' command is executed
string currentWorkingDirectory = Directory.GetCurrentDirectory();

// Base directory where the tool's DLL is actually running from (inside .nuget cache or bin folder)
string toolAssemblyDirectory = AppContext.BaseDirectory;

// Path to the wwwroot FOLDER PACKAGED WITH THE TOOL
string packagedWwwRootPath = Path.Combine(toolAssemblyDirectory, "wwwroot");

// Path to the test results FOLDER IN THE CURRENT WORKING DIRECTORY
string localResultsPath = Path.GetFullPath(Path.Combine(currentWorkingDirectory, resultsDirRelative));
// --- END PATHS ---

var toolVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "N/A";

// --- WebApplication Builder ---
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // *** Explicitly set the WebRootPath to the TOOL'S packaged wwwroot ***
    WebRootPath = packagedWwwRootPath,
    // *** ContentRootPath is usually the same as where the assembly is for tools ***
    ContentRootPath = toolAssemblyDirectory
});

builder.WebHost.UseUrls(url);
builder.Services.AddLogging(logging => logging.AddConsole());

var app = builder.Build();

// --- Middleware Pipeline ---
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("--------------------------------------");
logger.LogInformation($"Unicorn Report Server (Toolbox) v{toolVersion}");
logger.LogInformation($"Tool Assembly Location: {toolAssemblyDirectory}");
logger.LogInformation($"Current Working Directory: {currentWorkingDirectory}");

// Check if the packaged UI exists where we expect it
if (!Directory.Exists(packagedWwwRootPath))
{
    logger.LogError($"FATAL: Packaged UI directory not found at expected location: {packagedWwwRootPath}");
    logger.LogError("The tool might not have been packaged correctly. Ensure 'wwwroot' is included.");
    return 1; // Exit with error
}
else
{
    logger.LogInformation($"Serving custom UI from: {packagedWwwRootPath}");
}

// 1. Serve Static Files (CSS, JS) for the custom UI from the TOOL'S wwwroot
// This uses the WebRootPath we explicitly set in the builder options.
app.UseStaticFiles();

// 2. Serve Raw Test Results under /results path from the LOCAL results folder
if (Directory.Exists(localResultsPath))
{
    logger.LogInformation($"Serving raw results from local path: {localResultsPath} under /results/");
    app.UseFileServer(new FileServerOptions
    {
        // Use a PhysicalFileProvider pointing to the LOCAL results directory
        FileProvider = new PhysicalFileProvider(localResultsPath),
        RequestPath = "/results", // Map to the /results URL
        EnableDirectoryBrowsing = true
    });
}
else
{
    logger.LogWarning($"Local results directory not found: {localResultsPath}");
    logger.LogWarning($"Access to '{url}/results/' will likely yield 404.");
}

// 3. SPA Fallback: Serve the TOOL'S index.html for unmatched routes
// This will look for "index.html" inside the WebRootPath (packagedWwwRootPath).
app.MapFallbackToFile("index.html");

// --- Start the server ---
logger.LogInformation("--------------------------------------");
logger.LogInformation($"Report server started. Access custom UI at: {url}");
if (Directory.Exists(localResultsPath))
{
    logger.LogInformation($"Access raw results at: {url}/results/");
}
logger.LogInformation("Press CTRL+C to stop the server.");
logger.LogInformation("--------------------------------------");

await app.RunAsync();

return 0;