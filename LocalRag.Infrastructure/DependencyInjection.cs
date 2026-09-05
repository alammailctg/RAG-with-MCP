using AutoMapper;
using LocalRag.Application.Interfaces;
using LocalRag.Application.ServiceInterface;
using LocalRag.Domain.RepositoryInterfaces;
using LocalRag.Infrastructure.Agents;
using LocalRag.Infrastructure.MCPServers;
using LocalRag.Infrastructure.Repositories;
using LocalRag.Infrastructure.ServiceImplementation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddAutoMapper(
                cfg => { },
                typeof(DependencyInjection).Assembly);

            services.AddScoped<IVectorRepository, VectorRepository>();
            services.AddScoped<IDatabaseQueryService,DatabaseQueryService>();
            services.AddScoped<IMcpAgentService, McpAgentService>();
            services.AddScoped<DatabaseTools>();
            services.AddScoped<VectorSearchTools>();
            return services;
        }
    }
}
