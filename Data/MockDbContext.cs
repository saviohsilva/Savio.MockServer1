using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Data;

public class MockDbContext : IdentityDbContext<ApplicationUser>
{
    public MockDbContext(DbContextOptions<MockDbContext> options) : base(options)
    {
    }

    public DbSet<MockEndpointEntity> MockEndpoints { get; set; }
    public DbSet<RequestHistoryEntity> RequestHistory { get; set; }
    public DbSet<UnmockedRequestEntity> UnmockedRequests { get; set; }
    public DbSet<MockBinaryBlobEntity> MockBinaryBlobs { get; set; }
    public DbSet<MockGroupEntity> MockGroups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.Alias).IsUnique();
        });

        modelBuilder.Entity<MockGroupEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MockEndpointEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Route, e.Method });
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.MockGroup)
                .WithMany(g => g.MockEndpoints)
                .HasForeignKey(e => e.MockGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RequestHistoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MockEndpointId);
            entity.HasIndex(e => e.RequestedAt);

            entity.HasOne(e => e.MockEndpoint)
                .WithMany(m => m.RequestHistory)
                .HasForeignKey(e => e.MockEndpointId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UnmockedRequestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Route, e.Method });
            entity.HasIndex(e => e.MockCreated);
            entity.HasIndex(e => e.LastSeenAt);
        });

        modelBuilder.Entity<MockBinaryBlobEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
