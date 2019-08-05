using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Toolbox.LaunchAnalysis
{
    public class LaunchResult
    {
        public LaunchResult()
        {
            this.ResultsList = new List<List<TestResult>>();
        }

        public List<List<TestResult>> ResultsList { get; } 

        public void AppendResultsFromTrx(string trxFile)
        {
            var results = new TrxParser(trxFile).GetAllTests();

            if (results.Any())
            {
                ResultsList.Add(results);
            }
        }
    }
}
