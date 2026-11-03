using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shop.Application.ShoppingCarts;
using Microsoft.AspNetCore.RateLimiting;

namespace Shop.Presentation.Controllers;

[ApiController]
[Route("api/cart")]
[EnableRateLimiting("cart-requests")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CartController : ControllerBase
{
    private const string CookieName = "sally_cart_v1";
    private readonly ICartService _service;
    public CartController(ICartService service) => _service = service;

    // Guest cart cookie is unguessable, HttpOnly, SameSite Strict and HTTPS-only on production.
    private Guid? ExistingId()
        => Request.Cookies.TryGetValue(CookieName, out var value) && Guid.TryParse(value, out var id)
            && id != Guid.Empty ? id : null;
    private Guid GetOrCreateId()
    {
        var old = ExistingId();
        if (old.HasValue) return old.Value;
        var id = Guid.NewGuid();
        Response.Cookies.Append(CookieName, id.ToString("N"), new CookieOptions
        {
            HttpOnly = true, SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps || !HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            Path = "/", MaxAge = TimeSpan.FromDays(7), IsEssential = true
        });
        return id;
    }

    // The custom header makes cross-origin form/CSRF submissions fail without a same-origin fetch.
    private bool IsAjaxWrite() => Request.Headers.TryGetValue("X-SALLY-Request", out var v) && v == "cart-v1";

    [HttpGet]
    public async Task<ActionResult<CartDto>> Get(CancellationToken ct)
    {
        var id = ExistingId();
        return Ok(id.HasValue ? await _service.GetAsync(id.Value, ct) : CartDto.Empty);
    }

    public sealed record AddLineRequest(int ProductId, string? Size, int Quantity);
    public sealed record ChangeQuantityRequest(int Quantity);
    private IActionResult Respond(CartOutcome outcome)
        => outcome.Success ? Ok(outcome.Cart) : BadRequest(new { message = outcome.Message, cart = outcome.Cart });

    [HttpPost("lines")]
    public async Task<IActionResult> Add([FromBody] AddLineRequest request, CancellationToken ct)
    {
        if (!IsAjaxWrite()) return BadRequest(new { message = "Invalid request." });
        return Respond(await _service.AddAsync(GetOrCreateId(), request.ProductId, request.Size, request.Quantity, ct));
    }
    [HttpPut("lines/{lineId:int}")]
    public async Task<IActionResult> Update(int lineId, [FromBody] ChangeQuantityRequest request, CancellationToken ct)
    {
        if (!IsAjaxWrite()) return BadRequest(new { message = "Invalid request." });
        var id = ExistingId();
        return id.HasValue ? Respond(await _service.UpdateAsync(id.Value, lineId, request.Quantity, ct)) : NotFound();
    }
    [HttpDelete("lines/{lineId:int}")]
    public async Task<IActionResult> Remove(int lineId, CancellationToken ct)
    {
        if (!IsAjaxWrite()) return BadRequest(new { message = "Invalid request." });
        var id = ExistingId();
        return id.HasValue ? Respond(await _service.RemoveAsync(id.Value, lineId, ct)) : NotFound();
    }
}
