using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.Interfaces;
using DataAccess.Repositories.Abstraction;

namespace BusinessLogic.Service.Organizer;

public class DropdownService : IDropdownService
{
    private readonly IUnitOfWork _uow;

    public DropdownService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<CreateEventDropdownsDto> GetCreateEventDropdownsAsync()
    {
        var dto = new CreateEventDropdownsDto();

        var semesters = await _uow.Semesters.GetAllAsync(null, q => q.OrderByDescending(x => x.StartDate));
        dto.Semesters = semesters.Select(s => new SelectItemDto { Id = s.Id, Text = s.Name }).ToList();

        var departments = await _uow.Departments.GetAllAsync(null, q => q.OrderBy(x => x.Name));
        dto.Departments = departments.Select(d => new SelectItemDto { Id = d.Id, Text = d.Name }).ToList();

        var locations = await _uow.Locations.GetAllAsync(null, q => q.OrderBy(x => x.Name));
        dto.Locations = locations.Select(l => new SelectItemDto { Id = l.Id, Text = l.Name }).ToList();

        var topics = await _uow.Topics.GetAllAsync(null, q => q.OrderBy(x => x.Name));
        dto.Topics = topics.Select(t => new SelectItemDto { Id = t.Id, Text = t.Name }).ToList();

        return dto;
    }
}