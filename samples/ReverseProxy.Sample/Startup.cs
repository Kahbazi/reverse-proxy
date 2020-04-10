// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ReverseProxy.Core.Abstractions;
using Microsoft.ReverseProxy.Core.Configuration.DependencyInjection;

namespace Microsoft.ReverseProxy.Sample
{
    /// <summary>
    /// ASP .NET Core pipeline initialization.
    /// </summary>
    public class Startup
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddReverseProxy()
                .LoadFromConfig(_configuration.GetSection("ReverseProxy"), reloadOnChange: true)
                .ConfigureBackendDefaults((id, backend) =>
                {
                    backend.HealthCheckOptions ??= new HealthCheckOptions();
                    backend.HealthCheckOptions.Enabled = true;
                })
                .ConfigureBackend("backend1", backend =>
                {
                    backend.HealthCheckOptions.Enabled = false;
                })
                // You can access the endpoints from the backends so I'm not sure if you need these.
                // Besides, a BackendEndpoint has almost no configuration.
            // .ConfigureEndpointDefaults(Action<BackendId, Backend, EndpointId, Endpoint>)
            // .ConfigureEndpoint(BackendId, EndpointId, Action<Backend, Endpoint>)
                .ConfigureRouteDefaults(route =>
                {
                    // Do not let config based routes take priority over code based routes.
                    // Lower numbers are higher priority.
                    if (route.Priority.HasValue && route.Priority.Value < 0)
                    {
                        route.Priority = 0;
                    }
                })
                .ConfigureRoute("route1", route =>
                {
                    route.Priority = 10;
                })
                // What if I need services? They'd better be singletons
            // .ConfigureRoute<TService>(string RouteId, Action<ProxyRoute, TService>)
            ;
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        public void Configure(IApplicationBuilder app)
        {
            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapReverseProxy();
            });
        }
    }
}
