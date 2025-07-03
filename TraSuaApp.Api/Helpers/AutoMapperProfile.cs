using AutoMapper;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Api.Helpers // ✅ chỉnh lại nếu khác
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<SanPham, SanPhamDto>().ReverseMap();
            CreateMap<SanPhamBienThe, SanPhamBienTheDto>().ReverseMap();
        }
    }
}