using System;

namespace OLS.DataAccess.Entities;

/// <summary>
/// olsold: <c>user_goals</c> — kullanıcı başına aylık satış hedefi.
/// UserFormDrawer.vue'nin (Kullanıcılar formu) "Hedefler" sekmesinin
/// (UserTarget.vue) veri kaynağı; ayrı bir Reports/Hedef-ciro modülü DEĞİL.
/// </summary>
public partial class UserGoal
{
    public long Id { get; set; }

    public int? UserId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal GoalPrice { get; set; }

    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
