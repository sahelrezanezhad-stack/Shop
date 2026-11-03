using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.ShoppingCarts;
using Shop.Presentation.Security;

namespace Shop.Presentation.Controllers;

[Route("admin/inventory"), Authorize(Policy = SallySecurity.AdminPolicy), AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminInventoryController(IInventoryManagementService inventory) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await inventory.ListAsync(ct));

    [HttpPost("{id:int}")]
    public async Task<IActionResult> Update(int id, InventoryInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid || input.Entries is null || input.Entries.Count is < 1 or > 5)
        {
            TempData["error"] = "Enter valid size inventory quantities.";
            return RedirectToAction(nameof(Index));
        }
        var result = await inventory.SetAsync(id, input.ExpectedTotal,
            input.Entries.Select(x => new InventoryChange(x.Size, x.ExpectedQuantity, x.Quantity)).ToList(), ct);
        TempData[result.Success ? "notice" : "error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}

public sealed class InventoryInput
{
    [Range(0, int.MaxValue)] public int ExpectedTotal { get; set; }
    public List<InventorySizeInput> Entries { get; set; } = new();
}
public sealed class InventorySizeInput
{
    [Required, StringLength(4)] public string Size { get; set; } = "";
    [Range(0, int.MaxValue)] public int ExpectedQuantity { get; set; }
    [Range(0, 100000)] public int Quantity { get; set; }
}
