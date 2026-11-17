# Known Limitations

Updated at the end of every phase. Items move to "Resolved" with the phase that fixed them.

## Open

| # | Area | Limitation | Plan |
|---|---|---|---|
| L1 | Catalog content | Seeded curricula, streams, grade names and 2026/2027 calendar dates are **reasonable defaults that need product-owner review**. In particular, Jordan's Tawjihi recently moved to a field-based structure; only the classic Scientific/Literary streams are seeded. | Review the list in `CatalogSeedData.cs`; corrections ship as a migration. |
| L3 | Onboarding | The "Continue" button shows "Account creation is coming soon". Sign-up arrives in Phase 2. | Phase 2 |
| L4 | Dark mode | Dark tokens are designed but not implemented. The app is light-only. | After MVP |
| L5 | Eastern Arabic digits | No number formatting exists yet. The Western-digits default (D15) and its setting will be implemented with the first numeric UI (Phase 3). | Phase 3 |
| L6 | Platforms | Verified on the Android 15 emulator and Flutter web. Not yet on a physical Android device; iOS needs macOS to build. | iOS later |
| L7 | Rate limiting | Per-IP partitioning is wrong behind a proxy unless forwarded headers are configured. | Deployment phase |

## Resolved

| # | Resolved in | Note |
|---|---|---|
| L2 | Phase 1 | Fonts bundled: Figtree (static weights 400–700, upstream erikdkennedy/figtree) and IBM Plex Sans Arabic (google/fonts), with their OFL licences, in `mobile/assets/fonts/` (~1.2 MB). Verified on Flutter web: Arabic renders on the first frame without a network font download. |
| — | Phase 1 | Design token `onPrimaryMuted` (#CCE7E3 on teal) failed WCAG AA (4.20:1). Changed to #E6F2F0 (4.78:1); the mockups were updated too. |
