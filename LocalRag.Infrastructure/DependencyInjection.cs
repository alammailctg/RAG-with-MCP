using AutoMapper;
using LocalRag.Domain.RepositoryInterfaces;
using LocalRag.Infrastructure.Repositories;
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

            return services;
        }
    }
}
