using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.Expeditions;

/// <summary>
/// Sefer yazma tarafı. olsold: <c>ExpeditionController::save/update/delete</c>
///
/// Sefer numarası SİBER'de üretilir; akış kaynak koddan birebir taşındı:
///   1. Araç zaten aktif seferde mi kontrol edilir (durumid != 14).
///   2. Aynı yıl/iş türü/araç sahipliği için son sefer numarası regex ile
///      ayrıştırılıp bir artırılır.
///   3. Bu numaraya karşılık gelen skn_sefer varsa altına yeni pozisyon eklenir,
///      yoksa önce skn_sefer (seferno = max+1) sonra pozisyon oluşturulur.
///   4. Yerel Expedition kaydı, Siber'in ürettiği sefer numarasıyla yazılır.
/// </summary>
public interface IExpeditionWriteService
{
    Task<ExpeditionWriteResult> CreateAsync(
        ExpeditionWriteModel model, CancellationToken cancellationToken = default);

    Task<ExpeditionWriteResult> UpdateAsync(
        ExpeditionWriteModel model, CancellationToken cancellationToken = default);

    Task DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
}

public sealed class ExpeditionWriteModel
{
    public long? Id { get; init; }
    public long? RomorkId { get; init; }
    public long? WorkType { get; init; }
    public long? DepartmentId { get; init; }
    public long? ExpeditionTypeId { get; init; }
    public long? ExpeditionStatusId { get; init; }

    public DateOnly? ReleaseDate { get; init; }
    public DateOnly? EntryDate { get; init; }
    public DateOnly? LoadingDate { get; init; }
    public DateOnly? ReturnDate { get; init; }
    public DateOnly? CarExitDate { get; init; }
    public Guid? StartCityId { get; init; }
    public Guid? LoadCityId { get; init; }
    public Guid? EndCityId { get; init; }

    public long? CurrentUserId { get; init; }
}

/// <summary>
/// olsold doğrulama hatalarını <c>{'error': ['...']}</c> veya
/// <c>{'errors': {'message': ['...']}}</c> şeklinde döndürüyordu; mesaj metinleri korunur.
/// </summary>
public sealed record ExpeditionWriteResult(long? Id, string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;
    public static ExpeditionWriteResult Fail(string message) => new(null, message);
    public static ExpeditionWriteResult Ok(long id) => new(id, null);
}

public sealed class ExpeditionWriteService : IExpeditionWriteService
{
    /// <summary>Sefer numarasındaki iki harf arasındaki sayıyı yakalar (ör. "26A0007I" -> 0007).</summary>
    private static readonly Regex SeferNoPattern = new(@"[A-Za-z](\d+)[A-Za-z]", RegexOptions.Compiled);


    private readonly OlsDbContext _db;
    private readonly ISiberExpeditionRepository _siber;
    private readonly IClock _clock;

    public ExpeditionWriteService(
        OlsDbContext db, ISiberExpeditionRepository siber, IClock clock)
    {
        _db = db;
        _siber = siber;
        _clock = clock;
    }

    public async Task<ExpeditionWriteResult> CreateAsync(
        ExpeditionWriteModel model, CancellationToken cancellationToken = default)
    {
        if (!_siber.IsConfigured)
            return ExpeditionWriteResult.Fail("Siber bağlantısı yapılandırılmamış.");

        var car = await _db.Cars.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == model.RomorkId, cancellationToken);
        if (car?.SiberId is null)
            return ExpeditionWriteResult.Fail("Plaka bulunamadı");

        var workType = await _db.WorkTypes.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == model.WorkType, cancellationToken);
        if (workType is null)
            return ExpeditionWriteResult.Fail("İş türü bulunamadı");

        var department = await _db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == model.DepartmentId, cancellationToken);
        if (department is null)
            return ExpeditionWriteResult.Fail("Departman bulunamadı");

        var expeditionType = await _db.ExpeditionTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == model.ExpeditionTypeId, cancellationToken);
        if (expeditionType is null)
            return ExpeditionWriteResult.Fail("Sefer türü bulunamadı");

        if (await _siber.IsCarOnActiveTripAsync(car.SiberId, cancellationToken))
            return ExpeditionWriteResult.Fail("Bu araç zaten seferde");

        var owner = await _db.CarOwners.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == car.VehicleOwner, cancellationToken);

        // ARAÇ SAHİBİ KODU. Eskiden bu satır yerel bir id'ye bakıyordu
        // (`car.VehicleOwner == 2 ? 1 : 0`, olsold'dan taşınmış). Referans verisi
        // Siber'den yeniden içe aktarıldıktan sonra car_owners artık id 3'ten
        // başlıyor (Öz Mal=3, Kiralık=4, ...), yani id=2 diye bir satır KALMADI ve
        // bayrak HER ZAMAN 0 üretiyordu — kiralık bir araçla açılan seferde bile
        // "özmal" seferleri taranıyordu.
        //
        // Doğru kaynak, tanımın kendi kodu: car_owners.code Öz Mal=0, Kiralık=1
        // olarak duruyor ve Siber'in skn_sefer.aracsahip / skn_pozisyon.romorkaracsahip
        // kodlamasıyla birebir aynı (canlıda doğrulandı: aracsahip 0 -> OZ, 1 -> KR).
        var ownerFlag = owner?.Code ?? 0;

        var now = _clock.Now;
        var fullYear = now.ToString("yyyy");
        var shortYear = now.ToString("yy");
        var yearWeek = $"{now:yyyy}/{System.Globalization.ISOWeek.GetWeekOfYear(now)}";

        // Sıradaki sefer numarası: son pozisyonun numarasındaki sayı + 1
        var lastSeferNo = await _siber.FindLastSeferNoAsync(
            fullYear, workType.Code, ownerFlag, cancellationToken);

        var nextNumber = 1;
        if (lastSeferNo is not null)
        {
            var match = SeferNoPattern.Match(lastSeferNo);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var parsed))
                nextNumber = parsed + 1;
        }

        var seferId = await _siber.FindSeferIdAsync(
            shortYear, owner?.AdditionalCode, nextNumber, cancellationToken);

        // Sefer yoksa önce onu oluştur.
        if (seferId is null)
        {
            var newSeferId = (await _siber.GenerateSeferIdAsync(cancellationToken)).ToString();

            // Numara Siber tarafında, aynı kilitli işlem içinde üretiliyor —
            // sayacın (yıl, araç sahibi) kapsamı ve yarış durumu için bkz.
            // InsertSeferWithLockedNumberAsync'in XML açıklaması.
            await _siber.InsertSeferWithLockedNumberAsync(new SiberSefer
            {
                SeferId = newSeferId,
                AracSahip = ownerFlag,
                CikisTarih = ToDateTime(model.ReleaseDate),
                DonusTarih = ToDateTime(model.EntryDate),
                Yil = shortYear,
            }, cancellationToken);

            seferId = newSeferId;
        }

        var pozisyonId = (await _siber.GeneratePozisyonIdAsync(cancellationToken)).ToString();
        var sirano = await _siber.NextSiranoAsync(seferId, cancellationToken);

        var currentUserSiberCode = await CurrentUserSiberCodeAsync(model.CurrentUserId, cancellationToken);

        await _siber.InsertPozisyonAsync(new SiberPozisyon
        {
            PozisyonId = pozisyonId,
            SeferId = seferId,
            IsTuru = workType.Code,
            Sirano = sirano,
            DurumId = 1,
            RomorkId = car.SiberId,
            Hafta = yearWeek,
            DepartmanId = department.SiberId,
            KayitGirisTarih = now,
            SeferTurId = expeditionType.Code,
            KayitGiren = currentUserSiberCode,
        }, cancellationToken);

        // Sefer numarasını Siber üretiyor; geri okuyup yerel kayda yazıyoruz.
        var generatedSeferNo = await _siber.ReadPozisyonSeferNoAsync(pozisyonId, cancellationToken);

        var expedition = new Expedition
        {
            ExpeditionId = pozisyonId,
            ExpeditionNumber = generatedSeferNo,
            SeferId = seferId,
            StatusId = 1,
            WorkType = (int)workType.Id,
            RomorkId = (int)car.Id,
            YearWeek = yearWeek,
            ExpeditionTypeId = (int)expeditionType.Id,
            RegistrationLoginDate = DateOnly.FromDateTime(now),
            DepartmentId = (int)department.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Expeditions.Add(expedition);
        await _db.SaveChangesAsync(cancellationToken);

        return ExpeditionWriteResult.Ok(expedition.Id);
    }

    public async Task<ExpeditionWriteResult> UpdateAsync(
        ExpeditionWriteModel model, CancellationToken cancellationToken = default)
    {
        var expedition = await _db.Expeditions
            .FirstOrDefaultAsync(e => e.Id == model.Id, cancellationToken);

        if (expedition is null)
            return ExpeditionWriteResult.Fail("Sefer Bulunamadı");

        var car = await _db.Cars.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == model.RomorkId, cancellationToken);
        if (car is null)
            return ExpeditionWriteResult.Fail("Plaka bulunamadı");

        var status = await _db.ExpeditionStatuses.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == model.ExpeditionStatusId, cancellationToken);
        if (status is null)
            return ExpeditionWriteResult.Fail("Durum bulunamadı");

        var workType = await _db.WorkTypes.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == model.WorkType, cancellationToken);
        if (workType is null)
            return ExpeditionWriteResult.Fail("İş türü bulunamadı");

        var department = await _db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == model.DepartmentId, cancellationToken);
        if (department is null)
            return ExpeditionWriteResult.Fail("Departman bulunamadı");

        var expeditionType = await _db.ExpeditionTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == model.ExpeditionTypeId, cancellationToken);
        if (expeditionType is null)
            return ExpeditionWriteResult.Fail("Sefer türü bulunamadı");

        expedition.StatusId = (int)status.Id;
        expedition.WorkType = (int)workType.Id;
        expedition.RomorkId = (int)car.Id;
        expedition.DepartmentId = (int)department.Id;
        expedition.ExpeditionTypeId = (int)expeditionType.Id;
        expedition.ReleaseDate = model.ReleaseDate;
        expedition.LoadingDate = model.LoadingDate;
        expedition.ReturnDate = model.ReturnDate;
        expedition.CarExitDate = model.CarExitDate;
        expedition.StartCityId = model.StartCityId;
        expedition.LoadCityId = model.LoadCityId;
        expedition.EndCityId = model.EndCityId;
        expedition.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);

        if (_siber.IsConfigured && expedition.ExpeditionId is not null)
        {
            await _siber.UpdatePozisyonAsync(
                expedition.ExpeditionId, status.ExpeditionStatusId, car.SiberId, cancellationToken);
        }

        return ExpeditionWriteResult.Ok(expedition.Id);
    }

    /// <summary>
    /// olsold sefer silmede Siber'e dokunmuyordu; aynı davranış korunuyor
    /// (Siber'de pozisyona bağlı yük aktarma kayıtları olabilir).
    /// </summary>
    public async Task DeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var expeditions = await _db.Expeditions
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(cancellationToken);

        foreach (var expedition in expeditions)
        {
            _db.ExpeditionMovements.RemoveRange(
                _db.ExpeditionMovements.Where(m => m.ExpeditionId == expedition.Id));

            // Sefer–Yük eşlemeleri yerel sefer id'sini METİN olarak tutuyor
            // (bkz. SyncExpeditionLoadMappingsAsync) — FK yok, elle temizlenir.
            var key = expedition.Id.ToString();
            _db.ExpeditionLoadMappings.RemoveRange(
                _db.ExpeditionLoadMappings.Where(m => m.ExpeditionId == key));
        }

        _db.Expeditions.RemoveRange(expeditions);

        // ÖNCE SİBER, SONRA YEREL — bkz. LoadTransferWriteService.DeleteAsync'teki
        // gerekçe: yalnızca yerelden silinirse senkron kaydı geri getiriyor, ters
        // sırada ise Siber hatasında iki taraf tutarsız kalıyor.
        if (_siber.IsConfigured)
        {
            foreach (var siberId in expeditions.Select(e => e.ExpeditionId)
                         .Where(s => !string.IsNullOrWhiteSpace(s)).Distinct())
                await _siber.DeletePozisyonAsync(siberId!, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string?> CurrentUserSiberCodeAsync(
        long? userId, CancellationToken cancellationToken) =>
        userId is null
            ? null
            : await _db.Users.Where(u => u.Id == userId)
                .Select(u => u.SiberCode)
                .FirstOrDefaultAsync(cancellationToken);

    private static DateTime? ToDateTime(DateOnly? date) =>
        date?.ToDateTime(TimeOnly.MinValue);
}
