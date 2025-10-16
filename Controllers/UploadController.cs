//Controllers/UploadController.cs
using Microsoft.AspNetCore.Mvc;

namespace MyEcologicCrowsourcingApp.Controllers
{
    public class UploadController : Controller
    {
        [HttpGet]
        public IActionResult Welcome()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
