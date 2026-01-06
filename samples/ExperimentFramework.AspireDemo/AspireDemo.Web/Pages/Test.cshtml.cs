using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspireDemo.Web.Pages;

public class TestModel : PageModel
{
    private readonly ILogger<TestModel> _logger;

    public TestModel(ILogger<TestModel> logger)
    {
        _logger = logger;
    }

    [BindProperty]
    public string? Message { get; set; }

    public void OnGet()
    {
        _logger.LogInformation("Test.OnGet called");
    }

    public IActionResult OnPost()
    {
        _logger.LogInformation("Test.OnPost called. Message: {Message}", Message);
        return Page();
    }
}
