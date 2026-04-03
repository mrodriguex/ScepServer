using System.Security.Cryptography.X509Certificates;

namespace ScepAdmin.Services;

/// <summary>Builds SCEP CertRep response messages (SignedData wrapping EnvelopedData).</summary>
public interface IScepResponseBuilder
{
    byte[] BuildSuccess(
        X509Certificate2 caCert,
        X509Certificate2 clientCert,
        string transactionId,
        byte[] senderNonce,
        byte[] issuedCertDer);

    byte[] BuildFailure(
        X509Certificate2 caCert,
        string? transactionId,
        byte[]? senderNonce,
        int failInfo);
}
