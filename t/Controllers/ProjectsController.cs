using Microsoft.AspNetCore.Mvc;
using t.Application.Queries.Projects;

namespace t.Controllers;

[Route("du-an")]
public class ProjectsController : Controller
{
    private readonly ProjectsQueryHandler _projectsQueryHandler;

    public ProjectsController(ProjectsQueryHandler projectsQueryHandler)
    {
        _projectsQueryHandler = projectsQueryHandler;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? region, int page = 1)
    {
        var model = await _projectsQueryHandler.GetProjectsAsync(region, page);
        return View(model);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var model = await _projectsQueryHandler.GetProjectDetailAsync(slug);
        if (model is null)
            return NotFound();

        return View(model);
    }
}
