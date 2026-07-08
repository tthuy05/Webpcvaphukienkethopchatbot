using Microsoft.AspNetCore.Mvc;
using Webpcvaphukienkethopchatbot.Services;

namespace Webpcvaphukienkethopchatbot.Controllers;

public class ChatbotController : Controller
{
    private readonly IChatbotService _chatbotService;

    public ChatbotController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string question)
    {
        try
        {
            var response = await _chatbotService.AskAsync(User, question, HttpContext.RequestAborted);
            return View(response);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View();
        }
    }
}
