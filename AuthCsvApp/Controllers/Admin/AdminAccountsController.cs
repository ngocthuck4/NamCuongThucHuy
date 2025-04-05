using AuthCsvApp.Models;
using AuthCsvApp.Repositories;
using AuthCsvApp.Services;
using AuthCsvApp.Models;
using AuthCsvApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthCsvApp.Controllers
{
    [Route("admin/accounts")]
    public class AdminAccountsController : Controller
    {
        private readonly AdminAccountService _adminAccountService;
        private readonly AuthenticationService _authenticationService;

        public AdminAccountsController(AdminAccountService adminAccountService, AuthenticationService authenticationService)
        {
            _adminAccountService = adminAccountService;
            _authenticationService = authenticationService;
        }

        [HttpGet]
        [Route("index")]
        public IActionResult Index(string search, string role)
        {
            if (!_authenticationService.IsAdminAuthenticated(out string adminUsername))
            {
                return RedirectToAction("Login", "Auth");
            }

            var users = _adminAccountService.GetUsers(search, role);

            ViewBag.Search = search;
            ViewBag.Role = role;

            return View("~/Views/Admin/Accounts/Index.cshtml", users);
        }

        [HttpGet]
        [Route("add")]
        public IActionResult Add()
        {
            if (!_authenticationService.IsAdminAuthenticated(out string adminUsername))
            {
                return RedirectToAction("Login", "Auth");
            }

            return View("~/Views/Admin/Accounts/Add.cshtml");
        }

        [HttpPost]
        [Route("add")]
        public IActionResult Add(User model)
        {
            if (!_authenticationService.IsAdminAuthenticated(out string adminUsername))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                if (_adminAccountService.AddUser(model, out string errorMessage))
                {
                    TempData["Success"] = "Account added successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", errorMessage);
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        [Route("edit/{id}")]
        public IActionResult Edit(int id)
        {
            if (!_authenticationService.IsAdminAuthenticated(out string adminUsername))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = _adminAccountService.GetUserById(id);
            if (user == null)
            {
                return NotFound("Account not found.");
            }

            return View("~/Views/Admin/Accounts/Edit.cshtml", user);
        }

        [HttpPost]
        [Route("edit/{id}")]
        public IActionResult Edit(int id, User model)
        {
            if (!_authenticationService.IsAdminAuthenticated(out string adminUsername))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                if (_adminAccountService.UpdateUser(id, model, out string errorMessage))
                {
                    TempData["Success"] = "Account updated successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", errorMessage);
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        [Route("delete/{id}")]
        public IActionResult Delete(int id)
        {
            if (!_authenticationService.IsAdminAuthenticated(out string adminUsername))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (_adminAccountService.DeleteUser(id, adminUsername, out string errorMessage))
            {
                TempData["Success"] = "Account deleted successfully!";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Error"] = errorMessage;
                return RedirectToAction("Index");
            }
        }
    }
}