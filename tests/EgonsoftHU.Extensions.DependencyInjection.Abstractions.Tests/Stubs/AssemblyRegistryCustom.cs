// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System.Collections.Generic;
using System.Reflection;

namespace EgonsoftHU.Extensions.DependencyInjection.UnitTests.Stubs
{
    internal sealed class AssemblyRegistryCustom
    {
        public IEnumerable<Assembly> GetAssemblies()
        {
            yield return GetType().Assembly;
        }
    }
}
