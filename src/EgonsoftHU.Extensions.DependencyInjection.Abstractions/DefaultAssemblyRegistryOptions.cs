// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides options for configuring the default assembly registry, including prefixes and predicate for assembly file names.
    /// </summary>
    public sealed class DefaultAssemblyRegistryOptions
    {
        /// <summary>
        /// Gets or sets the factory used to create logger instances.
        /// </summary>
        /// <remarks>
        /// The default logger factory is the <see cref="NullLoggerFactory.Instance"/>.
        /// </remarks>
        public ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

        /// <summary>
        /// Gets the collection of prefixes used for assembly file names.
        /// </summary>
        /// <remarks>
        /// The file name pattern for searching assembly files will be the following: <c>[prefix].*.dll</c>
        /// <para>
        /// Note: <c>EgonsoftHU</c> will be automatically added to the prefix collection.
        /// </para>
        /// <para>
        /// Example:
        /// <br/><c>AssemblyFileNamePrefixes = ["MyCompany"]</c>
        /// <br/><br/>will search for assemblies with these file name patterns:
        /// <br/><c>MyCompany.*.dll</c>
        /// <br/><c>EgonsoftHU.*.dll</c>
        /// </para>
        /// </remarks>
        public IReadOnlyCollection<string> AssemblyFileNamePrefixes { get; set; } =
#if LANGVERSION12_0_OR_GREATER
            []
#else
            Array.Empty<string>()
#endif
            ;

        /// <summary>
        /// Gets or sets the predicate for assembly files.
        /// </summary>
        /// <remarks>
        /// The delegate will get the full path (e.g. <c>"C:\SomeFolder\SomeAssembly.dll"</c>) and must return a <see cref="Boolean"/> value.
        /// </remarks>
        public Func<string, bool>? AssemblyFilePredicate { get; set; }
    }
}
