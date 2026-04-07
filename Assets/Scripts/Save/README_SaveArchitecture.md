# Save Architecture (Phase 5)

## 1. Current Strategy

The project now uses a **centralized save flow first, local fallback second** strategy:

- Primary: `SaveManager` unified save/load.
- Fallback: local persistence (`PlayerPrefs` or standalone JSON files) only when `SaveManager` is not present.

This allows both:

- Full game scenes with centralized save/load.
- Lightweight standalone scenes without a `SaveManager`.

## 2. Core Components

- `SaveManager`
  - Owns save slot files.
  - Collects section data through `ISaveParticipant`.
  - Supports multiple participants per `SaveSection`.
- `ISaveParticipant`
  - `CaptureSaveData()`
  - `RestoreSaveData(json, timeSystem)`
- Legacy bridge interfaces
  - `IFarmSaveable`, `IShopSaveable`, `INPCSaveable`, `IQuestSaveable`,
    `IInventorySaveable`, `IPlayerSaveable`, `ICropSaveable`, `IBuildSaveable`

## 3. Registration Lifecycle Rule

To avoid duplicate registration, participants should follow one rule:

- Register in `OnEnable`.
- Unregister in `OnDisable`.

Do not register/unregister repeatedly in `Awake`, `Start`, and `OnDestroy` unless there is a special reason.

## 4. Fallback Boundary

The following modules use local persistence only when no `SaveManager` exists:

- `BuildSaveManager`
- `CropSaveManager`
- `InventoryPersistence`
- `WalletPersistence`

When `SaveManager` exists, these modules provide data via `ISaveParticipant` and do not run local autosave paths.

## 5. Data Compatibility

- If one section has one participant, data is stored as direct JSON payload (legacy compatible).
- If one section has multiple participants, data is stored in a wrapped collection payload.
- During load:
  - New wrapped format restores by participant key first.
  - Legacy direct payload restores to the section participant (or first one when multiple exist).

## 6. Runtime Validation Checklist

Use this checklist when validating scenes:

1. Scene with `SaveManager`:
   - Trigger save/load from `SaveManager`.
   - Verify build/crop/inventory/wallet all restore correctly.
2. Scene without `SaveManager`:
   - Verify standalone modules still persist and restore.
3. Mixed participants in same section:
   - Verify no participant data is overwritten by registration order.

## 7. Known Risks

- Participant key currently depends on object type + transform path/name.
  - Renaming scene hierarchy objects may affect key matching for wrapped payloads.
- Legacy payload with multiple participants in same section can only restore deterministically when there is one clear target.

For future hardening, prefer a stable explicit participant ID field in each participant component.

