using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using TrabajoSocial.Application.Reports;

namespace TrabajoSocial.Infrastructure.Reports;

/// <summary>
/// Dibuja la entrevista preliminar en 2 hojas Carta, imitando el formulario en papel:
/// etiquetas en negrita y el valor escrito sobre una línea. El diseño es de posición fija:
/// las filas reparten toda la hoja y los textos largos se achican hasta caber en su caja,
/// así que nunca se agrega una tercera hoja.
/// </summary>
internal sealed class SocialReportPdfRenderer : ISocialReportPdfRenderer
{
    public byte[] Render(SocialReportPrintModel model)
    {
        EmbeddedFontResolver.EnsureRegistered();

        using var document = new PdfDocument();
        document.Info.Title = $"Entrevista preliminar - HC {model.MedicalRecordNumber}";
        document.Info.Author = model.SocialWorkerName;
        document.Info.Creator = "TrabajoSocial";

        var first = document.AddPage();
        first.Size = PageSize.Letter;
        using (var gfx = XGraphics.FromPdfPage(first))
        {
            new FirstPage(new Canvas(gfx), model).Draw();
        }

        var second = document.AddPage();
        second.Size = PageSize.Letter;
        using (var gfx = XGraphics.FromPdfPage(second))
        {
            new SecondPage(new Canvas(gfx), model).Draw();
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    // =========================================================================================
    // Geometría y utilidades de dibujo
    // =========================================================================================

    private static class Layout
    {
        // Carta: 612 x 792 puntos.
        public const double PageWidth = 612;
        public const double PageHeight = 792;
        public const double MarginX = 42;
        public const double MarginTop = 30;
        public const double MarginBottom = 34;
        public const double ContentWidth = PageWidth - 2 * MarginX;
        public const double Right = MarginX + ContentWidth;
        public const double CellGap = 10;

        public const double LabelSize = 9.5;
        public const double ValueSize = 10;
        public const double MinValueSize = 6.5;
        public const double LastResortSize = 5.5;

        /// <summary>Distancia de la línea de escritura bajo la línea base del texto.</summary>
        public const double UnderlineOffset = 2.4;
    }

    /// <summary>XGraphics con fuentes en caché y las operaciones que usa el formulario.</summary>
    private sealed class Canvas(XGraphics gfx)
    {
        private static readonly XPen LinePen = new(XColor.FromArgb(140, 140, 140), 0.6);
        private static readonly XPen RulePen = new(XColor.FromArgb(60, 60, 60), 0.8);
        private readonly Dictionary<(double, bool), XFont> fonts = [];

        public XGraphics Gfx => gfx;

        public XFont Font(double size, bool bold = false)
        {
            if (!fonts.TryGetValue((size, bold), out var font))
            {
                font = new XFont(EmbeddedFontResolver.Family, size, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular);
                fonts[(size, bold)] = font;
            }

            return font;
        }

        public double Width(string text, XFont font) => text.Length == 0 ? 0 : gfx.MeasureString(text, font).Width;

        public void Text(string text, XFont font, double x, double baseline, XStringAlignment align = XStringAlignment.Near)
        {
            if (text.Length == 0)
            {
                return;
            }

            var format = align switch
            {
                XStringAlignment.Center => XStringFormats.BaseLineCenter,
                XStringAlignment.Far => XStringFormats.BaseLineRight,
                _ => XStringFormats.BaseLineLeft
            };
            gfx.DrawString(text, font, XBrushes.Black, x, baseline, format);
        }

        /// <summary>Línea de escritura (gris, como la del formulario).</summary>
        public void WritingLine(double x1, double x2, double baseline)
        {
            if (x2 - x1 > 1)
            {
                gfx.DrawLine(LinePen, x1, baseline + Layout.UnderlineOffset, x2, baseline + Layout.UnderlineOffset);
            }
        }

        public void Rule(double x1, double x2, double y) => gfx.DrawLine(RulePen, x1, y, x2, y);

        /// <summary>Mayor tamaño (≤ <paramref name="max"/>) con el que el texto cabe en una línea.</summary>
        public double FitSize(string text, double width, double max = Layout.ValueSize)
        {
            for (var size = max; size >= Layout.MinValueSize; size -= 0.5)
            {
                if (Width(text, Font(size)) <= width)
                {
                    return size;
                }
            }

            return Layout.MinValueSize;
        }

        /// <summary>Recorta el texto con "…" para que quepa en el ancho.</summary>
        public string Truncate(string text, XFont font, double width)
        {
            if (Width(text, font) <= width)
            {
                return text;
            }

            var length = text.Length;
            while (length > 0 && Width(text[..length].TrimEnd() + "…", font) > width)
            {
                length--;
            }

            return length == 0 ? string.Empty : text[..length].TrimEnd() + "…";
        }

        /// <summary>
        /// Reparte el texto en renglones de los anchos dados (el último ancho se repite).
        /// Respeta los saltos de línea del usuario; las palabras más largas que un renglón se cortan.
        /// </summary>
        public List<string> Wrap(string text, XFont font, IReadOnlyList<double> widths)
        {
            var lines = new List<string>();
            double WidthAt(int index) => widths[Math.Min(index, widths.Count - 1)];

            foreach (var paragraph in text.Split('\n'))
            {
                var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0)
                {
                    continue;
                }

                var current = string.Empty;
                foreach (var word in words)
                {
                    var candidate = current.Length == 0 ? word : $"{current} {word}";
                    if (Width(candidate, font) <= WidthAt(lines.Count))
                    {
                        current = candidate;
                        continue;
                    }

                    if (current.Length > 0)
                    {
                        lines.Add(current);
                    }

                    current = word;
                    while (Width(current, font) > WidthAt(lines.Count))
                    {
                        var cut = current.Length - 1;
                        while (cut > 1 && Width(current[..cut], font) > WidthAt(lines.Count))
                        {
                            cut--;
                        }

                        lines.Add(current[..cut]);
                        current = current[cut..];
                    }
                }

                lines.Add(current);
            }

            return lines;
        }

        public XImage? LoadImage(byte[]? bytes)
        {
            if (bytes is not { Length: > 0 })
            {
                return null;
            }

            try
            {
                return XImage.FromStream(new MemoryStream(bytes));
            }
            catch (Exception)
            {
                // Formato que PDFsharp no admite (p. ej. un logo WEBP): se imprime sin imagen.
                return null;
            }
        }
    }

    /// <summary>Texto repartido en renglones con un tamaño de fuente común.</summary>
    private sealed record WrappedText(IReadOnlyList<string> Lines, double Size);

    /// <summary>
    /// Busca el mayor tamaño con el que el texto cabe en los renglones disponibles. Si ni con el
    /// mínimo cabe (no debería ocurrir con los límites de caracteres), se recorta el último renglón.
    /// </summary>
    private static WrappedText FitWrapped(Canvas canvas, string? text, Func<double, IReadOnlyList<double>> widthsForSize)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new WrappedText([], Layout.ValueSize);
        }

        var sizes = Enumerable.Range(0, 8).Select(i => Layout.ValueSize - i * 0.5)
            .Append(6).Append(Layout.LastResortSize);
        foreach (var size in sizes)
        {
            var widths = widthsForSize(size);
            var lines = canvas.Wrap(text, canvas.Font(size), widths);
            if (lines.Count <= widths.Count)
            {
                return new WrappedText(lines, size);
            }
        }

        var lastWidths = widthsForSize(Layout.LastResortSize);
        var font = canvas.Font(Layout.LastResortSize);
        var all = canvas.Wrap(text, font, lastWidths);
        var kept = all.Take(lastWidths.Count).ToList();
        var last = kept.Count - 1;
        kept[last] = canvas.Truncate(kept[last] + " " + string.Join(' ', all.Skip(lastWidths.Count)), font, lastWidths[^1]);
        return new WrappedText(kept, Layout.LastResortSize);
    }

    // =========================================================================================
    // Hoja 1: identificación del paciente
    // =========================================================================================

    private abstract record Cell(double Weight);

    /// <summary>"Etiqueta: valor" sobre una línea. <paramref name="Size"/> fija el tamaño (texto repartido).</summary>
    private sealed record FieldCell(string Label, string? Value, double Weight, double? Size = null) : Cell(Weight);

    /// <summary>Renglón de continuación de un texto largo (sin etiqueta).</summary>
    private sealed record LineCell(string? Value, double Weight, double Size) : Cell(Weight);

    /// <summary>"Etiqueta: A ___ B _X_" con la opción elegida marcada.</summary>
    private sealed record OptionsCell(string Label, IReadOnlyList<PrintOption> Options, double Weight) : Cell(Weight);

    private sealed record HeadingCell(string Text, double Weight, bool Centered) : Cell(Weight);

    /// <summary>Una fila del formulario. <paramref name="Height"/> es relativa (1 = fila normal).</summary>
    private sealed record Row(IReadOnlyList<Cell> Cells, double Height = 1);

    private sealed class FirstPage(Canvas canvas, SocialReportPrintModel m)
    {
        private const double MaxHeaderImageHeight = 82;

        public void Draw()
        {
            var top = DrawHeader() + 6;
            var rows = BuildRows();

            var available = Layout.PageHeight - Layout.MarginBottom - top;
            var unit = available / rows.Sum(r => r.Height);
            foreach (var row in rows)
            {
                var height = unit * row.Height;
                DrawRow(row, top, height);
                top += height;
            }
        }

        private double DrawHeader()
        {
            var y = Layout.MarginTop;
            var titleFont = canvas.Font(11.5, bold: true);
            var company = m.CompanyName.Trim().ToUpperInvariant();

            using var header = canvas.LoadImage(m.HeaderImage);
            if (header is not null)
            {
                var scale = Math.Min(Layout.ContentWidth / header.PointWidth, MaxHeaderImageHeight / header.PointHeight);
                var (w, h) = (header.PointWidth * scale, header.PointHeight * scale);
                canvas.Gfx.DrawImage(header, Layout.MarginX + (Layout.ContentWidth - w) / 2, y, w, h);
                y += h + 15;
                if (company.Length > 0)
                {
                    canvas.Text(company, titleFont, Layout.PageWidth / 2, y, XStringAlignment.Center);
                    y += 15;
                }

                canvas.Text("ENTREVISTA PRELIMINAR", titleFont, Layout.PageWidth / 2, y, XStringAlignment.Center);
                return y + 6;
            }

            // Sin membrete: logo a la izquierda y el nombre de la empresa centrado.
            using var logo = canvas.LoadImage(m.Logo);
            var logoHeight = 0.0;
            if (logo is not null)
            {
                var scale = Math.Min(110 / logo.PointWidth, 56 / logo.PointHeight);
                logoHeight = logo.PointHeight * scale;
                canvas.Gfx.DrawImage(logo, Layout.MarginX, y, logo.PointWidth * scale, logoHeight);
            }

            var textTop = y + Math.Max(0, (logoHeight - 32) / 2);
            var nameFont = canvas.Font(13, bold: true);
            var reserved = logo is null ? 0 : 230; // el nombre no se monta sobre el logo
            var name = canvas.Truncate(company, nameFont, Layout.ContentWidth - reserved);
            canvas.Text(name, nameFont, Layout.PageWidth / 2, textTop + 14, XStringAlignment.Center);
            canvas.Text("ENTREVISTA PRELIMINAR", titleFont, Layout.PageWidth / 2, textTop + 31, XStringAlignment.Center);
            return Math.Max(y + logoHeight, textTop + 31) + 8;
        }

        private List<Row> BuildRows()
        {
            var rows = new List<Row>
            {
                new([
                    new FieldCell("Historia Clínica Nº:", m.MedicalRecordNumber, 0.34),
                    new FieldCell("Fecha de Ingreso:", m.AdmissionDate, 0.30),
                    new FieldCell("Religión:", m.Religion, 0.36)
                ]),
                new([new HeadingCell("IDENTIFICACIÓN DEL PACIENTE", 1, Centered: true)], 0.8),
                new([new FieldCell("Apellidos y Nombres:", m.PatientName, 1)]),
                new([
                    new FieldCell("Cédula de Identidad:", m.IdentityNumber, 0.42),
                    new OptionsCell(string.Empty, m.Nationality, 0.18),
                    new FieldCell("Nº de Pasaporte:", m.PassportNumber, 0.40)
                ]),
                new([
                    new FieldCell("Lugar de Nacimiento:", m.BirthPlace, 0.48),
                    new FieldCell("País:", m.BirthCountry, 0.24),
                    new FieldCell("Estado:", m.BirthState, 0.28)
                ]),
                new([
                    new FieldCell("Municipio:", m.BirthMunicipality, 0.36),
                    new FieldCell("Fecha de Nacimiento:", m.BirthDate, 0.38),
                    new FieldCell("Edad:", m.Age, 0.26)
                ]),
                new([
                    new OptionsCell("Sexo:", m.Gender, 0.30),
                    new FieldCell("Raza:", m.Race, 0.70)
                ]),
                new([new OptionsCell("Estado Civil:", m.MaritalStatus, 1)]),
                new([
                    new FieldCell("Grado de Instrucción:", m.EducationLevel, 0.50),
                    new FieldCell("Profesión u Oficio:", m.ProfessionOrOccupation, 0.50)
                ])
            };

            rows.AddRange(Wrapped("Dirección de Habitación:", m.HomeAddress, new FieldCell("Teléfonos:", m.Phones, 0.50)));

            rows.Add(new Row([new HeadingCell("Seguridad Social:", 1, Centered: false)], 0.8));
            rows.Add(new Row([
                new OptionsCell("IVSS:", m.Ivss, 0.25),
                new FieldCell("IPASME:", m.Ipasme, 0.13),
                new FieldCell("PÓLIZA:", m.InsurancePolicy, 0.31),
                new FieldCell("OTROS:", m.OtherSocialSecurity, 0.31)
            ]));
            rows.Add(new Row([new FieldCell("Referido Por:", m.ReferredBy, 1)]));
            rows.Add(new Row([new FieldCell("Lugar de Trabajo:", m.Workplace, 1)]));
            rows.AddRange(Wrapped("Dirección:", m.WorkAddress, trailing: null));
            rows.Add(new Row([
                new FieldCell("Teléfonos:", m.WorkPhones, 0.46),
                new FieldCell("Horario:", m.WorkSchedule, 0.54)
            ]));

            rows.Add(new Row([
                new OptionsCell("Paciente de Pediatría:", [new PrintOption("Sí", m.IsPediatric), new PrintOption("No", !m.IsPediatric)], 1)
            ]));
            rows.Add(new Row([
                new FieldCell("Representante:", m.GuardianFullName, 0.50),
                new FieldCell("C.I.:", m.GuardianIdentityCard, 0.22),
                new FieldCell("Parentesco:", m.GuardianKinship, 0.28)
            ]));

            rows.AddRange(Wrapped(
                "Dirección de Familiares y/o conocidos en el Distrito Capital o en su Localidad:", m.ContactAddress, trailing: null));
            rows.Add(new Row([new FieldCell("Apellidos y Nombres:", m.ContactFullName, 1)]));
            rows.AddRange(Wrapped("Teléfonos y Parentesco:", m.ContactPhoneAndKinship, trailing: null));
            return rows;
        }

        /// <summary>
        /// Campo de dos renglones: el primero sigue a la etiqueta y el segundo ocupa el ancho
        /// completo, o comparte la fila con <paramref name="trailing"/> (como "Teléfonos:" en el papel).
        /// </summary>
        private IEnumerable<Row> Wrapped(string label, string? value, FieldCell? trailing)
        {
            var labelWidth = canvas.Width(label, canvas.Font(Layout.LabelSize, bold: true));
            var firstWidth = Layout.ContentWidth - labelWidth - 8;
            var secondWeight = 1 - (trailing?.Weight ?? 0);
            var secondWidth = trailing is null
                ? Layout.ContentWidth - 2
                : (Layout.ContentWidth - Layout.CellGap) * secondWeight - 2;

            var text = FitWrapped(canvas, value, _ => [firstWidth, secondWidth]);
            yield return new Row([new FieldCell(label, text.Lines.ElementAtOrDefault(0), 1, text.Size)]);

            var second = new LineCell(text.Lines.ElementAtOrDefault(1), secondWeight, text.Size);
            yield return new Row(trailing is null ? [second] : [second, trailing]);
        }

        private void DrawRow(Row row, double top, double height)
        {
            var baseline = top + height - 7;
            var available = Layout.ContentWidth - Layout.CellGap * (row.Cells.Count - 1);
            var totalWeight = row.Cells.Sum(c => c.Weight);
            var x = Layout.MarginX;

            foreach (var cell in row.Cells)
            {
                var width = available * cell.Weight / totalWeight;
                DrawCell(cell, x, width, baseline);
                x += width + Layout.CellGap;
            }
        }

        private void DrawCell(Cell cell, double x, double width, double baseline)
        {
            var labelFont = canvas.Font(Layout.LabelSize, bold: true);
            switch (cell)
            {
                case FieldCell field:
                {
                    var labelWidth = canvas.Width(field.Label, labelFont);
                    canvas.Text(field.Label, labelFont, x, baseline);
                    var lineStart = x + labelWidth + (labelWidth > 0 ? 3 : 0);
                    canvas.WritingLine(lineStart, x + width, baseline);
                    DrawValue(field.Value, field.Size, lineStart + 3, x + width - lineStart - 4, baseline);
                    break;
                }
                case LineCell line:
                    canvas.WritingLine(x, x + width, baseline);
                    DrawValue(line.Value, line.Size, x + 1, width - 2, baseline);
                    break;
                case OptionsCell options:
                    DrawOptions(options, x, width, baseline);
                    break;
                case HeadingCell heading:
                {
                    var font = canvas.Font(heading.Centered ? 10.5 : Layout.LabelSize, bold: true);
                    if (heading.Centered)
                    {
                        canvas.Text(heading.Text, font, x + width / 2, baseline, XStringAlignment.Center);
                    }
                    else
                    {
                        canvas.Text(heading.Text, font, x, baseline);
                    }

                    break;
                }
            }
        }

        private void DrawValue(string? value, double? size, double x, double width, double baseline)
        {
            if (string.IsNullOrWhiteSpace(value) || width <= 0)
            {
                return;
            }

            var font = canvas.Font(size ?? canvas.FitSize(value, width));
            canvas.Text(canvas.Truncate(value, font, width), font, x, baseline);
        }

        /// <summary>Opciones con su espacio para marcar: "Sexo: F _X_  M ___".</summary>
        private void DrawOptions(OptionsCell cell, double x, double width, double baseline)
        {
            const double blank = 20;
            const double gap = 9;

            // Si las opciones no caben (catálogo con muchas), se achica la letra.
            var size = Layout.LabelSize;
            double NaturalWidth(double s)
            {
                var font = canvas.Font(s, bold: true);
                var labelWidth = cell.Label.Length > 0 ? canvas.Width(cell.Label, font) + 6 : 0;
                return labelWidth + cell.Options.Sum(o => canvas.Width(o.Label, font) + 3 + blank + gap);
            }

            while (size > 6 && NaturalWidth(size) > width)
            {
                size -= 0.5;
            }

            // Con demasiadas opciones (valores agregados con "Otro"), se escribe solo la elegida.
            if (NaturalWidth(size) > width)
            {
                var selected = cell.Options.FirstOrDefault(o => o.Selected)?.Label;
                DrawCell(new FieldCell(cell.Label, selected, 1), x, width, baseline);
                return;
            }

            var labelFont = canvas.Font(size, bold: true);
            var markFont = canvas.Font(Layout.ValueSize, bold: true);
            canvas.Text(cell.Label, labelFont, x, baseline);
            var cursor = x + (cell.Label.Length > 0 ? canvas.Width(cell.Label, labelFont) + 6 : 0);

            foreach (var option in cell.Options)
            {
                if (cursor + blank > x + width)
                {
                    break;
                }

                canvas.Text(option.Label, labelFont, cursor, baseline);
                cursor += canvas.Width(option.Label, labelFont) + 3;
                canvas.WritingLine(cursor, cursor + blank, baseline);
                if (option.Selected)
                {
                    canvas.Text("X", markFont, cursor + blank / 2, baseline, XStringAlignment.Center);
                }

                cursor += blank + gap;
            }
        }
    }

    // =========================================================================================
    // Hoja 2: evaluación social y firma
    // =========================================================================================

    private sealed class SecondPage(Canvas canvas, SocialReportPrintModel m)
    {
        private const double SignatureHeight = 112;

        /// <summary>Altura relativa de cada área, proporcional a su límite de caracteres.</summary>
        private (string Label, string? Text, double Weight)[] Sections =>
        [
            ("Área Médico Social:", m.MedicalSocialArea, 0.14),
            ("Área Psico-Social:", m.PsychosocialArea, 0.18),
            ("Área Social-Económica:", m.SocioeconomicArea, 0.25),
            ("Área Físico Ambiental:", m.PhysicalEnvironmentArea, 0.18),
            ("Diagnóstico Clínico:", m.ClinicalDiagnosis, 0.08),
            ("Servicios a que Refieren:", m.ReferralServices, 0.07),
            ("Observaciones:", m.Observations, 0.10)
        ];

        public void Draw()
        {
            var top = DrawStrip() + 8;
            var bottom = Layout.PageHeight - Layout.MarginBottom - SignatureHeight;
            var sections = Sections;
            var unit = (bottom - top) / sections.Sum(s => s.Weight);

            foreach (var (label, text, weight) in sections)
            {
                DrawSection(label, text, top, unit * weight);
                top += unit * weight;
            }

            DrawSignature(bottom);
        }

        /// <summary>Franja de identificación, por si las hojas se separan.</summary>
        private double DrawStrip()
        {
            var baseline = Layout.MarginTop + 8;
            var bold = canvas.Font(8, bold: true);
            var regular = canvas.Font(8);

            var left = "ENTREVISTA PRELIMINAR";
            canvas.Text(left, bold, Layout.MarginX, baseline);
            var record = $"  ·  Historia Clínica Nº {m.MedicalRecordNumber}";
            var leftWidth = canvas.Width(left, bold);
            canvas.Text(record, regular, Layout.MarginX + leftWidth, baseline);

            var used = leftWidth + canvas.Width(record, regular) + 20;
            var patient = canvas.Truncate($"Paciente: {m.PatientName}", regular, Layout.ContentWidth - used);
            canvas.Text(patient, regular, Layout.Right, baseline, XStringAlignment.Far);

            canvas.Rule(Layout.MarginX, Layout.Right, baseline + 5);
            return baseline + 5;
        }

        /// <summary>
        /// Área con renglones: el texto empieza junto a la etiqueta y sigue en los renglones de
        /// abajo. Si no cabe, se achica la letra (y con ella el interlineado, que suma renglones).
        /// </summary>
        private void DrawSection(string label, string? text, double top, double height)
        {
            var labelFont = canvas.Font(Layout.LabelSize, bold: true);
            var labelWidth = canvas.Width(label, labelFont);
            static double Spacing(double size) => size * 1.8;
            int LineCount(double size) => Math.Max(1, (int)Math.Floor(height / Spacing(size)));

            var firstWidth = Layout.ContentWidth - labelWidth - 7;
            var wrapped = FitWrapped(canvas, text, size =>
                [.. Enumerable.Repeat(Layout.ContentWidth - 2, LineCount(size)).Prepend(firstWidth).Take(LineCount(size))]);

            var spacing = Spacing(wrapped.Size);
            var font = canvas.Font(wrapped.Size);
            var lines = LineCount(wrapped.Size);
            for (var i = 0; i < lines; i++)
            {
                var baseline = top + spacing * (i + 1) - spacing * 0.3;
                var x = i == 0 ? Layout.MarginX + labelWidth + 4 : Layout.MarginX;
                if (i == 0)
                {
                    canvas.Text(label, labelFont, Layout.MarginX, baseline);
                }

                canvas.WritingLine(x, Layout.Right, baseline);
                if (i < wrapped.Lines.Count)
                {
                    canvas.Text(wrapped.Lines[i], font, x + (i == 0 ? 3 : 1), baseline);
                }
            }
        }

        private void DrawSignature(double top)
        {
            var center = Layout.PageWidth / 2;
            var title = canvas.Font(10, bold: true);
            var label = canvas.Font(9, bold: true);
            var value = canvas.Font(9.5);

            var baseline = top + 18;
            canvas.Text("TRABAJADOR(A) SOCIAL TRATANTE", title, center, baseline, XStringAlignment.Center);

            // Espacio para la firma y, bajo la línea, el nombre.
            baseline += 40;
            canvas.Rule(center - 120, center + 120, baseline);
            baseline += 13;
            if (!string.IsNullOrWhiteSpace(m.SocialWorkerName))
            {
                canvas.Text(canvas.Truncate(m.SocialWorkerName, value, 300), value, center, baseline, XStringAlignment.Center);
            }

            // Sin trabajadora asignada, los datos quedan en blanco para llenarlos a mano.
            const double blankWidth = 170;
            foreach (var (caption, text) in new[]
                     {
                         ("C.I.:", m.SocialWorkerIdentityCard),
                         ("Nº DE REGISTRO ANTS:", m.SocialWorkerAntsNumber),
                         ("COLEGIO DE TS:", m.SocialWorkerAssociation)
                     })
            {
                baseline += 13;
                var captionWidth = canvas.Width(caption, label) + 4;
                if (string.IsNullOrWhiteSpace(text))
                {
                    var blankStart = center - (captionWidth + blankWidth) / 2;
                    canvas.Text(caption, label, blankStart, baseline);
                    canvas.WritingLine(blankStart + captionWidth, blankStart + captionWidth + blankWidth, baseline);
                    continue;
                }

                var shown = canvas.Truncate(text, value, Layout.ContentWidth - captionWidth);
                var start = center - (captionWidth + canvas.Width(shown, value)) / 2;
                canvas.Text(caption, label, start, baseline);
                canvas.Text(shown, value, start + captionWidth, baseline);
            }
        }
    }
}
