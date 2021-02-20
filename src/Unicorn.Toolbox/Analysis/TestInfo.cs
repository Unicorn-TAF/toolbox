using System;
using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Toolbox.Analysis
{
    [Serializable]
    public struct TestInfo
    {
        public const string NoCategory = "<CATEGORY NOT SPECIFIED>";

        public TestInfo(string testName, string author, bool disabled, IEnumerable<string> categories)
        {
            Name = testName;
            Author = author;
            Disabled = disabled;
            Categories = new List<string>(categories);

            if (!Categories.Any())
            {
                Categories.Add(NoCategory);
            }
        }

        public string Name { get; set; }

        public string Author { get; set; }

        public bool Disabled { get; set; }

        public List<string> Categories { get; set; }

        public string CategoriesString => string.Join("\n", Categories).ToLowerInvariant();

    }
}
