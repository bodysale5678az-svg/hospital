using Microsoft.AspNetCore.Identity;

namespace hospital.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    // ============ ربط الحساب بالطبيب أو المريض بالـ Id ============
    // لو الحساب ده لطبيب: DoctorId بيشاور على صف في جدول Doctors
    public int? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    // لو الحساب ده لمريض: PatientId بيشاور على صف في جدول Patients
    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }
}
