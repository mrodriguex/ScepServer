using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace ScepAdmin.Services;

/// <summary>
/// Builds SCEP CertRep responses:
/// success → encrypt cert with client key, wrap in CA-signed SignedData;
/// failure → empty payload in CA-signed SignedData with failInfo attribute.
/// </summary>
public sealed class ScepResponseBuilder : IScepResponseBuilder
{
    public byte[] BuildSuccess(
        X509Certificate2 caCert,
        X509Certificate2 clientCert,
        string transactionId,
        byte[] senderNonce,
        byte[] issuedCertDer)
    {
        // RFC 8894 §3.2.1: the EnvelopedData plaintext MUST be a degenerate
        // certs-only SignedData, not raw DER bytes. jscep (and BouncyCastle Java)
        // call new CMSSignedData(bytes) on the decrypted payload, which requires a
        // proper ContentInfo wrapper — passing raw cert bytes causes "Malformed content".
        var innerPayload = WrapInDegenerateSignedData(issuedCertDer);
        var encryptedPayload = Encrypt(clientCert, innerPayload);
        return Sign(caCert, transactionId, senderNonce, pkiStatus: "0", encryptedPayload);
    }

    public byte[] BuildFailure(
        X509Certificate2 caCert,
        string? transactionId,
        byte[]? senderNonce,
        int failInfo)
    {
        return Sign(caCert, transactionId, senderNonce, pkiStatus: "2", Array.Empty<byte>(), failInfo);
    }

    /// <summary>
    /// Wraps a DER-encoded certificate in a degenerate (certs-only, no signers) SignedData.
    /// BouncyCastle C#'s X509CertificateParser can extract the cert from this format,
    /// and jscep/BouncyCastle Java require it for CertRep decoding.
    /// </summary>
    private static byte[] WrapInDegenerateSignedData(byte[] certDer)
    {
        var cert = new X509CertificateParser().ReadCertificate(certDer);
        var gen  = new CmsSignedDataGenerator();
        gen.AddCertificate(cert);
        // encapsulate: false → eContent is absent; no signers → degenerate
        return gen.Generate(new CmsProcessableByteArray(Array.Empty<byte>()), false).GetEncoded();
    }

    private static byte[] Encrypt(X509Certificate2 clientCert, byte[] payload)
    {
        var edGen = new CmsEnvelopedDataGenerator();
        edGen.AddKeyTransRecipient(DotNetUtilities.FromX509Certificate(clientCert));
        return edGen.Generate(new CmsProcessableByteArray(payload), CmsEnvelopedGenerator.Aes256Cbc).GetEncoded();
    }

    private static byte[] Sign(
        X509Certificate2 caCert,
        string? transactionId,
        byte[]? senderNonce,
        string pkiStatus,
        byte[] innerPayload,
        int? failInfo = null)
    {
        var caPrivKey  = DotNetUtilities.GetKeyPair(caCert.GetRSAPrivateKey()!).Private;
        var bcCaCert   = DotNetUtilities.FromX509Certificate(caCert);
        var signedAttrs = new AttributeTable(BuildAttributes(transactionId, senderNonce, pkiStatus, failInfo));

        var signedGen = new CmsSignedDataGenerator();
        signedGen.AddSigner(caPrivKey, bcCaCert, CmsSignedGenerator.DigestSha256,
            signedAttrs, (AttributeTable?)null);
        signedGen.AddCertificate(bcCaCert);

        return signedGen.Generate(new CmsProcessableByteArray(innerPayload), encapsulate: true).GetEncoded();
    }

    private static Asn1EncodableVector BuildAttributes(
        string? transactionId, byte[]? senderNonce, string pkiStatus, int? failInfo)
    {
        var v = new Asn1EncodableVector();

        v.Add(ScepAttribute(ScepOids.MessageType, new DerPrintableString("3"))); // CertRep
        v.Add(ScepAttribute(ScepOids.PkiStatus,   new DerPrintableString(pkiStatus)));

        if (failInfo.HasValue)
            v.Add(ScepAttribute(ScepOids.FailInfo, new DerPrintableString(failInfo.Value.ToString())));

        if (!string.IsNullOrEmpty(transactionId))
            v.Add(ScepAttribute(ScepOids.TransactionId, new DerPrintableString(transactionId)));

        if (senderNonce is { Length: > 0 })
            v.Add(ScepAttribute(ScepOids.RecipientNonce, new DerOctetString(senderNonce)));

        var serverNonce = new byte[16];
        RandomNumberGenerator.Fill(serverNonce);
        v.Add(ScepAttribute(ScepOids.SenderNonce, new DerOctetString(serverNonce)));

        return v;
    }

    private static Org.BouncyCastle.Asn1.Cms.Attribute ScepAttribute(string oid, Asn1Encodable value)
        => new(new DerObjectIdentifier(oid), new DerSet(value));
}
