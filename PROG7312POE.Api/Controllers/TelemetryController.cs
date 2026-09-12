using Microsoft.AspNetCore.Mvc;

namespace PROG7312POE.Api.Controllers
{
    public class TelemetryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
