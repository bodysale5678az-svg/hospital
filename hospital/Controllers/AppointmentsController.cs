using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using hospital.Models;
using hospital.Data;

namespace hospital.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: APPOINTMENTS
        public async Task<IActionResult> Index()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .AsNoTracking()
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }
        [Authorize]
        // GET: APPOINTMENTS/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // GET: APPOINTMENTS/Create
        // الحجز متاح للمدير والطبيب والمريض — التعديل والحذف للمدير فقط
        [Authorize(Roles = RoleNames.Staff)]
        public async Task<IActionResult> Create()
        {
            await PopulateDropDownLists();
            await PopulateClinicSettings();
            return View();
        }

        // POST: APPOINTMENTS/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Staff)]
        public async Task<IActionResult> Create(
            [Bind("AppointmentDate,Status,Notes,Diagnosis,DoctorId")] Appointment appointment,
            string? patientName, string? patientPhone, int? patientAge, string? patientGender)
        {
            if (!await _context.Doctors.AnyAsync(d => d.Id == appointment.DoctorId))
            {
                ModelState.AddModelError(nameof(Appointment.DoctorId), "يرجى اختيار الطبيب");
            }

            // ===== بيانات المريض المكتوبة يدوياً =====
            if (string.IsNullOrWhiteSpace(patientName))
                ModelState.AddModelError(nameof(patientName), "اسم المريض مطلوب");

            if (string.IsNullOrWhiteSpace(patientPhone))
                ModelState.AddModelError(nameof(patientPhone), "رقم الهاتف مطلوب");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(patientPhone.Trim(), @"^[0-9+\-\s]{7,20}$"))
                ModelState.AddModelError(nameof(patientPhone), "رقم الهاتف غير صحيح");

            if (patientAge is null || patientAge < 1 || patientAge > 120)
                ModelState.AddModelError(nameof(patientAge), "السن يجب أن يكون بين 1 و 120");

            if (string.IsNullOrWhiteSpace(patientGender))
                ModelState.AddModelError(nameof(patientGender), "النوع مطلوب");

            await ValidateSchedule(appointment, editingId: null);

            if (ModelState.IsValid)
            {
                // البحث برقم الهاتف أولاً: لو المريض مسجل بالفعل نستخدمه بدل ما نكرره
                var phone = patientPhone!.Trim();
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Phone == phone);

                if (patient == null)
                {
                    patient = new Patient
                    {
                        Name = patientName!.Trim(),
                        Phone = phone,
                        Age = patientAge!.Value,
                        Gender = patientGender!.Trim()
                    };
                    _context.Patients.Add(patient);
                }
                else
                {
                    // حدّث البيانات لو تغيّرت
                    patient.Name = patientName!.Trim();
                    patient.Age = patientAge!.Value;
                    patient.Gender = patientGender!.Trim();
                }

                appointment.Patient = patient;
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                var isNew = patient.Id == 0 || _context.Entry(patient).State == EntityState.Added;
                TempData["Success"] = isNew
                    ? $"تم إضافة المريض \"{patient.Name}\" وحجز الكشف بنجاح."
                    : $"تم حجز الكشف للمريض \"{patient.Name}\" بنجاح.";

                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDownLists(appointment.DoctorId);
            await PopulateClinicSettings();

            // نرجّع القيم المكتوبة عشان المستخدم ما يعيدش كتابتها
            ViewBag.PatientName = patientName;
            ViewBag.PatientPhone = patientPhone;
            ViewBag.PatientAge = patientAge;
            ViewBag.PatientGender = patientGender;

            return View(appointment);
        }

        // GET: APPOINTMENTS/Edit/5
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            await PopulateDropDownLists(appointment.DoctorId, appointment.PatientId);
            await PopulateClinicSettings();
            return View(appointment);
        }

        // POST: APPOINTMENTS/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Edit(int? id, [Bind("Id,AppointmentDate,Status,Notes,Diagnosis,DoctorId,PatientId")] Appointment appointment)
        {
            if (id != appointment.Id)
            {
                return NotFound();
            }

            if (!await _context.Doctors.AnyAsync(d => d.Id == appointment.DoctorId))
            {
                ModelState.AddModelError(nameof(Appointment.DoctorId), "يرجى اختيار الطبيب");
            }

            if (!await _context.Patients.AnyAsync(p => p.Id == appointment.PatientId))
            {
                ModelState.AddModelError(nameof(Appointment.PatientId), "يرجى اختيار المريض");
            }

            await ValidateSchedule(appointment, editingId: appointment.Id);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تعديل الحجز بنجاح.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDownLists(appointment.DoctorId, appointment.PatientId);
            await PopulateClinicSettings();
            return View(appointment);
        }

        // GET: APPOINTMENTS/Delete/5
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // POST: APPOINTMENTS/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف الحجز بنجاح.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int? id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }

        // ================= التحقق من مواعيد العمل =================
        private async Task ValidateSchedule(Appointment appointment, int? editingId)
        {
            var settings = await _context.ClinicSettings.AsNoTracking().FirstOrDefaultAsync();

            // لو مفيش إعدادات مسجلة، نتخطى التحقق
            if (settings == null)
            {
                return;
            }

            var date = appointment.AppointmentDate;
            var time = date.TimeOfDay;

            // 1. الموعد في الماضي
            if (date < DateTime.Now)
            {
                ModelState.AddModelError(nameof(Appointment.AppointmentDate), "لا يمكن الحجز في وقت ماضي");
            }

            // 2. خارج ساعات العمل
            if (time < settings.OpenTime || time >= settings.CloseTime)
            {
                ModelState.AddModelError(nameof(Appointment.AppointmentDate),
                    $"مواعيد العمل من {settings.OpenTime:hh\\:mm} إلى {settings.CloseTime:hh\\:mm}");
            }

            // 3. يوم إجازة
            var workingDays = settings.WorkingDays
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (!workingDays.Contains(date.DayOfWeek.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(Appointment.AppointmentDate),
                    "العيادة مغلقة في هذا اليوم");
            }

            // 4. وقت الراحة
            if (settings.BreakStart.HasValue && settings.BreakEnd.HasValue
                && time >= settings.BreakStart.Value && time < settings.BreakEnd.Value)
            {
                ModelState.AddModelError(nameof(Appointment.AppointmentDate),
                    $"هذا وقت الراحة ({settings.BreakStart.Value:hh\\:mm} - {settings.BreakEnd.Value:hh\\:mm})");
            }

            // 5. منع تعارض الحجز لنفس الطبيب والوقت
            var conflict = await _context.Appointments.AsNoTracking().AnyAsync(a =>
                a.DoctorId == appointment.DoctorId &&
                a.AppointmentDate == appointment.AppointmentDate &&
                a.Status != "ملغي" &&
                (editingId == null || a.Id != editingId));

            if (conflict)
            {
                ModelState.AddModelError(nameof(Appointment.AppointmentDate),
                    "هذا الموعد محجوز بالفعل لهذا الطبيب");
            }
        }

        // بيملا قوائم الأطباء والمرضى بالأسماء (مش الأرقام)
        private async Task PopulateDropDownLists(int? doctorId = null, int? patientId = null)
        {
            var doctors = await _context.Doctors
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync();

            var patients = await _context.Patients
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            ViewData["DoctorId"] = new SelectList(doctors, "Id", "Name", doctorId);
            ViewData["PatientId"] = new SelectList(patients, "Id", "Name", patientId);
        }

        // بيمرر إعدادات العيادة للـ View (ساعات العمل والراحة)
        private async Task PopulateClinicSettings()
        {
            var settings = await _context.ClinicSettings.AsNoTracking().FirstOrDefaultAsync();

            ViewBag.ClinicSettings = settings;

            if (settings != null)
            {
                ViewBag.OpenTime = settings.OpenTime.ToString(@"hh\:mm");
                ViewBag.CloseTime = settings.CloseTime.ToString(@"hh\:mm");
                ViewBag.BreakStart = settings.BreakStart?.ToString(@"hh\:mm");
                ViewBag.BreakEnd = settings.BreakEnd?.ToString(@"hh\:mm");
            }
        }
    }
}
