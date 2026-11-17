using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrabajoSocial.Application.Catalogs;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Infrastructure.Persistence;
using TrabajoSocial.UnitTests.Support;

namespace TrabajoSocial.UnitTests;

public sealed class CatalogServiceTests
{
    private static CatalogUpsertDto Item(string name, Guid? parentId = null, Guid? statusId = null, string? code = null)
        => new() { Name = name, Code = code, ParentId = parentId, StatusId = statusId ?? StatusIds.Active };

    private static ICatalogService Catalog(IServiceProvider sp, string key)
        => sp.GetRequiredService<ICatalogServiceFactory>().Get(key);

    [Fact]
    public async Task Create_sets_audit_fields_and_delete_is_logical()
    {
        await using var host = await TestHost.CreateAsync();

        var created = await host.RunAsync(sp => Catalog(sp, "genders").CreateAsync(Item("  Otro  ")));
        Assert.Equal("Otro", created.Name);
        Assert.Equal("tester", created.CreatedBy);
        Assert.Equal(host.Time.Now.UtcDateTime, created.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, created.CreatedAt.Kind);
        Assert.Null(created.UpdatedAt);

        host.CurrentUser.Username = "editor";
        host.Time.Advance(TimeSpan.FromMinutes(5));
        await host.RunAsync(sp => Catalog(sp, "genders").DeleteAsync(created.Id));

        await host.RunAsync(async sp =>
        {
            Assert.Null(await Catalog(sp, "genders").GetByIdAsync(created.Id));

            var row = await sp.GetRequiredService<AppDbContext>().Genders
                .IgnoreQueryFilters().SingleAsync(g => g.Id == created.Id);
            Assert.True(row.IsDeleted);
            Assert.Equal(StatusIds.Deleted, row.StatusId);
            Assert.Equal("editor", row.UpdatedBy);
            Assert.Equal(host.Time.Now.UtcDateTime, row.UpdatedAt);
            Assert.Equal("tester", row.CreatedBy); // la creación no se sobrescribe
        });
    }

    [Fact]
    public async Task Create_with_duplicate_name_throws_conflict_on_name_field()
    {
        await using var host = await TestHost.CreateAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            host.RunAsync(sp => Catalog(sp, "genders").CreateAsync(Item("Femenino"))));
        Assert.Equal("name", ex.Field);
    }

    [Fact]
    public async Task Name_can_be_reused_after_soft_delete()
    {
        await using var host = await TestHost.CreateAsync();

        var first = await host.RunAsync(sp => Catalog(sp, "religions").CreateAsync(Item("Católica")));
        await host.RunAsync(sp => Catalog(sp, "religions").DeleteAsync(first.Id));
        var second = await host.RunAsync(sp => Catalog(sp, "religions").CreateAsync(Item("Católica")));

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task Child_catalog_requires_parent()
    {
        await using var host = await TestHost.CreateAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            host.RunAsync(sp => Catalog(sp, "states").CreateAsync(Item("Antioquia"))));
        Assert.Contains("parentId", ex.Errors.Keys);
    }

    [Fact]
    public async Task Child_names_are_unique_per_parent_only()
    {
        await using var host = await TestHost.CreateAsync();

        var colombia = await host.RunAsync(sp => Catalog(sp, "countries").CreateAsync(Item("Colombia", code: "CO")));

        // "Sucre" existe en Venezuela (seed) y también puede existir en Colombia.
        var sucre = await host.RunAsync(sp => Catalog(sp, "states").CreateAsync(Item("Sucre", colombia.Id)));
        Assert.Equal("Colombia", sucre.ParentName);

        await Assert.ThrowsAsync<ConflictException>(() =>
            host.RunAsync(sp => Catalog(sp, "states").CreateAsync(Item("Sucre", colombia.Id))));
    }

    [Fact]
    public async Task Parent_with_children_cannot_be_deleted()
    {
        await using var host = await TestHost.CreateAsync();

        var venezuelaId = await host.RunAsync(async sp =>
            (await sp.GetRequiredService<AppDbContext>().Countries.SingleAsync(c => c.Code == "VE")).Id);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            host.RunAsync(sp => Catalog(sp, "countries").DeleteAsync(venezuelaId)));
        Assert.Contains("Estados", ex.Message);
    }

    [Fact]
    public async Task Lookup_returns_only_active_items_filtered_by_parent()
    {
        await using var host = await TestHost.CreateAsync();

        var created = await host.RunAsync(sp => Catalog(sp, "kinships").CreateAsync(Item("Vecino", statusId: StatusIds.Inactive)));
        var lookup = await host.RunAsync(sp => Catalog(sp, "kinships").GetLookupAsync(null));
        Assert.DoesNotContain(lookup, i => i.Id == created.Id);

        var states = await host.RunAsync(sp => Catalog(sp, "states").GetLookupAsync(Guid.NewGuid()));
        Assert.Empty(states);
    }

    [Fact]
    public async Task Paged_query_filters_searches_and_sorts()
    {
        await using var host = await TestHost.CreateAsync();

        var page = await host.RunAsync(sp => Catalog(sp, "states").GetPagedAsync(
            new CatalogQuery { Search = "ri", Page = 1, PageSize = 10, SortBy = "name" }));

        Assert.Equal(new[] { "Barinas", "Distrito Capital", "Guárico", "Mérida" }, page.Items.Select(i => i.Name));
        Assert.All(page.Items, i => Assert.Equal("Venezuela", i.ParentName));

        var second = await host.RunAsync(sp => Catalog(sp, "states").GetPagedAsync(
            new CatalogQuery { Page = 2, PageSize = 10, SortBy = "name", SortDesc = true }));
        Assert.Equal(25, second.TotalCount);
        Assert.Equal(3, second.TotalPages);
        Assert.Equal(10, second.Items.Count);
    }

    [Fact]
    public async Task Invalid_status_is_rejected()
    {
        await using var host = await TestHost.CreateAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            host.RunAsync(sp => Catalog(sp, "races").CreateAsync(Item("X", statusId: StatusIds.Deleted))));
        Assert.Contains("statusId", ex.Errors.Keys);
    }

    [Fact]
    public async Task Every_registered_catalog_resolves_and_queries()
    {
        await using var host = await TestHost.CreateAsync();

        foreach (var definition in CatalogRegistry.All)
        {
            var count = await host.RunAsync(sp => Catalog(sp, definition.Key).CountAsync());
            Assert.True(count >= 0);
        }

        await Assert.ThrowsAsync<NotFoundException>(() => host.RunAsync(sp => Task.FromResult(Catalog(sp, "nope"))));
    }
}
