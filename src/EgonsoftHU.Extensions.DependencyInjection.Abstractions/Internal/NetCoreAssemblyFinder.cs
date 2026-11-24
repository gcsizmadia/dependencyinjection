// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;

namespace EgonsoftHU.Extensions.DependencyInjection.Internal
{
    internal sealed class NetCoreAssemblyFinder : AssemblyFinder
    {
        internal override IEnumerable<string> FindAssemblies(DefaultAssemblyRegistryOptions options)
        {
            List<Func<string, bool>> predicates = GetPredicates(options);

            return
                Directory
                    .EnumerateFiles(AppContext.BaseDirectory, DllSearchPattern, SearchOption.AllDirectories)
                    .Where(filePath => predicates.Any(predicate => predicate.Invoke(Path.GetFullPath(filePath))));
        }

        private static List<Func<string, bool>> GetPredicates(DefaultAssemblyRegistryOptions options)
        {
            List<string> prefixes =
#if LANGVERSION12_0_OR_GREATER
                [.. options.AssemblyFileNamePrefixes, nameof(EgonsoftHU)]
#else
                new(options.AssemblyFileNamePrefixes) { nameof(EgonsoftHU) }
#endif
                ;

            var predicates =
                prefixes
                    .Select<string, Func<string, bool>>(
                        prefix =>
                            fileName => FileSystemName.MatchesWin32Expression($"{prefix}.{DllSearchPattern}", Path.GetFileName(fileName))
                    )
                    .ToList();

            if (options.AssemblyFilePredicate is { } predicate)
            {
                predicates.Add(predicate);
            }

            return predicates;
        }
    }
}
#endif
