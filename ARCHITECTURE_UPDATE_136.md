# ARCHITECTURE_UPDATE_136

- Events UI adopts the shared Core UI page composition: PageViewList, PageView, PageEdit and LargeCard/MediumCard.
- Added EventLargeCard, EventMediumCard, EventTypeLargeCard and EventTypeMediumCard.
- Event and EventType model base implements IEntityDTOModel, required by shared UI.
- Event detail children use ViewVector.
- Removed duplicate EventsMenu in favor of standard application menu rendered by Page.
- Existing creation/admin screens retain their forms and routes; their deeper card refactor is pending.
- No Core changes.
- Build not executed: dotnet SDK unavailable in runtime.
