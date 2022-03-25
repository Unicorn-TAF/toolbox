using System;
using System.Collections.Generic;
using System.IO;
using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Extensibility;
using UTesting = Unicorn.Taf.Core.Testing;

namespace Unicorn.ReportPortalAgent
{
    /// <summary>
    /// Report portal listener, which handles reporting stuff for all test items.
    /// </summary>
    public partial class ReportPortalListener
    {
        private const string Prefix = nameof(ReportPortalListener) + ": ";
        private const string BaseMessage = "ReportPortal exception was thrown.";

        private readonly IExtensionManager _extensionManager = new ExtensionManager();
        private readonly IClientService _rpService;

        private readonly Dictionary<UTesting.Status, Status> _statusMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportPortalListener"/> class.<br/>
        /// Initialization of RP service based on config.
        /// </summary>
        public ReportPortalListener()
        {
            var baseDir = Path.GetDirectoryName(new Uri(typeof(ReportPortalListener).Assembly.Location).LocalPath);

            Config = new ConfigurationBuilder().AddDefaults(baseDir).Build();

            _rpService = new ReportPortal.Shared.Reporter.Http.ClientServiceBuilder(Config).Build();
            _extensionManager.Explore(baseDir);

            _statusMap = new Dictionary<UTesting.Status, Status>
            {
                { UTesting.Status.Passed, Status.Passed },
                { UTesting.Status.Failed, Status.Failed },
                { UTesting.Status.Skipped, Status.Skipped },
            };
        }

        /// <summary>
        /// Gets or sets report portal configuration.
        /// </summary>
        public IConfiguration Config { get; }

        /// <summary>
        /// Gets or sets id of existing Report Portal launch.
        /// </summary>
        public string ExistingLaunchId { get; set; }

        /// <summary>
        /// Adds attachment to test (as bytes).
        /// </summary>
        /// <param name="test"><see cref="UTesting.Test"/> instance</param>
        /// <param name="text">attachment text</param>
        /// <param name="attachmentName">attachment name</param>
        /// <param name="mime">mime type</param>
        /// <param name="content">content in bytes</param>
        public void ReportAddAttachment(UTesting.Test test, string text, string attachmentName, string mime, byte[] content)
        {
            if (_testFlowIds.ContainsKey(test.Outcome.Id))
            {
                AddAttachment(test.Outcome.Id, LogLevel.Info, text, attachmentName, mime, content);
            }
        }
    }
}
