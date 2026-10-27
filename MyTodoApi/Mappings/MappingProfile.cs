using AutoMapper;
using MyTodoApi.DTO;
using MyTodoApi.Models;

namespace MyTodoApi.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Resident, ResidentDTO>().ReverseMap();
        
        CreateMap<ResidentDTO, Resident>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Workspace, WorkspaceDTO>().ReverseMap();
        
        CreateMap<UpdateWorkspaceDTO, Workspace>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


        CreateMap<Booking, BookingDTO>().ReverseMap();
        

        CreateMap<UpdateBookingDTO, Booking>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}