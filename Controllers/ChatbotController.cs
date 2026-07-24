using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Services;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Controllers;

public sealed class ChatbotController : Controller
{
    private readonly IChatbotService _chatbotService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatbotController(IChatbotService chatbotService, UserManager<ApplicationUser> userManager)
    {
        _chatbotService = chatbotService; _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Index() => View(new ChatbotViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ChatbotViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var answer = await _chatbotService.AskAsync(model.Question, _userManager.GetUserId(User), cancellationToken);
        model.Answer = answer.Answer;
        model.Recommendations = answer.Products;
        return View(model);
    }
}
