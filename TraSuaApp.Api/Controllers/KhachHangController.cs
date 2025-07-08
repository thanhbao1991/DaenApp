using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Shared.Dtos;

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
        return Result.Success().WithAfter(result).ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? Result.Failure("Không tìm thấy khách hàng.").ToActionResult()
            : Result.Success().WithId(id).WithAfter(result).ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] KhachHangDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return Result.Success("Đã thêm khách hàng.")
                .WithId(result.Id)
                .WithAfter(result)
                .ToActionResult();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message).ToActionResult();
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] KhachHangDto dto)
    {
        var before = await _service.GetByIdAsync(id);
        var result = await _service.UpdateAsync(id, dto);

        return result.IsSuccess
            ? result.WithId(id).WithBefore(before).WithAfter(dto).ToActionResult()
            : result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var before = await _service.GetByIdAsync(id);
        var result = await _service.DeleteAsync(id);

        return result.IsSuccess
            ? result.WithId(id).WithBefore(before).ToActionResult()
            : result.ToActionResult();
    }
}