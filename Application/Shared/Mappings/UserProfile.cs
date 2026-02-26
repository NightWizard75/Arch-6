using Application.Features.Users.Commands.CreateUser;
using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Shared.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // ═══════════════════════════════════════════════════════
        // 1. Entity → DTO (для ответов API)
        // ═══════════════════════════════════════════════════════
        CreateMap<User, UserDto>();
        // Обратный маппинг не нужен — User создаётся через конструктор
        
        // Command → Entity (для создания)
        CreateMap<CreateUserCommand, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RegistrationDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());
    }
}
