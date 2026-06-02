using Microsoft.AspNetCore.Mvc;
using Toptanci.Application.Common.Abstractions;

namespace Toptanci.Api.Controllers;

[Route("api/files")]
public sealed class FilesController : ApiControllerBase
{
    private readonly IFileStorage _storage;

    public FilesController(IFileStorage storage) => _storage = storage;

    /// <summary>Görsel/dosya yükler ve erişilebilir URL döner. (örn. kırık ürün fotoğrafı)</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string folder = "general", CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Dosya boş." });

        await using var stream = file.OpenReadStream();
        var url = await _storage.SaveAsync(stream, file.FileName, folder, ct);
        return Ok(new { url });
    }
}
