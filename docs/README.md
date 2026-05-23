# Blackjack Analytics
### A behavioral data pipeline built in C#, SQLite, Python, and Power BI

![Status](https://img.shields.io/badge/Status-Phase%203%20Complete-brightgreen)
![C#](https://img.shields.io/badge/C%23-.NET%208-purple)
![SQLite](https://img.shields.io/badge/Database-SQLite-blue)
![Python](https://img.shields.io/badge/Python-pandas%20%7C%20matplotlib-yellow)
![Power BI](https://img.shields.io/badge/Power%20BI-Phase%205-lightgrey)

---
[▶ Watch demo](/demo/blackjack_analytics_demo.mp4)
---

## What this is

Blackjack is one of the most statistically interesting environments you can build around. Every hand forces a decision under uncertainty — there is a mathematically correct move, real money on the line, and no guarantee the right decision produces the right outcome. That tension is not unique to cards. It shows up in any business where people make decisions under pressure with incomplete information and real consequences. If you understand how people behave at a blackjack table, you understand something real about decision-making under risk. That is not a game insight. That is a business insight.

This project is a full end-to-end behavioral analytics pipeline built around that environment. Think of it as an arcade machine — designed to collect structured data from every player who sits down, record every decision and outcome in real time, and feed that data into an analytics layer built to answer one core question: does access to better information actually improve decision-making, and does that show up in the results?

The game is the data source. SQLite is the storage layer. Python generates synthetic player data at the scale needed for statistically meaningful analysis. Power BI surfaces the insights. Each layer is independently functional — the game writes clean structured data whether or not Python has run, and the SQL queries work whether or not Power BI is connected. This is how real data pipelines are built.

---

## The pipeline

```
C# Console Application
    ↓  every hand writes one structured record in real time
SQLite Database  (blackjack.db)
    ↓  three normalized tables, 29-column behavioral schema
Python  (pandas)
    ↓  synthetic data generation, statistical analysis, matplotlib visualizations
Power BI Dashboard
    ↓  interactive visualization and KPI reporting
```

---

## Gameplay

![Login and session setup](/screenshots/gameplay_p3_login.png)

The game enforces a 21+ age gate, assigns a unique session ID, and begins writing to the database before the first card is dealt. Token balance, lifetime stats, and win streaks all persist across sessions via the Players table. The daily bonus system tracks time since last login at the database level.

![Strategy tip and recommendation engine](/screenshots/gameplay_p3_strategy_tip.png)

Players can enable a live strategy recommendation engine. Before each decision, the engine calculates the probability the dealer beats the player's current total by simulating all possible dealer draw sequences weighted by card frequency — not by looking up a static strategy table. Every recommendation, whether followed or ignored, is recorded alongside the probability estimate that drove it.

---

## Dealer reveal and hand resolution

The dealer's hole card is hidden until the player's turn ends. Each subsequent dealer draw is revealed one card at a time with deliberate pacing.

![Dealer reveal — win](/screenshots/gameplay_p3_dealer_reveal1.png)

![Tip firing mid-hand, dealer draw sequence, loss resolution](/screenshots/gameplay_p3_dealer_reveal2.png)

---

## Live database analytics

At the end of every session, three SQL queries run against the live database and surface behavioral insights directly in the console.

![Live database analytics output](/screenshots/gameplay_p3_analytics.png)

**Current Session Metrics** queries this session's GameSessions rows for hands played, win rate, net profit, and what percentage of recommendations the player followed.

**Strategy Recommendation Performance** queries lifetime data to compare win rate when following the engine versus ignoring it. Across the current dataset, players who followed recommendations won at a 41.7% rate versus 34.8% for those who ignored them.

**Decision Latency Analysis** groups all hands by how long the player took to decide and shows win rate per bucket. The pattern — faster decisions correlating with better outcomes — is consistent with the hypothesis that hesitation reflects uncertainty on hands the player is likely to lose regardless.

The footer shows total records, sessions, and players queried, giving immediate context for the statistical weight of the numbers above.

---

## The database

Three normalized tables linked by integer foreign keys. The schema was designed analytically before any code was written — every field answers a specific question that could not be derived after the fact.

---

### GameSessions — hand-level telemetry

![GameSessions table schema](/screenshots/db_schema_gamesessions.png)

The primary analytics table. One row per hand, 29 columns. Fields like `HandDurationSeconds`, `RecommendedAction`, `RecommendationFollowed`, `DealerWinProbability`, and `RiskLevel` were designed specifically to support behavioral analysis that existing blackjack datasets do not capture — the relationship between information access, decision quality, and outcomes.

---

### Players — lifetime stats

![Players table schema](/screenshots/db_schema_players.png)

One row per unique username. Stores token balance, total hands, total wins, favorite strategy mode, and longest win streak. All fields accumulate across sessions using SQL-side increments to avoid race conditions.

---

### Sessions — session-level tracking

![Sessions table schema](/screenshots/db_schema_sessions.png)

One row per session. Captures start and end balance, net profit, total hands, and timestamps. Exists as a separate table so session-level queries can run without aggregating across the full GameSessions table.

---

## Analytical queries

The compliance query is the centerpiece of the analytics layer. It answers the question the recommendation engine was built to investigate.

![Compliance query in DB Browser](/screenshots/db_query_compliance.png)

```sql
SELECT
    CASE WHEN RecommendationFollowed = 1
         THEN 'Followed' ELSE 'Ignored' END AS Compliance,
    COUNT(*) AS TotalGames,
    SUM(CASE WHEN Result = 'Win' THEN 1 ELSE 0 END) AS Wins,
    ROUND(100.0 * SUM(CASE WHEN Result = 'Win' THEN 1 ELSE 0 END)
        / COUNT(*), 2) AS WinRatePercent
FROM GameSessions
WHERE StrategyMode = 'On'
AND RecommendedAction != 'NONE'
GROUP BY RecommendationFollowed
ORDER BY RecommendationFollowed DESC;
```

Additional queries in the project cover win rate by dealer upcard, bust rate by opening hand total, session profitability trends, risk level accuracy against actual loss rates, and decision latency versus outcome correlation.

---

## Key engineering decisions

**Hand enumeration model over static lookup** — The strategy engine calculates dealer win probability dynamically from the current game state using weighted probability tree traversal. Standard casino strategy cards are static tables derived from historical simulation — the platform looks up the answer and no calculation happens at runtime. This system performs the calculation fresh on every hand from the actual cards in play. The practical difference in a single-deck game is small. The architectural difference matters: every recommendation is grounded in real probability derived from game state, not a static rule applied regardless of context.

**Analytics-first schema design** — The 29-column schema was designed before any code was written. Fields like `HandDurationSeconds` and `RecommendationFollowed` cannot be derived after the fact — they have to be captured at the moment of decision. Designing for the questions first and the schema second is the same discipline that separates useful data collection from data that accumulates without purpose.

**Three-table normalized design** — `Players`, `Sessions`, and `GameSessions` are linked by integer foreign keys. This supports fast aggregation at any level — player lifetime stats, session-level profitability, or individual hand analysis — without scanning the full hand table for every query.

**Sessions INSERT at start, UPDATE at end** — Each session is written to the database the moment it begins, not when it ends. This means session data exists and is queryable even for incomplete sessions, which mirrors how production session tracking systems work.

**Privacy by design** — The game collects a date of birth for age verification only. The full date is discarded immediately after the age is calculated. Only the integer age is written to the database. No names, no full birthdates, no PII stored anywhere in the system.

---

## Development phases

| Phase | Status | Description |
|---|---|---|
| 1 — Game engine | ✅ Complete | Core game, betting system, strategy mode, CSV logging |
| 2 — Realism + cleanup | ✅ Complete | 52-card deck, double down, proper deal order, PII removal |
| 3 — SQLite + analytics | ✅ Complete | Three-table schema, recommendation engine, live SQL queries |
| 4 — Python synthetic data | 🔄 Active | Simulate realistic player behavior at scale to populate the database |
| 5 — Power BI dashboard | ⬜ Planned | Interactive dashboard over the SQLite database |

Full engineering decision log: [CHANGELOG](/docs/CHANGELOG.md) · [ROADMAP](/docs/ROADMAP.md)

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

## About

Built by Alex Thomas — analytics professional with a background in operational systems and data pipeline development.

[LinkedIn](https://www.linkedin.com/in/alex-chase-thomas/) · [GitHub](https://github.com/AlexChaseThomas)
