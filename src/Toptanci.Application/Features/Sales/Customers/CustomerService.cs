using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;

namespace Toptanci.Application.Features.Sales.Customers;

public sealed record CustomerLookupDto(Guid Id, string Name);

public interface ICustomerService
{
    /// <summary>Depocu satış ekranı için yalnızca id+ad (parasal bilgi YOK).</summary>
    Task<IReadOnlyList<CustomerLookupDto>> GetLookupAsync(string? search, CancellationToken ct = default);
    Task<PagedResult<CustomerDto>> GetAllAsync(CustomerQuery query, CancellationToken ct = default);
    Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result> SetPortalCredentialsAsync(Guid id, SetPortalCredentialsRequest request, CancellationToken ct = default);
}

public sealed class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public CustomerService(IApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<CustomerLookupDto>> GetLookupAsync(string? search, CancellationToken ct = default)
    {
        var q = _db.Customers.AsNoTracking().Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.Name.Contains(search));
        return await q.OrderBy(c => c.Name).Take(100)
            .Select(c => new CustomerLookupDto(c.Id, c.Name)).ToListAsync(ct);
    }

    public async Task<PagedResult<CustomerDto>> GetAllAsync(CustomerQuery query, CancellationToken ct = default)
    {
        var q = _db.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(c => c.Name.Contains(query.Search) || (c.Phone != null && c.Phone.Contains(query.Search)));
        if (query.IsActive is { } active)
            q = q.Where(c => c.IsActive == active);
        if (query.OnlyWithBalance == true)
            q = q.Where(c => c.Balance != 0);

        q = q.OrderBy(c => c.Name);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CustomerDto(c.Id, c.Name, c.Phone, c.Address, c.TaxNumber, c.Notes, c.OpeningBalance, c.Balance, c.IsActive))
            .ToListAsync(ct);

        return new PagedResult<CustomerDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await _db.Customers.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CustomerDto(c.Id, c.Name, c.Phone, c.Address, c.TaxNumber, c.Notes, c.OpeningBalance, c.Balance, c.IsActive))
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result.Failure<CustomerDto>(Error.NotFound("Müşteri bulunamadı."))
            : Result.Success(dto);
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var entity = new Customer
        {
            Name = request.Name.Trim(),
            Phone = request.Phone,
            Address = request.Address,
            TaxNumber = request.TaxNumber,
            Notes = request.Notes,
            OpeningBalance = request.OpeningBalance,
            Balance = request.OpeningBalance // açılışta bakiye = devir
        };
        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return Result.Failure<CustomerDto>(Error.NotFound("Müşteri bulunamadı."));

        // Açılış bakiyesi değişirse, güncel bakiyeyi farkla düzelt (hareketlere dokunmadan)
        var openingDelta = request.OpeningBalance - entity.OpeningBalance;

        entity.Name = request.Name.Trim();
        entity.Phone = request.Phone;
        entity.Address = request.Address;
        entity.TaxNumber = request.TaxNumber;
        entity.Notes = request.Notes;
        entity.OpeningBalance = request.OpeningBalance;
        entity.Balance += openingDelta;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return Result.Failure(Error.NotFound("Müşteri bulunamadı."));

        var hasSales = await _db.Sales.AnyAsync(s => s.CustomerId == id, ct);
        if (hasSales)
            return Result.Failure(Error.Conflict("Müşterinin satış geçmişi var, silinemez. Pasif yapabilirsiniz."));

        _db.Customers.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetPortalCredentialsAsync(Guid id, SetPortalCredentialsRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return Result.Failure(Error.NotFound("Müşteri bulunamadı."));

        if (!request.Enabled)
        {
            entity.PortalEnabled = false;
            entity.PasswordHash = null;
            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }

        var phone = string.IsNullOrWhiteSpace(request.Phone) ? entity.Phone : request.Phone.Trim();
        if (string.IsNullOrWhiteSpace(phone))
            return Result.Failure(Error.Validation("Portal girişi için telefon gerekli."));

        // Aynı telefonla portal açık başka müşteri olmamalı
        var clash = await _db.Customers.AnyAsync(c => c.Id != id && c.PortalEnabled && c.Phone == phone, ct);
        if (clash)
            return Result.Failure(Error.Conflict("Bu telefon başka bir portal hesabında kullanılıyor."));

        entity.Phone = phone;
        entity.PortalEnabled = true;
        entity.PasswordHash = _passwordHasher.Hash(request.Password);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
