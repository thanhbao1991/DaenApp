using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SanPhamController : BaseApiController
{
    private readonly ISanPhamService _service;

    public SanPhamController(ISanPhamService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<SanPhamDto>>>> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Result<List<SanPhamDto>>.Success("Danh sách sản phẩm", list).WithAfter(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<SanPhamDto>>> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null
            ? Result<SanPhamDto>.Failure("Không tìm thấy sản phẩm.")
            : Result<SanPhamDto>.Success("Chi tiết sản phẩm", dto).WithId(id).WithAfter(dto);
    }

    [HttpPost]
    public async Task<ActionResult<Result<SanPhamDto>>> Create([FromBody] SanPhamDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result;
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Result<SanPhamDto>>> Update(Guid id, [FromBody] SanPhamDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result;
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result<SanPhamDto>>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return result;
    }
}