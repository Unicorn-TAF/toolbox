using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Unicorn.Toolbox.Models.Launch;

public class TrxParser
{
    private readonly XDocument _trx;
    private readonly XNamespace _xNamespace;

    public TrxParser(string fileName)
    {
        _trx = XDocument.Load(fileName);
        _xNamespace = _trx.Root.GetDefaultNamespace();
    }

    public ImmutableList<TestResult> GetTestsData()
    {
        var tests = new List<TestResult>();
        var xUnitTests = _trx.Root.Element(_xNamespace + "TestDefinitions").Elements(_xNamespace + "UnitTest");
        var xTestLists = _trx.Root.Element(_xNamespace + "TestLists").Elements(_xNamespace + "TestList");
        var results = _trx.Root.Element(_xNamespace + "Results").Elements(_xNamespace + "UnitTestResult");

        foreach (var xUnitTest in xUnitTests)
        {
            var id = xUnitTest.Attribute("id").Value;

            var name = xUnitTest.Attribute("name").Value;
            var description = xUnitTest.Element(_xNamespace + "Description")?.Value;
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
                    .Element(_xNamespace + "Output")
                    .Element(_xNamespace + "ErrorInfo")
                    .Element(_xNamespace + "Message")
                    .Value;
            }

            TestResult testResult = new(title, outcome, startTime, endTime, suiteId, suiteName, errorMessage);

            tests.Add(testResult);
        }

        return tests.ToImmutableList();
    }

    public TimeSpan GetLaunchDuration()
    {
        XElement timeNode = _trx.Root.Element(_xNamespace + "Times");

        DateTime finish = Convert.ToDateTime(timeNode.Attribute("finish").Value);
        DateTime start = Convert.ToDateTime(timeNode.Attribute("start").Value);
        return finish - start;
    }
}
