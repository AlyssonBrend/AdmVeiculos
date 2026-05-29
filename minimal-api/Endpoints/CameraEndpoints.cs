using ParkingControl.Services;

namespace ParkingControl.Endpoints;

public static class CameraEndpoints
{
    public static void MapCameraEndpoints(this WebApplication app)
    {
        // Detectar placa via RTSP
        app.MapPost("/api/camera/rtsp", async (RtspRequest req, CameraService cam) =>
        {
            if (string.IsNullOrWhiteSpace(req.RtspUrl))
                return Results.BadRequest(new { error = "RTSP URL is required." });

            var result = await cam.DetectPlateAsync(req.RtspUrl);
            return result.Success
                ? Results.Ok(result)
                : Results.UnprocessableEntity(result);
        }).RequireAuthorization();

        // Detectar placa via upload de imagem
        app.MapPost("/api/camera/upload", async (HttpRequest req, CameraService cam) =>
        {
            if (!req.HasFormContentType || req.Form.Files.Count == 0)
                return Results.BadRequest(new { error = "No image file provided." });

            var file = req.Form.Files[0];
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var result = cam.DetectPlateFromImage(ms.ToArray());
            return result.Success
                ? Results.Ok(result)
                : Results.UnprocessableEntity(result);
        }).RequireAuthorization()
          .DisableAntiforgery();
    }
}

public record RtspRequest(string RtspUrl);
