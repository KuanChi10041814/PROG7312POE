using Microsoft.AspNetCore.Mvc;

namespace PROG7312POE.Api.Data
{
    public class SmartXDbContext : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
