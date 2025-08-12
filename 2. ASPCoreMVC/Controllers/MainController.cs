using Microsoft.AspNetCore.Mvc;

namespace ASPCoreMVC.Controllers
{
    public class MainController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
