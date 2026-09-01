# Test koşucusu — `dotnet test` DOCKER İÇİNDE çalıştırır.
#
# NEDEN: entegrasyon testleri bir PostgreSQL'e ihtiyaç duyuyor. Ana
# bilgisayardan koşulduğunda bu, veritabanı portunun dışarı açılmasını
# gerektiriyordu. Konteyner Docker ağına katıldığında `postgres:5432` adresine
# doğrudan erişir ve HİÇBİR PORT AÇILMASI GEREKMEZ.
#
# İmaj çalıştırıldığında testleri koşar ve çıkar; kalıcı bir servis değildir.
FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /src

# Önce proje dosyaları: NuGet katmanı paket listesi değişmedikçe önbellekten gelir.
COPY Ols.sln ./
COPY src/OLS.API/OLS.API.csproj                         src/OLS.API/
COPY src/OLS.Business/OLS.Business.csproj               src/OLS.Business/
COPY src/OLS.DataAccess/OLS.DataAccess.csproj           src/OLS.DataAccess/
COPY tests/OLS.Business.Tests/OLS.Business.Tests.csproj tests/OLS.Business.Tests/
COPY tests/OLS.API.IntegrationTests/OLS.API.IntegrationTests.csproj tests/OLS.API.IntegrationTests/
# Ols.sln bu projeyi de referans veriyor; henuz hic test dosyasi yok ama
# csproj kopyalanmazsa cozum genelinde restore basarisiz oluyor.
COPY tests/OLS.DataAccess.Tests/OLS.DataAccess.Tests.csproj tests/OLS.DataAccess.Tests/
RUN dotnet restore

COPY src/   src/
COPY tests/ tests/

# Derleme imaj kurulumunda yapılır; konteyner her koşuşta yeniden derlemesin.
RUN dotnet build --no-restore -c Debug

# Testler kendi izole veritabanlarını (ols_scoped_inttest_*) açıp sildiği için
# bakım bağlantısı "postgres" veritabanına gider.
ENV TEST_DB_HOST=postgres \
    TEST_DB_PORT=5432

ENTRYPOINT ["dotnet", "test", "--no-build", "--nologo"]
