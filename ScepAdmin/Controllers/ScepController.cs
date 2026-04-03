using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;

using ScepAdmin.Services;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

using BCX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace ScepAdmin.Controllers;

// SCEP OIDs (RFC 8894 / draft-nourse-scep-23)
internal static class ScepOids
{
    public const string MessageType    = "2.16.840.1.113733.1.9.2";
    public const string PkiStatus      = "2.16.840.1.113733.1.9.3";
    public const string FailInfo       = "2.16.840.1.113733.1.9.4";
    public const string SenderNonce    = "2.16.840.1.113733.1.9.5";
    public const string RecipientNonce = "2.16.840.1.113733.1.9.6";
    public const string TransactionId  = "2.16.840.1.113733.1.9.7";
}

[ApiController]
[Route("scep")]
public class ScepController : ControllerBase
{
    private static readonly string[] Caps =
    [
        "POSTPKIOperation",
        "SHA-1",
        "SHA-256",
        "AES",
        "DES3"
    ];

    private readonly ICertificateService _certificateService;
    private readonly IChallengeValidationService _challengeValidationService;

    public ScepController(ICertificateService certificateService, IChallengeValidationService challengeValidationService)
    {
        _certificateService = certificateService;
        _challengeValidationService = challengeValidationService;
    }

    [HttpGet]
    public IActionResult Get([FromQuery] string operation)
    {
        if (string.Equals(operation, "GetCACaps", StringComparison.OrdinalIgnoreCase))
        {
            return Content(string.Join("\n", Caps), "text/plain");
        }

        if (string.Equals(operation, "GetCACert", StringComparison.OrdinalIgnoreCase))
        {
            var cert = _certificateService.GetCaCertificate();
            if (cert == null)
            {
                return StatusCode(503, new { status = "Unhealthy", reason = "CA certificate not loaded" });
            }

            var bytes = cert.Export(X509ContentType.Cert);
            return File(bytes, "application/x-x509-ca-cert", "ca.cer");
        }

        return BadRequest(new { message = "Unsupported operation" });
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromQuery] string? operation,
        CancellationToken cancellationToken)
    {
        var op = operation ?? "PKIOperation";

        if (!string.Equals(op, "PKIOperation", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Unsupported operation");

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, cancellationToken);
        var requestBytes = ms.ToArray();

        Console.WriteLine($"[SCEP] Received {requestBytes.Length} bytes");

        try
        {
            // ── 1. Decode outer SignedCms ────────────────────────────────────────
            var outerCms = new CmsSignedData(requestBytes);

            // Extract SCEP attributes from the first signer
            var signerInfos = outerCms.GetSignerInfos().GetSigners().Cast<SignerInformation>().ToList();
            if (signerInfos.Count == 0)
                return StatusCode(400, "No signer in SCEP request");

            var signerInfo = signerInfos[0];
            var signedAttrs = signerInfo.SignedAttributes;

            string? transactionId = GetPrintableStringAttr(signedAttrs, ScepOids.TransactionId);
            byte[]? senderNonce   = GetOctetStringAttr(signedAttrs, ScepOids.SenderNonce);

            Console.WriteLine($"[SCEP] transactionId={transactionId}");

            // ── 2. Decrypt inner EnvelopedCms to get the raw PKCS#10 bytes ──────
            var caCert = _certificateService.GetCaCertificate();
            if (caCert == null)
                return StatusCode(503, "CA certificate not loaded");

            // The outer signed content is the EnvelopedData DER bytes
            var envelopedContentStream = new MemoryStream();
            outerCms.SignedContent.Write(envelopedContentStream);
            var envelopedBytes = envelopedContentStream.ToArray();

            // Use System.Security.Cryptography.Pkcs to decrypt (needs the CA private key)
            var envelopedCms = new EnvelopedCms();
            envelopedCms.Decode(envelopedBytes);
            envelopedCms.Decrypt(new X509Certificate2Collection(caCert));
            var csrBytes = envelopedCms.ContentInfo.Content;

            Console.WriteLine($"[SCEP] Decrypted inner CMS, CSR={csrBytes.Length} bytes");

            // ── 3. Extract CSR public key and subject ────────────────────────────
            var bcCsr = new Pkcs10CertificationRequest(csrBytes);
            var subject = bcCsr.GetCertificationRequestInfo().Subject;
            var cn = subject.GetValueList(X509Name.CN).Cast<string>().FirstOrDefault() ?? "Unknown";
            Console.WriteLine($"[SCEP] CSR Subject={subject}, CN={cn}");

            // ── 4. Extract challenge password from CSR attributes ────────────────
            var challengePassword = ExtractChallengePassword(bcCsr);
            Console.WriteLine($"[SCEP] Challenge={challengePassword}");

            // ── 5. Validate challenge against any active company ─────────────────
            // For SCEP protocol we validate the raw challenge without a company ID;
            // look up the first matching company.
            bool challengeValid = await ValidateChallengeAnyCompanyAsync(challengePassword, cancellationToken);
            if (!challengeValid)
            {
                Console.WriteLine("[SCEP] Invalid challenge password — returning FAILURE");
                var failResponse = BuildCertRepFailure(caCert, transactionId, senderNonce, failInfo: 2 /* badRequest */);
                return File(failResponse, "application/x-pki-message");
            }

            // ── 6. Issue certificate (sign with CA key) ──────────────────────────
            var issuedCertDer = IssueCertificate(caCert, bcCsr);
            Console.WriteLine($"[SCEP] Issued certificate, {issuedCertDer.Length} bytes");

            // ── 7. Get the client's public key (from the self-signed cert in the request signers) ──
            // The self-signed cert is embedded in the outer SignedCms certificate store.
            var clientCertForEncryption = GetClientCertFromSignedData(outerCms);

            // ── 8. Build SCEP CertRep response ───────────────────────────────────
            var response = BuildCertRepSuccess(caCert, clientCertForEncryption, transactionId, senderNonce, issuedCertDer);
            return File(response, "application/x-pki-message");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SCEP] Error: {ex}");
            return StatusCode(500, ex.Message);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static string ExtractChallengePassword(Pkcs10CertificationRequest csr)
    {
        var attrs = csr.GetCertificationRequestInfo().Attributes;
        if (attrs == null) return string.Empty;

        foreach (Asn1Encodable attr in attrs)
        {
            var attribute = Org.BouncyCastle.Asn1.Cms.Attribute.GetInstance(attr);
            if (attribute.AttrType.Equals(PkcsObjectIdentifiers.Pkcs9AtChallengePassword))
            {
                var values = attribute.AttrValues;
                if (values.Count > 0)
                {
                    var val = values[0];
                    if (val is DerPrintableString ps) return ps.GetString();
                    if (val is DerUtf8String us) return us.GetString();
                    if (val is DerIA5String ia) return ia.GetString();
                    return val.ToString() ?? string.Empty;
                }
            }
        }
        return string.Empty;
    }

    private async Task<bool> ValidateChallengeAnyCompanyAsync(string challenge, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(challenge)) return false;
        return await _challengeValidationService.ValidateAnyAsync(challenge, ct);
    }

    private static string? GetPrintableStringAttr(Org.BouncyCastle.Asn1.Cms.AttributeTable attrs, string oid)
    {
        var attr = attrs[new DerObjectIdentifier(oid)];
        if (attr == null) return null;
        var val = attr.AttrValues[0];
        if (val is DerPrintableString ps) return ps.GetString();
        if (val is DerUtf8String us) return us.GetString();
        return val?.ToString();
    }

    private static byte[]? GetOctetStringAttr(Org.BouncyCastle.Asn1.Cms.AttributeTable attrs, string oid)
    {
        var attr = attrs[new DerObjectIdentifier(oid)];
        if (attr == null) return null;
        var val = attr.AttrValues[0];
        // The attr value is an OCTET STRING wrapped in its DER encoding
        var asn1 = Asn1Object.FromByteArray(val.GetDerEncoded());
        if (asn1 is DerOctetString oct) return oct.GetOctets();
        // already raw bytes
        return val.GetDerEncoded();
    }

    private static System.Security.Cryptography.X509Certificates.X509Certificate2 GetClientCertFromSignedData(CmsSignedData cms)
    {
        var store = cms.GetCertificates();
        var certs = store.EnumerateMatches(null).Cast<BCX509Certificate>().ToList();
        if (certs.Count == 0)
            throw new InvalidOperationException("No certificates found in the SCEP request's SignedData");
        // Return the first cert (the self-signed requester cert)
        return new System.Security.Cryptography.X509Certificates.X509Certificate2(certs[0].GetEncoded());
    }

    private static byte[] IssueCertificate(
        System.Security.Cryptography.X509Certificates.X509Certificate2 caCert,
        Pkcs10CertificationRequest csr)
    {
        var caPrivKey = DotNetUtilities.GetKeyPair(caCert.GetRSAPrivateKey()!).Private;
        var bcCaCert  = DotNetUtilities.FromX509Certificate(caCert);

        var certGen = new X509V3CertificateGenerator();

        // Serial: random 16 bytes
        var serialBytes = new byte[16];
        RandomNumberGenerator.Fill(serialBytes);
        serialBytes[0] &= 0x7F; // ensure positive
        certGen.SetSerialNumber(new BigInteger(serialBytes));

        certGen.SetIssuerDN(bcCaCert.SubjectDN);
        certGen.SetSubjectDN(csr.GetCertificationRequestInfo().Subject);
        certGen.SetNotBefore(DateTime.UtcNow.AddMinutes(-5));
        certGen.SetNotAfter(DateTime.UtcNow.AddDays(365));
        certGen.SetPublicKey(csr.GetPublicKey());

        // Copy SAN and EKU extensions from the CSR if present
        var csrAttributes = csr.GetCertificationRequestInfo().Attributes;
        if (csrAttributes != null)
        {
            foreach (Asn1Encodable attrEnc in csrAttributes)
            {
                var attr = Org.BouncyCastle.Asn1.Cms.Attribute.GetInstance(attrEnc);
                if (attr.AttrType.Equals(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest))
                {
                    var extSeq = X509Extensions.GetInstance(attr.AttrValues[0]);
                    foreach (DerObjectIdentifier extOid in extSeq.GetExtensionOids())
                    {
                        var ext = extSeq.GetExtension(extOid);
                        certGen.AddExtension(extOid, ext.IsCritical, ext.GetParsedValue());
                    }
                }
            }
        }

        // Basic constraints: not a CA
        certGen.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(false));

        // Key usage: digital signature + key encipherment
        certGen.AddExtension(X509Extensions.KeyUsage, true,
            new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment));

        // Subject Key Identifier
        var pubKeyBytes = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(csr.GetPublicKey()).GetDerEncoded();
        var skiDigest = DigestUtilities.CalculateDigest("SHA1", pubKeyBytes);
        certGen.AddExtension(X509Extensions.SubjectKeyIdentifier, false, new DerOctetString(skiDigest));

        // Authority Key Identifier from CA
        var caPubKeyBytes = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(bcCaCert.GetPublicKey()).GetDerEncoded();
        var akiDigest = DigestUtilities.CalculateDigest("SHA1", caPubKeyBytes);
        certGen.AddExtension(X509Extensions.AuthorityKeyIdentifier, false,
            new Org.BouncyCastle.Asn1.X509.AuthorityKeyIdentifier(new DerOctetString(akiDigest)));

        var signer = new Asn1SignatureFactory("SHA256WITHRSA", caPrivKey);
        var issuedCert = certGen.Generate(signer);

        return issuedCert.GetEncoded();
    }

    private static byte[] BuildCertRepSuccess(
        System.Security.Cryptography.X509Certificates.X509Certificate2 caCert,
        System.Security.Cryptography.X509Certificates.X509Certificate2 clientCert,
        string? transactionId,
        byte[]? senderNonce,
        byte[] issuedCertDer)
    {
        // ── a. Encrypt the issued certificate DER with the client's public key ──
        var encryptedPayload = EncryptWithClientCert(clientCert, issuedCertDer);

        // ── b. Sign the encrypted payload with the CA private key ────────────────
        return BuildSignedScepResponse(caCert, transactionId, senderNonce, "0" /* SUCCESS */, encryptedPayload);
    }

    private static byte[] BuildCertRepFailure(
        System.Security.Cryptography.X509Certificates.X509Certificate2 caCert,
        string? transactionId,
        byte[]? senderNonce,
        int failInfo)
    {
        // For failure, the content is empty (or we can use the CA cert bytes as a placeholder)
        var emptyContent = Array.Empty<byte>();
        return BuildSignedScepResponse(caCert, transactionId, senderNonce, "2" /* FAILURE */, emptyContent, failInfo);
    }

    private static byte[] EncryptWithClientCert(
        System.Security.Cryptography.X509Certificates.X509Certificate2 clientCert,
        byte[] payload)
    {
        // Use BouncyCastle CMS EnvelopedData with the client's RSA public key
        var bcClientCert = DotNetUtilities.FromX509Certificate(clientCert);

        var edGen = new CmsEnvelopedDataGenerator();
        edGen.AddKeyTransRecipient(bcClientCert);

        var processable = new CmsProcessableByteArray(payload);
        var envelopedData = edGen.Generate(processable, CmsEnvelopedGenerator.Aes256Cbc);
        return envelopedData.GetEncoded();
    }

    private static byte[] BuildSignedScepResponse(
        System.Security.Cryptography.X509Certificates.X509Certificate2 caCert,
        string? transactionId,
        byte[]? senderNonce,
        string pkiStatus,
        byte[] innerPayload,
        int? failInfo = null)
    {
        var caPrivKey   = DotNetUtilities.GetKeyPair(caCert.GetRSAPrivateKey()!).Private;
        var bcCaCert    = DotNetUtilities.FromX509Certificate(caCert);

        // ── Signed attributes ────────────────────────────────────────────────────
        var signedAttrs = new Org.BouncyCastle.Asn1.Cms.AttributeTable(
            BuildScepResponseAttributes(transactionId, senderNonce, pkiStatus, failInfo));

        var signedGen = new CmsSignedDataGenerator();
        signedGen.AddSigner(caPrivKey, bcCaCert, CmsSignedGenerator.DigestSha256,
            signedAttrs, (Org.BouncyCastle.Asn1.Cms.AttributeTable?)null);
        signedGen.AddCertificate(bcCaCert);

        var cms = signedGen.Generate(
            new CmsProcessableByteArray(innerPayload),
            true); // encapsulate content

        return cms.GetEncoded();
    }

    private static Asn1EncodableVector BuildScepResponseAttributes(
        string? transactionId, byte[]? senderNonce, string pkiStatus, int? failInfo)
    {
        var v = new Asn1EncodableVector();

        // messageType = 3 (CertRep)
        v.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
            new DerObjectIdentifier(ScepOids.MessageType),
            new DerSet(new DerPrintableString("3"))));

        // pkiStatus
        v.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
            new DerObjectIdentifier(ScepOids.PkiStatus),
            new DerSet(new DerPrintableString(pkiStatus))));

        // failInfo (only on failure)
        if (failInfo.HasValue)
        {
            v.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
                new DerObjectIdentifier(ScepOids.FailInfo),
                new DerSet(new DerPrintableString(failInfo.Value.ToString()))));
        }

        // transactionId
        if (!string.IsNullOrEmpty(transactionId))
        {
            v.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
                new DerObjectIdentifier(ScepOids.TransactionId),
                new DerSet(new DerPrintableString(transactionId))));
        }

        // recipientNonce = senderNonce from the request
        if (senderNonce != null && senderNonce.Length > 0)
        {
            v.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
                new DerObjectIdentifier(ScepOids.RecipientNonce),
                new DerSet(new DerOctetString(senderNonce))));
        }

        // senderNonce (server generates a fresh one)
        var serverNonce = new byte[16];
        RandomNumberGenerator.Fill(serverNonce);
        v.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
            new DerObjectIdentifier(ScepOids.SenderNonce),
            new DerSet(new DerOctetString(serverNonce))));

        return v;
    }
}
