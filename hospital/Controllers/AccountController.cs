using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using hospital.Data;
using hospital.Models;

namespace hospital.Controllers;


public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public AccountController(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _context = context;
    }

    // يوجّه المستخدم للصفحة المناسبة حسب دوره
    private async Task<IActionResult> RedirectByRole(ApplicationUser? user, string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        if (user != null && await _userManager.IsInRoleAsync(user, RoleNames.Admin))
            return RedirectToAction("Index", "Admin");

        if (user != null && await _userManager.IsInRoleAsync(user, RoleNames.Doctor))
            return RedirectToAction("Index", "DoctorArea");

        if (user != null && await _userManager.IsInRoleAsync(user, RoleNames.Patient))
            return RedirectToAction("Index", "PatientArea");

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string fullName, string address, string email, string password, string roleName)
    {
        var isFirstUser = !_userManager.Users.Any();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.error = "كل الحقول مطلوبة.";
            return View();
        }

        var newUser = new ApplicationUser
        {
            FullName = fullName,
            Address = address,
            Email = email,
            UserName = email
        };

        var result = await _userManager.CreateAsync(newUser, password);

        if (result.Succeeded)
        {
            // أول مستخدم في النظام بيبقى مدير تلقائياً
            var assignedRole = isFirstUser
                ? RoleNames.Admin
                : roleName switch
                {
                    RoleNames.Admin => RoleNames.Admin,
                    RoleNames.Doctor => RoleNames.Doctor,
                    _ => RoleNames.Patient
                };

            await _userManager.AddToRoleAsync(newUser, assignedRole);

            // لو الحساب مريض — نعمل ملف مريض تلقائياً ونربطه بالحساب
            if (assignedRole == RoleNames.Patient)
            {
                var patientFile = new Patient
                {
                    Name = string.IsNullOrWhiteSpace(fullName) ? email : fullName,
                    Phone = "غير محدد",
                    Age = 1,
                    Gender = "غير محدد"
                };

                _context.Patients.Add(patientFile);
                await _context.SaveChangesAsync();

                newUser.PatientId = patientFile.Id;
                await _userManager.UpdateAsync(newUser);
            }

            await _signInManager.SignInAsync(newUser, isPersistent: false);

            return await RedirectByRole(newUser, null);
        }

        ViewBag.error = string.Join(" | ", result.Errors.Select(e => e.Description));
        return View();
    }

    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewBag.returnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl)
    {
        var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return await RedirectByRole(user, returnUrl);
        }

        ViewBag.error = "البريد الإلكتروني أو كلمة المرور غير صحيحة.";
        ViewBag.returnUrl = returnUrl;
        return View();
    }

    [Authorize]
    public IActionResult Logout()
    {
        return View();
    }

    [HttpPost]
    [ActionName("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutConfirmed()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    // إنشاء الأدوار الأساسية - للمدير فقط
    [Authorize(Roles = RoleNames.Admin)]
    public IActionResult CreateRoles()
    {
        if (!_roleManager.RoleExistsAsync(RoleNames.Admin).Result)
            _roleManager.CreateAsync(new IdentityRole { Name = RoleNames.Admin }).Wait();

        if (!_roleManager.RoleExistsAsync(RoleNames.Doctor).Result)
            _roleManager.CreateAsync(new IdentityRole { Name = RoleNames.Doctor }).Wait();

        if (!_roleManager.RoleExistsAsync(RoleNames.Patient).Result)
            _roleManager.CreateAsync(new IdentityRole { Name = RoleNames.Patient }).Wait();

        return Content("Roles ready: Admin, Doctor, Patient");
    }
}
