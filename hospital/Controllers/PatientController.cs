using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using hospital.Models;
using hospital.Data;

namespace hospital.Controllers
{
    [Authorize]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = RoleNames.Admin)]
        // GET: /Patient
        public async Task<IActionResult> Index()
        {
            var patients = await _context.Patients
                .Include(p => p.Appointments)
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(patients);
        }
        [Authorize(Roles = RoleNames.Admin)]
        // GET: /Patient/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // GET: /Patient/Create
        [Authorize(Roles = RoleNames.Admin)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Patient/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Create([Bind("Name,Phone,Age,Gender")] Patient patient)
        {
            if (ModelState.IsValid)
            {
                _context.Add(patient);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"تم إضافة المريض «{patient.Name}» بنجاح.";
                return RedirectToAction(nameof(Index));
            }

            return View(patient);
        }

        // GET: /Patient/Edit/5
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // POST: /Patient/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Edit(int? id, [Bind("Id,Name,Phone,Age,Gender")] Patient patient)
        {
            if (id != patient.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تعديل بيانات المريض بنجاح.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.Id))
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

            return View(patient);
        }

        // GET: /Patient/Delete/5
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.Appointments)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // POST: /Patient/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var patient = await _context.Patients.FindAsync(id);

            if (patient == null)
            {
                return NotFound();
            }

            try
            {
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف المريض بنجاح.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "تعذر حذف المريض لوجود بيانات مرتبطة به.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int? id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }
    }
}
