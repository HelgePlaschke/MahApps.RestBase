using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using Hammock.Caching;
using Hammock.Extensions;
using Hammock.Web;
#if !Silverlight
using System.Web;
#endif

#if SILVERLIGHT
using Hammock.Silverlight.Compat;
#endif

namespace Hammock.Authentication.OAuth
{
#if !SILVERLIGHT
    [Serializable]
#endif
    public class OAuthWebQuery : WebQuery
    {
        public virtual string Realm { get; set; }
        public virtual OAuthParameterHandling ParameterHandling { get; private set; }

        public OAuthWebQuery(OAuthWebQueryInfo info)
            : base(info)
        {
            Initialize(info);
        }

        private void Initialize(OAuthWebQueryInfo info)
        {
            Method = info.WebMethod;
            ParameterHandling = info.ParameterHandling;
        }

        protected override WebRequest BuildPostOrPutWebRequest(PostOrPut method, string url, out byte[] content)
        {
            Uri uri;

            // [DC]: Prior to this call, there should be no parameter encoding
            url = AppendParameters(url, true, true); // Uses OAuthTools
            url = PreProcessPostParameters(url, out uri);

            var request = WebRequest.Create(url);
            AuthenticateRequest(request);
#if SILVERLIGHT
            var httpMethod = method == PostOrPut.Post ? "POST" : "PUT";;
            if (HasElevatedPermissions)
            {
                request.Method = httpMethod;
            }
            else
            {
                request.Method = "POST";
                request.Headers[SilverlightMethodHeader] = httpMethod;
            }
#else
            request.Method = method == PostOrPut.Post ? "POST" : "PUT";
#endif
            request.ContentType = "application/x-www-form-urlencoded";

#if TRACE
            Trace.WriteLine(String.Concat(
                "REQUEST: ", method.ToUpper(), " ", request.RequestUri)
                );
#endif
            // [DC] LSP violation necessary for "pure" mocks
            if (request is HttpWebRequest)
            {
                SetRequestMeta((HttpWebRequest)request);
            }
            else
            {
                AppendHeaders(request);
                if (!UserAgent.IsNullOrBlank())
                {
#if SILVERLIGHT
                    // [DC] User-Agent is still restricted in elevated mode
                    request.Headers[SilverlightUserAgentHeader] = UserAgent;
#else
                    request.Headers["User-Agent"] = UserAgent;
#endif
                }
            }

            content = PostProcessPostParameters(request, uri);
#if TRACE
            Trace.WriteLine(String.Concat(
                "\r\n", Encoding.UTF8.GetString(content, 0, content.Length))
                );
#endif

#if !SILVERLIGHT
            // [DC]: Silverlight sets this dynamically
            request.ContentLength = content.Length;
#endif
            return request;
        }

        protected override WebRequest BuildGetDeleteHeadOptionsWebRequest(GetDeleteHeadOptions method, string url)
        {
            Uri uri;

            // [DC]: Prior to this call, there should be no parameter encoding
            url = PreProcessPostParameters(url, out uri);
            url = AppendParameters(url, true, true); // Uses OAuthTools
            
            switch (ParameterHandling)
            {
                case OAuthParameterHandling.HttpAuthorizationHeader:
                    // [DC]: Handled in authentication
                    break;
                case OAuthParameterHandling.UrlOrPostParameters:
                    uri = GetAddressWithOAuthParameters(new Uri(url));
                    break;
            }

            var request = WebRequest.Create(url);
#if SILVERLIGHT
            var httpMethod = method.ToUpper();
            if (HasElevatedPermissions)
            {
                request.Method = httpMethod;
            }
            else
            {
                request.Method = "POST";
                request.Headers[SilverlightMethodHeader] = httpMethod;
            }
#else
            request.Method = method.ToUpper();
#endif
            AuthenticateRequest(request);
#if TRACE
            Trace.WriteLine(String.Concat(
                "REQUEST: ", request.Method, " ", request.RequestUri)
                );
#endif
            // [DC] LSP violation necessary for "pure" mocks
            if (request is HttpWebRequest)
            {
                SetRequestMeta((HttpWebRequest)request);
            }
            else
            {
                AppendHeaders(request);
                if (!UserAgent.IsNullOrBlank())
                {
#if SILVERLIGHT
                    // [DC] User-Agent is still restricted in elevated mode
                    request.Headers[SilverlightUserAgentHeader] = UserAgent;
#else
                    request.Headers["User-Agent"] = UserAgent;
#endif
                }
            }

            return request;
        }

        protected override string AppendParameters(string url)
        {
            //  Base behavior
            return AppendParameters(url, true /* escape */, false /* skipOAuth */);
        }

        protected virtual string AppendParameters(string url, bool escape, bool skipOAuth)
        {
            var parameters = 0;
            foreach (var parameter in Parameters.Where(
                parameter => !(parameter is HttpPostParameter) || Method == WebMethod.Post
                             ))
            {
                if (skipOAuth && parameter.Name.StartsWith("oauth_"))
                {
                    continue;
                }

                var value = escape
                                ? OAuthTools.UrlEncodeStrict(parameter.Value)
                                : parameter.Value;

                // GET parameters in URL
                url = url.Then(parameters > 0 || url.Contains("?") ? "&" : "?");
                url = url.Then("{0}={1}".FormatWith(parameter.Name, value));
                parameters++;
            }

            return url;
        }

        private Uri GetAddressWithOAuthParameters(Uri address)
        {
            var sb = new StringBuilder("?");
            var parameters = 0;
            foreach (var parameter in Parameters)
            {
                if (parameter.Name.IsNullOrBlank() || parameter.Value.IsNullOrBlank())
                {
                    continue;
                }

                parameters++;
                var format = parameters < Parameters.Count ? "{0}={1}&" : "{0}={1}";
                sb.Append(format.FormatWith(parameter.Name, parameter.Value));
            }

            return new Uri(address + sb.ToString());
        }

        private byte[] PostProcessPostParameters(WebRequest request, Uri uri)
        {
            var body = "";
            switch (ParameterHandling)
            {
                case OAuthParameterHandling.HttpAuthorizationHeader:
                    SetAuthorizationHeader(request, "Authorization");

                    // Only use the POST parameters that exist in the body
#if SILVERLIGHT
                    var postParameters = new WebParameterCollection(uri.Query.ParseQueryString());
#else
                    var postParameters = new WebParameterCollection(uri.Query.ParseQueryString());
#endif
                    // Append any leftover values to the POST body
                    var nonAuthParameters = GetPostParametersValue(postParameters, true /* escapeParameters */);
                    if (body.IsNullOrBlank())
                    {
                        body = nonAuthParameters;
                    }
                    else
                    {
                        if (!nonAuthParameters.IsNullOrBlank())
                        {
                            body += "&".Then(nonAuthParameters);
                        }
                    }
                    break;
                case OAuthParameterHandling.UrlOrPostParameters:
                    body = GetPostParametersValue(Parameters, false /* escapeParameters */);
                    break;
            }

            var content = Encoding.UTF8.GetBytes(body);
            return content;
        }

        private static string PreProcessPostParameters(string url, out Uri uri)
        {
            // Remove POST parameters from query
            uri = url.AsUri();
            url = uri.Scheme.Then("://")
#if !SILVERLIGHT
                .Then(uri.Authority);
#else
                .Then(uri.Host);
                if ((uri.Scheme.Equals("http") && uri.Port != 80) ||
                    (uri.Scheme.Equals("https") && uri.Port != 443))
                {
                    url = url.Then(":" + uri.Port);
                }
#endif
            url = url.Then(uri.AbsolutePath);
            return url;

        }
        private static string GetPostParametersValue(ICollection<WebPair> postParameters, bool escapeParameters)
        {
            var body = "";
            var count = 0;
            var parameters = postParameters.Where(p => !p.Name.IsNullOrBlank() &&
                                                       !p.Value.IsNullOrBlank()).ToList();

            foreach (var postParameter in parameters)
            {
                // [DC]: client_auth method does not function when these are escaped
                var name = escapeParameters
                               ? OAuthTools.UrlEncodeStrict(postParameter.Name)
                               : postParameter.Name;
                var value = escapeParameters
                                ? OAuthTools.UrlEncodeStrict(postParameter.Value)
                                : postParameter.Value;

                var token = "{0}={1}".FormatWith(name, value);
                body = body.Then(token);
                count++;
                if (count < postParameters.Count)
                {
                    body = body.Then("&");
                }
            }
            return body;
        }

        protected override void AuthenticateRequest(WebRequest request)
        {
            switch(ParameterHandling)
            {
                case OAuthParameterHandling.HttpAuthorizationHeader:
                    SetAuthorizationHeader(request, "Authorization");
                    break;
                case OAuthParameterHandling.UrlOrPostParameters:
                    // [DC]: Handled in builder method
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        protected override void SetAuthorizationHeader(WebRequest request, string header)
        {
            var authorization = GetAuthorizationHeader();
            AuthorizationHeader = authorization;

#if !SILVERLIGHT
            request.Headers["Authorization"] = AuthorizationHeader;
#else
            if (HasElevatedPermissions)
            {
                request.Headers[header] = AuthorizationHeader;
            }
            else
            {
                request.Headers[SilverlightAuthorizationHeader] = AuthorizationHeader;
            }
#endif
        }

        private string GetAuthorizationHeader()
        {
            var sb = new StringBuilder("OAuth ");
            if (!Realm.IsNullOrBlank())
            {
                sb.Append("realm=\"{0}\",".FormatWith(OAuthTools.UrlEncodeRelaxed(Realm)));
            }

            var parameters = 0;
            foreach (var parameter in Parameters.Where(parameter => 
                                                       !parameter.Name.IsNullOrBlank() && 
                                                       !parameter.Value.IsNullOrBlank() &&
                                                        parameter.Name.StartsWith("oauth_")
                                                       ))
            {
                parameters++;
                var format = parameters < Parameters.Count ? "{0}=\"{1}\"," : "{0}=\"{1}\"";
                sb.Append(format.FormatWith(parameter.Name, parameter.Value));
            }

            var authorization = sb.ToString();
            return authorization;
        }
       
#if !SILVERLIGHT
        public override string Request(string url, IEnumerable<HttpPostParameter> parameters, out WebException exception)
        {
            RecalculateProtectedResourceSignature(url);
            return base.Request(url, parameters, out exception);
        }

        public override string Request(string url, out WebException exception)
        {
            RecalculateProtectedResourceSignature(url);
            url = RestoreUrlParams(url, Parameters);
            return base.Request(url, out exception);
        }

        public override string Request(string url, string key, ICache cache, out WebException exception)
        {
            RecalculateProtectedResourceSignature(url);
            return base.Request(url, key, cache, out exception);
        }

        public override string Request(string url, string key, ICache cache, DateTime absoluteExpiration, out WebException exception)
        {
            RecalculateProtectedResourceSignature(url);
            return base.Request(url, key, cache, absoluteExpiration, out exception);
        }

        public override string Request(string url, string key, ICache cache, TimeSpan slidingExpiration, out WebException exception)
        {
            RecalculateProtectedResourceSignature(url);
            return base.Request(url, key, cache, slidingExpiration, out exception);
        }
#endif

#if !WindowsPhone
        public override WebQueryAsyncResult RequestAsync(string url, IEnumerable<HttpPostParameter> parameters, object userState)
        {
            RecalculateProtectedResourceSignature(url);
            return base.RequestAsync(url, parameters, userState);
        }

        public override WebQueryAsyncResult RequestAsync(string url, object userState)
        {
            RecalculateProtectedResourceSignature(url);
            url = RestoreUrlParams(url, Parameters);
            return base.RequestAsync(url, userState);
        }

        public override WebQueryAsyncResult RequestAsync(string url, string key, ICache cache, object userState)
        {
            RecalculateProtectedResourceSignature(url);
            return base.RequestAsync(url, key, cache, userState);
        }

        public override WebQueryAsyncResult RequestAsync(string url, string key, ICache cache, DateTime absoluteExpiration, object userState)
        {
            //RecalculateProtectedResourceSignature(url);
            return base.RequestAsync(url, key, cache, absoluteExpiration, userState);
        }

        public override WebQueryAsyncResult RequestAsync(string url, string key, ICache cache, TimeSpan slidingExpiration, object userState)
        {
            //RecalculateProtectedResourceSignature(url);
            return base.RequestAsync(url, key, cache, slidingExpiration, userState);
        }
#else
        public override void RequestAsync(string url, IEnumerable<HttpPostParameter> parameters, object userState)
        {
            RecalculateProtectedResourceSignature(url);
            base.RequestAsync(url, parameters, userState);
        }

        public override void RequestAsync(string url, object userState)
        {
            RecalculateProtectedResourceSignature(url);
            url = RestoreUrlParams(url, Parameters);
            base.RequestAsync(url, userState);
        }

        public override void RequestAsync(string url, string key, ICache cache, object userState)
        {
            RecalculateProtectedResourceSignature(url);
            base.RequestAsync(url, key, cache, userState);
        }

        public override void RequestAsync(string url, string key, ICache cache, DateTime absoluteExpiration, object userState)
        {
            RecalculateProtectedResourceSignature(url);
            base.RequestAsync(url, key, cache, absoluteExpiration, userState);
        }

        public override void RequestAsync(string url, string key, ICache cache, TimeSpan slidingExpiration, object userState)
        {
            RecalculateProtectedResourceSignature(url);
            base.RequestAsync(url, key, cache, slidingExpiration, userState);
        }
#endif
        
        private string RestoreUrlParams(string url, IEnumerable<WebPair> parameters)
        {
            if (Method != WebMethod.Post && Method != WebMethod.Put)
            {
                var builder = new StringBuilder();
                var first = true;
                foreach (var param in parameters.Where(p => !p.Name.ToLower().StartsWith("oauth_")))
                {
                    builder.Append(first ? "?" : "&");
                    first = false;
                    builder.Append(param.Name).Append('=').Append(param.Value);
                }
                url = url + builder;
            }
            return url;
        }

        private void RecalculateProtectedResourceSignature(string url)
        {
            var info = (OAuthWebQueryInfo) Info;
            if (info.Token.IsNullOrBlank() || info.TokenSecret.IsNullOrBlank())
            {
                // No signature values to work with
                return;
            }

            if(!info.ClientUsername.IsNullOrBlank() || !info.ClientPassword.IsNullOrBlank())
            {
                // Not a protected resource request
                return;
            }

            if(!string.IsNullOrEmpty(info.Verifier))
            {
                // This is an access token request
                return;
            }

            var oauth = new OAuthWorkflow
                            {
                                ConsumerKey = info.ConsumerKey,
                                ConsumerSecret = info.ConsumerSecret,
                                Token = info.Token,
                                TokenSecret = info.TokenSecret,
                                ClientUsername = info.ClientUsername,
                                ClientPassword = info.ClientPassword,
                                SignatureMethod = OAuthSignatureMethod.HmacSha1,
                                ParameterHandling = ParameterHandling,
                                CallbackUrl = info.Callback,
                                Verifier = info.Verifier
                            };

            // [DC]: Add any non-oauth parameters back into the signature hash
            var parameters = new WebParameterCollection();
            var nonAuthParameters = Parameters.Where(p => !p.Name.StartsWith("oauth_"));
            parameters.AddRange(nonAuthParameters);
            
            // [DC]: Don't escape parameters again when calcing the signature
            Info = oauth.BuildProtectedResourceInfo(Method, parameters, url);

            // [DC]: Add any non-oauth parameters back into parameter bag
            Parameters = ParseInfoParameters();
            Parameters.AddRange(nonAuthParameters);
        }
    }
}