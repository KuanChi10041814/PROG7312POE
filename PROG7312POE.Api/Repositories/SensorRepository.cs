using Microsoft.AspNetCore.Mvc;

namespace PROG7312POE.Api.Repositories
{
    public class SensorRepository : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
