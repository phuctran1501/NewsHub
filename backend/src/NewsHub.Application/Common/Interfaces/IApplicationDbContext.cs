namespace NewsHub.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using NewsHub.Domain.Entities;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<MediaFile> MediaFiles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
