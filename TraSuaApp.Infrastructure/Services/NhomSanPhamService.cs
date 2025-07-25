﻿using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class NhomSanPhamService : INhomSanPhamService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames[("NhomSanPham")];

    public NhomSanPhamService(AppDbContext context)
    {
        _context = context;
    }

    private NhomSanPhamDto ToDto(NhomSanPham entity)
    {
        return new NhomSanPhamDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            LastModified = entity.LastModified,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt
        };
    }

    public async Task<List<NhomSanPhamDto>> GetAllAsync()
    {
        var list = await _context.NhomSanPhams
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<NhomSanPhamDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.NhomSanPhams
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<NhomSanPhamDto>> CreateAsync(NhomSanPhamDto dto)
    {
        var entity = new NhomSanPham
        {
            Id = Guid.NewGuid(),
            Ten = StringHelper.NormalizeString(dto.Ten).Trim(),
            LastModified = DateTime.Now,
            CreatedAt = DateTime.Now,
            IsDeleted = false
        };

        _context.NhomSanPhams.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<NhomSanPhamDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<NhomSanPhamDto>> UpdateAsync(Guid id, NhomSanPhamDto dto)
    {
        var entity = await _context.NhomSanPhams.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
            return Result<NhomSanPhamDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<NhomSanPhamDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        var before = ToDto(entity);

        entity.Ten = StringHelper.NormalizeString(dto.Ten).Trim();
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<NhomSanPhamDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<NhomSanPhamDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.NhomSanPhams.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null || entity.IsDeleted)
            return Result<NhomSanPhamDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        return Result<NhomSanPhamDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<NhomSanPhamDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.NhomSanPhams.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null)
            return Result<NhomSanPhamDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<NhomSanPhamDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<NhomSanPhamDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<NhomSanPhamDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await _context.NhomSanPhams
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }
}