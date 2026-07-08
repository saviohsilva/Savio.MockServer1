using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Data;

public class MockDbContext(DbContextOptions<MockDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<MockEndpointEntity> MockEndpoints { get; set; }
    public DbSet<RequestHistoryEntity> RequestHistory { get; set; }
    public DbSet<UnmockedRequestEntity> UnmockedRequests { get; set; }
    public DbSet<MockBinaryBlobEntity> MockBinaryBlobs { get; set; }
    public DbSet<MockGroupEntity> MockGroups { get; set; }
    public DbSet<EmailSettingEntity> EmailSettings { get; set; }
    public DbSet<MockCertificateEntity> MockCertificates { get; set; }
    public DbSet<MockAuthConfigEntity> MockAuthConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.Alias).IsUnique();
        });

        builder.Entity<MockGroupEntity>(entity =>
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

        builder.Entity<MockEndpointEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Route, e.Method });
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.MockGroup)
                .WithMany(g => g.MockEndpoints)
                .HasForeignKey(e => e.MockGroupId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AuthConfig)
                .WithMany(a => a.MockEndpoints)
                .HasForeignKey(e => e.AuthConfigId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.RequiredClientCertificate)
                .WithMany()
                .HasForeignKey(e => e.RequiredClientCertificateId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<MockAuthConfigEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.RequiredCertificate)
                .WithMany(c => c.AuthConfigs)
                .HasForeignKey(e => e.RequiredCertificateId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MockCertificateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Thumbprint);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RequestHistoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MockEndpointId);
            entity.HasIndex(e => e.RequestedAt);
            entity.HasIndex(e => new { e.MockEndpointId, e.RequestedAt });

            entity.HasOne(e => e.MockEndpoint)
                .WithMany(m => m.RequestHistory)
                .HasForeignKey(e => e.MockEndpointId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UnmockedRequestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Route, e.Method });
            entity.HasIndex(e => e.MockCreated);
            entity.HasIndex(e => e.LastSeenAt);
            entity.HasIndex(e => e.UserId);
        });

        builder.Entity<MockBinaryBlobEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
        });

        builder.Entity<EmailSettingEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
        });
    }
}
