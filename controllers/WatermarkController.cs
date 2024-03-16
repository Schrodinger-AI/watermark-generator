using Microsoft.AspNetCore.Mvc;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using WebApiClient.interfaces;

namespace WebApiClient.controllers;

[Route("[controller]")]
[ApiController]
public class ImageController : ControllerBase
{
    private WatermarkSettings _watermarkSettings;
    private ResizeSettings _resizeSettings;
    private readonly ILogger<ImageController> _logger;

    public ImageController(ILogger<ImageController> logger, WatermarkSettings watermarkSettings,
        ResizeSettings resizeSettings)
    {
        _logger = logger;
        _watermarkSettings = watermarkSettings;
        _resizeSettings = resizeSettings;
    }

    [HttpPost("process")]
    public IActionResult AddWatermark([FromBody] WatermarkApiSchema.WatermarkRequest request)
    {
        try
        {
            // Convert input Base64 string to byte array
            var inputBytes = Convert.FromBase64String(request.SourceImage.Split(",")[1]);

            // Load the input image from byte array
            using var imageBig = Image.Load(inputBytes);

            // Convert the watermarked image to Base64 string
            var outputBase64 = AddWatermark(imageBig, request.Watermark.Text);

            using var imageSmall = Image.Load(inputBytes);
            imageSmall.Mutate(x => x.Resize(_resizeSettings.Width, _resizeSettings.Height));
            var resizedBase64 = AddWatermark(imageSmall, request.Watermark.Text);

            var logDetails = new WatermarkApiSchema.LogDetails
            {
                Timestamp = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}",
                RequestMethod = "POST",
                RequestBody = request,
                RequestUrl = "/image/process",
                StatusCode = 200,
                Response = outputBase64
            };

            _logger.LogInformation("Image processed successfully. {@LogDetails}", logDetails);

            return Ok(new
            {
                ProcessedImage = $"data:image/webp;base64,{outputBase64}",
                Resized = $"data:image/webp;base64,{resizedBase64}",
            });
        }
        catch (Exception ex)
        {
            var logDetails = new WatermarkApiSchema.LogDetails
            {
                Timestamp = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}",
                RequestMethod = "POST",
                RequestBody = request,
                RequestUrl = "/image/process",
                StatusCode = 500,
                Response = ex.Message
            };
            _logger.LogError("An error occurred while processing the image. {@LogDetails}", logDetails);
            var errorResponse = new
            {
                error = ex.Message
            };
            return StatusCode(500, errorResponse);
        }
    }

    private string AddWatermark(Image image, string text)
    {
        var config = _watermarkSettings.Big;
        if (image.Size.Width < _watermarkSettings.Cutoff)
        {
            config = _watermarkSettings.Small;
        }

        var fonts = new FontCollection();
        var fontFamily = fonts.Add(config.FilePath);
        var font = fontFamily.CreateFont(config.FontSize, FontStyle.Regular);

        var textSize = TextMeasurer.MeasureAdvance(text, new TextOptions(font));
        var textLocation = new PointF(image.Width - textSize.Width - config.PaddingX,
            image.Height - textSize.Height - config.PaddingY);

        var backgroundRectangle = new RectangularPolygon(image.Width - textSize.Width - 2 * config.PaddingX,
            image.Height - textSize.Height - 2 * config.PaddingY, textSize.Width + 2 * config.PaddingX,
            textSize.Height + 2 * config.PaddingY);

        image.Mutate(x => x.Fill(Color.White.WithAlpha(0.32f), backgroundRectangle));
        // Apply the watermark
        image.Mutate(x =>
            x.DrawText(
                text,
                font,
                Color.Black.WithAlpha(0.6f),
                textLocation
            )
        );
        return ConvertToBase64(image);
    }

    // Convert the image to Base64 string
    private static string ConvertToBase64(Image image)
    {
        using var memoryStream = new MemoryStream();
        image.Save(memoryStream, SixLabors.ImageSharp.Formats.Webp.WebpFormat.Instance);
        return Convert.ToBase64String(memoryStream.ToArray());
    }
}