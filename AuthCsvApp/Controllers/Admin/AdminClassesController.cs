
using AuthCsvApp.Models;
using AuthCsvApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthCsvApp.Controllers
{
    [Route("admin/classes")]
    public class AdminClassesController : Controller
    {
        private readonly AdminClassService _adminClassService;
        private readonly AuthenticationService _authenticationService;

        public AdminClassesController(AdminClassService adminClassService, AuthenticationService authenticationService)
        {
            _adminClassService = adminClassService;
            _authenticationService = authenticationService;
        }

        [HttpGet]
        [Route("index")]
        public IActionResult Index()
        {
            if (!_authenticationService.IsAdminAuthenticated(out string adminUsername))
            {
                return RedirectToAction("Login", "Auth");
            }

            var classes = _adminClassService.GetClasses();
            ViewBag.Subjects = _adminClassService.GetSubjects();
            ViewBag.Semesters = _adminClassService.GetSemesters();
            ViewBag.Teachers = _adminClassService.GetTeachers();
            return View("~/Views/Admin/Classes/Index.cshtml", classes);
        }

        [HttpGet]
        [Route("add")]
        public IActionResult Add()
        {
            if (!_authenticationService.IsAdminAuthenticated(out string adminUsername))
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.Subjects = _adminClassService.GetSubjects();
            ViewBag.Semesters = _adminClassService.GetSemesters();
            ViewBag.Teachers = _adminClassService.GetTeachers();
            return View("~/Views/Admin/Classes/Add.cshtml");
        }

        [HttpPost]
        [Route("add")]
        public IActionResult Add(Class model)
        {
            if (!_authenticationService.IsAdminAuthenticated(out string adminUsername))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                if (_adminClassService.AddClass(model, out string errorMessage))
                {
                    TempData["Success"] = "Class added successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", errorMessage);
                }
            }

            ViewBag.Subjects = _adminClassService.GetSubjects();
            ViewBag.Semesters = _adminClassService.GetSemesters();
            ViewBag.Teachers = _adminClassService.GetTeachers();
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

            var classToEdit = _adminClassService.GetClassById(id);
            if (classToEdit == null)
            {
                return NotFound("Class not found.");
            }

            ViewBag.Subjects = _adminClassService.GetSubjects();
            ViewBag.Semesters = _adminClassService.GetSemesters();
            ViewBag.Teachers = _adminClassService.GetTeachers();
            return View("~/Views/Admin/Classes/Edit.cshtml", classToEdit);
        }

        [HttpPost]
        [Route("edit/{id}")]
        public IActionResult Edit(int id, Class model)
        {
            if (!_authenticationService.IsAdminAuthenticated(out string adminUsername))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                if (_adminClassService.UpdateClass(id, model, out string errorMessage))
                {
                    TempData["Success"] = "Class updated successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", errorMessage);
                }
            }

            ViewBag.Subjects = _adminClassService.GetSubjects();
            ViewBag.Semesters = _adminClassService.GetSemesters();
            ViewBag.Teachers = _adminClassService.GetTeachers();
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

            if (_adminClassService.DeleteClass(id, out string errorMessage))
            {
                TempData["Success"] = "Class deleted successfully!";
            }
            else
            {
                TempData["Error"] = errorMessage;
            }

            return RedirectToAction("Index");
        }
    }
}