using AutoMapper;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Api.Helpers // ✅ chỉnh lại nếu khác
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Topping, ToppingDto>().ReverseMap();
            CreateMap<NhomSanPham, NhomSanPhamDto>().ReverseMap();
            CreateMap<SanPhamBienThe, SanPhamBienTheDto>().ReverseMap();
            CreateMap<TaiKhoan, TaiKhoanDto>().ReverseMap();

            CreateMap<SanPham, SanPhamDto>()
         .ForMember(dest => dest.TenNhomSanPham, opt => opt.MapFrom(src => src.NhomSanPham != null ? src.NhomSanPham.Ten : null))
         .ReverseMap();
            CreateMap<Topping, ToppingDto>()
         .ForMember(dest => dest.IdNhomSanPham,
                    opt => opt.MapFrom(src => src.DanhSachNhomSanPham.Select(x => x.IdNhomSanPham)))
         .ReverseMap()
         .ForMember(dest => dest.DanhSachNhomSanPham, opt => opt.Ignore());

        }
    }
}