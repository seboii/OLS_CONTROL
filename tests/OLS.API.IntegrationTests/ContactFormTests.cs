using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Destek Talebi = Website Contact Form (olsold'da ayrı bir ticket/support modülü
/// yok — bkz. docs/SECILI-MODUL-PARITE-MATRISI.md §8). Kaynakta (ContactFormController)
/// admin uçları (index/show/updateAnsweredStatus) TAMAMEN anonimdi — gerçek bir güvenlik
/// açığı (SEC-003) olarak belgelendi ve bilinçli olarak KOPYALANMADI: bu portta
/// [RequiresPermission] ile gerçek bir support_request_management yetkisi var. Ayrıca
/// kaynağın FormsTable.vue'sindeki arama kutusu backend'in index()'i hiçbir istek
/// parametresi okumadığı için görsel olarak var ama işlevsiz bir kalıntıydı — burada
/// bilinçli olarak gerçek arama eklendi. Bu testler her iki sapmayı da kilitliyor.
/// </summary>
[Collection("OlsApi")]
public sealed class ContactFormTests
{
    private readonly OlsApiFactory _factory;

    public ContactFormTests(OlsApiFactory factory) => _factory = factory;

    /// <summary>
    /// "public-form" politikası dakikada 5 istekle sınırlı (SEC-009 ile aynı desen);
    /// bu sınıftaki testlerin toplamı bunu aşabiliyor. OlsApiFactory.LoginAsync'teki
    /// aynı yaklaşım: pencereyi (60sn) aşan TEK bir bekleyiş bir sonraki pencereye
    /// geçişi garanti eder, limiti gevşetmek yerine testi ona dayanıklı yaparız.
    /// </summary>
    private static async Task<long> SubmitAsync(
        HttpClient anonymous, string firstName, string lastName, string email, string message, string? phone = null)
    {
        var payload = new { first_name = firstName, last_name = lastName, email, phone, message };
        var response = await anonymous.PostAsJsonAsync("/api/website/contact/form", payload);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            await Task.Delay(TimeSpan.FromSeconds(65));
            response = await anonymous.PostAsJsonAsync("/api/website/contact/form", payload);
        }

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        return body.GetProperty("data").GetProperty("id").GetInt64();
    }

    [Fact]
    public async Task Store_IsAnonymous_AndCreatesWithReadAndAnsweredFalse()
    {
        using var anonymous = _factory.CreateClient();
        var email = $"ayse-{Guid.NewGuid():N}@example.test";
        var id = await SubmitAsync(anonymous, "Ayşe", "Kaya", email, "Merhaba, bir sorum var.");

        using var admin = await _factory.CreateAdminClientAsync();

        // NOT: show/detay ucu "görüntülendiğinde okundu işaretle" yan etkisi taşıdığı
        // için başlangıç durumunu ONUNLA kontrol ETMİYORUZ (kendi kendini bozar) —
        // yan etkisiz liste ucundan okuyoruz.
        var listed = await ListAsync(admin, email);
        var row = listed.EnumerateArray().Single();
        row.GetProperty("is_read").GetBoolean().Should().BeFalse();
        row.GetProperty("is_answered").GetBoolean().Should().BeFalse();
        row.GetProperty("created_at").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Show_AsAdmin_MarksAsReadAsASideEffect()
    {
        using var anonymous = _factory.CreateClient();
        var email = $"mehmet-{Guid.NewGuid():N}@example.test";
        var id = await SubmitAsync(anonymous, "Mehmet", "Demir", email, "Okundu testi.");

        using var admin = await _factory.CreateAdminClientAsync();

        // Başlangıç durumunu yan etkisiz liste ucundan doğrula (show'un kendisi
        // görüntülemede okundu işaretlediği için başlangıcı ONUNLA kontrol edemeyiz).
        var beforeView = await ListAsync(admin, email);
        beforeView.EnumerateArray().Single().GetProperty("is_read").GetBoolean().Should().BeFalse();

        // Salt görüntüleme (GET) bile okundu bayrağını true yapmalı — kaynağın yan etkisi.
        // Bu ilk show çağrısının YANITI da (kaynaktaki $contact->update() sonrası aynı
        // instance'ı serialize eden davranışla birebir) zaten true göstermeli.
        var afterFirstView = await GetDetailAsync(admin, id);
        afterFirstView.GetProperty("is_read").GetBoolean().Should().BeTrue();

        // İkinci görüntüleme idempotent olmalı.
        (await GetDetailAsync(admin, id)).GetProperty("is_read").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAnswered_AsAdmin_TogglesFlagBothWays()
    {
        using var anonymous = _factory.CreateClient();
        var id = await SubmitAsync(anonymous, "Zeynep", "Şahin", $"zeynep-{Guid.NewGuid():N}@example.test", "Yanıtlanma testi.");

        using var admin = await _factory.CreateAdminClientAsync();

        var answeredResponse = await admin.PatchAsJsonAsync($"/api/website/contact/form/{id}/answered", new { is_answered = true });
        answeredResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetDetailAsync(admin, id)).GetProperty("is_answered").GetBoolean().Should().BeTrue();

        var revertResponse = await admin.PatchAsJsonAsync($"/api/website/contact/form/{id}/answered", new { is_answered = false });
        revertResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetDetailAsync(admin, id)).GetProperty("is_answered").GetBoolean().Should().BeFalse();
    }

    /// <summary>
    /// Regresyon: FormsTable.vue'nin arama kutusu kaynakta görsel-ama-işlevsiz bir
    /// kalıntıydı (index() hiçbir parametre okumuyordu). Burada eklenen gerçek arama,
    /// ad/soyad/e-posta/telefon/mesaj alanlarının HEPSİNDE eşleşmeli.
    /// </summary>
    [Fact]
    public async Task List_WithSearch_FiltersAcrossNameEmailPhoneAndMessage()
    {
        using var anonymous = _factory.CreateClient();
        var uniqueTag = Guid.NewGuid().ToString("N")[..8];
        var targetEmail = $"hedef-{uniqueTag}@example.test";
        await SubmitAsync(anonymous, "Aranan", "Kullanici", targetEmail, $"Sıradan mesaj {uniqueTag}.");
        await SubmitAsync(anonymous, "Alakasiz", "Kisi", $"other-{Guid.NewGuid():N}@example.test", "Bambaşka bir mesaj.");

        using var admin = await _factory.CreateAdminClientAsync();

        var byEmail = await ListAsync(admin, uniqueTag);
        byEmail.EnumerateArray().Should().ContainSingle(f => f.GetProperty("email").GetString() == targetEmail);

        var byMessage = await ListAsync(admin, $"Sıradan mesaj {uniqueTag}");
        byMessage.EnumerateArray().Should().ContainSingle(f => f.GetProperty("email").GetString() == targetEmail);

        var byUnrelatedTerm = await ListAsync(admin, $"hic-eslesmeyecek-{uniqueTag}");
        byUnrelatedTerm.EnumerateArray().Should().NotContain(f => f.GetProperty("email").GetString() == targetEmail);
    }

    [Fact]
    public async Task AdminEndpoints_WithoutAuthentication_Return401()
    {
        using var anonymous = _factory.CreateClient();
        var id = await SubmitAsync(anonymous, "Kimliksiz", "Erisim", $"anon-{Guid.NewGuid():N}@example.test", "Yetkisiz erişim testi.");

        (await anonymous.GetAsync("/api/website/contact/form")).StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "kaynakta bu uç tamamen anonimdi (SEC-003) — bilinçli olarak kopyalanmadı");
        (await anonymous.GetAsync($"/api/website/contact/form/{id}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.PatchAsJsonAsync($"/api/website/contact/form/{id}/answered", new { is_answered = true }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<JsonElement> GetDetailAsync(HttpClient admin, long id)
    {
        var response = await admin.GetAsync($"/api/website/contact/form/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data");
    }

    private static async Task<JsonElement> ListAsync(HttpClient admin, string search)
    {
        var response = await admin.GetAsync($"/api/website/contact/form?search={Uri.EscapeDataString(search)}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("data");
    }
}
