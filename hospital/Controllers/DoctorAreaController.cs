using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using hospital.Data;
using hospital.Models;

namespace hospital.Controllers;

[Authorize(Roles = RoleNames.Doctor)]
public class DoctorAreaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DoctorAreaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var me = await _userManager.GetUserAsync(User);
        ViewBag.Me = me;

        // لو الحساب مش مربوط بطبيب، نعرض رسالة بدل ما نرجّع مواعيد غلط
        if (me?.DoctorId == null)
        {
            ViewBag.NotLinked = true;
            return View(new List<Appointment>());
        }

        // المواعيد بتاعت الطبيب ده بالـ Id — مفيش مطابقة أسماء
        var myAppointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .AsNoTracking()
            .Where(a => a.DoctorId == me.DoctorId.Value)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        ViewBag.Doctor = await _context.Doctors
            .Include(d => d.Specialization)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == me.DoctorId.Value);

        return View(myAppointments);
    }
}
