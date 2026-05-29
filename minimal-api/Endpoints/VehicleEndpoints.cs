using Microsoft.EntityFrameworkCore;
using ParkingControl.Data;
using ParkingControl.Domain;

namespace ParkingControl.Endpoints;

public static class VehicleEndpoints
{
    public static void MapVehicleEndpoints(this WebApplication app)
    {
        // Listar todos
        app.MapGet("/api/vehicles", async (AppDbContext db) =>
            await db.Vehicles.OrderBy(v => v.Plate).ToListAsync())
            .RequireAuthorization();

        // Buscar por placa
        app.MapGet("/api/vehicles/{plate}", async (string plate, AppDbContext db) =>
        {
            plate = plate.ToUpper().Replace("-", "");
            var v = await db.Vehicles.FindAsync(plate);
            return v is null ? Results.NotFound() : Results.Ok(v);
        }).RequireAuthorization();

        // Cadastrar
        app.MapPost("/api/vehicles", async (VehicleRequest req, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Plate))
                return Results.BadRequest(new { error = "Plate is required." });

            var plate = req.Plate.ToUpper().Replace("-", "").Replace(" ", "");

            if (await db.Vehicles.AnyAsync(v => v.Plate == plate))
                return Results.Conflict(new { error = "Vehicle already registered." });

            var vehicle = new Vehicle
            {
                Plate = plate,
                Model = req.Model ?? "",
                Color = req.Color ?? "",
                OwnerName = req.OwnerName ?? "",
                OwnerPhone = req.OwnerPhone ?? ""
            };

            db.Vehicles.Add(vehicle);
            await db.SaveChangesAsync();
            return Results.Created($"/api/vehicles/{plate}", vehicle);
        }).RequireAuthorization();

        // Atualizar
        app.MapPut("/api/vehicles/{plate}", async (string plate, VehicleRequest req, AppDbContext db) =>
        {
            plate = plate.ToUpper().Replace("-", "");
            var vehicle = await db.Vehicles.FindAsync(plate);
            if (vehicle is null) return Results.NotFound();

            vehicle.Model = req.Model ?? vehicle.Model;
            vehicle.Color = req.Color ?? vehicle.Color;
            vehicle.OwnerName = req.OwnerName ?? vehicle.OwnerName;
            vehicle.OwnerPhone = req.OwnerPhone ?? vehicle.OwnerPhone;

            await db.SaveChangesAsync();
            return Results.Ok(vehicle);
        }).RequireAuthorization();

        // Deletar
        app.MapDelete("/api/vehicles/{plate}", async (string plate, AppDbContext db) =>
        {
            plate = plate.ToUpper().Replace("-", "");
            var vehicle = await db.Vehicles.FindAsync(plate);
            if (vehicle is null) return Results.NotFound();
            db.Vehicles.Remove(vehicle);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");
    }
}

public record VehicleRequest(string Plate, string? Model, string? Color, string? OwnerName, string? OwnerPhone);
