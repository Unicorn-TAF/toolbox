using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Unicorn.Toolbox.LaunchAnalasys
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
            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            var xUnitTests = trx.Root.Element(ns + "TestDefinitions").Elements(ns + "UnitTest");
            var results = trx.Root.Element(ns + "Results").Elements(ns + "UnitTestResult");

            foreach (var xUnitTest in xUnitTests)
            {
                var id = xUnitTest.Attribute("id").Value;
                var description = xUnitTest.Element(ns + "Description").Value;

                var xResult = results.First(r => r.Attribute("testId").Value.Equals(id));
                var startTime = Convert.ToDateTime(xResult.Attribute("startTime").Value);
                var endTime = Convert.ToDateTime(xResult.Attribute("endTime").Value);
                var testListId = xResult.Attribute("testListId").Value;
                
                var testResult = new TestResult(description, startTime, endTime, testListId);

                tests.Add(testResult);
            }

            return tests;
        }
    }
}
