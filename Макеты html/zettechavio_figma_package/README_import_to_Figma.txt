ZetTechAvio — Figma import package
==================================

Files included:
- zettechavio_homepage.png  (screenshot / raster preview)
- styles.json              (colors, typography, spacing tokens)
- README_import_to_Figma.txt (this file)

IMPORTANT: I can't upload directly to your Figma account. Follow the steps below to import and recreate the design in Figma.

Quick import steps (fast):
1. Open your Figma project (or create a new file).
2. Drag & drop 'zettechavio_homepage.png' from your file explorer into the Figma canvas. It will be placed as a raster image.
3. Create a Frame the size you want (e.g., Desktop 1366x768) and snap the image into the frame as a reference layer. Lock the layer (right-click > Lock) so it won't move.
4. Start recreating UI elements on top of the locked image:
   - Use rectangles with fills and shadows to recreate cards and panels.
   - Use the Styles in 'styles.json' (colors & typography) to create Figma color and text styles.
   - For icons, use open-source icon sets (Heroicons/Feather) or Figma's plugin 'Material Design Icons'.
5. Export assets: when you make buttons/icons, mark them as Exportable (right sidebar) and export as SVG/PNG for the web project.
6. To replicate the seatmap or map, use Figma vector tools (Pen/Line) or import a simplified SVG map background and draw markers on top.

Best practices:
- Create Color Styles: use Figma's 'Styles' to add PRIMARY_BLUE, DEEP_BLUE, SOFT_GREEN, MINT, TEXT, SUBTEXT.
- Create Text Styles: H1, H2, Body, Small with sizes from styles.json.
- Use auto-layout for search card and result list for responsive behaviour.
- Group components into "Components" (Header, SearchCard, ResultCard, MapPanel, SeatMap) and create instances to reuse.
- When done, publish the library (Team Library) so you can reuse components across files.

If you'd like, I can also:
- generate SVGs for the main UI components (buttons, icons, simple seatmap grid) and include them in this package;
- prepare a ready-made Figma .fig export (not possible directly), but I can prepare a JSON manifest describing layers to import with plugins like 'Figmotion' or use 'HTML to Figma' plugins.

If you want SVG components included now, reply 'Include SVGs' and I will add them to the package.
