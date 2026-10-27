using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MyTodoApi.DTO;
using MyTodoApi.Models;
using MyTodoApi.Repositories;

namespace MyTodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("2. Booking")]
public class BookingController : ControllerBase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IResidentRepository _residentRepository;
    private readonly IValidator<BookingDTO> _validator;
    private readonly IMapper _mapper;

    public BookingController(
        IBookingRepository bookingRepository,
        IWorkspaceRepository workspaceRepository,
        IResidentRepository residentRepository,
        IValidator<BookingDTO> validator,
        IMapper mapper)
    {
        _bookingRepository = bookingRepository;
        _workspaceRepository = workspaceRepository;
        _residentRepository = residentRepository;
        _validator = validator;
        _mapper = mapper;
    }

    [HttpGet]
    [EndpointSummary("Отримати всі бронювання")]
    public async Task<IActionResult> GetAll()
    {
        var bookings = await _bookingRepository.GetAllAsync();
        return Ok(bookings);
    }

    [HttpGet("{id}")]
    [EndpointSummary("Отримати бронювання за ID")]
    public async Task<IActionResult> GetById(string id)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        return booking is not null 
            ? Ok(booking) 
            : NotFound(new { Message = $"Бронювання з ID {id} не знайдено." });
    }

    [HttpPost]
    [EndpointSummary("Додати нове бронювання")]
    public async Task<IActionResult> Create([FromBody] BookingDTO newBookingDto)
    {
        var validationResult = await _validator.ValidateAsync(newBookingDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var roomTypeResult = GetRequiredRoomType(newBookingDto.ResidentMaxCount);
        if (!roomTypeResult.Success)
        {
            return BadRequest(roomTypeResult.Error);
        }

        var newStart = newBookingDto.BookingDate;
        var newEnd = newBookingDto.BookingDate.AddMinutes(newBookingDto.TimeBooking);

        var workspaces = await _workspaceRepository.GetAllAsync();
        var bookings = await _bookingRepository.GetAllAsync();
        var residents = await _residentRepository.GetAllAsync();

        var availableWorkspace = workspaces.FirstOrDefault(w =>
            w.IsActive &&
            w.RoomType == roomTypeResult.Type &&
            !bookings.Any(b => b.WorkspaceId == w.Id && 
                               newStart < b.BookingDate.AddMinutes(b.TimeBooking) && 
                               newEnd > b.BookingDate));

        if (availableWorkspace is null)
        {
            return BadRequest($"Немає вільних кімнат типу {roomTypeResult.Type} на вказаний час.");
        }

        decimal finalPrice = CalculatePrice(
            newBookingDto.TimeBooking,
            availableWorkspace.RoomType,
            newBookingDto.ResidentName,
            newBookingDto.ResidentSurName,
            newBookingDto.ResidentMaxCount,
            residents);

        var newBooking = _mapper.Map<Booking>(newBookingDto);
        newBooking.WorkspaceId = availableWorkspace.Id!;
        newBooking.TotalPrice = finalPrice;

        await _bookingRepository.CreateAsync(newBooking);

        availableWorkspace.CreatedAt ??= [];
        availableWorkspace.DuringDate ??= [];
        availableWorkspace.CreatedAt.Add(newStart);
        availableWorkspace.DuringDate.Add(newEnd);
        await _workspaceRepository.UpdateAsync(availableWorkspace.Id!, availableWorkspace);

        return CreatedAtAction(nameof(GetById), new { id = newBooking.Id }, newBooking);
    }

    [HttpPut("{id}")]
    [EndpointSummary("Повністю оновити бронювання")]
    public async Task<IActionResult> Put(string id, [FromBody] BookingDTO updateDto)
    {
        var validationResult = await _validator.ValidateAsync(updateDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var bookingToUpdate = await _bookingRepository.GetByIdAsync(id);
        if (bookingToUpdate is null)
        {
            return NotFound(new { Message = $"Бронювання з ID {id} не знайдено." });
        }

        var roomTypeResult = GetRequiredRoomType(updateDto.ResidentMaxCount);
        if (!roomTypeResult.Success)
        {
            return BadRequest(roomTypeResult.Error);
        }

        var newStart = updateDto.BookingDate;
        var newEnd = updateDto.BookingDate.AddMinutes(updateDto.TimeBooking);

        var workspaces = await _workspaceRepository.GetAllAsync();
        var bookings = await _bookingRepository.GetAllAsync();
        var residents = await _residentRepository.GetAllAsync();

        var suitableWorkspace = workspaces.FirstOrDefault(w =>
            w.IsActive &&
            w.RoomType == roomTypeResult.Type &&
            !bookings.Any(b => b.Id != id && 
                               b.WorkspaceId == w.Id && 
                               newStart < b.BookingDate.AddMinutes(b.TimeBooking) && 
                               newEnd > b.BookingDate));

        if (suitableWorkspace is null)
        {
            return BadRequest($"Немає вільних кімнат типу {roomTypeResult.Type} на вказаний час.");
        }

        var oldWorkspace = workspaces.FirstOrDefault(w => w.Id == bookingToUpdate.WorkspaceId);
        if (oldWorkspace is not null)
        {
            var oldEnd = bookingToUpdate.BookingDate.AddMinutes(bookingToUpdate.TimeBooking);
            oldWorkspace.CreatedAt?.RemoveAll(d => d == bookingToUpdate.BookingDate);
            oldWorkspace.DuringDate?.RemoveAll(d => d == oldEnd);
            await _workspaceRepository.UpdateAsync(oldWorkspace.Id!, oldWorkspace);
        }
        var targetWorkspace = (oldWorkspace?.Id == suitableWorkspace.Id) ? oldWorkspace : suitableWorkspace;
        targetWorkspace.CreatedAt ??= [];
        targetWorkspace.DuringDate ??= [];
        targetWorkspace.CreatedAt.Add(newStart);
        targetWorkspace.DuringDate.Add(newEnd);
        await _workspaceRepository.UpdateAsync(targetWorkspace.Id!, targetWorkspace);

        decimal finalPrice = CalculatePrice(
            updateDto.TimeBooking,
            targetWorkspace.RoomType,
            updateDto.ResidentName,
            updateDto.ResidentSurName,
            updateDto.ResidentMaxCount,
            residents);

        _mapper.Map(updateDto, bookingToUpdate);
        bookingToUpdate.WorkspaceId = targetWorkspace.Id!;
        bookingToUpdate.TotalPrice = finalPrice;

        var isUpdated = await _bookingRepository.UpdateAsync(id, bookingToUpdate);
        return isUpdated ? Ok(bookingToUpdate) : StatusCode(500, "Помилка оновлення запису в базі даних.");
    }

    [HttpPatch("{id}")]
    [EndpointSummary("Частково оновити бронювання")]
    public async Task<IActionResult> Patch(string id, [FromBody] UpdateBookingDTO patchDto)
    {
        var bookingToUpdate = await _bookingRepository.GetByIdAsync(id);
        if (bookingToUpdate is null)
        {
            return NotFound(new { Message = $"Бронювання з ID {id} не знайдено." });
        }

        var targetDate = patchDto.BookingDate ?? bookingToUpdate.BookingDate;
        var targetTime = patchDto.TimeBooking ?? bookingToUpdate.TimeBooking;
        var targetCount = patchDto.ResidentMaxCount ?? bookingToUpdate.ResidentMaxCount;
        var targetName = !string.IsNullOrWhiteSpace(patchDto.ResidentName) ? patchDto.ResidentName : bookingToUpdate.ResidentName;
        var targetSurName = !string.IsNullOrWhiteSpace(patchDto.ResidentSurName) ? patchDto.ResidentSurName : bookingToUpdate.ResidentSurName;

        if (targetDate < DateTime.Now)
        {
            return BadRequest("Час бронювання не може бути в минулому.");
        }

        if (targetTime <= 0)
        {
            return BadRequest("Тривалість бронювання має бути більшою за нуль.");
        }

        var roomTypeResult = GetRequiredRoomType(targetCount);
        if (!roomTypeResult.Success)
        {
            return BadRequest(roomTypeResult.Error);
        }

        var newStart = targetDate;
        var newEnd = targetDate.AddMinutes(targetTime);

        var workspaces = await _workspaceRepository.GetAllAsync();
        var bookings = await _bookingRepository.GetAllAsync();
        var residents = await _residentRepository.GetAllAsync();

        var suitableWorkspace = workspaces.FirstOrDefault(w =>
            w.IsActive &&
            w.RoomType == roomTypeResult.Type &&
            !bookings.Any(b => b.Id != id && 
                               b.WorkspaceId == w.Id && 
                               newStart < b.BookingDate.AddMinutes(b.TimeBooking) && 
                               newEnd > b.BookingDate));

        if (suitableWorkspace is null)
        {
            return BadRequest($"Немає доступних кімнат типу {roomTypeResult.Type} на обраний проміжок часу.");
        }

        var oldWorkspace = workspaces.FirstOrDefault(w => w.Id == bookingToUpdate.WorkspaceId);
        if (oldWorkspace is not null)
        {
            var oldEnd = bookingToUpdate.BookingDate.AddMinutes(bookingToUpdate.TimeBooking);
            oldWorkspace.CreatedAt?.RemoveAll(d => d == bookingToUpdate.BookingDate);
            oldWorkspace.DuringDate?.RemoveAll(d => d == oldEnd);
            await _workspaceRepository.UpdateAsync(oldWorkspace.Id!, oldWorkspace);
        }

        var targetWorkspace = (oldWorkspace?.Id == suitableWorkspace.Id) ? oldWorkspace : suitableWorkspace;
        targetWorkspace.CreatedAt ??= [];
        targetWorkspace.DuringDate ??= [];
        targetWorkspace.CreatedAt.Add(newStart);
        targetWorkspace.DuringDate.Add(newEnd);
        await _workspaceRepository.UpdateAsync(targetWorkspace.Id!, targetWorkspace);

        decimal finalPrice = CalculatePrice(targetTime, targetWorkspace.RoomType, targetName, targetSurName, targetCount, residents);

        _mapper.Map(patchDto, bookingToUpdate);
        bookingToUpdate.WorkspaceId = targetWorkspace.Id!;
        bookingToUpdate.TotalPrice = finalPrice;

        var isUpdated = await _bookingRepository.UpdateAsync(id, bookingToUpdate);
        return isUpdated ? Ok(bookingToUpdate) : StatusCode(500, "Помилка оновлення запису в базі даних.");
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Видалити бронювання за ID")]
    public async Task<IActionResult> DeleteById(string id)
    {
        var bookingToDelete = await _bookingRepository.GetByIdAsync(id);
        if (bookingToDelete is null)
        {
            return NotFound(new { Message = $"Бронювання з ID {id} не знайдено." });
        }

        decimal returnMoney;
        if (bookingToDelete.BookingDate - DateTime.Now > TimeSpan.FromHours(24))
        {
            returnMoney = bookingToDelete.TotalPrice;
        }
        else if (DateTime.Now > bookingToDelete.BookingDate)
        {
            return BadRequest("Неможливо скасувати бронювання, яке вже розпочалося або минуло.");
        }
        else
        {
            returnMoney = bookingToDelete.TotalPrice * 0.5m;
        }

        var workspaceToUpdate = await _workspaceRepository.GetByIdAsync(bookingToDelete.WorkspaceId);
        if (workspaceToUpdate is not null)
        {
            var bookingEnd = bookingToDelete.BookingDate.AddMinutes(bookingToDelete.TimeBooking);
            workspaceToUpdate.CreatedAt?.RemoveAll(d => d == bookingToDelete.BookingDate);
            workspaceToUpdate.DuringDate?.RemoveAll(d => d == bookingEnd);
            await _workspaceRepository.UpdateAsync(workspaceToUpdate.Id!, workspaceToUpdate);
        }

        var isDeleted = await _bookingRepository.DeleteAsync(id);
        if (!isDeleted)
        {
            return StatusCode(500, "Помилка при видаленні з бази даних.");
        }

        return Ok(new { Status = "Deleted", Id = id, ReturnMoney = returnMoney });
    }

    [HttpDelete("{surname},{name}")]
    [EndpointSummary("Видалити бронювання за прізвищем та ім'ям")]
    public async Task<IActionResult> DeleteByName(string surname, string name)
    {
        var bookings = await _bookingRepository.FindAsync(b =>
            string.Equals(b.ResidentSurName, surname, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(b.ResidentName, name, StringComparison.OrdinalIgnoreCase));

        var bookingToDelete = bookings.FirstOrDefault();
        if (bookingToDelete is null)
        {
            return NotFound(new { Message = $"Бронювання для {surname} {name} не знайдено." });
        }

        decimal returnMoney;
        if (bookingToDelete.BookingDate - DateTime.Now > TimeSpan.FromHours(24))
        {
            returnMoney = bookingToDelete.TotalPrice;
        }
        else if (DateTime.Now > bookingToDelete.BookingDate)
        {
            return BadRequest("Неможливо скасувати бронювання, яке вже розпочалося або минуло.");
        }
        else
        {
            returnMoney = bookingToDelete.TotalPrice * 0.5m;
        }

        var workspaceToUpdate = await _workspaceRepository.GetByIdAsync(bookingToDelete.WorkspaceId);
        if (workspaceToUpdate is not null)
        {
            var bookingEnd = bookingToDelete.BookingDate.AddMinutes(bookingToDelete.TimeBooking);
            workspaceToUpdate.CreatedAt?.RemoveAll(d => d == bookingToDelete.BookingDate);
            workspaceToUpdate.DuringDate?.RemoveAll(d => d == bookingEnd);
            await _workspaceRepository.UpdateAsync(workspaceToUpdate.Id!, workspaceToUpdate);
        }

        var isDeleted = await _bookingRepository.DeleteAsync(bookingToDelete.Id!);
        if (!isDeleted)
        {
            return StatusCode(500, "Помилка при видаленні з бази даних.");
        }

        return Ok(new { Status = "Deleted", Id = bookingToDelete.Id, ReturnMoney = returnMoney });
    }

    private static (bool Success, RoomType Type, string Error) GetRequiredRoomType(int count)
    {
        if (count > 1 && count <= 4) return (true, RoomType.Small, string.Empty);
        if (count > 4 && count <= 8) return (true, RoomType.Medium, string.Empty);
        if (count > 8 && count <= 12) return (true, RoomType.Big, string.Empty);
        if (count > 12 && count <= 15) return (true, RoomType.Large, string.Empty);

        return (false, RoomType.Small, "Кількість резидентів має бути від 2 до 15 осіб.");
    }

    private static decimal CalculatePrice(int timeMinutes, RoomType roomType, string name, string surName, int residentCount, List<Resident> residents)
    {
        decimal hours = (decimal)timeMinutes;
        decimal hourlyRate = roomType switch
        {
            RoomType.Small => 100m,
            RoomType.Medium => 150m,
            RoomType.Big => 200m,
            RoomType.Large => 250m,
            _ => 0m
        };

        decimal basePrice = hours * hourlyRate;

        var resident = residents.FirstOrDefault(r =>
            string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.SurName, surName, StringComparison.OrdinalIgnoreCase));

        bool isPremium = resident != null && resident.Type == ResidentType.VIP;
        bool isFullDay = hours >= 8;

        decimal finalPrice = basePrice;
        if (isFullDay || isPremium)
        {
            finalPrice *= 0.8m;
        }

        if (residentCount > 6)
        {
            finalPrice += 200m;
        }

        return finalPrice;
    }
}