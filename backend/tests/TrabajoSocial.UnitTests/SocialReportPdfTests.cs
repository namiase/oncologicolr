using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PdfSharp.Pdf.IO;
using TrabajoSocial.Infrastructure;
using TrabajoSocial.Application.Reports;
using TrabajoSocial.Domain.Entities.Reports;
using TrabajoSocial.Infrastructure.Persistence;
using TrabajoSocial.UnitTests.Support;
using static TrabajoSocial.UnitTests.SocialReportServiceTests;

namespace TrabajoSocial.UnitTests;

public sealed class SocialReportPdfTests
{
    /// <summary>PNG de 1×1 píxel.</summary>
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    private const string Sample =
        "La paciente refiere que vive con su madre y dos hijos menores en una vivienda alquilada de dos habitaciones; " +
        "el ingreso familiar proviene de trabajos ocasionales y de la ayuda de familiares que residen en Caracas. ";

    /// <summary>Texto realista de exactamente <paramref name="length"/> caracteres.</summary>
    private static string Text(int length)
    {
        var builder = new StringBuilder();
        while (builder.Length < length)
        {
            builder.Append(Sample);
        }

        return builder.ToString(0, length);
    }

    private static int PageCount(byte[] pdf)
    {
        using var stream = new MemoryStream(pdf);
        return PdfReader.Open(stream, PdfDocumentOpenMode.Import).PageCount;
    }

    /// <summary>El renderer tal como lo registra la infraestructura (sin base de datos).</summary>
    private static ISocialReportPdfRenderer CreateRenderer()
        => new ServiceCollection()
            .AddInfrastructureServices(new ConfigurationBuilder().Build())
            .BuildServiceProvider()
            .GetRequiredService<ISocialReportPdfRenderer>();

    private static SocialReportPrintModel Minimal => new()
    {
        CompanyName = "Instituto de Oncología Dr. Luis Razetti",
        MedicalRecordNumber = "123456",
        AdmissionDate = "16/11/2026",
        PatientName = "Pérez González, Ana Isabel",
        SocialWorkerName = "María López",
        SocialWorkerIdentityCard = "V-11111111",
        SocialWorkerAntsNumber = "12345",
        SocialWorkerAssociation = "Colegio de Trabajadores Sociales del Distrito Capital"
    };

    /// <summary>Todos los campos al máximo permitido.</summary>
    internal static SocialReportPrintModel Full => Minimal with
    {
        Religion = "Católica",
        PatientName = $"{Text(SocialReportLimits.Names)}, {Text(SocialReportLimits.Names)}",
        IdentityNumber = "12345678",
        Nationality = [new PrintOption("V", true), new PrintOption("E", false)],
        PassportNumber = Text(SocialReportLimits.PassportNumber),
        BirthPlace = Text(SocialReportLimits.BirthPlace),
        BirthCountry = "Venezuela",
        BirthState = "Distrito Capital",
        BirthMunicipality = "Libertador",
        BirthDate = "20/05/1980",
        Age = "46 años",
        Gender = [new PrintOption("F", true), new PrintOption("M", false)],
        Race = "Morena",
        MaritalStatus =
        [
            new PrintOption("Soltero(a)", false), new PrintOption("Casado(a)", true), new PrintOption("Divorciado(a)", false),
            new PrintOption("Viudo(a)", false), new PrintOption("Unión estable de hecho", false)
        ],
        EducationLevel = "Técnico superior universitario (TSU)",
        ProfessionOrOccupation = "Licenciado(a) en Educación",
        HomeAddress = Text(SocialReportLimits.Address),
        Phones = "0414-1234567 / 0212-7654321 / 0416-1112233 / 0426-4445566",
        Ivss = [new PrintOption("Sí", true), new PrintOption("No", false)],
        Ipasme = "No",
        InsurancePolicy = Text(SocialReportLimits.SocialSecurity),
        OtherSocialSecurity = Text(SocialReportLimits.SocialSecurity),
        ReferredBy = Text(SocialReportLimits.FullLine),
        Workplace = Text(SocialReportLimits.FullLine),
        WorkAddress = Text(SocialReportLimits.Address),
        WorkPhones = "0212-5551234 / 0212-5559876",
        WorkSchedule = Text(SocialReportLimits.WorkSchedule),
        IsPediatric = true,
        GuardianFullName = Text(SocialReportLimits.GuardianName),
        GuardianIdentityCard = "V-12345678",
        GuardianKinship = "Madre",
        ContactAddress = Text(SocialReportLimits.Address),
        ContactFullName = Text(SocialReportLimits.FullLine),
        // 6 teléfonos de familiares, cada uno con un parentesco escrito en "Otro" al máximo.
        ContactPhoneAndKinship = string.Join(" / ", Enumerable.Range(1, SocialReportLimits.ContactPhones)
            .Select(i => $"0414-123456{i} ({Text(SocialReportLimits.OtherKinship)})")),
        MedicalSocialArea = Text(SocialReportLimits.MedicalSocialArea),
        PsychosocialArea = Text(SocialReportLimits.PsychosocialArea),
        SocioeconomicArea = Text(SocialReportLimits.SocioeconomicArea),
        PhysicalEnvironmentArea = Text(SocialReportLimits.PhysicalEnvironmentArea),
        ClinicalDiagnosis = Text(SocialReportLimits.ClinicalDiagnosis),
        ReferralServices = Text(SocialReportLimits.ReferralServices),
        Observations = Text(SocialReportLimits.Observations)
    };

    [Fact]
    public void Empty_and_full_reports_are_always_two_letter_pages()
    {
        var renderer = CreateRenderer();

        var withoutSigner = Full with
        {
            SocialWorkerName = null, SocialWorkerIdentityCard = null, SocialWorkerAntsNumber = null, SocialWorkerAssociation = null
        };
        foreach (var model in new[] { Minimal, Full, Full with { HeaderImage = Png }, Full with { Logo = Png }, withoutSigner })
        {
            var pdf = renderer.Render(model);
            Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
            Assert.Equal(2, PageCount(pdf));
        }
    }

    [Fact]
    public void Unsupported_images_are_skipped()
    {
        var notAnImage = "RIFF....WEBPVP8 "u8.ToArray();
        var pdf = CreateRenderer().Render(Minimal with { HeaderImage = notAnImage, Logo = notAnImage });
        Assert.Equal(2, PageCount(pdf));
    }

    [Fact]
    public async Task Pdf_download_uses_the_saved_report_and_the_report_header()
    {
        await using var host = await TestHost.CreateAsync();
        var (_, mlopez) = await CreateWorker(host, "V-1111111", "mlopez");
        ActAs(host, mlopez!.Value, "mlopez");

        var header = await host.Files.SaveAsync(new MemoryStream(Png), "branding", ".png");
        await host.RunAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.CompanySettings.Single().ReportHeaderPath = header;
            await db.SaveChangesAsync();
        });

        var report = await host.RunAsync(sp => sp.GetRequiredService<ISocialReportService>().CreateAsync(Report()));
        var file = await host.RunAsync(sp => sp.GetRequiredService<ISocialReportService>().GetPdfAsync(report.Id));

        Assert.Equal("Entrevista-HC123456-Perez-Gonzalez.pdf", file.FileName);
        Assert.Equal(2, PageCount(file.Content));
    }

    /// <summary>
    /// Genera PDFs de muestra para revisarlos a ojo. Solo corre si se define la variable de
    /// entorno REPORT_PDF_SAMPLES con la carpeta de salida.
    /// </summary>
    [Fact]
    public void Write_samples_when_requested()
    {
        var folder = Environment.GetEnvironmentVariable("REPORT_PDF_SAMPLES");
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        Directory.CreateDirectory(folder);
        var renderer = CreateRenderer();
        File.WriteAllBytes(Path.Combine(folder, "minimo.pdf"), renderer.Render(Minimal));
        File.WriteAllBytes(Path.Combine(folder, "completo.pdf"), renderer.Render(Full));
        File.WriteAllBytes(Path.Combine(folder, "sin-firma.pdf"), renderer.Render(Minimal with
        {
            SocialWorkerName = null, SocialWorkerIdentityCard = null, SocialWorkerAntsNumber = null, SocialWorkerAssociation = null
        }));
    }
}
