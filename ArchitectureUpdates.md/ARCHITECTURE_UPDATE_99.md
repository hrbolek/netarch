# ZArchitecture 99 – recent entity navigation

- Added the reusable `Base.UI/Components/RecentEntityHistory.razor` immediately below the shared navigation bar in `Page.razor`.
- Displays up to five recently visited entities, in one horizontally scrolling row, with compact pill labels.
- Identifies entities by `app/entity/id`, regardless of the action segment (`view`, `edit`, `designer`, ...).
- Revisiting an entity moves it to the most recent position. Clicking a history link does the same before navigating.
- Saves the most recent actual URL per entity to browser `localStorage` (`zarchitecture.recentEntities.v1`). This allows returning to the last action.
- Avoids recording generic routes without a GUID, and degrades gracefully when browser storage is unavailable.
- Client script only manages persistence and navigation; page markup remains a Razor component.

## Verification

Static review only; .NET SDK is unavailable here, so this change has not been compiled or browser-tested.
