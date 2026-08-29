using Microsoft.EntityFrameworkCore;

using SteamReviews.DataBase.Models;

namespace SteamReviews.DataBase;

public sealed class SteamReviewsCacheDbContext(DbContextOptions<SteamReviewsCacheDbContext> options) : DbContext(options)
{
    internal DbSet<SteamReviewsCacheEntry> AppReviews => Set<SteamReviewsCacheEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SteamReviewsCacheEntry>(entity =>
        {
            entity.ToTable("app_reviews_cache");
            entity.HasKey(entry => entry.ApplicationId);
            entity.Property(entry => entry.ApplicationId).HasColumnName("application_id");
            entity.Property(entry => entry.TotalReviews).HasColumnName("total_reviews");
            entity.Property(entry => entry.Rating).HasColumnName("rating");
            entity.Property(entry => entry.ExpiresAtUnixSeconds).HasColumnName("expires_at_unix_seconds");
        });
    }
}
