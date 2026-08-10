namespace OLS.API.IntegrationTests;

/// <summary>
/// Bu koleksiyona giren tüm test sınıfları TEK bir OlsApiFactory (ve dolayısıyla
/// tek bir Postgres konteyneri) paylaşır. xUnit aynı koleksiyondaki testleri
/// sıralı çalıştırır, bu yüzden paylaşılan veritabanı durumu yarış koşuluna girmez.
/// </summary>
[CollectionDefinition("OlsApi")]
public sealed class OlsApiCollection : ICollectionFixture<OlsApiFactory>;
