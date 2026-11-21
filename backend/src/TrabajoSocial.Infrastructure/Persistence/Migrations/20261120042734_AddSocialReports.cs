using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrabajoSocial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReportHeaderPath",
                schema: "dbo",
                table: "CompanySettings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SocialReports",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalRecordNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AdmissionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReligionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastNames = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    FirstNames = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    IdentityCard = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    PassportNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BirthPlace = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    BirthCountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BirthStateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BirthMunicipalityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    GenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaritalStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EducationLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProfessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccupationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HomeAddress = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    Phone1 = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    Phone2 = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    HasIvss = table.Column<bool>(type: "bit", nullable: true),
                    HasIpasme = table.Column<bool>(type: "bit", nullable: true),
                    InsurancePolicy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    OtherSocialSecurity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ReferredBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Workplace = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WorkAddress = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    WorkPhone = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    WorkSchedule = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    IsPediatric = table.Column<bool>(type: "bit", nullable: false),
                    GuardianFullName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GuardianIdentityCard = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    GuardianKinshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactAddress = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    ContactFullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    ContactKinshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MedicalSocialArea = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: true),
                    PsychosocialArea = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SocioeconomicArea = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    PhysicalEnvironmentArea = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ClinicalDiagnosis = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ReferralServices = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SocialWorkerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialReports_Countries_BirthCountryId",
                        column: x => x.BirthCountryId,
                        principalSchema: "catalog",
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReports_EducationLevels_EducationLevelId",
                        column: x => x.EducationLevelId,
                        principalSchema: "catalog",
                        principalTable: "EducationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReports_Genders_GenderId",
                        column: x => x.GenderId,
                        principalSchema: "catalog",
                        principalTable: "Genders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReports_Kinships_ContactKinshipId",
                        column: x => x.ContactKinshipId,
                        principalSchema: "catalog",
                        principalTable: "Kinships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReports_Kinships_GuardianKinshipId",
                        column: x => x.GuardianKinshipId,
                        principalSchema: "catalog",
                        principalTable: "Kinships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReports_MaritalStatuses_MaritalStatusId",
                        column: x => x.MaritalStatusId,
                        principalSchema: "catalog",
                        principalTable: "MaritalStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReports_Municipalities_BirthMunicipalityId",
                        column: x => x.BirthMunicipalityId,
                        principalSchema: "catalog",
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReports_Occupations_OccupationId",
                        column: x => x.OccupationId,
                        principalSchema: "catalog",
                        principalTable: "Occupations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReports_Professions_ProfessionId",
                        column: x => x.ProfessionId,
                        principalSchema: "catalog",
                        principalTable: "Professions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReports_Races_RaceId",
                        column: x => x.RaceId,
                        principalSchema: "catalog",
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReports_Religions_ReligionId",
                        column: x => x.ReligionId,
                        principalSchema: "catalog",
                        principalTable: "Religions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReports_SocialWorkers_SocialWorkerId",
                        column: x => x.SocialWorkerId,
                        principalSchema: "dbo",
                        principalTable: "SocialWorkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialReports_States_BirthStateId",
                        column: x => x.BirthStateId,
                        principalSchema: "catalog",
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_BirthCountryId",
                schema: "dbo",
                table: "SocialReports",
                column: "BirthCountryId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_BirthMunicipalityId",
                schema: "dbo",
                table: "SocialReports",
                column: "BirthMunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_BirthStateId",
                schema: "dbo",
                table: "SocialReports",
                column: "BirthStateId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_ContactKinshipId",
                schema: "dbo",
                table: "SocialReports",
                column: "ContactKinshipId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_CreatedBy",
                schema: "dbo",
                table: "SocialReports",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_EducationLevelId",
                schema: "dbo",
                table: "SocialReports",
                column: "EducationLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_GenderId",
                schema: "dbo",
                table: "SocialReports",
                column: "GenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_GuardianKinshipId",
                schema: "dbo",
                table: "SocialReports",
                column: "GuardianKinshipId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_IdentityCard",
                schema: "dbo",
                table: "SocialReports",
                column: "IdentityCard");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_MaritalStatusId",
                schema: "dbo",
                table: "SocialReports",
                column: "MaritalStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_MedicalRecordNumber",
                schema: "dbo",
                table: "SocialReports",
                column: "MedicalRecordNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_OccupationId",
                schema: "dbo",
                table: "SocialReports",
                column: "OccupationId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_ProfessionId",
                schema: "dbo",
                table: "SocialReports",
                column: "ProfessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_RaceId",
                schema: "dbo",
                table: "SocialReports",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_ReligionId",
                schema: "dbo",
                table: "SocialReports",
                column: "ReligionId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_SocialWorkerId_AdmissionDate",
                schema: "dbo",
                table: "SocialReports",
                columns: new[] { "SocialWorkerId", "AdmissionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SocialReports",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "ReportHeaderPath",
                schema: "dbo",
                table: "CompanySettings");
        }
    }
}
