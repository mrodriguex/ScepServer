using System.Security.Cryptography.X509Certificates;

namespace ScepAdmin.Services;

/// <summary>Decodes a raw SCEP PKIOperation request into a <see cref="ScepPkiRequest"/>.</summary>
public interface IScepRequestDecoder
{
    ScepPkiRequest Decode(byte[] requestBytes, X509Certificate2 caCert);
}
