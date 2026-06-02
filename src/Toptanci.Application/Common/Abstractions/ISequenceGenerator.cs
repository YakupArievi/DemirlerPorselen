namespace Toptanci.Application.Common.Abstractions;

/// <summary>SQL Server sequence'lerinden bir sonraki değeri atomik olarak üretir.</summary>
public interface ISequenceGenerator
{
    Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default);
}
