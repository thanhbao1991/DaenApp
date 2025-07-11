﻿//using AutoMapper;
//using Microsoft.EntityFrameworkCore;
//using TraSuaApp.Domain.Entities;
//using TraSuaApp.Infrastructure.Data;
//using TraSuaApp.Shared.Dtos;
//using TraSuaApp.Shared.Helpers;

//public class LogService : ILogService
//{
//    private readonly AppDbContext _context;
//    private readonly IMapper _mapper;

//    public LogService(AppDbContext context, IMapper mapper)
//    {
//        _context = context;
//        _mapper = mapper;
//    }

//    public async Task LogAsync(Log log)
//    {
//        _context.Logs.Add(log);
//        await _context.SaveChangesAsync();
//    }

//    public async Task<Result<PagedResultDto<LogDto>>> GetLogsAsync(LogFilterDto filter)
//    {
//        var query = _context.Logs.AsQueryable();

//        if (!string.IsNullOrWhiteSpace(filter.UserName))
//            query = query.Where(x => x.UserName!.Contains(filter.UserName));

//        if (!string.IsNullOrWhiteSpace(filter.Path))
//            query = query.Where(x => x.Path!.Contains(filter.Path));

//        if (filter.StatusCode.HasValue)
//            query = query.Where(x => x.StatusCode == filter.StatusCode);

//        if (filter.TuNgay.HasValue)
//            query = query.Where(x => x.ThoiGian >= filter.TuNgay);

//        if (filter.DenNgay.HasValue)
//            query = query.Where(x => x.ThoiGian <= filter.DenNgay);

//        var total = await query.CountAsync();

//        var logs = await query
//            .OrderByDescending(x => x.ThoiGian)
//            .Skip((filter.Page - 1) * filter.PageSize)
//            .Take(filter.PageSize)
//            .ToListAsync();

//        var items = _mapper.Map<List<LogDto>>(logs);
//        var resultDto = new PagedResultDto<LogDto>(items, total);

//        return Result<PagedResultDto<LogDto>>.Success().WithAfter(resultDto);
//    }

//    public async Task<Result<LogDto>> GetLogByIdAsync(Guid id)
//    {
//        var entity = await _context.Logs.FindAsync(id);
//        if (entity == null)
//            return Result<LogDto>.Failure("Không tìm thấy log.");

//        var dto = _mapper.Map<LogDto>(entity);
//        return Result<LogDto>.Success().WithId(id).WithAfter(dto);
//    }
//}