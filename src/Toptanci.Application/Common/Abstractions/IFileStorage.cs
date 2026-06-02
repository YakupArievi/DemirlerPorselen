namespace Toptanci.Application.Common.Abstractions;

/// <summary>Dosya (görsel) depolama soyutlaması. Yerel disk veya bulut implementasyonu olabilir.</summary>
public interface IFileStorage
{
    /// <summary>Dosyayı kaydeder ve erişilebilir (göreli) URL döner.</summary>
    Task<string> SaveAsync(Stream content, string fileName, string folder, CancellationToken cancellationToken = default);
}
