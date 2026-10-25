// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides options for configuring the assembly registry.
    /// </summary>
    public class AssemblyRegistryOptions
    {
        /// <summary>
        /// Gets or sets the factory used to create logger instances.
        /// </summary>
        /// <remarks>
        /// The default logger factory is the <see cref="NullLoggerFactory.Instance"/>.
        /// </remarks>
        public ILoggerFactory LoggerFactory { get; internal set; } = NullLoggerFactory.Instance;
    }
}
