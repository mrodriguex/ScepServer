namespace ScepAdmin.Services;

public interface IChallengeValidationService
{
    Task<bool> ValidateAsync(int companyId, string challenge, CancellationToken cancellationToken = default);
    Task<bool> ValidateAnyAsync(string challenge, CancellationToken cancellationToken = default);
    /// <summary>Returns the company ID if the challenge is valid for any active company, or null if invalid.</summary>
    Task<int?> GetCompanyIdAsync(string challenge, CancellationToken cancellationToken = default);
}
