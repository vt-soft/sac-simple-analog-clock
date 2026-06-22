# Copilot Instructions

## Project Guidelines
- User prefers a single MSIX file for packaging ('I want only one msix file'). Remember this preference for future packaging guidance.

## SAC (Simple Analog Clock) Project Guidelines
- When resizing the ClockForm, use `this.Refresh()` after `this.Invalidate()` to significantly improve rendering of borders and buttons.
- Do NOT remove the Anchor property from `resizeButton` as it's needed for proper positioning during form resize operations. The anchor style (Bottom|Right) works correctly with the manual resize logic.