namespace ScepAdmin.Services;

/// <summary>SCEP attribute OIDs per RFC 8894 / draft-nourse-scep-23 §3.1.1.</summary>
internal static class ScepOids
{
    public const string MessageType    = "2.16.840.1.113733.1.9.2";
    public const string PkiStatus      = "2.16.840.1.113733.1.9.3";
    public const string FailInfo       = "2.16.840.1.113733.1.9.4";
    public const string SenderNonce    = "2.16.840.1.113733.1.9.5";
    public const string RecipientNonce = "2.16.840.1.113733.1.9.6";
    public const string TransactionId  = "2.16.840.1.113733.1.9.7";
}
