using Docnet.Core;
using Docnet.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SmartResume.Services.Interfaces;
using System.Text;
using System.Text.RegularExpressions;
using Tesseract;

public class OcrService : IOcrService
{
    private readonly string _tessDataPath;

    public OcrService()
    {
        _tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
    }

    public string ExtractTextFromPdf(byte[] pdfBytes)
    {
        using var engine = new TesseractEngine(_tessDataPath, "eng+tur", EngineMode.LstmOnly);
        engine.SetVariable("preserve_interword_spaces", "1");
        engine.DefaultPageSegMode = PageSegMode.Auto;

        var result = new StringBuilder();

        // Convert PDF pages to images and perform OCR
        foreach (var imageBytes in ConvertPdfToImages(pdfBytes))
        {
            using var pix = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(pix);
            result.AppendLine(page.GetText());
        }

        return CleanText(result.ToString());
    }

    // Convert PDF pages to preprocessed images
    private List<byte[]> ConvertPdfToImages(byte[] pdfBytes)
    {
        var images = new List<byte[]>();

        using var docReader = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions(2.0));

        for (int i = 0; i < docReader.GetPageCount(); i++)
        {
            using var pageReader = docReader.GetPageReader(i);

            var rawBytes = pageReader.GetImage();
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();

            using var image = Image.LoadPixelData<Bgra32>(rawBytes, width, height);

            // Preprocess for better OCR accuracy
            image.Mutate(x =>
            {
                x.BackgroundColor(Color.White);
                x.Grayscale();
                x.Contrast(1.4f);
                x.GaussianSharpen(1.2f);
            });

            using var outputStream = new MemoryStream();
            image.SaveAsPng(outputStream);

            images.Add(outputStream.ToArray());
        }

        return images;
    }

    // Clean OCR text output
    private string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        text = Regex.Replace(text, @"(?<=^|\s)\|(?=\s)", "I");
        text = text.Replace("\r", " ").Replace("\n", " ");
        text = Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }
}
