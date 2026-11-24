// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System.Collections.Generic;

namespace EgonsoftHU.Extensions.DependencyInjection.Internal
{
    internal abstract class AssemblyFinder
    {
        protected const string DllSearchPattern = "*.dll";

        internal static AssemblyFinder Instance { get; } =
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            new NetCoreAssemblyFinder()
#else
            new NetFxAssemblyFinder()
#endif
            ;

        internal abstract IEnumerable<string> FindAssemblies(DefaultAssemblyRegistryOptions options);
    }
}
