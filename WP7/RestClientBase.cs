using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hammock;
using Hammock.Authentication.Basic;
using Hammock.Authentication.OAuth;
using Hammock.Web;

namespace MahApps.RESTBase
{
    public class RestClientBase
    {
        #region Delegates

        public delegate void AccessTokenCallbackDelegate(
            RestRequest request, RestResponse response, Credentials Credentials);

        public delegate void RequestUrlCallbackDelegate(RestRequest request, RestResponse response, String Url);

        public delegate void VoidDelegate();

        #endregion

        public String Authority = "";

        public String OAuthBase = "";
        public String TokenAccessUrl = "";
        public String TokenAuthUrl = "";
        public String TokenRequestUrl = "";
        public String Version = "";
        public IRestClient Client { get; set; }

        public OAuthCredentials Credentials { get; set; }
        public BasicAuthCredentials BasicCredentials { get; set; }

        private AccessTokenCallbackDelegate AccessTokenCallback { get; set; }
        private RequestUrlCallbackDelegate RequestUrlCallback { get; set; }

        public void BeginGetRequestUrl(RequestUrlCallbackDelegate callback)
        {
            RequestUrlCallback = callback;
            BeginRequest(TokenRequestUrl, EndGetRequestUrl);
        }

        private void EndGetRequestUrl(RestRequest request, RestResponse response, object userState)
        {
            var r = new Regex("oauth_token=([^&.]*)&oauth_token_secret=([^&.]*)");
            Match match = r.Match(response.Content);
            (Credentials).Token = match.Groups[1].Value;
            (Credentials).TokenSecret = match.Groups[2].Value;

            RequestUrlCallback(request, response, String.Format("{0}{1}?{2}", OAuthBase, TokenAuthUrl, response.Content));
        }

        public void BeginGetAccessToken(Uri VerifierUri, AccessTokenCallbackDelegate callback)
        {
            var r = new Regex("oauth_token=([^&.]*)&oauth_verifier=([^&.]*)");
            Match match = r.Match(VerifierUri.AbsoluteUri);
            BeginGetAccessToken(match.Groups[2].Value, callback);
        }

        public void BeginGetAccessToken(String Verifier, AccessTokenCallbackDelegate callback)
        {
            AccessTokenCallback = callback;
            Credentials.Type = OAuthType.AccessToken;
            Credentials.Verifier = Verifier.Trim();

            BeginRequest(TokenAccessUrl, EndGetAccessToken);
        }

        private void EndGetAccessToken(RestRequest request, RestResponse response, object userState)
        {
            var r = new Regex("oauth_token=([^&.]*)&oauth_token_secret=([^&.]*)");
            Match match = r.Match(response.Content);
            var c = new Credentials
                        {
                            OAuthToken = match.Groups[1].Value,
                            OAuthTokenSecret = match.Groups[2].Value
                        };
            SetOAuthToken(c);

            AccessTokenCallback(request, response, c);
        }

        public void SetOAuthToken(Credentials C)
        {
            Credentials.Token = C.OAuthToken;
            Credentials.TokenSecret = C.OAuthTokenSecret;
            Credentials.Type = OAuthType.ProtectedResource;

            Client = new RestClient
                         {
#if SILVERLIGHT
                             HasElevatedPermissions = true,
#endif
                             Authority = Authority,
                             VersionPath = Version
                         };
        }

        public void BeginRequest(String Path, RestCallback callback)
        {
            BeginRequest(Path, null, WebMethod.Post, callback);
        }

        public void BeginRequest(String Path, Dictionary<String, String> Parameters, WebMethod Method,
                                 RestCallback callback)
        {
            var request = new RestRequest
                              {
                                  Path = Path,
                                  Method = Method
                              };

            if (Credentials != null)
                request.Credentials = Credentials;

            if (Parameters != null)
                foreach (var p in Parameters)
                {
                    request.AddParameter(p.Key, p.Value);
                }

            Client.BeginRequest(request, callback);
        }
    }
}