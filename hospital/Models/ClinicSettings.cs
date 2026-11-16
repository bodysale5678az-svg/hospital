namespace hospital.Models
{
    public class ClinicSettings
    {
        public int Id { get; set; }

        public string ClinicName { get; set; } = "عيادات آل صالح التخصصي";

        public TimeSpan OpenTime { get; set; }        // 09:00
        public TimeSpan CloseTime { get; set; }       // 23:00
        public TimeSpan? BreakStart { get; set; }     // 17:00 (راحة)
        public TimeSpan? BreakEnd { get; set; }       // 18:00

        // "Saturday,Sunday,Monday,Tuesday,Wednesday,Thursday"
        public string WorkingDays { get; set; } = "Saturday,Sunday,Monday,Tuesday,Wednesday,Thursday";

        public int SlotMinutes { get; set; } = 30;    // مدة الكشف
    }
}
