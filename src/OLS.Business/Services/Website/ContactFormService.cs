using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Website;

/// <summary>
/// Tanıtım sitesi iletişim formu.
/// olsold: <c>Website\Contact\ContactFormController</c>
///
///   POST   /api/website/contact/form                 kimlik doğrulaması YOK
///   GET    /api/website/contact/form                 liste (sayfalı)
///   GET    /api/website/contact/form/{id}            okundu işaretler
///   PATCH  /api/website/contact/form/{id}/answered   cevaplandı bayrağı
///
/// DİKKAT: Bu rotalar <c>/api/v1</c> ALTINDA DEĞİL, doğrudan <c>/api</c>
/// altında ve <c>auth:api</c> dışında. Arayüz de <c>/api/website/contact/form</c>
/// çağırıyor (<c>FormsTable.vue</c>).
///
/// Yanıt zarfı da farklı: <c>{success, data}</c> / <c>{success, message, data}</c>.
/// </summary>
public interface IContactFormService
{
    Task<object> ListAsync(int page, string path, CancellationToken cancellationToken = default);

    /// <summary>Görüntülendiğinde okundu olarak işaretler (kaynaktaki yan etki).</summary>
    Task<ContactFormDto?> ShowAsync(long id, CancellationToken cancellationToken = default);

    Task<ContactFormDto> CreateAsync(ContactFormInput input, CancellationToken cancellationToken = default);

    Task<ContactFormDto?> SetAnsweredAsync(
        long id, bool isAnswered, CancellationToken cancellationToken = default);
}

public sealed class ContactFormInput
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Message { get; set; }
}

public sealed class ContactFormDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("first_name")] public string? FirstName { get; init; }
    [JsonPropertyName("last_name")] public string? LastName { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("message")] public string? Message { get; init; }
    [JsonPropertyName("is_read")] public bool IsRead { get; init; }
    [JsonPropertyName("is_answered")] public bool IsAnswered { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }
}

public sealed class ContactFormService : IContactFormService
{
    /// <summary>Kaynak sabit 10 kayıt sayfalıyor.</summary>
    private const int PageSize = 10;

    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public ContactFormService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<object> ListAsync(
        int page, string path, CancellationToken cancellationToken = default)
    {
        // Kaynak latest() → created_at DESC.
        var forms = _db.WebsiteContactForms.AsNoTracking()
            .OrderByDescending(f => f.CreatedAt)
            .Select(Project());

        return await forms.ToPagedOrListAsync(PageSize, page, path, cancellationToken);
    }

    public async Task<ContactFormDto?> ShowAsync(
        long id, CancellationToken cancellationToken = default)
    {
        var form = await _db.WebsiteContactForms
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (form is null)
            return null;

        if (!form.IsRead)
        {
            form.IsRead = true;
            form.UpdatedAt = _clock.Now;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return ToDto(form);
    }

    public async Task<ContactFormDto> CreateAsync(
        ContactFormInput input, CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;

        var form = new WebsiteContactForm
        {
            FirstName = input.FirstName ?? string.Empty,
            LastName = input.LastName ?? string.Empty,
            Email = input.Email ?? string.Empty,
            Phone = input.Phone,
            Message = input.Message ?? string.Empty,
            IsRead = false,
            IsAnswered = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.WebsiteContactForms.Add(form);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(form);
    }

    public async Task<ContactFormDto?> SetAnsweredAsync(
        long id, bool isAnswered, CancellationToken cancellationToken = default)
    {
        var form = await _db.WebsiteContactForms
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (form is null)
            return null;

        form.IsAnswered = isAnswered;
        form.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(form);
    }

    private static ContactFormDto ToDto(WebsiteContactForm form) => new()
    {
        Id = form.Id,
        FirstName = form.FirstName,
        LastName = form.LastName,
        Email = form.Email,
        Phone = form.Phone,
        Message = form.Message,
        IsRead = form.IsRead,
        IsAnswered = form.IsAnswered,
        CreatedAt = form.CreatedAt,
    };

    private static System.Linq.Expressions.Expression<Func<WebsiteContactForm, ContactFormDto>> Project() =>
        f => new ContactFormDto
        {
            Id = f.Id,
            FirstName = f.FirstName,
            LastName = f.LastName,
            Email = f.Email,
            Phone = f.Phone,
            Message = f.Message,
            IsRead = f.IsRead,
            IsAnswered = f.IsAnswered,
            CreatedAt = f.CreatedAt,
        };
}
