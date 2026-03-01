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
            CreateMap<Transaction, TransactionDto>();
        }
    }
}
