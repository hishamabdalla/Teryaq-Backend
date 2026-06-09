namespace Teryaq.Infrastructure.Persistence.Seed;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Teryaq.Domain.Features.Drugs;

/// <summary>Seeds the global Drug catalog from the bundled EDA JSON file on first run.</summary>
public static partial class DrugSeeder
{
    private const string SeedFileName = "egyptian-drugs.json";
    private const int BatchSize = 500;

    /// <summary>Inserts all EDA drugs into the catalog if the table is empty. Safe to call on every startup.</summary>
    public static async Task SeedAsync(AppDbContext context, ILogger logger, CancellationToken ct = default)
    {
        if (await context.Drugs.AnyAsync(ct))
        {
            Log.AlreadySeeded(logger);
            return;
        }

        string filePath = Path.Combine(AppContext.BaseDirectory, "Seed", SeedFileName);
        if (!File.Exists(filePath))
        {
            Log.FileNotFound(logger, filePath);
            return;
        }

        await using var stream = File.OpenRead(filePath);
        var records = await JsonSerializer.DeserializeAsync<List<EdaDrugRecord>>(stream, JsonOptions, ct)
                      ?? [];

        Log.SeedingStarted(logger, records.Count);

        int inserted = 0;
        for (int i = 0; i < records.Count; i += BatchSize)
        {
            var batch = records.GetRange(i, Math.Min(BatchSize, records.Count - i));

            var drugs = batch.Select(r => Drug.Create(
                tradeNameAr: Truncate(r.CommercialNameAr ?? string.Empty, 500),
                tradeNameEn: Truncate(r.CommercialNameEn, 500),
                genericName: r.ScientificName,
                dosageForm: Truncate(NormaliseDosageForm(r.Route), 100),
                strength: "-",
                packSize: 1,
                price: (decimal)r.PriceEgp,
                barcode: null,
                manufacturerAr: null,
                manufacturerEn: r.Manufacturer is null ? null : Truncate(r.Manufacturer, 300),
                source: DrugSource.EDA));

            context.Drugs.AddRange(drugs);
            await context.SaveChangesAsync(ct);
            context.ChangeTracker.Clear();

            inserted += batch.Count;
            Log.BatchProgress(logger, inserted, records.Count);
        }

        Log.SeedingComplete(logger, inserted);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string NormaliseDosageForm(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
            return "Unknown";

        return System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(route.Replace('.', ' ').Replace('_', ' ').ToLowerInvariant());
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Drug catalog already seeded — skipping.")]
        public static partial void AlreadySeeded(ILogger logger);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Drug seed file not found at {Path} — skipping.")]
        public static partial void FileNotFound(ILogger logger, string path);

        [LoggerMessage(Level = LogLevel.Information, Message = "Seeding {Count} drugs from EDA catalog...")]
        public static partial void SeedingStarted(ILogger logger, int count);

        [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {Inserted}/{Total} drugs...")]
        public static partial void BatchProgress(ILogger logger, int inserted, int total);

        [LoggerMessage(Level = LogLevel.Information, Message = "Drug catalog seeding complete — {Total} drugs inserted.")]
        public static partial void SeedingComplete(ILogger logger, int total);
    }

    private sealed class EdaDrugRecord
    {
        [JsonPropertyName("commercial_name_en")]
        public string CommercialNameEn { get; init; } = string.Empty;

        [JsonPropertyName("commercial_name_ar")]
        public string? CommercialNameAr { get; init; }

        [JsonPropertyName("scientific_name")]
        public string ScientificName { get; init; } = string.Empty;

        [JsonPropertyName("manufacturer")]
        public string? Manufacturer { get; init; }

        [JsonPropertyName("route")]
        public string Route { get; init; } = string.Empty;

        [JsonPropertyName("price_egp")]
        public double PriceEgp { get; init; }
    }
}
