using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Common;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Application.Common.Models;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Reports;
using TrabajoSocial.Domain.Entities.SocialWorkers;

namespace TrabajoSocial.Application.SocialWorkers;

public interface ISocialWorkerService
{
    Task<PagedResult<SocialWorkerDto>> GetPagedAsync(PagedQuery query, CancellationToken ct = default);
    Task<SocialWorkerDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <param name="onlyWithoutUser">Solo las que aún no tienen usuario del sistema vinculado.</param>
    Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(bool onlyWithoutUser = false, CancellationToken ct = default);
    Task<SocialWorkerDto> CreateAsync(SocialWorkerUpsertDto dto, CancellationToken ct = default);
    Task<SocialWorkerDto> UpdateAsync(Guid id, SocialWorkerUpsertDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class SocialWorkerService(
    IRepository<SocialWorker> repository,
    IRepository<SocialReport> reports,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ISocialWorkerService
{
    public async Task<PagedResult<SocialWorkerDto>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        var q = repository.Query();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(w => w.FirstNames.Contains(s) || w.LastNames.Contains(s)
                             || w.IdentityCard.Contains(s) || w.AntsRegistrationNumber.Contains(s)
                             || w.SocialWorkersAssociation.Contains(s));
        }

        if (query.StatusId is { } statusId)
        {
            q = q.Where(w => w.StatusId == statusId);
        }

        var desc = query.SortDesc;
        q = query.SortBy?.ToLowerInvariant() switch
        {
            "firstnames" => desc ? q.OrderByDescending(w => w.FirstNames) : q.OrderBy(w => w.FirstNames),
            "identitycard" => desc ? q.OrderByDescending(w => w.IdentityCard) : q.OrderBy(w => w.IdentityCard),
            "antsregistrationnumber" => desc ? q.OrderByDescending(w => w.AntsRegistrationNumber) : q.OrderBy(w => w.AntsRegistrationNumber),
            "graduationyear" => desc ? q.OrderByDescending(w => w.GraduationYear) : q.OrderBy(w => w.GraduationYear),
            "status" => desc ? q.OrderByDescending(w => w.Status.Name) : q.OrderBy(w => w.Status.Name),
            "createdat" => desc ? q.OrderByDescending(w => w.CreatedAt) : q.OrderBy(w => w.CreatedAt),
            _ => desc
                ? q.OrderByDescending(w => w.LastNames).ThenByDescending(w => w.FirstNames)
                : q.OrderBy(w => w.LastNames).ThenBy(w => w.FirstNames)
        };

        return await Project(q).ToPagedResultAsync(query.Page, query.PageSize, ct);
    }

    public Task<SocialWorkerDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Project(repository.Query().Where(w => w.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(bool onlyWithoutUser = false, CancellationToken ct = default)
        => await repository.Query()
            .Where(w => w.StatusId == StatusIds.Active && (!onlyWithoutUser || w.UserId == null))
            .OrderBy(w => w.LastNames).ThenBy(w => w.FirstNames)
            .Select(w => new LookupItemDto(w.Id, w.FirstNames + " " + w.LastNames, w.IdentityCard, null))
            .ToListAsync(ct);

    public async Task<SocialWorkerDto> CreateAsync(SocialWorkerUpsertDto dto, CancellationToken ct = default)
    {
        var entity = new SocialWorker();
        await ApplyAsync(entity, dto, currentId: null, ct);
        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return (await GetByIdAsync(entity.Id, ct))!;
    }

    public async Task<SocialWorkerDto> UpdateAsync(Guid id, SocialWorkerUpsertDto dto, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(id, ct) ?? throw NotFound();
        await ApplyAsync(entity, dto, currentId: id, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return (await GetByIdAsync(id, ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(id, ct) ?? throw NotFound();

        // Sus informes deben seguir mostrando quién los firmó.
        if (await reports.Query().AnyAsync(r => r.SocialWorkerId == id, ct))
        {
            throw new BusinessRuleException(
                "No se puede eliminar: la trabajadora social tiene informes registrados. Puede inactivarla.");
        }

        repository.Remove(entity);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task ApplyAsync(SocialWorker entity, SocialWorkerUpsertDto dto, Guid? currentId, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        var identityCard = IdentityCards.Normalize(dto.IdentityCard);
        if (identityCard is null)
        {
            errors["identityCard"] = [IdentityCards.FormatError];
        }

        var currentYear = time.GetUtcNow().Year;
        if (dto.GraduationYear < 1950 || dto.GraduationYear > currentYear)
        {
            errors["graduationYear"] = [$"El año de grado debe estar entre 1950 y {currentYear}."];
        }

        if (!StatusIds.Assignable.Contains(dto.StatusId))
        {
            errors["statusId"] = ["El estatus seleccionado no es válido."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var ants = dto.AntsRegistrationNumber.Trim();

        if (await repository.Query().AnyAsync(w => w.IdentityCard == identityCard && w.Id != currentId, ct))
        {
            throw new ConflictException($"Ya existe una trabajadora social con la cédula {identityCard}.", "identityCard");
        }

        if (await repository.Query().AnyAsync(w => w.AntsRegistrationNumber == ants && w.Id != currentId, ct))
        {
            throw new ConflictException($"Ya existe una trabajadora social con el registro ANTS {ants}.", "antsRegistrationNumber");
        }

        // El vínculo con un usuario del sistema se gestiona desde el módulo de Usuarios.
        entity.FirstNames = dto.FirstNames.Trim();
        entity.LastNames = dto.LastNames.Trim();
        entity.IdentityCard = identityCard!;
        entity.AntsRegistrationNumber = ants;
        entity.SocialWorkersAssociation = dto.SocialWorkersAssociation.Trim();
        entity.GraduationYear = dto.GraduationYear;
        entity.StatusId = dto.StatusId;
    }

    private static IQueryable<SocialWorkerDto> Project(IQueryable<SocialWorker> q)
        => q.Select(w => new SocialWorkerDto
        {
            Id = w.Id,
            FirstNames = w.FirstNames,
            LastNames = w.LastNames,
            IdentityCard = w.IdentityCard,
            AntsRegistrationNumber = w.AntsRegistrationNumber,
            SocialWorkersAssociation = w.SocialWorkersAssociation,
            GraduationYear = w.GraduationYear,
            UserId = w.UserId,
            Username = w.User != null ? w.User.Username : null,
            StatusId = w.StatusId,
            StatusName = w.Status.Name,
            CreatedAt = w.CreatedAt,
            CreatedBy = w.CreatedBy,
            UpdatedAt = w.UpdatedAt,
            UpdatedBy = w.UpdatedBy
        });

    private static NotFoundException NotFound() => new("La trabajadora social solicitada no existe.");
}
