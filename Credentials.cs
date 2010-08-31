using System;
using System.Xml.Serialization;

namespace MahApps.RESTBase
{
    [XmlRoot(ElementName = "credentials")]
    public class Credentials
    {
        [XmlElement(ElementName = "oauth_token")]
        public String OAuthToken { get; set; }

        [XmlElement(ElementName = "oauth_token_secret")]
        public String OAuthTokenSecret { get; set; }
    }
}
