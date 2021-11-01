using System;
using System.Collections.Generic;
using System.Text;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using UTesting = Unicorn.Taf.Core.Testing;
using ULogging = Unicorn.Taf.Core.Logging;
using ReportPortal.Shared.Reporter;

namespace Unicorn.ReportPortalAgent
{
    public partial class ReportPortalListener
    {
        private readonly Dictionary<Guid, ITestReporter> _suitesFlow = new Dictionary<Guid, ITestReporter>();
        private readonly Dictionary<Guid, string> _suitesSetNames = new Dictionary<Guid, string>();

        internal void StartSuite(UTesting.TestSuite suite)
        {
            try
            {
                var id = suite.Outcome.Id;
                var parentId = Guid.Empty;
                var name = suite.Outcome.Name;

                if (!string.IsNullOrEmpty(suite.Outcome.DataSetName))
                {
                    name += "[" + suite.Outcome.DataSetName + "]";
                    _suitesSetNames[id] = name;
                }

                var startSuiteRequest = new StartTestItemRequest
                {
                    StartTime = DateTime.UtcNow,
                    Name = name,
                    Type = TestItemType.Suite
                };

                if (!string.IsNullOrEmpty(ExistingLaunchId))
                {
                    startSuiteRequest.LaunchUuid = ExistingLaunchId;
                }

                startSuiteRequest.Attributes = new List<ItemAttribute>
                {
                    GetAttribute(MachineAttribute, Environment.MachineName)
                };

                var test = parentId.Equals(Guid.Empty) || !_suitesFlow.ContainsKey(parentId) ?
                    launchReporter.StartChildTestReporter(startSuiteRequest) :
                    _suitesFlow[parentId].StartChildTestReporter(startSuiteRequest);

                _suitesFlow[id] = test;
            }
            catch (Exception exception)
            {
                ULogging.Logger.Instance.Log(
                    ULogging.LogLevel.Warning,
                    Prefix + BaseMessage + Environment.NewLine + exception);
            }
        }

        internal void FinishSuite(UTesting.TestSuite suite)
        {
            try
            {
                var id = suite.Outcome.Id;
                var result = suite.Outcome.Result;
                var parentId = Guid.Empty;

                if (parentId.Equals(Guid.Empty) && _suitesFlow.ContainsKey(id))
                {
                    var attributes = new List<ItemAttribute>
                    {
                        GetAttribute(MachineAttribute, Environment.MachineName)
                    };

                    if (!string.IsNullOrEmpty(suite.Outcome.DataSetName))
                    {
                        attributes.Add(GetAttribute("parameterized"));
                    }

                    // adding tags to suite
                    if (suite.Tags != null)
                    {
                        foreach (var tag in suite.Tags)
                        {
                            attributes.Add(GetAttribute(tag.ToLowerInvariant()));
                        }
                    }

                    // adding description to suite
                    var description = new StringBuilder();

                    foreach (var key in suite.Metadata.Keys)
                    {
                        var value = suite.Metadata[key];
                        var appendString = 
                            value.StartsWith("http", StringComparison.InvariantCultureIgnoreCase) ?
                            $"[{key}]({value})" : 
                            $"{key}: {value}";
                        
                        description.AppendLine(appendString);
                    }

                    // finishing suite
                    var finishSuiteRequest = new FinishTestItemRequest
                    {
                        EndTime = DateTime.UtcNow,
                        Description = description.ToString(),
                        Attributes = attributes,
                        Status = result.Equals(UTesting.Status.Skipped) ? Status.Failed : _statusMap[result]
                    };
                        
                    _suitesFlow[id].Finish(finishSuiteRequest);
                    _suitesFlow.Remove(id);
                    _suitesSetNames.Remove(id);
                }
            }
            catch (Exception exception)
            {
                ULogging.Logger.Instance.Log(
                    ULogging.LogLevel.Warning,
                    Prefix + BaseMessage + Environment.NewLine + exception);
            }
        }
    }
}