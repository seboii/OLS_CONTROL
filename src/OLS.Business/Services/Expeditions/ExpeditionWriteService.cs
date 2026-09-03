using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Siber;
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

    /// <summary>Çekici plakası (cars). Siber: skn_pozisyon.cekiciid.</summary>
    public long? TractorId { get; init; }

    /// <summary>Sürücü (personnel). Siber: skn_pozisyon.surucuid.</summary>
    public long? DriverId { get; init; }

    /// <summary>Aracın kiralandığı firma (accounts). Siber: kiralananfirmaid.</summary>
    public long? RentedCompanyId { get; init; }

    /// <summary>
    /// Kaydın açılacağı şirket. YALNIZCA iki şirketi de gören kullanıcı için
    /// anlamlı; kapsamı olan kullanıcıda yok sayılır (bkz.
    /// <see cref="ICompanyScope.ResolveWriteCompanyAsync"/>).
    /// </summary>
    public string? SiberCompanyId { get; init; }

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
    private readonly ISiberCityResolver _cities;
    private readonly ISiberReferenceValidator _references;
    private readonly ICompanyScope _companyScope;
    private readonly IClock _clock;

    public ExpeditionWriteService(
        OlsDbContext db, ISiberExpeditionRepository siber, IClock clock,
        ISiberReferenceValidator references, ISiberCityResolver cities,
        ICompanyScope companyScope)
    {
        _db = db;
        _siber = siber;
        _clock = clock;
        _references = references;
        _cities = cities;
        _companyScope = companyScope;
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

        // SİBER REFERANSLARI YAZIMDAN ÖNCE DOĞRULANIR.
        //
        // skn_pozisyon.romorkid hem FK'li HEM NOT NULL: karşılığı olmayan bir
        // araç INSERT'i kesin düşürür. departmanid de FK'li ve SiberId'si boş
        // olsa sessizce NULL yazılır — sefer Siber'de departmansız açılırdı.
        // Bu akış eskiden yalnızca YEREL kaydın varlığına bakıyordu.
        // GÜZERGÂH ŞEHİRLERİ de doğrulanır: baslangicsehirid ve bitissehirid
        // skn_pozisyon'da FK'li, karşılığı olmayan şehir INSERT'i düşürür.
        var cityMap = await _cities.ResolveAsync(
            [model.StartCityId, model.LoadCityId, model.EndCityId], cancellationToken);

        // ÇEKİCİ / SÜRÜCÜ / KİRALANAN FİRMA — üçü de Siber'de FK'li.
        var tractorSiberId = await CarSiberIdAsync(model.TractorId, cancellationToken);
        var driverSiberId = await DriverSiberIdAsync(model.DriverId, cancellationToken);
        var rentedCompanySiberId = await AccountSiberIdAsync(model.RentedCompanyId, cancellationToken);

        var referenceFailure = await _references.ValidateAsync(
            [
                new("Araç", SiberReferenceTable.Arac, car.SiberId),
                new("Çekici", SiberReferenceTable.Arac, tractorSiberId),
                new("Sürücü", SiberReferenceTable.Personel, driverSiberId),
                new("Kiralanan firma", SiberReferenceTable.Firma, rentedCompanySiberId),
                new("Departman", SiberReferenceTable.Departman, department.SiberId),
                new("Başlangıç şehri", SiberReferenceTable.Sehir, SiberCity(cityMap, model.StartCityId)),
                new("Yükleme şehri", SiberReferenceTable.Sehir, SiberCity(cityMap, model.LoadCityId)),
                new("Bitiş şehri", SiberReferenceTable.Sehir, SiberCity(cityMap, model.EndCityId)),
            ],
            cancellationToken);

        if (referenceFailure is not null)
            return ExpeditionWriteResult.Fail(referenceFailure);

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

        // SEFER, AÇAN KULLANICININ ŞİRKETİNE YAZILIR. Eskiden şirket ve şube
        // depoda sabitti (hep OLS): Avrora kullanıcısının açtığı sefer OLS'e
        // düşüyor ve görünürlük kuralı gereği KENDİ listesinde hiç
        // görünmüyordu. Yük akışı bunu zaten böyle yapıyor.
        var companyId = await _companyScope.ResolveWriteCompanyAsync(
            model.CurrentUserId, model.SiberCompanyId, cancellationToken);

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

        var existing = await _siber.FindSeferAsync(
            shortYear, owner?.AdditionalCode, nextNumber, cancellationToken);

        // MEVCUT SEFER HER ZAMAN YENİDEN KULLANILAMAZ. Siber'in
        // skn_pozisyon_seferromorkkontrol_tr trigger'ı, EX/IM iş türünde ve
        // özmal/sözleşmeli kiralık seferde pozisyonun römorkunun seferinkiyle
        // AYNI olmasını şart koşuyor. Başka bir römorkla açılmış sefere
        // eklemeye çalışmak INSERT'i ROLLBACK ettiriyor ve kullanıcıya
        // "beklenmedik hata" olarak yansıyordu; böyle bir durumda kendi
        // seferimizi açıyoruz.
        var seferId = existing is not null && existing.AcceptsPosition(workType.Code, car.SiberId)
            ? existing.SeferId
            : null;

        if (seferId is null)
        {
            var newSeferId = (await _siber.GenerateSeferIdAsync(cancellationToken)).ToString();

            // Numara Siber tarafında, aynı kilitli işlem içinde üretiliyor —
            // sayacın (yıl, araç sahibi) kapsamı ve yarış durumu için bkz.
            // InsertSeferWithLockedNumberAsync'in XML açıklaması.
            await _siber.InsertSeferWithLockedNumberAsync(new SiberSefer
            {
                SeferId = newSeferId,
                SirketId = companyId,
                AracSahip = ownerFlag,
                CikisTarih = ToDateTime(model.ReleaseDate),
                DonusTarih = ToDateTime(model.EntryDate),
                Yil = shortYear,
                // Trigger'ın aradığı eşitlik: seferin römorku pozisyonunkiyle aynı.
                RomorkId = car.SiberId,
            }, cancellationToken);

            seferId = newSeferId;
        }

        var pozisyonId = (await _siber.GeneratePozisyonIdAsync(cancellationToken)).ToString();
        var sirano = await _siber.NextSiranoAsync(seferId, cancellationToken);

        var currentUserSiberCode = await CurrentUserSiberCodeAsync(model.CurrentUserId, cancellationToken);

        await _siber.InsertPozisyonAsync(new SiberPozisyon
        {
            PozisyonId = pozisyonId,
            SirketId = companyId,
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
            // GÜZERGÂH VE TARİHLER. Form bunları zaten topluyordu ama ne Siber'e
            // gidiyor ne de yerelde saklanıyordu; sefer Siber'de güzergâhsız ve
            // tarihsiz açılıyordu (bkz. SiberPozisyon.BaslangicSehirId).
            BaslangicSehirId = SiberCity(cityMap, model.StartCityId),
            YuklemeSehirId = SiberCity(cityMap, model.LoadCityId),
            BitisSehirId = SiberCity(cityMap, model.EndCityId),
            CikisTarih = ToDateTime(model.ReleaseDate),
            DonusTarih = ToDateTime(model.ReturnDate),
            YuklemeTarih = ToDateTime(model.LoadingDate),
            AracCikisTarih = ToDateTime(model.CarExitDate),
            CekiciId = tractorSiberId,
            SurucuId = driverSiberId,
            KiralananFirmaId = rentedCompanySiberId,
        }, cancellationToken);

        // Sefer numarasını Siber üretiyor; geri okuyup yerel kayda yazıyoruz.
        var generatedSeferNo = await _siber.ReadPozisyonSeferNoAsync(pozisyonId, cancellationToken);

        var expedition = new Expedition
        {
            ExpeditionId = pozisyonId,
            ExpeditionNumber = generatedSeferNo,
            SeferId = seferId,
            // Senkron bunu Siber'den zaten getiriyor, ama ilk turdan ÖNCE de
            // kayıt kendi listesinde görünsün diye burada da yazılır.
            SiberCompanyId = companyId,
            StatusId = 1,
            WorkType = (int)workType.Id,
            RomorkId = (int)car.Id,
            YearWeek = yearWeek,
            ExpeditionTypeId = (int)expeditionType.Id,
            RegistrationLoginDate = DateOnly.FromDateTime(now),
            DepartmentId = (int)department.Id,
            // Eskiden bu yedi alan oluşturmada yerelde de KAYBOLUYORDU; yalnızca
            // güncelleme ekranından girilebiliyordu.
            ReleaseDate = model.ReleaseDate,
            LoadingDate = model.LoadingDate,
            ReturnDate = model.ReturnDate,
            CarExitDate = model.CarExitDate,
            StartCityId = model.StartCityId,
            LoadCityId = model.LoadCityId,
            EndCityId = model.EndCityId,
            TractorId = (int?)model.TractorId,
            DriverId = model.DriverId,
            RentedCompanyId = (int?)model.RentedCompanyId,
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
        // ŞİRKET TAŞIMA — yalnızca iki şirketi de gören kullanıcı yapabilir ve
        // yalnızca değer GÖNDERİLDİĞİNDE. Kapsamlı kullanıcının her kaydetmesi
        // kaydı sessizce kendi şirketine çekmesin diye alan boşsa dokunulmuyor.
        if (!string.IsNullOrWhiteSpace(model.SiberCompanyId)
            && await _companyScope.CanChooseCompanyAsync(model.CurrentUserId, cancellationToken))
        {
            var target = await _companyScope.ResolveWriteCompanyAsync(
                model.CurrentUserId, model.SiberCompanyId, cancellationToken);

            if (!string.Equals(target, expedition.SiberCompanyId, StringComparison.OrdinalIgnoreCase))
            {
                // Siber'e de yazılmalı: senkron her turda sirketid'yi yerel
                // aynanın üzerine yazıyor, yalnızca yerel değişiklik geri alınırdı.
                if (_siber.IsConfigured && expedition.ExpeditionId is not null)
                    await _siber.MovePozisyonCompanyAsync(expedition.ExpeditionId, target, cancellationToken);

                expedition.SiberCompanyId = target;
            }
        }

        expedition.TractorId = (int?)model.TractorId;
        expedition.DriverId = model.DriverId;
        expedition.RentedCompanyId = (int?)model.RentedCompanyId;
        expedition.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);

        if (_siber.IsConfigured && expedition.ExpeditionId is not null)
        {
            var updateCities = await _cities.ResolveAsync(
                [model.StartCityId, model.LoadCityId, model.EndCityId], cancellationToken);

            await _siber.UpdatePozisyonAsync(new SiberPozisyon
            {
                PozisyonId = expedition.ExpeditionId,
                DurumId = status.ExpeditionStatusId ?? 0,
                RomorkId = car.SiberId,
                IsTuru = workType.Code,
                DepartmanId = department.SiberId,
                SeferTurId = expeditionType.Code,
                BaslangicSehirId = SiberCity(updateCities, model.StartCityId),
                YuklemeSehirId = SiberCity(updateCities, model.LoadCityId),
                BitisSehirId = SiberCity(updateCities, model.EndCityId),
                CikisTarih = ToDateTime(model.ReleaseDate),
                DonusTarih = ToDateTime(model.ReturnDate),
                YuklemeTarih = ToDateTime(model.LoadingDate),
                AracCikisTarih = ToDateTime(model.CarExitDate),
                CekiciId = await CarSiberIdAsync(model.TractorId, cancellationToken),
                SurucuId = await DriverSiberIdAsync(model.DriverId, cancellationToken),
                KiralananFirmaId = await AccountSiberIdAsync(model.RentedCompanyId, cancellationToken),
            }, cancellationToken);
        }

        return ExpeditionWriteResult.Ok(expedition.Id);
    }

    private async Task<string?> CarSiberIdAsync(long? carId, CancellationToken cancellationToken) =>
        carId is null ? null : await _db.Cars.AsNoTracking()
            .Where(c => c.Id == carId).Select(c => c.SiberId)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<string?> DriverSiberIdAsync(long? driverId, CancellationToken cancellationToken) =>
        driverId is null ? null : await _db.Personnel.AsNoTracking()
            .Where(p => p.Id == driverId).Select(p => p.SiberId)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<string?> AccountSiberIdAsync(long? accountId, CancellationToken cancellationToken) =>
        accountId is null ? null : await _db.Accounts.AsNoTracking()
            .Where(a => a.Id == accountId).Select(a => a.SiberId)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Yerel şehir kimliğinin Siber karşılığı; seçilmemişse null.</summary>
    private static string? SiberCity(IReadOnlyDictionary<string, string> map, Guid? cityId) =>
        cityId is { } id && map.TryGetValue(id.ToString(), out var siberId) ? siberId : null;

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
