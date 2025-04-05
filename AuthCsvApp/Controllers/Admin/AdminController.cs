
using Microsoft.AspNetCore.Mvc;

namespace AuthCsvApp.Controllers.Admin
{
    public class AdminController :Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }

}
