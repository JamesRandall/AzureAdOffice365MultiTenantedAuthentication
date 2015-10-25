using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using Serilog;
using Thinktecture.IdentityServer.Core.Configuration;
using Thinktecture.IdentityServer.Core.Models;
using Thinktecture.IdentityServer.Core.Services;
using Thinktecture.IdentityServer.Core.Services.InMemory;
using AuthenticationOptions = Thinktecture.IdentityServer.Core.Configuration.AuthenticationOptions;

namespace AzureAdOffice365Authentication
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Trace()
                .CreateLogger();

            X509Certificate2 certificate = new X509Certificate2(
                $@"{AppDomain.CurrentDomain.BaseDirectory}\bin\Configuration\idsrv3test.pfx", "idsrv3test");

            List<InMemoryUser> users = new List<InMemoryUser>();
            List<Client> clients = new List<Client>
            {
                new Client
                {
                    Enabled = true,
                    ClientName = "MVC Client",
                    ClientId = "mvc",
                    Flow = Flows.Implicit,
                    EnableLocalLogin = false,
                    RequireConsent = false,

                    RedirectUris = new List<string>
                    {
                        "https://localhost:44300/"
                    }
                }
            };

            app.Map("/identity", idsrvApp =>
            {
                idsrvApp.UseIdentityServer(new IdentityServerOptions
                {
                    Factory = new IdentityServerServiceFactory
                    {
                        ClientStore = new Registration<IClientStore>(r => new InMemoryClientStore(clients)),
                        UserService = new Registration<IUserService>(r => new InMemoryUserService(users)),
                        ScopeStore = new Registration<IScopeStore>(r => new InMemoryScopeStore(StandardScopes.All))
                    },
                    SiteName = "AccidentalFish Office365 Sample",
                    SigningCertificate = certificate,
                    AuthenticationOptions = new AuthenticationOptions
                    {
                        CookieOptions = new CookieOptions
                        {
                            SlidingExpiration = true
                        },
                        EnableLocalLogin = false,
                        EnablePostSignOutAutoRedirect = true,
                        IdentityProviders = ConfigureIdentityProviders
                    }
                });
            });

            // Configure MVC to use the above token endpoint
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Cookies"
            });
            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                Authority = "https://localhost:44300/identity",
                ClientId = "mvc",
                RedirectUri = "https://localhost:44300/",
                ResponseType = "id_token",

                SignInAsAuthenticationType = "Cookies"
            });
        }

        private void ConfigureIdentityProviders(IAppBuilder app, string signInAsType)
        {
            string clientId = Constants.ClientId; // your client ID as configured in Azure
            string redirectUri = "https://localhost:44300/identity/signin-azuread"; // the reply URL as configured in Azure
            string postLogoutRedirectUri = "https://localhost:44300"; // an appropriate page for your project

            OpenIdConnectAuthenticationOptions options = new OpenIdConnectAuthenticationOptions
            {
                AuthenticationType = "AzureAd",
                Caption = "Sign in with Azure AD",
                Scope = "openid email",
                ClientId = clientId,
                Authority = "https://login.windows.net/common/",
                PostLogoutRedirectUri = postLogoutRedirectUri,
                RedirectUri = redirectUri,
                AuthenticationMode = AuthenticationMode.Passive,
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false
                },
                SignInAsAuthenticationType = signInAsType // this MUST come after TokenValidationParameters
            };
            app.UseOpenIdConnectAuthentication(options);
        }
    }
}


