using AutoMapper;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Api.Helpers // ✅ chỉnh lại nếu khác
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<NhomSanPham, NhomSanPhamDto>().ReverseMap();
            CreateMap<SanPham, SanPhamDto>().ReverseMap();
            CreateMap<SanPhamBienThe, SanPhamBienTheDto>().ReverseMap();
            CreateMap<TaiKhoan, TaiKhoanDto>().ReverseMap();
        }
    }
}