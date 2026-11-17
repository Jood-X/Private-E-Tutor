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
