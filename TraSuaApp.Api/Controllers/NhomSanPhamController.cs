using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NhomSanPhamController : BaseApiController
{
    private readonly INhomSanPhamService _service;

    public NhomSanPhamController(INhomSanPhamService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<NhomSanPhamDto>>>> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Result<List<NhomSanPhamDto>>.Success("Danh sách nhóm sản phẩm", list).WithAfter(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<NhomSanPhamDto>>> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null
            ? Result<NhomSanPhamDto>.Failure("Không tìm thấy nhóm sản phẩm.")
            : Result<NhomSanPhamDto>.Success("Chi tiết nhóm sản phẩm", dto).WithId(id).WithAfter(dto);
    }

    [HttpPost]
    public async Task<ActionResult<Result<NhomSanPhamDto>>> Create([FromBody] NhomSanPhamDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result;
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Result<NhomSanPhamDto>>> Update(Guid id, [FromBody] NhomSanPhamDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result;
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result<NhomSanPhamDto>>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return result;
    }
}