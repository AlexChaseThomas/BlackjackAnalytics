# CHANGELOG
## BlackjackAnalytics — C# Blackjack Analytics Pipeline
**Author:** Alex Thomas
**Repository:** https://github.com/AlexChaseThomas/BlackjackAnalytics
**Document version:** 3.0 (Phase 3 In Progress)

---

## HOW TO USE THIS DOCUMENT

This changelog is a living engineering record of the BlackjackAnalytics project.
It documents every significant decision, bug fix, architectural change, and feature addition
made during development — including the reasoning behind each decision and future considerations.

Each entry follows this format:

  [CATEGORY] Short title
  - Date: When this was addressed
  - Phase: Which phase this belongs to
  - Problem: What issue or gap existed
  - Why it mattered: Impact on gameplay, data quality, or architecture
  - Solution: What was implemented
  - Future considerations: What this decision affects going forward

Categories:
- [ARCHITECTURE] — structural or design decisions
- [BUG FIX] — something was broken and was corrected
- [FEATURE] — new capability added
- [SECURITY] — privacy or security concern addressed
- [UI/UX] — player-facing experience change
- [DATA] — analytics, CSV, or data pipeline change
- [REFACTOR] — code cleaned up without changing behavior
- [DECISION] — a deliberate choice between alternatives

---

## VERSION HISTORY SUMMARY

Version | Phase | Description | Date
- 0.1 | Phase 1 | Initial game engine — college project base | May 2026
- 0.2 | Phase 1 | Betting system, token economy, CSV persistence | May 2026
- 0.3 | Phase 1 | Strategy mode, bust percentage warnings | May 2026
- 0.4 | Phase 1 | Dealer AI fixes, hard 17 rule | May 2026
- 0.5 | Phase 1 | Soft Ace handling for player and dealer | May 2026
- 0.6 | Phase 1 | Stand-on-zero guard clause | May 2026
- 1.0 | Phase 1 Complete | Stable game engine baseline committed to GitHub | May 2026
- 1.1 | Phase 2 | Single shared Random instance | May 2026
- 1.2 | Phase 2 | Dictionary replaces if/else card value chains | May 2026
- 1.3 | Phase 2 | DetermineWinner() method extracted | May 2026
- 1.4 | Phase 2 | Username system, PII removal, password gate removed | May 2026
- 1.5 | Phase 2 | Proper blackjack deal order, hole card, double down | May 2026
- 1.6 | Phase 2 | UI fixes — strategy warnings, play again flow | May 2026
- 2.0 | Phase 2 Complete | Full Phase 2 committed to GitHub | May 2026
- 2.1 | Phase 3 | 52-card deck, Fisher-Yates shuffle, Card class | May 2026
- 2.2 | Phase 3 | Game engine bug fixes — dealer, bust rules, overrides | May 2026
- 2.3 | Phase 3 | UI polish — dealer reveal pause, animations, prompts | May 2026
- 2.4 | Phase 3 | SQLite schema design, InitializeDatabase() | May 2026
- 3.0 | Phase 3 Complete | In progress | May 2026

---

---

# PHASE 1 — GAME ENGINE CONSTRUCTION

---

[ARCHITECTURE] Initial project structure and class design
- Date: May 2026
- Phase: Phase 1
- Problem: Needed to evolve a college console project into a structured analytics-ready application.
- Why it mattered: The original project was a single-file script without data classes, persistence, or analytics tracking. It could play blackjack but generated no reusable data.
- Solution: Introduced three distinct class types — PlayerInfo (identity data), SessionRecord (per-hand analytics record), and GameStats (session-level counters). Separated data classes from the logic class BlackjackGame. This mirrors real-world data modeling patterns and sets up a clean migration path to SQL tables later.
- Future considerations: PlayerInfo, SessionRecord, and GameStats map directly to future SQL tables. The field names chosen here should be preserved through the database migration to avoid breaking the analytics pipeline.

---

[FEATURE] Token economy with CSV persistence
- Date: May 2026
- Phase: Phase 1
- Problem: No state persisted between sessions — every run started from scratch.
- Why it mattered: Persistent balances are essential for meaningful analytics. Token flow over time is one of the most analytically interesting datasets the game produces.
- Solution: Implemented LoadPlayerBalance() which reads the CSV backwards to find the player's most recent TokensAfter value. New players start with 100 tokens. WriteRecordToCSV() appends a new row after every hand.
- Future considerations: The CSV approach has known limitations — no referential integrity, no querying, vulnerable to corruption. SQLite replaces this entirely in Phase 3.

---

[FEATURE] Daily login bonus system
- Date: May 2026
- Phase: Phase 1
- Problem: No incentive for players to return after running low on tokens.
- Why it mattered: Return frequency and bonus dependency are analytically interesting behavioral dimensions.
- Solution: CheckDailyBonus() reads the player's most recent LoginTime from the CSV, calculates elapsed time as a TimeSpan, and awards 50 tokens if 24 or more hours have passed. Uses an out parameter to return both the updated balance and hours remaining.
- Future considerations: In Phase 3 this is replaced with a single SQL query against the Players table which stores LastSeen directly.

---

[FEATURE] Strategy suggestion mode
- Date: May 2026
- Phase: Phase 1
- Problem: Players had no guidance on decision quality and the game produced no data about whether players made statistically sound decisions.
- Why it mattered: Strategy mode is one of the most analytically valuable features. OverrodeSuggestion enables future analysis of whether warned players bust more or less and whether strategy mode improves win rates.
- Solution: Added strategyOn boolean set at session start. CalculateBustChance() calculates bust probability from current total using parallel arrays. Warnings shown when drawing at 17+ or standing at 11 or lower.
- Future considerations: A future improvement would factor in the dealer's visible card — the core of real basic strategy.

---

[FEATURE] Betting system with forfeit confirmation
- Date: May 2026
- Phase: Phase 1
- Problem: No financial stakes made the game feel meaningless and produced no wagering data.
- Why it mattered: Bet sizing, risk behavior, and token flow are core analytics dimensions.
- Solution: Betting prompt before each hand. Minimum 5 tokens, maximum 100 or current balance. Double-ESC forfeit confirmation. BetAmount, TokensBefore, and TokensAfter written to every SessionRecord.
- Future considerations: Future analytics will correlate BetAmount with Result to identify risk behavior patterns.

---

[BUG FIX] Dealer did not draw to 17 — drew only once per player draw
- Date: May 2026
- Phase: Phase 1
- Problem: Dealer draw logic was coupled to the player draw loop. Every time the player drew, the dealer drew exactly once.
- Why it mattered: Violated fundamental blackjack rules and made every hand result unreliable.
- Solution: Removed dealer drawing from the player draw branch. Added a dedicated dealer draw phase using while (dealerTotal < 17). Dealer now draws independently after the player's turn resolves.
- Future considerations: Hard 17 rule implemented. Soft 17 configurable option is a future analytics variable idea.

---

[BUG FIX] Soft Ace handling missing for player and dealer
- Date: May 2026
- Phase: Phase 1
- Problem: Aces always counted as 11. Ace + Ace = 22 and immediate bust.
- Why it mattered: Incorrect bust detection, unfair losses, unreliable bust rate analytics.
- Solution: Added playerAces and dealerAces integer counters. After any card is added, a while (total > 21 and aces > 0) loop subtracts 10 (converting one Ace from 11 to 1) until the total is legal or no soft Aces remain.
- Future considerations: PlayerBusted now correctly reflects only genuine busts with no available Ace rescue.

---

[BUG FIX] Player could stand on 0 and win
- Date: May 2026
- Phase: Phase 1
- Problem: Pressing N before drawing any card set playerTotal to 0. Dealer bust would record a Win with PlayerTotal = 0.
- Why it mattered: Corrupted analytics data — a Win with PlayerTotal = 0 is meaningless.
- Solution: Added guard clause checking numberOfDraws == 0 before allowing a stand.
- Future considerations: This guard was removed in Phase 2 when opening deal was introduced — players now always start with two cards, making a zero-total stand impossible by design.

---

[BUG FIX] Duplicate card and total output in draw branch
- Date: May 2026
- Phase: Phase 1
- Problem: Drawn card and total printed twice per draw.
- Solution: Consolidated all output into a single print block. ReadKey(true) removed from strategy warning blocks. Game loop's own ReadKey captures the player's actual decision.

---

[DATA] CSV schema design — initial column structure
- Date: May 2026
- Phase: Phase 1
- Problem: Needed a persistent data format that would support future SQL queries, Python analysis, and Power BI dashboards.
- Solution: Designed 16-column CSV covering identity, timing, gameplay outcomes, wagering, and strategy. Column order matches the planned SQL table structure.
- Future considerations: DoubledDown added in Phase 2. Each new feature adds a column rather than repurposing existing ones.

---

---

# PHASE 2 — CODE CLEANUP + IDENTITY SYSTEM + BLACKJACK REALISM

---

[REFACTOR] Single shared Random instance at class level
- Date: May 2026
- Phase: Phase 2
- Problem: Draw() and SuitAssigner() each created a new Random instance per call. Close calls received the same seed and produced identical results.
- Why it mattered: Would produce subtle statistical anomalies corrupting card frequency analysis.
- Solution: Declared static Random rand = new Random() at class level. All methods share one instance.
- Future considerations: Not thread-safe — would need replacement if multithreading is introduced in a web version.

---

[REFACTOR] Dictionary replaces if/else card value chains
- Date: May 2026
- Phase: Phase 2
- Problem: Card value lookup was a 13-line if/else chain duplicated in three locations.
- Why it mattered: Duplicated logic is a maintenance risk. A change in one place not mirrored in others creates subtle bugs.
- Solution: Declared static Dictionary cardValues at class level. All three chains replaced with a single cardValues[cardName] lookup.
- Future considerations: Direct analog to a SQL lookup table or Python dictionary. Demonstrates key-value data structure understanding.

---

[REFACTOR] DetermineWinner() extracted from Main()
- Date: May 2026
- Phase: Phase 2
- Problem: Win/loss/tie logic lived inline in Main() and was not reusable.
- Solution: Created static string DetermineWinner(int playerTotal, int dealerTotal) returning Win, Loss, or Tie. Conditions ordered most-specific to most-general.
- Future considerations: Candidate for a shared Core/ library in a future multi-game casino platform.

---

[SECURITY] Password gate removed
- Date: May 2026
- Phase: Phase 2
- Problem: Hardcoded password visible in plain text in the source code on a public GitHub repository.
- Why it mattered: A visible hardcoded password signals poor security awareness to any code reviewer.
- Solution: Removed PasswordChecker() entirely. Program opens directly to welcome screen. Username system provides identity tracking.
- Future considerations: A future web version would use properly hashed passwords stored in the database.

---

[SECURITY] PII removal — real names and full DOB replaced
- Date: May 2026
- Phase: Phase 2
- Problem: System collected first name, last name, and full DOB and stored all of it in the CSV.
- Why it mattered: Storing real names and birthdates in a public repository is bad data governance and creates negative recruiter optics.
- Solution: Replaced names with a username system. DOB is collected for verification only, calculated into an integer age, and immediately discarded. The dob variable never touches PlayerInfo or any stored record.
- Future considerations: Age as an integer is still meaningful for analytics while being non-identifying. A production system would require email verification.

---

[FEATURE] Username system with auto-registration
- Date: May 2026
- Phase: Phase 2
- Problem: Real-name system created PII risks and duplicate identity problems.
- Solution: Players choose a username (3-20 characters, stored lowercase). Existing usernames load the previous balance. New usernames start with 100 tokens automatically.
- Future considerations: Username uniqueness not enforced in CSV — resolved in Phase 3 via SQL UNIQUE constraint.

---

[FEATURE] Proper blackjack deal order
- Date: May 2026
- Phase: Phase 2
- Problem: Original deal flow was unrealistic — dealer card shown before player had any cards.
- Why it mattered: Gameplay realism directly affects data quality. Player decisions made in an unrealistic context produce unreliable behavioral analytics.
- Solution: Standard casino deal order implemented: player receives two cards face up, dealer receives one visible and one hole card, player decides, then dealer reveals and draws.
- Future considerations: numberOfDraws now counts only additional draws beyond the opening two cards.

---

[FEATURE] Hole card reveal
- Date: May 2026
- Phase: Phase 2
- Problem: Dealer's second card was not tracked or hidden.
- Solution: dealerHoleCard stored at deal time, not displayed during player's turn. Revealed as Dealer reveals hole card before dealer draws. dealerAcesStart tracks whether either starting card was an Ace.
- Future considerations: Future analytics could track how often the dealer's hole card would have made the dealer stand without drawing.

---

[FEATURE] Double down mechanic
- Date: May 2026
- Phase: Phase 2
- Problem: Double down missing from the game — an important betting behavior data point absent.
- Solution: D key handled only on opening two cards. Valid double down doubles currentBet, deals exactly one additional card, sets gameOver = true immediately. DoubledDown bool added to SessionRecord and CSV.
- Future considerations: DoubledDown enables win rate comparison between doubled and non-doubled hands.

---

[UI/UX] Strategy warning shown for high opening hands
- Date: May 2026
- Phase: Phase 2
- Problem: Strategy warning only fired inside the draw branch — a player dealt 20 on opening received no warning.
- Solution: Strategy warning check added after opening hand display and before game loop starts.

---

[UI/UX] Compact control header reprinted after strategy warnings
- Date: May 2026
- Phase: Phase 2
- Problem: Strategy warning consumed a keypress via ReadKey(true) — player had to press N twice to stand.
- Solution: Removed ReadKey(true) from all strategy warning blocks. Compact control reminder shown instead. Game loop's own ReadKey captures the player's decision.

---

[UI/UX] Play again blended with betting prompt
- Date: May 2026
- Phase: Phase 2
- Problem: Two separate prompts after each hand — play again confirmation then betting prompt.
- Solution: Single line: Type your bet to continue, or type exit to quit. Betting loop appears immediately below.

---

[UI/UX] End of session menu added
- Date: May 2026
- Phase: Phase 2
- Problem: Session ended with static summary and no option to play again without restarting.
- Solution: Menu added with P (play again) and ESC (exit). Pressing P calls Main() recursively.
- Future considerations: Recursive Main() is technical debt. Phase 3 will replace with a proper top-level game loop.

---

[DATA] DoubledDown column added to CSV schema
- Date: May 2026
- Phase: Phase 2
- Problem: Double down implemented as a mechanic but not tracked in analytics data.
- Solution: public bool DoubledDown added to SessionRecord. Column appended to CSV header and data row.

---

[BUG FIX] Game header border misalignment
- Date: May 2026
- Phase: Phase 2
- Problem: GAME # header line did not correctly pad to box width, causing right border misalignment.
- Solution: Replaced format specifier with stats.TotalGames.ToString().PadRight(30).

---

---

# PHASE 3 — 52-CARD DECK, GAME ENGINE POLISH, SQLITE INTEGRATION

---

[FEATURE] Card class and 52-card deck system
- Date: May 2026
- Phase: Phase 3
- Problem: The original Draw() and SuitAssigner() methods picked randomly from a 13-card array with no memory of what was already drawn. The same card could appear multiple times in one hand — impossible in a real deck.
- Why it mattered: Duplicate cards in a hand corrupt every analytics dimension — hand totals, bust rates, draw counts. The data being generated was statistically invalid.
- Solution: Introduced the Card class with Name, Suit, and ToString() override (prints Ace of Hearts). BuildDeck() creates a full 52-card List (13 values x 4 suits). ShuffleDeck() uses the Fisher-Yates algorithm — the standard unbiased shuffle that gives every possible ordering equal probability. DealCard() deals from index 0, removes the card, and auto-reshuffles when fewer than 10 cards remain. Duplicate cards within a hand are now impossible.
- Future considerations: The 10-card reshuffle threshold is conservative. A future multi-deck shoe version would track cards across multiple decks for card counting analytics.

---

[ARCHITECTURE] File header condensed and unused imports removed
- Date: May 2026
- Phase: Phase 3
- Problem: The file opened with a large multi-paragraph OOP explanation block and included using System.Runtime.InteropServices which was never needed.
- Why it mattered: Stale educational scaffolding in a professional portfolio project signals a learning-phase codebase rather than a polished one. Unused imports add noise.
- Solution: Replaced the large comment block with a clean header showing author, GitHub link, version, and class inventory. Removed the accidental import. Preserved architectural decision comments inside methods where they add genuine value.

---

[BUG FIX] Opening Blackjack dealer draw missing
- Date: May 2026
- Phase: Phase 3
- Problem: When the player hit 21 on the opening deal, the game skipped the dealer draw phase entirely. The dealer did not draw to 17 and the result was determined against the dealer's opening two-card total only.
- Why it mattered: The house rule requires the dealer to draw to 17 regardless of player outcome. Skipping this meant some wins were incorrectly awarded when the dealer would have also reached 21 (a tie) or beyond.
- Solution: Added a dedicated dealer draw loop inside the opening Blackjack resolution block using while (dealerTotal < 17) with soft Ace handling. Dealer now always completes their hand before DetermineWinner() is called.

---

[BUG FIX] Dealer did not complete hand when player busted
- Date: May 2026
- Phase: Phase 3
- Problem: The dealer draw loop was wrapped in if (playerTotal <= 21) — meaning when the player busted, the dealer never drew. DealerTotal in the CSV recorded only the opening two-card total, not the actual final hand.
- Why it mattered: DealerTotal being wrong on bust hands corrupts every analytics query that uses that field — average dealer totals, dealer bust rates, hand comparison distributions.
- Solution: Removed the guard clause from around the dealer draw loop. Dealer now always draws to 17 regardless of player bust. DealerTotal in every CSV and database row now reflects the actual completed dealer hand.

---

[BUG FIX] Both-bust rule — player always loses when they bust
- Date: May 2026
- Phase: Phase 3
- Problem: DetermineWinner() had a special case returning Tie when both player and dealer busted. In real blackjack the player always loses when they bust — the dealer does not need to also bust to win. The house edge depends on this rule.
- Why it mattered: Recording Tie instead of Loss on both-bust hands and refunding the bet incorrectly represents the game's economics. Token flow analytics and win rate calculations would be wrong.
- Solution: Removed the both-bust Tie case. Added if (playerTotal > 21) return Loss as the second condition in DetermineWinner() — player bust is always a loss regardless of dealer outcome.

---

[BUG FIX] Both-21 should be a Tie not a Win
- Date: May 2026
- Phase: Phase 3
- Problem: When both player and dealer hit exactly 21, DetermineWinner() returned Win because the player-21 check fired before the dealer-21 check.
- Why it mattered: A push (tie) when both reach 21 is the correct casino rule. Recording it as a Win overstates win rates and awards tokens incorrectly.
- Solution: Added if (playerTotal == 21 and dealerTotal == 21) return Tie as the first condition in DetermineWinner(), before all other checks.

---

[BUG FIX] Duplicate BLACKJACK print in opening deal path
- Date: May 2026
- Phase: Phase 3
- Problem: The BLACKJACK! You hit 21 on the deal! message printed twice when the player hit 21 on the opening hand.
- Solution: Removed the duplicate Console.WriteLine block. Message now prints exactly once.

---

[BUG FIX] overrodeSuggestion logic was inverted
- Date: May 2026
- Phase: Phase 3
- Problem: overrodeSuggestion was set to true whenever a strategy warning was shown, even if the player then stood correctly. It should only be true if the player was warned AND chose to draw anyway.
- Why it mattered: The SuggestionsOverridden count in the session summary was inflated. Every warning shown counted as an override even when the player followed the advice. This made the strategy analytics unreliable.
- Solution: Introduced warningActive bool declared at the top of each hand's variables. Strategy warnings set warningActive = true instead of overrodeSuggestion = true. At the top of the draw branch, if warningActive is true the draw is flagged as an override, overrodeSuggestion is set to true, stats.SuggestionsOverridden increments, and warningActive resets to false. Standing after a warning no longer counts as an override.

---

[BUG FIX] Exit at betting prompt dealt a phantom hand
- Date: May 2026
- Phase: Phase 3
- Problem: Typing exit at the betting prompt set sessionActive = false and broke out of the betting loop, but the session loop continued into the hand setup, dealing cards and starting the game loop with a zero bet. The player was forced to ESC out of a hand they never intended to play.
- Solution: Added if (!sessionActive) break immediately after the betting loop closes. If the player typed exit, execution skips the entire hand setup and jumps directly to the session summary.

---

[BUG FIX] Hands played count off by one when exiting
- Date: May 2026
- Phase: Phase 3
- Problem: stats.TotalGames++ incremented at the top of the session loop before the bet was placed. When the player typed exit, the counter had already incremented for a hand that was never played.
- Why it mattered: The session summary showed one more hand than was actually played, and the last SessionRecord written had an incorrect GameNumber.
- Solution: Moved stats.TotalGames++ to after the if (!sessionActive) break guard clause — the counter only increments after a valid bet is confirmed and a hand is actually starting.

---

[UI/UX] Dealer hole card reveal pause animation
- Date: May 2026
- Phase: Phase 3
- Problem: The dealer's hole card and all subsequent dealer draws appeared instantly with no pacing. The suspense of the dealer reveal — a core part of the blackjack experience — was absent.
- Solution: Added Thread.Sleep(1500) before the hole card reveal with a Dealer revealing... prompt. Added Thread.Sleep(800) before each dealer draw with a Dealer drawing... prompt. Both use Console.SetCursorPosition to overwrite the suspense line with the actual card reveal, mimicking a card flip animation.
- Future considerations: Cursor positioning behavior varies between terminal emulators. If the overwrite effect does not render correctly in a specific environment, falling back to Console.WriteLine without overwriting still preserves the timing pause.

---

[UI/UX] Dealer hole card always revealed even on player bust
- Date: May 2026
- Phase: Phase 3
- Problem: When the player busted, the dealer's hole card was never revealed. The player lost but had no visibility into what the dealer was holding.
- Why it mattered: Transparency in game outcome is important for player trust and UX. A player should always be able to see what they were up against, even when they lose by busting.
- Solution: Moved the hole card reveal block outside the if (playerTotal <= 21) guard. The hole card is now always revealed regardless of player bust status. The dealer draw loop also always runs — see the DealerTotal accuracy fix above.

---

[UI/UX] Prompt text consistency
- Date: May 2026
- Phase: Phase 3
- Problem: The continue/exit prompt used different wording in different paths — Type your bet to continue, or type exit to quit in some places and Place a bet to continue, or type exit to see your session summary in others.
- Solution: Standardized all instances to: Place a bet to continue, or type exit to see your session summary. This wording is more informative — the player knows typing exit will show them something useful rather than just closing.

---

[ARCHITECTURE] SQLite database schema designed and initialized
- Date: May 2026
- Phase: Phase 3
- Problem: CSV flat file has no referential integrity, no querying capability, and no enforcement of data constraints. Username uniqueness cannot be enforced at the storage level.
- Why it mattered: The analytics pipeline requires a queryable data store. SQL queries for win rates, bust rates, and strategy impact cannot run against a CSV. The Python and Power BI phases depend on a database being in place.
- Solution: Designed a normalized two-table schema. Players table stores one row per unique username with PlayerID (AUTOINCREMENT PRIMARY KEY), Username (UNIQUE NOT NULL), PlayerAge, FirstSeen, LastSeen, and TokenBalance. GameSessions table stores one row per hand with a foreign key reference back to Players via Username. All bool fields stored as INTEGER (0 or 1) since SQLite has no native bool type. InitializeDatabase() creates both tables using CREATE TABLE IF NOT EXISTS — safe to call every program start. System.Data.SQLite.Core v1.0.119 NuGet package installed.
- Future considerations: All SQL queries will use parameterized queries (the ? placeholder pattern) to prevent SQL injection. RegisterOrLoginPlayer(), InsertGameRecord(), and PrintQuerySummary() are the next three methods to implement.

---

---

# KNOWN LIMITATIONS AND TECHNICAL DEBT

- Item | Phase introduced | Planned resolution
- Username uniqueness not enforced | Phase 2 | Phase 3 — SQL UNIQUE constraint
- ESC during betting requires typing exit | Phase 2 | Phase 3 — ReadKey betting flow
- Recursive Main() for play again | Phase 2 | Phase 3 — top-level game loop
- CSV still active in parallel | Phase 3 | Phase 3 — deleted after SQLite complete
- CalculateBustChance uses parallel arrays | Phase 1 | Phase 3 — replace with Dictionary
- No running hand display | Phase 1 | Phase 3 — show all cards held
- Soft Ace not visually indicated | Phase 1 | Phase 3 — show Ace counting as 1
- Username allows spaces | Phase 2 | Phase 3 — add space validation
- No card counting / deck depletion | Phase 1 | Future feature — multi-deck tracking
- Strategy suggestions not context-aware | Phase 1 | Future feature — dealer card factored in
- Split hands not implemented | Phase 2 | Future feature — post-Phase 3

---

---

# UPCOMING — PHASE 3 REMAINING WORK

The following items are in progress or planned for completion before Phase 3 is committed as complete.

Game engine fixes still in progress:
- Fix 5 — Strategy warning on N stand waits for next keypress before standing
- Fix 7 — Running hand display (show all cards held, not just last drawn)
- Fix 8 — Bet confirmation line before cards are dealt
- Fix 9 — Visual separator between hands
- Fix 10 — Bust message before dealer turn
- Fix 11 — Session summary shows PlayerBusts and DealerBusts counts
- Fix 12 — CalculateBustChance parallel arrays replaced with Dictionary
- Fix 13 — Username validation rejects spaces
- Fix 14 — Soft Ace display shows when Ace is counting as 1

SQLite methods to implement:
- RegisterOrLoginPlayer() — replaces LoadPlayerBalance() and username entry flow
- InsertGameRecord() — replaces WriteRecordToCSV()
- CheckDailyBonus() migrated to read from Players.LastSeen
- PrintQuerySummary() — live SQL analytics at end of session
- Delete CSV dependency entirely
- Test: play 10 hands, verify rows appear correctly in both tables
- Commit: Phase 3 complete

---

This document is updated at the end of each development phase.
Next update: Phase 3 Complete
