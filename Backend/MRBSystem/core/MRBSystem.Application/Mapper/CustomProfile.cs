using AutoMapper;
using MRBSystem.Contracts.DTOs.AppRoleDtos;
using MRBSystem.Contracts.DTOs.AppUserDtos;
using MRBSystem.Contracts.DTOs.BuildingDtos;
using MRBSystem.Contracts.DTOs.CheckinDtos;
using MRBSystem.Contracts.DTOs.CommentDtos;
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
        // ===== AppUser =====
        CreateMap<AppUser, UserResponseDto>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore()); // UserManager ile ayrica doldurulur

        // ===== AppRole =====
        CreateMap<AppRole, ResponseAppRoleDto>();
        CreateMap<CreateAppRoleDto, AppRole>();
        CreateMap<UpdateAppRoleDto, AppRole>();

        // ===== Building =====
        CreateMap<Building, ResponseBuildingDto>();
        CreateMap<CreateBuildingDto, Building>();
        CreateMap<UpdateBuildingDto, Building>();

        // ===== Floor =====
        CreateMap<Floor, ResponseFloorDto>()
            .ForMember(dest => dest.BuildingName, opt => opt.MapFrom(src => src.Building.Name));
        CreateMap<CreateFloorDto, Floor>();
        CreateMap<UpdateFloorDto, Floor>();

        // ===== Room =====
        CreateMap<Room, ResponseRoomDto>()
            .ForMember(dest => dest.FloorName, opt => opt.MapFrom(src => src.Floor.Name))
            .ForMember(dest => dest.BuildingName, opt => opt.MapFrom(src => src.Floor.Building.Name));
        CreateMap<CreateRoomDto, Room>();
        CreateMap<UpdateRoomDto, Room>();

        // ResponseAvailableRoomDto - availability sorgusunda service/repository
        // tereifnden custom quraaraq doldurulur (Equipment siyahisi, tarix filtri ile
        // baglidir), birbasa Entity -> Dto map-i mentiqli deyil, service-de elde qurulacaq

        // ===== Equipment =====
        CreateMap<Equipment, ResponseEquipmentDto>();
        CreateMap<CreateEquipmentDto, Equipment>();
        CreateMap<UpdateEquipmentDto, Equipment>();

        // ===== RoomEquipment =====
        CreateMap<RoomEquipment, ResponseRoomEquipmentDto>()
            .ForMember(dest => dest.EquipmentName, opt => opt.MapFrom(src => src.Equipment.Name));
        CreateMap<CreateRoomEquipmentDto, RoomEquipment>();
        CreateMap<UpdateRoomEquipmentDto, RoomEquipment>();

        // ===== RoomBlackout =====
        CreateMap<RoomBlackout, ResponseRoomBlackoutDto>()
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room.Name));
        CreateMap<CreateRoomBlackoutDto, RoomBlackout>();
        CreateMap<UpdateRoomBlackoutDto, RoomBlackout>();

        // ===== Reservation =====
        CreateMap<Reservation, ResponseReservationDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room.Name))
            .ForMember(dest => dest.ReservedByUserName,
                opt => opt.MapFrom(src => src.ReservedByUser.FirstName + " " + src.ReservedByUser.LastName))
            .ForMember(dest => dest.OrganizerName,
                opt => opt.MapFrom(src => src.Organizer.FirstName + " " + src.Organizer.LastName))
            .ForMember(dest => dest.IsCheckedIn, opt => opt.MapFrom(src => src.CheckIn != null));

        CreateMap<CreateReservationDto, Reservation>();
        CreateMap<UpdateReservationDto, Reservation>();

        // CalendarReservationDto / RoomCalendarDto - composite, birden cox
        // entity-den (Room + Reservation + AppUser) qurulur, service/repository-de
        // custom sorgu ile elde doldurulur, avtomatik CreateMap yazilmir

        // ===== Checkin =====
        CreateMap<Checkin, ResponseCheckinDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User.FirstName + " " + src.User.LastName));
        // CreateCheckinDto -> Entity map yazilmir, QrCodeToken-dan Reservation/User
        // tapilmasi service-de custom logika teleb edir

        // ===== Comment =====
        CreateMap<Comment, ResponseCommentDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User.FirstName + " " + src.User.LastName))
            .ForMember(dest => dest.LikesCount, opt => opt.MapFrom(src => src.Likes.Count))
            .ForMember(dest => dest.RepliesCount, opt => opt.MapFrom(src => src.Replies.Count))
            .ForMember(dest => dest.IsLikedByCurrentUser, opt => opt.Ignore()); // service-de current user-e gore doldurulur

        CreateMap<Comment, DetailedCommentDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User.FirstName + " " + src.User.LastName))
            .ForMember(dest => dest.LikesCount, opt => opt.MapFrom(src => src.Likes.Count))
            .ForMember(dest => dest.IsLikedByCurrentUser, opt => opt.Ignore()); // service-de current user-e gore doldurulur
                                                                                // Replies eyni adli collection oldugu ucun avtomatik uygunlasir (Comment -> ResponseCommentDto)
                                                                                // Replies daxilindeki her ResponseCommentDto-nun IsLikedByCurrentUser-i de
                                                                                // service-de ayrica doldurulmalidir

        CreateMap<CreateCommentDto, Comment>();
        CreateMap<UpdateCommentDto, Comment>();

        // ===== Participant =====
        CreateMap<Participant, ResponseParticipantDto>()
            .ForMember(dest => dest.ResponseStatus, opt => opt.MapFrom(src => src.ResponseStatus.ToString()))
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User != null
                    ? src.User.FirstName + " " + src.User.LastName
                    : null));

        CreateMap<CreateParticipantDto, Participant>();

        CreateMap<UpdateParticipantDto, Participant>()
            .ForMember(dest => dest.ResponseStatus,
                opt => opt.MapFrom(src => Enum.Parse<ResponseStatus>(src.ResponseStatus)));

        // ===== Notification =====
        CreateMap<Notification, ResponseNotificationDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));
        // Create/Update yoxdur - sistem avtomatik yaradir
    }
}
