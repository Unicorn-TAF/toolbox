using System;
using System.Collections.Generic;
using System.Text;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using UTesting = Unicorn.Taf.Core.Testing;
using ULogging = Unicorn.Taf.Core.Logging;

namespace Unicorn.ReportPortalAgent
{
    public partial class ReportPortalListener
    {
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

                if (commonSuitesTags != null)
                {
                    foreach (var tag in commonSuitesTags)
                    {
                        startSuiteRequest.Attributes.Add(GetAttribute(tag));
                    }
                }

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
                            attributes.Add(GetAttribute(tag));
                        }
                    }

                    if (commonSuitesTags != null)
                    {
                        foreach (var tag in commonSuitesTags)
                        {
                            attributes.Add(GetAttribute(tag));
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