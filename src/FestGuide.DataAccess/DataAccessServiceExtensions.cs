using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using FestGuide.DataAccess.Abstractions;
using FestGuide.DataAccess.Repositories;

namespace FestGuide.DataAccess;

/// <summary>
/// Extension methods for registering data access services.
/// </summary>
public static class DataAccessServiceExtensions
{
    /// <summary>
    /// Adds data access services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddDataAccessServices(this IServiceCollection services, string connectionString)
    {
        // Register IDbConnection factory
        services.AddScoped<IDbConnection>(_ => new SqlConnection(connectionString));

        // Phase 1 Repositories - Authentication & User Management
        services.AddScoped<IUserRepository, SqlServerUserRepository>();
        services.AddScoped<IRefreshTokenRepository, SqlServerRefreshTokenRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, SqlServerEmailVerificationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, SqlServerPasswordResetTokenRepository>();
        services.AddScoped<IFestivalPermissionRepository, SqlServerFestivalPermissionRepository>();

        // Phase 2 Repositories - Organizer Publishing
        services.AddScoped<IFestivalRepository, SqlServerFestivalRepository>();
        services.AddScoped<IEditionRepository, SqlServerEditionRepository>();
        services.AddScoped<IVenueRepository, SqlServerVenueRepository>();
        services.AddScoped<IStageRepository, SqlServerStageRepository>();
        services.AddScoped<IArtistRepository, SqlServerArtistRepository>();
        services.AddScoped<IScheduleRepository, SqlServerScheduleRepository>();
        services.AddScoped<ITimeSlotRepository, SqlServerTimeSlotRepository>();
        services.AddScoped<IEngagementRepository, SqlServerEngagementRepository>();

        // Phase 4 Repositories - Attendee Experience
        services.AddScoped<IPersonalScheduleRepository, SqlServerPersonalScheduleRepository>();

        return services;
    }
}
