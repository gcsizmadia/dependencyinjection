// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

#if !LANGVERSION12_0_OR_GREATER
using System;
#endif
using System.Collections.Generic;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides options for configuring the default assembly registry, including file name prefixes for assemblies.
    /// </summary>
    public sealed class DefaultAssemblyRegistryOptions : AssemblyRegistryOptions
    {
        /// <summary>
        /// Gets the collection of prefixes used for assembly file names.
        /// </summary>
        public IReadOnlyCollection<string> AssemblyFileNamePrefixes { get; internal set; } =
#if LANGVERSION12_0_OR_GREATER
            []
#else
            Array.Empty<string>()
#endif
            ;
    }
}
