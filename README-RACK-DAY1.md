# SALLY — Day 1 on original Shop codebase

## Changes (non-destructive)
- Replaced `Shop.Presentation/Views/Home/Index.cshtml` (only storefront Home view)
- Added `Shop.Presentation/Views/Shared/_StoreLayout.cshtml` as a storefront-only layout (existing MVC layout left untouched)
- Added `Shop.Presentation/wwwroot/css/rack.css` and `wwwroot/js/rack.js`
- Added 5 approved product photos with transparent backgrounds in `wwwroot/images/collection`
- Added `PREVIEW.html` for no-backend browser testing

## Run
1. Open `PREVIEW.html` from extracted root with Chrome/Edge (no database needed).
2. For MVC: `Shop.sln` > Set Startup Project `Shop.Presentation` > Run.

## Interactions
Click clothing / use arrow buttons or arrow keys / swipe on mobile to browse. Click product to focus. FRONT displays provided photograph. BACK not available yet (requires actual back photo). SIZE and demo BAG are local UI, *not* real orders. Color is the photographed color only, no misleading fake colors. Escape or BACK TO RACK exits focus mode.

## Status
This is a frontend iteration on original Shop MVC structure, not a new project. Existing product CRUD and backend unchanged. No DB integration, payments, stock verification, actual color variants or back views yet. Front images are user-supplied references; make sure commercial usage rights cover production use.
