using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;
using Toptanci.Domain.Enums;

namespace Toptanci.Infrastructure.Persistence;

/// <summary>
/// Uygulama başlarken migration'ları uygular ve hiç kullanıcı yoksa varsayılan admin'i oluşturur.
/// </summary>
public sealed class ApplicationDbContextInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApplicationDbContextInitializer> _logger;

    public ApplicationDbContextInitializer(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<ApplicationDbContextInitializer> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Veritabanı migration sırasında hata oluştu.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        if (await _context.Users.AnyAsync())
            return;

        var userName = _configuration["DefaultAdmin:UserName"] ?? "admin";
        var password = _configuration["DefaultAdmin:Password"] ?? "Admin123!";

        _context.Users.Add(new User
        {
            UserName = userName,
            FullName = "Sistem Yöneticisi",
            Role = UserRole.Admin,
            IsActive = true,
            PasswordHash = _passwordHasher.Hash(password)
        });

        await _context.SaveChangesAsync();
        _logger.LogInformation("Varsayılan admin kullanıcısı oluşturuldu: {UserName}", userName);
    }
}
