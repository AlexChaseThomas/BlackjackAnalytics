# Blackjack Analytics
### A full-stack data pipeline built in C#, SQL, Python, and Power BI

![Status](https://img.shields.io/badge/Status-Phase%203%20Active-green)
![C#](https://img.shields.io/badge/C%23-.NET%208-purple)
![SQLite](https://img.shields.io/badge/Database-SQLite-blue)
![Python](https://img.shields.io/badge/Python-pandas%20%7C%20matplotlib-yellow)
![Power BI](https://img.shields.io/badge/Power%20BI-Planned-lightgrey)

---

## What this project is

I love Blackjack. Not just as a game, but as a system.

Every hand is a decision under uncertainty with a mathematically correct answer. You can know the right move, make it perfectly, and still lose. That tension between sound decision-making and unpredictable outcomes is what makes Blackjack one of the most interesting statistical environments there is. If you understand how people behave at a Blackjack table, you understand something real about how people make decisions when facing risk, scarcity, and incomplete information. That is not a game insight. That is a business insight.

I also love building things. I love coding, I love statistics, and I wanted a project that would let me combine all of it into something I could be proud to show. So I built a full analytics pipeline around a Blackjack game, where the game itself is the data generator and every hand produces a structured record that flows through SQL, Python, and Power BI.

Nobody assigned this to me. I identified gaps in what my background could demonstrate technically, designed a project that would fill them, and built it.

---

## Gameplay

![Gameplay](/screenshots/gameplay_p3_5.18.26_image1.png)
![Gameplay](/screenshots/gameplay_p3_5.18.26_image2.png)

---

## The pipeline

```
C# Console App
    ↓  generates structured session data per hand
SQLite Database
    ↓  stores normalized records across three tables
Python (pandas)
    ↓  cleans, analyzes, and visualizes the data
Power BI Dashboard
    ↓  interactive business intelligence layer
```

Each layer is independently functional. The C# game writes clean data regardless of whether Python has run. The SQL queries work regardless of whether Power BI is connected. This is how real data pipelines are built.

---

## Tech stack

| Layer | Technology | Purpose |
|---|---|---|
| Game engine | C# (.NET 8) | Data generation, OOP, business logic |
| Database | SQLite | Persistent storage, SQL querying |
| Analysis | Python (pandas, matplotlib) | ETL, statistical analysis, charting |
| Visualization | Power BI | Interactive dashboard, KPI reporting |
| Version control | Git / GitHub | Commit history, documentation |

---

## What the data captures

Every hand generates one row in the `GameSessions` table. The schema was designed analytically before a single row was written — every field answers a specific question.

| Field | What it measures |
|---|---|
| `PlayerTotal` / `DealerTotal` | Final hand values |
| `Result` | Win / Loss / Tie / Forfeit |
| `PlayerBusted` / `DealerBusted` | Bust tracking |
| `NumberOfDraws` | Drawing behavior beyond opening hand |
| `BetAmount` | Wagering decisions |
| `TokensBefore` / `TokensAfter` | Token flow per hand |
| `StrategyMode` | Whether suggestions were active |
| `OverrodeSuggestion` | Whether the player ignored a warning |
| `DoubledDown` | Double down decision tracking |
| `DealerVisibleCard` / `DealerVisibleValue` | Dealer upcard — enables win rate by upcard analysis |
| `OpeningPlayerTotal` / `OpeningDealerTotal` | Two-card starting totals before any draws |
| `PlayerHandWasSoft` | Whether player held a soft Ace at any point |
| `HandDurationSeconds` | Decision time per hand |
| `OSVersion` | Environment tracking |

The `Players` table stores lifetime stats per username including token balance, total hands, total wins, favorite strategy mode, and longest win streak. The `Sessions` table stores one row per session for session-level analytics without requiring aggregation across `GameSessions`.

---

## Project structure

```
BlackjackAnalytics/
  ├── Program.cs                  C# game engine and data pipeline
  ├── blackjack.db                SQLite database (pre-seeded)
  ├── analysis/
  │     ├── generate_synthetic_data.py
  │     ├── bust_analysis.py
  │     ├── win_rate_trends.py
  │     ├── strategy_impact.py
  │     └── token_flow.py
  ├── powerbi/
  │     ├── BlackjackDashboard.pbix
  │     └── screenshots/
  ├── docs/
  │     ├── CHANGELOG.md          Full engineering decision log
  │     ├── ROADMAP.md            Phase tracking and future plans
  │     └── ARCHITECTURE.md      System design documentation
  └── README.md
```

---

## Development phases

### ✅ Phase 1 — Game engine (complete)
Built a fully functional Blackjack game with token economy, persistent balances, betting system, strategy suggestion mode, dealer AI (hard 17 rule), and soft Ace handling. Every hand writes a structured record to CSV.

### ✅ Phase 2 — Code cleanup + blackjack realism (complete)
Replaced real-name collection with a username system. Removed PII. Implemented proper casino deal order (player gets two cards, dealer gets one visible + one hole card). Added double down. Extracted `DetermineWinner()` method. Replaced if/else card value chains with a Dictionary lookup. Added opening hand strategy warnings.

### 🔄 Phase 3 — SQLite integration (active)
CSV replaced with a normalized SQLite database across three tables: `Players`, `Sessions`, and `GameSessions`. Full 25-column schema capturing hand analytics, behavioral data, and environmental context. Win streak tracking, daily bonus, and player lifetime stats all persist in the database. Python and Power BI are next.

### ⬜ Phase 4 — Python analysis
Pandas scripts analyzing bust rates, win rate trends, strategy impact, and token flow. Matplotlib charts committed to the repo.

### ⬜ Phase 5 — Power BI dashboard
Four-page interactive dashboard connecting directly to the SQLite database. Session overview, hand analysis, strategy analysis, token economy.

---

## Why I built this

There is a version of this project that never gets built. You take a Kaggle dataset, run some queries, make a chart, call it a portfolio project. I have seen that version. I did not want to build that version.

I wanted to build something where I had to think about every layer: how the data gets created, what fields matter and why, how the schema supports the questions I want to ask later, what a real SQL migration looks like, how Python connects to a database, what makes a dashboard actually useful versus just visual.

Blackjack gave me a reason to care about all of it. The statistics are genuinely interesting to me. The behavioral patterns are genuinely interesting to me. The idea that you can quantify decision quality, track risk behavior over time, and measure whether people follow statistically sound logic when money is on the line — that is not just a game problem. It is a business problem. It is a life problem.

I am self-taught on most of what is in this project. I identified what I needed to know, built the thing, debugged it, improved it, and documented it. That is how I work.

---

## Key engineering decisions

A full log of every architectural decision, bug fix, and design choice is in [`docs/CHANGELOG.md`](/docs/CHANGELOG.md). A few highlights:

**Privacy by design** — The game collects a date of birth for age verification (21+ restriction) but immediately discards it after calculating the player's age. Only an integer age is stored. No real names, no full birthdates, no PII in the database.

**Analytics-first schema** — The data schema was designed with SQL normalization in mind before a single row was written. Every field answers a specific analytical question. Fields that are derivable in SQL were not stored — only fields that require capture at game time are in the schema.

**Three-table normalized design** — `Players` (one row per username), `Sessions` (one row per session), `GameSessions` (one row per hand). Integer foreign keys link all three. This structure supports fast aggregation at any level without scanning the full hand table.

**Iterative improvement** — This project was built in documented phases, with each phase committed to GitHub. The commit history shows how the system evolved from a college project into a portfolio piece. That progression is intentional — it is evidence of how I actually work.

---

## Running the game

**Requirements:** .NET 8 SDK

```bash
git clone https://github.com/AlexChaseThomas/BlackjackAnalytics
cd BlackjackAnalytics
dotnet run
```

The game creates `blackjack.db` automatically on first run. No setup required.

---

## Current analytics output (Phase 3)

The game prints a session summary at the end of each session showing:

- Final token balance
- Strategy mode (On / Off)
- Suggestions overridden
- Hands played
- Wins / Losses / Ties
- Player and dealer bust counts
- Longest win streak this session
- Data saved to: blackjack.db

SQL-powered live analytics will be added via `PrintQuerySummary()` before Phase 3 is closed.

---

## About

Built by Alex Thomas — analytics-oriented builder with a background in operational data systems.

[LinkedIn](https://www.linkedin.com/in/alex-chase-thomas/) • [GitHub](https://github.com/AlexChaseThomas)
