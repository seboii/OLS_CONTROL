using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Loads;

/// <summary>
/// Müşteriye teklif e-postası — <c>POST /offer_send_email</c>.
/// olsold: <c>Front\OfferEmail\OfferEmailController::save</c> +
/// <c>resources/views/emails/front/offer_customer_email.blade.php</c>
///
/// Kaynak, teklifi tüm ilişkileriyle yükleyip blade şablonunu render ediyor ve
/// <b>müşterinin e-posta adresine</b> gönderiyor.
///
/// > DURUM: Gerçek gönderim <b>varsayılan olarak KAPALI</b>. Bu uç dışarıya
/// > gerçek e-posta çıkaran bir entegrasyon; canlı entegrasyonlar
/// > (Uyumsoft / IMAP / SMS) gibi açıkça ertelendi. Açmak için
/// > <c>Mail:Enabled=true</c> ve <c>Mail:Host</c>/<c>Port</c>/<c>Username</c>/
/// > <c>Password</c>/<c>From</c> ayarlanmalı. Kapalıyken uç teklifi bulur,
/// > gövdeyi üretir ve "gönderim kapalı" bilgisini döner — böylece şablon
/// > çıktısı gönderim yapmadan doğrulanabilir.
///
/// > ŞABLON NOTU: 771 satırlık blade birebir kopyalanmadı; aynı alanları
/// > (teklif başlığı, güzergâh, sorumlular, mali kalemler, yük içeriği)
/// > taşıyan eşdeğer bir HTML üretilir.
/// </summary>
public interface IOfferEmailService
{
    Task<OfferEmailResult> SendAsync(long loadId, CancellationToken cancellationToken = default);
}

public sealed record OfferEmailResult(bool Sent, string Message, string? Recipient, string? Html);

public sealed class OfferEmailService : IOfferEmailService
{
    private readonly OlsDbContext _db;
    private readonly IConfiguration _configuration;

    public OfferEmailService(OlsDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<OfferEmailResult> SendAsync(
        long loadId, CancellationToken cancellationToken = default)
    {
        var load = await _db.Loads.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == loadId, cancellationToken);

        if (load is null)
            return new OfferEmailResult(false, "Kayıt Bulunamadı", null, null);

        var customer = await _db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == load.CustomerId, cancellationToken);

        if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
            return new OfferEmailResult(false, "Müşterinin e-posta adresi yok.", null, null);

        var html = await BuildHtmlAsync(load.Id, cancellationToken);

        if (!_configuration.GetValue("Mail:Enabled", false))
            return new OfferEmailResult(
                false, "E-posta gönderimi kapalı (Mail:Enabled=false).", customer.Email, html);

        await SendSmtpAsync(customer.Email, "Teklif Talebi Formu", html, cancellationToken);

        return new OfferEmailResult(true, "Teklif maili gönderildi.", customer.Email, null);
    }

    private async Task SendSmtpAsync(
        string to, string subject, string html, CancellationToken cancellationToken)
    {
        var host = _configuration["Mail:Host"] ?? throw new InvalidOperationException("Mail:Host tanımlı değil.");
        var port = _configuration.GetValue("Mail:Port", 587);
        var from = _configuration["Mail:From"] ?? throw new InvalidOperationException("Mail:From tanımlı değil.");

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = _configuration.GetValue("Mail:UseSsl", true),
            Credentials = new NetworkCredential(
                _configuration["Mail:Username"], _configuration["Mail:Password"]),
        };

        using var message = new MailMessage(from, to, subject, html) { IsBodyHtml = true };

        await client.SendMailAsync(message, cancellationToken);
    }

    /// <summary>
    /// Teklif gövdesi. Blade şablonunun okuduğu alanların hepsi burada:
    /// teklif no/tarih, iş türü, güzergâh, cari/gönderici, sorumlu kişiler,
    /// mali kalemler ve yük içeriği.
    /// </summary>
    private async Task<string> BuildHtmlAsync(long loadId, CancellationToken cancellationToken)
    {
        var header = await (
            from l in _db.Loads.AsNoTracking()
            where l.Id == loadId
            select new
            {
                l.ReservationNumber,
                l.OfferDate,
                l.Description,
                WorkType = _db.WorkTypes.Where(w => w.Id == l.WorkTypeId).Select(w => w.Name).FirstOrDefault(),
                Customer = _db.Accounts.Where(a => a.Id == l.CustomerId).Select(a => a.Name).FirstOrDefault(),
                Sender = _db.Accounts.Where(a => a.Id == l.SenderId).Select(a => a.Name).FirstOrDefault(),
                Departure = _db.Countries.Where(c => c.Id == l.DepartureCountryId).Select(c => c.Name).FirstOrDefault(),
                Transit = _db.Countries.Where(c => c.Id == l.TransitCountryId).Select(c => c.Name).FirstOrDefault(),
                Target = _db.Countries.Where(c => c.Id == l.TargetCountryId).Select(c => c.Name).FirstOrDefault(),
            })
            .FirstAsync(cancellationToken);

        var people = await (
            from p in _db.LoadChargePeople.AsNoTracking()
            where p.LoadId == (int)loadId
            join u in _db.Users.AsNoTracking() on (long?)p.UserId equals u.Id
            select new { p.UserType, u.Name, u.Surname, u.Email, u.Phone })
            .ToListAsync(cancellationToken);

        var items = await (
            from i in _db.LoadFinancialItems.AsNoTracking()
            where i.LoadId == loadId
            select new
            {
                Item = _db.FinancialItems.Where(f => f.Id == i.Item).Select(f => f.Name).FirstOrDefault(),
                ItemType = _db.ItemTypes.Where(t => t.Id == i.ItemTypeId).Select(t => t.Name).FirstOrDefault(),
                Symbol = _db.Currencies.Where(c => c.Id == i.Currency).Select(c => c.Symbol).FirstOrDefault(),
                i.Quantity,
                i.NetPrice,
                i.TaxPrice,
                i.TotalPrice,
            })
            .ToListAsync(cancellationToken);

        var contents = await (
            from c in _db.LoadContents.AsNoTracking()
            where c.LoadId == loadId
            select new
            {
                CaseType = _db.CaseTypes.Where(t => t.Id == c.CaseTypeId).Select(t => t.Name).FirstOrDefault(),
                ProductType = _db.ProductTypes.Where(t => t.Id == c.ProductTypeId).Select(t => t.Name).FirstOrDefault(),
                c.Quantity,
                c.NetWeight,
                c.Volume,
            })
            .ToListAsync(cancellationToken);

        var tr = new CultureInfo("tr-TR");
        var html = new StringBuilder();

        html.Append("<html><body style=\"font-family:Arial,Helvetica,sans-serif;color:#222\">");
        html.Append("<h2>Teklif Talebi Formu</h2>");

        html.Append("<table cellpadding=\"6\" cellspacing=\"0\" border=\"1\" style=\"border-collapse:collapse\">");
        Row(html, "Teklif No", header.ReservationNumber);
        Row(html, "Teklif Tarihi", header.OfferDate?.ToString("dd.MM.yyyy", tr));
        Row(html, "İş Türü", header.WorkType);
        Row(html, "Müşteri", header.Customer);
        Row(html, "Gönderici", header.Sender);
        Row(html, "Çıkış Ülkesi", header.Departure);
        Row(html, "Transit Ülke", header.Transit);
        Row(html, "Varış Ülkesi", header.Target);
        Row(html, "Açıklama", header.Description);
        html.Append("</table>");

        if (people.Count > 0)
        {
            html.Append("<h3>İlgili Kişiler</h3>");
            html.Append("<table cellpadding=\"6\" cellspacing=\"0\" border=\"1\" style=\"border-collapse:collapse\">");
            html.Append("<tr><th>Görev</th><th>Ad Soyad</th><th>E-posta</th><th>Telefon</th></tr>");

            foreach (var person in people)
            {
                html.Append("<tr>");
                Cell(html, person.UserType == 1 ? "Yükleyen" : "Sorumlu");
                Cell(html, $"{person.Name} {person.Surname}".Trim());
                Cell(html, person.Email);
                Cell(html, person.Phone);
                html.Append("</tr>");
            }

            html.Append("</table>");
        }

        if (items.Count > 0)
        {
            html.Append("<h3>Mali Kalemler</h3>");
            html.Append("<table cellpadding=\"6\" cellspacing=\"0\" border=\"1\" style=\"border-collapse:collapse\">");
            html.Append("<tr><th>Kalem</th><th>Tip</th><th>Adet</th><th>Net Tutar</th><th>Vergi</th><th>Toplam</th></tr>");

            foreach (var item in items)
            {
                html.Append("<tr>");
                Cell(html, item.Item);
                Cell(html, item.ItemType);
                Cell(html, item.Quantity?.ToString(tr));
                Cell(html, Money(item.NetPrice, item.Symbol, tr));
                Cell(html, Money(item.TaxPrice, item.Symbol, tr));
                Cell(html, Money(item.TotalPrice, item.Symbol, tr));
                html.Append("</tr>");
            }

            html.Append("</table>");
        }

        if (contents.Count > 0)
        {
            html.Append("<h3>Yük İçeriği</h3>");
            html.Append("<table cellpadding=\"6\" cellspacing=\"0\" border=\"1\" style=\"border-collapse:collapse\">");
            html.Append("<tr><th>Ambalaj</th><th>Ürün</th><th>Adet</th><th>Net Ağırlık</th><th>Hacim</th></tr>");

            foreach (var content in contents)
            {
                html.Append("<tr>");
                Cell(html, content.CaseType);
                Cell(html, content.ProductType);
                Cell(html, content.Quantity?.ToString(tr));
                Cell(html, content.NetWeight?.ToString(tr));
                Cell(html, content.Volume?.ToString(tr));
                html.Append("</tr>");
            }

            html.Append("</table>");
        }

        html.Append("</body></html>");

        return html.ToString();
    }

    private static string Money(decimal? value, string? symbol, CultureInfo culture) =>
        value is null ? string.Empty : $"{value.Value.ToString("N2", culture)} {symbol}".Trim();

    private static void Row(StringBuilder html, string label, string? value)
    {
        html.Append("<tr><th align=\"left\">").Append(WebUtility.HtmlEncode(label)).Append("</th>");
        Cell(html, value);
        html.Append("</tr>");
    }

    private static void Cell(StringBuilder html, string? value) =>
        html.Append("<td>").Append(WebUtility.HtmlEncode(value ?? string.Empty)).Append("</td>");
}
