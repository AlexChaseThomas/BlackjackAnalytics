# CHANGELOG
## BlackjackAnalytics — C# Blackjack Analytics Pipeline
**Author:** Alex Thomas  
**Repository:** https://github.com/AlexChaseThomas/BlackjackAnalytics  
**Document version:** 4.0 (Phase 3 Complete)

---

## HOW TO USE THIS DOCUMENT

This changelog is a living engineering record of the BlackjackAnalytics project.
It documents every significant decision, bug fix, architectural change, and feature addition
made during development — including the reasoning behind each decision and future considerations.

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
| 2.8 | Phase 3 | Strategy recommendation engine, real-time probability model, color-coded tips | May 21, 2026 |
| 2.9 | Phase 3 | 29-column schema, Sessions table tracking, Players lifetime stats | May 22, 2026 |
| 2.91 | Phase 3 | Daily bonus bug fix, dealer reveal redesign, tip framing polish | May 22, 2026 |
| **3.0** | **Phase 3 ✅** | **PrintQuerySummary(), CSV removed, comment pass, final Phase 3 commit** | **May 22, 2026** |

---

---

# PHASE 1 — GAME ENGINE CONSTRUCTION

---

**[ARCHITECTURE] Initial project structure and class design**
- Date: May 7, 2026
- Phase: Phase 1
- Problem: Needed to evolve a college console project into a structured analytics-ready application.
- Why it mattered: The original project was a single-file script without data classes, persistence, or analytics tracking. It could play blackjack but generated no reusable data.
- Solution: Introduced three distinct class types — PlayerInfo (identity data), SessionRecord (per-hand analytics record), and GameStats (session-level counters). Separated data classes from the logic class BlackjackGame. This mirrors real-world data modeling patterns and sets up a clean migration path to SQL tables later.
- Future considerations: PlayerInfo, SessionRecord, and GameStats map directly to future SQL tables. The field names chosen here should be preserved through the database migration to avoid breaking the analytics pipeline.

---

**[FEATURE] Token economy with CSV persistence**
- Date: May 7, 2026
- Phase: Phase 1
- Problem: No state persisted between sessions — every run started from scratch.
- Why it mattered: Persistent balances are essential for meaningful analytics. Token flow over time is one of the most analytically interesting datasets the game produces.
- Solution: Implemented LoadPlayerBalance() which reads the CSV backwards to find the player's most recent TokensAfter value. New players start with 100 tokens. WriteRecordToCSV() appends a new row after every hand.
- Future considerations: The CSV approach has known limitations — no referential integrity, no querying, vulnerable to corruption. SQLite replaces this entirely in Phase 3.

---

**[FEATURE] Daily login bonus system**
- Date: May 7, 2026
- Phase: Phase 1
- Problem: No incentive for players to return after running low on tokens.
- Why it mattered: Return frequency and bonus dependency are analytically interesting behavioral dimensions.
- Solution: CheckDailyBonus() reads the player's most recent LoginTime from the CSV, calculates elapsed time as a TimeSpan, and awards 50 tokens if 24 or more hours have passed. Uses an out parameter to return both the updated balance and hours remaining.
- Future considerations: In Phase 3 this is replaced with a single SQL query against the Players table which stores LastSeen directly.

---

**[FEATURE] Strategy suggestion mode**
- Date: May 7, 2026
- Phase: Phase 1
- Problem: Players had no guidance on decision quality and the game produced no data about whether players made statistically sound decisions.
- Why it mattered: Strategy mode is one of the most analytically valuable features. OverrodeSuggestion enables future analysis of whether warned players bust more or less and whether strategy mode improves win rates.
- Solution: Added strategyOn boolean set at session start. CalculateBustChance() calculates bust probability from current total. Warnings shown when drawing at 17+ or standing at 11 or lower.
- Future considerations: A future improvement would factor in the dealer's visible card — the core of real basic strategy. Implemented in Phase 3.

---

**[FEATURE] Betting system with forfeit confirmation**
- Date: May 7, 2026
- Phase: Phase 1
- Problem: No financial stakes made the game feel meaningless and produced no wagering data.
- Why it mattered: Bet sizing, risk behavior, and token flow are core analytics dimensions.
- Solution: Betting prompt before each hand. Minimum 5 tokens, maximum 100 or current balance. Double-ESC forfeit confirmation. BetAmount, TokensBefore, and TokensAfter written to every SessionRecord.
- Future considerations: Future analytics will correlate BetAmount with Result to identify risk behavior patterns.

---

**[BUG FIX] Dealer did not draw to 17 — drew only once per player draw**
- Date: May 7, 2026
- Phase: Phase 1
- Problem: Dealer draw logic was coupled to the player draw loop. Every time the player drew, the dealer drew exactly once.
- Why it mattered: Violated fundamental blackjack rules and made every hand result unreliable.
- Solution: Removed dealer drawing from the player draw branch. Added a dedicated dealer draw phase using while (dealerTotal < 17). Dealer now draws independently after the player's turn resolves.
- Future considerations: Hard 17 rule implemented. Soft 17 configurable option is a future analytics variable idea.

---

**[BUG FIX] Soft Ace handling missing for player and dealer**
- Date: May 7, 2026
- Phase: Phase 1
- Problem: Aces always counted as 11. Ace + Ace = 22 and immediate bust.
- Why it mattered: Incorrect bust detection, unfair losses, unreliable bust rate analytics.
- Solution: Added playerAces and dealerAces integer counters. After any card is added, a while (total > 21 and aces > 0) loop subtracts 10 (converting one Ace from 11 to 1) until the total is legal or no soft Aces remain.
- Future considerations: PlayerBusted now correctly reflects only genuine busts with no available Ace rescue.

---

**[BUG FIX] Player could stand on 0 and win**
- Date: May 7, 2026
- Phase: Phase 1
- Problem: Pressing N before drawing any card set playerTotal to 0. Dealer bust would record a Win with PlayerTotal = 0.
- Why it mattered: Corrupted analytics data — a Win with PlayerTotal = 0 is meaningless.
- Solution: Added guard clause checking numberOfDraws == 0 before allowing a stand.
- Future considerations: This guard was removed in Phase 2 when the opening deal was introduced — players now always start with two cards, making a zero-total stand impossible by design.

---

**[BUG FIX] Duplicate card and total output in draw branch**
- Date: May 7, 2026
- Phase: Phase 1
- Problem: Drawn card and total printed twice per draw.
- Solution: Consolidated all output into a single print block. ReadKey(true) removed from strategy warning blocks. Game loop's own ReadKey captures the player's actual decision.

---

**[DATA] CSV schema design — initial column structure**
- Date: May 7, 2026
- Phase: Phase 1
- Problem: Needed a persistent data format that would support future SQL queries, Python analysis, and Power BI dashboards.
- Solution: Designed 16-column CSV covering identity, timing, gameplay outcomes, wagering, and strategy. Column order matches the planned SQL table structure.
- Future considerations: DoubledDown added in Phase 2. Each new feature adds a column rather than repurposing existing ones.

---

---

# PHASE 2 — CODE CLEANUP + IDENTITY SYSTEM + BLACKJACK REALISM

---

**[REFACTOR] Single shared Random instance at class level**
- Date: May 8, 2026
- Phase: Phase 2
- Problem: Draw() and SuitAssigner() each created a new Random instance per call. Close calls received the same seed and produced identical results.
- Why it mattered: Would produce subtle statistical anomalies corrupting card frequency analysis.
- Solution: Declared static Random rand = new Random() at class level. All methods share one instance.
- Future considerations: Not thread-safe — would need replacement if multithreading is introduced in a web version.

---

**[REFACTOR] Dictionary replaces if/else card value chains**
- Date: May 8, 2026
- Phase: Phase 2
- Problem: Card value lookup was a 13-line if/else chain duplicated in three locations.
- Why it mattered: Duplicated logic is a maintenance risk. A change in one place not mirrored in others creates subtle bugs.
- Solution: Declared static Dictionary cardValues at class level. All three chains replaced with a single cardValues[cardName] lookup.
- Future considerations: Direct analog to a SQL lookup table or Python dictionary. Demonstrates key-value data structure understanding.

---

**[REFACTOR] DetermineWinner() extracted from Main()**
- Date: May 8, 2026
- Phase: Phase 2
- Problem: Win/loss/tie logic lived inline in Main() and was not reusable.
- Solution: Created static string DetermineWinner(int playerTotal, int dealerTotal) returning Win, Loss, or Tie. Conditions ordered most-specific to most-general.
- Future considerations: Candidate for a shared Core/ library in a future multi-game casino platform.

---

**[SECURITY] Password gate removed**
- Date: May 11, 2026
- Phase: Phase 2
- Problem: Hardcoded password visible in plain text in the source code on a public GitHub repository.
- Why it mattered: A visible hardcoded password signals poor security awareness to any code reviewer.
- Solution: Removed PasswordChecker() entirely. Program opens directly to welcome screen. Username system provides identity tracking.
- Future considerations: A future web version would use properly hashed passwords stored in the database.

---

**[SECURITY] PII removal — real names and full DOB replaced**
- Date: May 11, 2026
- Phase: Phase 2
- Problem: System collected first name, last name, and full DOB and stored all of it in the CSV.
- Why it mattered: Storing real names and birthdates in a public repository is bad data governance and creates negative recruiter optics.
- Solution: Replaced names with a username system. DOB is collected for verification only, calculated into an integer age, and immediately discarded. The dob variable never touches PlayerInfo or any stored record.
- Future considerations: Age as an integer is still meaningful for analytics while being non-identifying. A production system would require email verification.

---

**[FEATURE] Username system with auto-registration**
- Date: May 11, 2026
- Phase: Phase 2
- Problem: Real-name system created PII risks and duplicate identity problems.
- Solution: Players choose a username (3–20 characters, stored lowercase). Existing usernames load the previous balance. New usernames start with 100 tokens automatically.
- Future considerations: Username uniqueness not enforced in CSV — resolved in Phase 3 via SQL UNIQUE constraint.

---

**[FEATURE] Proper blackjack deal order**
- Date: May 11, 2026
- Phase: Phase 2
- Problem: Original deal flow was unrealistic — dealer card shown before player had any cards.
- Why it mattered: Gameplay realism directly affects data quality. Player decisions made in an unrealistic context produce unreliable behavioral analytics.
- Solution: Standard casino deal order implemented: player receives two cards face up, dealer receives one visible and one hole card, player decides, then dealer reveals and draws.
- Future considerations: numberOfDraws now counts only additional draws beyond the opening two cards.

---

**[FEATURE] Hole card reveal**
- Date: May 11, 2026
- Phase: Phase 2
- Problem: Dealer's second card was not tracked or hidden.
- Solution: dealerHoleCard stored at deal time, not displayed during player's turn. Revealed as "Dealer reveals hole card" before dealer draws. dealerAcesStart tracks whether either starting card was an Ace.
- Future considerations: Future analytics could track how often the dealer's hole card would have made the dealer stand without drawing.

---

**[FEATURE] Double down mechanic**
- Date: May 11, 2026
- Phase: Phase 2
- Problem: Double down missing from the game — an important betting behavior data point absent.
- Solution: D key handled only on opening two cards. Valid double down doubles currentBet, deals exactly one additional card, sets gameOver = true immediately. DoubledDown bool added to SessionRecord and CSV.
- Future considerations: DoubledDown enables win rate comparison between doubled and non-doubled hands.

---

**[UI/UX] Strategy warning shown for high opening hands**
- Date: May 11, 2026
- Phase: Phase 2
- Problem: Strategy warning only fired inside the draw branch — a player dealt 20 on opening received no warning.
- Solution: Strategy warning check added after opening hand display and before game loop starts.

---

**[UI/UX] Compact control header reprinted after strategy warnings**
- Date: May 11, 2026
- Phase: Phase 2
- Problem: Strategy warning consumed a keypress via ReadKey(true) — player had to press N twice to stand.
- Solution: Removed ReadKey(true) from all strategy warning blocks. Compact control reminder shown instead. Game loop's own ReadKey captures the player's decision.

---

**[UI/UX] Play again blended with betting prompt**
- Date: May 11, 2026
- Phase: Phase 2
- Problem: Two separate prompts after each hand — play again confirmation then betting prompt.
- Solution: Single line: "Type your bet to continue, or type exit to quit." Betting loop appears immediately below.

---

**[UI/UX] End of session menu added**
- Date: May 11, 2026
- Phase: Phase 2
- Problem: Session ended with static summary and no option to play again without restarting.
- Solution: Menu added with P (play again) and ESC (exit). Pressing P calls Main() recursively.
- Future considerations: Recursive Main() is technical debt. Acceptable for current scope.

---

**[DATA] DoubledDown column added to CSV schema**
- Date: May 11, 2026
- Phase: Phase 2
- Problem: Double down implemented as a mechanic but not tracked in analytics data.
- Solution: public bool DoubledDown added to SessionRecord. Column appended to CSV header and data row.

---

**[BUG FIX] Game header border misalignment**
- Date: May 8, 2026
- Phase: Phase 2
- Problem: GAME # header line did not correctly pad to box width, causing right border misalignment.
- Solution: Replaced format specifier with stats.TotalGames.ToString().PadRight(30).

---

---

# PHASE 3 — 52-CARD DECK, GAME ENGINE POLISH, SQLITE INTEGRATION

---

**[FEATURE] Card class and 52-card deck system**
- Date: May 13, 2026
- Phase: Phase 3
- Problem: The original Draw() and SuitAssigner() methods picked randomly from a 13-card array with no memory of what was already drawn. The same card could appear multiple times in one hand — impossible in a real deck.
- Why it mattered: Duplicate cards in a hand corrupt every analytics dimension — hand totals, bust rates, draw counts. The data being generated was statistically invalid.
- Solution: Introduced the Card class with Name, Suit, and ToString() override (prints "Ace of Hearts"). BuildDeck() creates a full 52-card List (13 values x 4 suits). ShuffleDeck() uses the Fisher-Yates algorithm — the standard unbiased shuffle that gives every possible ordering equal probability. DealCard() deals from index 0, removes the card, and auto-reshuffles when fewer than 10 cards remain.
- Future considerations: The 10-card reshuffle threshold is conservative. A future multi-deck shoe version would track cards across multiple decks for card counting analytics.

---

**[REFACTOR] CalculateBustChance refactored to Dictionary**
- Date: May 16, 2026
- Phase: Phase 3
- Problem: CalculateBustChance() used a separate parallel array structure to map card values to their weights — inconsistent with the Dictionary pattern established for cardValues throughout the codebase.
- Why it mattered: Inconsistent data structure patterns across methods create maintenance risk and make the codebase harder to read. A reviewer familiar with cardValues would expect the same pattern here.
- Solution: Refactored CalculateBustChance() and CalculateBustChanceDouble() to use Dictionary<int, int> valueCount matching the cardValues pattern. Same logic, consistent structure.

---

**[ARCHITECTURE] File header updated — purpose, architecture, and key decisions documented**
- Date: May 13, 2026 (initial); May 22, 2026 (Phase 3 complete update)
- Phase: Phase 3
- Problem: The file opened with a large multi-paragraph OOP explanation block and included using System.Runtime.InteropServices which was never needed.
- Why it mattered: Stale educational scaffolding in a professional portfolio project signals a learning-phase codebase rather than a polished one.
- Solution: Replaced with a clean header showing author, GitHub link, version, and class inventory. At Phase 3 close, updated to add PURPOSE, ARCHITECTURE, DATABASE TABLES, and KEY DESIGN DECISIONS sections.

---

**[BUG FIX] Opening Blackjack dealer draw missing**
- Date: May 15, 2026
- Phase: Phase 3
- Problem: When the player hit 21 on the opening deal, the game skipped the dealer draw phase entirely. The dealer did not draw to 17 and the result was determined against the dealer's opening two-card total only.
- Why it mattered: The house rule requires the dealer to draw to 17 regardless of player outcome. Skipping this meant some wins were incorrectly awarded when the dealer would have also reached 21 (a tie) or beyond.
- Solution: Added a dedicated dealer draw loop inside the opening Blackjack resolution block using while (dealerTotal < 17) with soft Ace handling. Dealer now always completes their hand before DetermineWinner() is called.

---

**[FEATURE] Dealer natural 21 — dedicated resolution path added**
- Date: May 16, 2026
- Phase: Phase 3
- Problem: When the dealer held 21 on the opening deal, the game had no dedicated handling — the player was allowed to draw before the dealer's natural was revealed, which is incorrect under casino rules.
- Why it mattered: Casino rules require checking for a dealer natural before player decisions begin. Allowing the player to draw against a dealer natural produces incorrect results and unreliable data.
- Solution: Added a dedicated natural 21 check immediately after the opening deal. If the dealer has 21 and the player does not, the hole card is revealed, "Dealer has 21! Hand over." is displayed, and the hand resolves immediately without the player drawing. The full dealer hand is displayed with the reveal.

---

**[BUG FIX] Dealer did not complete hand when player busted**
- Date: May 15, 2026
- Phase: Phase 3
- Problem: The dealer draw loop was wrapped in if (playerTotal <= 21) — meaning when the player busted, the dealer never drew. DealerTotal in the CSV recorded only the opening two-card total, not the actual final hand.
- Why it mattered: DealerTotal being wrong on bust hands corrupts every analytics query that uses that field — average dealer totals, dealer bust rates, hand comparison distributions.
- Solution: Removed the guard clause from around the dealer draw loop. Dealer now always draws to 17 regardless of player bust. DealerTotal in every row now reflects the actual completed dealer hand.

---

**[BUG FIX] Both-bust rule — player always loses when they bust**
- Date: May 15, 2026
- Phase: Phase 3
- Problem: DetermineWinner() had a special case returning Tie when both player and dealer busted. In real blackjack the player always loses when they bust — the house edge depends on this rule.
- Why it mattered: Recording Tie instead of Loss on both-bust hands and refunding the bet incorrectly represents the game's economics. Token flow analytics and win rate calculations would be wrong.
- Solution: Removed the both-bust Tie case. Added if (playerTotal > 21) return Loss as the second condition in DetermineWinner() — player bust is always a loss regardless of dealer outcome.

---

**[BUG FIX] Both-21 should be a Tie not a Win**
- Date: May 13, 2026
- Phase: Phase 3
- Problem: When both player and dealer hit exactly 21, DetermineWinner() returned Win because the player-21 check fired before the dealer-21 check.
- Why it mattered: A push (tie) when both reach 21 is the correct casino rule. Recording it as a Win overstates win rates and awards tokens incorrectly.
- Solution: Added if (playerTotal == 21 && dealerTotal == 21) return Tie as the first condition in DetermineWinner(), before all other checks.

---

**[BUG FIX] overrodeSuggestion logic was inverted**
- Date: May 15, 2026
- Phase: Phase 3
- Problem: overrodeSuggestion was set to true whenever a strategy warning was shown, even if the player then stood correctly. It should only be true if the player was warned AND chose to draw anyway.
- Why it mattered: The SuggestionsOverridden count in the session summary was inflated. Every warning shown counted as an override even when the player followed the advice. This made the strategy analytics unreliable.
- Solution: Introduced warningActive bool declared at the top of each hand's variables. Strategy warnings set warningActive = true. At the top of the draw branch, if warningActive is true the draw is flagged as an override. Standing after a warning no longer counts as an override.

---

**[BUG FIX] Exit at betting prompt dealt a phantom hand**
- Date: May 13, 2026
- Phase: Phase 3
- Problem: Typing exit at the betting prompt set sessionActive = false and broke out of the betting loop, but the session loop continued into the hand setup, dealing cards and starting the game loop with a zero bet.
- Solution: Added if (!sessionActive) break immediately after the betting loop closes.

---

**[BUG FIX] Hands played count off by one when exiting**
- Date: May 13, 2026
- Phase: Phase 3
- Problem: stats.TotalGames++ incremented at the top of the session loop before the bet was placed. When the player typed exit, the counter had already incremented for a hand that was never played.
- Why it mattered: The session summary showed one more hand than was actually played, and the last SessionRecord written had an incorrect GameNumber.
- Solution: Moved stats.TotalGames++ to after the if (!sessionActive) break guard clause.

---

**[BUG FIX] Daily bonus LastSeen race condition**
- Date: May 22, 2026
- Phase: Phase 3
- Problem: RegisterOrLoginPlayer() updated Players.LastSeen to the current login time before CheckDailyBonusDB() read it. By the time the bonus check ran, LastSeen was already set to now, making the elapsed time always ~0 hours and the bonus effectively impossible to trigger.
- Why it mattered: Players would never receive their daily bonus regardless of how much time had passed since their last login.
- Solution: Removed the LastSeen update from RegisterOrLoginPlayer() entirely. CheckDailyBonusDB() now owns all LastSeen updates — updating it in both the bonus-awarded and bonus-not-yet-due paths.

---

**[UI/UX] Dealer hole card reveal pause animation**
- Date: May 22, 2026
- Phase: Phase 3
- Problem: The dealer's hole card and all subsequent dealer draws appeared instantly with no pacing.
- Solution: Added Thread.Sleep(1500) before the hole card reveal with a "Dealer revealing..." prompt. Uses Console.SetCursorPosition to overwrite the suspense line so it disappears and the full dealer hand appears in its place — the hole card is implied by its position in the hand rather than announced separately. Thread.Sleep(1000) added between each dealer draw. Thread.Sleep(800) after the initial reveal before drawing begins.

---

**[UI/UX] Dealer hole card always revealed even on player bust**
- Date: May 15, 2026
- Phase: Phase 3
- Problem: When the player busted, the dealer's hole card was never revealed.
- Why it mattered: Transparency in game outcome is important for player trust. A player should always see what the dealer was holding.
- Solution: Moved the hole card reveal block outside the if (playerTotal <= 21) guard. Hole card always revealed regardless of player bust.

---

**[UI/UX] Soft Ace display**
- Date: May 16, 2026
- Phase: Phase 3
- Problem: Players could not see when an Ace had silently dropped from 11 to 1, making the hand total confusing.
- Why it mattered: A player seeing a hand total that does not add up to what they expect loses trust in the game.
- Solution: PrintPlayerHand() updated to accept aceCountingAsOne bool parameter. When true, appends "(Ace counting as 1)" after the hand display in DarkYellow. aceDropped flag set to true whenever a soft Ace adjustment occurs in any path.

---

**[UI/UX] Strategy mode selection redesigned with numbered options and color coding**
- Date: May 16, 2026
- Phase: Phase 3
- Problem: The strategy mode selection prompt was a plain text question with no visual hierarchy or clear distinction between options.
- Why it mattered: The strategy mode choice is one of the most important decisions the player makes — it controls whether the recommendation engine activates and affects what data gets written to the database. The prompt should reflect that importance.
- Solution: Full box display built with box-drawing characters. [1] ON displayed in green, [2] OFF displayed in yellow. Description text explains what strategy mode does. Consistent with the GAME # box display style throughout the application.

---

**[UI/UX] Bust message, visual hand separator, session summary bust counts, input buffer flush fix**
- Date: May 16, 2026
- Phase: Phase 3
- Problem: Multiple small display issues — no explicit bust message when player went over 21, hand cards displayed without visual separation, session summary did not show bust counts, and buffered keypresses from the dealer animation were being consumed at the start of the next hand.
- Why it mattered: The bust message is important feedback for the player. The input buffer issue caused the next hand to fire a hit or stand automatically from a leftover keypress, which is a serious gameplay bug.
- Solution: Added "Bust! You went over 21." message when playerTotal > 21. Added pipe separators between cards in PrintPlayerHand() and PrintDealerHand(). Added PlayerBusts and DealerBusts counters to GameStats and displayed them in the session summary. Added while (Console.KeyAvailable) Console.ReadKey(true) flush before each hand loop start.

---

**[UI/UX] Prompt text consistency**
- Date: May 15, 2026
- Phase: Phase 3
- Problem: The continue/exit prompt used different wording in different paths.
- Solution: Standardized all instances to: "Place a bet to continue, or type exit to see your session summary."

---

**[UI/UX] Session summary reveal animation**
- Date: May 22, 2026
- Phase: Phase 3
- Problem: Session summary printed all lines instantly with no visual pacing.
- Solution: Added Thread.Sleep(150) between each session summary line. Consistent with the paced reveal pattern used for dealer cards and the live analytics display.

---

**[UI/UX] Username validation rejects spaces**
- Date: May 16, 2026
- Phase: Phase 3
- Problem: Usernames containing spaces were accepted, which would cause issues with future CSV parsing and made username matching unreliable.
- Why it mattered: A space in a username breaks the CSV column structure since the file uses comma-separated values. In the SQLite version spaces create inconsistent matching behavior.
- Solution: Added username.Contains(" ") check to the validation while loop condition. Validation now requires usernames to be 3–20 characters and contain no spaces.

---

**[ARCHITECTURE] Three-table SQLite schema designed and initialized**
- Date: May 13, 2026
- Phase: Phase 3
- Problem: CSV flat file has no referential integrity, no querying capability, and no enforcement of data constraints.
- Why it mattered: The analytics pipeline requires a queryable data store. SQL queries for win rates, bust rates, and strategy impact cannot run against a CSV. The Python and Power BI phases depend on a database being in place.
- Solution: Designed a normalized three-table schema. Players stores one row per unique username with PlayerID (AUTOINCREMENT PRIMARY KEY), Username (UNIQUE NOT NULL), PlayerAge, FirstSeen, LastSeen, TokenBalance (CHECK >= 0), TotalHandsAllTime, TotalWinsAllTime, FavoriteStrategyMode, and LongestWinStreak. Sessions stores one row per session with start/end times, balance, and net profit. GameSessions stores one row per hand with 29 columns and foreign key references to both Players and Sessions. InitializeDatabase() creates all three tables using CREATE TABLE IF NOT EXISTS.
- Future considerations: All SQL queries use parameterized queries to prevent SQL injection.

---

**[FEATURE] RegisterOrLoginPlayer()**
- Date: May 18, 2026
- Phase: Phase 3
- Problem: LoadPlayerBalance() read the CSV backwards — no integrity, no querying, vulnerable to corruption.
- Why it mattered: Balance persistence is the most critical piece of state in the game.
- Solution: RegisterOrLoginPlayer() queries the Players table by Username using a parameterized SELECT. Returning players load their TokenBalance and LongestWinStreak. New players get a fresh INSERT with 100 starting tokens. Returns a (balance, playerID, longestWinStreak) tuple. LastSeen update removed from this method — owned by CheckDailyBonusDB() to prevent the race condition described above.
- Future considerations: Replaces LoadPlayerBalance() entirely.

---

**[FEATURE] InsertGameRecord()**
- Date: May 18, 2026
- Phase: Phase 3
- Problem: WriteRecordToCSV() produced a flat file with no queryability, no referential integrity, and no constraint enforcement.
- Solution: InsertGameRecord() writes one row to GameSessions using a fully parameterized INSERT covering all 29 columns, then updates Players.TokenBalance in the same connection. Called at every hand resolution point — normal resolve, opening Blackjack, dealer natural, and forfeit. Bools converted to 0/1 integers at the parameter binding layer.
- Future considerations: Replaces WriteRecordToCSV() as the primary data store.

---

**[FEATURE] InsertSessionRecord() and UpdateSessionRecord()**
- Date: May 22, 2026
- Phase: Phase 3
- Problem: Sessions table existed in the schema but was never populated. No session-level analytics were being recorded.
- Why it mattered: Session-level queries — profitability per session, session length trends, strategy mode adoption — require a Sessions row. Without it those queries require aggregating the full GameSessions table.
- Solution: InsertSessionRecord() writes a row to Sessions at session start capturing SessionID, PlayerID, Username, StartTime, and StartBalance. UpdateSessionRecord() fills in EndTime, TotalHands, EndBalance, and NetProfit at session end. sessionStartBalance snapshot taken after the daily bonus is applied so NetProfit correctly reflects in-session performance only. INSERT OR IGNORE prevents duplicate rows on recursive Main() calls.
- Future considerations: Session exists in the database from the moment it starts — not just when it completes.

---

**[FEATURE] CheckDailyBonusDB()**
- Date: May 18, 2026
- Phase: Phase 3
- Problem: CheckDailyBonus() read LoginTime from the CSV — slow on large files and dependent on a file Phase 3 is eliminating.
- Solution: CheckDailyBonusDB() reads Players.LastSeen with a single parameterized SELECT. Computes elapsed time as a TimeSpan. If 24+ hours have passed, awards 50 tokens and updates both TokenBalance and LastSeen in one UPDATE. Returns updated balance and hoursUntilBonus out parameter.
- Future considerations: Replaces CheckDailyBonus() entirely.

---

**[DATA] 25-column GameSessions schema expansion**
- Date: May 17, 2026
- Phase: Phase 3
- Problem: Original 17-column schema lacked fields needed for dealer upcard analysis, opening hand analysis, soft hand tracking, decision timing, and environmental context.
- Why it mattered: Without DealerVisibleCard there is no way to analyze win rate by dealer upcard. Without OpeningPlayerTotal there is no way to study bust rates by starting hand.
- Solution: Added DealerVisibleCard, DealerVisibleValue, OpeningPlayerTotal, OpeningDealerTotal, PlayerHandWasSoft, HandDurationSeconds, OSVersion. All new columns have DEFAULT values so existing rows remain valid.

---

**[DATA] Four strategy analytics fields added — schema expanded to 29 columns**
- Date: May 22, 2026
- Phase: Phase 3
- Problem: Strategy recommendations were being displayed to the player but not recorded in the database.
- Why it mattered: The strategy recommendation engine is the most analytically valuable feature. Without recording its outputs, compliance analysis, override heatmaps, and decision quality queries are all impossible.
- Solution: Added RecommendedAction (TEXT DEFAULT NONE), RecommendationFollowed (INTEGER DEFAULT 0), RiskLevel (TEXT DEFAULT NONE), DealerWinProbability (REAL DEFAULT 0.0). RecommendationFollowed logic: STAND is followed if numberOfDraws == 0, HIT is followed if numberOfDraws > 0, NONE if strategy mode was off.
- Future considerations: These four fields enable the compliance analysis and recommendation accuracy validation queries central to the Power BI dashboard.

---

**[FEATURE] Strategy recommendation engine with real-time probability calculation**
- Date: May 21, 2026
- Phase: Phase 3
- Problem: Phase 1 strategy warnings used only bust probability and did not account for the dealer's visible card, did not calculate win probability, and did not make a clear HIT or STAND recommendation.
- Why it mattered: A strategy system that ignores the dealer's position does not reflect real basic strategy. The dealer upcard is the most important variable in any blackjack decision.
- Solution: GetStrategyRecommendation() implements the core basic strategy ruleset using dealerVisibleValue as the primary variable — dealer 7-Ace (strong) means hit more aggressively, dealer 2-6 (weak) means stand more conservatively. CalculateDealerWinProbability() calculates probability dynamically using a weighted probability tree traversal across all possible dealer hole cards and draw sequences. This is an architectural distinction from static lookup tables — the calculation runs at runtime from current game state, not from pre-computed averages.
- Future considerations: The hand enumeration model is designed to support future context-aware extensions.

---

**[ARCHITECTURE] Weighted probability tree traversal — SimulateDealerDraw() and CalculateDealerWinProbability()**
- Date: May 21, 2026
- Phase: Phase 3
- Problem: Needed a way to calculate dealer win probability dynamically without enumerating every possible card sequence exhaustively.
- Why it mattered: The architectural choice between a static lookup table and a runtime probability model is significant. Static tables are fast but opaque. A dynamic model is transparent, context-aware, and extensible.
- Solution: SimulateDealerDraw() is a recursive method that traverses the dealer probability tree using weighted card frequencies. Each path is assigned a probability weight (e.g., a 10-value card has weight 4/13). Paths multiply weights at each draw level. Outcomes are accumulated into ref double dealerWins and ref double totalOutcomes — ref parameters allow recursive accumulation across the full tree without return value complexity.
- Future considerations: This architecture is documented in detail in the method comments as an interview reference. The distinction between this approach and a lookup table is one of the strongest technical talking points in the project.

---

**[FEATURE] CalculateDealerBustProbability() and SimulateDealerBust()**
- Date: May 21, 2026
- Phase: Phase 3
- Problem: When the dealer shows a weak card (4-6), the relevant metric for a STAND recommendation is the probability the dealer busts — not the probability the dealer wins. Showing dealer win probability for weak dealer cards produces a misleadingly low number.
- Why it mattered: A tip that says "dealer has 38% chance of winning" when dealer shows a 5 could mislead a player into hitting. The correct framing is "dealer has 41% chance of busting."
- Solution: CalculateDealerBustProbability() mirrors CalculateDealerWinProbability() but counts bust outcomes instead of win outcomes. Threshold: dealers 4-6 use bust probability framing, dealers 2-3 and 7-Ace use win probability framing.

---

**[FEATURE] PrintStrategyRecommendation() — two-tier display with color-coded controls**
- Date: May 21, 2026
- Phase: Phase 3
- Problem: Phase 1 strategy warnings were uniform regardless of the decision stakes.
- Solution: Two-tier system: totals 11 or lower receive no display (player cannot bust). Totals 12 or higher receive an inline tip with the probability estimate and color-coded controls — green for the recommended action, red for the override, cyan for quit. Tip framing is conditional: dealer 4-6 uses bust probability, dealer 2-3 and 7-Ace uses player win probability. Blank line added after controls.
- Future considerations: Color-coded controls create a clear visual signal visible during screen recordings.

---

**[FEATURE] UpdatePlayerLifetimeStats()**
- Date: May 22, 2026
- Phase: Phase 3
- Problem: TotalHandsAllTime, TotalWinsAllTime, and FavoriteStrategyMode existed in the Players table but were never updated during gameplay.
- Solution: UpdatePlayerLifetimeStats() runs at session end with a single parameterized UPDATE using SQL-side increments: TotalHandsAllTime = TotalHandsAllTime + @hands. SQL-side incrementing avoids race conditions. FavoriteStrategyMode updates to reflect the current session's strategy choice.

---

**[FEATURE] Win streak tracking**
- Date: May 18, 2026
- Phase: Phase 3
- Problem: LongestWinStreak existed in the Players table schema but was never populated during gameplay.
- Why it mattered: Win streak is one of the most behaviorally interesting player stats — it captures hot streaks, tilt behavior, and session momentum.
- Solution: currentWinStreak declared in Main() session scope. Increments on every Win result across all three resolution paths. When currentWinStreak exceeds longestWinStreak, an immediate UPDATE writes the new record to Players.LongestWinStreak. Resets to 0 on any non-Win result.

---

**[FEATURE] PrintQuerySummary() — three live SQL queries at session end**
- Date: May 22, 2026
- Phase: Phase 3
- Problem: The session summary printed C# variables. No SQL queries were visible to demonstrate that the database was actively being queried.
- Why it mattered: For portfolio purposes, showing live SQL queries running against the database and surfacing insights is the highest-impact demonstration of the analytics layer. It proves the pipeline is functional end to end within the C# layer.
- Solution: PrintQuerySummary() runs three parameterized queries against blackjack.db at session end. Query 1 (Current Session Metrics): COUNT, win rate, net profit, and recommendation adherence for this SessionID. Query 2 (Strategy Recommendation Performance): win rate grouped by RecommendationFollowed across lifetime strategy-on hands. Query 3 (Decision Latency Analysis): win rate grouped by HandDurationSeconds buckets across all lifetime hands. Footer query: COUNT(*), COUNT(DISTINCT SessionID), COUNT(DISTINCT PlayerID) from GameSessions. Loading animation and per-query checkmarks provide visual confirmation that queries are executing. Each result line reveals with a Thread.Sleep delay.
- Future considerations: These three queries are the in-game preview of the Power BI dashboard.

---

**[REFACTOR] CSV dependency removed entirely**
- Date: May 22, 2026
- Phase: Phase 3
- Problem: WriteRecordToCSV(), LoadPlayerBalance(), and CheckDailyBonus() remained in the codebase as dead code after their SQLite replacements were confirmed working.
- Solution: Deleted WriteRecordToCSV(), LoadPlayerBalance(), and CheckDailyBonus() methods entirely. Removed csvPath variable from Main(). Removed all four WriteRecordToCSV() call sites. Removed unused using static System.Net.Mime.MediaTypeNames import.
- Future considerations: The CSV layer served its purpose as a working Phase 1-2 data store. Its removal marks the SQLite migration as complete.

---

**[REFACTOR] Comment pass on all SQLite methods**
- Date: May 22, 2026
- Phase: Phase 3
- Problem: CheckDailyBonusDB() had a placeholder comment header. Several method comments referenced CSV patterns that no longer existed.
- Solution: Full comment pass on CheckDailyBonusDB(), RegisterOrLoginPlayer(), InsertGameRecord(), InsertSessionRecord(), UpdateSessionRecord(), UpdatePlayerLifetimeStats(), and PrintQuerySummary(). All comments updated to remove CSV references and explain the SQLite architecture in consistent educational style.

---

---

# KNOWN LIMITATIONS AND TECHNICAL DEBT

| Item | Status | Phase Introduced | Planned Resolution |
|------|--------|-----------------|-------------------|
| CSV still active in parallel | ✅ RESOLVED — May 22, 2026 | Phase 3 | Deleted |
| Sessions table not yet populated | ✅ RESOLVED — May 22, 2026 | Phase 3 | InsertSessionRecord/UpdateSessionRecord implemented |
| Players lifetime stats not yet updated | ✅ RESOLVED — May 22, 2026 | Phase 3 | UpdatePlayerLifetimeStats() implemented |
| PrintQuerySummary() not yet implemented | ✅ RESOLVED — May 22, 2026 | Phase 3 | Three live SQL queries implemented |
| Comment pass on SQLite methods pending | ✅ RESOLVED — May 22, 2026 | Phase 3 | Full comment pass completed |
| Strategy suggestions not context-aware | ✅ RESOLVED — May 21, 2026 | Phase 1 | Dealer card factored into all probability calculations |
| No card counting / deck depletion tracking | OPEN | Phase 1 | Future feature — multi-deck shoe |
| Split hands not implemented | OPEN | Phase 2 | Future feature — post-Phase 5 |
| Recursive Main() for play again | OPEN | Phase 2 | Noted technical debt — acceptable for current scope |
| Soft 17 dealer rule not configurable | OPEN | Phase 1 | Future feature — adds an analytically interesting variable |

---

*This document is updated at the end of each development phase. Next update: Phase 4 Complete*
