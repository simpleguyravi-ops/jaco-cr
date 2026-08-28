using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JACO.CR.Web.Controllers;

// The dashboard and the change request list are one consolidated page now
// (ChangeRequestController.Index) -- this just keeps "/" working as the landing route.
[Authorize]
public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "ChangeRequest");

    [AllowAnonymous]
    public IActionResult Error() => View();
}
