using System.ComponentModel.DataAnnotations;

namespace hospital.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المريض مطلوب")]
        [MaxLength(150)]
        [Display(Name = "اسم المريض")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [MaxLength(20)]
        [Phone]
        [Display(Name = "رقم الهاتف")]
        public string Phone { get; set; } = string.Empty;

        [Range(1, 120, ErrorMessage = "السن يجب أن يكون بين 1 و 120")]
        [Display(Name = "السن")]
        public int Age { get; set; }

        // شيلنا الـ Enum وخليناها string عادي يشيل كلمة "ذكر" أو "أنثى"
        [Required(ErrorMessage = "النوع مطلوب")]
        [MaxLength(10)]
        [Display(Name = "النوع")]
        public string Gender { get; set; } = string.Empty;

        // ================= علاقته بالحجوزات (One-to-Many) =================
        // عشان نقدر نجيب التاريخ المرضي (كل الحجوزات السابقة) بتاع المريض ده عن طريق: patient.Appointments
        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
