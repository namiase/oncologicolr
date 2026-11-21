using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrabajoSocial.Domain.Entities.Catalogs;
using TrabajoSocial.Domain.Entities.Reports;
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
        builder.Property(e => e.ReportHeaderPath).HasMaxLength(300);
    }
}

internal sealed class SocialReportConfiguration : IEntityTypeConfiguration<SocialReport>
{
    public void Configure(EntityTypeBuilder<SocialReport> builder)
    {
        builder.ToTable("SocialReports", Schemas.Default);
        builder.ConfigureBase();

        builder.Property(e => e.MedicalRecordNumber).HasMaxLength(SocialReportLimits.MedicalRecordNumber).IsRequired();
        builder.Property(e => e.LastNames).HasMaxLength(SocialReportLimits.Names).IsRequired();
        builder.Property(e => e.FirstNames).HasMaxLength(SocialReportLimits.Names).IsRequired();
        builder.Property(e => e.IdentityCard).HasMaxLength(SocialReportLimits.IdentityCard);
        builder.Property(e => e.PassportNumber).HasMaxLength(SocialReportLimits.PassportNumber);
        builder.Property(e => e.BirthPlace).HasMaxLength(SocialReportLimits.BirthPlace);
        builder.Property(e => e.HomeAddress).HasMaxLength(SocialReportLimits.Address);
        builder.Property(e => e.InsurancePolicy).HasMaxLength(SocialReportLimits.SocialSecurity);
        builder.Property(e => e.OtherSocialSecurity).HasMaxLength(SocialReportLimits.SocialSecurity);
        builder.Property(e => e.ReferredBy).HasMaxLength(SocialReportLimits.FullLine);
        builder.Property(e => e.Workplace).HasMaxLength(SocialReportLimits.FullLine);
        builder.Property(e => e.WorkAddress).HasMaxLength(SocialReportLimits.Address);
        builder.Property(e => e.WorkSchedule).HasMaxLength(SocialReportLimits.WorkSchedule);
        builder.Property(e => e.GuardianFullName).HasMaxLength(SocialReportLimits.GuardianName);
        builder.Property(e => e.GuardianIdentityCard).HasMaxLength(SocialReportLimits.IdentityCard);
        builder.Property(e => e.ContactAddress).HasMaxLength(SocialReportLimits.Address);
        builder.Property(e => e.ContactFullName).HasMaxLength(SocialReportLimits.FullLine);
        builder.Property(e => e.MedicalSocialArea).HasMaxLength(SocialReportLimits.MedicalSocialArea);
        builder.Property(e => e.PsychosocialArea).HasMaxLength(SocialReportLimits.PsychosocialArea);
        builder.Property(e => e.SocioeconomicArea).HasMaxLength(SocialReportLimits.SocioeconomicArea);
        builder.Property(e => e.PhysicalEnvironmentArea).HasMaxLength(SocialReportLimits.PhysicalEnvironmentArea);
        builder.Property(e => e.ClinicalDiagnosis).HasMaxLength(SocialReportLimits.ClinicalDiagnosis);
        builder.Property(e => e.ReferralServices).HasMaxLength(SocialReportLimits.ReferralServices);
        builder.Property(e => e.Observations).HasMaxLength(SocialReportLimits.Observations);

        builder.HasIndex(e => new { e.SocialWorkerId, e.AdmissionDate });
        builder.HasIndex(e => e.CreatedBy);
        builder.HasIndex(e => e.MedicalRecordNumber);
        builder.HasIndex(e => e.IdentityCard);

        builder.HasOne(e => e.SocialWorker).WithMany().HasForeignKey(e => e.SocialWorkerId).IsRequired(false);
        builder.HasOne(e => e.Religion).WithMany().HasForeignKey(e => e.ReligionId);
        builder.HasOne(e => e.BirthCountry).WithMany().HasForeignKey(e => e.BirthCountryId);
        builder.HasOne(e => e.BirthState).WithMany().HasForeignKey(e => e.BirthStateId);
        builder.HasOne(e => e.BirthMunicipality).WithMany().HasForeignKey(e => e.BirthMunicipalityId);
        builder.HasOne(e => e.Gender).WithMany().HasForeignKey(e => e.GenderId);
        builder.HasOne(e => e.Race).WithMany().HasForeignKey(e => e.RaceId);
        builder.HasOne(e => e.MaritalStatus).WithMany().HasForeignKey(e => e.MaritalStatusId);
        builder.HasOne(e => e.EducationLevel).WithMany().HasForeignKey(e => e.EducationLevelId);
        builder.HasOne(e => e.Profession).WithMany().HasForeignKey(e => e.ProfessionId);
        builder.HasOne(e => e.Occupation).WithMany().HasForeignKey(e => e.OccupationId);
        builder.HasOne(e => e.GuardianKinship).WithMany().HasForeignKey(e => e.GuardianKinshipId);

        // Columnas de la primera versión, reemplazadas por SocialReportPhones. Se conservan (sin uso,
        // como propiedades "sombra") porque no se eliminan columnas con datos sin que el usuario lo
        // decida; la migración ReportPhones copió su contenido a la tabla nueva.
        builder.Property<string?>("Phone1").HasMaxLength(12);
        builder.Property<string?>("Phone2").HasMaxLength(12);
        builder.Property<string?>("WorkPhone").HasMaxLength(12);
        builder.Property<string?>("ContactPhone").HasMaxLength(12);
        builder.Property<Guid?>("ContactKinshipId");
        builder.HasOne<Kinship>().WithMany().HasForeignKey("ContactKinshipId");
    }
}

internal sealed class SocialReportPhoneConfiguration : IEntityTypeConfiguration<SocialReportPhone>
{
    public void Configure(EntityTypeBuilder<SocialReportPhone> builder)
    {
        builder.ToTable("SocialReportPhones", Schemas.Default);
        builder.ConfigureBase();

        builder.Property(e => e.Number).HasMaxLength(SocialReportLimits.PhoneNumber).IsRequired();
        builder.HasIndex(e => new { e.SocialReportId, e.Kind, e.SortOrder });

        builder.HasOne(e => e.SocialReport).WithMany(r => r.Phones).HasForeignKey(e => e.SocialReportId);
        builder.HasOne(e => e.AreaCode).WithMany().HasForeignKey(e => e.AreaCodeId);
        builder.HasOne(e => e.Kinship).WithMany().HasForeignKey(e => e.KinshipId);
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
