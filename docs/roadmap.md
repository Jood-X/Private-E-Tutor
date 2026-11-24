# Roadmap

| Phase | Scope | Status |
|---|---|---|
| 0 | Discovery: architecture, database design, design direction, decisions | Done |
| 1 | Foundation: monorepo, Clean Architecture backend, catalog schema + seed, envelope/errors/logging, Flutter core + design system + EN/AR localization, tests, docs | **Done** (see `progress.md`) |
| 2 | Authentication & profiles: Identity, JWT + refresh rotation, teacher/student registration, profiles with country/curriculum/grade, secure token storage, dev seed command | **Done** (see `progress.md`) |
| 3 | Courses & enrollment: teacher discovery, per-course enrollment requests and approval, units/lessons/resources CRUD, ownership tests, file uploads | **Done** (see `progress.md`) |
| 4 | Video: provider abstraction, YouTube "link an unlisted video" flow, in-app player, playback authorization | **Done** (see `progress.md`) |
| 5a | Learning progress: resource/lesson/course progress, video coverage bitmap, engagement metrics | |
| 5b | Assessments: question bank, quizzes and mock exams, assignments and grading, topic analytics | |
| 6 | Scheduling: availability, booking requests with auto-enrollment, recurring bookings, sessions, DB-enforced conflict prevention, time zones | |
| 7 | Release preparation: security review, accessibility audit, performance checks, clean-machine setup test | |

Post-MVP candidates: teacher web dashboard (Flutter web), iOS, dark mode, push notifications, messaging,
cloud video provider with signed playback, payments, AI features.
