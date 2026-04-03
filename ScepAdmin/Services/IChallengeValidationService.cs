namespace ScepAdmin.Services;

public interface IChallengeValidationService
{
    Task<bool> ValidateAsync(int companyId, string challenge, CancellationToken cancellationToken = default);
    Task<bool> ValidateAnyAsync(string challenge, CancellationToken cancellationToken = default);
}
