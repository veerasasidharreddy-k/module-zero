﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.EntityFramework;
using Abp.EntityFrameworkCore;
using Abp.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Abp.Temp
{
    /* TODO: Move to Abp.EntityFrameworkCore package! */

    public class DefaultDbContextResolver : IDbContextResolver, ITransientDependency
    {
        private readonly IIocResolver _iocResolver;
        private readonly IDbContextTypeMatcher _dbContextTypeMatcher;

        public DefaultDbContextResolver(
            IIocResolver iocResolver,
            IDbContextTypeMatcher dbContextTypeMatcher)
        {
            _iocResolver = iocResolver;
            _dbContextTypeMatcher = dbContextTypeMatcher;
        }

        public TDbContext Resolve<TDbContext>(string connectionString)
            where TDbContext : DbContext
        {
            var dbContextType = typeof(TDbContext);

            if (!dbContextType.IsAbstract)
            {
                return _iocResolver.Resolve<TDbContext>(new
                {
                    options = CreateOptions<TDbContext>(connectionString)
                });
            }

            var concreteType = _dbContextTypeMatcher.GetConcreteType(dbContextType);

            return (TDbContext)_iocResolver.Resolve(concreteType, new
            {
                options = CreateOptionsForType(concreteType, connectionString)
            });
        }

        private object CreateOptionsForType(Type dbContextType, string connectionString)
        {
            var createOptionsMethod = typeof(DefaultDbContextResolver)
                .GetMethod("CreateOptions", BindingFlags.NonPublic | BindingFlags.Instance);

            var createOptionsGenericMethod = createOptionsMethod.MakeGenericMethod(dbContextType);

            return createOptionsGenericMethod.Invoke(this, new object[] {connectionString});
        }

        protected virtual DbContextOptions<TDbContext> CreateOptions<TDbContext>(string connectionString)
            where TDbContext : DbContext
        {
            if (_iocResolver.IsRegistered<IAbpDbContextConfigurer<TDbContext>>())
            {
                var configuration = new AbpDbContextConfiguration<TDbContext>(connectionString);

                using (var configurer = _iocResolver.ResolveAsDisposable<IAbpDbContextConfigurer<TDbContext>>())
                {
                    configurer.Object.Configure(configuration);
                }

                return configuration.DbContextOptions.Options;
            }

            if (_iocResolver.IsRegistered<DbContextOptions<TDbContext>>())
            {
                return _iocResolver.Resolve<DbContextOptions<TDbContext>>();
            }

            throw new AbpException($"Could not resolve DbContextOptions for {typeof(TDbContext).AssemblyQualifiedName}.");
        }
    }
}
