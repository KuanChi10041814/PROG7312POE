using Microsoft.AspNetCore.Mvc;

namespace PROG7312POE.Api.Controllers
{
    public class SensorsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
