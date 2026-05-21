using Microsoft.AspNetCore.Mvc;
using t.Application.Queries.Projects;
using t.Models.ViewModels;

namespace t.Controllers.Api;

[ApiController]
[Route("api/projects")]
public class ProjectsApiController : ControllerBase
{
    private readonly ProjectsQueryHandler _projectsQueryHandler;

    public ProjectsApiController(ProjectsQueryHandler projectsQueryHandler)
    {
        _projectsQueryHandler = projectsQueryHandler;
    }

    [HttpGet]
    public async Task<ActionResult<ProjectListPageViewModel>> GetProjects(string? region, int page = 1)
    {
        var model = await _projectsQueryHandler.GetProjectsAsync(region, page);
        return Ok(model);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ProjectDetailViewModel>> GetProjectBySlug(string slug)
    {
        var model = await _projectsQueryHandler.GetProjectDetailAsync(slug);
        if (model is null)
            return NotFound();

        return Ok(model);
    }
}
