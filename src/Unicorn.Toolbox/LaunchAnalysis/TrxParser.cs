using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Unicorn.Toolbox.LaunchAnalysis
{
    public class TrxParser
    {
        private readonly XDocument trx;

        public TrxParser(string fileName)
        {
            trx = XDocument.Load(fileName);
        }

        public List<TestResult> GetAllTests()
        {
            var tests = new List<TestResult>();
            XNamespace ns = trx.Root.GetDefaultNamespace();
            var xUnitTests = trx.Root.Element(ns + "TestDefinitions").Elements(ns + "UnitTest");
            var xTestLists = trx.Root.Element(ns + "TestLists").Elements(ns + "TestList");
            var results = trx.Root.Element(ns + "Results").Elements(ns + "UnitTestResult");

            foreach (var xUnitTest in xUnitTests)
            {
                var id = xUnitTest.Attribute("id").Value;

                var name = xUnitTest.Attribute("name").Value;
                var description = xUnitTest.Element(ns + "Description")?.Value;
                var title = description == null ? name : description;

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

            return tests;
        }
    }
}
