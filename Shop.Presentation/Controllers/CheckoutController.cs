using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Shop.Application.Orders;
using Microsoft.AspNetCore.RateLimiting;

namespace Shop.Presentation.Controllers;

[ApiController]
[Route("api/checkout")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CheckoutController : ControllerBase
{
    private const string CartCookie = "sally_cart_v1";
    private readonly ICheckoutService _checkout;
    private readonly IWebHostEnvironment _env;
    public CheckoutController(ICheckoutService checkout, IWebHostEnvironment env)
        => (_checkout, _env) = (checkout, env);

    [HttpPost, EnableRateLimiting("checkout-writes")]
    public async Task<IActionResult> Place([FromBody] CheckoutInput input, CancellationToken ct)
    {
        if (Request.ContentType is null || !Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
            || !Request.Headers.TryGetValue("X-SALLY-Request", out var header) || header != "checkout-v1")
            return BadRequest(new { message = "Invalid request." });
        if (!Request.Cookies.TryGetValue(CartCookie, out var token)
            || !Guid.TryParse(token, out var cartId) || cartId == Guid.Empty)
            return BadRequest(new { message = "The shopping bag is invalid." });

        Guid? accountId = null;
        if (User.Identity?.IsAuthenticated == true &&
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var signedIn))
            accountId = signedIn;
        var outcome = await _checkout.PlaceAsync(cartId, input, accountId, ct);
        if (!outcome.Success)
            return StatusCode(outcome.ErrorCode is "OutOfStock" or "InventoryMismatch" ? 409 : 400,
                new { message = outcome.Message, code = outcome.ErrorCode });

        // Checkout is complete; next purchase gets an entirely new guest cart.
        Response.Cookies.Append(CartCookie, Guid.NewGuid().ToString("N"), new CookieOptions
        {
            HttpOnly = true, SameSite = SameSiteMode.Strict,
            Secure = !_env.IsDevelopment() || Request.IsHttps,
            Path = "/", MaxAge = TimeSpan.FromDays(7), IsEssential = true
        });
        return Ok(outcome.Order);
    }
}
