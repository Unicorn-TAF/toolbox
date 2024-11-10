using System;
using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Toolbox.Stats;

[Serializable]
public struct TestInfo
{
    public const string NoCategory = "[without-category]";

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

    public string Name { get; }

    public string Author { get; }

    public bool Disabled { get; }

    public List<string> Categories { get; }
}
