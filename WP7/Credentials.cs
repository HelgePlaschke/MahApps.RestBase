using System.Xml.Serialization;

namespace MahApps.RESTBase
{
    [XmlRoot(ElementName = "credentials")]
    public class Credentials
    {
        [XmlElement(ElementName = "oauth_token")]
        public string OAuthToken { get; set; }

        [XmlElement(ElementName = "oauth_token_secret")]
        public string OAuthTokenSecret { get; set; }
    }
}