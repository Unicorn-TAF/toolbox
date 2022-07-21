#if NETCOREAPP || NET
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Unicorn.Toolbox.Stats
{
    /// <summary>
    /// Provides with ability to manipulate with Unicorn test assembly in dedicated <see cref="AssemblyLoadContext"/>
    /// </summary>
    public class StatsAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _assemblyDirectory;
        private readonly List<Assembly> _loadedAssemblies;
        private readonly Dictionary<string, Assembly> _sharedAssemblies;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnicornAssemblyLoadContext"/> class 
        /// based on specified tests assembly directory and types shared across load contexts.
        /// </summary>
        /// <param name="contextDirectory">tests assembly directory</param>
        public StatsAssemblyLoadContext(string contextDirectory) : base(isCollectible: true)
        {
            _assemblyDirectory = contextDirectory;
            _loadedAssemblies = new List<Assembly>();
            _sharedAssemblies = new Dictionary<string, Assembly>();
        }

        /// <summary>
        /// Loads all assemblies from tests assembly directory except assemblies containing shared types.
        /// </summary>
        /// <param name="sharedTypes">types shared across load contexts (usually types from Taf.Api)</param>
        public StatsAssemblyLoadContext Initialize(params Type[] sharedTypes)
        {
            foreach (Type sharedType in sharedTypes)
            {
                _sharedAssemblies[Path.GetFileName(sharedType.Assembly.Location)] = sharedType.Assembly;
            }

            foreach (string dll in Directory.EnumerateFiles(_assemblyDirectory, "*.dll"))
            {
                if (!_sharedAssemblies.ContainsKey(Path.GetFileName(dll)))
                {
                    _loadedAssemblies.Add(LoadFromAssemblyPath(dll));
                }
            }

            return this;
        }

        /// <summary>
        /// Gets <see cref="Assembly"/> containing specified type from the load context.
        /// </summary>
        /// <param name="type">type belonging to desired assembly</param>
        /// <returns><see cref="Assembly"/> containing the type</returns>
        public Assembly GetAssemblyContainingType(Type type) =>
            _loadedAssemblies
            .First(a => a.GetName().Name.Equals(type.Assembly.GetName().Name, StringComparison.InvariantCulture));

        /// <summary>
        /// Gets <see cref="Assembly"/> by its <see cref="AssemblyName"/>.
        /// </summary>
        /// <param name="assemblyName">assembly name</param>
        /// <returns><see cref="Assembly"/> located at path</returns>
        public Assembly GetAssembly(AssemblyName assemblyName) =>
            _loadedAssemblies
            .First(a => a.GetName().FullName.Equals(assemblyName.FullName, StringComparison.InvariantCulture));

        /// <summary>
        /// Loads an assembly from specified path. 
        /// </summary>
        /// <param name="assemblyPath">path of assembly to load</param>
        public void LoadAssemblyFrom(string assemblyPath) =>
            _loadedAssemblies.Add(LoadFromAssemblyPath(assemblyPath));

        /// <summary>
        /// Loads an assembly with specified <see cref="AssemblyName"/>. 
        /// If assembly is in shared assemblies it's returned.
        /// </summary>
        /// <param name="assemblyName">assembly name</param>
        /// <returns><see cref="Assembly"/> instance</returns>
        protected override Assembly Load(AssemblyName assemblyName)
        {
            string fileName = $"{assemblyName.Name}.dll";

            if (_sharedAssemblies.ContainsKey(fileName))
            {
                return _sharedAssemblies[fileName];
            }

            return Assembly.Load(assemblyName);
        }
    }
}
#endif