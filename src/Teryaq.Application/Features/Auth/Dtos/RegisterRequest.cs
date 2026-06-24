namespace Teryaq.Application.Features.Auth.Dtos;

/// <summary>Payload for registering a new pharmacy (tenant + owner user + main branch).</summary>
/// <param name="PharmacyName">Display name of the pharmacy business.</param>
/// <param name="BranchName">Display name of the main branch.</param>
/// <param name="BranchAddress">Optional street address of the main branch.</param>
/// <param name="BranchPhone">Optional contact phone number of the main branch.</param>
/// <param name="OwnerEmail">Email address used to log in.</param>
/// <param name="OwnerPassword">Password for the owner account (min 8 chars).</param>
/// <param name="OwnerName">Display name for the owner; defaults to the email address when omitted.</param>
public sealed record RegisterRequest(
    string PharmacyName,
    string BranchName,
    string? BranchAddress,
    string? BranchPhone,
    string OwnerEmail,
    string OwnerPassword,
    string? OwnerName = null);
