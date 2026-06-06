using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Admin.Users;

public sealed record UserDto(Guid Id, string UserName, string FullName, UserRole Role, bool IsActive);
public sealed record CreateUserRequest(string UserName, string FullName, UserRole Role, string Password);
public sealed record UpdateUserRequest(string FullName, UserRole Role, bool IsActive);
public sealed record ResetPasswordRequest(string Password);

public sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(4);
    }
}

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default);
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
    Task<Result> ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default);
}

public sealed class UserService : IUserService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default)
        => await _db.Users.AsNoTracking().OrderBy(u => u.UserName)
            .Select(u => new UserDto(u.Id, u.UserName, u.FullName, u.Role, u.IsActive)).ToListAsync(ct);

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var userName = request.UserName.Trim();
        if (await _db.Users.AnyAsync(u => u.UserName == userName, ct))
            return Result.Failure<UserDto>(Error.Conflict("Bu kullanıcı adı zaten var."));

        var entity = new User
        {
            UserName = userName,
            FullName = request.FullName.Trim(),
            Role = request.Role,
            IsActive = true,
            PasswordHash = _passwordHasher.Hash(request.Password)
        };
        _db.Users.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success(new UserDto(entity.Id, entity.UserName, entity.FullName, entity.Role, entity.IsActive));
    }

    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (entity is null)
            return Result.Failure<UserDto>(Error.NotFound("Kullanıcı bulunamadı."));

        entity.FullName = request.FullName.Trim();
        entity.Role = request.Role;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);
        return Result.Success(new UserDto(entity.Id, entity.UserName, entity.FullName, entity.Role, entity.IsActive));
    }

    public async Task<Result> ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (entity is null)
            return Result.Failure(Error.NotFound("Kullanıcı bulunamadı."));
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 4)
            return Result.Failure(Error.Validation("Parola en az 4 karakter olmalı."));

        entity.PasswordHash = _passwordHasher.Hash(request.Password);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
