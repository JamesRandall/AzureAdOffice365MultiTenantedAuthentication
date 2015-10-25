using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AzureAdOffice365Authentication.Configuration;

namespace AzureAdOffice365Authentication.Controllers
{
    public class AzureOnboardingController : Controller
    {
        private readonly AzureTenantService _service = new AzureTenantService();

        public ActionResult AdminConsent()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AdminConsent(bool proceed)
        {
            string state = _service.AddTemporaryTenant();
            string url = _service.CreateAuthorizationRequest(state);
            return new RedirectResult(url);
        }

        // ReSharper disable InconsistentNaming
        public ActionResult Associate(string code, string error, string error_description, string state)
        // ReSharper restore InconsistentNaming
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                return RedirectToAction("ErrorAssociating", new { error, errorDescription = error_description });
            }

            Uri requestUri = new Uri(Request.Url.GetLeftPart(UriPartial.Path));
            AzureTenantService.AssociationStatusEnum result = _service.Associate(state, code, requestUri);

            switch (result)
            {
                case AzureTenantService.AssociationStatusEnum.Associated:
                    return RedirectToAction("Success");
                case AzureTenantService.AssociationStatusEnum.AlreadyAssociated:
                    return RedirectToAction("AlreadyAssociated");
                case AzureTenantService.AssociationStatusEnum.TenantNotFound:
                    return RedirectToAction("ErrorAssociating", new { error = "Error associating", errorDescription = "The tenant could not be found, your session may have expired. Please log out and try again." });
            }

            return RedirectToAction("ErrorAssociating", new { error = "Error associating", errorDescription = "Unknown error occurred" });
        }

        public ActionResult Success()
        {
            return View();
        }

        public ActionResult AlreadyAssociated()
        {
            return View();
        }

        public ActionResult ErrorAssociating(string error, string errorDescription)
        {
            ViewBag.Error = error;
            ViewBag.ErrorDescription = errorDescription;
            return View();
        }
    }
}