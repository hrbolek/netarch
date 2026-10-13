# Contract: add a specialized entity visualization

No complete visualization generator exists in `/templates/` yet. This document defines the integration requirements; it is NOT proof that a ready-to-run calendar/timetable template exists.

1. Define a typed read projection/DTO, filtering and pagination appropriate to the view. Query through secured services; no UI/HTTP direct EF access.
2. Resolver maps a named view endpoint to a service. Preserve standard detail/list/edit and use established content negotiation (`ApplyViewModel`/JSON) where appropriate.
3. Razor UI extends existing `Page`/`PageView` or domain `LargeCard` composition. Cards, related `EntityLink`s, permissions and navigation stay consistent.
4. Specializations (tree, plan, calendar, timetable) are independent **visual components** over read models, not competing CRUD implementations or new authorization systems.
5. Test empty data, hierarchies, large intervals, overlap handling for timetables, visibility of secured items, and correct deep links to entity detail.

For relation visualization use the `EntityUITemplate` composition and this contract; no source for prebuilt User/Group domain vectors is required. There is not yet a verified temporal visualization scaffold; record that as a missing template, not a reason to fabricate a standard component.
