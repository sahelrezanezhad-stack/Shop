# Landing composition and hanger lowering — 2026-11-01

Landing: replace capped columns and negative model translation with proportional columns, right-align the complete photograph, keep separate text/photo regions, and reduce copy padding on short viewports. Mobile retains stacked text and full photograph without horizontal overflow.

Hangers: lower photographed front shoulders by 14 SVG units. Adjust photographed neck height around a fixed rail-contact point so the shoulder join stays connected. Front neckline masks remain unchanged. Back and side transforms remain unchanged. Original raster images are untouched.

Verified in static browser preview: landing at 1363x936 and embedded 1280x640; mobile iframe 390x844 (375 CSS content width because of vertical scrollbar), no horizontal overflow; selected suede jacket and original grey/white hoodie front collars. Algebraic check for all eight front hanger fits: shoulder join drops 14 units, hook contact remains fixed, no gap introduced at the shared join. JavaScript syntax and git whitespace checks passed. Runtime .NET/SQL validation of the integrated refactor still pending; no Release build or publication.

Rejected intermediate attempt: utility vest width scale 0.64 and shoulder drop 42. The user rejected the small, floating appearance; this is superseded by the correction below.

Final follow-up correction: restore utility vest width scale 1 and normal 14-unit shoulder offset. Replace its oversized neckline aperture with the actual inner collar curve: right edge (442,164), V tip (360,278), left edge (275,164). This occludes hanger ends behind the existing fabric without shrinking the hanger. Static browser inspection covered rack-sized front via navigation and enlarged front with a close view of the collar. Other garment fits, original raster assets, back and side transforms are unchanged. User acceptance remains pending; static browser QA is not .NET runtime testing.
