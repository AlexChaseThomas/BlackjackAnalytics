# ROADMAP
## BlackjackAnalytics — Development Phases and Milestones
**Author:** Alex Thomas  
**Repository:** https://github.com/AlexChaseThomas/BlackjackAnalytics  
**Last updated:** May 29, 2026

---

## How to read this document

This roadmap tracks every development phase from initial build through portfolio completion.

Status indicators:
- ✅ Complete
- 🔄 In progress
- ⬜ Planned
- 💡 Future idea (not yet scoped)

---

## Project goal

Build a full-stack behavioral analytics pipeline using a Blackjack game as the data generator. Demonstrate end-to-end technical ability across C#, SQL, Python, and Power BI in a single cohesive project that is publicly visible, well documented, and directly relevant to data analytics and business analyst roles.

---

## ✅ PHASE 1 — Game Engine
**Status:** Complete  
**Committed:** May 7, 2026

### Goal
Build a stable, realistic Blackjack game that generates clean, structured analytics data on every hand.

### Completed milestones
- [x] Password gate (class project requirement, removed in Phase 2)
- [x] Player identity collection (real names and DOB — redesigned for privacy in Phase 2)
- [x] DOB-based 21+ age verification
- [x] Token economy with starting balance of 100
- [x] CSV session persistence — balance carries across sessions
- [x] Session ID generation from timestamp
- [x] Daily login bonus system (50 tokens after 24 hours)
- [x] Strategy suggestion mode with bust percentage warnings
- [x] Betting system with minimum/maximum enforcement
- [x] Double-ESC forfeit confirmation
- [x] Dealer AI — hard 17 rule (draw on 16 or below, stand on 17 or above)
- [x] Soft Ace handling for player and dealer
- [x] CSV schema with 16 columns covering identity, gameplay, wagering, and strategy
- [x] Session statistics summary at end of session
- [x] GameStats class tracking wins, losses, ties, busts, strategy overrides

---

## ✅ PHASE 2 — Code Cleanup, Identity System, Blackjack Realism
**Status:** Complete  
**Committed:** May 11, 2026

### Goal
Clean up the codebase, remove PII, implement proper casino blackjack mechanics, and improve analytics data quality.

### Completed milestones
- [x] Single shared Random instance at class level (seed collision bug fix)
- [x] Dictionary replaces all three if/else card value chains
- [x] DetermineWinner() method extracted from Main()
- [x] Password gate removed
- [x] Real names replaced with username system (3–20 characters, stored lowercase)
- [x] Full DOB collected for verification only, immediately discarded, age integer stored
- [x] Proper casino deal order — player gets two cards, dealer gets one visible and one hole card
- [x] Hole card hidden during player turn, revealed before dealer draws
- [x] Double down mechanic — D key, opening two cards only, doubles bet, one card, auto-stand
- [x] DoubledDown field added to SessionRecord and CSV schema
- [x] Opening hand strategy warning for totals of 17 or higher
- [x] Play again blended with betting prompt
- [x] End of session menu with Play Again and Exit options

---

## ✅ PHASE 3 — 52-Card Deck, Game Engine Polish, SQLite Integration
**Status:** Complete  
**Committed:** May 22, 2026

### Goal
Complete game engine realism, fix all known data integrity and gameplay bugs, replace CSV with a normalized SQLite database, build a context-aware strategy recommendation engine, and deliver live SQL analytics at session end.

### Completed milestones

**52-card deck and game engine**
- [x] Card class with Name, Suit, and ToString() override
- [x] BuildDeck() — creates full 52-card deck (13 values x 4 suits)
- [x] ShuffleDeck() — Fisher-Yates algorithm, unbiased shuffle
- [x] DealCard() — deals from top of deck, auto-reshuffles when fewer than 10 cards remain
- [x] Duplicate cards within a hand now impossible
- [x] CalculateBustChance refactored to Dictionary, consistent with codebase pattern

**Bug fixes**
- [x] Opening Blackjack dealer draw fixed — dealer draws to 17 even on player Blackjack
- [x] Dealer always reveals hole card even when player busts
- [x] Dealer always completes hand to 17 regardless of player bust — DealerTotal now accurate on all rows
- [x] DetermineWinner both-bust rule fixed — player always loses when they bust
- [x] DetermineWinner both-21 tie fixed — both hitting 21 correctly returns Tie
- [x] overrodeSuggestion logic fixed — only true when player was warned AND drew anyway
- [x] warningActive flag introduced to track warning state between keypresses
- [x] Exit bug fixed — typing exit at bet prompt no longer deals a phantom hand
- [x] Hands played count fixed — stats.TotalGames no longer increments on exit
- [x] Daily bonus LastSeen race condition fixed
- [x] Username validation rejects spaces

**UI/UX**
- [x] Dealer natural 21 ends hand immediately before player draws
- [x] Dealer reveal animation with pacing and cursor overwrite
- [x] Soft Ace display — "(Ace counting as 1)" shown when Ace drops from 11 to 1
- [x] Bust message, visual hand separator, session summary bust counts, input buffer flush fix
- [x] Prompt text consistency — all continue/exit prompts unified
- [x] Strategy mode selection redesigned with numbered options and color coding
- [x] Session summary reveal animation — 150ms per line
- [x] Redundant exit prompt removed

**SQLite integration**
- [x] Three-table normalized schema — Players, Sessions, GameSessions (29 columns)
- [x] RegisterOrLoginPlayer(), InsertGameRecord(), CheckDailyBonusDB()
- [x] InsertSessionRecord() and UpdateSessionRecord()
- [x] UpdatePlayerLifetimeStats() — SQL-side increments for TotalHandsAllTime, TotalWinsAllTime
- [x] Win streak tracking — updates Players.LongestWinStreak in real time

**Strategy recommendation engine**
- [x] GetStrategyRecommendation() — context-aware, uses dealerVisibleValue as primary variable
- [x] CalculateDealerWinProbability() — weighted probability tree traversal at runtime
- [x] CalculateDealerBustProbability() — conditional framing for weak dealer cards
- [x] PrintStrategyRecommendation() — two-tier display, color-coded controls
- [x] RecommendedAction, RecommendationFollowed, RiskLevel, DealerWinProbability in schema

**Live analytics and cleanup**
- [x] PrintQuerySummary() — three live SQL queries at session end
- [x] CSV dependency removed entirely
- [x] Comment pass on all SQLite methods
- [x] File header updated — PURPOSE, ARCHITECTURE, DATABASE TABLES, KEY DESIGN DECISIONS

---

## ✅ PHASE 4 — Python Synthetic Data Generation
**Status:** Complete  
**Committed:** May 23, 2026

### Goal
Populate the database with realistic synthetic player data at the scale needed for statistically meaningful analytics and dashboard visualization.

### Why synthetic data
The game is functionally an arcade machine: it is designed to collect data from many players over time. Without deployment to a web platform, real play sessions take too long to accumulate at the scale needed. Synthetic data generation is standard practice for analytics demos and portfolio projects. The generator is documented openly and uses real game logic, meaning all outcomes are derived from actual card probabilities.

### Completed milestones
- [x] generate_synthetic_data.py created in /analysis folder
- [x] Six behavioral archetypes — disciplined, impulsive, deliberate, casual, risk_taker, novice
- [x] Individual player variation added on top of archetype baselines
- [x] Weighted probability lookup table built with lru_cache memoization — mirrors C# probability model
- [x] Real game logic simulation — cards dealt, hands play out, outcomes determined by actual probability
- [x] 250 synthetic players generated across all archetypes
- [x] Age distribution 21–68 years — enables age vs behavior analysis
- [x] All 29 GameSessions columns populated correctly
- [x] Players, Sessions, and GameSessions tables all populated
- [x] Final database: 262 players (250 synthetic + 12 real), 1,377 sessions, 9,511 hands
- [x] PrintQuerySummary() verified — meaningful output confirmed against full dataset
- [x] Demo video recorded and committed
- [x] Phase 4 complete commit — May 23, 2026

---

## ✅ PHASE 5 — Power BI Dashboard
**Status:** Complete  
**Committed:** May 29, 2026

### Goal
Build a four-page interactive Power BI dashboard that connects directly to the SQLite database and visualizes the full behavioral analytics dataset.

### Completed milestones
- [x] SQLite3 ODBC driver installed and configured
- [x] Power BI Desktop connected to blackjack.db via ODBC
- [x] All three tables loaded — Players, Sessions, GameSessions
- [x] Table relationships confirmed — Players → Sessions (1:many), Players → GameSessions (1:many)
- [x] LoginTime, StartTime, EndTime converted from TEXT to DateTime in Power Query
- [x] DAX measures created — Win Rate, Win Rate Followed, Win Rate Ignored, Net Token Flow, Player Net PnL, Total Tokens Wagered, Avg Bet Strategy On/Off, Avg Decision Time, Compliance Rate, Player Win Rate, Hands Won Following, Hands Lost Ignoring, Token Deviation
- [x] Calculated columns — SpeedBucket, SpeedBucketSort, AgeBracket, AgeBracketSort, MonthYear
- [x] Page 1 — Overview: KPI cards, compliance bar chart (40.1% vs 29.6%), win/loss/tie donut, hands by result
- [x] Page 2 — Token Economy: monthly volume column chart, house edge line chart, avg bet gauges (25.04 on vs 40.11 off)
- [x] Page 3 — Behavioral Analytics: decision speed vs win rate line chart (peak at 7-10s), age vs decision time, compliance rate by age group
- [x] Page 4 — Player Intelligence: top 15 leaderboard, compliance vs win rate scatter, strategy mode distribution
- [x] Color scheme applied — black centerpiece, red primary, gray secondary (playing card aesthetic)
- [x] Axis disclosure footnotes on compressed Y axes
- [x] Insight annotations added to all four pages
- [x] Dashboard screenshots committed to /screenshots/
- [x] BlackjackAnalytics.pbix committed to repository
- [x] Phase 5 complete commit — May 29, 2026

---

## ✅ PORTFOLIO POLISH
**Status:** Complete

### Completed milestones
- [x] README.md updated to reflect all five phases complete
- [x] CHANGELOG.md updated through Phase 5
- [x] ROADMAP.md updated through Phase 5
- [x] Demo video recorded and published (unlisted): https://youtu.be/IBBQVjk49mE
- [x] Demo video linked in README with thumbnail
- [x] DB Browser compliance query screenshot committed
- [x] All gameplay screenshots committed to /screenshots/
- [x] Dashboard preview screenshots committed to /screenshots/

---

## 💡 FUTURE IDEAS (not yet scoped)

- **Blazor WebAssembly front end** — game playable in browser, enables real multi-user data collection
- **Split hands mechanic** — adds another betting behavior data point
- **Card counting detection** — track running count, analyze whether count correlates with player decisions
- **Multi-game casino platform** — shared core library, Poker, Craps, Roulette as separate games
- **Soft 17 dealer rule option** — configurable, adds an analytically interesting variable
- **Azure or cloud deployment** — host database in the cloud, enable real multi-user capability

---

## MILESTONE SUMMARY

| Phase | Status | Committed |
|-------|--------|-----------|
| Phase 1 — Game Engine | ✅ Complete | May 7, 2026 |
| Phase 2 — Code Cleanup + Realism | ✅ Complete | May 11, 2026 |
| Phase 3 — SQLite + Analytics Engine | ✅ Complete | May 22, 2026 |
| Phase 4 — Python Synthetic Data | ✅ Complete | May 23, 2026 |
| Phase 5 — Power BI Dashboard | ✅ Complete | May 29, 2026 |

---

*Project complete. All five phases delivered.*
