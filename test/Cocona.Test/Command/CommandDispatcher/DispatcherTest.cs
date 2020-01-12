﻿using Cocona.Command;
using Cocona.Command.Binder;
using Cocona.Command.Dispatcher;
using Cocona.CommandLine;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Cocona.Test.Command.CommandDispatcher
{
    public class DispatcherTest
    {
        [Fact]
        public async Task SimpleSingleCommandDispatch()
        {
            var services = new ServiceCollection();
            {
                services.AddTransient<ICoconaCommandProvider>(serviceProvider => new CoconaCommandProvider(new Type[] { typeof(TestCommand) }));
                services.AddTransient<ICoconaCommandLineArgumentProvider>(serviceProvider => new CoconaCommandLineArgumentProvider(new string[] { "--option0=hogehoge" }));
                services.AddTransient<ICoconaParameterBinder, CoconaParameterBinder>();
                services.AddTransient<ICoconaValueConverter, CoconaValueConverter>();
                services.AddTransient<ICoconaCommandLineParser, CoconaCommandLineParser>();
                services.AddTransient<ICoconaCommandDispatcher, CoconaCommandDispatcher>();
                services.AddTransient<ICoconaCommandDispatcherPipelineBuilder>(
                    serviceProvider => new CoconaCommandDispatcherPipelineBuilder(serviceProvider).UseMiddleware<CoconaCommandInvokeMiddleware>());

                services.AddSingleton<TestCommand>();
                services.AddSingleton<TestMultipleCommand>();
            }
            var serviceProvider = services.BuildServiceProvider();

            var dispatcher = serviceProvider.GetService<ICoconaCommandDispatcher>();
            var result = await dispatcher.DispatchAsync();
            serviceProvider.GetService<TestCommand>().Log[0].Should().Be($"{nameof(TestCommand.Test)}:option0 -> hogehoge");
        }

        [Fact]
        public async Task MultipleCommand_Option1()
        {
            var services = new ServiceCollection();
            {
                services.AddTransient<ICoconaCommandProvider>(serviceProvider => new CoconaCommandProvider(new Type[] { typeof(TestMultipleCommand) }));
                services.AddTransient<ICoconaCommandLineArgumentProvider>(serviceProvider => new CoconaCommandLineArgumentProvider(new string[] { "A", "--option0", "Hello" }));
                services.AddTransient<ICoconaParameterBinder, CoconaParameterBinder>();
                services.AddTransient<ICoconaValueConverter, CoconaValueConverter>();
                services.AddTransient<ICoconaCommandLineParser, CoconaCommandLineParser>();
                services.AddTransient<ICoconaCommandDispatcher, CoconaCommandDispatcher>();
                services.AddTransient<ICoconaCommandDispatcherPipelineBuilder>(
                    serviceProvider => new CoconaCommandDispatcherPipelineBuilder(serviceProvider).UseMiddleware<CoconaCommandInvokeMiddleware>());

                services.AddSingleton<TestCommand>();
                services.AddSingleton<TestMultipleCommand>();
            }
            var serviceProvider = services.BuildServiceProvider();

            var dispatcher = serviceProvider.GetService<ICoconaCommandDispatcher>();
            var result = await dispatcher.DispatchAsync();
            serviceProvider.GetService<TestMultipleCommand>().Log[0].Should().Be($"{nameof(TestMultipleCommand.A)}:option0 -> Hello");
        }

        [Fact]
        public async Task CommandNotFound_Single()
        {
            var services = new ServiceCollection();
            {
                services.AddTransient<ICoconaCommandProvider>(serviceProvider => new CoconaCommandProvider(new Type[] { typeof(NoCommand) }));
                services.AddTransient<ICoconaCommandLineArgumentProvider>(serviceProvider => new CoconaCommandLineArgumentProvider(new string[] { "C" }));
                services.AddTransient<ICoconaParameterBinder, CoconaParameterBinder>();
                services.AddTransient<ICoconaValueConverter, CoconaValueConverter>();
                services.AddTransient<ICoconaCommandLineParser, CoconaCommandLineParser>();
                services.AddTransient<ICoconaCommandDispatcher, CoconaCommandDispatcher>();
                services.AddTransient<ICoconaCommandDispatcherPipelineBuilder>(
                    serviceProvider => new CoconaCommandDispatcherPipelineBuilder(serviceProvider).UseMiddleware<CoconaCommandInvokeMiddleware>());

                services.AddSingleton<TestCommand>();
                services.AddSingleton<TestMultipleCommand>();
            }
            var serviceProvider = services.BuildServiceProvider();

            var dispatcher = serviceProvider.GetService<ICoconaCommandDispatcher>();
            var ex = await Assert.ThrowsAsync<CommandNotFoundException>(async () => await dispatcher.DispatchAsync());
            ex.Command.Should().BeEmpty();
            ex.ImplementedCommands.All.Should().BeEmpty();
        }

        [Fact]
        public async Task CommandNotFound_Multiple()
        {
            var services = new ServiceCollection();
            {
                services.AddTransient<ICoconaCommandProvider>(serviceProvider => new CoconaCommandProvider(new Type[] { typeof(TestMultipleCommand) }));
                services.AddTransient<ICoconaCommandLineArgumentProvider>(serviceProvider => new CoconaCommandLineArgumentProvider(new string[] { "C" }));
                services.AddTransient<ICoconaParameterBinder, CoconaParameterBinder>();
                services.AddTransient<ICoconaValueConverter, CoconaValueConverter>();
                services.AddTransient<ICoconaCommandLineParser, CoconaCommandLineParser>();
                services.AddTransient<ICoconaCommandDispatcher, CoconaCommandDispatcher>();
                services.AddTransient<ICoconaCommandDispatcherPipelineBuilder>(
                    serviceProvider => new CoconaCommandDispatcherPipelineBuilder(serviceProvider).UseMiddleware<CoconaCommandInvokeMiddleware>());

                services.AddSingleton<TestCommand>();
                services.AddSingleton<TestMultipleCommand>();
            }
            var serviceProvider = services.BuildServiceProvider();

            var dispatcher = serviceProvider.GetService<ICoconaCommandDispatcher>();
            var ex = await Assert.ThrowsAsync<CommandNotFoundException>(async () => await dispatcher.DispatchAsync());
            ex.Command.Should().Be("C");
            ex.ImplementedCommands.All.Should().HaveCount(2);
        }

        public class NoCommand
        { }

        public class TestCommand
        {
            public List<string> Log { get; } = new List<string>();

            public void Test(string option0)
            {
                Log.Add($"{nameof(TestCommand.Test)}:{nameof(option0)} -> {option0}");
            }
        }

        public class TestMultipleCommand
        {
            public List<string> Log { get; } = new List<string>();

            public void A(string option0)
            {
                Log.Add($"{nameof(TestMultipleCommand.A)}:{nameof(option0)} -> {option0}");
            }
            public void B(bool option0, [Argument]string arg0)
            {
                Log.Add($"{nameof(TestMultipleCommand.B)}:{nameof(option0)} -> {option0}");
            }
        }
    }
}