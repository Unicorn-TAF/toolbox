using System;
using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Toolbox.Stats;

[Serializable]
public struct SuiteInfo
{
    public const string NoTag = "[without-tag]";

    public SuiteInfo(string suiteName, IEnumerable<string> features, Dictionary<string, string> metadata)
    {
        Name = suiteName;
        TestsInfos = new List<TestInfo>();
        Tags = new List<string>(features);
        Metadata = metadata;

        if (!Tags.Any())
        {
            Tags.Add(NoTag);
        }
    }

    public string Name { get; }

    public List<TestInfo> TestsInfos { get; set; }

    public List<string> Tags { get; }

    public Dictionary<string, string> Metadata { get; }

    public void SetTestInfo(List<TestInfo> newInfos) =>
        TestsInfos = newInfos;
}
