using hospital.Models;
using Microsoft.EntityFrameworkCore;

namespace hospital.Data
{
    // 1. الوراثة من DbContext: لازم يورث من الكلاس ده عشان يكتسب كل قدرات الـ Entity Framework
    public class ApplicationDbContext : DbContext
    {
        // 2. الـ Constructor: بيستقبل الإعدادات (زي اسم السيرفر ونوع الداتابيز) من ملف Program.cs ويبعتها للكلاس الأب (base)
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 3. الـ DbSets (الجداول): كل سطر من دول معناه: "يا EF Core، اعمل جدول في الداتابيز للكلاس ده بالاسم ده"
        public DbSet<Specialization> Specializations => Set<Specialization>();
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<ClinicSettings> ClinicSettings => Set<ClinicSettings>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // فهارس
            modelBuilder.Entity<Doctor>()
                .HasIndex(d => d.Phone);

            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.Phone);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => new { a.DoctorId, a.AppointmentDate });

            // العلاقات
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Specialization)
                .WithMany(s => s.Doctors)
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // بيانات أولية
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Specialization>().HasData(
                new Specialization { Id = 1, Name = "طب الأسنان", Description = "زراعة وتجميل وتقويم الأسنان" },
                new Specialization { Id = 2, Name = "الجلدية والتجميل", Description = "العلاجات الجلدية والتجميلية" },
                new Specialization { Id = 3, Name = "طب الأطفال", Description = "رعاية صحة الأطفال ونموهم" },
                new Specialization { Id = 4, Name = "الباطنة", Description = "الأمراض الباطنية والفحوصات الدورية" },
                new Specialization { Id = 5, Name = "النساء والولادة", Description = "متابعة الحمل وصحة المرأة" },
                new Specialization { Id = 6, Name = "الأنف والأذن والحنجرة", Description = "اضطرابات السمع والتنفس والحنجرة" }
            );

            modelBuilder.Entity<Doctor>().HasData(
                new Doctor { Id = 1, Name = "د. أحمد آل صالح", Phone = "0500000001", ConsultationFee = 150m, SpecializationId = 1 },
                new Doctor { Id = 2, Name = "د. سارة العتيبي", Phone = "0500000002", ConsultationFee = 200m, SpecializationId = 2 },
                new Doctor { Id = 3, Name = "د. خالد الحربي", Phone = "0500000003", ConsultationFee = 120m, SpecializationId = 3 },
                new Doctor { Id = 4, Name = "د. نورة القحطاني", Phone = "0500000004", ConsultationFee = 180m, SpecializationId = 5 },
                new Doctor { Id = 5, Name = "د. ماجد الزهراني", Phone = "0500000005", ConsultationFee = 160m, SpecializationId = 4 },
                new Doctor { Id = 6, Name = "د. ريم السبيعي", Phone = "0500000006", ConsultationFee = 190m, SpecializationId = 6 }
            );
            Microsoft.EntityFrameworkCore.Metadata.Builders.DataBuilder<ClinicSettings> dataBuilder = modelBuilder.Entity<ClinicSettings>().HasData(new ClinicSettings
            {
                Id = 1,
                ClinicName = "عيادات آل صالح التخصصي",
                OpenTime = new TimeSpan(9, 0, 0),
                CloseTime = new TimeSpan(23, 0, 0),
                BreakStart = new TimeSpan(17, 0, 0),
                BreakEnd = new TimeSpan(18, 0, 0),
                WorkingDays = "Saturday,Sunday,Monday,Tuesday,Wednesday,Thursday",
                SlotMinutes = 30
            });

        }
    }
}
