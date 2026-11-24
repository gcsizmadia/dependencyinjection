// Copyright © 2022-2026 Gabor Csizmadia
// This code is licensed under MIT license (see LICENSE for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
#if NETFRAMEWORK
using System.Diagnostics.CodeAnalysis;
#endif
using System.Reflection;
using System.Runtime.CompilerServices;

using EgonsoftHU.Extensions.Bcl;
using EgonsoftHU.Extensions.DependencyInjection.UnitTests.Stubs;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Sinks.XUnit3;

using Xunit;

namespace EgonsoftHU.Extensions.DependencyInjection.UnitTests
{
#if NETFRAMEWORK
    [ExcludeFromCodeCoverage]
#endif
    public class AssemblyRegistryTests : UnitTest<AssemblyRegistryTests>
    {
        public AssemblyRegistryTests(ITestOutputHelper output, LoggingFixture<AssemblyRegistryTests> fixture)
            : base(output, fixture)
        {
        }

        [Fact]
        public void WhenNotInitializedThenEmptyAssemblyRegistry()
        {
            // Arrange
            string expectedTypeFullName = "EgonsoftHU.Extensions.DependencyInjection.AssemblyRegistry+EmptyAssemblyRegistry";
            IAssemblyRegistry assemblyRegistry = AssemblyRegistry.Current;

            // Act
            string? actualTypeFullName = assemblyRegistry.GetType().FullName;

            // Assert
            actualTypeFullName.Should().Be(expectedTypeFullName);
            assemblyRegistry.GetAssemblies().Should().BeEmpty();
        }

        [Fact]
        public void CanInitializeOnlyOnce()
        {
            RunInNewProcess(
                () =>
                {
                    // Arrange

                    // Act
                    Action sut = Initialize;

                    // Assert
                    sut.Should().Throw<InvalidOperationException>().WithMessage("The assembly registry can be set only once.");

                    static void Initialize()
                    {
                        AssemblyRegistry.UseDefault();
                        AssemblyRegistry.UseCustom<AssemblyRegistryUsingInterfaceAndParamlessCtor>();
                    }
                }
            );
        }

        [Fact]
        public void CanInitializeUsingDefault()
        {
            RunInNewProcess(
                () =>
                {
                    // Arrange
                    var expectedAssemblies = new List<Assembly>()
                    {
                        typeof(TypeHelper).Assembly,
                        typeof(IAssemblyRegistry).Assembly,
#if !NETFRAMEWORK
                        GetType().Assembly
#endif
                    };

                    ILoggerFactory loggerFactory =
                        LoggerFactory.Create(
                            loggingBuilder =>
                                loggingBuilder
                                    .SetMinimumLevel(LogLevel.Debug)
                                    .AddSerilog(
                                        new LoggerConfiguration()
                                            .MinimumLevel.Verbose()
                                            .WriteTo.XUnit3TestOutput()
                                            .CreateLogger(),
                                        dispose: true
                                    )
                        );

                    // Act
                    Action sut = () => Initialize(loggerFactory);

                    // Assert
                    sut.Should().NotThrow();

                    AssemblyRegistry.Current.GetAssemblies().Should().BeEquivalentTo(expectedAssemblies);

                    static void Initialize(ILoggerFactory loggerFactory)
                    {
                        AssemblyRegistry.UseDefault(loggerFactory);
                    }
                }
            );
        }

        [Fact]
        public void CanInitializeUsingImplementedInterfaceAndParamlessCtor()
        {
            RunInNewProcess(
                () =>
                {
                    // Arrange

                    // Act
                    Action sut = Initialize;

                    // Assert
                    sut.Should().NotThrow();

                    AssemblyRegistry.Current.GetAssemblies().Should().HaveCount(1).And.HaveElementAt(0, GetType().Assembly);

                    static void Initialize()
                    {
                        AssemblyRegistry.UseCustom<AssemblyRegistryUsingInterfaceAndParamlessCtor>();
                    }
                }
            );
        }

        [Fact]
        public void CanInitializeUsingImplementedInterfaceButNoParamlessCtor()
        {
            RunInNewProcess(
                () =>
                {
                    // Arrange

                    // Act
                    Action sut = Initialize;

                    // Assert
                    sut.Should().NotThrow();

                    AssemblyRegistry.Current.GetAssemblies().Should().HaveCount(1).And.HaveElementAt(0, GetType().Assembly);

                    static void Initialize()
                    {
                        AssemblyRegistry.UseCustom(new AssemblyRegistryUsingInterfaceButNoParamlessCtor("some param"));
                    }
                }
            );
        }

        [Fact]
        public void CanInitializeUsingCustom()
        {
            RunInNewProcess(
                () =>
                {
                    // Arrange

                    // Act
                    Action sut = Initialize;

                    // Assert
                    sut.Should().NotThrow();

                    AssemblyRegistry.Current.GetAssemblies().Should().HaveCount(1).And.HaveElementAt(0, GetType().Assembly);

                    static void Initialize()
                    {
                        AssemblyRegistry.UseCustom(new AssemblyRegistryCustom());
                    }
                }
            );
        }

        private static void RunInNewProcess(Action testBody, [CallerMemberName] string testName = "")
        {
            // If we are already inside the spawned child process, execute the actual test
            if (Environment.GetEnvironmentVariable("XUNIT_CHILD_PROCESS") == "1")
            {
                testBody();
                return;
            }

            string assemblyPath = typeof(AssemblyRegistryTests).Assembly.Location;
            string fullTestName = $"{typeof(AssemblyRegistryTests).FullName}.{testName}";

            var psi = new ProcessStartInfo("dotnet", $"test \"{assemblyPath}\" --filter \"FullyQualifiedName={fullTestName}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            psi.Environment["XUNIT_CHILD_PROCESS"] = "1";

            using Process process = Process.Start(psi) ?? throw new InvalidOperationException("Isolated test not started.");

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                throw new InvalidOperationException(
                    $"Isolated test failed. ExitCode=[{process.ExitCode}]\n{output}\n{error}"
                );
            }
        }
    }
}
