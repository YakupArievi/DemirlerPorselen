using Microsoft.Extensions.Configuration;
using Toptanci.Application.Common.Abstractions;

namespace Toptanci.Infrastructure.Storage;

/// <summary>
/// Dosyaları yerel diske kaydeder (varsayılan: wwwroot/uploads). Statik dosya middleware'i ile sunulur.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;
    private readonly string _publicBase;

    public LocalFileStorage(IConfiguration configuration)
    {
        _rootPath = configuration["Storage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        _publicBase = configuration["Storage:PublicBaseUrl"] ?? "/uploads";
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string folder, CancellationToken cancellationToken = default)
    {
        var safeName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var folderPath = Path.Combine(_rootPath, "uploads", folder);
        Directory.CreateDirectory(folderPath);

        var fullPath = Path.Combine(folderPath, safeName);
        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(fs, cancellationToken);
        }

        return $"{_publicBase}/{folder}/{safeName}".Replace("\\", "/");
    }
}
