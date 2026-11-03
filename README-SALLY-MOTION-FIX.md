# SALLY — Rack Motion Fix

Based on the original supplied screen recording, not an imagined 3D mock-up.

Changes:
- Removed selected-garment reveal keyframe that restarted from the same centred pose and hid movement when switching items.
- All five garment elements now interpolate between their own previous and next rack / focus transforms.
- Maintained one consistent brand SALLY and softened the headline styling.
- Preserved original ASP.NET Core application and SQL Server layers.

Test: open `PREVIEW.html`, select each of five garments, navigate arrows in and out of focus, use BACK TO RACK.

Limit: photographed front views cannot create an authentic side/back view or real cloth physics. Current 3D rotation is an approximation. Front/back needs actual additional images.
