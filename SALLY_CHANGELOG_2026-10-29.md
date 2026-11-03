# SALLY source update — 2026-10-29

## Changes in this source package
- Standardized the storefront and customer account UI to English and left-to-right layout.
- Translated storefront catalog copy, cart/checkout feedback, account messages, and outgoing email subjects/bodies to English.
- Kept the existing product images, product IDs, prices, size options, rack motion code and Hangfire schedule.
- Added `My SALLY > Change email address`, protected by the current password, account authentication, anti-forgery validation, and the existing account-write rate limit. The new address must be verified again.
- Superseded unsent verification messages when the email address changes. Pending order notifications remain queued until the new address is verified.
- Made the cart quantity increase control disabled when the current line reaches the available stock or the business cap of 10. Decrease remains available at quantity 1 so the line can be removed.
- Updated the storefront API's presentation descriptions/materials to English so legacy Persian descriptions in ShopDb are not rendered in the customer storefront.
- Updated demo catalog seed descriptions to English and added a read-only SQL inspection script.
- Changed the light-garment outline from a dark edge to a softer grey edge; garment images themselves were not edited.

## Intentionally unchanged
- `ShopDb` schema and data; no migrations were added or executed.
- The existing connection string in `appsettings.json`.
- `Hangfire:Enabled` remains `false`.
- SMTP credentials remain empty and must be configured through User Secrets or environment variables, not committed to source.
- Approved front/back garment image assets and rack interaction transforms.

## Validation
- JavaScript syntax check: run `node --check Shop.Presentation/wwwroot/js/rack.js`.
- ZIP/source file integrity: checked before delivery.
- .NET build and live SQL/SMTP integration tests could not be run in this environment because the .NET SDK is not installed here. Run Build in Visual Studio before replacing the laptop copy.
