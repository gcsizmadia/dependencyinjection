// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

using EgonsoftHU.Extensions.Bcl;
using EgonsoftHU.Extensions.Bcl.Constants;
using EgonsoftHU.Extensions.DependencyInjection.Logging;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    /// <summary>
    /// Default implementation of the <see cref="IAssemblyRegistry"/> interface.
    /// </summary>
    public sealed partial class DefaultAssemblyRegistry : IAssemblyRegistry
    {
        private static readonly string[] Separator =
#if LANGVERSION12_0_OR_GREATER
            [Strings.CommaSpaceSeparator]
#else
            new[] { Strings.CommaSpaceSeparator }
#endif
            ;

        private readonly Dictionary<string, AssemblyRegistryEntry> assemblies =
#if LANGVERSION12_0_OR_GREATER
            []
#else
            new()
#endif
            ;

        private DefaultAssemblyRegistryOptions options = new();

        private ILogger logger = NullLogger.Instance;

        internal DefaultAssemblyRegistry()
        {
            AppDomain
                .CurrentDomain
                .GetAssemblies()
                .ToList()
                .ForEach(RegisterAssembly);

            RegisterAssembly(Assembly.GetEntryAssembly());

            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            LoadAssemblies();
        }

        /// <summary>
        /// Resets and reconfigures the options and the logger.
        /// </summary>
        /// <param name="configureAction">The delegate that configures the options.</param>
        internal void Configure(Action<DefaultAssemblyRegistryOptions> configureAction)
        {
            options = new();
            configureAction?.Invoke(options);

            logger = options.LoggerFactory.CreateLogger(GetType());
        }

        /// <inheritdoc/>
        public IEnumerable<Assembly> GetAssemblies()
        {
            using IDisposable? scope = logger.Here();

            Assembly[] relevantAssemblies =
                assemblies
                    .Select(kvp => kvp.Value.Assembly)
                    .OrderBy(GetAssemblyFullName)
                    .ToArray();

            Log.RegisteredAssemblyCount(logger, relevantAssemblies.Length);

            foreach (Assembly assembly in relevantAssemblies)
            {
                Log.Assembly(logger, GetAssemblyFullName(assembly), assembly.SafeGetLocation().DefaultIfNullOrWhiteSpace(LogConstants.Unknown));
            }

            return relevantAssemblies;
        }

        private void RegisterAssembly(Assembly? assembly)
        {
            if (assembly is null || !IsRelevantAssembly(assembly))
            {
                return;
            }

            var assemblyRegistryEntry = new AssemblyRegistryEntry(assembly);

            if (!assemblies.TryGetValue(assemblyRegistryEntry.Name, out AssemblyRegistryEntry? _))
            {
                assemblies[assemblyRegistryEntry.Name] = assemblyRegistryEntry;
            }
        }

        private bool IsRelevantAssembly(Assembly assembly)
        {
            return
                options.AssemblyFileNamePrefixes.Any(
                    assemblyFileNamePrefix => Path.GetFileName(assembly.SafeGetLocation())?.StartsWith(assemblyFileNamePrefix) ?? false
                );
        }

        private void LoadAssemblies()
        {
            IEnumerable<Assembly> alreadyLoadedRelevantAssemblies = GetAssemblies();

            var assemblyFileNames =
                options
                    .AssemblyFileNamePrefixes
                    .SelectMany(
                        assemblyFileNamePrefix =>
                        Directory.GetFiles(
                            AppContext.BaseDirectory,
                            $"{assemblyFileNamePrefix}.*.dll",
                            SearchOption.AllDirectories
                        )
                    )
                    .Where(
                        assemblyFileName =>
                        !alreadyLoadedRelevantAssemblies.Any(
                            assembly =>
                            String.Equals(
                                Path.GetFileName(assemblyFileName),
                                Path.GetFileName(assembly.SafeGetLocation()),
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                    )
                    .ToList();

            assemblyFileNames.ForEach(assemblyFileName => LoadAssembly(assemblyFileName));
        }

        private Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            using IDisposable? scope = logger.Here();

            string requestedAssemblyFullName = args.Name;

            string requestedAssemblyName =
                requestedAssemblyFullName
#if NET5_0_OR_GREATER
                    .Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
#else
                    .Split(Separator, StringSplitOptions.RemoveEmptyEntries)
#endif
                    [0];

            Log.RequestedAssembly(logger, requestedAssemblyName, requestedAssemblyFullName);

            Assembly? assembly = null;

            if (assemblies.TryGetValue(requestedAssemblyName, out AssemblyRegistryEntry? assemblyRegistryEntry))
            {
                assembly = assemblyRegistryEntry.Assembly;
            }
            else
            {
                string[] assemblyFileNames = FindAssemblyFiles(requestedAssemblyName);

                if (assemblyFileNames.Length != 0)
                {
                    assembly = LoadAssembly(assemblyFileNames[0]);
                }
            }

            Log.ResolvedAssembly(
                logger,
                GetAssemblyFullName(assembly),
                assembly.SafeGetLocation().DefaultIfNullOrWhiteSpace(LogConstants.Unknown),
                assembly
#if NETFRAMEWORK
                    .SafeGetCodeBase()
#else
                    .SafeGetLocation()
#endif
                    .DefaultIfNullOrWhiteSpace(LogConstants.Unknown)
            );

            return assembly;
        }

        private void CurrentDomain_AssemblyLoad(object? sender, AssemblyLoadEventArgs args)
        {
            using IDisposable? scope = logger.Here();

            Assembly assembly = args.LoadedAssembly;

            Log.LoadedAssembly(
                logger,
                GetAssemblyFullName(assembly),
                assembly.SafeGetLocation().DefaultIfNullOrWhiteSpace(LogConstants.Unknown),
                assembly
#if NETFRAMEWORK
                    .SafeGetCodeBase()
#else
                    .SafeGetLocation()
#endif
                    .DefaultIfNullOrWhiteSpace(LogConstants.Unknown)
            );

            RegisterAssembly(assembly);
        }

        private static string GetAssemblyFullName(Assembly? assembly)
        {
            return assembly?.FullName ?? LogConstants.Unknown;
        }

        private static string[] FindAssemblyFiles(string assemblyShortName)
        {
            return Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, $"{assemblyShortName}.dll", SearchOption.AllDirectories);
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
