using Microsoft.EntityFrameworkCore;
using ParkingControl.Data;
using ParkingControl.Domain;
using static ParkingControl.Domain.PlateHelper;

namespace ParkingControl.Services;

public class ParkingService
{
    private readonly AppDbContext _db;

    public ParkingService(AppDbContext db) => _db = db;

    // ── Entrada ───────────────────────────────────────────────────────────────
    public async Task<ParkingRecord> RegisterEntryAsync(string plate, int? operatorId = null, string notes = "")
    {
        plate = Normalize(plate);

        // Verifica se já está dentro
        var active = await _db.ParkingRecords
            .FirstOrDefaultAsync(r => r.Plate == plate && r.ExitTime == null);
        if (active is not null)
            throw new InvalidOperationException("Vehicle is already in the parking lot.");

        var record = new ParkingRecord
        {
            Plate = plate,
            EntryTime = DateTime.UtcNow,
            OperatorId = operatorId,
            Notes = notes
        };

        _db.ParkingRecords.Add(record);
        await _db.SaveChangesAsync();
        return record;
    }

    // ── Saída + cálculo de valor ───────────────────────────────────────────────
    public async Task<ParkingRecord> RegisterExitAsync(string plate)
    {
        plate = Normalize(plate);

        var record = await _db.ParkingRecords
            .Include(r => r.Vehicle)
            .FirstOrDefaultAsync(r => r.Plate == plate && r.ExitTime == null)
            ?? throw new KeyNotFoundException($"No active record found for plate {plate}.");

        var config = await GetPricingAsync();
        record.ExitTime = DateTime.UtcNow;
        record.TotalAmount = CalculateAmount(record.EntryTime, record.ExitTime.Value, config);

        await _db.SaveChangesAsync();
        return record;
    }

    // ── Consulta valor atual (sem fechar) ─────────────────────────────────────
    public async Task<(ParkingRecord Record, decimal CurrentAmount)> GetCurrentValueAsync(string plate)
    {
        plate = Normalize(plate);

        var record = await _db.ParkingRecords
            .Include(r => r.Vehicle)
            .FirstOrDefaultAsync(r => r.Plate == plate && r.ExitTime == null)
            ?? throw new KeyNotFoundException($"No active record found for plate {plate}.");

        var config = await GetPricingAsync();
        var amount = CalculateAmount(record.EntryTime, DateTime.UtcNow, config);
        return (record, amount);
    }

    // ── Cálculo ───────────────────────────────────────────────────────────────
    public static decimal CalculateAmount(DateTime entry, DateTime exit, PricingConfig config)
    {
        var duration = exit - entry;
        if (duration.TotalMinutes <= config.ToleranceMinutes) return 0m;

        var hours = Math.Ceiling(duration.TotalHours);
        var amount = (decimal)hours * config.HourlyRate;
        return Math.Min(amount, config.DailyMaxRate);
    }

    public async Task<PricingConfig> GetPricingAsync()
    {
        var config = await _db.PricingConfigs.FirstOrDefaultAsync();
        if (config is null)
        {
            config = new PricingConfig();
            _db.PricingConfigs.Add(config);
            await _db.SaveChangesAsync();
        }
        return config;
    }

    public async Task<List<ParkingRecord>> GetActiveRecordsAsync() =>
        await _db.ParkingRecords
            .Include(r => r.Vehicle)
            .Where(r => r.ExitTime == null)
            .OrderBy(r => r.EntryTime)
            .ToListAsync();

    public async Task<List<ParkingRecord>> GetHistoryAsync(int page = 1, int pageSize = 20) =>
        await _db.ParkingRecords
            .Include(r => r.Vehicle)
            .Where(r => r.ExitTime != null)
            .OrderByDescending(r => r.ExitTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
}
