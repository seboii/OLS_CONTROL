using System.Net.Http.Json;
using System.Text.Json;

namespace OLS.API.IntegrationTests;

/// <summary>
/// olsold <c>FrontAccountController\RequestSave</c>/<c>RequestUpdate</c>: <c>name</c>,
/// <c>country_id</c> ve <c>discount</c> ikisinde de zorunlu. Bu yardımcı, yalnızca ada
/// önem veren testlerin geri kalan 2 zorunlu alanı gerçek (seed edilmiş) bir ülke
/// id'siyle doldurabilmesi için var.
/// </summary>
public static class TestAccountHelper
{
    public static async Task<MultipartFormDataContent> MinimalAccountFormAsync(
        HttpClient admin, string name, long? id = null)
    {
        var countryId = await FirstCountryIdAsync(admin);

        var form = new MultipartFormDataContent
        {
            { new StringContent(name), "name" },
            { new StringContent(countryId), "country_id" },
            { new StringContent("0"), "discount" },
        };
        if (id is not null)
            form.Add(new StringContent(id.Value.ToString()), "id");

        return form;
    }

    private static async Task<string> FirstCountryIdAsync(HttpClient admin)
    {
        var response = await admin.GetAsync("/api/v1/country");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").EnumerateArray().First()
            .GetProperty("id").GetGuid().ToString();
    }
}
