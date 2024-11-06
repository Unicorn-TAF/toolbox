using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using Unicorn.Toolbox.Stats;

namespace Unicorn.Toolbox.Models.Coverage
{
    public class SpecsCoverage
    {
        public void ReadSpecs(string jsonFile)
        {
            DataContractJsonSerializer formatter = new DataContractJsonSerializer(typeof(AppSpecs));

            using (FileStream fs = new FileStream(jsonFile, FileMode.Open))
            {
                Specs = formatter.ReadObject(fs) as AppSpecs;
            }
        }

        public AppSpecs Specs { get; private set; }

        public void Analyze(List<SuiteInfo> suites)
        {
            foreach (CoverageModule module in Specs.Modules)
            {
                module.Suites = suites.Where(s => s.Tags.Intersect(module.Features, StringComparer.InvariantCultureIgnoreCase).Any());
            }
        }
    }
}
