# Refactor checkpoint — 2026-11-01

Implemented separate product, cart, inventory and order command/query services. Checkout validation is in Application/Orders/Commands; the SQL transaction remains in Infrastructure/Repositories. All six repository interfaces live in Domain/Repositories, with no Domain dependency on Application or Infrastructure. Existing service interfaces remain compatibility facades. DependencyInjection entry points are at the layer roots; the three unused Class1 placeholders were removed.

Inventory and cancellation decisions execute inside repository-held SQL locks. Product-before-size lock order, conditional inventory updates and commit-on-success behavior remain in the persistence layer. The new cart lock contract carries generic results rather than Application DTOs.

Previous user runtime evidence (before this refactor): unpaid order 1 cancellation restored product 3002 total/M stock from 8 to 10; refreshing left both at 10. Inventory editor saved product 3003 M=10 and retained it after refresh. Product 3003 description edit succeeded with price and stock retained. Admin logout redirected subsequent /admin/orders navigation to login.

Checks performed here: all C# sources and tests parsed with tree-sitter-c-sharp; JavaScript syntax check; git diff whitespace check; dependency boundary checks; repository placement checks; migrations and approved image assets unchanged in git diff. These are not a .NET build or SQL integration tests. SDK and SQL Server are unavailable here. Four xUnit regression cases added for stale inventory, totals, repeated cancellation, and paid-order cancellation refusal; NOT EXECUTED.

Earlier pending UI changes in this checkout: XS button, invariant Gregorian UTC display, drawer scroll containment. Browser/runtime validation remains pending. Landing layout and hanger-position refinements were subsequently implemented and checked in static browser preview; see layout-hanger-validation-2026-11-01.md. SMTP remains unconfigured. No migrations, Release build, publication, original database writes, or secret collection were performed. Existing downloadable ZIP has not been replaced with this checkpoint.
