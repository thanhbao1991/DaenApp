
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Api.Controllers;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class TaiKhoanController : BaseApiController
{
    private readonly ITaiKhoanService _service;

    public TaiKhoanController(ITaiKhoanService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<TaiKhoanDto>>>> GetAll()
    {
        var data = await _service.GetAllAsync();
        return Result<List<TaiKhoanDto>>.Success("Danh sách tài khoản", data).WithAfter(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<TaiKhoanDto>>> GetByIdAsync(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null
            ? Result<TaiKhoanDto>.Failure("Không tìm thấy tài khoản.")
            : Result<TaiKhoanDto>.Success("Chi tiết tài khoản", dto).WithId(id).WithAfter(dto);
    }

    [HttpPost]
    public async Task<ActionResult<Result<TaiKhoanDto>>> Create([FromBody] TaiKhoanDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result;
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Result<TaiKhoanDto>>> Update(Guid id, [FromBody] TaiKhoanDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result;
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result<TaiKhoanDto>>> Delete(Guid id)
    {
        var response = new
        {
            status = 403,
            success = false,
            message = "Tính năng này bị khoá."
        };
        return StatusCode(StatusCodes.Status403Forbidden, response);

        var result = await _service.DeleteAsync(id);
        return result;
    }
}