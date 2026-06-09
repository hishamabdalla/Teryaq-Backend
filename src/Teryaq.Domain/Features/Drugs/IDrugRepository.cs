namespace Teryaq.Domain.Features.Drugs;

using Teryaq.Domain.Interfaces;

/// <summary>Repository contract for the global Drug catalog.</summary>
public interface IDrugRepository : IRepository<Drug>
{
    /// <summary>Returns the drug with the given barcode, or <see langword="null"/> if not found.</summary>
    /// <param name="barcode">The EDA barcode to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Drug?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if a drug with the same English trade name, strength, and dosage form already exists.</summary>
    /// <param name="tradeNameEn">English trade name to check.</param>
    /// <param name="strength">Dosage strength to check.</param>
    /// <param name="dosageForm">Dosage form to check.</param>
    /// <param name="excludeId">Optional drug ID to exclude from the check (used during updates).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> ExistsByNameAndFormAsync(
        string tradeNameEn,
        string strength,
        string dosageForm,
        Guid? excludeId,
        CancellationToken ct = default);

    /// <summary>Returns a filtered, paged slice of the catalog together with the total matching count.</summary>
    /// <param name="search">Optional substring matched against <c>TradeNameAr</c>, <c>TradeNameEn</c>, and <c>Barcode</c>.</param>
    /// <param name="source">Optional source filter.</param>
    /// <param name="isActive">Optional active-status filter.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Maximum records to return.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<(IReadOnlyList<Drug> Items, int TotalCount)> SearchAsync(
        string? search,
        DrugSource? source,
        bool? isActive,
        int skip,
        int take,
        CancellationToken ct = default);
}
