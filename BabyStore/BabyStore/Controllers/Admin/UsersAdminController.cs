using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using BabyStore.Models;
using BabyStore.ViewModel.Security;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;

namespace BabyStore.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class UsersAdminController : Controller
    {
        #region Security properties
        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            set
            {
                _userManager = value;
            }
        }

        private ApplicationRoleManager _roleManager;
        public ApplicationRoleManager RoleManager
        {
            get
            {
                return _roleManager ?? HttpContext.GetOwinContext().Get<ApplicationRoleManager>();
            }
            private set
            {
                _roleManager = value;
            }
        }
        #endregion

        public UsersAdminController()
        {
        }

        public UsersAdminController(ApplicationUserManager userManager, ApplicationRoleManager roleManager)
        {
            UserManager = userManager;
            RoleManager = roleManager;
        }



        // GET: UsersAdmin
        public async Task<ActionResult> Index()
        {
            return View(await UserManager.Users.ToListAsync());
        }

        // GET: UsersAdmin/Details/5
        public async Task<ActionResult> Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            ViewBag.Roles = await UserManager.GetRolesAsync(user.Id);
            
            return View(user);
        }

        // GET: UsersAdmin/Create
        public async Task<ActionResult> Create()
        {
            ViewBag.RoleId = new SelectList(await RoleManager.Roles.ToListAsync(), "Name", "Name");
            return View();
        }

        // POST: UsersAdmin/Create
        [HttpPost]
        public async Task<ActionResult> Create(RegisterViewModel viewModel, params string[] selectedRoles)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RoleId = new SelectList(await RoleManager.Roles.ToListAsync(), "Name", "Name");
                return View();
            }

            var user = new ApplicationUser()
            {
                UserName = viewModel.Email,
                Email = viewModel.Email,
                FirstName = viewModel.FirstName,
                LastName = viewModel.LastName,
                DateOfBirth = viewModel.DateOfBirth,
                Address = viewModel.Address
            };

            var createResult = await UserManager.CreateAsync(user, viewModel.Password);

            if (!createResult.Succeeded)
            {
                ModelState.AddModelError("", createResult.Errors.First());
                ViewBag.RoleId = new SelectList(RoleManager.Roles, "Name", "Name");
                return View();
            }

            if (selectedRoles != null)
            {
                var result = await UserManager.AddToRolesAsync(user.Id, selectedRoles);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", createResult.Errors.First());
                    ViewBag.RoleId = new SelectList(await RoleManager.Roles.ToListAsync(), "Name", "Name");
                    return View();
                }
            }
            return RedirectToAction("Index");
        }

        // GET: UsersAdmin/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var user = await UserManager.FindByIdAsync(id);

            if (user == null)
            {
                return HttpNotFound();
            }
            var userRoles = await UserManager.GetRolesAsync(id);
            var allRoles = RoleManager.Roles.ToList();
            EditUserViewModel viewModel = new EditUserViewModel()
            {
                FirstName = user.FirstName,
                LastName =  user.LastName,
                Email = user.Email,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth,
                RolesList = allRoles.Select(x => new SelectListItem()
                {
                    Selected = userRoles.Contains(x.Name),
                    Text = x.Name,
                    Value = x.Name
                })
            };
            
            return View(viewModel);
        }

        // POST: UsersAdmin/Edit/5
        [HttpPost]
        public async Task<ActionResult> Edit(EditUserViewModel viewModel, params string[] selectedRole)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByIdAsync(viewModel.Id);
                if (user == null)
                {
                    return HttpNotFound();
                }

                user.FirstName = viewModel.FirstName;
                user.LastName = viewModel.LastName;
                user.DateOfBirth = viewModel.DateOfBirth;
                user.Address = viewModel.Address;
                
                var userRoles = await UserManager.GetRolesAsync(viewModel.Id);
               
                var addResult = await UserManager.AddToRolesAsync(user.Id, selectedRole.Except(userRoles).ToArray());
                if (!addResult.Succeeded)
                {
                    ModelState.AddModelError("", addResult.Errors.First());
                    return View();
                }
                
                var removeResult = await UserManager.RemoveFromRolesAsync(user.Id, userRoles.Except(selectedRole).ToArray());
                if (!removeResult.Succeeded)
                {
                    ModelState.AddModelError("", removeResult.Errors.First());
                    return View();
                }

                return RedirectToAction("Index");
            }
           
            ModelState.AddModelError("", "Something failed.");
            return View();
        }
    }
}
