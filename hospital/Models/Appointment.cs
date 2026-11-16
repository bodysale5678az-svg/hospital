using System.ComponentModel.DataAnnotations;

namespace hospital.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "تاريخ ووقت الكشف مطلوب")]
        [Display(Name = "تاريخ ووقت الكشف")]
        public DateTime AppointmentDate { get; set; }

        // حالة الكشف خليناها string، واديناها قيمة افتراضية "قيد الانتظار" عشان أول ما نحجز كشف جديد ياخد الحالة دي لوحده
        [Required]
        [MaxLength(50)]
        [Display(Name = "حالة الكشف")]
        public string Status { get; set; } = "قيد الانتظار";

        // ملاحظات بيكتبها موظف الاستقبال وقت الحجز (اختياري عشان كده حطينا ?)
        [MaxLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        // التشخيص والروشتة اللي هيكتبها الدكتور لما المريض يدخل يكشف (برضه اختياري وقت إنشاء الحجز)
        [MaxLength(2000)]
        [Display(Name = "التشخيص والروشتة")]
        public string? Diagnosis { get; set; }

        // ================= 1. الربط مع الطبيب =================
        [Required(ErrorMessage = "يرجى اختيار الطبيب")]
        [Display(Name = "الطبيب")]
        public int DoctorId { get; set; } // Foreign Key
        public Doctor? Doctor { get; set; } // Navigation Property

        // ================= 2. الربط مع المريض =================
        [Required(ErrorMessage = "يرجى اختيار المريض")]
        [Display(Name = "المريض")]
        public int PatientId { get; set; } // Foreign Key
        public Patient? Patient { get; set; } // Navigation Property
    }
}
