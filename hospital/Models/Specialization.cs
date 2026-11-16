using System.ComponentModel.DataAnnotations;

namespace hospital.Models
{
    public class Specialization
    {
        // 1. الـ Primary Key: الـ EF Core أول ما بيشوف خاصية اسمها Id بيعرف لوحده إنها مفتاح أساسي وبيخليها Identity (تزيد لوحدها 1, 2, 3)

        public int Id { get; set; }

        // 2. Data Annotations: دي شروط بنحطها فوق الخاصية عشان تتحكم في شكل العمود في الداتابيز وكمان تعمل Validation في الفورم
        [Required(ErrorMessage = "اسم التخصص مطلوب")] // يعني العمود ده NOT NULL في الداتابيز
        [MaxLength(100)] // يعني نوعه في الداتابيز هيبقى nvarchar(100) بدل nvarchar(max) وده أحسن للأداء
        [Display(Name = "اسم التخصص")]
        public string Name { get; set; } = string.Empty;

        // علامة الاستفهام (?) جنب string معناها إن الحقل ده اختياري (Nullable) يعني ممكن اليوزر ميملاهوش
        [MaxLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        // ================= العلاقة (Relationship) =================
        // Navigation Property: دي مش بتعمل عمود في جدول التخصصات في الداتابيز!
        // أمال لازمتها إيه؟ دي عشان في الكود (C#) لما أجيب تخصص معين، أقدر أقوله: specialization.Doctors فيجيبلي لستة الدكاترة اللي جواه.
        public List<Doctor> Doctors { get; set; } = [];
    }
}
