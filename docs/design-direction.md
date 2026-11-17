# Design Direction

> Status: **PROPOSED — awaiting approval** (Phase 0).
> Mockups: the "E-Tutor Design Direction" canvas (9 artboards). The link is shared in the Phase 0 report.

## How this was produced

The `ui-ux-pro-max` skill was used as required:

| Step | Query / source | Result and how it was used |
|---|---|---|
| Rules | `references/quick-reference.md`, `references/pro-rules.md` | Read fully. Its accessibility, touch, navigation and form rules are applied below. Its pre-delivery checklist will gate each UI phase. |
| Design system #1 | `"education e-learning mobile app tutoring" --design-system` | Claymorphism with Baloo 2 / Comic Neue, aimed at **children**. **Rejected**: wrong audience for high-school students and it has no Arabic support. |
| Design system #2 (retry) | `"high school exam prep tutoring professional trustworthy" --design-system` | Minimalism & Swiss style, navy palette, Figtree font. **Used for structure and Latin type.** |
| Product / style (RTL) | `--domain product --rtl`, `--domain style --rtl` | The Educational App product row is only `rtl_level: partial` (claymorphism). Chose **Soft UI Evolution** and **Bento Grid**, both `rtl_level: full` with low accessibility risk. |
| Color | `"education trustworthy focus" --domain color` | LMS palette: teal + amber. Adopted, with the contrast fix below. |
| Typography | `--domain typography` (Arabic), `--domain google-fonts` | Noto Naskh/Sans Arabic pairing (Naskh is too traditional for UI). **IBM Plex Sans Arabic** chosen from the Google Fonts results. |
| Flutter | `--stack flutter` (theme retry, performance, accessibility) | ThemeData + `Theme.of(context)`, dark theme slot, `Semantics`, `TextScaler`, minimal rebuild scope. |
| Charts | `--domain chart` | Score trends use line charts with direct labels and line styles, not hue alone. |

## Identity

**Calm, focused, premium learning.** It's built for teenagers preparing for national exams: serious but
encouraging, with no cartoon styling. Soft depth (low single-layer shadows, 14–20 px radii), generous
whitespace, and bento tiles on dashboards.

## Tokens

| Token | Value | Check |
|---|---|---|
| primary | `#0F766E` | White text 5.47:1. The skill's `#0D9488` was **rejected** (3.74:1 with white). |
| primary-strong | `#115E59` | Pressed state, dark surfaces |
| primary-soft | `#E6F2F0` | Selected backgrounds; primary text on it 4.78:1 |
| accent | `#F59E0B` | Progress and highlights. Ink text only (8.1:1); never white text. |
| accent-text | `#B45309` | Amber text on white 5.02:1 |
| background / surface | `#F4F7F6` / `#FFFFFF` | — |
| ink / muted | `#0F172A` / `#475569` | 16.6:1 / 7.0:1 on background |
| success / warning / error | `#15803D` / `#B45309` / `#DC2626` | ≥ 4.8:1 on white, always paired with an icon and text |
| input border | `#64748B` | ≥ 3:1 non-text contrast |
| dark mode (post-MVP) | primary `#2DD4BF` on `#0B1514` | 9.97:1. Tokens are defined from day one. |

**Type:** Figtree (Latin) + IBM Plex Sans Arabic, bundled in the app (no runtime font fetching).
Scale: 32 / 24 / 20 / 17 / 16 / 14 / 13 (minimum 12). Body line-height 1.5; Arabic about 1.65.
**Spacing:** 4-pt grid (4, 8, 12, 16, 20, 24, 32, 48). **Radius:** 8 for chips, 14 for buttons and
inputs, 18–20 for cards. **Icons:** Lucide line icons at a 1.75 stroke. Never emoji.

## Navigation

* Student bottom navigation: **Home · Discover · Learning · Schedule · Profile**.
* Teacher bottom navigation: **Dashboard · Courses · Students · Schedule · Profile**.
* Both have five items, each with icon and label, and the active item gets a tinted pill. Deep links are
  provided for every key screen.

## Key interaction decisions

* One primary CTA per screen, pinned to the bottom on task screens (lesson, quiz, booking).
* **Booking:** pick a length (1 h / 2 h) → preferred days → open times for each day. A summary shows
  "N lessons / week", and the screen explicitly says that **nothing is reserved until the teacher
  approves**. Unavailable times are shown dashed and labelled, not just greyed out.
* **Lesson player:** embedded player with no share action; completion rules are stated on screen.
* **Quiz:** server-synced timer with tabular figures, a per-question progress strip, and an "answer
  saved" confirmation. Correct answers are hidden until the review policy allows.
* **Teacher dashboard:** pending requests first, then today's sessions, then "need attention"
  (inactive students and low scores).
* Arabic mockup (Student home AR) demonstrates full mirroring. **Open question:** Eastern Arabic
  digits (٠١٢) as drawn, or Western digits (012) in Arabic UI.

## Pre-delivery gate (per UI phase)

The skill's `pro-rules.md` checklist runs on every UI phase: small phone (375 px) and landscape,
largest text size, reduced motion, 48 dp targets, safe areas, screen-reader order and labels, both
locales, and dark-mode contrast once dark mode is enabled.
