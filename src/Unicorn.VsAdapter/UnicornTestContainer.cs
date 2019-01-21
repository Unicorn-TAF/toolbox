using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.TestWindow.Extensibility.Model;

namespace Unicorn.TestAdapter
{
    public class UnicornTestContainer : ITestContainer
    {
        private readonly ITestContainerDiscoverer discoverer;
        private readonly DateTime timeStamp;

        public UnicornTestContainer(ITestContainerDiscoverer discoverer, string source, Uri executorUri)
            :this(discoverer, source, executorUri, Enumerable.Empty<Guid>())
        {}

        public UnicornTestContainer(ITestContainerDiscoverer discoverer, string source, Uri executorUri, IEnumerable<Guid> debugEngines)
        {
            this.Source = source;
            this.ExecutorUri = executorUri;
            this.DebugEngines = debugEngines;
            this.discoverer = discoverer;
            this.TargetFramework = FrameworkVersion.None;
            this.TargetPlatform = Architecture.AnyCPU;
            this.timeStamp = GetTimeStamp();
        }

        private UnicornTestContainer(UnicornTestContainer copy)
            : this(copy.discoverer, copy.Source, copy.ExecutorUri)
        {
            this.timeStamp = copy.timeStamp;
        }

        private DateTime GetTimeStamp() => !String.IsNullOrEmpty(this.Source) && File.Exists(this.Source) ?
                File.GetLastWriteTime(this.Source) :
                DateTime.MinValue;

        public  string Source { get; set; }

        public  Uri ExecutorUri { get; set; }

        public  IEnumerable<Guid> DebugEngines { get; set; }

        public FrameworkVersion TargetFramework { get; set; }

        public Architecture TargetPlatform { get; set; }

        public bool IsAppContainerTestContainer => false;

        public ITestContainerDiscoverer Discoverer => discoverer;

        public override string ToString() =>
            this.ExecutorUri.ToString() + "/" + this.Source;

        public IDeploymentData DeployAppContainer() => null;

        public int CompareTo(ITestContainer other)
        {
            var testContainer = other as UnicornTestContainer;

            if (testContainer == null)
            {
                return -1;
            }

            var result = String.Compare(this.Source, testContainer.Source, StringComparison.OrdinalIgnoreCase);

            if (result != 0)
            {
                return result;
            }

            return this.timeStamp.CompareTo(testContainer.timeStamp);
        }

        public ITestContainer Snapshot() => new UnicornTestContainer(this);
    }
}
