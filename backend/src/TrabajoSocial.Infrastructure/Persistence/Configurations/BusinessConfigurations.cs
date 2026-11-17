using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrabajoSocial.Domain.Entities.Settings;
using TrabajoSocial.Domain.Entities.SocialWorkers;
using static TrabajoSocial.Infrastructure.Persistence.Configurations.EntityTypeBuilderExtensions;

namespace TrabajoSocial.Infrastructure.Persistence.Configurations;

internal sealed class SocialWorkerConfiguration : IEntityTypeConfiguration<SocialWorker>
{
    public void Configure(EntityTypeBuilder<SocialWorker> builder)
    {
        builder.ToTable("SocialWorkers", Schemas.Default);
        builder.ConfigureBase();

        builder.Property(e => e.FirstNames).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastNames).HasMaxLength(100).IsRequired();
        builder.Property(e => e.IdentityCard).HasMaxLength(15).IsRequired();
        builder.Property(e => e.AntsRegistrationNumber).HasMaxLength(30).IsRequired();
        builder.Property(e => e.SocialWorkersAssociation).HasMaxLength(200).IsRequired();

        builder.HasIndex(e => e.IdentityCard).IsUnique().HasFilter(NotDeleted);
        builder.HasIndex(e => e.AntsRegistrationNumber).IsUnique().HasFilter(NotDeleted);
        builder.HasIndex(e => e.UserId).IsUnique().HasFilter(NotNullAndNotDeleted(nameof(SocialWorker.UserId)));

        builder.HasOne(e => e.User).WithOne(u => u.SocialWorker).HasForeignKey<SocialWorker>(e => e.UserId);
        builder.HasOne(e => e.Status).WithMany().HasForeignKey(e => e.StatusId);
    }
}

internal sealed class CompanySettingConfiguration : IEntityTypeConfiguration<CompanySetting>
{
    public void Configure(EntityTypeBuilder<CompanySetting> builder)
    {
        builder.ToTable("CompanySettings", Schemas.Default);
        builder.ConfigureBase();

        builder.Property(e => e.CompanyName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.LogoPath).HasMaxLength(300);
    }
}

internal sealed class SeedHistoryConfiguration : IEntityTypeConfiguration<SeedHistory>
{
    public void Configure(EntityTypeBuilder<SeedHistory> builder)
    {
        builder.ToTable("SeedHistory", Schemas.Default);
        builder.ConfigureBase();

        builder.Property(e => e.Key).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Key).IsUnique();
    }
}
