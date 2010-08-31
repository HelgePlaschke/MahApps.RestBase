using System;
using System.Collections.Generic;
using Hammock.Extensions;
using Hammock.Web;
#if !SILVERLIGHT
using System.Web;
using System.Compat.Web;
#endif

namespace Hammock.Authentication.OAuth
{
    /// <summary>
    /// A class to encapsulate OAuth authentication flow.
    /// <seealso cref="http://oauth.net/core/1.0#anchor9"/>
    /// </summary>
    public class OAuthWorkflow
    {
        public string Version { get; set; }
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string Token { get; set; }
        public string TokenSecret { get; set; }
        public string CallbackUrl { get; set; }
        public string Verifier { get; set; }

        public OAuthSignatureMethod SignatureMethod { get; set; }
        public OAuthSignatureTreatment SignatureTreatment { get; set; }
        public OAuthParameterHandling ParameterHandling { get; set; }

        public string ClientUsername { get; set; }
        public string ClientPassword { get; set; }

        /// <summary>
        /// The request token url.
        /// </summary>
        /// <seealso cref="http://oauth.net/core/1.0#request_urls"/>
        public string RequestTokenUrl { get; set; }

        /// <summary>
        /// The access token url.
        /// </summary>
        /// <seealso cref="http://oauth.net/core/1.0#request_urls"/>
        public string AccessTokenUrl { get; set; }

        /// <summary>
        /// THe user authorization url.
        /// </summary>
        /// <seealso cref="http://oauth.net/core/1.0#request_urls"/>
        public string AuthorizationUrl { get; set; }

        /// <summary>
        /// Generates a <see cref="OAuthWebQueryInfo"/> instance to pass to an
        /// <see cref="OAuthWebQuery" /> for the purpose of requesting an
        /// unauthorized request token.
        /// </summary>
        /// <param name="method">The HTTP method for the intended request</param>
        /// <seealso cref="http://oauth.net/core/1.0#anchor9"/>
        /// <returns></returns>
        public OAuthWebQueryInfo BuildRequestTokenInfo(WebMethod method)
        {
            return BuildRequestTokenInfo(method, null);
        }

        /// <summary>
        /// Generates a <see cref="OAuthWebQueryInfo"/> instance to pass to an
        /// <see cref="OAuthWebQuery" /> for the purpose of requesting an
        /// unauthorized request token.
        /// </summary>
        /// <param name="method">The HTTP method for the intended request</param>
        /// <param name="parameters">Any existing, non-OAuth query parameters desired in the request</param>
        /// <seealso cref="http://oauth.net/core/1.0#anchor9"/>
        /// <returns></returns>
        public OAuthWebQueryInfo BuildRequestTokenInfo(WebMethod method, WebParameterCollection parameters)
        {
            ValidateTokenRequestState();

            if (parameters == null)
            {
                parameters = new WebParameterCollection();
            }

            var timestamp = OAuthTools.GetTimestamp();
            var nonce = OAuthTools.GetNonce();

            AddAuthParameters(parameters, timestamp, nonce);

            var signatureBase = OAuthTools.ConcatenateRequestElements(method, RequestTokenUrl, parameters);
            var signature = OAuthTools.GetSignature(SignatureMethod, SignatureTreatment, signatureBase, ConsumerSecret);

            var info = new OAuthWebQueryInfo
                           {
                               WebMethod = method,
                               ParameterHandling = ParameterHandling,
                               ConsumerKey = ConsumerKey,
                               SignatureMethod = SignatureMethod.ToRequestValue(),
                               SignatureTreatment = SignatureTreatment,
                               Signature = signature,
                               Timestamp = timestamp,
                               Nonce = nonce,
                               Version = Version,
                               Callback = OAuthTools.UrlEncodeRelaxed(CallbackUrl ?? ""),
                               UserAgent = "Hammock",
                               TokenSecret = TokenSecret,
                               ConsumerSecret = ConsumerSecret
                           };

            return info;
        }

        /// <summary>
        /// Generates a <see cref="OAuthWebQueryInfo"/> instance to pass to an
        /// <see cref="OAuthWebQuery" /> for the purpose of exchanging a request token
        /// for an access token authorized by the user at the Service Provider site.
        /// </summary>
        /// <param name="method">The HTTP method for the intended request</param>
        /// <seealso cref="http://oauth.net/core/1.0#anchor9"/>
        public OAuthWebQueryInfo BuildAccessTokenInfo(WebMethod method)
        {
            return BuildAccessTokenInfo(method, null);
        }

        /// <summary>
        /// Generates a <see cref="OAuthWebQueryInfo"/> instance to pass to an
        /// <see cref="OAuthWebQuery" /> for the purpose of exchanging a request token
        /// for an access token authorized by the user at the Service Provider site.
        /// </summary>
        /// <param name="method">The HTTP method for the intended request</param>
        /// <seealso cref="http://oauth.net/core/1.0#anchor9"/>
        /// <param name="parameters">Any existing, non-OAuth query parameters desired in the request</param>
        public OAuthWebQueryInfo BuildAccessTokenInfo(WebMethod method, WebParameterCollection parameters)
        {
            ValidateAccessRequestState();

            if (parameters == null)
            {
                parameters = new WebParameterCollection();
            }

            var uri = new Uri(AccessTokenUrl);
            var timestamp = OAuthTools.GetTimestamp();
            var nonce = OAuthTools.GetNonce();

            AddAuthParameters(parameters, timestamp, nonce);

            var signatureBase = OAuthTools.ConcatenateRequestElements(method, uri.ToString(), parameters);
            var signature = OAuthTools.GetSignature(SignatureMethod, SignatureTreatment, signatureBase, ConsumerSecret, TokenSecret);

            var info = new OAuthWebQueryInfo
                           {
                               WebMethod = method,
                               ParameterHandling = ParameterHandling,
                               ConsumerKey = ConsumerKey,
                               Token = Token,
                               SignatureMethod = SignatureMethod.ToRequestValue(),
                               SignatureTreatment = SignatureTreatment,
                               Signature = signature,
                               Timestamp = timestamp,
                               Nonce = nonce,
                               Version = Version,
                               Verifier = Verifier,
                               Callback = CallbackUrl,
                               UserAgent = "Hammock",
                               TokenSecret = TokenSecret,
                               ConsumerSecret = ConsumerSecret,
                           };

            return info;
        }

        /// <summary>
        /// Generates a <see cref="OAuthWebQueryInfo"/> instance to pass to an
        /// <see cref="OAuthWebQuery" /> for the purpose of exchanging user credentials
        /// for an access token authorized by the user at the Service Provider site.
        /// </summary>
        /// <param name="method">The HTTP method for the intended request</param>
        /// <seealso cref="http://tools.ietf.org/html/draft-dehora-farrell-oauth-accesstoken-creds-00#section-4"/>
        /// <param name="parameters">Any existing, non-OAuth query parameters desired in the request</param>
        public OAuthWebQueryInfo BuildClientAuthAccessTokenInfo(WebMethod method, WebParameterCollection parameters)
        {
            ValidateClientAuthAccessRequestState();

            if (parameters == null)
            {
                parameters = new WebParameterCollection();
            }

            var uri = new Uri(AccessTokenUrl);
            var timestamp = OAuthTools.GetTimestamp();
            var nonce = OAuthTools.GetNonce();

            AddXAuthParameters(parameters, timestamp, nonce);

            var signatureBase = OAuthTools.ConcatenateRequestElements(method, uri.ToString(), parameters);
            var signature = OAuthTools.GetSignature(SignatureMethod, SignatureTreatment, signatureBase, ConsumerSecret);

            var info = new OAuthWebQueryInfo
                           {
                               WebMethod = method,
                               ParameterHandling = ParameterHandling,
                               ClientMode = "client_auth",
                               ClientUsername = ClientUsername,
                               ClientPassword = ClientPassword,
                               ConsumerKey = ConsumerKey,
                               SignatureMethod = SignatureMethod.ToRequestValue(),
                               SignatureTreatment = SignatureTreatment,
                               Signature = signature,
                               Timestamp = timestamp,
                               Nonce = nonce,
                               Version = Version,
                               UserAgent = "Hammock",
                               TokenSecret = TokenSecret,
                               ConsumerSecret = ConsumerSecret
                           };

            return info;
        }

        public OAuthWebQueryInfo BuildProtectedResourceInfo(WebMethod method, 
                                                            WebParameterCollection parameters,
                                                            string url)
        {
            ValidateProtectedResourceState();

            if (parameters == null)
            {
                parameters = new WebParameterCollection();
            }

            // Include url parameters in query pool
            var uri = new Uri(url);
#if !SILVERLIGHT
            var urlParameters = HttpUtility.ParseQueryString(uri.Query);
#else
            var urlParameters = uri.Query.ParseQueryString();
#endif

#if !SILVERLIGHT
            foreach (var parameter in urlParameters.AllKeys)
#else
            foreach (var parameter in urlParameters.Keys)
#endif
            {
                switch (method)
                {
                    case WebMethod.Post:
                        parameters.Add(new HttpPostParameter(parameter, urlParameters[parameter]));
                        break;
                    default:
                        parameters.Add(parameter, urlParameters[parameter]);
                        break;
                }
            }

            var timestamp = OAuthTools.GetTimestamp();
            var nonce = OAuthTools.GetNonce();

            // [DC] Make a copy of the parameters so that the signature double-encode isn't used
            var copy = new WebParameterCollection();
            foreach(var parameter in parameters)
            {
                copy.Add(new WebPair(parameter.Name, parameter.Value));
            }

            AddAuthParameters(copy, timestamp, nonce);

            // [DC] Escape parameters at this point; do not escape again if recalculating
            var signatureBase = OAuthTools.ConcatenateRequestElements(method, url, copy);
            var signature = OAuthTools.GetSignature(SignatureMethod, SignatureTreatment, signatureBase, ConsumerSecret, TokenSecret);

            var info = new OAuthWebQueryInfo
                           {
                               WebMethod = method,
                               ParameterHandling = ParameterHandling,
                               ConsumerKey = ConsumerKey,
                               Token = Token,
                               SignatureMethod = SignatureMethod.ToRequestValue(),
                               SignatureTreatment = SignatureTreatment,
                               Signature = signature,
                               Timestamp = timestamp,
                               Nonce = nonce,
                               Version = Version ?? "1.0",
                               UserAgent = "Hammock",
                               ConsumerSecret = ConsumerSecret,
                               TokenSecret = TokenSecret
                           };
            
            return info;
        }

        private void ValidateTokenRequestState()
        {
            if (RequestTokenUrl.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify a request token URL");
            }

            if (ConsumerKey.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify a consumer key");
            }

            if (ConsumerSecret.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify a consumer secret");
            }
        }

        private void ValidateAccessRequestState()
        {
            if (AccessTokenUrl.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify an access token URL");
            }

            if (ConsumerKey.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify a consumer key");
            }

            if (ConsumerSecret.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify a consumer secret");
            }

            if (Token.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify a token");
            }
        }

        private void ValidateClientAuthAccessRequestState()
        {
            if (AccessTokenUrl.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify an access token URL");
            }

            if (ConsumerKey.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify a consumer key");
            }

            if (ConsumerSecret.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify a consumer secret");
            }

            if (ClientUsername.IsNullOrBlank() || ClientPassword.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify user credentials");
            }
        }

        private void ValidateProtectedResourceState()
        {
            if (ConsumerKey.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify a consumer key");
            }

            if (ConsumerSecret.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify a consumer secret");
            }

            if (Token.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify a token");
            }

            if (TokenSecret.IsNullOrBlank())
            {
                throw new ArgumentException("You must specify a token secret");
            }
        }

        private void AddAuthParameters(ICollection<WebPair> parameters, string timestamp, string nonce)
        {
            var authParameters = new WebParameterCollection
                                     {
                                         new WebPair("oauth_consumer_key", ConsumerKey),
                                         new WebPair("oauth_nonce", nonce),
                                         new WebPair("oauth_signature_method", SignatureMethod.ToRequestValue()),
                                         new WebPair("oauth_timestamp", timestamp),
                                         new WebPair("oauth_version", Version ?? "1.0")
                                     };

            if (!Token.IsNullOrBlank())
            {
                authParameters.Add(new WebPair("oauth_token", Token));
            }

            if (!CallbackUrl.IsNullOrBlank())
            {
                authParameters.Add(new WebPair("oauth_callback", CallbackUrl));
            }

            if (!Verifier.IsNullOrBlank())
            {
                authParameters.Add(new WebPair("oauth_verifier", Verifier));
            }

            foreach (var authParameter in authParameters)
            {
                parameters.Add(authParameter);
            }
        }

        private void AddXAuthParameters(ICollection<WebPair> parameters, string timestamp, string nonce)
        {
            var authParameters = new WebParameterCollection
                                     {
                                         new WebPair("x_auth_username", ClientUsername),
                                         new WebPair("x_auth_password", ClientPassword),
                                         new WebPair("x_auth_mode", "client_auth"),
                                         new WebPair("oauth_consumer_key", ConsumerKey),
                                         new WebPair("oauth_signature_method", SignatureMethod.ToRequestValue()),
                                         new WebPair("oauth_timestamp", timestamp),
                                         new WebPair("oauth_nonce", nonce),
                                         new WebPair("oauth_version", Version ?? "1.0")
                                     };

            foreach (var authParameter in authParameters)
            {
                parameters.Add(authParameter);
            }
        }
    }
}