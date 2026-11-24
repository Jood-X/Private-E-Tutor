# Decision Log

| # | Date | Decision | Status |
|---|---|---|---|
| D1 | 2026-11-14 | Monorepo: `backend/`, `mobile/`, `docs/`, `scripts/`. No separate `database/` folder; EF Core migrations are the schema source of truth. | Approved |
| D2 | 2026-11-14 | Enrollment is **per course** with explicit teacher approval. | Approved |
| D3 | 2026-11-14 | YouTube model **A**: teachers link an unlisted, embeddable video from their own channel. The backend validates it via the Data API (server-side key). OAuth upload (B) deferred. | Approved |
| D4 | 2026-11-14 | Android first; iOS later (requires macOS); Flutter web teacher dashboard after the MVP. | Approved |
| D5 | 2026-11-14 | Teachers are hidden from discovery until verified (flag set via seed/SQL in MVP). | Approved |
| D6 | 2026-11-14 | No in-app messaging in MVP (request notes + assignment feedback only). | Approved |
| D7 | 2026-11-14 | Private lessons are one-to-one; online (meeting link) or in person (location), no conferencing integration. | Approved |
| D8 | 2026-11-14 | .NET 10 LTS (SDK 10.0.404 installed). Flutter upgrade and Android license acceptance are left to the user. | Approved |
| D9 | 2026-11-14 | Internal code name `ETutor`; all user-visible branding comes from configuration. | Approved |
| D10 | 2026-11-14 | Defaults: 13+ self-registration (legal review before launch), dev-only email sender, local file storage behind `IFileStorage`, catalog seed for JO + Gulf + International. | Approved |
| D11 | 2026-11-14 | Backend Clean Architecture: Domain / Application / Infrastructure / Api projects, with MVC controllers at the edge. See `architecture.md`. | Approved |
| D12 | 2026-11-14 | Database design incl. booking model with DB-enforced slot reservations. See `database-design.md`. | Approved |
| D13 | 2026-11-14 | Design direction (Soft UI Evolution, teal/amber, Figtree + IBM Plex Sans Arabic). See `design-direction.md`. | Approved |
| D14 | 2026-11-14 | Private-lesson booking does **not** require prior enrollment. Approving a booking automatically creates/approves the student's enrollment in the booked course (same transaction). | Approved |
| D15 | 2026-11-14 | Arabic UI uses Western digits (012) by default; a single formatting setting can switch to Eastern Arabic digits. | Approved (default) |
| D16 | 2026-11-14 | The database is implemented incrementally per phase by following the approved design: Phase 1 = catalog + reference seed; later phases add their tables through new migrations. | Approved (default) |
| D17 | 2026-11-18 | Authentication: ASP.NET Core Identity (PBKDF2), password policy 8+ characters with a letter and a digit, lockout after 5 failures for 15 minutes. JWT access tokens 15 min (HMAC-SHA256); refresh tokens 30 days, random 256-bit, stored as SHA-256 hashes, single-use with rotation and reuse detection (reuse revokes the whole session). | Approved (default, Phase 2) |
| D18 | 2026-11-18 | Authorization is deny-by-default (fallback policy). Public endpoints are marked explicitly. Consequence: anonymous requests to unknown routes get 401, not 404, so routes are not discoverable without signing in. | Approved (default, Phase 2) |
| D19 | 2026-11-18 | Android minimum SDK raised to 23 (Android 6.0), required by flutter_secure_storage (Keystore-backed token storage). | Approved (default, Phase 2) |
| D20 | 2026-11-18 | A new user's time zone defaults to their country's default zone (from the catalog); editable later through the profile API. The app adopts the language saved in the profile when a user signs in. | Approved (default, Phase 2) |
| D21 | 2026-11-18 | Deferred from Phase 2: email verification, password reset (both need a real email sender) and profile-editing screens in the app (the API endpoints exist). | Proposed - confirm scheduling |
| D22 | 2026-11-18 | Course lifecycle: Draft → Published (needs at least one published lesson in a published unit) → Archived. A course with students can't go back to Draft. Only drafts can be deleted; published units/lessons are archived. Catalog placement (curriculum, grade, subject, year) is fixed at creation. | Approved (default, Phase 3) |
| D23 | 2026-11-18 | Students see a published course's summary before enrolling; the outline, lessons and documents only with an Approved enrollment. Enrolled students keep read access when a course is archived. Only verified teachers who accept students appear in discovery and can be requested. | Approved (default, Phase 3) |
| D24 | 2026-11-18 | Lesson resources in Phase 3: PDF documents (max 20 MB, verified by file signature, stored under random keys outside the web root, streamed only to the owner teacher or approved students, `Cache-Control: private, no-store`) and HTTPS links. Video arrives in Phase 4. | Approved (default, Phase 3) |
