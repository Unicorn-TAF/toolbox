using System;
using Unicorn.TestAdapter.EventWatchers.EventArgs;

namespace Unicorn.TestAdapter.EventWatchers
{
    public interface ITestFilesUpdateWatcher
    {
        event EventHandler<TestFileChangedEventArgs> FileChangedEvent;
        void AddWatch(string path);
        void RemoveWatch(string path);
    }
}