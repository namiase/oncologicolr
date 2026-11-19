using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrabajoSocial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanManageAdministrators",
                schema: "security",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuperAdmin",
                schema: "security",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Bases existentes: el principal es el administrador que creó el sistema (el del
            // seeder, CreatedBy = 'system'); si no lo hay, el administrador más antiguo.
            migrationBuilder.Sql("""
                UPDATE [security].[Users] SET [IsSuperAdmin] = 1
                WHERE [Id] = (
                    SELECT TOP (1) u.[Id]
                    FROM [security].[Users] u
                    JOIN [security].[Roles] r ON r.[Id] = u.[RoleId]
                    WHERE u.[IsDeleted] = 0 AND r.[Name] = N'Administrador'
                    ORDER BY CASE WHEN u.[CreatedBy] = N'system' THEN 0 ELSE 1 END, u.[CreatedAt]);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsSuperAdmin",
                schema: "security",
                table: "Users",
                column: "IsSuperAdmin",
                unique: true,
                filter: "[IsSuperAdmin] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IsSuperAdmin",
                schema: "security",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanManageAdministrators",
                schema: "security",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsSuperAdmin",
                schema: "security",
                table: "Users");
        }
    }
}
