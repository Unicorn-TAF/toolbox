#if NET || NETCOREAPP
using System.Reflection;
using Unicorn.Taf.Api;

namespace Unicorn.Toolbox.Stats
{
    public class LoadContextCollector : IDataCollector
    {
        private readonly bool _considerParameterization;

        public LoadContextCollector(bool considerParameterization)
        {
            _considerParameterization = considerParameterization;
        }

        public IOutcome CollectData(Assembly assembly) =>
            AutomationDataCollector.CollectData(assembly, _considerParameterization);
    }
}
#endif