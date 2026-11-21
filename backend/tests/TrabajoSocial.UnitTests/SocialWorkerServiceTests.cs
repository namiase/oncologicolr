using Microsoft.Extensions.DependencyInjection;
using TrabajoSocial.Application.Common;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Application.SocialWorkers;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.UnitTests.Support;

namespace TrabajoSocial.UnitTests;

public sealed class SocialWorkerServiceTests
{
    [Theory]
    [InlineData("V-12345678", "V-12345678")]
    [InlineData("v12.345.678", "V-12345678")]
    [InlineData(" e 1234567 ", "E-1234567")]
    [InlineData("X-12345678", null)]
    [InlineData("V-12", null)]
    public void Identity_card_is_normalized(string input, string? expected)
        => Assert.Equal(expected, IdentityCards.Normalize(input));

    private static SocialWorkerUpsertDto Worker(string identityCard, string ants, short year = 2005) => new()
    {
        FirstNames = "María José",
        LastNames = "Pérez",
        IdentityCard = identityCard,
        AntsRegistrationNumber = ants,
        SocialWorkersAssociation = "Colegio de Trabajadores Sociales del Distrito Capital",
        GraduationYear = year,
        StatusId = StatusIds.Active
    };

    [Fact]
    public async Task Duplicated_identity_card_or_ants_number_is_a_conflict()
    {
        await using var host = await TestHost.CreateAsync();
        await host.RunAsync(sp => sp.GetRequiredService<ISocialWorkerService>().CreateAsync(Worker("V-1234567", "A-1")));

        var byCard = await Assert.ThrowsAsync<ConflictException>(() => host.RunAsync(sp =>
            sp.GetRequiredService<ISocialWorkerService>().CreateAsync(Worker("v1.234.567", "A-2"))));
        Assert.Equal("identityCard", byCard.Field);

        var byAnts = await Assert.ThrowsAsync<ConflictException>(() => host.RunAsync(sp =>
            sp.GetRequiredService<ISocialWorkerService>().CreateAsync(Worker("V-7654321", "A-1"))));
        Assert.Equal("antsRegistrationNumber", byAnts.Field);
    }

    [Fact]
    public async Task Future_graduation_year_is_rejected()
    {
        await using var host = await TestHost.CreateAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => host.RunAsync(sp =>
            sp.GetRequiredService<ISocialWorkerService>().CreateAsync(Worker("V-1234567", "A-1", year: 2030))));
        Assert.Contains("graduationYear", ex.Errors.Keys);
    }
}
