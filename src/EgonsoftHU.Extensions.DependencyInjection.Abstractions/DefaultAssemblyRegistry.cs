// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

using EgonsoftHU.Extensions.Bcl;
using EgonsoftHU.Extensions.DependencyInjection.Internal;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    internal sealed partial class DefaultAssemblyRegistry : IAssemblyRegistry
    {
        private readonly Dictionary<string, Assembly> assembliesByName =
#if LANGVERSION12_0_OR_GREATER
            []
#else
            new()
#endif
            ;

        private DefaultAssemblyRegistryOptions options = new();

        private ILogger logger = NullLogger.Instance;

        private Dictionary<string, List<string>> relevantAssemblyLocationsByFileName =
#if LANGVERSION12_0_OR_GREATER
            []
#else
            new()
#endif
            ;

        /// <inheritdoc/>
        public IEnumerable<Assembly> GetAssemblies()
        {
            using IDisposable? scope = logger.Here();

            Log.RegisteredAssemblyCount(logger, assembliesByName.Count);

            var assemblies = new List<Assembly>();

            foreach (Assembly assembly in assembliesByName.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value))
            {
                Log.RegisteredAssembly(
                    logger,
                    assembly.GetName().FullName.DefaultIfNullOrWhiteSpace(LogConstants.Unknown),
                    assembly.SafeGetLocation().DefaultIfNullOrWhiteSpace(LogConstants.Unknown)
                );

                assemblies.Add(assembly);
            }

            return assemblies;
        }

        internal void Configure(Action<DefaultAssemblyRegistryOptions> configureAction)
        {
            options = new();
            configureAction?.Invoke(options);

            logger = options.LoggerFactory.CreateLogger<DefaultAssemblyRegistry>();
        }

        internal void Initialize()
        {
            relevantAssemblyLocationsByFileName =
                AssemblyFinder
                    .Instance
                    .FindAssemblies(options)
                    .GroupBy(path => Path.GetFileName(path))
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(path => Path.GetFullPath(path).DefaultIfNullOrWhiteSpace(String.Empty)).ToList(),
                        StringComparer.OrdinalIgnoreCase
                    );

            var assemblies =
                new List<Assembly?>(AppDomain.CurrentDomain.GetAssemblies())
                {
                    Assembly.GetEntryAssembly()
                };

            foreach (Assembly assembly in assemblies.OfType<Assembly>().Where(IsRelevantAssembly))
            {
                RegisterAssembly(assembly);
            }

            LoadAssemblies();
        }

        private void RegisterAssembly(Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name.DefaultIfNullOrWhiteSpace(String.Empty);

            if (!assembliesByName.TryGetValue(assemblyName, out Assembly? _))
            {
                assembliesByName[assemblyName] = assembly;
            }
        }

        private bool IsRelevantAssembly(Assembly assembly)
        {
            return
                relevantAssemblyLocationsByFileName.ContainsKey(
                    Path.GetFileName(assembly.SafeGetLocation())
                    ??
                    String.Empty
                );
        }

        private void LoadAssemblies()
        {
            using IDisposable? scope = logger.Here();

            foreach (KeyValuePair<string, List<string>> assemblyLocationsByFileName in relevantAssemblyLocationsByFileName)
            {
                string fileName = assemblyLocationsByFileName.Key;

                string possibleAssemblyName = Path.GetFileNameWithoutExtension(fileName) ?? String.Empty;

                if (assembliesByName.ContainsKey(possibleAssemblyName))
                {
                    Log.AssemblyFoundByName(logger, possibleAssemblyName);
                    continue;
                }

                List<string> locations = assemblyLocationsByFileName.Value;

                var matchingLocations = locations.Intersect(assembliesByName.Values.Select(entry => entry.Location)).ToList();

                if (matchingLocations.Count > 0)
                {
                    Log.AssemblyFoundByLocation(logger, matchingLocations[0]);
                    continue;
                }

                string location = locations[0];

                Log.LoadAssembly(logger, location);

                RegisterAssembly(LoadAssembly(location));
            }
        }

        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "<Pending>")]
        [SuppressMessage("SonarQube", "S1481")]
        private static Assembly LoadAssembly(string assemblyFileName)
        {
            if (File.Exists(assemblyFileName))
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(assemblyFileName);
                    var assembly = Assembly.Load(assemblyName);
                    Type[] types = assembly.GetTypes();

                    return assembly;
                }
                catch (ReflectionTypeLoadException ex)
                {
                    var exceptions = new List<Exception>()
                    {
                        CreateFileLoadException(assemblyFileName)
                    };

                    exceptions.AddRange(ex.LoaderExceptions.OfType<Exception>());

                    throw new AggregateException(exceptions);
                }
                catch (Exception ex)
                {
                    throw CreateFileLoadException(assemblyFileName, ex);
                }
            }
            else
            {
                throw CreateFileNotFoundException(assemblyFileName);
            }
        }

        private static FileLoadException CreateFileLoadException(string assemblyFileName, Exception? ex = null)
        {
            return new FileLoadException($"Load failed for assembly: [{assemblyFileName}]", assemblyFileName, ex);
        }

        private static FileNotFoundException CreateFileNotFoundException(string assemblyFileName)
        {
            return new FileNotFoundException($"Load failed for assembly: [{assemblyFileName}]", assemblyFileName);
        }
    }
}
