using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Unicorn.Toolbox.Models.Launch
{
    public class TrxParser
    {
        private readonly XDocument _trx;
        private readonly XNamespace xNamespace;

        public TrxParser(string fileName)
        {
            _trx = XDocument.Load(fileName);
            xNamespace = _trx.Root.GetDefaultNamespace();
        }

        public ImmutableList<TestResult> GetTestsData()
        {
            var tests = new List<TestResult>();
            var xUnitTests = _trx.Root.Element(xNamespace + "TestDefinitions").Elements(xNamespace + "UnitTest");
            var xTestLists = _trx.Root.Element(xNamespace + "TestLists").Elements(xNamespace + "TestList");
            var results = _trx.Root.Element(xNamespace + "Results").Elements(xNamespace + "UnitTestResult");

            foreach (var xUnitTest in xUnitTests)
            {
                var id = xUnitTest.Attribute("id").Value;

                var name = xUnitTest.Attribute("name").Value;
                var description = xUnitTest.Element(xNamespace + "Description")?.Value;
                var title = description ?? name;

                var xResult = results.First(r => r.Attribute("testId").Value.Equals(id));
                var startTime = Convert.ToDateTime(xResult.Attribute("startTime").Value);
                var endTime = Convert.ToDateTime(xResult.Attribute("endTime").Value);
                var suiteId = xResult.Attribute("testListId").Value;
                var suiteName = xTestLists.First(tl => tl.Attribute("id").Value.Equals(suiteId)).Attribute("name").Value;

                Status outcome;

                switch(xResult.Attribute("outcome").Value)
                {
                    case "Failed":
                        outcome = Status.Failed;
                        break;
                    case "Inconclusive":
                        outcome = Status.Skipped;
                        break;
                    default:
                        outcome = Status.Passed;
                        break;
                }

                string errorMessage = null;
                
                if (outcome.Equals(Status.Failed))
                {
                    errorMessage = xResult
                        .Element(xNamespace + "Output")
                        .Element(xNamespace + "ErrorInfo")
                        .Element(xNamespace + "Message")
                        .Value;
                }

                TestResult testResult = 
                    new TestResult(title, outcome, startTime, endTime, suiteId, suiteName, errorMessage);

                tests.Add(testResult);
            }

            return tests.ToImmutableList();
        }

        public TimeSpan GetLaunchDuration()
        {
            XElement timeNode = _trx.Root.Element(xNamespace + "Times");

            DateTime finish = Convert.ToDateTime(timeNode.Attribute("finish").Value);
            DateTime start = Convert.ToDateTime(timeNode.Attribute("start").Value);
            return finish - start;
        }
    }
}
