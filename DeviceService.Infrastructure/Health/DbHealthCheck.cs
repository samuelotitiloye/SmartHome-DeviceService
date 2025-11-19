using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace DeviceService.Infrastructure.Health
{
    public class DbHealthCheck : IHealthCheck
    {
        public readonly string _connectionString;

        public DbHealthCheck(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
        
                //check if DB can simple command
                using var cmd = new NpgsqlCommand("SELECT 1", connection);
                var result = await cmd.ExecuteScalarAsync(cancellationToken);

                return HealthCheckResult.Healthy("Database is reachable.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database connection failed.", ex);
            }
        }
    }
}