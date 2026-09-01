# OLS.API - ASP.NET Core 9
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Once proje dosyalari: restore katmani NuGet degismedikce onbellekten gelsin
COPY Ols.sln ./
COPY src/OLS.API/OLS.API.csproj src/OLS.API/
COPY src/OLS.Business/OLS.Business.csproj src/OLS.Business/
COPY src/OLS.DataAccess/OLS.DataAccess.csproj src/OLS.DataAccess/
RUN dotnet restore src/OLS.API/OLS.API.csproj

COPY src/ src/
RUN dotnet publish src/OLS.API/OLS.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Yuklenen evraklar (olimpikgama'dan tasinan 2 GB dosya) buraya baglanir
RUN mkdir -p /app/storage/app/public

COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Root olmayan kullanici
RUN useradd -m -u 1001 olsuser && chown -R olsuser:olsuser /app
USER olsuser

ENTRYPOINT ["dotnet", "OLS.API.dll"]
