using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadFile
{
    public long Id { get; set; }

    public int? LoadId { get; set; }

    /// <summary>
    /// Dosyanın bağlı olduğu YÜK. Eskiden dosyalar yalnızca yükü doğuran
    /// TEKLİFE (LoadId) bağlanabiliyordu; teklifi olmayan yüklerde dosya ekleme
    /// sessizce çalışmıyordu. Canlıda ölçüldü: 7.998 yükün 4.285'i (%54)
    /// teklifsiz — yani ekranın yarısından fazlasında özellik ölüydü.
    /// İkisinden biri dolu olur.
    /// </summary>
    public long? LoadTransferId { get; set; }

    public string? File { get; set; }

    public string? MimeType { get; set; }

    public string? OrgName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
