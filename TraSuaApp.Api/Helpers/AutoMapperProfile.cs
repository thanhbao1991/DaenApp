using AutoMapper;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Api.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Log, LogDto>();
            CreateMap<KhachHang, KhachHangDto>()
                .ForMember(dest => dest.Phones, opt => opt.MapFrom(src => src.KhachHangPhones))
                .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.KhachHangAddresses))
                .ReverseMap();
            CreateMap<KhachHangPhone, KhachHangPhoneDto>().ReverseMap();
            CreateMap<KhachHangAddress, KhachHangAddressDto>().ReverseMap();
            CreateMap<SanPham, SanPhamDto>()
                .ForMember(dest => dest.TenNhomSanPham, opt => opt.MapFrom(src => src.IdNhomSanPham != null ? src.IdNhomSanPham : null))
                .ReverseMap();
            CreateMap<SanPhamBienThe, SanPhamBienTheDto>().ReverseMap();
            CreateMap<Topping, ToppingDto>()
                .ForMember(dest => dest.IdNhomSanPham, opt => opt.MapFrom(src => src.IdNhomSanPhams.Select(nsp => nsp.Id)))
                .ReverseMap()
                .ForMember(dest => dest.IdNhomSanPhams, opt => opt.Ignore()); // Navigation map ngược thường cần xử lý tay
            CreateMap<NhomSanPham, NhomSanPhamDto>().ReverseMap();
            CreateMap<TaiKhoan, TaiKhoanDto>().ReverseMap();
        }
    }
}