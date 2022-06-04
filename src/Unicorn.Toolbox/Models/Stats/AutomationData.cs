using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unicorn.Taf.Api;
using Unicorn.Toolbox.Models.Stats.Filtering;

namespace Unicorn.Toolbox.Models.Stats
{
    [Serializable]
    public class AutomationData : IOutcome
    {
        public AutomationData()
        {
            SuitesInfos = new List<SuiteInfo>();
            UniqueTags = new HashSet<string>();
            UniqueCategories = new HashSet<string>();
            UniqueAuthors = new HashSet<string>();
            FilteredInfo = null;
        }

        public List<SuiteInfo> SuitesInfos { get; }

        public List<SuiteInfo> FilteredInfo { get; set; }

        public HashSet<string> UniqueTags { get; }

        public HashSet<string> UniqueCategories { get; }

        public HashSet<string> UniqueAuthors { get; }

        public void ClearFilters() =>
            FilteredInfo = SuitesInfos;

        public void FilterBy(ISuitesFilter filter)
        {
            FilteredInfo = filter.FilterSuites(FilteredInfo);

            if (filter is ITestsFilter)
            {
                for (int i = 0; i < FilteredInfo.Count; i++)
                {
                    var info = FilteredInfo[i];

                    info.SetTestInfo((filter as ITestsFilter).FilterTests(info.TestsInfos));
                    FilteredInfo[i] = info;
                }
            }
        }

        public void AddSuiteData(SuiteInfo suiteData)
        {
            SuitesInfos.Add(suiteData);
            UniqueTags.UnionWith(suiteData.Tags);

            var authors = from TestInfo ti 
                          in suiteData.TestsInfos
                          select ti.Author;

            UniqueAuthors.UnionWith(authors);

            foreach (var testInfo in suiteData.TestsInfos)
            {
                UniqueCategories.UnionWith(testInfo.Categories);
            }
        }

        public override string ToString()
        {
            StringBuilder statistics = new StringBuilder();

            statistics.Append($"suites: {SuitesInfos.Count}  |  ")
                .Append($"tests: {SuitesInfos.Sum(s => s.TestsInfos.Count)}  |  ")
                .Append($"features: {UniqueTags.Count}  |  ")
                .Append($"categories: {UniqueCategories.Count}  |  ")
                .Append($"authors: {UniqueAuthors.Count}");

            return statistics.ToString();
        }
    }
}
