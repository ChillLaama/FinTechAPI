using AutoMapper;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Domain.Models;

namespace FinTechAPI.Application.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Account, AccountDto>();
            CreateMap<Transaction, TransactionDto>()
                .ForMember(dest => dest.BusinessStatus, opt => opt.MapFrom(src => src.Status));
        }
    }
}
