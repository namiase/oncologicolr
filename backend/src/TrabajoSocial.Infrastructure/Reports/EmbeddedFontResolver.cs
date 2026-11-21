using System.Collections.Concurrent;
using System.Reflection;
using PdfSharp.Fonts;

namespace TrabajoSocial.Infrastructure.Reports;

/// <summary>
/// Entrega a PDFsharp la fuente incrustada en el ensamblado (Liberation Sans, SIL OFL 1.1),
/// así el PDF se ve igual en Windows y en Linux sin depender de las fuentes del servidor.
/// </summary>
internal sealed class EmbeddedFontResolver : IFontResolver
{
    public const string Family = "Liberation Sans";

    private const string Regular = "LiberationSans-Regular";
    private const string Bold = "LiberationSans-Bold";

    private static readonly EmbeddedFontResolver Instance = new();
    private static readonly Lock Gate = new();
    private static readonly ConcurrentDictionary<string, byte[]> Cache = new();

    /// <summary>
    /// PDFsharp exige registrar el resolver una sola vez, antes de usar cualquier fuente,
    /// y luego siempre la misma instancia.
    /// </summary>
    public static void EnsureRegistered()
    {
        lock (Gate)
        {
            if (GlobalFontSettings.FontResolver is not EmbeddedFontResolver)
            {
                GlobalFontSettings.FontResolver = Instance;
            }
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        => new(isBold ? Bold : Regular);

    public byte[]? GetFont(string faceName) => Cache.GetOrAdd(faceName, static name =>
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"fonts.{name}.ttf")
            ?? throw new InvalidOperationException($"No se encontró la fuente incrustada '{name}'.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    });
}
