using System.Buffers.Binary;
using SkiaSharp;
using Whatsinthebox.UI;

namespace Whatsinthebox;

static class FontPreview
{
    public static RenderInfo Render(string input, string output)
    {
        var length = new FileInfo(input).Length;
        if (length > 32L * 1024 * 1024) throw new InvalidDataException(L.T("font.limit"));
        using (var stream = File.OpenRead(input))
        {
            Span<byte> header = stackalloc byte[12];
            if (stream.Read(header) != 12) throw new InvalidDataException(L.T("font.invalid"));
            uint magic = BinaryPrimitives.ReadUInt32BigEndian(header);
            int tables = BinaryPrimitives.ReadUInt16BigEndian(header[4..]);
            if ((magic != 0x00010000 && magic != 0x4F54544F) || tables < 1 || tables > 256 || 12L + tables * 16 > length)
                throw new InvalidDataException(L.T("font.invalid"));
            Span<byte> table = stackalloc byte[16];
            for (int i = 0; i < tables; i++)
            {
                stream.ReadExactly(table);
                long offset = BinaryPrimitives.ReadUInt32BigEndian(table[8..]);
                long size = BinaryPrimitives.ReadUInt32BigEndian(table[12..]);
                if (offset < 12L + tables * 16 || offset + size > length)
                    throw new InvalidDataException(L.T("font.invalid"));
            }
        }
        using var face = SKTypeface.FromFile(input) ?? throw new InvalidDataException(L.T("font.invalid"));
        if (face.GlyphCount < 1) throw new InvalidDataException(L.T("font.invalid"));
        using var surface = SKSurface.Create(new SKImageInfo(1200, 1500));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        using var paint = new SKPaint { Color = new SKColor(24, 35, 54), IsAntialias = true };
        using var captionFace = SKTypeface.FromFamilyName(L.Language == "zh" ? "Microsoft YaHei UI" : "Segoe UI");
        void Draw(string text, float size, float y, bool sample = false)
        {
            using var font = new SKFont(sample ? face : captionFace, size);
            float width = font.MeasureText(text, paint);
            if (width > 1080) font.Size *= 1080 / width;
            canvas.DrawText(text, 60, y, SKTextAlign.Left, font, paint);
        }
        Draw(L.T("font.mode"), 24, 65);
        Draw(face.FamilyName, 48, 135);
        Draw(Path.GetFileName(input), 22, 178);
        Draw(L.T("font.details", face.FontWeight, face.GlyphCount), 22, 217);
        using (var line = new SKPaint { Color = new SKColor(218, 226, 235), StrokeWidth = 2 })
            canvas.DrawLine(60, 245, 1140, 245, line);
        Draw("Aa Bb Cc 123", 110, 385, true);
        Draw(L.T("font.sample"), 24, 470);
        Draw(L.T("font.sentence"), 44, 535, true);
        Draw(L.T("font.sentence"), 32, 595, true);
        Draw(L.T("font.sentence"), 22, 642, true);
        Draw(L.T("font.characters"), 24, 755);
        string[] rows = { "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "abcdefghijklmnopqrstuvwxyz", "0123456789 !? @#%& () [] {}", "ÇĞİÖŞÜ çğıöşü ÄÖÜäöüß", "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЫЭЮЯ" };
        using var glyphFont = new SKFont(face, 36);
        for (int i = 0; i < rows.Length; i++)
        {
            string supported = string.Concat(rows[i].Where(c => c == ' ' || glyphFont.GetGlyphs(c.ToString())[0] != 0));
            Draw(supported, 36, 830 + i * 70, true);
        }
        bool missing = glyphFont.GetGlyphs(L.T("font.sentence")).Any(g => g == 0);
        if (missing) Draw(L.T("font.missing"), 22, 1270);
        Draw(L.T("font.note"), 22, 1400);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(output);
        data.SaveTo(file);
        return new(true, L.T("font.note"), 1200, 1500, L.T("font.mode"));
    }
}
