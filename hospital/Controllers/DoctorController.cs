using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using hospital.Models;
using hospital.Data;

namespace hospital.Controllers
{
    [Authorize]
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DOCTORS
        public async Task<IActionResult> Index()
        {
            var doctors = await _context.Doctors
                .Include(d => d.Specialization)
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync();

            return View(doctors);
        }

        // GET: DOCTORS/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Specialization)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // GET: DOCTORS/Create
        [Authorize(Roles = RoleNames.Admin)]
        public IActionResult Create()
        {
            PopulateSpecializationsDropDownList();
            return View();
        }

        // POST: DOCTORS/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Create([Bind("Id,Name,Phone,ConsultationFee,ImageUrl,SpecializationId")] Doctor doctor)
        {
            if (doctor.SpecializationId <= 0 || !await _context.Specializations.AnyAsync(s => s.Id == doctor.SpecializationId))
            {
                ModelState.AddModelError(nameof(Doctor.SpecializationId), "يرجى اختيار Specialization");
            }

            if (ModelState.IsValid)
            {
                _context.Add(doctor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم إضافة الطبيب بنجاح.";
                return RedirectToAction(nameof(Index));
            }

            PopulateSpecializationsDropDownList(doctor.SpecializationId);
            return View(doctor);
        }

        // GET: DOCTORS/Edit/5
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }

            PopulateSpecializationsDropDownList(doctor.SpecializationId);
            return View(doctor);
        }

        // POST: DOCTORS/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Edit(int? id, [Bind("Id,Name,Phone,ConsultationFee,ImageUrl,SpecializationId")] Doctor doctor)
        {
            if (id != doctor.Id)
            {
                return NotFound();
            }

            if (doctor.SpecializationId <= 0 || !await _context.Specializations.AnyAsync(s => s.Id == doctor.SpecializationId))
            {
                ModelState.AddModelError(nameof(Doctor.SpecializationId), "يرجى اختيار Specialization");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تعديل بيانات الطبيب بنجاح.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.Id))
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

            PopulateSpecializationsDropDownList(doctor.SpecializationId);
            return View(doctor);
        }

        // GET: DOCTORS/Delete/5
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Specialization)
                .Include(d => d.Appointments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // POST: DOCTORS/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.Appointments)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            // منع الحذف لو فيه حجوزات مرتبطة (العلاقة Restrict)
            if (doctor.Appointments.Any())
            {
                TempData["Error"] = "لا يمكن حذف طبيب لديه حجوزات. احذف أو انقل الحجوزات أولاً.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف الطبيب بنجاح.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "تعذر حذف الطبيب لوجود بيانات مرتبطة به.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DoctorExists(int? id)
        {
            return _context.Doctors.Any(e => e.Id == id);
        }

        // بيملا قائمة Specialization بالأسماء (مش الأرقام) في ViewBag
        private void PopulateSpecializationsDropDownList(object? selected = null)
        {
            var specializations = _context.Specializations
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToList();

            ViewBag.Specializations = new SelectList(specializations, "Id", "Name", selected);
        }
    }
}
