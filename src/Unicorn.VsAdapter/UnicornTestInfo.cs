using System;

namespace Unicorn.TestAdapter
{
    [Serializable]
    public struct UnicornTestInfo
    {
        public UnicornTestInfo(string fullName, string displayName, string methodName, string className)
        {
            this.FullName = fullName;
            this.DisplayName = displayName;
            this.MethodName = methodName;
            this.ClassName = className;
        }

        public string FullName { get; private set; }

        public string DisplayName { get; private set; }

        public string ClassName { get; private set; }

        public string MethodName { get; private set; }
    }
}
