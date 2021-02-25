using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unicorn.Toolbox.Analysis.Filtering;

namespace Unicorn.Toolbox.Analysis
{
    [Serializable]
    public class AutomationData
    {
        public AutomationData()
        {
            SuitesInfos = new List<SuiteInfo>();
            UniqueFeatures = new HashSet<string>();
            UniqueCategories = new HashSet<string>();
            UniqueAuthors = new HashSet<string>();
            FilteredInfo = null;
        }

        public List<SuiteInfo> SuitesInfos { get; protected set; }

        public List<SuiteInfo> FilteredInfo { get; set; }

        public HashSet<string> UniqueFeatures { get; protected set; }

        public HashSet<string> UniqueCategories { get; protected set; }

        public HashSet<string> UniqueAuthors { get; protected set; }

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
            UniqueFeatures.UnionWith(suiteData.Tags);

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

            statistics.Append($"suites: {SuitesInfos.Count}    |    ")
                .Append($"tests: {SuitesInfos.Sum(s => s.TestsInfos.Count)}    |    ")
                .Append($"features: {UniqueFeatures.Count}    |    ")
                .Append($"categories: {UniqueCategories.Count}    |    ")
                .Append($"authors: {UniqueAuthors.Count}");

            return statistics.ToString();
        }
    }
}
