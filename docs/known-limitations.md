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
| L8 | Accounts | No email verification and no password reset (needs an email provider). | Decision D21 |
| L9 | Profile | The app shows the profile but has no edit screens yet; the API (`PUT /me`, `/me/student`, `/me/teacher`) is implemented and tested. Changing the language in Settings is not yet saved to the server profile. | Phase 3 |
| L10 | Teachers | Teacher verification has no admin screen; set `TeacherProfiles.VerificationStatus = 1` with SQL or use `seed-dev` accounts. | Admin tooling later |
| L12 | Files | Uploaded PDFs are not malware-scanned, and local-disk storage is development-only (single server). Deleting a unit that contains documents leaves their files on disk (rows are removed by cascade). | Before production: scanning + blob storage + cleanup job |
| L13 | Content | No server-side rich-text/Markdown rendering: lesson notes are stored as plain text and displayed as-is. | Phase 5 (lesson viewer) |
| L14 | Mobile content | The app cannot upload or view PDFs yet (API supports both); course details, unit descriptions and resource reordering are not editable from the app. | Phase 5 (lesson viewer / uploads) |
| L15 | Video | Without a YouTube API key, linked videos have no duration, and private videos can't be told apart from videos with embedding turned off. Video status is checked only when a video is linked: a video deleted or made private on YouTube later still appears until it is linked again (playback then fails in the player). | Configure `YouTube:ApiKey`; periodic re-check later |
| L16 | Video | Unlisted YouTube IDs can be copied and shared outside the app; YouTube offers no signed URLs or DRM for ordinary channels (decision D3). | Another provider with signed playback (post-MVP) |
| L17 | Video | Android "ATD" emulator images (e.g. the `Pixel_6_Pro_API_35` AVD) can't render WebView video: the player area stays blank. Use a Google APIs / Play Store image (e.g. `flutter_emulator`) or a real device to test playback. | Test-environment only |
| L18 | Video | Videos play without progress tracking yet (watched time, completion). The PDF viewer is also still pending (L14). | Phase 5a (progress) |
| L11 | Mobile logs | On Android, flutter_secure_storage 10 logs `EncryptedSharedPreferences initialization failed … Falling back to custom ciphers` at startup (legacy-format check). Harmless: tokens are stored and read correctly. | Watch library updates |

## Resolved

| # | Resolved in | Note |
|---|---|---|
| L2 | Phase 1 | Fonts bundled: Figtree (static weights 400–700, upstream erikdkennedy/figtree) and IBM Plex Sans Arabic (google/fonts), with their OFL licences, in `mobile/assets/fonts/` (~1.2 MB). Verified on Flutter web: Arabic renders on the first frame without a network font download. |
| — | Phase 1 | Design token `onPrimaryMuted` (#CCE7E3 on teal) failed WCAG AA (4.20:1). Changed to #E6F2F0 (4.78:1); the mockups were updated too. |
