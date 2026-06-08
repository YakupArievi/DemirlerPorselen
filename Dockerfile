# Toptanci.Api — Render/bulut için container imajı
# Build aşaması: SDK ile publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce sadece kaynak proje dosyalarını kopyala (katman önbelleği için restore'u ayır)
COPY src/ ./src/
RUN dotnet restore src/Toptanci.Api/Toptanci.Api.csproj
RUN dotnet publish src/Toptanci.Api/Toptanci.Api.csproj -c Release -o /app/publish --no-restore

# Çalışma aşaması: yalnızca ASP.NET runtime (küçük imaj)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render PORT env'i ile portu enjekte eder; Program.cs bunu okuyup 0.0.0.0:PORT dinler.
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "Toptanci.Api.dll"]
