# Shop — Rack revision B

This is the same Shop ASP.NET Core solution. Only the rack JS and CSS were changed from the previous SALLY build.

## Test without .NET
Open `Shop/PREVIEW.html` in Chrome, click any narrow garment profile to bring it forward. Click `BACK TO RACK` to collapse it. Use the arrow keys, arrows and drag/swipe on mobile to browse.

## Scope and limitations
- Five original user-selected garment photos remain untouched, with their cut-out backgrounds.
- Rack profiles and side silhouettes are **CSS approximations**, NOT actual photographed or simulated side views. Accurate garment thickness, details and realistic full rotation would require side photos or 3D assets.
- Back-view and alternate color imagery are deliberately disabled.
- Cart remains a local browser prototype, not database checkout.
- No database migrations or changes to backend code.
