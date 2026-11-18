using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using hospital.Data;
using hospital.Models;

namespace hospital.Controllers
{
    
    public class SpecializationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SpecializationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // القراءة متاحة لأي مستخدم مسجل
        public async Task<IActionResult> Index()
        {
            var specializations = await _context.Specializations
                .Include(s => s.Doctors)
                .AsNoTracking()
                .ToListAsync();

            return View(specializations);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var specialization = await _context.Specializations
                .Include(s => s.Doctors)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (specialization == null) return NotFound();

            return View(specialization);
        }

        // ============ التعديل للمدير فقط ============

        [Authorize(Roles = RoleNames.Admin)]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Create(Specialization specialization)
        {
            if (ModelState.IsValid)
            {
                _context.Add(specialization);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(specialization);
        }

        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var specialization = await _context.Specializations.FindAsync(id);
            if (specialization == null) return NotFound();

            return View(specialization);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Edit(int id, Specialization specialization)
        {
            if (id != specialization.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(specialization);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SpecializationExists(specialization.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(specialization);
        }

        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var specialization = await _context.Specializations
                .Include(s => s.Doctors)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (specialization == null) return NotFound();

            return View(specialization);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var specialization = await _context.Specializations
                .Include(s => s.Doctors)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (specialization != null)
            {
                if (specialization.Doctors.Any())
                {
                    ModelState.AddModelError("", "لا يمكن حذف تخصص مرتبط بأطباء.");
                    return View(specialization);
                }

                _context.Specializations.Remove(specialization);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SpecializationExists(int id)
        {
            return _context.Specializations.Any(e => e.Id == id);
        }
    }
}
