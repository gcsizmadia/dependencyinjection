// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Extensions.FileSystemGlobbing;

namespace EgonsoftHU.Extensions.DependencyInjection.Internal
{
    internal sealed class NetFxAssemblyFinder : AssemblyFinder
    {
        internal override IEnumerable<string> FindAssemblies(DefaultAssemblyRegistryOptions options)
        {
            List<string> prefixes =
#if LANGVERSION12_0_OR_GREATER
                [.. options.AssemblyFileNamePrefixes, nameof(EgonsoftHU)]
#else
                new(options.AssemblyFileNamePrefixes) { nameof(EgonsoftHU) }
#endif
                ;

            var matcher = new Matcher();

            foreach (string prefix in prefixes)
            {
                matcher.AddInclude($"{prefix}.{DllSearchPattern}");
            }

            IEnumerable<string> result = matcher.GetResultsInFullPath(AppContext.BaseDirectory);

            if (options.AssemblyFilePredicate is { } predicate)
            {
                result =
                    result.Union(
                        Directory
                            .EnumerateFiles(AppContext.BaseDirectory, DllSearchPattern, SearchOption.AllDirectories)
                            .Where(filePath => predicate.Invoke(Path.GetFullPath(filePath)))
                    );
            }

            return result;
        }
    }
}
#endif
