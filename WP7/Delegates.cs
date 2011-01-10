using Hammock;

namespace MahApps.RESTBase.Delegates
{
    public delegate void AccessTokenCallbackDelegate(RestRequest request, RestResponse response, Credentials credentials);

    public delegate void RequestUrlCallbackDelegate(RestRequest request, RestResponse response, string url);

    public delegate void VoidDelegate();
}
