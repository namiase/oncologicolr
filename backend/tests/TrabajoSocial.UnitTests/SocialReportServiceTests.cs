using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Application.Common.Models;
using TrabajoSocial.Application.Reports;
using TrabajoSocial.Application.SocialWorkers;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Catalogs;
using TrabajoSocial.Domain.Entities.Reports;
using TrabajoSocial.Domain.Entities.Security;
using TrabajoSocial.Domain.Entities.SocialWorkers;
using TrabajoSocial.Infrastructure.Persistence;
using TrabajoSocial.UnitTests.Support;

namespace TrabajoSocial.UnitTests;

public sealed class SocialReportServiceTests
{
    private static readonly DateOnly Today = new(2026, 11, 16); // fecha de TestHost.Time

    /// <summary>Crea una trabajadora social y, si se indica, un usuario vinculado a ella.</summary>
    internal static Task<(Guid WorkerId, Guid? UserId)> CreateWorker(
        TestHost host, string identityCard, string? username = null, string role = RoleNames.SocialWorker)
        => host.RunAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            User? user = null;
            if (username is not null)
            {
                var roleId = (await db.Roles.SingleAsync(r => r.Name == role)).Id;
                user = new User { Username = username, FullName = username, RoleId = roleId, PasswordHash = "x" };
                db.Users.Add(user);
            }

            var worker = new SocialWorker
            {
                FirstNames = "María",
                LastNames = $"López {identityCard}",
                IdentityCard = identityCard,
                AntsRegistrationNumber = $"ANTS-{identityCard}",
                SocialWorkersAssociation = "Colegio de Trabajadores Sociales del Distrito Capital",
                GraduationYear = 2005,
                User = user
            };
            db.SocialWorkers.Add(worker);
            await db.SaveChangesAsync();
            return (worker.Id, user?.Id);
        });

    internal static Task<Guid> UserId(TestHost host, string username)
        => host.RunAsync(async sp => (await sp.GetRequiredService<AppDbContext>().Users.SingleAsync(u => u.Username == username)).Id);

    internal static void ActAs(TestHost host, Guid userId, string username)
    {
        host.CurrentUser.UserId = userId;
        host.CurrentUser.Username = username;
    }

    internal static SocialReportUpsertDto Report(Action<SocialReportUpsertDto>? customize = null)
    {
        var dto = new SocialReportUpsertDto
        {
            MedicalRecordNumber = "  123456 ",
            AdmissionDate = Today,
            LastNames = "Pérez González",
            FirstNames = "Ana Isabel",
            BirthDate = new DateOnly(1980, 5, 20)
        };
        customize?.Invoke(dto);
        return dto;
    }

    private static Task<T> Run<T>(TestHost host, Func<ISocialReportService, Task<T>> action)
        => host.RunAsync(sp => action(sp.GetRequiredService<ISocialReportService>()));

    /// <summary>Id de un código de área sembrado (0212, 0412, 0414, 0424, 0416, 0426).</summary>
    internal static Task<Guid> AreaCode(TestHost host, string code)
        => host.RunAsync(async sp => (await sp.GetRequiredService<AppDbContext>().AreaCodes.SingleAsync(a => a.Code == code)).Id);

    private static Task<T> Db<T>(TestHost host, Func<AppDbContext, Task<T>> query)
        => host.RunAsync(sp => query(sp.GetRequiredService<AppDbContext>()));

    [Fact]
    public async Task Social_worker_signs_her_reports_and_data_is_normalized()
    {
        await using var host = await TestHost.CreateAsync();
        var (mine, userId) = await CreateWorker(host, "V-1111111", "mlopez");
        ActAs(host, userId!.Value, "mlopez");
        var mobile = await AreaCode(host, "0414");

        var context = await Run(host, s => s.GetContextAsync());
        Assert.Equal(mine, context.SocialWorker?.Id);
        Assert.False(context.CanChooseSocialWorker);

        var created = await Run(host, s => s.CreateAsync(Report(r =>
        {
            r.IdentityCard = "v12.345.678";
            r.Phones = [new() { AreaCodeId = mobile, Number = "123 45 67" }, new()]; // la fila vacía se ignora
            r.GuardianFullName = "No aplica"; // no es paciente de pediatría
            r.SocioeconomicArea = "Vive con su madre.  \r\n\r\nIngreso familiar bajo.   ";
        })));

        Assert.Equal(mine, created.SocialWorkerId);
        Assert.Equal("123456", created.MedicalRecordNumber);
        Assert.Equal("V-12345678", created.IdentityCard);
        var phone = Assert.Single(created.Phones);
        Assert.Equal(("0414", "1234567"), (phone.AreaCode, phone.Number));
        Assert.Null(created.GuardianFullName);
        Assert.Equal("Vive con su madre.\n\nIngreso familiar bajo.", created.SocioeconomicArea);
        Assert.Equal("mlopez", created.CreatedBy);
    }

    [Fact]
    public async Task Administrator_without_link_creates_reports_and_chooses_the_signer_when_printing()
    {
        await using var host = await TestHost.CreateAsync();
        var (worker, mlopez) = await CreateWorker(host, "V-1111111", "mlopez");
        ActAs(host, await UserId(host, "admin"), "admin");

        var context = await Run(host, s => s.GetContextAsync());
        Assert.Null(context.SocialWorker);
        Assert.True(context.CanChooseSocialWorker);

        var created = await Run(host, s => s.CreateAsync(Report()));
        Assert.Null(created.SocialWorkerId);

        // Sin firma elegida, el PDF sale con la firma en blanco; con firma, con la trabajadora.
        Assert.NotEmpty((await Run(host, s => s.GetPdfAsync(created.Id))).Content);
        Assert.NotEmpty((await Run(host, s => s.GetPdfAsync(created.Id, worker))).Content);
        var missing = await Assert.ThrowsAsync<ValidationException>(() => Run(host, s => s.GetPdfAsync(created.Id, Guid.NewGuid())));
        Assert.Contains("socialWorkerId", missing.Errors.Keys);

        // Una trabajadora social no puede cambiar la firma: el parámetro se ignora.
        ActAs(host, mlopez!.Value, "mlopez");
        var own = await Run(host, s => s.CreateAsync(Report()));
        Assert.NotEmpty((await Run(host, s => s.GetPdfAsync(own.Id, Guid.NewGuid()))).Content);
    }

    [Fact]
    public async Task Social_worker_user_without_link_cannot_create_reports()
    {
        await using var host = await TestHost.CreateAsync();
        var userId = await Db(host, async db =>
        {
            var roleId = (await db.Roles.SingleAsync(r => r.Name == RoleNames.SocialWorker)).Id;
            var user = new User { Username = "sinvinculo", FullName = "Sin vínculo", RoleId = roleId, PasswordHash = "x" };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.Id;
        });
        ActAs(host, userId, "sinvinculo");

        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(host, s => s.CreateAsync(Report())));
    }

    [Fact]
    public async Task Reports_are_only_visible_to_their_author_treating_worker_and_administrators()
    {
        await using var host = await TestHost.CreateAsync();
        var (_, mlopez) = await CreateWorker(host, "V-1111111", "mlopez");
        var (_, jperez) = await CreateWorker(host, "V-2222222", "jperez");

        ActAs(host, mlopez!.Value, "mlopez");
        var report = await Run(host, s => s.CreateAsync(Report()));

        ActAs(host, jperez!.Value, "jperez");
        Assert.Null(await Run(host, s => s.GetByIdAsync(report.Id)));
        await Assert.ThrowsAsync<NotFoundException>(() => Run(host, s => s.UpdateAsync(report.Id, Report())));
        await Assert.ThrowsAsync<NotFoundException>(() => Run(host, s => s.GetPdfAsync(report.Id)));
        Assert.Empty((await Run(host, s => s.GetPagedAsync(new PagedQuery()))).Items);

        ActAs(host, await UserId(host, "admin"), "admin");
        var all = await Run(host, s => s.GetPagedAsync(new PagedQuery { Search = "123456" }));
        Assert.Equal("Pérez González, Ana Isabel", Assert.Single(all.Items).PatientName);
    }

    [Fact]
    public async Task Other_values_are_added_once_to_the_master_tables()
    {
        await using var host = await TestHost.CreateAsync();
        var (_, mlopez) = await CreateWorker(host, "V-1111111", "mlopez");
        ActAs(host, mlopez!.Value, "mlopez");

        var first = await Run(host, s => s.CreateAsync(Report(r =>
        {
            r.RaceOther = "  mestiza ";
            r.ProfessionOther = "Bioanalista";
            r.ContactPhones = [new() { AreaCodeOther = "0261", Number = "7654321", KinshipOther = "vecina" }];
        })));

        // El mismo valor escrito de otra forma reutiliza el registro: no se duplica.
        var second = await Run(host, s => s.CreateAsync(Report(r =>
        {
            r.RaceOther = "MESTIZA";
            r.ContactPhones = [new() { AreaCodeOther = "0261", Number = "1112233", KinshipOther = "Vecina" }];
        })));

        var races = await Db(host, db => db.Races.Where(x => x.Name.ToLower() == "mestiza").ToListAsync());
        var race = Assert.Single(races);
        Assert.Equal("Mestiza", race.Name);
        Assert.Equal(StatusIds.Active, race.StatusId);
        Assert.Equal("mlopez", race.CreatedBy);
        Assert.Equal(race.Id, first.RaceId);
        Assert.Equal(race.Id, second.RaceId);

        var areaCode = Assert.Single(await Db(host, db => db.AreaCodes.Where(a => a.Code == "0261").ToListAsync()));
        Assert.Equal("0261", areaCode.Name);
        Assert.Single(await Db(host, db => db.Kinships.Where(k => k.Name == "Vecina").ToListAsync()));
        Assert.NotNull(first.ProfessionId);
        Assert.Equal("0261-7654321 · Vecina", $"{first.ContactPhones[0].AreaCode}-{first.ContactPhones[0].Number} · {first.ContactPhones[0].Kinship}");
    }

    [Fact]
    public async Task Editing_keeps_the_treating_social_worker_and_syncs_the_phones()
    {
        await using var host = await TestHost.CreateAsync();
        var (mine, mlopez) = await CreateWorker(host, "V-1111111", "mlopez");
        ActAs(host, mlopez!.Value, "mlopez");
        var (mobile, landline) = (await AreaCode(host, "0414"), await AreaCode(host, "0212"));
        var report = await Run(host, s => s.CreateAsync(Report(r => r.Phones =
        [
            new() { AreaCodeId = mobile, Number = "1111111" },
            new() { AreaCodeId = landline, Number = "2222222" },
            new() { AreaCodeId = mobile, Number = "3333333" }
        ])));

        var updated = await Run(host, s => s.UpdateAsync(report.Id, Report(r =>
        {
            r.Observations = "Se refiere a farmacia.";
            r.Phones = [new() { AreaCodeId = landline, Number = "4444444" }];
        })));

        Assert.Equal(mine, updated.SocialWorkerId);
        Assert.Equal("Se refiere a farmacia.", updated.Observations);
        Assert.Equal("mlopez", updated.UpdatedBy);
        Assert.Equal(("0212", "4444444"), (Assert.Single(updated.Phones).AreaCode, updated.Phones[0].Number));

        // Los que sobran quedan con borrado lógico, como el resto del sistema.
        var rows = await Db(host, db => db.SocialReportPhones.IgnoreQueryFilters().Where(p => p.SocialReportId == report.Id).ToListAsync());
        Assert.Equal(3, rows.Count);
        Assert.Equal(2, rows.Count(p => p.IsDeleted));
    }

    [Fact]
    public async Task Invalid_data_is_rejected_field_by_field()
    {
        await using var host = await TestHost.CreateAsync();
        var (_, mlopez) = await CreateWorker(host, "V-1111111", "mlopez");
        ActAs(host, mlopez!.Value, "mlopez");
        var mobile = await AreaCode(host, "0414");

        var (profession, occupation, country, foreignState) = await Db(host, async db =>
        {
            var colombia = new Country { Code = "CO", Name = "Colombia" };
            var antioquia = new State { Name = "Antioquia", Parent = colombia };
            var p = new Profession { Name = "Docente" };
            var o = new Occupation { Name = "Albañil" };
            db.AddRange(colombia, antioquia, p, o);
            await db.SaveChangesAsync();
            var venezuela = await db.Countries.SingleAsync(c => c.Code == "VE");
            return (p.Id, o.Id, venezuela.Id, antioquia.Id);
        });

        var ex = await Assert.ThrowsAsync<ValidationException>(() => Run(host, s => s.CreateAsync(Report(r =>
        {
            r.MedicalRecordNumber = "   ";
            r.AdmissionDate = Today.AddDays(5);
            r.BirthDate = Today.AddDays(10);
            r.IdentityCard = "X-123";
            r.ProfessionId = profession;
            r.OccupationId = occupation;
            r.IsPediatric = true;
            r.BirthCountryId = country;
            r.BirthStateId = foreignState;
            r.GenderId = Guid.NewGuid();
            r.Phones = [new() { AreaCodeId = mobile, Number = "123" }, new() { Number = "1234567" }];
            r.WorkPhones = [new() { AreaCodeOther = "261", Number = "1234567" }];
            r.ContactPhones = [.. Enumerable.Range(0, SocialReportLimits.ContactPhones + 1)
                .Select(_ => new SocialReportPhoneInput { AreaCodeId = mobile, Number = "1234567" })];
        }))));

        Assert.Equal(
            new[]
            {
                "admissionDate", "birthDate", "birthStateId", "contactPhones", "genderId", "guardianFullName",
                "identityCard", "medicalRecordNumber", "phones[0].number", "phones[1].areaCodeId", "professionId",
                "workPhones[0].areaCodeOther"
            },
            ex.Errors.Keys.Order());
    }

    [Fact]
    public async Task Social_worker_with_reports_cannot_be_deleted()
    {
        await using var host = await TestHost.CreateAsync();
        var (worker, mlopez) = await CreateWorker(host, "V-1111111", "mlopez");
        ActAs(host, mlopez!.Value, "mlopez");
        await Run(host, s => s.CreateAsync(Report()));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            host.RunAsync(sp => sp.GetRequiredService<ISocialWorkerService>().DeleteAsync(worker)));
    }

    [Theory]
    [InlineData("1980-05-20", "2026-11-16", "46 años")]
    [InlineData("2025-11-16", "2026-11-16", "1 año")]
    [InlineData("2026-03-17", "2026-11-16", "7 meses")]
    [InlineData("2026-10-16", "2026-11-16", "1 mes")]
    [InlineData("2026-11-04", "2026-11-16", "12 días")]
    public void Age_is_described_at_the_admission_date(string birth, string admission, string expected)
        => Assert.Equal(expected, PatientAge.Describe(DateOnly.Parse(birth), DateOnly.Parse(admission)));
}
