using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrabajoSocial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReportPhones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SocialWorkerId",
                schema: "dbo",
                table: "SocialReports",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateTable(
                name: "SocialReportPhones",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocialReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    AreaCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    KinshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialReportPhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialReportPhones_AreaCodes_AreaCodeId",
                        column: x => x.AreaCodeId,
                        principalSchema: "catalog",
                        principalTable: "AreaCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReportPhones_Kinships_KinshipId",
                        column: x => x.KinshipId,
                        principalSchema: "catalog",
                        principalTable: "Kinships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReportPhones_SocialReports_SocialReportId",
                        column: x => x.SocialReportId,
                        principalSchema: "dbo",
                        principalTable: "SocialReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SocialReportPhones_AreaCodeId",
                schema: "dbo",
                table: "SocialReportPhones",
                column: "AreaCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReportPhones_KinshipId",
                schema: "dbo",
                table: "SocialReportPhones",
                column: "KinshipId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReportPhones_SocialReportId_Kind_SortOrder",
                schema: "dbo",
                table: "SocialReportPhones",
                columns: new[] { "SocialReportId", "Kind", "SortOrder" });

            // Copia los teléfonos de la primera versión (columnas Phone1, Phone2, WorkPhone,
            // ContactPhone y ContactKinshipId, que se conservan sin uso) a la tabla nueva.
            // 1) Códigos de área que usan esos teléfonos y aún no están en la tabla maestra.
            migrationBuilder.Sql(@"
INSERT INTO catalog.AreaCodes (Id, Code, Name, SortOrder, StatusId, CreatedAt, CreatedBy, IsDeleted)
SELECT NEWID(), n.Code, n.Code,
       (SELECT ISNULL(MAX(SortOrder), 0) FROM catalog.AreaCodes) + ROW_NUMBER() OVER (ORDER BY n.Code),
       '00000000-0000-0000-0000-000000000001', SYSUTCDATETIME(), 'migracion', 0
FROM (
    SELECT DISTINCT LEFT(v.Phone, 4) AS Code
    FROM dbo.SocialReports r
    CROSS APPLY (VALUES (r.Phone1), (r.Phone2), (r.WorkPhone), (r.ContactPhone)) AS v(Phone)
    WHERE r.IsDeleted = 0 AND v.Phone LIKE '0[0-9][0-9][0-9]-[0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
) AS n
WHERE NOT EXISTS (SELECT 1 FROM catalog.AreaCodes a WHERE (a.Code = n.Code OR a.Name = n.Code) AND a.IsDeleted = 0);");

            // 2) Los teléfonos, con su código de área y (en el de familiares) su parentesco.
            migrationBuilder.Sql(@"
INSERT INTO dbo.SocialReportPhones (Id, SocialReportId, Kind, AreaCodeId, Number, KinshipId, SortOrder, CreatedAt, CreatedBy, IsDeleted)
SELECT NEWID(), r.Id, v.Kind, a.Id, RIGHT(v.Phone, 7), v.KinshipId, v.SortOrder, SYSUTCDATETIME(), r.CreatedBy, 0
FROM dbo.SocialReports r
CROSS APPLY (VALUES (r.Phone1, 1, 1, CAST(NULL AS uniqueidentifier)),
                    (r.Phone2, 1, 2, NULL),
                    (r.WorkPhone, 2, 1, NULL),
                    (r.ContactPhone, 3, 1, r.ContactKinshipId)) AS v(Phone, Kind, SortOrder, KinshipId)
CROSS APPLY (SELECT TOP (1) ac.Id FROM catalog.AreaCodes ac
             WHERE (ac.Code = LEFT(v.Phone, 4) OR ac.Name = LEFT(v.Phone, 4)) AND ac.IsDeleted = 0
             ORDER BY CASE WHEN ac.Code = LEFT(v.Phone, 4) THEN 0 ELSE 1 END) AS a
WHERE r.IsDeleted = 0 AND v.Phone LIKE '0[0-9][0-9][0-9]-[0-9][0-9][0-9][0-9][0-9][0-9][0-9]';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SocialReportPhones",
                schema: "dbo");

            migrationBuilder.AlterColumn<Guid>(
                name: "SocialWorkerId",
                schema: "dbo",
                table: "SocialReports",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
