# ROADMAP
## BlackjackAnalytics — Development Phases and Milestones
**Author:** Alex Thomas  
**Repository:** https://github.com/AlexChaseThomas/BlackjackAnalytics  
**Last updated:** May 23, 2026

---

## How to read this document

This roadmap tracks every development phase from initial build through portfolio completion. It is a living document updated at the end of each phase or major milestone.

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

### Key decisions made
- Used C# model classes (POCOs) to represent data — PlayerInfo, SessionRecord, GameStats
- CSV schema designed to match planned SQL table structure from the start
- Educational comments preserved throughout for learning and portfolio value

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
- [x] Real names replaced with username system (3-20 characters, stored lowercase)
- [x] Full DOB collected for verification only, immediately discarded, age integer stored
- [x] Proper casino deal order — player gets two cards, dealer gets one visible and one hole card
- [x] Hole card hidden during player turn, revealed before dealer draws
- [x] Double down mechanic — D key, opening two cards only, doubles bet, one card, auto-stand
- [x] DoubledDown field added to SessionRecord and CSV schema
- [x] Opening hand strategy warning for totals of 17 or higher
- [x] Play again blended with betting prompt
- [x] End of session menu with Play Again and Exit options

### Key decisions made
- No passwords collected — username alone identifies the player
- DOB verified then discarded — only calculated age integer persists in data
- numberOfDraws counts only additional draws beyond opening two cards
- Recursive Main() used for play again — noted as technical debt, acceptable for current scope

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
- [x] Daily bonus LastSeen race condition fixed — RegisterOrLoginPlayer() no longer overwrites LastSeen before CheckDailyBonusDB() reads it
- [x] Username validation rejects spaces

**UI/UX**
- [x] Dealer natural 21 ends hand immediately before player draws
- [x] Dealer reveal animation — "Dealer revealing..." disappears, full dealer hand appears in its place
- [x] Thread.Sleep(1000) pacing between each dealer card draw
- [x] Soft Ace display — "(Ace counting as 1)" shown in DarkYellow when Ace drops from 11 to 1
- [x] Bust message shown when player goes over 21
- [x] Visual pipe separator between cards in hand display
- [x] Session summary bust counts added (PlayerBusts and DealerBusts)
- [x] Input buffer flush fix — buffered keypresses from dealer animation no longer fire on next hand
- [x] Prompt text consistency — all continue/exit prompts unified
- [x] Strategy mode selection redesigned with numbered options and color coding
- [x] Session summary reveal animation — 150ms per line

**SQLite integration**
- [x] System.Data.SQLite.Core NuGet installed
- [x] Three-table normalized schema — Players, Sessions, GameSessions
- [x] Players table — PlayerID, Username, PlayerAge, FirstSeen, LastSeen, TokenBalance, TotalHandsAllTime, TotalWinsAllTime, FavoriteStrategyMode, LongestWinStreak
- [x] Sessions table — one row per session, StartBalance, EndBalance, NetProfit, TotalHands
- [x] GameSessions table — 29-column schema, one row per hand
- [x] InitializeDatabase() — creates all three tables with CREATE TABLE IF NOT EXISTS
- [x] RegisterOrLoginPlayer() — replaces LoadPlayerBalance(), returns (balance, playerID, longestWinStreak) tuple
- [x] InsertGameRecord() — replaces WriteRecordToCSV(), inserts hand and updates Players.TokenBalance
- [x] CheckDailyBonusDB() — reads Players.LastSeen instead of CSV, owns all LastSeen updates
- [x] InsertSessionRecord() — writes session row at session start
- [x] UpdateSessionRecord() — fills in EndTime, TotalHands, EndBalance, NetProfit at session end
- [x] sessionStartBalance snapshot taken after daily bonus for accurate NetProfit calculation
- [x] New analytics fields populating — DealerVisibleCard, DealerVisibleValue, OpeningPlayerTotal, OpeningDealerTotal, PlayerHandWasSoft, HandDurationSeconds, OSVersion
- [x] Win streak tracking — currentWinStreak updates Players.LongestWinStreak in real time across all resolution paths
- [x] UpdatePlayerLifetimeStats() — SQL-side increments for TotalHandsAllTime, TotalWinsAllTime; FavoriteStrategyMode updated at session end

**Strategy recommendation engine**
- [x] GetStrategyRecommendation() — core basic strategy ruleset using dealerVisibleValue as primary variable
- [x] CalculateDealerWinProbability() — dynamic runtime calculation using weighted probability tree traversal
- [x] SimulateDealerDraw() — recursive helper, ref parameters accumulate outcomes across full probability tree
- [x] CalculateDealerBustProbability() and SimulateDealerBust() — for weak dealer card STAND framing
- [x] RecommendedAction, RecommendationFollowed, RiskLevel, DealerWinProbability added to schema (25 → 29 columns)
- [x] PrintStrategyRecommendation() — two-tier display, color-coded controls (green = recommended, red = override, cyan = quit)
- [x] Conditional framing — dealer 4-6 uses bust probability, dealer 2-3 and 7-Ace uses player win probability

**Live analytics and cleanup**
- [x] PrintQuerySummary() — three live SQL queries at session end with reveal animation and footer
- [x] CSV dependency removed entirely — WriteRecordToCSV(), LoadPlayerBalance(), CheckDailyBonus() deleted
- [x] Comment pass on all SQLite methods
- [x] File header updated — PURPOSE, ARCHITECTURE, DATABASE TABLES, KEY DESIGN DECISIONS documented

---

## 🔄 PHASE 4 — Python Synthetic Data Generation
**Status:** Active  
**Dependency:** Phase 3 complete ✅

### Goal
Write Python scripts that connect to the SQLite database and populate it with realistic synthetic player data at scale. The database needs hundreds of rows across multiple players and sessions for the analytics queries and Power BI dashboard to produce statistically meaningful output.

### Why synthetic data
The game is functionally an arcade machine — it is designed to collect data from many players over time. Without deployment to a web platform, real play sessions take too long to accumulate at the scale needed. Synthetic data generation is standard practice for analytics demos and portfolio projects. The Python generator also demonstrates pandas and SQLite integration as standalone skills within the pipeline.

### Milestones
- [ ] Write generate_synthetic_data.py
  - [ ] Multiple player profiles with varying strategy compliance rates
  - [ ] Realistic HandDurationSeconds distributions (fast/moderate/slow players)
  - [ ] Realistic betting behavior patterns
  - [ ] Varied session lengths
  - [ ] Populate 500-1000 hands across 10+ players and 50+ sessions
- [ ] Verify data in DB Browser — confirm all 29 columns populating correctly
- [ ] Verify PrintQuerySummary() produces meaningful output against synthetic dataset
- [ ] Write bust_analysis.py — bust rate by opening hand total
- [ ] Write win_rate_trends.py — win rate over sessions
- [ ] Write strategy_impact.py — win rate followed vs ignored recommendations
- [ ] Write token_flow.py — token balance trends per player
- [ ] Export all charts as .png into /analysis/charts/
- [ ] Commit: Phase 4 complete

---

## ⬜ PHASE 5 — Power BI Dashboard
**Status:** Planned  
**Dependency:** Phase 4 complete

### Goal
Build a four-page interactive Power BI dashboard that connects directly to the SQLite database and visualizes the full behavioral analytics dataset.

### Milestones
- [ ] Connect Power BI Desktop to blackjack.db
- [ ] Build Page 1 — Session Overview (KPIs, win/loss/tie breakdown, sessions over time)
- [ ] Build Page 2 — Hand Analysis (player total distribution, bust rate trends, average totals by result)
- [ ] Build Page 3 — Strategy Analysis (win rate followed vs ignored, override frequency, recommendation accuracy)
- [ ] Build Page 4 — Token Economy (balance trends, bet sizing patterns, session profitability)
- [ ] Export dashboard screenshots as .png
- [ ] Add screenshots to /powerbi/screenshots/ and README.md
- [ ] Commit .pbix file and screenshots
- [ ] Commit: Phase 5 complete

---

## 💡 FUTURE IDEAS (not yet scoped)

- **Blazor WebAssembly front end** — game playable in browser, increases recruiter accessibility significantly, enables real multi-user data collection
- **Split hands mechanic** — adds another betting behavior data point, makes game more realistic
- **Card counting detection** — track running count, analyze whether count correlates with player decisions
- **Multi-game casino platform** — shared core library, add Poker, Craps, Roulette as separate games
- **Leaderboard** — SELECT + ORDER BY on TokensAfter, simple SQL feature, good dashboard visual
- **Achievement system** — query-based, no schema change needed
- **Soft 17 dealer rule option** — some casinos hit on soft 17, configurable option, interesting analytics variable
- **Azure or cloud deployment** — host database in the cloud, enable real multi-user capability

---

## MILESTONE SUMMARY

| Phase | Status | Committed |
|-------|--------|-----------|
| Phase 1 — Game Engine | ✅ Complete | May 7, 2026 |
| Phase 2 — Code Cleanup + Realism | ✅ Complete | May 11, 2026 |
| Phase 3 — SQLite + Analytics Engine | ✅ Complete | May 22, 2026 |
| Phase 4 — Python Synthetic Data | 🔄 Active | — |
| Phase 5 — Power BI Dashboard | ⬜ Planned | — |

---

*This document is updated at the end of each development phase. Next update: Phase 4 Complete*
