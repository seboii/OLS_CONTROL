using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class ExpeditionLoadMapping
{
    public long Id { get; set; }

    public string? Yukaktarmaid { get; set; }

    public int? UploadUnload { get; set; }

    public string? LoadTransferId { get; set; }

    public string? ExpeditionId { get; set; }

    public string? RomorkId { get; set; }

    public string? YerId { get; set; }

    public DateOnly? Date { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
