using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Security;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Linq;

namespace DiScribe.Dialer.GraphAuthentication
{
    internal class UserPassAuthProvider : IAuthenticationProvider
    {
        private IPublicClientApplication _msalClient;
        private string[] _scopes;
        private IAccount _userAccount;
        private string _username;
        private SecureString _password;
        AuthenticationResult result;
        

        public UserPassAuthProvider(string appId, string user, SecureString pass, string[] scopes,string tenantID)
        {
            _scopes = scopes;
            _username = user;
            _password = pass;

            _msalClient = PublicClientApplicationBuilder
                .Create(appId)
                .WithRedirectUri("http://localhost")
                // Set the tenant ID to "organizations" to disable personal accounts
                // Azure OAuth does not support device code flow for personal accounts
                // See https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-device-code
                .WithTenantId(tenantID)
                .Build();

        }

        public async Task<string> GetAccessToken()
        {
            _userAccount = await _msalClient.GetAccountAsync(_username);          
    
            // If there is no saved user account, the user must sign-in
            if (_userAccount == null)
            {
                try
                {
                    // Invoke device code flow so user can sign-in with a browser
                    var result = await _msalClient.AcquireTokenByUsernamePassword(_scopes, _username, _password)
                        .ExecuteAsync();

                    _userAccount = result.Account;
                    return result.AccessToken;
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Error getting access token: {exception.Message}");
                    return result.AccessToken;
                }
            }
            else
            {
                // If there is an account, call AcquireTokenSilent
                // By doing this, MSAL will refresh the token automatically if
                // it is expired. Otherwise it returns the cached token.

                var result = await _msalClient
                    .AcquireTokenSilent(_scopes, _userAccount)
                    .ExecuteAsync();

                return result.AccessToken;
            }
        }
        
        // This is the required function to implement IAuthenticationProvider
        // The Graph SDK will call this function each time it makes a Graph
        // call.
        public async Task AuthenticateRequestAsync(HttpRequestMessage requestMessage)
        {
            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("bearer", await GetAccessToken());
        }
    }
}
