# ROADMAP
## BlackjackAnalytics — Development Phases and Milestones
**Author:** Alex Thomas  
**Repository:** https://github.com/AlexChaseThomas/BlackjackAnalytics  
**Last updated:** May 2026

---

## How to read this document

This roadmap tracks every development phase from initial build through portfolio completion. It is a living document updated at the end of each phase or major milestone.

Status indicators:
- ✅ Complete
- 🔄 In progress
- ⬜ Planned
- 💡 Future idea (not yet scoped)

---
a# ROADMAP
## BlackjackAnalytics — Development Phases and Milestones
**Author:** Alex Thomas
**Repository:** https://github.com/AlexChaseThomas/BlackjackAnalytics
**Last updated:** May 2026

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

Build a full-stack data analytics pipeline using a Blackjack game as the data generator. Demonstrate end-to-end technical ability across C#, SQL, Python, and Power BI in a single cohesive project that is publicly visible, well documented, and directly relevant to data analytics roles.

---

## ✅ PHASE 1 — Game Engine
**Status:** Complete
**Committed:** May 2026

### Completed milestones
- [x] Password gate (class project requirement, later removed in Phase 2)
- [x] Player identity collection (first name, last name, DOB — later redesigned in Phase 2)
- [x] DOB-based 21+ age verification
- [x] Token economy with starting balance of 100
- [x] CSV session persistence — balance carries across sessions
- [x] Session ID generation from timestamp
- [x] Daily login bonus system (50 tokens after 24 hours)
- [x] Strategy suggestion mode with bust percentage warnings
- [x] Betting system with minimum/maximum enforcement
- [x] Double-ESC forfeit confirmation
- [x] Dealer AI — hard 17 rule
- [x] Soft Ace handling for player and dealer
- [x] CSV schema with 16 columns
- [x] Session statistics summary at end of session
- [x] GameStats class tracking wins, losses, ties, busts, strategy overrides

---

## ✅ PHASE 2 — Code Cleanup, Identity System, Blackjack Realism
**Status:** Complete
**Committed:** May 2026

### Completed milestones
- [x] Single shared Random instance at class level (seed collision bug fix)
- [x] Dictionary replaces all three if/else card value chains
- [x] DetermineWinner() method extracted from Main()
- [x] Password gate removed
- [x] Real names replaced with username system
- [x] Full DOB collected for verification only, immediately discarded, age integer stored
- [x] Username validation (3-20 characters, stored lowercase)
- [x] Proper casino deal order — player gets two cards, dealer gets one visible and one hole card
- [x] Hole card hidden during player turn, revealed before dealer draws
- [x] Double down mechanic — D key, opening two cards only, doubles bet, one card, auto-stand
- [x] DoubledDown field added to SessionRecord and CSV schema
- [x] Opening hand strategy warning for totals of 17 or higher
- [x] Play again blended with betting prompt
- [x] End of session menu with Play Again and Exit options

---

## 🔄 PHASE 3 — 52-Card Deck, Game Engine Polish, SQLite Integration
**Status:** Active — SQLite integration complete, PrintQuerySummary() and final cleanup remaining
**Target:** Current development session

### Completed milestones
- [x] Card class with Name, Suit, and ToString() override
- [x] BuildDeck() — creates full 52-card deck (13 values x 4 suits)
- [x] ShuffleDeck() — Fisher-Yates algorithm, unbiased shuffle
- [x] DealCard() — deals from top of deck, auto-reshuffles when fewer than 10 cards remain
- [x] Duplicate cards within a hand now impossible
- [x] Opening Blackjack dealer draw fixed — dealer draws to 17 even on player Blackjack
- [x] Dealer always reveals hole card even when player busts
- [x] Dealer always completes hand to 17 regardless of player bust
- [x] DetermineWinner both-bust rule fixed — player always loses when they bust
- [x] DetermineWinner both-21 tie fixed
- [x] overrodeSuggestion logic fixed — only true when player was warned AND drew anyway
- [x] warningActive flag introduced to track warning state between keypresses
- [x] Dealer reveal dramatic pause — Thread.Sleep animation
- [x] Exit bug fixed — typing exit at bet prompt no longer deals a phantom hand
- [x] Session summary shows PlayerBusts and DealerBusts counts
- [x] CalculateBustChance replaced with Dictionary-based implementation
- [x] Username validation rejects spaces
- [x] Soft Ace display shows (Ace counting as 1) when Ace has dropped from 11 to 1
- [x] Dealer natural 21 ends hand immediately
- [x] Player and dealer hand display shows full card history after every draw
- [x] Three-table SQLite schema — Players, Sessions, GameSessions
- [x] Players table — PlayerID, Username, PlayerAge, FirstSeen, LastSeen, TokenBalance, TotalHandsAllTime, TotalWinsAllTime, FavoriteStrategyMode, LongestWinStreak
- [x] Sessions table — one row per session for session-level analytics
- [x] GameSessions table — 25-column schema, one row per hand
- [x] InitializeDatabase() — creates all three tables with IF NOT EXISTS
- [x] RegisterOrLoginPlayer() — replaces LoadPlayerBalance(), returns (balance, playerID, longestWinStreak) tuple
- [x] InsertGameRecord() — replaces WriteRecordToCSV(), inserts hand and updates Players.TokenBalance
- [x] CheckDailyBonusDB() — reads Players.LastSeen instead of CSV
- [x] PlayerID foreign key wired into GameSessions
- [x] New fields populating — DealerVisibleCard, DealerVisibleValue, OpeningPlayerTotal, OpeningDealerTotal, PlayerHandWasSoft, HandDurationSeconds, OSVersion
- [x] Win streak tracking — currentWinStreak in session loop, updates Players.LongestWinStreak in real time
- [x] Session summary updated — longest win streak displayed, data saved to blackjack.db

### Remaining before Phase 3 close
- [ ] Sessions table INSERT at session start, UPDATE at session end
- [ ] Players lifetime stats updated at session end — TotalHandsAllTime, TotalWinsAllTime, FavoriteStrategyMode
- [ ] PrintQuerySummary() — live SQL analytics printed at end of session
- [ ] Comment pass on all SQLite methods
- [ ] Delete CSV dependency — remove WriteRecordToCSV(), LoadPlayerBalance(), CheckDailyBonus()
- [ ] Final Phase 3 commit

---

## ⬜ PHASE 4 — Python Analysis Scripts
**Status:** Planned
**Dependency:** Phase 3 complete

### Milestones
- [ ] Verify Python installation and pip
- [ ] Install pandas and matplotlib
- [ ] Create /analysis folder in repository
- [ ] Write generate_synthetic_data.py — populates database with 300-500 realistic fake sessions
- [ ] Write bust_analysis.py
- [ ] Write win_rate_trends.py
- [ ] Write strategy_impact.py
- [ ] Write token_flow.py
- [ ] Write draws_vs_outcome.py
- [ ] Export all charts as .png files into /analysis/charts/
- [ ] Commit: Phase 4 complete

---

## ⬜ PHASE 5 — Power BI Dashboard
**Status:** Planned
**Dependency:** Phase 4 complete

### Milestones
- [ ] Download Power BI Desktop
- [ ] Install SQLite ODBC driver
- [ ] Connect Power BI to blackjack.db
- [ ] Build Page 1 — Session Overview
- [ ] Build Page 2 — Hand Analysis
- [ ] Build Page 3 — Strategy Analysis
- [ ] Build Page 4 — Token Economy
- [ ] Export dashboard screenshots
- [ ] Add screenshots to /powerbi/screenshots/ and README
- [ ] Commit: Phase 5 complete

---

## ⬜ GITHUB AND PORTFOLIO POLISH
**Status:** Planned
**Dependency:** Phase 5 complete

### Milestones
- [ ] Final README pass — add dashboard screenshots inline
- [ ] Write docs/ARCHITECTURE.md
- [ ] Final CHANGELOG update
- [ ] Add repo topics on GitHub
- [ ] Pin repository to GitHub profile
- [ ] Prepare 2-minute verbal explanation for interviews

---

## 💡 FUTURE IDEAS (not yet scoped)

- Blazor WebAssembly front end
- Split hands mechanic
- Card counting detection
- Multi-game casino platform
- Leaderboard
- Achievement system
- Context-aware strategy suggestions — factor in dealer visible card
- Soft 17 dealer rule option
- Azure or cloud deployment

---

## MILESTONE SUMMARY

- Phase 1 complete — DONE
- Phase 2 complete — DONE
- Phase 3 complete — ACTIVE — SQLite integration live, cleanup remaining
- Phase 4 complete — PLANNED
- Phase 5 complete — PLANNED

---

This document is updated at the end of each development phase.
Next update: Phase 3 Complete

## Project goal

Build a full-stack data analytics pipeline using a Blackjack game as the data generator. Demonstrate end-to-end technical ability across C#, SQL, Python, and Power BI in a single cohesive project that is publicly visible, well documented, and directly relevant to data analytics roles.

---

## ✅ PHASE 1 — Game Engine
**Status:** Complete  
**Committed:** May 2026

### Goal
Build a stable, realistic Blackjack game that generates clean, structured analytics data on every hand.

### Completed milestones
- [x] Password gate (class project requirement, later removed in Phase 2)
- [x] Player identity collection (first name, last name, DOB — later redesigned in Phase 2)
- [x] DOB-based 21+ age verification
- [x] Token economy with starting balance of 100
- [x] CSV session persistence — balance carries across sessions
- [x] Session ID generation from timestamp
- [x] Daily login bonus system (50 tokens after 24 hours)
- [x] Strategy suggestion mode with bust percentage warnings
- [x] Betting system with minimum/maximum enforcement
- [x] Double-ESC forfeit confirmation
- [x] Dealer AI — hard 17 rule (draw on 16 or below, stand on 17 or higher)
- [x] Soft Ace handling for player and dealer
- [x] CSV schema with 16 columns covering identity, gameplay, wagering, and strategy
- [x] Session statistics summary at end of session
- [x] GameStats class tracking wins, losses, ties, busts, strategy overrides

### Key decisions made
- Used C# model classes (POCOs) to represent data — PlayerInfo, SessionRecord, GameStats
- CSV schema designed to match planned SQL table structure from the start
- Educational comments preserved throughout for learning and portfolio value

### Known issues resolved
- Dealer draw bug — dealer was coupled to player draw loop instead of drawing independently
- Duplicate output bug — card and total printed twice per draw
- Soft Ace bug — Ace + Ace incorrectly caused immediate bust

---

## ✅ PHASE 2 — Code Cleanup, Identity System, Blackjack Realism
**Status:** Complete  
**Committed:** May 2026

### Goal
Clean up the codebase, remove PII, implement proper casino blackjack mechanics, and improve analytics data quality.

### Completed milestones
- [x] Single shared Random instance at class level (seed collision bug fix)
- [x] Dictionary replaces all three if/else card value chains
- [x] DetermineWinner() method extracted from Main()
- [x] Password gate removed
- [x] Real names replaced with username system
- [x] Full DOB collected for verification only, immediately discarded, age integer stored
- [x] Username validation (3-20 characters, stored lowercase)
- [x] Proper casino deal order — player gets two cards, dealer gets one visible and one hole card
- [x] Hole card hidden during player turn, revealed before dealer draws
- [x] Double down mechanic — D key, opening two cards only, doubles bet, one card, auto-stand
- [x] DoubledDown field added to SessionRecord and CSV schema
- [x] Opening hand strategy warning for totals of 17 or higher
- [x] Play again blended with betting prompt
- [x] End of session menu with Play Again and Exit options
- [x] Exit via typing "exit" at betting prompt

### Key decisions made
- No passwords collected — username alone identifies the player
- DOB verified then discarded — only calculated age integer persists in data
- numberOfDraws counts only additional draws beyond opening two cards
- Double down available only on opening two cards (standard casino rule)
- Recursive Main() used for play again — noted as technical debt for Phase 3

### Known limitations (carried forward)
- Username uniqueness not enforced at CSV level — resolved in Phase 3 via SQL UNIQUE constraint
- Exit during betting requires typing "exit" — resolved in Phase 3 with ReadKey betting flow
- Recursive Main() for play again — resolved in Phase 3 with top-level game loop

---

## 🔄 PHASE 3 — 52-Card Deck, Game Engine Polish, SQLite Integration
**Status:** In progress  
**Target:** Current development session

### Goal
Complete game engine realism, fix all known data integrity and gameplay bugs, and replace the CSV flat file with a normalized SQLite database. Add SQL-powered live analytics at end of each session.

### Completed milestones
- [x] Card class with Name, Suit, and ToString() override
- [x] BuildDeck() — creates full 52-card deck (13 values x 4 suits)
- [x] ShuffleDeck() — Fisher-Yates algorithm, unbiased shuffle
- [x] DealCard() — deals from top of deck, auto-reshuffles when fewer than 10 cards remain
- [x] Duplicate cards within a hand now impossible
- [x] Opening Blackjack dealer draw fixed — dealer draws to 17 even on player Blackjack
- [x] Dealer always reveals hole card even when player busts
- [x] Dealer always completes hand to 17 regardless of player bust — DealerTotal now accurate
- [x] DetermineWinner both-bust rule fixed — player always loses when they bust (house rule)
- [x] DetermineWinner both-21 tie fixed — both hitting 21 correctly returns Tie
- [x] Duplicate BLACKJACK print fixed in opening deal path
- [x] Prompt text consistency — all continue/exit prompts unified
- [x] overrodeSuggestion logic fixed — only true when player was warned AND drew anyway
- [x] warningActive flag introduced to track warning state between keypresses
- [x] Dealer reveal dramatic pause — Dealer revealing... with Thread.Sleep animation
- [x] Dealer draw animation — Dealer drawing... pause before each dealer card
- [x] Exit bug fixed — typing exit at bet prompt no longer deals a phantom hand
- [x] Session summary double-print fixed
- [x] InitializeDatabase() — creates blackjack.db with Players and GameSessions tables
- [x] System.Data.SQLite.Core v1.0.119 NuGet installed
- [x] File header condensed to professional format
- [x] Stale phase comments and unused imports removed

### In progress
- [ ] Fix 5 — Strategy warning on N stand waits for next keypress before standing
- [ ] Fix 6 — stats.TotalGames increments before exit check (hands played count off by one)
- [ ] Fix 7 — Running hand display (show all cards held, not just last drawn)
- [ ] Fix 8 — Bet confirmation line before cards are dealt
- [ ] Fix 9 — Visual separator between hands
- [ ] Fix 10 — Bust message before dealer turn
- [ ] Fix 11 — Session summary shows PlayerBusts and DealerBusts counts
- [ ] Fix 12 — CalculateBustChance parallel arrays replaced with Dictionary
- [ ] Fix 13 — Username validation rejects spaces
- [ ] Fix 14 — Soft Ace display shows when Ace is counting as 1
- [ ] RegisterOrLoginPlayer() — replaces LoadPlayerBalance() and username entry flow
- [ ] InsertGameRecord() — replaces WriteRecordToCSV()
- [ ] CheckDailyBonus() migrated to read from Players.LastSeen
- [ ] PrintQuerySummary() — live SQL analytics at end of session
- [ ] Delete CSV dependency entirely
- [ ] Test: play 10 hands, verify rows appear correctly in both tables
- [ ] Commit: Phase 3 complete

### Planned SQL schema

TABLE: Players
- PlayerID      INTEGER  PRIMARY KEY AUTOINCREMENT
- Username      TEXT     UNIQUE NOT NULL
- PlayerAge     INTEGER
- FirstSeen     TEXT
- LastSeen      TEXT
- TokenBalance  INTEGER

TABLE: GameSessions
- RecordID           INTEGER  PRIMARY KEY AUTOINCREMENT
- SessionID          INTEGER
- Username           TEXT     REFERENCES Players(Username)
- LoginTime          TEXT
- GameNumber         INTEGER
- PlayerTotal        INTEGER
- DealerTotal        INTEGER
- Result             TEXT
- PlayerBusted       INTEGER  (0 or 1)
- DealerBusted       INTEGER  (0 or 1)
- NumberOfDraws      INTEGER
- BetAmount          INTEGER
- TokensBefore       INTEGER
- TokensAfter        INTEGER
- StrategyMode       TEXT
- OverrodeSuggestion INTEGER  (0 or 1)
- DoubledDown        INTEGER  (0 or 1)

### Planned SQL queries for PrintQuerySummary()

Win/loss/tie breakdown for this player:
SELECT Result, COUNT(*) as Total
FROM GameSessions WHERE Username = ?
GROUP BY Result

Average hand total by outcome:
SELECT Result, ROUND(AVG(PlayerTotal), 1) as AvgTotal
FROM GameSessions WHERE Username = ?
GROUP BY Result

Player bust rate:
SELECT ROUND(AVG(CAST(PlayerBusted AS FLOAT)) * 100, 1) as BustRate
FROM GameSessions WHERE Username = ?

Strategy impact on win rate:
SELECT StrategyMode,
       ROUND(AVG(CASE WHEN Result='Win' THEN 1.0 ELSE 0.0 END) * 100, 1) as WinRate
FROM GameSessions WHERE Username = ?
GROUP BY StrategyMode

Double down win rate:
SELECT ROUND(AVG(CASE WHEN DoubledDown=1 AND Result='Win' THEN 1.0 ELSE 0.0 END) * 100, 1)
FROM GameSessions WHERE Username = ?

---

## ⬜ PHASE 4 — Python Analysis Scripts
**Status:** Planned  
**Dependency:** Phase 3 complete

### Goal
Write Python scripts that connect to the SQLite database, perform statistical analysis, and produce matplotlib charts committed to the repository.

### Milestones
- [ ] Write generate_synthetic_data.py — populates database with 300-500 realistic fake sessions
- [ ] Write bust_analysis.py — histogram of player hand totals at time of bust
- [ ] Write win_rate_trends.py — line chart of win rate over time
- [ ] Write strategy_impact.py — bar chart comparing win rates strategy ON vs OFF
- [ ] Write token_flow.py — token balance over time per player
- [ ] Write draws_vs_outcome.py — average draws per result category
- [ ] Export all charts as .png files into /analysis/charts/
- [ ] Commit: Phase 4 complete - Python analysis scripts and charts

### Why synthetic data
The database needs 300-500 rows to make dashboard visuals meaningful. Real play sessions take time to accumulate. Synthetic data is standard practice for demos and portfolio projects — the README notes this clearly. The Python generator also demonstrates pandas and SQLite integration as a standalone skill.

---

## ⬜ PHASE 5 — Power BI Dashboard
**Status:** Planned  
**Dependency:** Phase 4 complete (synthetic data needed for meaningful visuals)

### Goal
Build a four-page interactive Power BI dashboard that connects directly to the SQLite database and visualizes the full dataset.

### Milestones
- [ ] Connect Power BI to blackjack.db
- [ ] Build Page 1 — Session Overview
- [ ] Build Page 2 — Hand Analysis
- [ ] Build Page 3 — Strategy Analysis
- [ ] Build Page 4 — Token Economy
- [ ] Export dashboard screenshots as .png files
- [ ] Add screenshots to /powerbi/screenshots/
- [ ] Add screenshots inline to README.md
- [ ] Commit .pbix file and screenshots
- [ ] Commit: Phase 5 complete - Power BI dashboard

### Planned dashboard pages

Page 1 — Session Overview
- KPI cards: total games, overall win rate, bust rate, average bet
- Win/Loss/Tie donut chart
- Sessions over time line chart

Page 2 — Hand Analysis
- Histogram: distribution of player final totals
- Bar chart: average player vs dealer total by result
- Bust rate trend over time

Page 3 — Strategy Analysis
- Side by side: win rate with strategy ON vs OFF
- Bar chart: suggestion override frequency by player total
- Outcome breakdown for overridden vs followed advice

Page 4 — Token Economy
- Line chart: average token balance trend across sessions
- Distribution: bet sizing patterns
- Count: sessions that ended with player running out of tokens

---

## ⬜ GITHUB AND PORTFOLIO POLISH
**Status:** Planned  
**Dependency:** Phase 5 complete

### Goal
Make the repository presentation-ready for recruiters, hiring managers, and technical interviewers.

### Milestones
- [ ] Final README pass — add dashboard screenshots inline
- [ ] Update all phase checkboxes in README to reflect completion
- [ ] Write docs/ARCHITECTURE.md — system design, data flow, class relationships
- [ ] Final CHANGELOG update — all phases documented
- [ ] Add repo topics on GitHub: csharp, sqlite, python, powerbi, data-analytics, blackjack
- [ ] Pin repository to GitHub profile
- [ ] Add project link to LinkedIn profile
- [ ] Write LinkedIn post about the project
- [ ] Prepare 2-minute verbal explanation of the project for interviews
- [ ] Prepare answers to: walk me through your data pipeline, what was the hardest technical problem, why Blackjack

---

## 💡 FUTURE IDEAS (not yet scoped)

- Blazor WebAssembly front end — game playable in browser, increases recruiter accessibility significantly
- Split hands mechanic — adds another betting behavior data point, makes game more realistic
- Card counting detection — track running count, analyze whether count correlates with player decisions
- Multi-game casino platform — shared core library, add Poker, Craps, Roulette as separate games
- Leaderboard — SELECT + ORDER BY on TokensAfter, simple SQL feature, good dashboard visual
- Achievement system — query-based, no schema change needed
- Context-aware strategy suggestions — factor in dealer visible card, closer to real basic strategy tables
- Soft 17 dealer rule option — some casinos hit on soft 17, configurable option, interesting analytics variable
- Azure or cloud deployment — host the database in the cloud, real multi-user capability

---

## MILESTONE SUMMARY

- Phase 1 complete — DONE — Stable game engine, CSV persistence
- Phase 2 complete — DONE — Username system, deal order, double down
- Phase 3 complete — IN PROGRESS — 52-card deck, game polish, SQLite integration
- Phase 4 complete — PLANNED — Python analysis scripts
- Phase 5 complete — PLANNED — Power BI dashboard

---

This document is updated at the end of each development phase.
Next update: Phase 3 Complete
