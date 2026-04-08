using Microsoft.EntityFrameworkCore;
using ScepAdmin.Data;


/// <summary>
/// Validates SCEP challenge passwords against active companies.
/// </summary>
namespace ScepAdmin.Services;

/// <summary>
/// Service for validating SCEP challenge passwords and resolving company IDs.
/// </summary>
public class ChallengeValidationService : IChallengeValidationService
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Constructs the service with a database context.
    /// </summary>
    public ChallengeValidationService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Validates a challenge password for a specific company.
    /// </summary>
    public async Task<bool> ValidateAsync(int companyId, string challenge, CancellationToken cancellationToken = default)
    {
        // Find the company and check challenge
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId && c.IsActive, cancellationToken);

        if (company == null || string.IsNullOrWhiteSpace(company.ChallengePassword))
        {
            return false;
        }

        return string.Equals(company.ChallengePassword, challenge, StringComparison.Ordinal);
    }

    /// <summary>
    /// Validates a challenge password for any active company.
    /// </summary>
    public async Task<bool> ValidateAnyAsync(string challenge, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(challenge)) return false;

        // Check if any active company matches the challenge
        return await _db.Companies
            .AsNoTracking()
            .AnyAsync(c => c.IsActive && c.ChallengePassword == challenge, cancellationToken);
    }

    /// <summary>
    /// Returns the company ID for a valid challenge password, or null if not found.
    /// </summary>
    public async Task<int?> GetCompanyIdAsync(string challenge, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(challenge)) return null;

        // Find the first active company with this challenge
        return await _db.Companies
            .AsNoTracking()
            .Where(c => c.IsActive && c.ChallengePassword == challenge)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
