// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

namespace EgonsoftHU.Extensions.DependencyInjection.Logging
{
    internal static class ILoggerExtensions
    {
        internal static IDisposable? Here(this ILogger logger, [CallerMemberName] string? callerMemberName = null)
        {
            return logger.BeginScope(
                new Dictionary<string, object>()
                {
                    ["SourceMember"] = callerMemberName ?? LogConstants.Unknown
                }
            );
        }
    }
}
