using OpenCvSharp;
using Tesseract;

namespace ParkingControl.Services;

public class CameraService
{
    private readonly ILogger<CameraService> _logger;

    public CameraService(ILogger<CameraService> logger) => _logger = logger;

    /// <summary>
    /// Captura um frame do stream RTSP e tenta reconhecer a placa via OCR.
    /// </summary>
    public async Task<PlateDetectionResult> DetectPlateAsync(string rtspUrl)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var capture = new VideoCapture(rtspUrl);
                if (!capture.IsOpened())
                    return new PlateDetectionResult { Success = false, Error = "Cannot open RTSP stream." };

                using var frame = new Mat();
                capture.Read(frame);

                if (frame.Empty())
                    return new PlateDetectionResult { Success = false, Error = "Empty frame captured." };

                // Pré-processamento para OCR
                using var gray    = new Mat();
                using var blurred = new Mat();
                using var thresh  = new Mat();

                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);
                Cv2.Threshold(blurred, thresh, 0, 255,
                    ThresholdTypes.Binary | ThresholdTypes.Otsu);

                // Salva frame temporário para Tesseract
                var tempPath = Path.Combine(Path.GetTempPath(), $"plate_{Guid.NewGuid()}.png");
                thresh.SaveImage(tempPath);

                var plate = RunOcr(tempPath);
                File.Delete(tempPath);

                return new PlateDetectionResult
                {
                    Success = !string.IsNullOrWhiteSpace(plate),
                    Plate = plate,
                    CapturedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting plate from RTSP stream");
                return new PlateDetectionResult { Success = false, Error = "Failed to process RTSP stream." };
            }
        });
    }

    /// <summary>
    /// Recebe uma imagem enviada por upload e tenta reconhecer a placa.
    /// </summary>
    public PlateDetectionResult DetectPlateFromImage(byte[] imageBytes)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"plate_{Guid.NewGuid()}.png");
            File.WriteAllBytes(tempPath, imageBytes);

            using var src     = Cv2.ImDecode(imageBytes, ImreadModes.Color);
            using var gray    = new Mat();
            using var blurred = new Mat();
            using var thresh  = new Mat();

            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);
            Cv2.Threshold(blurred, thresh, 0, 255,
                ThresholdTypes.Binary | ThresholdTypes.Otsu);

            thresh.SaveImage(tempPath);
            var plate = RunOcr(tempPath);
            File.Delete(tempPath);

            return new PlateDetectionResult
            {
                Success = !string.IsNullOrWhiteSpace(plate),
                Plate = plate,
                CapturedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting plate from uploaded image");
            return new PlateDetectionResult { Success = false, Error = "Failed to process image." };
        }
    }

    private static string RunOcr(string imagePath)
    {
        var tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

        using var engine = new TesseractEngine(tessDataPath, "por+eng", EngineMode.Default);
        engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");

        using var img  = Pix.LoadFromFile(imagePath);
        using var page = engine.Process(img);

        var text = page.GetText()
            .ToUpper()
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "");

        // Tenta extrair padrão de placa brasileiro (ABC1234 ou ABC1D23)
        var match = System.Text.RegularExpressions.Regex.Match(
            text, @"[A-Z]{3}[0-9][A-Z0-9][0-9]{2}");

        return match.Success ? match.Value : text.Trim();
    }
}

public class PlateDetectionResult
{
    public bool Success { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
}
