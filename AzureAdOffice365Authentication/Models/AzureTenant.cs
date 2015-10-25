using System;

namespace AzureAdOffice365Authentication.Models
{
    public class AzureTenant
    {
        public Guid Id { get; set; }

        public string IssValue { get; set; }

        public string Name { get; set; }

        public bool AdminConsented { get; set; }

        public bool IsTemporaryTenant { get; set; }
    }
}