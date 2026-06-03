namespace Teryaq.Domain.Features.Drugs;

/// <summary>Identifies how a drug entry was added to the catalog.</summary>
public enum DrugSource
{
    /// <summary>Sourced from the Egyptian Drug Authority registry.</summary>
    EDA,

    /// <summary>Imported via CSV or Excel file.</summary>
    Import,

    /// <summary>Added manually by a pharmacy owner.</summary>
    Manual,
}
