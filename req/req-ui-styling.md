# Requirement - UI Styling

## UI Style Guide

- **Theme & Color**
  - Support both light and dark theme modes (`prefers-color-scheme`) — must include a toggle to switch theme
  - Use CSS variables for theme colors
  - Gradient headers or accent elements preferred
  - UI should feel modern, clean, and visually polished
- **Accessibility**
  - Ensure accessible color contrast in both light and dark themes — minimum WCAG AA (4.5:1 for body text, 3:1 for large text)
  - Override any framework or default utility/component styles that keep their own text or background colors. All app text and surfaces must use theme-safe colors with readable contrast.
  - Validation: visually verify every page in both light/dark mode has no low-contrast text before considering the UI complete
- **Layout & Structure**
  - Responsive, mobile-first layouts (Flexbox or Grid)
  - Card-based dashboards/panels; styled tables with spacing & subtle shadows preferred
  - Avoid outdated patterns (tables for layout, fixed-width pages)
  - Use semantic HTML elements
- **Components & Interactivity**
  - Smooth hover/focus states for buttons, links, and interactive elements
    - Do not use transform/translate effects on hover that move or shift elements
    - Hover effects should be limited to color, opacity, or box-shadow changes
  - Subtle rounded corners (4–12px) on cards, panels, buttons, tables
  - Avoid inline styles unless necessary; override default browser styling
- **Typography & Spacing**
  - Typography should feel contemporary and readable, not old-fashioned
  - Consistent spacing and typography (e.g., 4px / 8px scale)

---
