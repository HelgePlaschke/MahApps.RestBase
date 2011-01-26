using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Hammock;
using Hammock.Authentication.Basic;
using Hammock.Authentication.OAuth;
using Hammock.Web;
using MahApps.RESTBase.Delegates;

namespace MahApps.RESTBase
{
    public class RestClientBase : IRestClientBase
    {
        #region Delegates

        #endregion

        public String Authority = "";

        public String OAuthBase = "";
        public String TokenAccessUrl = "";
        public String TokenAuthUrl = "";
        public String TokenRequestUrl = "";
        public String Version = "";

        public IRestClient Client { get; private set; }

        public OAuthCredentials Credentials { get; set; }
        public BasicAuthCredentials BasicCredentials { get; set; }

        private AccessTokenCallbackDelegate AccessTokenCallback { get; set; }
        private RequestUrlCallbackDelegate RequestUrlCallback { get; set; }

        public RestClientBase()
        {
            Client = new RestClient
            {
#if SILVERLIGHT
                HasElevatedPermissions = true,
#endif
                Authority = Authority,
                VersionPath = Version,
            };
        }

        public RestClientBase(IRestClient restClient)
        {
            Client = restClient;
        }

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

        public void BeginGetAccessToken(Uri verifierUri, AccessTokenCallbackDelegate callback)
        {
            var r = new Regex("oauth_token=([^&.]*)&oauth_verifier=([^&.]*)");
            Match match = r.Match(verifierUri.AbsoluteUri);
            BeginGetAccessToken(match.Groups[2].Value, callback);
        }

        public void BeginGetAccessToken(string verifier, AccessTokenCallbackDelegate callback)
        {
            AccessTokenCallback = callback;
            Credentials.Type = OAuthType.AccessToken;
            Credentials.Verifier = verifier.Trim();

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

        public void SetOAuthToken(Credentials credentials)
        {
            var consumerKey = Credentials.ConsumerKey;
            var consumerSecret = Credentials.ConsumerSecret;

            Credentials = new OAuthCredentials()
            {
                Token = credentials.OAuthToken,
                TokenSecret = credentials.OAuthTokenSecret,
                Type = OAuthType.ProtectedResource,
                ConsumerKey = consumerKey,
                ConsumerSecret = consumerSecret,
            };
            Client = new RestClient
                         {
#if SILVERLIGHT
                             HasElevatedPermissions = true,
#endif
                             Authority = Authority,
                             VersionPath = Version,
                         };
        }

        public void BeginRequest(string path, RestCallback callback)
        {
            BeginRequest(path, null, WebMethod.Post, callback);
        }

        public void BeginRequest(string path, RestCallback callback, object userState)
        {
            BeginRequest(path, null, WebMethod.Post, callback, userState);
        }

        public void BeginRequest(string path, IDictionary<string, string> parameters, WebMethod method, RestCallback callback)
        {
            BeginRequest(path, parameters, null, method, callback);
        }

        public void BeginRequest(string path, IDictionary<string, string> parameters, WebMethod method, RestCallback callback, object userState)
        {
            BeginRequest(path, parameters, null, method, callback, userState);
        }

        public void BeginRequest(string path, IDictionary<string, string> parameters, IDictionary<string, File> files, WebMethod method, RestCallback callback)
        {

#if !SILVERLIGHT
            WebRequest.DefaultWebProxy = WebRequest.GetSystemWebProxy();
            WebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultCredentials;
            HttpWebRequest.DefaultWebProxy = HttpWebRequest.GetSystemWebProxy();
            HttpWebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultCredentials;
#endif

            var request = new RestRequest
                              {
                                  Path = path,
                                  Method = method
                              };

            if (files != null)
            {
                foreach (var f in files)
                    request.AddFile(f.Key, f.Value.FileName, f.Value.FilePath);
            }

            if (Credentials != null)
                request.Credentials = Credentials;

            if (parameters != null)
                foreach (var p in parameters)
                {
                    request.AddParameter(p.Key, p.Value);
                }
#if !SILVERLIGHT

            if (files != null)
                foreach (var f in files)
                {
                    byte[] rawData = System.IO.File.ReadAllBytes(f.Value.FilePath);
                    request.AddPostContent(rawData);
                }
            //request.AddFile(f.Key, f.Value.FileName, f.Value.FilePath, "image/jpeg");
#endif

            Client.BeginRequest(request, (req, res, obj) =>
            {
                if (callback != null)
                    callback(req, res, obj);
            });
        }


        public void BeginRequest(string path, IDictionary<string, string> parameters, IDictionary<string, File> files, WebMethod method, RestCallback callback, object userState)
        {

#if !SILVERLIGHT
            WebRequest.DefaultWebProxy = WebRequest.GetSystemWebProxy();
            WebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultCredentials;
            HttpWebRequest.DefaultWebProxy = HttpWebRequest.GetSystemWebProxy();
            HttpWebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultCredentials;
#endif

            var request = new RestRequest
            {
                Path = path,
                Method = method
            };

            if (files != null)
            {
                foreach (var f in files)
                    request.AddFile(f.Key, f.Value.FileName, f.Value.FilePath);
            }

            if (Credentials != null)
                request.Credentials = Credentials;

            if (parameters != null)
                foreach (var p in parameters)
                {
                    request.AddParameter(p.Key, p.Value);
                }
#if !SILVERLIGHT

            if (files != null)
                foreach (var f in files)
                {
                    byte[] rawData = System.IO.File.ReadAllBytes(f.Value.FilePath);
                    request.AddPostContent(rawData);
                }
            //request.AddFile(f.Key, f.Value.FileName, f.Value.FilePath, "image/jpeg");
#endif

            Client.BeginRequest(request, (req, res, obj) =>
            {
                if (callback != null)
                    callback(req, res, obj);
            });
        }
    }
}