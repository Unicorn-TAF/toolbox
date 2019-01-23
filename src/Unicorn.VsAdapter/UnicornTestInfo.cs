using System;

namespace Unicorn.TestAdapter
{
    [Serializable]
    public struct UnicornTestInfo
    {
        public UnicornTestInfo(string fullName, string displayName)
        {
            this.FullName = fullName;
            this.DisplayName = displayName;
        }

        public string FullName { get; set; }

        public string DisplayName { get; set; }
    }
}
