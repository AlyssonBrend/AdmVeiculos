using System.Security.Claims;
using ParkingControl.Domain;
using ParkingControl.Services;

namespace ParkingControl.Endpoints;

public static class ParkingEndpoints
{
    public static void MapParkingEndpoints(this WebApplication app)
    {
        // Registrar entrada
        app.MapPost("/api/parking/entry", async (EntryRequest req, ParkingService svc, ClaimsPrincipal user) =>
        {
            try
            {
                var opId = int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
                var record = await svc.RegisterEntryAsync(req.Plate, opId, req.Notes ?? "");
                return Results.Ok(record);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        }).RequireAuthorization();

        // Registrar saída
        app.MapPost("/api/parking/exit/{plate}", async (string plate, ParkingService svc) =>
        {
            try
            {
                var record = await svc.RegisterExitAsync(plate);
                return Results.Ok(new
                {
                    record.Id,
                    record.Plate,
                    record.EntryTime,
                    record.ExitTime,
                    record.TotalAmount,
                    Duration = (record.ExitTime!.Value - record.EntryTime).ToString(@"hh\:mm\:ss")
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        }).RequireAuthorization();

        // Consultar valor atual por placa (sem fechar)
        app.MapGet("/api/parking/status/{plate}", async (string plate, ParkingService svc) =>
        {
            try
            {
                var (record, amount) = await svc.GetCurrentValueAsync(plate);
                return Results.Ok(new
                {
                    record.Plate,
                    record.EntryTime,
                    ElapsedMinutes = (int)(DateTime.UtcNow - record.EntryTime).TotalMinutes,
                    CurrentAmount = amount,
                    Vehicle = record.Vehicle is null ? null : new
                    {
                        record.Vehicle.Model,
                        record.Vehicle.Color,
                        record.Vehicle.OwnerName
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        }).RequireAuthorization();

        // Veículos ativos no momento
        app.MapGet("/api/parking/active", async (ParkingService svc) =>
            Results.Ok(await svc.GetActiveRecordsAsync()))
            .RequireAuthorization();

        // Histórico paginado
        app.MapGet("/api/parking/history", async (ParkingService svc, int page = 1, int pageSize = 20) =>
            Results.Ok(await svc.GetHistoryAsync(page, pageSize)))
            .RequireAuthorization();

        // Configuração de preços
        app.MapGet("/api/parking/pricing", async (ParkingService svc) =>
            Results.Ok(await svc.GetPricingAsync()))
            .RequireAuthorization();

        app.MapPut("/api/parking/pricing", async (PricingConfig config, ParkingService svc, Data.AppDbContext db) =>
        {
            var existing = await svc.GetPricingAsync();
            existing.HourlyRate = config.HourlyRate;
            existing.ToleranceMinutes = config.ToleranceMinutes;
            existing.DailyMaxRate = config.DailyMaxRate;
            existing.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(existing);
        }).RequireAuthorization("AdminOnly");
    }
}

public record EntryRequest(string Plate, string? Notes);
