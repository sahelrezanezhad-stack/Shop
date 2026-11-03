using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Orders;
using Shop.Presentation.Security;

namespace Shop.Presentation.Controllers;

[Route("admin/orders"), Authorize(Policy = SallySecurity.AdminPolicy), AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminOrdersController(IOrderManagementService orders) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? status, int page = 1, CancellationToken ct = default)
    {
        if (status is not null && status.Length > 40) return BadRequest();
        ViewData["status"] = status;
        return View(await orders.ListAsync(status, page, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        var order = await orders.GetAsync(id, ct);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await orders.CancelUnpaidAsync(id, ct);
        TempData[result.Success ? "notice" : "error"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id });
    }
}
