namespace ScepAdmin.Services;

public interface ICertificateIssuanceService
{
    Task<IssuanceResult> IssueAsync(ScepEnrollmentRequest request, string clientIp, CancellationToken cancellationToken = default);
    Task<IssuanceResult> RevokeBySerialAsync(string serialNumber, string reason, string clientIp, CancellationToken cancellationToken = default);
}
