using System.Security.Cryptography.X509Certificates;

namespace ScepAdmin.Services;

/// <summary>Decoded, ready-to-process SCEP PKIOperation request.</summary>
public sealed class ScepPkiRequest
{
    public required string TransactionId  { get; init; }
    public required byte[] SenderNonce    { get; init; }
    public required byte[] CsrBytes       { get; init; }
    public required string ChallengePassword { get; init; }
    /// <summary>The requester's self-signed certificate, used to encrypt the response.</summary>
    public required X509Certificate2 ClientCert { get; init; }
}
