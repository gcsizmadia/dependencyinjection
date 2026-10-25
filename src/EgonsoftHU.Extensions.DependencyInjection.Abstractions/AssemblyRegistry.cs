// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Extensions.Logging;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides access to the current assembly registry.
    /// </summary>
    public static class AssemblyRegistry
    {
        /// <summary>
        /// Gets the current assembly registry.
        /// </summary>
        public static IAssemblyRegistry Current { get; set; } = new EmptyAssemblyRegistry();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAssemblyRegistry"/> class with the specified assembly file name prefixes
        /// then sets it as the current assembly registry.
        /// </summary>
        /// <param name="assemblyFileNamePrefixes">The prefixes of the assembly file names.</param>
        /// <remarks>
        /// By default, logging is off. If you need logging then use one of the following overloads so that you can provide an <see cref="ILoggerFactory"/> instance:
        /// <list type="bullet">
        /// <item><see cref="UseDefault(ILoggerFactory, String[])"/></item>
        /// <item><see cref="UseDefault(Action{DefaultAssemblyRegistryOptions})"/></item>
        /// </list>
        /// </remarks>
        public static void UseDefault(params string[] assemblyFileNamePrefixes)
        {
            UseDefault(options => options.AssemblyFileNamePrefixes = assemblyFileNamePrefixes);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAssemblyRegistry"/> class with the specified logger factory and assembly file name prefixes
        /// then sets it as the current assembly registry.
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="assemblyFileNamePrefixes">The prefixes of the assembly file names.</param>
        public static void UseDefault(ILoggerFactory loggerFactory, params string[] assemblyFileNamePrefixes)
        {
            UseDefault(
                options =>
                {
                    options.AssemblyFileNamePrefixes = assemblyFileNamePrefixes;
                    options.LoggerFactory = loggerFactory;
                }
            );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAssemblyRegistry"/> class with configured options
        /// and sets it as the current assembly registry.
        /// </summary>
        /// <param name="configureAction">The delegate that configures the options.</param>
        public static void UseDefault(Action<DefaultAssemblyRegistryOptions> configureAction)
        {
            var registry = new DefaultAssemblyRegistry();

            registry.Configure(configureAction);

            Current = registry;
        }

        private sealed class EmptyAssemblyRegistry : IAssemblyRegistry
        {
            public IEnumerable<Assembly> GetAssemblies()
            {
                return
#if LANGVERSION12_0_OR_GREATER
                    []
#else
                    Array.Empty<Assembly>()
#endif
                    ;
            }
        }
    }
}
