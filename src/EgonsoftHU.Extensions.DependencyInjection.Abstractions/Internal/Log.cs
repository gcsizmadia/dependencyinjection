// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using Microsoft.Extensions.Logging;

namespace EgonsoftHU.Extensions.DependencyInjection.Internal
{
    internal static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Assemblies registered: {RegisteredAssemblyCount}", SkipEnabledCheck = false)]
        internal static partial void RegisteredAssemblyCount(ILogger logger, int registeredAssemblyCount);

        [LoggerMessage(2, LogLevel.Debug, "Assembly: FullName=[{AssemblyFullName}] Location=[{AssemblyLocation}]", SkipEnabledCheck = false)]
        internal static partial void RegisteredAssembly(ILogger logger, string assemblyFullName, string assemblyLocation);

        [LoggerMessage(3, LogLevel.Debug, "Assembly found by name: [{AssemblyName}]", SkipEnabledCheck = false)]
        internal static partial void AssemblyFoundByName(ILogger logger, string assemblyName);

        [LoggerMessage(4, LogLevel.Debug, "Assembly found by location: [{AssemblyLocation}]", SkipEnabledCheck = false)]
        internal static partial void AssemblyFoundByLocation(ILogger logger, string assemblyLocation);

        [LoggerMessage(5, LogLevel.Debug, "Assembly not found, loading from location: [{AssemblyLocation}]", SkipEnabledCheck = false)]
        internal static partial void LoadAssembly(ILogger logger, string assemblyLocation);
    }
}
