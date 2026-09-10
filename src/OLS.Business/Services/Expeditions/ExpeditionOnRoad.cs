using System.Linq.Expressions;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Expeditions;

/// <summary>
/// "YOLDAKİ SEFER" TEK KURAL.
///
/// Panelin "Yoldaki Seferler" sayısı ile o karta tıklayınca açılan liste AYNI
/// kümeyi göstermek zorunda; kural bu yüzden tek yerde duruyor.
///
/// İKİ ŞİRKET İKİ FARKLI ŞEKİLDE ÇALIŞIYOR — BULUNAN GERÇEK HATA:
///
/// • <b>OLS</b> sefer durumunu ilerletiyor: canlıda 16 sefer "ÇIKIŞ YAPTI /
///   YÜKLENDİ / YURT DIŞI YOLDA / BOŞALTMADA" durumlarında.
/// • <b>AVRORA</b> durumu HİÇ ilerletmiyor: 285 seferin 260'ı "BOŞALTILDI",
///   25'i "HAZIR" ve arada başka hiçbir durum yok. Yolda olup olmadığı
///   TARİHLERDEN okunuyor — çıkış tarihi geçmiş, dönüş tarihi girilmemiş.
///
/// Yalnızca duruma bakan ilk sürüm bu yüzden Avrora'da HİÇBİR sefer
/// göstermiyordu; kullanıcının bildirdiği "özmal araçlar yolda ama
/// gözükmüyor" tam olarak buydu.
///
/// SON <see cref="RecentDepartureDays"/> GÜN SINIRI ŞART: sınırsız bırakılınca
/// OLS'te 40 sefer çıkıyor ve bunların 35'i yıllar önce çıkmış, dönüşü hiç
/// girilmemiş terk edilmiş kayıtlar. Sınır keyfi değil — tamamlanmış 3.559
/// seferin süresi ortanca 4 gün, %95'i 37 gün, %99'u 94 gün; 60 gün gerçek
/// seferlerin neredeyse tamamını kapsıyor, terk edilmiş kaydı kapsamıyor.
///
/// Ölçüm (2026-09-10): AVRORA 0 → 5, OLS 16 → 21.
/// </summary>
public static class ExpeditionOnRoad
{
    /// <summary>Çıkış yapmış sayılan en küçük durum sırası (15 = ÇIKIŞ YAPTI).</summary>
    public const int DepartedOrder = 15;

    /// <summary>Sefer bitmiş sayılan durum sırası (90 = BOŞALTILDI).</summary>
    public const int UnloadedOrder = 90;

    /// <summary>Dönüşü girilmemiş seferin "hâlâ yolda" sayılacağı en uzun süre.</summary>
    public const int RecentDepartureDays = 60;

    /// <summary>
    /// <paramref name="onRoadStatusIds"/>: sırası 15–89 arası durumların yerel
    /// kimlikleri. <paramref name="finishedStatusIds"/>: sırası ≥ 90 olanlar —
    /// tarih kuralı bunları dışarıda bırakıyor, yoksa boşaltılmış ama dönüşü
    /// girilmemiş sefer yolda görünürdü.
    /// </summary>
    public static Expression<Func<Expedition, bool>> Predicate(
        IReadOnlyCollection<long> onRoadStatusIds,
        IReadOnlyCollection<long> finishedStatusIds,
        DateOnly today)
    {
        var earliestDeparture = today.AddDays(-RecentDepartureDays);

        return e =>
            // 1) Durumu ilerleten şirket (OLS)
            (e.StatusId != null && onRoadStatusIds.Contains((long)e.StatusId))
            // 2) Durumu ilerletmeyen şirket (Avrora): tarihlerden oku
            || (!(e.StatusId != null && finishedStatusIds.Contains((long)e.StatusId))
                && e.ReleaseDate != null
                && e.ReleaseDate <= today
                && e.ReleaseDate >= earliestDeparture
                && (e.ReturnDate == null || e.ReturnDate >= today));
    }
}
