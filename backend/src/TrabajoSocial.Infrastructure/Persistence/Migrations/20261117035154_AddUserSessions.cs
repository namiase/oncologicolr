using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrabajoSocial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cada refresh token pasa a pertenecer a una sesión. Los existentes no tienen sesión,
            // así que se eliminan: los usuarios conectados solo deberán iniciar sesión otra vez.
            migrationBuilder.Sql("DELETE FROM [security].[RefreshTokens];");

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                schema: "security",
                table: "RefreshTokens",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "UserSessions",
                schema: "security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_SessionId",
                schema: "security",
                table: "RefreshTokens",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_RevokedAt",
                schema: "security",
                table: "UserSessions",
                column: "RevokedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                schema: "security",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_UserSessions_SessionId",
                schema: "security",
                table: "RefreshTokens",
                column: "SessionId",
                principalSchema: "security",
                principalTable: "UserSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_UserSessions_SessionId",
                schema: "security",
                table: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserSessions",
                schema: "security");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_SessionId",
                schema: "security",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "security",
                table: "RefreshTokens");
        }
    }
}
