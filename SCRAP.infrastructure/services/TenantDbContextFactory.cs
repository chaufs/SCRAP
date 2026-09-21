using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
namespace SCRAP.infrastructure.services;
    using System.Threading.Tasks;           // for Task<>
using Microsoft.EntityFrameworkCore;     // for DbContextOptionsBuilder, UseSqlServer
using SCRAP.infrastructure.data; 

    public class TenantDbContextFactory:ITenantDbContextFactory
    {
        private readonly ITenantDatabaseResolver _resolver;

        private readonly IConfiguration _configuration;



        public TenantDbContextFactory(

          ITenantDatabaseResolver resolver,

          IConfiguration configuration)

        {

            _resolver = resolver;

            _configuration = configuration;

        }



        public async Task<TenantErpDbContext> CreateAsync(int companyId)

        {

            var databaseInfo = await _resolver.GetDatabaseInfoAsync(companyId);



            var userId = _configuration[

              $"TenantCredentials:{databaseInfo.CredentialKey}:UserId"];



            var password = _configuration[

              $"TenantCredentials:{databaseInfo.CredentialKey}:Password"];



            if (string.IsNullOrWhiteSpace(userId) ||

              string.IsNullOrWhiteSpace(password))

            {

                throw new InvalidOperationException(

                  $"Credentials not found for key '{databaseInfo.CredentialKey}'.");

            }



            var connectionString =

              $"Server={databaseInfo.ServerName};" +

              $"Database={databaseInfo.DatabaseName};" +

              $"User Id={userId};" +

              $"Password={password};" +

              $"Encrypt=True;" +

              $"TrustServerCertificate=True;" +

              $"MultipleActiveResultSets=True;";



            var options = new DbContextOptionsBuilder<TenantErpDbContext>()

              .UseSqlServer(connectionString)

              .Options;



            return new TenantErpDbContext(options);

        }


    }

