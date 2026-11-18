namespace hospital;

/// <summary>
/// أسماء الأدوار الثابتة + ترجمتها للعربي للعرض.
/// القيم المخزنة في الداتابيز إنجليزي (Admin/Doctor/Patient).
/// </summary>
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Patient = "Patient";

    /// <summary>
    /// الثلاثة أدوار في نص واحد — لاستخدامه في [Authorize(Roles = ...)] كـ OR.
    /// ملاحظة: attributes منفصلة معناها AND (مستحيل)، النص الواحد معناه OR.
    /// </summary>
    public const string Staff = Admin + "," + Doctor + "," + Patient;

    public static string ToArabic(string? role) => role switch
    {
        Admin => "مدير",
        Doctor => "طبيب",
        Patient => "مريض",
        _ => "بدون دور"
    };

    public static string Of(System.Security.Claims.ClaimsPrincipal user)
    {
        if (user.IsInRole(Admin)) return "مدير";
        if (user.IsInRole(Doctor)) return "طبيب";
        if (user.IsInRole(Patient)) return "مريض";
        return "زائر";
    }
}
