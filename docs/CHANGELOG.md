# CHANGELOG
## BlackjackAnalytics — C# Blackjack Analytics Pipeline
**Author:** Alex Thomas  
**Repository:** https://github.com/AlexChaseThomas/BlackjackAnalytics  
**Document version:** 5.0 (Project Complete — All Phases)

---

## HOW TO USE THIS DOCUMENT

This changelog is a living engineering record of the BlackjackAnalytics project.
It documents every significant decision, bug fix, architectural change, and feature addition
made during development; including the reasoning behind each decision and future considerations.

Each entry follows this format:

```
[CATEGORY] Short title
- Date: When this was addressed
- Phase: Which phase this belongs to
- Problem: What issue or gap existed
- Why it mattered: Impact on gameplay, data quality, or architecture
- Solution: What was implemented
- Future considerations: What this decision affects going forward
```

Categories:
- **[ARCHITECTURE]** — structural or design decisions
- **[BUG FIX]** — something was broken and was corrected
- **[FEATURE]** — new capability added
- **[SECURITY]** — privacy or security concern addressed
- **[UI/UX]** — player-facing experience change
- **[DATA]** — analytics, schema, or data pipeline change
- **[REFACTOR]** — code cleaned up without changing behavior
- **[DECISION]** — a deliberate choice between alternatives

---

## VERSION HISTORY SUMMARY

| Version | Phase | Description | Date |
|---------|-------|-------------|------|
| 0.1 | Phase 1 | Initial game engine — college project base | May 7, 2026 |
| 0.2 | Phase 1 | Betting system, token economy, CSV persistence | May 7, 2026 |
| 0.3 | Phase 1 | Strategy mode, bust percentage warnings | May 7, 2026 |
| 0.4 | Phase 1 | Dealer AI fixes, hard 17 rule | May 7, 2026 |
| 0.5 | Phase 1 | Soft Ace handling for player and dealer | May 7, 2026 |
| 0.6 | Phase 1 | Stand-on-zero guard clause | May 7, 2026 |
| **1.0** | **Phase 1 ✅** | **Stable game engine baseline committed to GitHub** | **May 7, 2026** |
| 1.1 | Phase 2 | Single shared Random instance | May 8, 2026 |
| 1.2 | Phase 2 | Dictionary replaces if/else card value chains | May 8, 2026 |
| 1.3 | Phase 2 | DetermineWinner() method extracted | May 8, 2026 |
| 1.4 | Phase 2 | Username system, PII removal, password gate removed | May 11, 2026 |
| 1.5 | Phase 2 | Proper blackjack deal order, hole card, double down | May 11, 2026 |
| 1.6 | Phase 2 | UI fixes — strategy warnings, play again flow | May 11, 2026 |
| **2.0** | **Phase 2 ✅** | **Full Phase 2 committed to GitHub** | **May 11, 2026** |
| 2.1 | Phase 3 | 52-card deck, Fisher-Yates shuffle, Card class | May 13, 2026 |
| 2.2 | Phase 3 | Game engine bug fixes — dealer, bust rules, overrides | May 15, 2026 |
| 2.3 | Phase 3 | UI polish — dealer reveal pause, animations, prompts | May 16, 2026 |
| 2.4 | Phase 3 | SQLite schema design, InitializeDatabase() | May 13, 2026 |
| 2.5 | Phase 3 | RegisterOrLoginPlayer(), InsertGameRecord(), CheckDailyBonusDB() | May 18, 2026 |
| 2.6 | Phase 3 | 25-column schema — new fields wired in and populating | May 17, 2026 |
| 2.7 | Phase 3 | Win streak tracking, session summary updated | May 18, 2026 |
| 2.8 | Phase 3 | Strategy recommendation engine, real-time probability model | May 21, 2026 |
| 2.9 | Phase 3 | 29-column schema, Sessions table tracking, Players lifetime stats | May 22, 2026 |
| 2.91 | Phase 3 | Daily bonus bug fix, dealer reveal redesign, tip framing polish | May 22, 2026 |
| **3.0** | **Phase 3 ✅** | **PrintQuerySummary(), CSV removed, comment pass, final Phase 3 commit** | **May 22, 2026** |
| 3.1 | Phase 4 | generate_synthetic_data.py — behavioral archetypes, probability lookup table | May 23, 2026 |
| 3.2 | Phase 4 | Exit prompt cleanup, token guard clause fix | May 23, 2026 |
| **4.0** | **Phase 4 ✅** | **250 players, 9,511 hands, demo video — Phase 4 complete** | **May 23, 2026** |
| 4.1 | Phase 5 | SQLite ODBC connection, DateTime fix in Power Query | May 26, 2026 |
| 4.2 | Phase 5 | DAX measures, calculated columns, Page 1 Overview | May 26, 2026 |
| 4.3 | Phase 5 | Pages 2-3 — Token Economy, Behavioral Analytics | May 27-28, 2026 |
| 4.4 | Phase 5 | Page 4 Player Intelligence, axis disclosures, annotations | May 29, 2026 |
| **5.0** | **Phase 5 ✅** | **Power BI dashboard complete — project complete** | **May 29, 2026** |


---

*[Phases 1-3 entries preserved — see full changelog for complete engineering decision log]*

---


---

---

# PHASE 4 — PYTHON SYNTHETIC DATA GENERATION

---

**[FEATURE] generate_synthetic_data.py — synthetic data generation pipeline**
- Date: May 23, 2026
- Phase: Phase 4
- Problem: The database contained only real player sessions — too few records for statistically meaningful analytics queries or visually compelling Power BI dashboards. The platform is designed as an arcade machine intended to collect data from many players over time, but without web deployment, real sessions accumulate too slowly.
- Why it mattered: Every analytical finding becomes more credible and every dashboard visual becomes more meaningful at scale. With only a handful of real sessions, win rate comparisons between compliance groups were not statistically reliable and the Power BI dashboard had nothing interesting to show.
- Solution: Built generate_synthetic_data.py in the /analysis folder. The generator connects to blackjack.db via sqlite3, simulates 250 players across six behavioral archetypes, and writes complete records to all three tables — Players, Sessions, and GameSessions. All outcomes are derived from real game logic: cards are dealt from a 52-card deck, hands play out, and results are determined by actual blackjack probability. The generator does not assign results randomly — it simulates them.
- Future considerations: The script is documented openly in the repository with a clear explanation of why synthetic data is used. It demonstrates Python, sqlite3, and functional programming skills as a standalone portfolio item within the pipeline.

---

**[ARCHITECTURE] Six behavioral archetypes with individual variation**
- Date: May 23, 2026
- Phase: Phase 4
- Problem: Generating 250 players with hand-coded behavioral profiles would have been impractical. A single archetype for all players would produce unrealistically uniform data with no behavioral signal.
- Why it mattered: The analytical value of the dataset depends on real behavioral variation across players. Without different compliance rates, decision speeds, and betting patterns, the compliance analysis and decision latency analysis produce flat, uninformative results.
- Solution: Defined six archetypes — disciplined (high compliance, moderate speed, conservative bets), impulsive (low compliance, fast decisions, aggressive bets), deliberate (high compliance, slow decisions, conservative bets), casual (mixed compliance, moderate speed), risk-taker (low compliance, aggressive bets), and novice (inconsistent compliance, slow decisions, erratic bets). Each player is assigned to an archetype with individual variation added on top using random sampling within archetype ranges. Ages are distributed across a 21-68 range weighted by archetype to enable age-behavior correlation analysis.
- Future considerations: The archetype field was intentionally not written to the database. Real players do not have archetypes, and adding the field would create null values for all real sessions — a data governance decision documented in the project.

---

**[ARCHITECTURE] Weighted probability lookup table with lru_cache memoization**
- Date: May 23, 2026
- Phase: Phase 4
- Problem: Simulating real blackjack probability for 9,500+ hands required calculating dealer win probability for every hand. The recursive probability tree traversal used in the C# game engine would be prohibitively slow in Python if called fresh on every hand without optimization.
- Why it mattered: Performance and accuracy were both at risk. A naive implementation would either be too slow for practical use or would approximate probability rather than calculating it correctly.
- Solution: Built build_win_prob_table() which pre-computes dealer win probability for all relevant (playerTotal, dealerVisibleValue) combinations using a recursive helper function decorated with @lru_cache. The lru_cache transforms the exponential recursion tree into a linear memoized lookup by caching each unique (player_total, dealer_total, dealer_aces) game state. The table is computed once at startup; all in-simulation lookups are O(1). This mirrors the architectural distinction in the C# engine between static lookup tables and runtime probability calculation — the Python generator uses the same weighted tree traversal model but optimizes it for batch simulation performance.
- Future considerations: The lru_cache approach and its architectural reasoning are documented in method comments for portfolio reference.

---

**[DATA] 250 synthetic players — final database scale**
- Date: May 23, 2026
- Phase: Phase 4
- Problem: Needed a dataset large enough to produce statistically credible findings across multiple analytical dimensions simultaneously — compliance groups, decision speed buckets, age brackets, and individual player leaderboards.
- Why it mattered: At 50 players the compliance groups would have too few hands for reliable comparison. At 250 players with 4-7 sessions each and 8-16 hands per session the dataset produces stable results across all dimensions.
- Solution: Generated 250 synthetic players producing 1,375 sessions and approximately 9,495 hands. Combined with real player sessions, the final database contained 262 total players, 1,377 sessions, and 9,511 hands. The compliance analysis (followed 40.1% vs ignored 29.6%) runs across 5,888 strategy-on hands — well above the minimum threshold for credible directional analysis. All 29 GameSessions columns populated correctly and verified in DB Browser before the final commit.
- Future considerations: The synthetic data is clearly documented in the README and CHANGELOG. The generator script is committed to the repository with full explanatory comments. This is standard practice for analytics portfolio projects where real multi-user deployment is not feasible.

---

**[BUG FIX] Redundant exit prompt removed**
- Date: May 23, 2026
- Phase: Phase 4
- Problem: After selecting ESC at the session end menu, the program displayed "Thanks for playing. Press any key to exit." and required an additional keypress before closing.
- Why it mattered: The player had already made the explicit choice to exit by pressing ESC. A second prompt after that decision adds friction without purpose and makes the session exit feel inconsistent with the decisive exit design elsewhere in the application.
- Solution: Removed the Console.WriteLine("Thanks for playing. Press any key to exit.") and Console.ReadKey() calls from the exit path. The program now closes cleanly when the player selects ESC. The same redundant ReadKey() was removed from the token guard clause (zero balance at login) path to maintain consistency.

---

**[BUG FIX] Token guard clause — analytics path on zero balance mid-session**
- Date: May 23, 2026
- Phase: Phase 4
- Problem: When a player ran out of tokens mid-session, the session ended abruptly without running PrintQuerySummary(), UpdateSessionRecord(), or UpdatePlayerLifetimeStats(). The session record was left incomplete.
- Why it mattered: A session that ends due to token exhaustion is still a valid data point. The analytics queries should fire and the session should be closed correctly regardless of why the session ended.
- Solution: Confirmed that the existing sessionActive = false path correctly falls through to the UpdateSessionRecord() and PrintQuerySummary() calls at the bottom of the session loop. The zero-balance-at-login guard clause was verified separately — this path correctly exits before InsertSessionRecord() is called, so there is no session to summarize, and no queries fire. Both paths now behave correctly.

---

---

# PHASE 5 — POWER BI DASHBOARD

---

**[ARCHITECTURE] Power BI connected to live SQLite database via ODBC**
- Date: May 26, 2026
- Phase: Phase 5
- Problem: Power BI does not have a native SQLite connector. Connecting the dashboard to the live database required a driver layer between Power BI's ODBC interface and the SQLite file format.
- Why it mattered: The dashboard needed to connect to the actual blackjack.db file rather than importing static data. A live connection means the dashboard updates automatically when new game sessions are played.
- Solution: Installed Christian Werner's SQLite ODBC driver (sqliteodbc.exe and sqliteodbc_w64.exe). Connected Power BI Desktop via Get Data → ODBC → SQLite3 Datasource with connection string Database=[path to blackjack.db]. All three tables loaded successfully — GameSessions, Players, Sessions. Table relationships auto-detected correctly: Players → Sessions (1:many), Players → GameSessions (1:many), Sessions → GameSessions (1:many).

---

**[DATA] LoginTime converted from TEXT to DateTime in Power Query**
- Date: May 26, 2026
- Phase: Phase 5
- Problem: SQLite stores datetime values as TEXT strings. Power BI imported LoginTime, StartTime, and EndTime as text columns with no date hierarchy, making time-based analysis impossible and causing every attempt to use these fields on a chart axis to fail silently.
- Why it mattered: The Token Economy page required monthly aggregation of LoginTime for the house edge and platform volume charts. Text columns have no date hierarchy in Power BI — drill up/down buttons are grayed out and the axis renders as individual timestamps rather than months.
- Solution: Opened Transform Data (Power Query Editor). Changed the data type of LoginTime in GameSessions, and StartTime and EndTime in Sessions from Text to Date/Time using Transform → Change Type → Date/Time. Clicked Close & Apply. Power BI now recognizes these as datetime values and creates an automatic date hierarchy (Year → Quarter → Month → Day) enabling drill navigation and proper monthly aggregation.
- Future considerations: This is a common issue when connecting Power BI to SQLite. The fix is documented here as a reference for future connections to the same database.

---

**[FEATURE] DAX measures — full list**
- Date: May 26-29, 2026
- Phase: Phase 5
- Problem: Power BI's default aggregations (Sum, Count, Average) are not sufficient to answer the analytical questions the dashboard is built around. Custom measures were required for win rates by compliance group, average bet by strategy mode, and player-level deviation from mean token balance.
- Solution: Created the following measures in GameSessions and Players tables:
  - Win Rate — DIVIDE(COUNTROWS(FILTER wins), COUNTROWS all)
  - Win Rate Followed — win rate where RecommendationFollowed=1 and StrategyMode="On"
  - Win Rate Ignored — win rate where RecommendationFollowed=0 and StrategyMode="On" and RecommendedAction<>"NONE"
  - Player Net PnL — SUMX evaluating BetAmount as positive on wins, negative on losses
  - Total Tokens Wagered — SUM(BetAmount) for platform volume analysis
  - Net Token Flow — TokensAfter minus TokensBefore across all hands
  - Avg Bet Strategy On — CALCULATE(AVERAGE(BetAmount), StrategyMode="On")
  - Avg Bet Strategy Off — CALCULATE(AVERAGE(BetAmount), StrategyMode="Off")
  - Avg Decision Time — AVERAGE(HandDurationSeconds)
  - Compliance Rate — DIVIDE followed strategy-on hands by total strategy-on hands with recommendations
  - Player Win Rate — DIVIDE wins by total for player-level leaderboard context
  - Hands Won Following — COUNTROWS wins where RecommendationFollowed=1 and StrategyMode="On"
  - Hands Lost Ignoring — COUNTROWS losses where RecommendationFollowed=0 and StrategyMode="On" and RecommendedAction<>"NONE"
  - Token Deviation — MAX(TokenBalance) minus CALCULATE(AVERAGE(TokenBalance), ALL(Players))
- Future considerations: All measures use parameterized filter conditions that could be adapted for supply chain KPIs — fill rate, compliance rate, on-time delivery rate — by substituting table and column names.

---

**[FEATURE] Calculated columns — SpeedBucket, AgeBracket, sort columns**
- Date: May 27-28, 2026
- Phase: Phase 5
- Problem: HandDurationSeconds is a continuous numeric field. AgeBracket requires grouping PlayerAge into four ranges. Both needed to be converted to labeled categories for axis display. Text categories in Power BI sort alphabetically by default — 0-3s, 11-15s, 16-25s, 26s+, 4-6s, 7-10s — which destroys the analytical meaning of the decision speed chart.
- Solution: Created SpeedBucket as a calculated column using nested IF statements: 0-3s, 4-6s, 7-10s, 11-15s, 16-25s, 26s+. Created SpeedBucketSort as a numeric companion column (1-6) and set SpeedBucket to Sort by Column SpeedBucketSort. Created AgeBracket (21-29, 30-39, 40-49, 50+) and AgeBracketSort (1-4) with the same sort pattern. Both sort columns are invisible to the report consumer but ensure correct chronological and demographic ordering on all visuals.
- Future considerations: The sort-by-column pattern is the standard Power BI solution for any text category that needs non-alphabetical ordering. This applies to month names, age brackets, risk levels, and any other labeled bucket.

---

**[FEATURE] Page 1 — Overview**
- Date: May 26, 2026
- Phase: Phase 5
- Problem: Needed a page that orients any viewer in under 10 seconds and leads with the most important analytical finding in the project.
- Solution: Three KPI cards across the top (9,511 Total Hands Played, 262 Total Players, 38% Overall Win Rate) provide dataset context. Centerpiece visual in black: clustered column chart titled "How does suggestion adherence affect win rate?" comparing 40.1% (Followed) versus 29.6% (Ignored) with Y axis minimum set to 25% to emphasize the gap. Supporting visuals: Win/Loss/Tie donut chart and Hands by Outcome horizontal bar chart. Insight annotation below centerpiece: "Players who follow strategy recommendations win 35% more often than those who ignore them, suggesting the recommendation engine provides measurable decision quality improvement." Axis disclosure footnote: "Y axis range 25%-50%. Absolute spread between followed and ignored is ~10.5 percentage points."

---

**[FEATURE] Page 2 — Token Economy**
- Date: May 27, 2026
- Phase: Phase 5
- Problem: Needed a page that shows the financial story of the platform — how token volume grew over time, whether the house edge was consistent, and whether having strategy mode active changed betting behavior.
- Solution: Centerpiece in black: Monthly Token Volume (Platform Engagement) column chart showing tokens wagered by month from November 2025 to May 2026 — a clear growth pattern peaking in April. Supporting visual left: House Edge Over Time line chart (Player Net PnL by month) showing the house consistently extracting tokens with a constant line at 0 for reference. Supporting visual right: Two gauge charts side by side — Average Bet Strategy Off (40.11) and Average Bet Strategy On (25.04) — with a text annotation: "Players with strategy mode active bet 37% less on average, suggesting a correlation between strategy engagement and conservative risk behavior." The 37% betting difference is the most behaviorally interesting finding on this page and the most operationally transferable to other domains.

---

**[FEATURE] Page 3 — Behavioral Analytics**
- Date: May 28, 2026
- Phase: Phase 5
- Problem: Needed a page that answers the most original analytical question in the project: does decision speed correlate with outcomes, and is the relationship linear or threshold-based?
- Solution: Centerpiece in black: Decision Speed vs. Win Rate — Is there an optimal window? Line chart with six speed buckets showing win rates: 0-3s (41.4%), 4-6s (40.0%), 7-10s (42.2%), 11-15s (39.7%), 16-25s (37.7%), 26s+ (35.7%). The peak at 7-10 seconds reveals a non-linear threshold effect — neither the fastest nor the slowest players win most, but players in the 7-10 second optimal window perform best. Y axis disclosure footnote: "Note: Y axis range 34%-43%. Absolute spread across all speed buckets is ~6 percentage points." Supporting visual left: Compliance Rate by Age Group bar chart showing a clean staircase from 38.2% (21-29) to 55.8% (50+). Supporting visual right: Average Decision Time by Age Group bar chart showing 14s (21-29) through 31s (50+). Together the three visuals tell a coherent story: older players decide more slowly and follow recommendations more consistently, yet fall into the slower speed buckets where win rates are lowest — indicating hesitation correlates with harder hands, not poor strategy.

---

**[FEATURE] Page 4 — Player Intelligence**
- Date: May 29, 2026
- Phase: Phase 5
- Problem: Needed a page that surfaces individual player profiles and tests whether the aggregate compliance finding holds at the individual level.
- Solution: Centerpiece: Player Leaderboard — Top 15 by Token Balance table showing Username, Token Deviation from mean, TokenBalance, Win Rate, Compliance Rate, Won Following Advice, and Lost Ignoring Advice. Top player (austinthomas) holds 1,081 tokens — 1,032 above the mean — with 57.8% win rate. Blank cells in compliance columns correctly indicate players who played with strategy off. Footnote: "Blank cells related to compliance metrics represent players who did not use strategy mode." Supporting visual left: Compliance Rate vs Win Rate by Player scatter chart — 262 dots, one per player. The scatter shows no strong individual-level correlation between compliance rate and win rate, confirming that the aggregate finding (40.1% vs 29.6%) holds at the population level but individual outcomes show high variance. Annotation: "No strong individual-level correlation between compliance rate and win rate — aggregate findings hold at population level but individual outcomes show high variance." Supporting visual right: Sessions by Strategy Mode donut — 70.72% strategy on, 29.28% off. Annotation: "70.7% of hands played with strategy mode active — compliance analysis is representative of the majority of gameplay."

---

**[DECISION] Color scheme — black centerpiece, red primary, gray secondary**
- Date: May 26, 2026
- Phase: Phase 5
- Problem: Needed a visual identity for the dashboard that was both professional and thematically appropriate to the project domain.
- Solution: Playing card aesthetic — black background on each page's centerpiece visual, red as the primary data color, dark gray as the secondary color. This creates a clear visual hierarchy: the viewer's eye goes to the black centerpiece first, then reads the supporting visuals in red and gray. The color scheme was consistent across all four pages and all visual types including cards, bar charts, line charts, gauges, and table headers. The choice was intentional and documented rather than left as a default Power BI theme.

---

**[DATA] Axis disclosure footnotes — compressed Y axes**
- Date: May 29, 2026
- Phase: Phase 5
- Problem: Several visuals use compressed Y axis ranges that amplify visual differences beyond their actual magnitude. A 6 percentage point spread across decision speed buckets looks dramatic when the axis runs 34-43% but looks trivial at 0-100%. Both representations are technically accurate; neither is complete without context.
- Why it mattered: Compressed axes are one of the most common ways dashboards mislead unintentionally — and one of the most common things a technically literate reviewer will notice. Disclosing the axis range proactively signals data literacy and intellectual honesty. The decision to compress was justified (the relative pattern is the analytically interesting finding), but it required disclosure.
- Solution: Added text box annotations below each affected visual with the Y axis range and absolute spread. Page 1 compliance chart: "Y axis range 25%-50%. Absolute spread ~10.5 percentage points." Page 3 decision speed chart: "Y axis range 34%-43%. Absolute spread across all speed buckets is ~6 percentage points." Formatting: small font (9-10pt), gray color, positioned below the visual without competing for attention.

---

---

# KNOWN LIMITATIONS AND TECHNICAL DEBT — UPDATED

| Item | Status | Phase Introduced | Resolution |
|------|--------|-----------------|-----------|
| CSV still active in parallel | ✅ RESOLVED — May 22, 2026 | Phase 3 | Deleted |
| Sessions table not yet populated | ✅ RESOLVED — May 22, 2026 | Phase 3 | InsertSessionRecord/UpdateSessionRecord implemented |
| Players lifetime stats not yet updated | ✅ RESOLVED — May 22, 2026 | Phase 3 | UpdatePlayerLifetimeStats() implemented |
| PrintQuerySummary() not yet implemented | ✅ RESOLVED — May 22, 2026 | Phase 3 | Three live SQL queries implemented |
| Comment pass on SQLite methods pending | ✅ RESOLVED — May 22, 2026 | Phase 3 | Full comment pass completed |
| Strategy suggestions not context-aware | ✅ RESOLVED — May 21, 2026 | Phase 1 | Dealer card factored into all probability calculations |
| Database too small for meaningful analytics | ✅ RESOLVED — May 23, 2026 | Phase 4 | 250 synthetic players, 9,500+ hands |
| Power BI dashboard not yet built | ✅ RESOLVED — May 29, 2026 | Phase 5 | Four-page dashboard complete |
| No card counting / deck depletion tracking | OPEN | Phase 1 | Future feature — multi-deck shoe |
| Split hands not implemented | OPEN | Phase 2 | Future feature |
| Recursive Main() for play again | OPEN | Phase 2 | Technical debt — acceptable for current scope |
| Soft 17 dealer rule not configurable | OPEN | Phase 1 | Future feature |

---

*This document is updated at the end of each development phase. Project complete as of May 29, 2026.*
