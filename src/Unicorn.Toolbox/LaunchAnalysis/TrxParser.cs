using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Unicorn.Toolbox.LaunchAnalysis
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

        public ImmutableList<TestResult> AllTests
        {
            get
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
                    var testListId = xResult.Attribute("testListId").Value;
                    var testListName = xTestLists.First(tl => tl.Attribute("id").Value.Equals(testListId)).Attribute("name").Value;


                    if (xResult.Attribute("duration").Value.Equals("00:00:00.0000000", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    var testResult = new TestResult(title, startTime, endTime, testListId, testListName);

                    tests.Add(testResult);
                }

                return tests.ToImmutableList();
            }
        }

        public TimeSpan TrxDuration
        {
            get
            {
                var timeNode = _trx.Root.Element(xNamespace + "Times");

                var finish = Convert.ToDateTime(timeNode.Attribute("finish").Value);
                var start = Convert.ToDateTime(timeNode.Attribute("start").Value);
                return finish - start;
            }
        }
    }
}
