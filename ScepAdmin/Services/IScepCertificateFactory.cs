using System.Security.Cryptography.X509Certificates;

namespace ScepAdmin.Services;

/// <summary>Builds a signed X.509 certificate from a PKCS#10 CSR using the CA key.</summary>
public interface IScepCertificateFactory
{
    byte[] Build(X509Certificate2 caCert, byte[] csrBytes);
}
