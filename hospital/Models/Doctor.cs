using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hospital.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الطبيب مطلوب")]
        [MaxLength(150)]
        [Display(Name = "اسم الطبيب")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [MaxLength(20)]
        [Display(Name = "رقم الهاتف")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "سعر الكشف مطلوب")]
        [Column(TypeName = "decimal(18,2)")] // بنحدد دقة الرقم العشري في SQL عشان الفلوس (18 رقم منهم رقمين عشريين)
        [Display(Name = "سعر الكشف")]
        public decimal ConsultationFee { get; set; }

        // هنا هنخزن "اسم أو مسار الصورة" بس كـ نص في الداتابيز، لكن الصورة نفسها هتتحفظ في فولدر wwwroot
        [Display(Name = "صورة الطبيب")]
        public string? ImageUrl { get; set; }

        // ================= 1. علاقته بالتخصص (Many-to-One) =================
        // ده الـ Foreign Key الفعلي اللي هيتعمل بيه عمود في جدول الأطباء في الداتابيز
        [Required(ErrorMessage = "يرجى اختيار Specialization")]
        [Display(Name = "Specialization")]
        public int SpecializationId { get; set; }

        // دي الـ Navigation Property عشان لما أجيب الدكتور أقدر أوصل لاسم التخصص بتاعه عن طريق: doctor.Specialization.Name
        public Specialization? Specialization { get; set; }

        // ================= 2. علاقته بالحجوزات (One-to-Many) =================
        // الدكتور الواحد عنده مواعيد وحجوزات كتير
        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
