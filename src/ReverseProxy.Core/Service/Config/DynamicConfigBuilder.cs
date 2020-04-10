// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.ReverseProxy.Core.Abstractions;
using Microsoft.ReverseProxy.Core.ConfigModel;
using Microsoft.ReverseProxy.Utilities;

namespace Microsoft.ReverseProxy.Core.Service
{
    internal class DynamicConfigBuilder : IDynamicConfigBuilder
    {
        private readonly DynamicConfigBuilderOptions _options;
        private readonly IBackendsRepo _backendsRepo;
        private readonly IRoutesRepo _routesRepo;
        private readonly IRouteValidator _parsedRouteValidator;

        public DynamicConfigBuilder(
            IOptions<DynamicConfigBuilderOptions> options,
            IBackendsRepo backendsRepo,
            IRoutesRepo routesRepo,
            IRouteValidator parsedRouteValidator)
        {
            Contracts.CheckValue(options, nameof(options));
            Contracts.CheckValue(backendsRepo, nameof(backendsRepo));
            Contracts.CheckValue(routesRepo, nameof(routesRepo));
            Contracts.CheckValue(parsedRouteValidator, nameof(parsedRouteValidator));
            _options = options.Value;
            _backendsRepo = backendsRepo;
            _routesRepo = routesRepo;
            _parsedRouteValidator = parsedRouteValidator;
        }

        public async Task<Result<DynamicConfigRoot>> BuildConfigAsync(IConfigErrorReporter errorReporter, CancellationToken cancellation)
        {
            Contracts.CheckValue(errorReporter, nameof(errorReporter));

            var backends = await GetBackendsAsync(cancellation);
            var routes = await GetRoutesAsync(errorReporter, cancellation);

            var config = new DynamicConfigRoot
            {
                Backends = backends,
                Routes = routes,
            };

            return Result.Success(config);
        }

        public async Task<IDictionary<string, Backend>> GetBackendsAsync(CancellationToken cancellation)
        {
            var backends = await _backendsRepo.GetBackendsAsync(cancellation) ?? new Dictionary<string, Backend>(StringComparer.Ordinal);

            // The IBackendsRepo provides a fresh snapshot that we need to reconfigure each time.
            foreach (var pair in backends)
            {
                // Default config for all backends
                foreach (var backendConfig in _options.BackendDefaultConfigs)
                {
                    backendConfig(pair.Key, pair.Value);
                }

                // Specific config for this backend
                if (_options.BackendConfigs.TryGetValue(pair.Key, out var backendConfigs))
                {
                    foreach (var backendConfig in backendConfigs)
                    {
                        backendConfig(pair.Value);
                    }
                }
            }

            return backends;
        }

        private async Task<IList<ParsedRoute>> GetRoutesAsync(IConfigErrorReporter errorReporter, CancellationToken cancellation)
        {
            var routes = await _routesRepo.GetRoutesAsync(cancellation);

            var seenRouteIds = new HashSet<string>();
            var sortedRoutes = new SortedList<(int, string), ParsedRoute>(routes?.Count ?? 0);
            if (routes != null)
            {
                foreach (var route in routes)
                {
                    if (seenRouteIds.Contains(route.RouteId))
                    {
                        errorReporter.ReportError(ConfigErrors.RouteDuplicateId, route.RouteId, $"Duplicate route '{route.RouteId}'.");
                        continue;
                    }

                    // Default config for all routes
                    foreach (var routeConfig in _options.RouteDefaultConfigs)
                    {
                        routeConfig(route);
                    }

                    // Specific config for this route
                    if (_options.RouteConfigs.TryGetValue(route.RouteId, out var routeConfigs))
                    {
                        foreach (var routeConfig in routeConfigs)
                        {
                            routeConfig(route);
                        }
                    }

                    var parsedRoute = new ParsedRoute {
                        RouteId = route.RouteId,
                        Methods = route.Match.Methods,
                        Host = route.Match.Host,
                        Path = route.Match.Path,
                        Priority = route.Priority,
                        BackendId = route.BackendId,
                        Metadata = route.Metadata,
                    };

                    if (!_parsedRouteValidator.ValidateRoute(parsedRoute, errorReporter))
                    {
                        // parsedRouteValidator already reported error message
                        continue;
                    }

                    sortedRoutes.Add((parsedRoute.Priority ?? 0, parsedRoute.RouteId), parsedRoute);
                }
            }

            return sortedRoutes.Values;
        }
    }
}
