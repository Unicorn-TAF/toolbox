using System;
using Unicorn.TestAdapter.EventWatchers.EventArgs;

namespace Unicorn.TestAdapter.EventWatchers
{
    public interface ITestFileAddRemoveListener
    {
        event EventHandler<TestFileChangedEventArgs> TestFileChanged;
        void StartListeningForTestFileChanges();
        void StopListeningForTestFileChanges();
    }
}