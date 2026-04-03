using ScepAdmin.Models;

namespace ScepAdmin.Services;

public interface ICrlService
{
    Task<CrlGenerationResult> GenerateAsync(CancellationToken cancellationToken = default);
    Task<CrlStatus?> GetStatusAsync(CancellationToken cancellationToken = default);
}
