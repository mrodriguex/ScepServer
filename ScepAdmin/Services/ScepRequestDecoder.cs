using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Pkcs;

using BCX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace ScepAdmin.Services;

/// <summary>
/// Decodes a raw SCEP PKIOperation request:
/// outer SignedData → extract SCEP attributes → decrypt inner EnvelopedData → parse CSR.
/// </summary>
public sealed class ScepRequestDecoder : IScepRequestDecoder
{
    /// <summary>
    /// Decodes a SCEP PKIOperation request, extracting transactionId, senderNonce, CSR, challenge, and client cert.
    /// </summary>
    /// <param name="requestBytes">Raw PKCS#7 SignedData bytes.</param>
    /// <param name="caCert">CA certificate (with private key for decryption).</param>
    /// <returns>Decoded ScepPkiRequest.</returns>
    public ScepPkiRequest Decode(byte[] requestBytes, X509Certificate2 caCert)
    {
        // Parse outer SignedData
        var outerCms    = new CmsSignedData(requestBytes);
        var signerInfos = outerCms.GetSignerInfos().GetSigners().Cast<SignerInformation>().ToList();

        if (signerInfos.Count == 0)
            throw new InvalidOperationException("No signer found in SCEP request.");

        var signedAttrs = signerInfos[0].SignedAttributes;

        // Extract SCEP attributes
        var transactionId = GetPrintableStringAttr(signedAttrs, ScepOids.TransactionId)
            ?? throw new InvalidOperationException("Missing transactionId in SCEP request.");
        var senderNonce = GetOctetStringAttr(signedAttrs, ScepOids.SenderNonce)
            ?? throw new InvalidOperationException("Missing senderNonce in SCEP request.");

        // Decrypt inner EnvelopedData using the CA private key
        using var envelopedStream = new MemoryStream();
        outerCms.SignedContent.Write(envelopedStream);

        var envelopedCms = new EnvelopedCms();
        envelopedCms.Decode(envelopedStream.ToArray());
        envelopedCms.Decrypt(new X509Certificate2Collection(caCert));
        var csrBytes = envelopedCms.ContentInfo.Content;

        // Parse CSR and extract challenge and client cert
        var csr              = new Pkcs10CertificationRequest(csrBytes);
        var challengePassword = ExtractChallengePassword(csr);
        var clientCert       = ExtractClientCert(outerCms);

        return new ScepPkiRequest
        {
            TransactionId     = transactionId,
            SenderNonce       = senderNonce,
            CsrBytes          = csrBytes,
            ChallengePassword = challengePassword,
            ClientCert        = clientCert,
        };
    }

    /// <summary>
    /// Extracts the challenge password from a PKCS#10 CSR, if present.
    /// </summary>
    private static string ExtractChallengePassword(Pkcs10CertificationRequest csr)
    {
        var attrs = csr.GetCertificationRequestInfo().Attributes;
        if (attrs == null) return string.Empty;

        foreach (Asn1Encodable attrEnc in attrs)
        {
            var attribute = Org.BouncyCastle.Asn1.Cms.Attribute.GetInstance(attrEnc);
            if (!attribute.AttrType.Equals(PkcsObjectIdentifiers.Pkcs9AtChallengePassword))
                continue;

            if (attribute.AttrValues.Count > 0)
            {
                var val = attribute.AttrValues[0];
                if (val is DerPrintableString ps) return ps.GetString();
                if (val is DerUtf8String us)     return us.GetString();
                if (val is DerIA5String ia)      return ia.GetString();
                return val.ToString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts the client certificate from the SignedData cert store.
    /// </summary>
    private static X509Certificate2 ExtractClientCert(CmsSignedData cms)
    {
        var certs = cms.GetCertificates().EnumerateMatches(null).Cast<BCX509Certificate>().ToList();
        if (certs.Count == 0)
            throw new InvalidOperationException("No client certificate found in SCEP request's SignedData.");

        return new X509Certificate2(certs[0].GetEncoded());
    }

    /// <summary>
    /// Helper to extract a printable string attribute from SCEP signed attributes.
    /// </summary>
    private static string? GetPrintableStringAttr(AttributeTable attrs, string oid)
    {
        var attr = attrs[new DerObjectIdentifier(oid)];
        if (attr == null) return null;
        var val = attr.AttrValues[0];
        if (val is DerPrintableString ps) return ps.GetString();
        if (val is DerUtf8String us)     return us.GetString();
        return val?.ToString();
    }

    /// <summary>
    /// Helper to extract an octet string attribute from SCEP signed attributes.
    /// </summary>
    private static byte[]? GetOctetStringAttr(AttributeTable attrs, string oid)
    {
        var attr = attrs[new DerObjectIdentifier(oid)];
        if (attr == null) return null;
        var val  = attr.AttrValues[0];
        var asn1 = Asn1Object.FromByteArray(val.GetDerEncoded());
        if (asn1 is DerOctetString oct) return oct.GetOctets();
        return val.GetDerEncoded();
    }
}
