// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using Microsoft.Extensions.Logging;

namespace EgonsoftHU.Extensions.DependencyInjection.Logging
{
    internal static partial class Log
    {
        [LoggerMessage(LogLevel.Information, "Assemblies registered: {RegisteredAssemblyCount}")]
        internal static partial void RegisteredAssemblyCount(ILogger logger, int registeredAssemblyCount);

        [LoggerMessage(LogLevel.Information, "Assembly: FullName=[{AssemblyFullName}] Location=[{AssemblyLocation}]")]
        internal static partial void Assembly(ILogger logger, string assemblyFullName, string assemblyLocation);

        [LoggerMessage(LogLevel.Information, "RequestedAssembly: Name=[{AssemblyName}] FullName=[{AssemblyFullName}]")]
        internal static partial void RequestedAssembly(ILogger logger, string assemblyName, string assemblyFullName);

        [LoggerMessage(LogLevel.Information, "ResolvedAssembly: FullName=[{AssemblyFullName}] Location=[{AssemblyLocation}] CodeBase=[{AssemblyCodeBase}]")]
        internal static partial void ResolvedAssembly(ILogger logger, string assemblyFullName, string assemblyLocation, string assemblyCodeBase);

        [LoggerMessage(LogLevel.Information, "LoadedAssembly: FullName=[{AssemblyFullName}] Location=[{AssemblyLocation}] CodeBase=[{AssemblyCodeBase}]")]
        internal static partial void LoadedAssembly(ILogger logger, string assemblyFullName, string assemblyLocation, string assemblyCodeBase);
    }
}
