using System.Net.Http.Json;
using System.Text.Json;

namespace OLS.API.IntegrationTests;

/// <summary>
/// olsold <c>CarSave</c>/<c>CarUpdate</c>: <c>plate_number</c> dışında da 9 zorunlu alan
/// var (<c>car_type/romork_type/vehicle_owner/vehicle_status/customer_id/km/width/
/// length/height/capacity</c>). Bu yardımcı, yalnızca belirli bir alanla ilgilenen
/// testlerin geri kalan zorunlu alanları gerçek (seed edilmiş) lookup id'leriyle
/// doldurabilmesi için var — sabit/varsayımsal id kullanmak seed sırası değişirse kırılır.
/// </summary>
public static class TestCarHelper
{
    public static async Task<Dictionary<string, object?>> RequiredCarFieldsAsync(HttpClient admin) =>
        new()
        {
            ["car_type"] = await FirstLookupIdAsync(admin, "car_type"),
            ["romork_type"] = await FirstLookupIdAsync(admin, "romork_type"),
            ["vehicle_owner"] = await FirstLookupIdAsync(admin, "car_owner"),
            ["vehicle_status"] = await FirstLookupIdAsync(admin, "car_status"),
            ["customer_id"] = "TEST-CUSTOMER",
            ["km"] = 100,
            ["width"] = 2.5,
            ["length"] = 12,
            ["height"] = 3.2,
            ["capacity"] = 20,
        };

    public static async Task<int> FirstLookupIdAsync(HttpClient admin, string route)
    {
        var response = await admin.GetAsync($"/api/v1/{route}");
        response.EnsureSuccessStatusCode();
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        return data.EnumerateArray().First().GetProperty("id").GetInt32();
    }
}
