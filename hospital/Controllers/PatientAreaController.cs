using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using hospital.Data;
using hospital.Models;

namespace hospital.Controllers;

[Authorize(Roles = RoleNames.Patient)]
public class PatientAreaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientAreaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var me = await _userManager.GetUserAsync(User);
        ViewBag.Me = me;

        // لو الحساب دور Patient ومش مربوط بملف مريض — نعمله ملف تلقائياً.
        // شرط الدور إلزامي: من غيره أي مستخدم (أو زائر) يدخل الصفحة يعمله ملف مريض وهمي.
        if (me != null && me.PatientId == null && User.IsInRole(RoleNames.Patient))
        {
            var newPatient = new Patient
            {
                Name = string.IsNullOrWhiteSpace(me.FullName) ? (me.Email ?? "مريض") : me.FullName!,
                Phone = string.IsNullOrWhiteSpace(me.PhoneNumber) ? "غير محدد" : me.PhoneNumber!,
                Age = 1,
                Gender = "غير محدد"
            };

            _context.Patients.Add(newPatient);
            await _context.SaveChangesAsync();

            me.PatientId = newPatient.Id;
            await _userManager.UpdateAsync(me);

            // نعمل refresh عشان الـ claim/cookie تاخد القيمة الجديدة
            ViewBag.Patient = newPatient;
            ViewBag.JustLinked = true;

            return View(new List<Appointment>());
        }

        if (me?.PatientId == null)
        {
            ViewBag.NotLinked = true;
            return View(new List<Appointment>());
        }

        var appointments = await _context.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialization)
            .AsNoTracking()
            .Where(a => a.PatientId == me.PatientId.Value)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        ViewBag.Patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == me.PatientId.Value);

        return View(appointments);
    }

    // GET: PATIENTAREA/EditProfile — المريض يعدّل بياناته
    public async Task<IActionResult> EditProfile()
    {
        var me = await _userManager.GetUserAsync(User);
        if (me?.PatientId == null)
        {
            TempData["Error"] = "حسابك مش مربوط بملف مريض، مش ممكن تعدّل البيانات.";
            return RedirectToAction(nameof(Index));
        }

        var patient = await _context.Patients.FindAsync(me.PatientId.Value);
        if (patient == null)
        {
            TempData["Error"] = "ملف المريض غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        return View(patient);
    }

    // POST: PATIENTAREA/EditProfile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(
        [Bind("Name,Phone,Age,Gender")] Patient input)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me?.PatientId == null)
        {
            TempData["Error"] = "حسابك مش مربوط بملف مريض.";
            return RedirectToAction(nameof(Index));
        }

        var patient = await _context.Patients.FindAsync(me.PatientId.Value);
        if (patient == null)
        {
            TempData["Error"] = "ملف المريض غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        // المريض مسموح له يعدّل بياناته الشخصية فقط، مش مواعيده أو ملفه الطبي
        if (!ModelState.IsValid)
        {
            input.Id = patient.Id;
            return View(input);
        }

        patient.Name = input.Name.Trim();
        patient.Phone = input.Phone.Trim();
        patient.Age = input.Age;
        patient.Gender = input.Gender.Trim();

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم تحديث بياناتك بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    // POST: PATIENTAREA/CancelAppointment/5 — المريض يلغي موعده
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAppointment(int id)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me?.PatientId == null)
        {
            return RedirectToAction(nameof(Index));
        }

        // ⚠️ مهم جداً: الشرط على PatientId يمنع المريض من إلغاء موعد حد تاني
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == me.PatientId.Value);

        if (appointment == null)
        {
            return NotFound();
        }

        if (appointment.Status == "ملغي")
        {
            TempData["Error"] = "الموعد ملغي بالفعل.";
            return RedirectToAction(nameof(Index));
        }

        if (appointment.Status == "مكتمل")
        {
            TempData["Error"] = "مش ممكن إلغاء موعد مكتمل.";
            return RedirectToAction(nameof(Index));
        }

        appointment.Status = "ملغي";
        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إلغاء الموعد.";
        return RedirectToAction(nameof(Index));
    }
}
