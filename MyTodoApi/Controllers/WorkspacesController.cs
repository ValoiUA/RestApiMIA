using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MyTodoApi.DTO;
using MyTodoApi.Models;
using AutoMapper;
using MyTodoApi.Repositories;

namespace MyTodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("1. Workspaces")]
public class WorkspacesController : ControllerBase
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IValidator<WorkspaceDTO> _validator;
    private readonly IMapper _mapper;

    public WorkspacesController(
        IWorkspaceRepository workspaceRepository,
        IValidator<WorkspaceDTO> validator,
        IMapper imapper)
    {
        _workspaceRepository = workspaceRepository;
        _validator = validator;
        _mapper = imapper;
    }

    [HttpGet]
    [EndpointSummary("Отримати всі робочі зони")]
    public async Task<IActionResult> GetAll()
    {
        var workspaces = await _workspaceRepository.GetAllAsync();
        return Ok(workspaces);
    }

    [HttpGet("{id}")]
    [EndpointSummary("Отримати робочу зону за ID")]
    public async Task<IActionResult> GetById(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { Message = "Некоректний формат ID." });
        }

        var workspace = await _workspaceRepository.GetByIdAsync(id);
        return workspace is not null 
            ? Ok(workspace) 
            : NotFound(new { Message = $"Робочу зону з ID {id} не знайдено." });
    }

    [HttpPost]
    [EndpointSummary("Додати нову робочу зону")]
    public async Task<IActionResult> Create([FromBody] WorkspaceDTO dto)
    {
        var result = await _validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            return BadRequest(result.Errors.Select(e => e.ErrorMessage));
        }

        var newWorkspace = _mapper.Map<Workspace>(dto);

        await _workspaceRepository.CreateAsync(newWorkspace);

        return CreatedAtAction(nameof(GetById), new { id = newWorkspace.Id }, newWorkspace);
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Видалити робочу зону за ID")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { Message = "Некоректний формат ID." });
        }

        var isDeleted = await _workspaceRepository.DeleteAsync(id);
        if (!isDeleted)
        {
            return NotFound(new { Message = $"Робочу зону з ID {id} не знайдено." });
        }

        return Ok(new { Status = "Deleted", Id = id });
    }

    [HttpPut("{id}")]
    [EndpointSummary("Повністю оновити робочу зону за ID")]
    public async Task<IActionResult> Update(string id, [FromBody] WorkspaceDTO updateDTO)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { Message = "Некоректний формат ID." });
        }

        var validationResult = await _validator.ValidateAsync(updateDTO);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var workspaceToPut = await _workspaceRepository.GetByIdAsync(id);
        if (workspaceToPut is null)
        {
            return NotFound(new { Message = $"Робочу зону з ID {id} не знайдено." });
        }

        _mapper.Map(updateDTO, workspaceToPut);

        var isUpdated = await _workspaceRepository.UpdateAsync(id, workspaceToPut);
        if (!isUpdated)
        {
            return StatusCode(500, "Помилка при оновленні робочої зони в базі даних.");
        }

        return Ok(workspaceToPut);
    }

    [HttpPatch("{id}")]
    [EndpointSummary("Частково змінити робочу зону за ID")]
    public async Task<IActionResult> Patch(string id, [FromBody] UpdateWorkspaceDTO dto)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { Message = "Некоректний формат ID." });
        }

        var workspaceToPatch = await _workspaceRepository.GetByIdAsync(id);
        _mapper.Map(dto, workspaceToPatch);

        var isUpdated = await _workspaceRepository.UpdateAsync(id, workspaceToPatch);
        if (!isUpdated)
        {
            return StatusCode(500, "Помилка при оновленні робочої зони в базі даних.");
        }

        return Ok(workspaceToPatch);
    }
}