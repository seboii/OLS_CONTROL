using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class Expedition
{
    public long Id { get; set; }

    public string? ExpeditionId { get; set; }

    public string? ExpeditionNumber { get; set; }

    public string? SeferId { get; set; }

    public int? WorkType { get; set; }

    public int? StatusId { get; set; }

    public int? RomorkId { get; set; }

    public string? YearWeek { get; set; }

    public int? DepartmentId { get; set; }

    public DateOnly? RegistrationLoginDate { get; set; }

    public int? ExpeditionTypeId { get; set; }

    public DateOnly? CarExitDate { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public DateOnly? LoadingDate { get; set; }

    public DateOnly? ReturnDate { get; set; }

    public Guid? StartCityId { get; set; }

    public Guid? LoadCityId { get; set; }

    public Guid? EndCityId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ExpeditionMovement> ExpeditionMovements { get; set; } = new List<ExpeditionMovement>();
}
