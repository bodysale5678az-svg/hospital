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
