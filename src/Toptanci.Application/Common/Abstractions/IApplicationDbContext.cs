using Microsoft.EntityFrameworkCore;
using Toptanci.Domain.Entities;

namespace Toptanci.Application.Common.Abstractions;

/// <summary>
/// Application katmanının veritabanına eriştiği soyutlama. DbSet'ler entity geldikçe eklenir.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
