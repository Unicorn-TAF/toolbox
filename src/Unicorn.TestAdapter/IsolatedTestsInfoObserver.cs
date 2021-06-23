using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing.Attributes;

namespace Unicorn.TestAdapter
{
    /// <summary>
    /// Represents serializable test information object for cross domain usage.
    /// </summary>
    [Serializable]
    public struct TestInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestInfo"/> struct.
        /// </summary>
        /// <param name="fullName">full test name (reflected type + test method name)</param>
        /// <param name="displayName">test display name</param>
        /// <param name="methodName">test method name</param>
        /// <param name="className">test class name</param>
        /// <param name="author">test author</param>
        /// <param name="categories">test categories</param>
        internal TestInfo(string fullName, string methodName, 
            string className, string author, string categories)
        {
            FullName = fullName;
            MethodName = methodName;
            ClassName = className;
            Author = author;
            Categories = categories;
        }

        /// <summary>
        /// Gets test full name (reflected type full name and test method name).
        /// </summary>
        internal string FullName { get; }

        /// <summary>
        /// Gets test class name.
        /// </summary>
        internal string ClassName { get; }

        /// <summary>
        /// Gets test method name.
        /// </summary>
        internal string MethodName { get; }

        /// <summary>
        /// Gets test author.
        /// </summary>
        internal string Author { get; }

        /// <summary>
        /// Gets test category.
        /// </summary>
        internal string Categories { get; }
    }

    /// <summary>
    /// Provides with ability to get <see cref="TestInfo"/> data from specified assembly in separate AppDomain.
    /// </summary>
    public class IsolatedTestsInfoObserver : MarshalByRefObject
    {
        /// <summary>
        /// Gets list of <see cref="TestInfo"/> from specified assembly in separate AppDomain.
        /// </summary>
        /// <param name="assembly">test assembly file</param>
        /// <returns>test info list</returns>
        public List<TestInfo> GetTests(string assembly)
        {
            var bytes = File.ReadAllBytes(assembly);
            var testsAssembly = Assembly.Load(bytes);

            var tests = TestsObserver.ObserveTests(testsAssembly);
            var infos = new List<TestInfo>();

            foreach (var unicornTest in tests)
            {
                var testAttribute = unicornTest
                    .GetCustomAttribute(typeof(TestAttribute), true) as TestAttribute;

                if (testAttribute != null)
                {
                    infos.Add(GetTestInfo(unicornTest, testAttribute.Title));
                }
            }

            return infos;
        }

        private TestInfo GetTestInfo(MethodInfo methodInfo, string title)
        {
            var methodName = methodInfo.Name;
            var className = methodInfo.DeclaringType.FullName;
            var fullName = AdapterUtilities.GetFullTestMethodName(methodInfo);

            var authorAttribute = methodInfo
                .GetCustomAttribute(typeof(AuthorAttribute), true) as AuthorAttribute;

            var author = authorAttribute != null ? authorAttribute.Author : null;

            var categoryAttributes = methodInfo
                .GetCustomAttributes(typeof(CategoryAttribute), true) as CategoryAttribute[];

            var categories = categoryAttributes.Any() ? 
                string.Join(",", categoryAttributes.Select(a => a.Category)) : 
                null;

            return new TestInfo(fullName, methodName, className, author, categories);
        }
    }
}
