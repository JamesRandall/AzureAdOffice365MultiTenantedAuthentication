using System;
using System.Collections.Generic;
using System.Linq;
using AzureAdOffice365Authentication.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AzureAdOffice365Authentication.Configuration
{
    public class AzureTenantService
    {
        private readonly static List<AzureTenant> Tenants = new List<AzureTenant>();
        // Get these from somewhere secure
        

        public enum AssociationStatusEnum
        {
            AlreadyAssociated,
            Associated,
            TenantNotFound
        }

        public string AddTemporaryTenant()
        {
            AzureTenant tenant = new AzureTenant
            {
                Id = Guid.NewGuid(),
                AdminConsented = false,
                IssValue = Guid.NewGuid().ToString(),
                Name = "Temporary Tenant",
                IsTemporaryTenant = true
            };
            Tenants.Add(tenant);
            return tenant.IssValue;
        }

        private AssociationStatusEnum ConvertTemporaryTenantToPermanentTenant(string temporaryIssValue, string tenantId)
        {
            string issValue = $"https://sts.windows.net/{tenantId}/";
            if (Tenants.Any(x => x.IssValue == issValue))
            {
                return AssociationStatusEnum.AlreadyAssociated;
            }

            AzureTenant tenant = Tenants.SingleOrDefault(x => x.IssValue == temporaryIssValue && x.IsTemporaryTenant);
            if (tenant == null)
            {
                return AssociationStatusEnum.TenantNotFound;
            }

            tenant.IssValue = issValue;
            tenant.AdminConsented = true;
            tenant.IsTemporaryTenant = false;

            return AssociationStatusEnum.Associated;
        }

        public string CreateAuthorizationRequest(string state)
        {
            string encodedClientId = Uri.EscapeDataString(Constants.ClientId);
            string encodedResource = Uri.EscapeDataString("https://graph.windows.net");
            string encodedRedirectUri = Uri.EscapeDataString($"https://localhost:44300/AzureOnboarding/Associate");
            string encodedState = Uri.EscapeDataString(state);
            string encodedAdminConsent = Uri.EscapeDataString("admin_consent");
            string request =
                $"https://login.windows.net/common/oauth2/authorize?response_type=code&client_id={encodedClientId}&resource={encodedResource}&redirect_uri={encodedRedirectUri}&state={encodedState}&prompt={encodedAdminConsent}";

            return request;
        }

        public AssociationStatusEnum Associate(string state, string code, Uri requestUri)
        {
            ClientCredential clientCredential = new ClientCredential(Constants.ClientId, Constants.ClientKey);
            AuthenticationContext context = new AuthenticationContext("https://login.windows.net/common/");
            AuthenticationResult result = context.AcquireTokenByAuthorizationCode(code, requestUri, clientCredential);

            return ConvertTemporaryTenantToPermanentTenant(state, result.TenantId);
        }
    }
}