using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Toolbox.Models.Launch
{
    /// <summary>
    /// Interaction logic for FailedTestsGroup.xaml
    /// </summary>
    public class FailedTestsGroup
    {
        public FailedTestsGroup(IEnumerable<TestResult> tests)
        {
            Count = tests.Count();
            ErrorMessage = tests.First().ErrorMessage;

            var uniqueSuites = new HashSet<string>(tests.Select(r => r.SuiteName));

            FailedResults = new Dictionary<string, IEnumerable<TestResult>>();

            foreach (var suite in uniqueSuites)
            {
                var matchingResults = tests.Where(r => r.SuiteName.Equals(suite));
                FailedResults.Add(suite, matchingResults);
            }
        }

        public Dictionary<string, IEnumerable<TestResult>> FailedResults { get; }

        public int Count { get; }

        public string ErrorMessage { get; }

        //private void LabelFoundFailedTests_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    var window = new DialogHost("Failed tests by error message filter")
        //    { 
        //        DataContext = new DialogHostViewModel(FailedResults) 
        //    };

        //    //window.SetDataSource(FailedResults);
        //    window.ShowDialog();
        //}
    }
}
