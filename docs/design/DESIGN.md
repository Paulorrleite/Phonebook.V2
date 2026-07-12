# Phonebook WebUi design guide

## Purpose

This guide defines the starting design rules for the Vue WebUi. Follow it before
creating, modifying, reviewing, or refactoring frontend code.

The WebUi is an operational contact-management tool. It must feel clear,
efficient, and trustworthy. Avoid marketing-page patterns, decorative hero
sections, and oversized content blocks.

## Layout

- Start with the contact list as the first screen.
- Use a compact application shell with a main content area.
- Keep list, create, and edit workflows easy to move between.
- Use full-width page sections or unframed layouts, not nested cards.
- Use cards only for repeated contact rows, modals, and genuinely framed tools.
- Keep fixed-format controls stable with explicit dimensions.
- Ensure text never overlaps or escapes its container on mobile or desktop.

## Visual style

- Use a restrained, work-focused palette.
- Avoid one-note color themes and large gradients.
- Use color to communicate state: primary action, success, warning, danger, and
  disabled.
- Use border radius of 8 px or less unless a component needs a smaller radius.
- Use consistent spacing and align form controls on a predictable grid.
- Use tabular numbers for pagination and count displays.

## Components

- Keep reusable UI in `WebUi/src/shared/components`.
- Keep contact-specific UI in `WebUi/src/features/contacts`.
- Use the shared contact form for create and edit.
- Use icon buttons for common row actions when the icon is familiar.
- Give every icon-only button an `aria-label`.
- Use tooltips for unfamiliar icon-only actions.
- Use confirmation modal for delete.
- Use toggles, checkboxes, segmented controls, sliders, and menus where they fit
  the input type.

## Forms

- Use visible labels for every form field.
- Use `type="email"` for email fields and `type="tel"` for phone fields.
- Use meaningful `name` and `autocomplete` attributes.
- Disable spellcheck for email.
- Do not block paste.
- Keep the submit button enabled until the request starts.
- Show a spinner or busy state during submit.
- Show validation errors inline next to fields.
- Focus the first invalid field after submit.
- Warn before navigation when the form has unsaved changes.

## Contact list

- Show photo or a stable fallback avatar.
- Show display name, selected phone, and email.
- Use favorite phone first. If no favorite exists, use `Mobile`,
  `Residential`, then `Commercial`.
- Keep search and pagination state in the URL query string.
- Show an empty state when no contacts match the current search.
- Keep row actions keyboard accessible.

## Accessibility

- Use semantic HTML before ARIA.
- Use `<button>` for actions and links for navigation.
- Provide a skip link to main content.
- Keep heading levels hierarchical.
- Ensure every interactive element has a visible `:focus-visible` state.
- Use `aria-live="polite"` for async validation or save messages.
- Respect `prefers-reduced-motion`.

## Performance

- Project API list data to only the fields needed by the list.
- Avoid expensive controlled-input work on every keystroke.
- If the rendered list grows beyond 50 visible rows, use virtualization or
  pagination instead of rendering every item.
- Provide explicit image dimensions to avoid layout shift.

## Copy

- Use direct, action-oriented labels.
- Use specific button text, such as **Create Contact**, **Save Contact**, and
  **Delete Contact**.
- Write validation messages with the fix included.
- Use `…` for loading text, such as `Saving…`.
