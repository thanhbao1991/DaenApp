public class CodeGenService
{
    public string EntityName { get; set; } = "";
    public string EntityNamespace { get; set; } = "";
    public string DtoNamespace { get; set; } = "TraSuaApp.Shared.Dtos";
    public string InterfaceNamespace { get; set; } = "TraSuaApp.Application.Interfaces";
    public string DbContextName { get; set; } = "AppDbContext";
    public string GenerateAddScopedService()
    {
        return $"builder.Services.AddScoped<I{EntityName}Service, {EntityName}Service>();";
    }
    public string GenerateServiceInterface()
    {
        return
$@"using {DtoNamespace};

namespace {InterfaceNamespace};

public interface I{EntityName}Service
{{
    Task<List<{EntityName}Dto>> GetAllAsync();
    Task<{EntityName}Dto?> GetByIdAsync(Guid id);
    Task<{EntityName}Dto> CreateAsync({EntityName}Dto dto);
    Task<bool> UpdateAsync(Guid id, {EntityName}Dto dto);
    Task<bool> DeleteAsync(Guid id);
}}";
    }

    public string GenerateServiceImplementation()
    {
        return
$@"using AutoMapper;
using Microsoft.EntityFrameworkCore;
using {InterfaceNamespace};
using {EntityNamespace};
using TraSuaApp.Infrastructure.Data;
using {DtoNamespace};

namespace TraSuaApp.Infrastructure.Services;

public class {EntityName}Service : I{EntityName}Service
{{
    private readonly {DbContextName} _context;
    private readonly IMapper _mapper;

    public {EntityName}Service({DbContextName} context, IMapper mapper)
    {{
        _context = context;
        _mapper = mapper;
    }}

    public async Task<List<{EntityName}Dto>> GetAllAsync()
    {{
        var list = await _context.{EntityNamePlural()}.ToListAsync();
        return _mapper.Map<List<{EntityName}Dto>>(list);
    }}

    public async Task<{EntityName}Dto?> GetByIdAsync(Guid id)
    {{
        var entity = await _context.{EntityNamePlural()}.FindAsync(id);
        return entity == null ? null : _mapper.Map<{EntityName}Dto>(entity);
    }}

    public async Task<{EntityName}Dto> CreateAsync({EntityName}Dto dto)
    {{
        var entity = _mapper.Map<{EntityName}>(dto);
        entity.Id = Guid.NewGuid();
        _context.{EntityNamePlural()}.Add(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<{EntityName}Dto>(entity);
    }}

    public async Task<bool> UpdateAsync(Guid id, {EntityName}Dto dto)
    {{
        var entity = await _context.{EntityNamePlural()}.FindAsync(id);
        if (entity == null) return false;

        _mapper.Map(dto, entity);
        await _context.SaveChangesAsync();
        return true;
    }}

    public async Task<bool> DeleteAsync(Guid id)
    {{
        var entity = await _context.{EntityNamePlural()}.FindAsync(id);
        if (entity == null) return false;

        _context.{EntityNamePlural()}.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }}
}}";
    }

    public string GenerateController()
    {
        return
$@"using Microsoft.AspNetCore.Mvc;
using {InterfaceNamespace};
using {DtoNamespace};

namespace TraSuaApp.Api.Controllers;

[ApiController]
[Route(""api/[controller]"")]
public class {EntityName}Controller : ControllerBase
{{
    private readonly I{EntityName}Service _service;

    public {EntityName}Controller(I{EntityName}Service service)
    {{
        _service = service;
    }}

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet(""{{id}}"")]
    public async Task<IActionResult> GetById(Guid id)
    {{
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }}

    [HttpPost]
    public async Task<IActionResult> Create({EntityName}Dto dto)
        => Ok(await _service.CreateAsync(dto));

    [HttpPut(""{{id}}"")]
    public async Task<IActionResult> Update(Guid id, {EntityName}Dto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete(""{{id}}"")]
    public async Task<IActionResult> Delete(Guid id)
        => Ok(await _service.DeleteAsync(id));
}}";
    }

    public string GenerateDto()
    {
        return
$@"namespace {DtoNamespace};

public class {EntityName}Dto
{{
    public Guid Id {{ get; set; }}
    public int? IdOld {{ get; set; }}
    public string Ten {{ get; set; }} = string.Empty;

    public int STT {{ get; set; }}
    public string? TenNormalized {{ get; set; }}
}}";
    }

    public string GenerateAutoMapperProfileEntry()
    {
        return
$@"CreateMap<{EntityName}, {EntityName}Dto>().ReverseMap();";
    }

    private string EntityNamePlural()
    {
        if (EntityName.EndsWith("s"))
            return EntityName + "es";
        else
            return EntityName + "s";
    }
}