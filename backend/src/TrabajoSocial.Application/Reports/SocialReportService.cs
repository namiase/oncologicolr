using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Catalogs;
using TrabajoSocial.Application.Common;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Application.Common.Models;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Reports;
using TrabajoSocial.Domain.Entities.Security;
using TrabajoSocial.Domain.Entities.Settings;
using TrabajoSocial.Domain.Entities.SocialWorkers;

namespace TrabajoSocial.Application.Reports;

public interface ISocialReportService
{
    Task<SocialReportContextDto> GetContextAsync(CancellationToken ct = default);
    Task<PagedResult<SocialReportListItemDto>> GetPagedAsync(PagedQuery query, CancellationToken ct = default);
    Task<SocialReportDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SocialReportDto> CreateAsync(SocialReportUpsertDto dto, CancellationToken ct = default);
    Task<SocialReportDto> UpdateAsync(Guid id, SocialReportUpsertDto dto, CancellationToken ct = default);

    /// <param name="signerId">Solo administradores: otra trabajadora social para la firma del PDF.</param>
    Task<SocialReportFile> GetPdfAsync(Guid id, Guid? signerId = null, CancellationToken ct = default);
}

/// <summary>
/// Informes sociales (entrevista preliminar). Un administrador ve todos; los demás usuarios,
/// solo los que crearon o en los que son la trabajadora tratante. Para el resto, el informe
/// "no existe" (404): no se revela que existe.
/// </summary>
public sealed partial class SocialReportService(
    IRepository<SocialReport> reports,
    IRepository<SocialReportPhone> phones,
    IRepository<SocialWorker> socialWorkers,
    IRepository<User> users,
    IRepository<CompanySetting> settings,
    ICatalogServiceFactory catalogs,
    ICurrentUserService currentUser,
    IFileStorage fileStorage,
    ISocialReportPdfRenderer pdfRenderer,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ISocialReportService
{
    private static readonly DateOnly MinAdmissionDate = new(1950, 1, 1);
    private static readonly DateOnly MinBirthDate = new(1900, 1, 1);

    /// <summary>Usuario que hace la petición.</summary>
    private sealed record Actor(string Username, bool IsAdministrator, SocialWorkerSummaryDto? SocialWorker);

    /// <summary>Valor de una lista: un registro de la tabla maestra o un texto nuevo ("Otro").</summary>
    private sealed record Choice(Guid? Id, string? Other)
    {
        public bool IsSet => Id is not null || Other is not null;
    }

    /// <summary>Teléfono validado, antes de resolver los "Otro".</summary>
    private sealed record PhoneDraft(Choice AreaCode, string Number, Choice Kinship);

    /// <summary>Teléfono listo para guardar.</summary>
    private sealed record PhoneRow(Guid AreaCodeId, string Number, Guid? KinshipId);

    /// <summary>Campo y tamaño máximo de cada grupo de teléfonos.</summary>
    private static readonly (ReportPhoneKind Kind, string Field, int Max, bool WithKinship)[] PhoneGroups =
    [
        (ReportPhoneKind.Patient, "phones", SocialReportLimits.PatientPhones, false),
        (ReportPhoneKind.Work, "workPhones", SocialReportLimits.WorkPhones, false),
        (ReportPhoneKind.Contact, "contactPhones", SocialReportLimits.ContactPhones, true)
    ];

    public async Task<SocialReportContextDto> GetContextAsync(CancellationToken ct = default)
    {
        var actor = await GetActorAsync(ct);
        return new SocialReportContextDto(actor.SocialWorker, actor.IsAdministrator);
    }

    public async Task<PagedResult<SocialReportListItemDto>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(ct);
        var q = Visible(actor);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(r => r.MedicalRecordNumber.Contains(s) || r.LastNames.Contains(s)
                             || r.FirstNames.Contains(s) || (r.IdentityCard != null && r.IdentityCard.Contains(s)));
        }

        var desc = query.SortDesc;
        q = query.SortBy?.ToLowerInvariant() switch
        {
            "medicalrecordnumber" => desc ? q.OrderByDescending(r => r.MedicalRecordNumber) : q.OrderBy(r => r.MedicalRecordNumber),
            "patientname" => desc
                ? q.OrderByDescending(r => r.LastNames).ThenByDescending(r => r.FirstNames)
                : q.OrderBy(r => r.LastNames).ThenBy(r => r.FirstNames),
            "admissiondate" => desc ? q.OrderByDescending(r => r.AdmissionDate) : q.OrderBy(r => r.AdmissionDate),
            _ => q.OrderByDescending(r => r.CreatedAt)
        };

        return await q
            .Select(r => new SocialReportListItemDto(
                r.Id,
                r.MedicalRecordNumber,
                r.LastNames + ", " + r.FirstNames,
                r.IdentityCard,
                r.AdmissionDate,
                r.SocialWorkerId,
                r.SocialWorker != null ? r.SocialWorker.FirstNames + " " + r.SocialWorker.LastNames : null,
                r.CreatedAt,
                r.UpdatedAt))
            .ToPagedResultAsync(query.Page, query.PageSize, ct);
    }

    public async Task<SocialReportDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await GetAsync(id, await GetActorAsync(ct), ct);

    public async Task<SocialReportDto> CreateAsync(SocialReportUpsertDto dto, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(ct);
        var entity = new SocialReport();
        await ApplyAsync(entity, dto, actor, isNew: true, ct);
        await reports.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return (await GetAsync(entity.Id, actor, ct))!;
    }

    public async Task<SocialReportDto> UpdateAsync(Guid id, SocialReportUpsertDto dto, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(ct);
        if (!await Visible(actor).AnyAsync(r => r.Id == id, ct))
        {
            throw NotFound();
        }

        var entity = await reports.QueryTracked().Include(r => r.Phones).FirstOrDefaultAsync(r => r.Id == id, ct)
                     ?? throw NotFound();
        await ApplyAsync(entity, dto, actor, isNew: false, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return (await GetAsync(id, actor, ct))!;
    }

    public async Task<SocialReportFile> GetPdfAsync(Guid id, Guid? signerId = null, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(ct);
        var row = await Visible(actor)
            .Where(r => r.Id == id)
            .Select(r => new
            {
                Report = r,
                Religion = r.Religion!.Name,
                BirthCountry = r.BirthCountry!.Name,
                BirthState = r.BirthState!.Name,
                BirthMunicipality = r.BirthMunicipality!.Name,
                GenderCode = r.Gender!.Code,
                GenderName = r.Gender!.Name,
                Race = r.Race!.Name,
                MaritalStatus = r.MaritalStatus!.Name,
                EducationLevel = r.EducationLevel!.Name,
                Profession = r.Profession!.Name,
                Occupation = r.Occupation!.Name,
                GuardianKinship = r.GuardianKinship!.Name,
                Worker = r.SocialWorker,
                Phones = r.Phones
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.SortOrder)
                    .Select(p => new
                    {
                        p.Kind,
                        AreaCode = p.AreaCode.Code ?? p.AreaCode.Name,
                        p.Number,
                        Kinship = p.Kinship != null ? p.Kinship.Name : null
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct) ?? throw NotFound();

        var r = row.Report;

        // Firma: la trabajadora del informe; un administrador puede elegir otra al imprimir.
        var signer = row.Worker;
        if (actor.IsAdministrator && signerId is { } chosen && chosen != signer?.Id)
        {
            signer = await socialWorkers.Query().FirstOrDefaultAsync(w => w.Id == chosen, ct)
                     ?? throw new ValidationException("socialWorkerId", "La trabajadora social seleccionada no existe.");
        }

        // Código de área + número; en los de familiares, también el parentesco.
        string? PhoneList(ReportPhoneKind kind) => Join(" / ", [
            .. row.Phones.Where(p => p.Kind == kind)
                .Select(p => p.Kinship is null || kind != ReportPhoneKind.Contact
                    ? $"{p.AreaCode}-{p.Number}"
                    : $"{p.AreaCode}-{p.Number} ({p.Kinship})")
        ]);
        var branding = await settings.Query().OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(ct);
        var headerImage = branding?.ReportHeaderPath is { } headerPath ? await fileStorage.ReadAsync(headerPath, ct) : null;
        var logo = headerImage is null && branding?.LogoPath is { } logoPath ? await fileStorage.ReadAsync(logoPath, ct) : null;

        var genders = await catalogs.Get("genders").GetLookupAsync(null, ct);
        var maritalStatuses = await catalogs.Get("marital-statuses").GetLookupAsync(null, ct);
        var nationality = r.IdentityCard?[..1];

        var model = new SocialReportPrintModel
        {
            CompanyName = branding?.CompanyName ?? string.Empty,
            HeaderImage = headerImage,
            Logo = logo,

            MedicalRecordNumber = r.MedicalRecordNumber,
            AdmissionDate = FormatDate(r.AdmissionDate),
            Religion = row.Religion,

            PatientName = $"{r.LastNames}, {r.FirstNames}",
            IdentityNumber = r.IdentityCard?[2..],
            Nationality = [new PrintOption("V", nationality == "V"), new PrintOption("E", nationality == "E")],
            PassportNumber = r.PassportNumber,
            BirthPlace = r.BirthPlace,
            BirthCountry = row.BirthCountry,
            BirthState = row.BirthState,
            BirthMunicipality = row.BirthMunicipality,
            BirthDate = r.BirthDate is { } birth ? FormatDate(birth) : null,
            Age = r.BirthDate is { } b ? PatientAge.Describe(b, r.AdmissionDate) : null,
            Gender = Options(genders, r.GenderId, row.GenderCode ?? row.GenderName, g => g.Code ?? g.Name),
            Race = row.Race,
            MaritalStatus = Options(maritalStatuses, r.MaritalStatusId, row.MaritalStatus, m => m.Name),
            EducationLevel = row.EducationLevel,
            ProfessionOrOccupation = row.Profession ?? row.Occupation,
            HomeAddress = r.HomeAddress,
            Phones = PhoneList(ReportPhoneKind.Patient),

            Ivss = [new PrintOption("Sí", r.HasIvss == true), new PrintOption("No", r.HasIvss == false)],
            Ipasme = YesNo(r.HasIpasme),
            InsurancePolicy = r.InsurancePolicy,
            OtherSocialSecurity = r.OtherSocialSecurity,
            ReferredBy = r.ReferredBy,

            Workplace = r.Workplace,
            WorkAddress = r.WorkAddress,
            WorkPhones = PhoneList(ReportPhoneKind.Work),
            WorkSchedule = r.WorkSchedule,

            IsPediatric = r.IsPediatric,
            GuardianFullName = r.GuardianFullName,
            GuardianIdentityCard = r.GuardianIdentityCard,
            GuardianKinship = row.GuardianKinship,

            ContactAddress = r.ContactAddress,
            ContactFullName = r.ContactFullName,
            ContactPhoneAndKinship = PhoneList(ReportPhoneKind.Contact),

            MedicalSocialArea = r.MedicalSocialArea,
            PsychosocialArea = r.PsychosocialArea,
            SocioeconomicArea = r.SocioeconomicArea,
            PhysicalEnvironmentArea = r.PhysicalEnvironmentArea,
            ClinicalDiagnosis = r.ClinicalDiagnosis,
            ReferralServices = r.ReferralServices,
            Observations = r.Observations,

            SocialWorkerName = signer?.FullName,
            SocialWorkerIdentityCard = signer?.IdentityCard,
            SocialWorkerAntsNumber = signer?.AntsRegistrationNumber,
            SocialWorkerAssociation = signer?.SocialWorkersAssociation
        };

        return new SocialReportFile(pdfRenderer.Render(model), FileName(r));
    }

    // ---------------------------------------------------------------------------------------

    private async Task<Actor> GetActorAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new AuthenticationFailedException("Sesión no válida.");
        var user = await users.Query()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Username,
                IsAdministrator = u.Role.Name == RoleNames.Administrator,
                WorkerId = u.SocialWorker != null && u.SocialWorker.StatusId == StatusIds.Active
                    ? u.SocialWorker.Id
                    : (Guid?)null
            })
            .FirstOrDefaultAsync(ct) ?? throw new AuthenticationFailedException("Sesión no válida.");

        var worker = user.WorkerId is { } workerId
            ? await socialWorkers.Query()
                .Where(w => w.Id == workerId)
                .Select(w => new SocialWorkerSummaryDto(
                    w.Id, w.FirstNames + " " + w.LastNames, w.IdentityCard, w.AntsRegistrationNumber, w.SocialWorkersAssociation))
                .FirstOrDefaultAsync(ct)
            : null;
        return new Actor(user.Username, user.IsAdministrator, worker);
    }

    /// <summary>
    /// Informes no eliminados que el usuario puede ver. Se ignoran los filtros globales para que
    /// un catálogo o una trabajadora eliminados después no oculten el informe ni sus datos.
    /// </summary>
    private IQueryable<SocialReport> Visible(Actor actor)
    {
        var q = reports.Query().IgnoreQueryFilters().Where(r => !r.IsDeleted);
        if (actor.IsAdministrator)
        {
            return q;
        }

        var username = actor.Username;
        var workerId = actor.SocialWorker?.Id;
        return q.Where(r => r.CreatedBy == username || r.SocialWorkerId == workerId);
    }

    private Task<SocialReportDto?> GetAsync(Guid id, Actor actor, CancellationToken ct)
        => Visible(actor).Where(r => r.Id == id).Select(r => new SocialReportDto
        {
            Id = r.Id,
            MedicalRecordNumber = r.MedicalRecordNumber,
            AdmissionDate = r.AdmissionDate,
            ReligionId = r.ReligionId,
            LastNames = r.LastNames,
            FirstNames = r.FirstNames,
            IdentityCard = r.IdentityCard,
            PassportNumber = r.PassportNumber,
            BirthPlace = r.BirthPlace,
            BirthCountryId = r.BirthCountryId,
            BirthStateId = r.BirthStateId,
            BirthMunicipalityId = r.BirthMunicipalityId,
            BirthDate = r.BirthDate,
            GenderId = r.GenderId,
            RaceId = r.RaceId,
            MaritalStatusId = r.MaritalStatusId,
            EducationLevelId = r.EducationLevelId,
            ProfessionId = r.ProfessionId,
            OccupationId = r.OccupationId,
            HomeAddress = r.HomeAddress,
            Phones = r.Phones
                .Where(p => !p.IsDeleted && p.Kind == ReportPhoneKind.Patient)
                .OrderBy(p => p.SortOrder)
                .Select(p => new SocialReportPhoneDto(p.AreaCodeId, p.AreaCode.Code ?? p.AreaCode.Name, p.Number, null, null))
                .ToList(),
            HasIvss = r.HasIvss,
            HasIpasme = r.HasIpasme,
            InsurancePolicy = r.InsurancePolicy,
            OtherSocialSecurity = r.OtherSocialSecurity,
            ReferredBy = r.ReferredBy,
            Workplace = r.Workplace,
            WorkAddress = r.WorkAddress,
            WorkPhones = r.Phones
                .Where(p => !p.IsDeleted && p.Kind == ReportPhoneKind.Work)
                .OrderBy(p => p.SortOrder)
                .Select(p => new SocialReportPhoneDto(p.AreaCodeId, p.AreaCode.Code ?? p.AreaCode.Name, p.Number, null, null))
                .ToList(),
            WorkSchedule = r.WorkSchedule,
            IsPediatric = r.IsPediatric,
            GuardianFullName = r.GuardianFullName,
            GuardianIdentityCard = r.GuardianIdentityCard,
            GuardianKinshipId = r.GuardianKinshipId,
            ContactAddress = r.ContactAddress,
            ContactFullName = r.ContactFullName,
            ContactPhones = r.Phones
                .Where(p => !p.IsDeleted && p.Kind == ReportPhoneKind.Contact)
                .OrderBy(p => p.SortOrder)
                .Select(p => new SocialReportPhoneDto(
                    p.AreaCodeId, p.AreaCode.Code ?? p.AreaCode.Name, p.Number, p.KinshipId, p.Kinship != null ? p.Kinship.Name : null))
                .ToList(),
            MedicalSocialArea = r.MedicalSocialArea,
            PsychosocialArea = r.PsychosocialArea,
            SocioeconomicArea = r.SocioeconomicArea,
            PhysicalEnvironmentArea = r.PhysicalEnvironmentArea,
            ClinicalDiagnosis = r.ClinicalDiagnosis,
            ReferralServices = r.ReferralServices,
            Observations = r.Observations,
            SocialWorkerId = r.SocialWorkerId,
            SocialWorkerName = r.SocialWorker != null ? r.SocialWorker.FirstNames + " " + r.SocialWorker.LastNames : null,
            CreatedAt = r.CreatedAt,
            CreatedBy = r.CreatedBy,
            UpdatedAt = r.UpdatedAt,
            UpdatedBy = r.UpdatedBy
        }).FirstOrDefaultAsync(ct);

    private async Task ApplyAsync(SocialReport entity, SocialReportUpsertDto dto, Actor actor, bool isNew, CancellationToken ct)
    {
        var socialWorkerId = ResolveSocialWorker(entity, actor, isNew);
        var errors = new Dictionary<string, string[]>();

        var medicalRecordNumber = Clean(dto.MedicalRecordNumber);
        var lastNames = Clean(dto.LastNames);
        var firstNames = Clean(dto.FirstNames);
        if (medicalRecordNumber is null)
        {
            errors["medicalRecordNumber"] = ["El N° de historia clínica es obligatorio."];
        }

        if (lastNames is null)
        {
            errors["lastNames"] = ["Los apellidos son obligatorios."];
        }

        if (firstNames is null)
        {
            errors["firstNames"] = ["Los nombres son obligatorios."];
        }

        // La fecha local de Venezuela nunca es posterior a la fecha UTC; el día extra cubre relojes desfasados.
        var latestAdmission = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime).AddDays(1);
        var admission = dto.AdmissionDate ?? default;
        if (dto.AdmissionDate is null)
        {
            errors["admissionDate"] = ["La fecha de ingreso es obligatoria."];
        }
        else if (admission > latestAdmission || admission < MinAdmissionDate)
        {
            errors["admissionDate"] = ["La fecha de ingreso no puede ser futura."];
        }

        if (dto.BirthDate is { } birthDate && (birthDate < MinBirthDate || (dto.AdmissionDate is not null && birthDate > admission)))
        {
            errors["birthDate"] = ["La fecha de nacimiento debe ser anterior a la fecha de ingreso."];
        }

        var identityCard = NormalizeOptional(dto.IdentityCard, IdentityCards.Normalize, "identityCard", IdentityCards.FormatError, errors);

        // Paciente de pediatría: los datos del representante solo se guardan si aplica.
        var guardianFullName = dto.IsPediatric ? Clean(dto.GuardianFullName) : null;
        var guardianIdentityCard = dto.IsPediatric
            ? NormalizeOptional(dto.GuardianIdentityCard, IdentityCards.Normalize, "guardianIdentityCard", IdentityCards.FormatError, errors)
            : null;
        if (dto.IsPediatric && guardianFullName is null)
        {
            errors["guardianFullName"] = ["Indique el representante del paciente de pediatría."];
        }

        // Listas: un registro de la tabla maestra o un valor nuevo escrito en "Otro".
        var religion = await CheckChoiceAsync("religions", dto.ReligionId, dto.ReligionOther, entity.ReligionId, "religion", errors, ct);
        var race = await CheckChoiceAsync("races", dto.RaceId, dto.RaceOther, entity.RaceId, "race", errors, ct);
        var maritalStatus = await CheckChoiceAsync(
            "marital-statuses", dto.MaritalStatusId, dto.MaritalStatusOther, entity.MaritalStatusId, "maritalStatus", errors, ct);
        var educationLevel = await CheckChoiceAsync(
            "education-levels", dto.EducationLevelId, dto.EducationLevelOther, entity.EducationLevelId, "educationLevel", errors, ct);
        var profession = await CheckChoiceAsync(
            "professions", dto.ProfessionId, dto.ProfessionOther, entity.ProfessionId, "profession", errors, ct);
        var occupation = await CheckChoiceAsync(
            "occupations", dto.OccupationId, dto.OccupationOther, entity.OccupationId, "occupation", errors, ct);
        var guardianKinship = dto.IsPediatric
            ? await CheckChoiceAsync("kinships", dto.GuardianKinshipId, dto.GuardianKinshipOther, entity.GuardianKinshipId,
                "guardianKinship", errors, ct, SocialReportLimits.OtherKinship)
            : new Choice(null, null);
        if (profession.IsSet && occupation.IsSet)
        {
            errors["professionId"] = ["Indique la profesión o el oficio, no ambos."];
        }

        await CheckCatalogAsync("genders", dto.GenderId, entity.GenderId, "genderId", errors, ct);
        await CheckCatalogAsync("countries", dto.BirthCountryId, entity.BirthCountryId, "birthCountryId", errors, ct);
        await CheckCatalogAsync("states", dto.BirthStateId, entity.BirthStateId, "birthStateId", errors, ct,
            dto.BirthCountryId, "El estado no pertenece al país seleccionado.");
        await CheckCatalogAsync("municipalities", dto.BirthMunicipalityId, entity.BirthMunicipalityId, "birthMunicipalityId", errors, ct,
            dto.BirthStateId, "El municipio no pertenece al estado seleccionado.");

        var phoneDrafts = new Dictionary<ReportPhoneKind, List<PhoneDraft>>();
        foreach (var (kind, field, max, withKinship) in PhoneGroups)
        {
            var input = kind switch
            {
                ReportPhoneKind.Patient => dto.Phones,
                ReportPhoneKind.Work => dto.WorkPhones,
                _ => dto.ContactPhones
            };
            phoneDrafts[kind] = await CheckPhonesAsync(entity, kind, input, field, max, withKinship, errors, ct);
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        // Los valores nuevos se agregan a las tablas maestras antes de tocar el informe: agregarlos
        // guarda en la base, y al editar no debe quedar guardado el informe a medias.
        var religionId = await ResolveAsync("religions", religion, ct);
        var raceId = await ResolveAsync("races", race, ct);
        var maritalStatusId = await ResolveAsync("marital-statuses", maritalStatus, ct);
        var educationLevelId = await ResolveAsync("education-levels", educationLevel, ct);
        var professionId = await ResolveAsync("professions", profession, ct);
        var occupationId = await ResolveAsync("occupations", occupation, ct);
        var guardianKinshipId = await ResolveAsync("kinships", guardianKinship, ct);

        var resolvedPhones = new Dictionary<ReportPhoneKind, List<PhoneRow>>();
        foreach (var (kind, drafts) in phoneDrafts)
        {
            var rows = new List<PhoneRow>();
            foreach (var draft in drafts)
            {
                var areaCodeId = await ResolveAsync("area-codes", draft.AreaCode, ct);
                rows.Add(new PhoneRow(areaCodeId!.Value, draft.Number, await ResolveAsync("kinships", draft.Kinship, ct)));
            }

            resolvedPhones[kind] = rows;
        }

        entity.MedicalRecordNumber = medicalRecordNumber!;
        entity.AdmissionDate = admission;
        entity.ReligionId = religionId;
        entity.LastNames = lastNames!;
        entity.FirstNames = firstNames!;
        entity.IdentityCard = identityCard;
        entity.PassportNumber = Clean(dto.PassportNumber);
        entity.BirthPlace = Clean(dto.BirthPlace);
        entity.BirthCountryId = dto.BirthCountryId;
        entity.BirthStateId = dto.BirthStateId;
        entity.BirthMunicipalityId = dto.BirthMunicipalityId;
        entity.BirthDate = dto.BirthDate;
        entity.GenderId = dto.GenderId;
        entity.RaceId = raceId;
        entity.MaritalStatusId = maritalStatusId;
        entity.EducationLevelId = educationLevelId;
        entity.ProfessionId = professionId;
        entity.OccupationId = occupationId;
        entity.HomeAddress = Clean(dto.HomeAddress);

        entity.HasIvss = dto.HasIvss;
        entity.HasIpasme = dto.HasIpasme;
        entity.InsurancePolicy = Clean(dto.InsurancePolicy);
        entity.OtherSocialSecurity = Clean(dto.OtherSocialSecurity);
        entity.ReferredBy = Clean(dto.ReferredBy);

        entity.Workplace = Clean(dto.Workplace);
        entity.WorkAddress = Clean(dto.WorkAddress);
        entity.WorkSchedule = Clean(dto.WorkSchedule);

        entity.IsPediatric = dto.IsPediatric;
        entity.GuardianFullName = guardianFullName;
        entity.GuardianIdentityCard = guardianIdentityCard;
        entity.GuardianKinshipId = guardianKinshipId;

        entity.ContactAddress = Clean(dto.ContactAddress);
        entity.ContactFullName = Clean(dto.ContactFullName);

        entity.MedicalSocialArea = CleanText(dto.MedicalSocialArea);
        entity.PsychosocialArea = CleanText(dto.PsychosocialArea);
        entity.SocioeconomicArea = CleanText(dto.SocioeconomicArea);
        entity.PhysicalEnvironmentArea = CleanText(dto.PhysicalEnvironmentArea);
        entity.ClinicalDiagnosis = CleanText(dto.ClinicalDiagnosis);
        entity.ReferralServices = CleanText(dto.ReferralServices);
        entity.Observations = CleanText(dto.Observations);

        entity.SocialWorkerId = socialWorkerId;

        foreach (var (kind, rows) in resolvedPhones)
        {
            SyncPhones(entity, kind, rows);
        }
    }

    /// <summary>
    /// Trabajadora tratante: la vinculada al usuario que crea el informe (no se elige en el
    /// formulario). Un administrador sin trabajadora vinculada puede crearlo sin ella: elige quién
    /// firma al descargar el PDF. Al editar no cambia.
    /// </summary>
    private static Guid? ResolveSocialWorker(SocialReport entity, Actor actor, bool isNew)
    {
        if (!isNew)
        {
            return entity.SocialWorkerId;
        }

        if (actor.SocialWorker is { } worker)
        {
            return worker.Id;
        }

        return actor.IsAdministrator
            ? null
            : throw new BusinessRuleException(
                "Su usuario no está vinculado a una trabajadora social. Pida al administrador que lo vincule.");
    }

    /// <summary>
    /// Valida una lista con opción "Otro"; los errores van a "{field}Id" o "{field}Other".
    /// Si se escribió algo en "Otro", manda el texto (se agrega a la tabla maestra al guardar).
    /// </summary>
    private async Task<Choice> CheckChoiceAsync(
        string key, Guid? id, string? other, Guid? current, string field, Dictionary<string, string[]> errors,
        CancellationToken ct, int maxLength = SocialReportLimits.OtherValue)
    {
        if (Clean(other) is { } text)
        {
            if (text.Length > maxLength)
            {
                errors[$"{field}Other"] = [$"Máximo {maxLength} caracteres."];
            }

            return new Choice(null, text);
        }

        await CheckCatalogAsync(key, id, current, $"{field}Id", errors, ct);
        return new Choice(id, null);
    }

    /// <summary>
    /// Valida un grupo de teléfonos. Las filas vacías se ignoran; los errores llevan el índice de
    /// la fila tal como llegó (p. ej. "contactPhones[2].number").
    /// </summary>
    private async Task<List<PhoneDraft>> CheckPhonesAsync(
        SocialReport entity, ReportPhoneKind kind, IReadOnlyList<SocialReportPhoneInput> input, string field, int max,
        bool withKinship, Dictionary<string, string[]> errors, CancellationToken ct)
    {
        var current = entity.Phones.Where(p => p.Kind == kind && !p.IsDeleted).ToList();
        var drafts = new List<PhoneDraft>();

        for (var i = 0; i < input.Count; i++)
        {
            var row = input[i];
            var prefix = $"{field}[{i}].";
            var digits = DigitsRegex().Replace(row.Number ?? string.Empty, "");
            var areaOther = Clean(row.AreaCodeOther);
            var kinshipId = withKinship ? row.KinshipId : null;
            var kinshipOther = withKinship ? Clean(row.KinshipOther) : null;

            if (row.AreaCodeId is null && areaOther is null && digits.Length == 0 && kinshipId is null && kinshipOther is null)
            {
                continue; // fila vacía
            }

            if (areaOther is not null)
            {
                if (!AreaCodeRegex().IsMatch(areaOther))
                {
                    errors[prefix + "areaCodeOther"] = ["El código de área tiene 4 dígitos y empieza por 0 (p. ej. 0261)."];
                }
            }
            else if (row.AreaCodeId is null)
            {
                errors[prefix + "areaCodeId"] = ["Seleccione el código de área."];
            }
            else
            {
                var known = current.Any(p => p.AreaCodeId == row.AreaCodeId) ? row.AreaCodeId : null;
                await CheckCatalogAsync("area-codes", row.AreaCodeId, known, prefix + "areaCodeId", errors, ct);
            }

            if (digits.Length != SocialReportLimits.PhoneNumber)
            {
                errors[prefix + "number"] = [$"El número debe tener {SocialReportLimits.PhoneNumber} dígitos."];
            }

            var kinship = new Choice(null, null);
            if (withKinship)
            {
                var knownKinship = current.Any(p => p.KinshipId == kinshipId) ? kinshipId : null;
                kinship = await CheckChoiceAsync("kinships", kinshipId, kinshipOther, knownKinship, prefix + "kinship", errors, ct,
                    SocialReportLimits.OtherKinship);
            }

            drafts.Add(new PhoneDraft(new Choice(areaOther is null ? row.AreaCodeId : null, areaOther), digits, kinship));
        }

        if (drafts.Count > max)
        {
            errors[field] = [$"Máximo {max} teléfonos."];
        }

        return drafts;
    }

    /// <summary>Id del valor elegido; si se escribió en "Otro", lo busca o lo agrega a la tabla maestra.</summary>
    private async Task<Guid?> ResolveAsync(string key, Choice choice, CancellationToken ct)
    {
        if (choice.Other is not { } text)
        {
            return choice.Id;
        }

        // Un código de área nuevo se guarda con el código también como nombre ("0261").
        return await catalogs.Get(key).FindOrCreateAsync(text, key == "area-codes" ? text : null, ct);
    }

    /// <summary>
    /// Deja los teléfonos de un grupo como en el formulario: actualiza en orden las filas que ya
    /// había, agrega las nuevas y quita (borrado lógico) las que sobran.
    /// </summary>
    private void SyncPhones(SocialReport entity, ReportPhoneKind kind, IReadOnlyList<PhoneRow> rows)
    {
        var existing = entity.Phones.Where(p => p.Kind == kind && !p.IsDeleted).OrderBy(p => p.SortOrder).ToList();
        for (var i = 0; i < rows.Count; i++)
        {
            var phone = i < existing.Count ? existing[i] : null;
            if (phone is null)
            {
                phone = new SocialReportPhone { Kind = kind };
                entity.Phones.Add(phone);
            }

            phone.AreaCodeId = rows[i].AreaCodeId;
            phone.Number = rows[i].Number;
            phone.KinshipId = rows[i].KinshipId;
            phone.SortOrder = i + 1;
        }

        foreach (var extra in existing.Skip(rows.Count))
        {
            phones.Remove(extra);
        }
    }

    /// <summary>
    /// El registro elegido debe existir y estar activo (se acepta el valor que ya tenía el informe,
    /// aunque luego se haya inactivado). Si se indica <paramref name="parentId"/>, debe ser su padre.
    /// </summary>
    private async Task CheckCatalogAsync(
        string key, Guid? id, Guid? current, string field, Dictionary<string, string[]> errors, CancellationToken ct,
        Guid? parentId = null, string? parentError = null)
    {
        if (id is not { } value)
        {
            return;
        }

        var item = await catalogs.Get(key).GetByIdAsync(value, ct);
        if (item is null)
        {
            // Un valor que el informe ya tenía se conserva aunque luego se haya eliminado del catálogo.
            if (value != current)
            {
                errors[field] = ["El valor seleccionado no es válido."];
            }
        }
        else if (item.StatusId != StatusIds.Active && value != current)
        {
            errors[field] = ["El valor seleccionado no es válido."];
        }
        else if (parentError is not null && item.ParentId != parentId)
        {
            errors[field] = [parentError];
        }
    }

    private static string? NormalizeOptional(
        string? value, Func<string, string?> normalize, string field, string error, Dictionary<string, string[]> errors)
    {
        if (Clean(value) is not { } text)
        {
            return null;
        }

        var normalized = normalize(text);
        if (normalized is null)
        {
            errors[field] = [error];
        }

        return normalized;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Textos largos: conserva los saltos de línea, sin espacios sobrantes al final de cada línea.</summary>
    private static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var lines = value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Select(l => l.TrimEnd());
        return string.Join('\n', lines).Trim();
    }

    /// <summary>Opciones marcables de un catálogo; si el valor del informe ya no está activo, se agrega.</summary>
    private static IReadOnlyList<PrintOption> Options(
        IReadOnlyList<LookupItemDto> items, Guid? selectedId, string? selectedLabel, Func<LookupItemDto, string> label)
    {
        var options = items.Select(i => new PrintOption(label(i), i.Id == selectedId)).ToList();
        if (selectedId is not null && items.All(i => i.Id != selectedId) && selectedLabel is not null)
        {
            options.Add(new PrintOption(selectedLabel, true));
        }

        return options;
    }

    private static string? YesNo(bool? value) => value switch { true => "Sí", false => "No", null => null };

    private static string? Join(string separator, params string?[] values)
    {
        var parts = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
        return parts.Length == 0 ? null : string.Join(separator, parts);
    }

    private static string FormatDate(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>"Entrevista-HC12345-Perez-Gonzalez.pdf": sin acentos ni caracteres especiales.</summary>
    private static string FileName(SocialReport r)
        => $"Entrevista-HC{Slug(r.MedicalRecordNumber)}-{Slug(r.LastNames)}.pdf";

    private static string Slug(string value)
    {
        var builder = new StringBuilder();
        foreach (var c in value.Normalize(NormalizationForm.FormD))
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                builder.Append(c);
            }
            else if ((char.IsWhiteSpace(c) || c is '-' or '_' or '/') && builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim('-');
    }

    private static NotFoundException NotFound() => new("El informe solicitado no existe.");

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsRegex();

    [GeneratedRegex(@"^0\d{3}$")]
    private static partial Regex AreaCodeRegex();
}
