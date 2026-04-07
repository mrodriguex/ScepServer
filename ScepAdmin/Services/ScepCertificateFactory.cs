using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

namespace ScepAdmin.Services;

/// <summary>
/// Generates a signed X.509v3 end-entity certificate from a PKCS#10 CSR,
/// copying SAN/EKU extensions and adding standard CA-issued extensions.
/// </summary>
public sealed class ScepCertificateFactory : IScepCertificateFactory
{
    public byte[] Build(X509Certificate2 caCert, byte[] csrBytes)
    {
        var caPrivKey = DotNetUtilities.GetKeyPair(caCert.GetRSAPrivateKey()!).Private;
        var bcCaCert  = DotNetUtilities.FromX509Certificate(caCert);
        var csr       = new Pkcs10CertificationRequest(csrBytes);

        var certGen = new X509V3CertificateGenerator();

        var serialBytes = new byte[16];
        RandomNumberGenerator.Fill(serialBytes);
        serialBytes[0] &= 0x7F; // ensure positive
        certGen.SetSerialNumber(new BigInteger(serialBytes));

        certGen.SetIssuerDN(bcCaCert.SubjectDN);
        certGen.SetSubjectDN(csr.GetCertificationRequestInfo().Subject);
        certGen.SetNotBefore(DateTime.UtcNow.AddMinutes(-5));
        certGen.SetNotAfter(DateTime.UtcNow.AddDays(365));
        certGen.SetPublicKey(csr.GetPublicKey());

        AddExtensionsFromCsr(certGen, csr);

        certGen.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(false));
        certGen.AddExtension(X509Extensions.KeyUsage, true,
            new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment));

        var pubKeyBytes = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(csr.GetPublicKey()).GetDerEncoded();
        certGen.AddExtension(X509Extensions.SubjectKeyIdentifier, false,
            new DerOctetString(DigestUtilities.CalculateDigest("SHA1", pubKeyBytes)));

        var caPubKeyBytes = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(bcCaCert.GetPublicKey()).GetDerEncoded();
        certGen.AddExtension(X509Extensions.AuthorityKeyIdentifier, false,
            new AuthorityKeyIdentifier(new DerOctetString(DigestUtilities.CalculateDigest("SHA1", caPubKeyBytes))));

        return certGen.Generate(new Asn1SignatureFactory("SHA256WITHRSA", caPrivKey)).GetEncoded();
    }

    private static void AddExtensionsFromCsr(X509V3CertificateGenerator certGen, Pkcs10CertificationRequest csr)
    {
        var attrs = csr.GetCertificationRequestInfo().Attributes;

        X509Extensions? csrExtensions = null;
        if (attrs != null)
        {
            foreach (Asn1Encodable attrEnc in attrs)
            {
                var attr = Org.BouncyCastle.Asn1.Cms.Attribute.GetInstance(attrEnc);
                if (attr.AttrType.Equals(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest))
                {
                    csrExtensions = X509Extensions.GetInstance(attr.AttrValues[0]);
                    break;
                }
            }
        }

        // ── Copy all requested extensions except SAN (we rebuild SAN below) ────
        if (csrExtensions != null)
        {
            foreach (DerObjectIdentifier extOid in csrExtensions.GetExtensionOids())
            {
                if (extOid.Equals(X509Extensions.SubjectAlternativeName))
                    continue; // handled below

                var ext = csrExtensions.GetExtension(extOid);
                certGen.AddExtension(extOid, ext.IsCritical, ext.GetParsedValue());
            }
        }

        // ── Build SAN: start from CSR entries, then ensure CN is included ──────
        var sanNames = new List<GeneralName>();

        var existingSan = csrExtensions?.GetExtension(X509Extensions.SubjectAlternativeName);
        if (existingSan != null)
        {
            var parsed = GeneralNames.GetInstance(existingSan.GetParsedValue());
            sanNames.AddRange(parsed.GetNames());
        }

        // Always include CN as a DNS SAN (RFC 2818 §3.1)
        var cn = csr.GetCertificationRequestInfo().Subject
            .GetValueList(X509Name.CN)
            .Cast<string>()
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(cn))
        {
            var cnEntry = new GeneralName(GeneralName.DnsName, cn);
            if (!sanNames.Any(n => n.TagNo == GeneralName.DnsName &&
                                   string.Equals(n.Name.ToString(), cn, StringComparison.OrdinalIgnoreCase)))
            {
                sanNames.Add(cnEntry);
            }
        }

        if (sanNames.Count > 0)
            certGen.AddExtension(X509Extensions.SubjectAlternativeName, false,
                new GeneralNames(sanNames.ToArray()));
    }
}
