using System;
using System.Collections.Generic;
using Hammock;
using Hammock.Web;
using MahApps.RESTBase.Delegates;

namespace MahApps.RESTBase
{
    public interface IRestClientBase
    {
        void BeginGetRequestUrl(RequestUrlCallbackDelegate callback);
        void BeginGetAccessToken(Uri verifierUri, AccessTokenCallbackDelegate callback);
        void BeginGetAccessToken(string verifier, AccessTokenCallbackDelegate callback);
        void BeginRequest(string path, RestCallback callback);
        void BeginRequest(string path, IDictionary<string, string> parameters,  WebMethod method, RestCallback callback);
        void BeginRequest(string path, IDictionary<string, string> parameters, IDictionary<string, File> files, WebMethod method, RestCallback callback);

        void EndGetAccessToken(RestRequest request, RestResponse response, object userState,AccessTokenCallbackDelegate callback);
    }
}