// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ReverseProxy.Core.Abstractions;
using Microsoft.ReverseProxy.Core.Service;

namespace Microsoft.ReverseProxy.Core.Configuration.DependencyInjection
{
    /// <summary>
    /// Extensions for <see cref="IServiceCollection"/>
    /// used to register the ReverseProxy's components.
    /// </summary>
    public static class ReverseProxyServiceCollectionExtensions
    {
        /// <summary>
        /// Adds ReverseProxy's services to Dependency Injection.
        /// </summary>
        public static IReverseProxyBuilder AddReverseProxy(this IServiceCollection services)
        {
            var builder = new ReverseProxyBuilder(services);
            builder
                .AddTelemetryShims()
                .AddMetrics()
                .AddInMemoryRepos()
                .AddConfigBuilder()
                .AddRuntimeStateManagers()
                .AddConfigManager()
                .AddDynamicEndpointDataSource()
                .AddProxy()
                .AddBackgroundWorkers();

            return builder;
        }

        /// <summary>
        /// Loads routes and endpoints from config.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="config"></param>
        /// <param name="reloadOnChange"></param>
        /// <returns></returns>
        public static IReverseProxyBuilder LoadFromConfig(this IReverseProxyBuilder builder, IConfiguration config, bool reloadOnChange = true)
        {
            builder.Services.Configure<ProxyConfigOptions>(config);
            builder.Services.Configure<ProxyConfigOptions>(options => options.ReloadOnChange = reloadOnChange);
            builder.Services.AddHostedService<ProxyConfigLoader>();

            return builder;
        }

        /// <summary>
        /// Provides a configuration action that runs on all routes each time the configuration is generated. This can be called more than once and
        /// the Actions are run in the given order.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IReverseProxyBuilder ConfigureRouteDefaults(this IReverseProxyBuilder builder, Action<ProxyRoute> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.Configure<DynamicConfigBuilderOptions>(options =>
            {
                options.RouteDefaultConfigs.Add(configure);
            });

            return builder;
        }

        /// <summary>
        /// Provides a configuration action that runs on the named route each time the configuration is generated. This can be called more than once
        /// per route and the Actions are run in the given order. These are applied after any default configuration Actions.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="routeId"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IReverseProxyBuilder ConfigureRoute(this IReverseProxyBuilder builder, string routeId, Action<ProxyRoute> configure)
        {
            if (routeId is null)
            {
                throw new ArgumentNullException(nameof(routeId));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.Configure<DynamicConfigBuilderOptions>(options =>
            {
                if (!options.RouteConfigs.TryGetValue(routeId, out var configs))
                {
                    configs = new List<Action<ProxyRoute>>(1);
                    options.RouteConfigs[routeId] = configs;
                }
                configs.Add(configure);
            });

            return builder;
        }

        /// <summary>
        /// Provides a configuration action that runs on all backends each time the configuration is generated. This can be called more than once and
        /// the Actions are run in the given order.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IReverseProxyBuilder ConfigureBackendDefaults(this IReverseProxyBuilder builder, Action<string, Backend> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.Configure<DynamicConfigBuilderOptions>(options =>
            {
                options.BackendDefaultConfigs.Add(configure);
            });

            return builder;
        }

        /// <summary>
        /// Provides a configuration action that runs on the named backend each time the configuration is generated. This can be called more than once
        /// per backend and the Actions are run in the given order. These are applied after any default configuration Actions.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="backendId"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IReverseProxyBuilder ConfigureBackend(this IReverseProxyBuilder builder, string backendId, Action<Backend> configure)
        {
            if (backendId is null)
            {
                throw new ArgumentNullException(nameof(backendId));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.Configure<DynamicConfigBuilderOptions>(options =>
            {
                if (!options.BackendConfigs.TryGetValue(backendId, out var configs))
                {
                    configs = new List<Action<Backend>>(1);
                    options.BackendConfigs[backendId] = configs;
                }
                configs.Add(configure);
            });

            return builder;
        }
    }
}
