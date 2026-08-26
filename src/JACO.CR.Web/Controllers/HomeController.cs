using Microsoft.AspNetCore.Mvc;

namespace JACO.CR.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();
}
