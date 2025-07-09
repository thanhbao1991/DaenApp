using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Api.Extensions;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class KhachHangController : BaseApiController
{
    private readonly IKhachHangService _service;

    public KhachHangController(IKhachHangService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Result<List<KhachHangDto>>.Success("Danh sách khách hàng", result)
            .WithAfter(result)
            .ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        return result == null
            ? Result<KhachHangDto>.Failure("Không tìm thấy khách hàng.").ToActionResult()
            : Result<KhachHangDto>.Success("Chi tiết khách hàng", result)
                .WithId(id)
                .WithAfter(result)
                .ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] KhachHangDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] KhachHangDto dto)
    {
        var beforeResult = await _service.GetByIdAsync(id);
        var result = await _service.UpdateAsync(id, dto);

        return result.IsSuccess
            ? result.WithId(id).WithBefore(beforeResult).WithAfter(result.Data).ToActionResult()
            : result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var beforeResult = await _service.GetByIdAsync(id);
        var result = await _service.DeleteAsync(id);

        return result.IsSuccess
            ? result.WithId(id).WithBefore(beforeResult).ToActionResult()
            : result.ToActionResult();
    }
}