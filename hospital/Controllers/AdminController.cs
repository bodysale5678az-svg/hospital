using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using hospital.Data;
using hospital.Models;

namespace hospital.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .Include(u => u.Doctor)
            .Include(u => u.Patient)
            .OrderBy(u => u.Email)
            .ToListAsync();

        var rows = new List<UserRow>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            rows.Add(new UserRow
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName,
                Address = user.Address,
                Role = roles.FirstOrDefault() ?? "",
                DoctorId = user.DoctorId,
                DoctorName = user.Doctor?.Name,
                PatientId = user.PatientId,
                PatientName = user.Patient?.Name
            });
        }

        ViewBag.TotalUsers = rows.Count;
        ViewBag.Admins = rows.Count(r => r.Role == RoleNames.Admin);
        ViewBag.Doctors = rows.Count(r => r.Role == RoleNames.Doctor);
        ViewBag.Patients = rows.Count(r => r.Role == RoleNames.Patient);

        await PopulateLists();
        return View(rows);
    }

    private async Task PopulateLists()
    {
        ViewBag.DoctorList = new SelectList(
            await _context.Doctors.AsNoTracking().OrderBy(d => d.Name).ToListAsync(),
            "Id", "Name");

        ViewBag.PatientList = new SelectList(
            await _context.Patients.AsNoTracking().OrderBy(p => p.Name).ToListAsync(),
            "Id", "Name");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string userId, string role)
    {
        if (role != RoleNames.Admin && role != RoleNames.Doctor && role != RoleNames.Patient)
            return BadRequest("دور غير معروف");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var current = await _userManager.GetRolesAsync(user);
        if (current.Any())
            await _userManager.RemoveFromRolesAsync(user, current);

        await _userManager.AddToRoleAsync(user, role);

        // لو الدور بقى مش طبيب/مريض، نشيل الربط
        if (role == RoleNames.Admin)
        {
            user.DoctorId = null;
            user.PatientId = null;
            await _userManager.UpdateAsync(user);
        }
        else if (role == RoleNames.Doctor)
        {
            user.PatientId = null;
            await _userManager.UpdateAsync(user);
        }
        else if (role == RoleNames.Patient)
        {
            user.DoctorId = null;
            await _userManager.UpdateAsync(user);
        }

        if (user.Id == _userManager.GetUserId(User))
            await _userManager.UpdateSecurityStampAsync(user);

        TempData["Success"] = "تم تحديث الصلاحية.";
        return RedirectToAction(nameof(Index));
    }

    // ربط حساب طبيب بملف طبيب بالـ Id
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkDoctor(string userId, int? doctorId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (doctorId.HasValue)
        {
            // تأكد إن الطبيب ده مش مربوط بحساب تاني
            var taken = await _userManager.Users.AnyAsync(u => u.DoctorId == doctorId.Value && u.Id != user.Id);
            if (taken)
            {
                TempData["Error"] = "الطبيب ده مربوط بحساب تاني بالفعل.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _context.Doctors.AnyAsync(d => d.Id == doctorId.Value))
            {
                TempData["Error"] = "الطبيب غير موجود.";
                return RedirectToAction(nameof(Index));
            }
        }

        user.DoctorId = doctorId;
        user.PatientId = null;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = doctorId.HasValue ? "تم ربط الحساب بالطبيب." : "تم إلغاء ربط الطبيب.";
        return RedirectToAction(nameof(Index));
    }

    // ربط حساب مريض بملف مريض بالـ Id
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkPatient(string userId, int? patientId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (patientId.HasValue)
        {
            var taken = await _userManager.Users.AnyAsync(u => u.PatientId == patientId.Value && u.Id != user.Id);
            if (taken)
            {
                TempData["Error"] = "المريض ده مربوط بحساب تاني بالفعل.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _context.Patients.AnyAsync(p => p.Id == patientId.Value))
            {
                TempData["Error"] = "المريض غير موجود.";
                return RedirectToAction(nameof(Index));
            }
        }

        user.PatientId = patientId;
        user.DoctorId = null;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = patientId.HasValue ? "تم ربط الحساب بالمريض." : "تم إلغاء ربط المريض.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (user.Id == _userManager.GetUserId(User))
            return BadRequest("مش ممكن تمسح حسابك الحالي");

        await _userManager.DeleteAsync(user);
        TempData["Success"] = "تم حذف المستخدم.";
        return RedirectToAction(nameof(Index));
    }

    // POST: ADMIN/CreateDoctorAccount — إنشاء حساب دخول لطبيب
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDoctorAccount(int doctorId, string email, string password)
    {
        var doctor = await _context.Doctors.FindAsync(doctorId);
        if (doctor == null) return NotFound();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            TempData["Error"] = "الإيميل وكلمة المرور (6 أحرف على الأقل) مطلوبين.";
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.FindByEmailAsync(email) != null)
        {
            TempData["Error"] = $"الإيميل {email} مستخدم بالفعل.";
            return RedirectToAction(nameof(Index));
        }

        var user = new ApplicationUser
        {
            UserName = email.Trim(),
            Email = email.Trim(),
            FullName = doctor.Name,
            DoctorId = doctor.Id,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            TempData["Error"] = "تعذّر إنشاء الحساب: " +
                string.Join(" | ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        await _userManager.AddToRoleAsync(user, RoleNames.Doctor);

        TempData["Success"] = $"تم إنشاء حساب للطبيب {doctor.Name} ({email}).";
        return RedirectToAction(nameof(Index));
    }

    // ================= إدارة كلمات المرور =================

    // GET: ADMIN/Passwords — قائمة كل المستخدمين لتغيير باسورداتهم
    public async Task<IActionResult> Passwords()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .ToListAsync();

        var rows = new List<PasswordRow>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            rows.Add(new PasswordRow
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? "",
                HasLockout = await _userManager.IsLockedOutAsync(user)
            });
        }

        return View(rows);
    }

    // POST: ADMIN/SetPassword — المدير يحدد باسورد جديد لأي مستخدم
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(string userId, string newPassword, string? confirmPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["Error"] = "كلمة المرور لازم تكون 6 أحرف على الأقل.";
            return RedirectToAction(nameof(Passwords));
        }

        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "كلمتا المرور مش متطابقتين.";
            return RedirectToAction(nameof(Passwords));
        }

        // نشيل الباسورد القديم ونحط الجديد (Remove+Add أأمن من ResetPasswordToken)
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            TempData["Error"] = "تعذّر تغيير كلمة المرور: " +
                string.Join(" | ", removeResult.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Passwords));
        }

        var addResult = await _userManager.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded)
        {
            TempData["Error"] = "كلمة المرور مرفوضة: " +
                string.Join(" | ", addResult.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Passwords));
        }

        // تحديث الـ SecurityStamp يقطع أي جلسة مفتوحة للحساب ده
        await _userManager.UpdateSecurityStampAsync(user);

        // لو المدير غيّر باسورده هو نفسه، لازم يعيد الدخول
        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Success"] = "تم تغيير كلمة مرورك. سجّل الدخول من جديد.";
            return RedirectToAction("LogoutConfirmed", "Account");
        }

        TempData["Success"] = $"تم تغيير كلمة مرور {user.Email}.";
        return RedirectToAction(nameof(Passwords));
    }

    // POST: ADMIN/UnlockUser — فك قفل حساب بعد محاولات دخول فاشلة
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        TempData["Success"] = $"تم فك قفل {user.Email}.";
        return RedirectToAction(nameof(Passwords));
    }

    public class PasswordRow
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string? FullName { get; set; }
        public string Role { get; set; } = "";
        public bool HasLockout { get; set; }
    }

    public class UserRow
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Address { get; set; } = "";
        public string Role { get; set; } = "";
        public int? DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public int? PatientId { get; set; }
        public string? PatientName { get; set; }
    }
}
