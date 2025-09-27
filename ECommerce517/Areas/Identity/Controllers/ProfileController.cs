using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ECommerce517.Areas.Identity.Controllers
{
    [Area(SD.IdentityArea)]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var updateUser = user.Adapt<UpdatePersonalInfoVM>();

            return View(updateUser);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInfo(UpdatePersonalInfoVM updatePersonalInfoVM)
        {
            if (!ModelState.IsValid)
            {
                return View(updatePersonalInfoVM);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            user.Name = updatePersonalInfoVM.Name;
            user.Email = updatePersonalInfoVM.Email;
            user.PhoneNumber = updatePersonalInfoVM.PhoneNumber;
            user.Street = updatePersonalInfoVM.Street;
            user.State = updatePersonalInfoVM.State;
            user.City = updatePersonalInfoVM.City;
            user.ZipCode = updatePersonalInfoVM.ZipCode;
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index), "Profile", new { area = "Identity" });
        }
    }
}
