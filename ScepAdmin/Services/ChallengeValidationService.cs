using Microsoft.EntityFrameworkCore;
using ScepAdmin.Data;

namespace ScepAdmin.Services;

public class ChallengeValidationService : IChallengeValidationService
{
    private readonly AppDbContext _db;

    public ChallengeValidationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ValidateAsync(int companyId, string challenge, CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId && c.IsActive, cancellationToken);

        if (company == null || string.IsNullOrWhiteSpace(company.ChallengePassword))
        {
            return false;
        }

        return string.Equals(company.ChallengePassword, challenge, StringComparison.Ordinal);
    }

    public async Task<bool> ValidateAnyAsync(string challenge, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(challenge)) return false;

        return await _db.Companies
            .AsNoTracking()
            .AnyAsync(c => c.IsActive && c.ChallengePassword == challenge, cancellationToken);
    }
}
