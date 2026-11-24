// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using EgonsoftHU.Extensions.Bcl;

using Microsoft.Extensions.Logging;

namespace EgonsoftHU.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides access to the current assembly registry.
    /// </summary>
    public static class AssemblyRegistry
    {
        private const string SetAllowedOnlyOnce = "The assembly registry can be set only once.";

#if !LANGVERSION14_0_OR_GREATER
        private static IAssemblyRegistry current = EmptyAssemblyRegistry.Instance;
#endif

        /// <summary>
        /// Gets the current assembly registry.
        /// </summary>
        public static IAssemblyRegistry Current
        {
#if LANGVERSION14_0_OR_GREATER
            get;
            private set
            {
                if (field is not EmptyAssemblyRegistry)
                {
                    throw new InvalidOperationException(SetAllowedOnlyOnce);
                }

                field = value;
            }
#else
            get => current;
            private set
            {
                if (current is not EmptyAssemblyRegistry)
                {
                    throw new InvalidOperationException(SetAllowedOnlyOnce);
                }

                current = value;
            }
#endif
        }
#if LANGVERSION14_0_OR_GREATER
            = EmptyAssemblyRegistry.Instance;
#endif

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
        /// The file name pattern for searching assembly files will be the following: <c>[prefix].*.dll</c>
        /// <para>
        /// Note: <c>EgonsoftHU</c> will be automatically added to the prefix collection.
        /// </para>
        /// <para>
        /// Example:
        /// <br/><c>UseDefault("MyCompany");</c>
        /// <br/><br/>will search for assemblies with these file name patterns:
        /// <br/><c>MyCompany.*.dll</c>
        /// <br/><c>EgonsoftHU.*.dll</c>
        /// </para>
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
        /// <remarks>
        /// The file name pattern for searching assembly files will be the following: <c>[prefix].*.dll</c>
        /// <para>
        /// Note: <c>EgonsoftHU</c> will be automatically added to the prefix collection.
        /// </para>
        /// <para>
        /// Example:
        /// <br/><c>UseDefault(loggerFactory, "MyCompany");</c>
        /// <br/><br/>will search for assemblies with these file name patterns:
        /// <br/><c>MyCompany.*.dll</c>
        /// <br/><c>EgonsoftHU.*.dll</c>
        /// </para>
        /// </remarks>
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
            registry.Initialize();

            Current = registry;
        }

        /// <summary>
        /// Initializes a new instance of the <typeparamref name="TAssemblyRegistry"/> type using its default parameterless constructor
        /// then sets it as the current assembly registry.
        /// </summary>
        /// <typeparam name="TAssemblyRegistry">
        /// The type of the custom assembly registry implementation.
        /// <para>
        /// This type must implement the <see cref="IAssemblyRegistry"/> interface and have a parameterless constructor.
        /// </para>
        /// </typeparam>
        public static void UseCustom<TAssemblyRegistry>()
            where TAssemblyRegistry : IAssemblyRegistry, new()
        {
            Current = new TAssemblyRegistry();
        }

        /// <summary>
        /// Sets a specified <see cref="IAssemblyRegistry"/> instance as the current assembly registry.
        /// </summary>
        /// <param name="assemblyRegistry">The custom assembly registry implementation to be set as the current assembly registry.</param>
        public static void UseCustom(IAssemblyRegistry assemblyRegistry)
        {
            assemblyRegistry.ThrowIfNull();

            Current = assemblyRegistry;
        }

        /// <summary>
        /// Sets a specified custom assembly registry implementation as the current assembly registry.
        /// </summary>
        /// <param name="assemblyRegistry">The custom assembly registry implementation to be set as the current assembly registry.</param>
        /// <remarks>
        /// The custom assembly registry implementation will be used through a wrapper class that looks for a method (using reflection) that
        /// satisfies the following criteria:
        /// <list type="bullet">
        /// <item>public parameterless instance method</item>
        /// <item>method name is <c>GetAssemblies</c></item>
        /// <item>return type can be assigned to a variable of the <c><see cref="IEnumerable{T}">IEnumerable</see>&lt;<see cref="Assembly"/>&gt;</c> type</item>
        /// </list>
        /// </remarks>
        public static void UseCustom(object assemblyRegistry)
        {
            assemblyRegistry.ThrowIfNull();

            Current = ObjectWrapperAssemblyRegistry.Create(assemblyRegistry);
        }

        private sealed class EmptyAssemblyRegistry : IAssemblyRegistry
        {
            internal static readonly EmptyAssemblyRegistry Instance = new();

            private EmptyAssemblyRegistry()
            {
            }

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

        private sealed class ObjectWrapperAssemblyRegistry : IAssemblyRegistry
        {
            private readonly object assemblyRegistry;
            private readonly MethodInfo getAssembliesMethod;

            internal static ObjectWrapperAssemblyRegistry Create(object assemblyRegistry)
            {
                TypeInfo typeInfo = assemblyRegistry.GetType().GetTypeInfo();

                MethodInfo? getAssembliesMethod =
                    typeInfo
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .SingleOrDefault(
                            methodInfo =>
                            String.Equals(nameof(IAssemblyRegistry.GetAssemblies), methodInfo.Name, StringComparison.OrdinalIgnoreCase)
                            &&
                            methodInfo.GetParameters().Length == 0
                            &&
                            typeof(IEnumerable<Assembly>).IsAssignableFrom(methodInfo.ReturnType)
                        );

                if (getAssembliesMethod is null)
                {
                    var ex = new ArgumentException($"Expected method not found in type. Type=[{typeInfo.FullName}]", nameof(assemblyRegistry));
                    ex.Data["ExpectedMethod.IsPublic"] = true;
                    ex.Data["ExpectedMethod.IsStatic"] = false;
                    ex.Data["ExpectedMethod.ReturnType"] = typeof(IEnumerable<Assembly>);
                    ex.Data["ExpectedMethod.Name"] = nameof(IAssemblyRegistry.GetAssemblies);
                    ex.Data["ExpectedMethod.GetParameter().Length"] = 0;

                    throw ex;
                }

                return new(assemblyRegistry, getAssembliesMethod);
            }

            private ObjectWrapperAssemblyRegistry(object assemblyRegistry, MethodInfo getAssembliesMethod)
            {
                this.assemblyRegistry = assemblyRegistry;
                this.getAssembliesMethod = getAssembliesMethod;
            }

            public IEnumerable<Assembly> GetAssemblies()
            {
                return
                    getAssembliesMethod.Invoke(assemblyRegistry, null) is IEnumerable<Assembly> result
                        ? result
#if LANGVERSION12_0_OR_GREATER
                        : []
#else
                        : Array.Empty<Assembly>()
#endif
                        ;
            }
        }
    }
}
