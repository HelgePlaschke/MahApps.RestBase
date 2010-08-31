using System;
using System.Runtime.Serialization;

namespace Hammock.Authentication.OAuth
{
#if !SILVERLIGHT
    [Serializable]
#endif
    public enum OAuthSignatureMethod
    {
#if !SILVERLIGHT && !Smartphone && !ClientProfiles
        [EnumMember] HmacSha1,
        [EnumMember] PlainText,
        [EnumMember] RsaSha1
#else
        HmacSha1,
        PlainText,
        RsaSha1
#endif
    }
}