using ECommerce517.Models;
using ECommerce517.Utility;
using ECommerce517.ViewModels;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Threading.Tasks;

namespace ECommerce517.Areas.Identity.Controllers
{
    [Area("Identity")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IRepository<UserOTP> _userOTP;

        public AccountController(UserManager<ApplicationUser> userManager, IEmailSender emailSender, SignInManager<ApplicationUser> signInManager, IRepository<UserOTP> userOTP)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _signInManager = signInManager;
            _userOTP = userOTP;
        }

        [HttpGet]
        [UserAuthenticatedFilter]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            if(!ModelState.IsValid)
            {
                return View(registerVM);
            }

            ApplicationUser applicationUser = new()
            {
                Name = registerVM.Name,
                Email = registerVM.Email,
                City = registerVM.City,
                Street = registerVM.Street,
                State = registerVM.State,
                ZipCode = registerVM.ZipCode,
                UserName = registerVM.UserName
            };

            //ApplicationUser applicationUser = registerVM.Adapt<ApplicationUser>();

            var result = await _userManager.CreateAsync(applicationUser, registerVM.Password);

            if(!result.Succeeded)
            {
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, item.Description);
                }

                return View(registerVM);
            }

            // Add user to customer role
            await _userManager.AddToRoleAsync(applicationUser, SD.CustomerRole);

            // Send confirmation msg
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(applicationUser);
            var link = Url.Action("ConfirmEmail", "Account", new { area = "Identity", token = token, userId = applicationUser.Id }, Request.Scheme);

            await _emailSender.SendEmailAsync(applicationUser.Email, $"Confirm Your Email!", $"<h1>Confirm Your Email By Clicking <a href='{link}'>Here</a></h1>");

            TempData["success-notification"] = "Create User successfully, Confirm Your Email!";
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        public async Task<IActionResult> ConfirmEmail(string token, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
                TempData["error-notification"] = "Link Expired!, Resend Email Confirmation";
            else
                TempData["success-notification"] = "Confirm Email successfully";
            
            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }

        [HttpGet]
        [UserAuthenticatedFilter]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            if (!ModelState.IsValid)
            {
                return View(loginVM);
            }

            var user = await _userManager.FindByEmailAsync(loginVM.EmailOrUserName) ?? await _userManager.FindByNameAsync(loginVM.EmailOrUserName);

            if (user is null)
            {
                TempData["error-notification"] = "Invalid User name Or password";
                return View(loginVM);
            }

            //_userManager.CheckPasswordAsync();
            var result = await _signInManager.PasswordSignInAsync(user, loginVM.Password, loginVM.RememberMe, true);

            if(!result.Succeeded)
            {
                if(result.IsLockedOut)
                    //ModelState.AddModelError(string.Empty, "Too many attempts");
                    TempData["error-notification"] = "Too many attempts";

                TempData["error-notification"] = "Invalid User name Or password";
                return View(loginVM);
            }

            if(!user.EmailConfirmed)
            {
                TempData["error-notification"] = "Confirm Your Email First!";
                return View(loginVM);
            }

            if (!user.LockoutEnabled)
            {
                TempData["error-notification"] = $"You have a block till {user.LockoutEnd}";
                return View(loginVM);
            }

            TempData["success-notification"] = "Login successfully";
            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }

        [HttpGet]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationVM resendEmailConfirmationVM)
        {
            if (!ModelState.IsValid)
            {
                return View(resendEmailConfirmationVM);
            }

            var user = await _userManager.FindByEmailAsync(resendEmailConfirmationVM.EmailOrUserName) ?? await _userManager.FindByNameAsync(resendEmailConfirmationVM.EmailOrUserName);

            if (user is null)
            {
                TempData["error-notification"] = "Invalid User name Or Email";
                return View(resendEmailConfirmationVM);
            }

            if (user.EmailConfirmed)
            {
                TempData["error-notification"] = "Already Confirmed!";
                return View(resendEmailConfirmationVM);
            }

            // Send confirmation msg
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action("ConfirmEmail", "Account", new { area = "Identity", token = token, userId = user.Id }, Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email!, $"Confirm Your Email!", $"<h1>Confirm Your Email By Clicking <a href='{link}'>Here</a></h1>");

            TempData["success-notification"] = "Send Email successfully, Confirm Your Email!";
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        [HttpGet]
        public IActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordVM forgetPasswordVM)
        {
            if (!ModelState.IsValid)
            {
                return View(forgetPasswordVM);
            }

            var user = await _userManager.FindByEmailAsync(forgetPasswordVM.EmailOrUserName) ?? await _userManager.FindByNameAsync(forgetPasswordVM.EmailOrUserName);

            if (user is null)
            {
                TempData["error-notification"] = "Invalid User name Or Email";
                return View(forgetPasswordVM);
            }

            // Send confirmation msg
            var OTPNumber = new Random().Next(1000, 9999);
            var link = Url.Action("ResetPassword", "Account", new { area = "Identity", userId = user.Id }, Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email!, $"Reset Password!", $"<h1>Reset Password Using {OTPNumber}. Don't share it!</h1>");

            await _userOTP.CreateAsync(new()
            {
                ApplicationUserId = user.Id,
                OTPNumber = OTPNumber.ToString(),
                ValidTo = DateTime.UtcNow.AddDays(1)
            });
            await _userOTP.CommitAsync();

            TempData["success-notification"] = "Send OTP Number to Your Email successfully";
            return RedirectToAction("ResetPassword", "Account", new { area = "Identity", UserId = user.Id });
        }

        [HttpGet]
        public IActionResult ResetPassword(string UserId)
        {
            return View(new ResetPasswordVM()
            {
                ApplicationUserId = UserId
            });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM resetPasswordVM)
        {
            if (!ModelState.IsValid)
            {
                return View(resetPasswordVM);
            }

            var user = await _userManager.FindByIdAsync(resetPasswordVM.ApplicationUserId);

            if (user is null)
            {
                TempData["error-notification"] = "Invalid User name Or Email";
                return View(resetPasswordVM);
            }

            var userOTP = (await _userOTP.GetAsync(e => e.ApplicationUserId == resetPasswordVM.ApplicationUserId)).OrderBy(e=>e.Id).LastOrDefault();

            if (userOTP is null)
                return NotFound();

            if(userOTP.OTPNumber != resetPasswordVM.OTPNumber)
            {
                TempData["error-notification"] = "Invalid OTP";
                return View(resetPasswordVM);
            }

            if (DateTime.UtcNow > userOTP.ValidTo)
            {
                TempData["error-notification"] = "Expired OTP";
                return View(resetPasswordVM);
            }

            TempData["success-notification"] = "Success OTP";
            return RedirectToAction("NewPassword", "Account", new { area = "Identity", UserId = user.Id });
        }

        [HttpGet]
        public IActionResult NewPassword(string UserId)
        {
            return View(new NewPasswordVM()
            {
                ApplicationUserId = UserId
            });
        }

        [HttpPost]
        public async Task<IActionResult> NewPassword(NewPasswordVM newPasswordVM)
        {
            if (!ModelState.IsValid)
            {
                return View(newPasswordVM);
            }

            var user = await _userManager.FindByIdAsync(newPasswordVM.ApplicationUserId);

            if (user is null)
            {
                TempData["error-notification"] = "Invalid User name Or Email";
                return View(newPasswordVM);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, newPasswordVM.Password);

            TempData["success-notification"] = "Change Password Successfully!";
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }
    }
}
