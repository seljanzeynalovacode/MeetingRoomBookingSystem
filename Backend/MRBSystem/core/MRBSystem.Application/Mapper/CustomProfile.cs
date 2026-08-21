using AutoMapper;
using MRBSystem.Contracts.DTOs.AppRoleDtos;
using MRBSystem.Contracts.DTOs.AppUserDtos;
using MRBSystem.Contracts.DTOs.BuildingDtos;
using MRBSystem.Contracts.DTOs.EquipmentDtos;
using MRBSystem.Contracts.DTOs.FloorDtos;
using MRBSystem.Contracts.DTOs.NotificationDtos;
using MRBSystem.Contracts.DTOs.ParticipantDtos;
using MRBSystem.Contracts.DTOs.ReservationDtos;
using MRBSystem.Contracts.DTOs.RoomBlackoutDtos;
using MRBSystem.Contracts.DTOs.RoomDtos;
using MRBSystem.Contracts.DTOs.RoomEquipmentDtos;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Enums;

namespace MRBSystem.Application.Mapper;

public class CustomProfile : Profile
{
    public CustomProfile()
    {
        //  AppUser 
        CreateMap<AppUser, UserResponseDto>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore());

        //  AppRole 
        CreateMap<AppRole, ResponseAppRoleDto>();
        CreateMap<CreateAppRoleDto, AppRole>(MemberList.Source);
        CreateMap<UpdateAppRoleDto, AppRole>(MemberList.Source);

        //  Building 
        CreateMap<Building, ResponseBuildingDto>();
        CreateMap<CreateBuildingDto, Building>(MemberList.Source);
        CreateMap<UpdateBuildingDto, Building>(MemberList.Source);

        //  Floor 
        CreateMap<Floor, ResponseFloorDto>()
            .ForMember(dest => dest.BuildingName, opt => opt.MapFrom(src => src.Building.Name));
        CreateMap<CreateFloorDto, Floor>(MemberList.Source);
        CreateMap<UpdateFloorDto, Floor>(MemberList.Source);

        //  Room 
        CreateMap<Room, ResponseRoomDto>()
            .ForMember(dest => dest.FloorName, opt => opt.MapFrom(src => src.Floor.Name))
            .ForMember(dest => dest.BuildingName, opt => opt.MapFrom(src => src.Floor.Building.Name));
        CreateMap<CreateRoomDto, Room>(MemberList.Source);
        CreateMap<UpdateRoomDto, Room>(MemberList.Source);


        //  Equipment 
        CreateMap<Equipment, ResponseEquipmentDto>();
        CreateMap<CreateEquipmentDto, Equipment>(MemberList.Source);
        CreateMap<UpdateEquipmentDto, Equipment>(MemberList.Source);

        //  RoomEquipment 
        CreateMap<RoomEquipment, ResponseRoomEquipmentDto>()
            .ForMember(dest => dest.EquipmentName, opt => opt.MapFrom(src => src.Equipment.Name))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room.Name));

        CreateMap<CreateRoomEquipmentDto, RoomEquipment>(MemberList.Source);
        CreateMap<UpdateRoomEquipmentDto, RoomEquipment>(MemberList.Source);

        //  RoomBlackout 
        CreateMap<RoomBlackout, ResponseRoomBlackoutDto>()
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room.Name));
        CreateMap<CreateRoomBlackoutDto, RoomBlackout>(MemberList.Source);
        CreateMap<UpdateRoomBlackoutDto, RoomBlackout>(MemberList.Source);


        //  Reservation 
        CreateMap<Reservation, ResponseReservationDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room.Name))
            .ForMember(dest => dest.ReservedByUserName,
                opt => opt.MapFrom(src => src.ReservedByUser.FirstName + " " + src.ReservedByUser.LastName));

        CreateMap<CreateReservationDto, Reservation>(MemberList.Source);
        CreateMap<UpdateReservationDto, Reservation>(MemberList.Source);

        
        //  Participant 
        CreateMap<Participant, ResponseParticipantDto>()
            .ForMember(dest => dest.ResponseStatus, opt => opt.MapFrom(src => src.ResponseStatus.ToString()))
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User != null
                    ? src.User.FirstName + " " + src.User.LastName
                    : null));

        CreateMap<CreateParticipantDto, Participant>(MemberList.Source);

        CreateMap<UpdateParticipantDto, Participant>(MemberList.Source)
    .ForMember(dest => dest.ResponseStatus,
        opt => opt.MapFrom(src => Enum.Parse<ResponseStatus>(src.ResponseStatus)))
    .ForSourceMember(src => src.ResponseStatus, opt => opt.DoNotValidate());

        //  Notification 
        CreateMap<Notification, ResponseNotificationDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));
    }
}