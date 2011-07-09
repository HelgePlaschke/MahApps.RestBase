using System;
using Hammock;
using Hammock.Authentication.OAuth;

using MahApps.RESTBase;
using MahApps.RESTBase.Delegates;
using NSubstitute;
using NUnit.Framework;

namespace MahApps.RestBase.Tests
{
    [TestFixture]
    public class RestClientBaseTests
    {
        [Test]
        [ExpectedException(typeof(NullReferenceException))]
        public void BeginGetAccessToken_WithNoCredentialsSet_ThrowsException()
        {
            var mock = Substitute.For<IRestClient>();
            var client = new RestClientBase(mock);

            // client.Credentials = new OAuthCredentials() { Token = "ABC", TokenSecret = "DEF " };

            bool isCallbackFired = false;
            AccessTokenCallbackDelegate callback = (req, resp, o) => { isCallbackFired = true; };
            client.BeginGetAccessToken(@"http://google.com/foo", callback);

            Assert.Fail();
        }

        [Test]
        public void BeginGetAccessToken_WithCredentialsSet_ThrowsException()
        {
            const string verifier = @"http://google.com/foo";

            var mock = Substitute.For<IRestClient>();
            var client = new RestClientBase(mock);

            client.Credentials = new OAuthCredentials() { Token = "ABC", TokenSecret = "DEF " };

            bool isCallbackFired = false;
            AccessTokenCallbackDelegate callback = (req, resp, o) => { isCallbackFired = true; };
            client.BeginGetAccessToken(verifier, callback);

            var modifiedCredentials = client.Credentials as OAuthCredentials;

            Assert.That(modifiedCredentials.Type, Is.EqualTo(OAuthType.AccessToken));
            Assert.That(modifiedCredentials.Verifier, Is.EqualTo(verifier));
        }
    }
}
