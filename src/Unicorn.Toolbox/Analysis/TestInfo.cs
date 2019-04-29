using System;
using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Toolbox.Analysis
{
    [Serializable]
    public struct TestInfo
    {
        public const string NoCategory = "<CATEGORY NOT SPECIFIED>";

        public TestInfo(string testName, string author, IEnumerable<string> categories)
        {
            this.Name = testName;
            this.Author = author;
            this.Categories = new List<string>(categories);

            if (!this.Categories.Any())
            {
                this.Categories.Add(NoCategory);
            }
        }

        public string Name { get; set; }

        public string Author { get; set; }

        public List<string> Categories { get; set; }
    }
}
