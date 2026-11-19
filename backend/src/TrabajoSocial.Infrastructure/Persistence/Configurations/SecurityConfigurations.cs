using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrabajoSocial.Domain.Entities.Security;
using static TrabajoSocial.Infrastructure.Persistence.Configurations.EntityTypeBuilderExtensions;

namespace TrabajoSocial.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", Schemas.Security);
        builder.ConfigureBase();

        builder.Property(e => e.Username).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(150);
        builder.Property(e => e.FullName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();

        builder.HasIndex(e => e.Username).IsUnique().HasFilter(NotDeleted);
        builder.HasIndex(e => e.Email).IsUnique().HasFilter(NotNullAndNotDeleted(nameof(User.Email)));

        // Un solo administrador principal.
        builder.HasIndex(e => e.IsSuperAdmin).IsUnique().HasFilter($"[{nameof(User.IsSuperAdmin)}] = 1");

        builder.HasOne(e => e.Role).WithMany(r => r.Users).HasForeignKey(e => e.RoleId);
        builder.HasOne(e => e.Status).WithMany().HasForeignKey(e => e.StatusId);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", Schemas.Security);
        builder.ConfigureBase();

        builder.Property(e => e.Name).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(250);
        builder.HasIndex(e => e.Name).IsUnique().HasFilter(NotDeleted);

        builder.HasOne(e => e.Status).WithMany().HasForeignKey(e => e.StatusId);
    }
}

internal sealed class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("Menus", Schemas.Security);
        builder.ConfigureBase();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Path).HasMaxLength(200);
        builder.Property(e => e.Icon).HasMaxLength(50);
        builder.HasIndex(e => e.Code).IsUnique().HasFilter(NotDeleted);

        builder.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId);
        builder.HasOne(e => e.Status).WithMany().HasForeignKey(e => e.StatusId);
    }
}

internal sealed class RoleMenuConfiguration : IEntityTypeConfiguration<RoleMenu>
{
    public void Configure(EntityTypeBuilder<RoleMenu> builder)
    {
        builder.ToTable("RoleMenus", Schemas.Security);
        builder.ConfigureBase();

        builder.HasIndex(e => new { e.RoleId, e.MenuId }).IsUnique().HasFilter(NotDeleted);
        builder.HasOne(e => e.Role).WithMany(r => r.RoleMenus).HasForeignKey(e => e.RoleId);
        builder.HasOne(e => e.Menu).WithMany(m => m.RoleMenus).HasForeignKey(e => e.MenuId);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", Schemas.Security);
        builder.ConfigureBase();

        builder.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(e => e.ReplacedByTokenHash).HasMaxLength(128);
        builder.HasIndex(e => e.TokenHash).IsUnique();

        builder.HasOne(e => e.User).WithMany(u => u.RefreshTokens).HasForeignKey(e => e.UserId);
        builder.HasOne(e => e.Session).WithMany(s => s.RefreshTokens).HasForeignKey(e => e.SessionId);
    }
}

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions", Schemas.Security);
        builder.ConfigureBase();

        builder.Property(e => e.RevokedReason).HasMaxLength(30);
        builder.Property(e => e.IpAddress).HasMaxLength(64);
        builder.Property(e => e.UserAgent).HasMaxLength(256);

        // Para recargar al iniciar las sesiones cerradas recientemente.
        builder.HasIndex(e => e.RevokedAt);
        builder.HasOne(e => e.User).WithMany(u => u.Sessions).HasForeignKey(e => e.UserId);
    }
}
