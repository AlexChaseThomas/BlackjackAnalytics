# CHANGELOG
## BlackjackAnalytics — C# Blackjack Analytics Pipeline
**Author:** Alex Thomas  
**Repository:** https://github.com/AlexChaseThomas/BlackjackAnalytics  
**Document version:** 2.0 (Phase 2 Complete)

---

## HOW TO USE THIS DOCUMENT

This changelog is a living engineering record of the BlackjackAnalytics project.
It documents every significant decision, bug fix, architectural change, and feature addition
made during development — including the reasoning behind each decision and future considerations.

Each entry follows this format:

```
### [CATEGORY] Short title
- **Date:** When this was addressed
- **Phase:** Which phase this belongs to
- **Problem:** What issue or gap existed
- **Why it mattered:** Impact on gameplay, data quality, or architecture
- **Solution:** What was implemented
- **Future considerations:** What this decision affects going forward
```

**Categories:**
- `[ARCHITECTURE]` — structural or design decisions
- `[BUG FIX]` — something was broken and was corrected
- `[FEATURE]` — new capability added
- `[SECURITY]` — privacy or security concern addressed
- `[UI/UX]` — player-facing experience change
- `[DATA]` — analytics, CSV, or data pipeline change
- `[REFACTOR]` — code cleaned up without changing behavior
- `[DECISION]` — a deliberate choice between alternatives

---

## VERSION HISTORY SUMMARY

| Version | Phase | Description | Date |
|---|---|---|---|
| 0.1 | Phase 1 | Initial game engine — college project base | May 2026 |
| 0.2 | Phase 1 | Betting system, token economy, CSV persistence | May 2026 |
| 0.3 | Phase 1 | Strategy mode, bust percentage warnings | May 2026 |
| 0.4 | Phase 1 | Dealer AI fixes, hard 17 rule | May 2026 |
| 0.5 | Phase 1 | Soft Ace handling for player and dealer | May 2026 |
| 0.6 | Phase 1 | Stand-on-zero guard clause | May 2026 |
| 1.0 | Phase 1 Complete | Stable game engine baseline committed to GitHub | May 2026 |
| 1.1 | Phase 2 | Single shared Random instance | May 2026 |
| 1.2 | Phase 2 | Dictionary replaces if/else card value chains | May 2026 |
| 1.3 | Phase 2 | DetermineWinner() method extracted | May 2026 |
| 1.4 | Phase 2 | Username system, PII removal, password gate removed | May 2026 |
| 1.5 | Phase 2 | Proper blackjack deal order, hole card, double down | May 2026 |
| 1.6 | Phase 2 | UI fixes — strategy warnings, play again flow | May 2026 |
| 2.0 | Phase 2 Complete | Full Phase 2 committed to GitHub | May 2026 |

---

---

# PHASE 1 — GAME ENGINE CONSTRUCTION

---

### [ARCHITECTURE] Initial project structure and class design
- **Date:** May 2026
- **Phase:** Phase 1
- **Problem:** Needed to evolve a college console project into a structured analytics-ready application.
- **Why it mattered:** The original project was a single-file script without data classes, persistence, or analytics tracking. It could play blackjack but generated no reusable data.
- **Solution:** Introduced three distinct class types — `PlayerInfo` (identity data), `SessionRecord` (per-hand analytics record), and `GameStats` (session-level counters). Separated data classes from the logic class `BlackjackGame`. This mirrors real-world data modeling patterns and sets up a clean migration path to SQL tables later.
- **Future considerations:** `PlayerInfo`, `SessionRecord`, and `GameStats` map directly to future SQL tables. The field names chosen here should be preserved through the database migration to avoid breaking the analytics pipeline.

---

### [FEATURE] Token economy with CSV persistence
- **Date:** May 2026
- **Phase:** Phase 1
- **Problem:** No state persisted between sessions — every run started from scratch with no memory of previous play.
- **Why it mattered:** Persistent balances are essential for meaningful analytics. Token flow over time (gains, losses, streaks, risk behavior) is one of the most analytically interesting datasets the game produces. Without persistence, every session is isolated and unconnected.
- **Solution:** Implemented `LoadPlayerBalance()` which reads the CSV backwards to find the player's most recent `TokensAfter` value. New players start with 100 tokens. `WriteRecordToCSV()` appends a new row after every hand. The CSV acts as a flat database until SQLite integration in Component 2.
- **Future considerations:** The CSV approach has known limitations — no referential integrity, no querying, vulnerable to corruption if the file is manually edited. SQLite will replace this entirely in Component 2. The column schema was designed to match the planned SQL schema from the start.

---

### [FEATURE] Daily login bonus system
- **Date:** May 2026
- **Phase:** Phase 1
- **Problem:** No incentive for players to return after running low on tokens, and no way to reward consistent engagement.
- **Why it mattered:** A daily bonus adds a behavioral dynamic that produces analytically interesting data — return frequency, bonus dependency, session patterns. It also makes the game more realistic as a casino simulation.
- **Solution:** Implemented `CheckDailyBonus()` which reads the player's most recent `LoginTime` from the CSV, calculates the elapsed time as a `TimeSpan`, and awards 50 tokens if 24 or more hours have passed. Uses an `out` parameter to return both the updated balance and the hours remaining until the next bonus.
- **Future considerations:** The daily bonus check currently reads the entire CSV to find the last login. In Component 2 this will be replaced with a single SQL query against the `Players` table which stores `LastSeen` directly, making this much more efficient.

---

### [FEATURE] Strategy suggestion mode
- **Date:** May 2026
- **Phase:** Phase 1
- **Problem:** Players had no guidance on decision quality and the game produced no data about whether players made statistically sound decisions.
- **Why it mattered:** Strategy mode is one of the most analytically valuable features in the project. By tracking whether suggestions were shown and whether the player followed them, the dataset can answer questions like: do warned players bust more or less? Does strategy mode improve win rates? What is the override rate by total?
- **Solution:** Added `strategyOn` boolean set once at session start. `CalculateBustChance()` calculates bust probability from the current total using parallel arrays. Warnings shown when drawing at 17+ or standing at 11 or lower. `OverrodeSuggestion` boolean written to every `SessionRecord`.
- **Future considerations:** The strategy warning currently fires on every draw above 17. A future improvement would be context-aware suggestions that factor in the dealer's visible card — the core of real basic strategy. This would be a significant analytics feature improvement.

---

### [FEATURE] Betting system with forfeit confirmation
- **Date:** May 2026
- **Phase:** Phase 1
- **Problem:** No financial stakes made the game feel meaningless and produced no wagering data for analytics.
- **Why it mattered:** Bet sizing, risk behavior, and token flow are core analytics dimensions. Without a betting system there is no way to analyze player risk appetite or correlate bet size with outcomes.
- **Solution:** Added betting prompt before each hand. Minimum 5 tokens, maximum 100 tokens or current balance, whichever is lower. Double-ESC confirmation before forfeiting a bet. `BetAmount`, `TokensBefore`, and `TokensAfter` written to every `SessionRecord`.
- **Future considerations:** Future analytics will correlate `BetAmount` with `Result` to identify risk behavior patterns. The `TokensBefore` and `TokensAfter` fields together allow reconstruction of the player's full financial history hand by hand.

---

### [BUG FIX] Dealer did not draw to 17 — drew only once per player draw
- **Date:** May 2026
- **Phase:** Phase 1
- **Problem:** The dealer draw logic was coupled to the player draw loop. Every time the player drew a card, the dealer drew exactly one card. This meant the dealer could stand on 3 or draw to 30 depending on how many cards the player took.
- **Why it mattered:** This violated the fundamental rule of blackjack and made every hand result unreliable. The data being generated was not realistic and would produce meaningless analytics.
- **Solution:** Removed dealer drawing from the player draw branch entirely. Added a dedicated dealer draw phase inside `if (gameOver && sessionActive)` using a `while (dealerTotal < 17)` loop. The dealer now draws independently after the player's turn is fully resolved, following the standard hard 17 rule.
- **Future considerations:** The hard 17 rule (stand on all 17s) is implemented. Some casinos use soft 17 (hit on soft 17 — Ace + 6). This could be added as a configurable option in a future version and would make an interesting analytics variable.

---

### [BUG FIX] Soft Ace handling missing for player and dealer
- **Date:** May 2026
- **Phase:** Phase 1
- **Problem:** Aces were always counted as 11. Drawing Ace + Ace produced a total of 22 and an immediate bust, even though standard blackjack rules allow one Ace to count as 1.
- **Why it mattered:** This caused incorrect bust detection and unfair losses. It also made the bust rate analytics unreliable since some busts were not genuine busts under real rules.
- **Solution:** Added `playerAces` and `dealerAces` integer counters tracking how many Aces are currently counted as 11. After any card is added to the total, a `while (total > 21 && aces > 0)` loop subtracts 10 (converting one Ace from 11 to 1) until the total is legal or no soft Aces remain. Applied to player draw phase, opening hand deal, and dealer draw phase.
- **Future considerations:** The Ace counter resets each hand. In a future multi-deck or card-counting version, Ace tracking would need to persist across hands. For analytics purposes, `PlayerBusted` now correctly reflects only genuine busts with no available Ace rescue.

---

### [BUG FIX] Player could stand on 0 and win
- **Date:** May 2026
- **Phase:** Phase 1
- **Problem:** Pressing N immediately after a hand started — before drawing any card — set `playerTotal` to 0. If the dealer then busted, the result was recorded as a Win with `PlayerTotal = 0`, which is an invalid game state.
- **Why it mattered:** This produced corrupted analytics data. A Win with `PlayerTotal = 0` and `NumberOfDraws = 0` is meaningless and would skew win rate calculations and hand total distributions.
- **Solution:** Added a guard clause in the stand branch checking `if (numberOfDraws == 0)` — if true, print an error and do not set `gameOver = true`. The player must draw at least one card before standing.
- **Future considerations:** This guard clause was later removed in Phase 2 when the opening deal system was introduced — players now always start with two cards, making a zero-total stand impossible by design. The data integrity concern is fully resolved.

---

### [BUG FIX] Duplicate card and total output in draw branch
- **Date:** May 2026
- **Phase:** Phase 1
- **Problem:** The player's drawn card and total were printed twice per draw — once before the strategy warning check and once after it.
- **Why it mattered:** Made the UI confusing and looked like a bug to any observer of the game.
- **Solution:** Consolidated all output into a single print block after the card value is calculated and the total is updated. The strategy warning prints after the single output block. `ReadKey(true)` was removed from the strategy warning — the game loop's `ReadKey` at the top of the next iteration serves as the acknowledgment.
- **Future considerations:** None — this was a straightforward output ordering fix.

---

### [DATA] CSV schema design — initial column structure
- **Date:** May 2026
- **Phase:** Phase 1
- **Problem:** Needed a persistent data format that would store enough information to support future SQL queries, Python analysis, and Power BI dashboards.
- **Why it mattered:** The CSV is the intermediate data store before SQLite integration. If the schema was poorly designed, the SQL migration would require transformation work rather than a clean lift-and-shift.
- **Solution:** Designed the CSV with 16 columns covering identity (`SessionID`, `Name`, `PlayerAge`), timing (`LoginTime`, `GameNumber`), gameplay outcomes (`PlayerTotal`, `DealerTotal`, `Result`, `PlayerBusted`, `DealerBusted`, `NumberOfDraws`), wagering (`BetAmount`, `TokensBefore`, `TokensAfter`), and strategy (`StrategyMode`, `OverrodeSuggestion`). Column order matches the planned SQL table structure.
- **Future considerations:** `Name` was later renamed `Username` in Phase 2. `PlayerAge` remains. `DoubledDown` was added in Phase 2. Each new feature should add a column rather than repurpose an existing one to maintain backwards compatibility awareness.

---

---

# PHASE 2 — CODE CLEANUP + IDENTITY SYSTEM + BLACKJACK REALISM

---

### [REFACTOR] Single shared Random instance at class level
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** `Draw()` and `SuitAssigner()` each created a new `Random` instance on every call. If two calls happened close together in time, they received the same seed from the system clock and produced identical results — meaning the same card could be drawn twice in quick succession.
- **Why it mattered:** This is a known C# bug pattern. It would produce subtle statistical anomalies in the game data — card distribution would not be uniform, which would corrupt any frequency analysis done in Python or SQL later.
- **Solution:** Declared `static Random rand = new Random()` at the `BlackjackGame` class level. Removed `new Random()` from inside both `Draw()` and `SuitAssigner()`. All methods now share one instance. One `rand` object = one seed = truly random sequence.
- **Future considerations:** This is the correct pattern for all C# applications using `Random`. In Component 2+, if multithreading is ever introduced (e.g. for a web version), `Random` is not thread-safe and would need to be replaced with `System.Security.Cryptography.RandomNumberGenerator` or a thread-local instance.

---

### [REFACTOR] Dictionary replaces if/else card value chains
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** Card value lookup was implemented as a 13-line if/else chain in three separate locations — player draw, dealer starting card, and dealer draw phase. Any change to card values required updating all three locations.
- **Why it mattered:** Duplicated logic is a maintenance risk. A change in one place that isn't mirrored in the others creates subtle bugs. It also made the code harder to read and scan.
- **Solution:** Declared `static Dictionary<string, int> cardValues` at the class level containing all 13 card-to-value mappings. All three if/else chains replaced with a single `cardValues[cardName]` lookup. The `CalculateBustChance()` method retains its own local parallel arrays since it uses a different data structure for probability calculation.
- **Future considerations:** The Dictionary is a direct analog to a SQL lookup table or a Python dictionary. It demonstrates understanding of key-value data structures which is relevant to data engineering roles. If additional card types were ever added (jokers, custom decks), only the Dictionary needs updating.

---

### [REFACTOR] DetermineWinner() extracted from Main()
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** Win/loss/tie determination logic lived inline in `Main()` as an 8-line if/else chain. It was not reusable and mixed result logic with display logic.
- **Why it mattered:** Extracting logic into named methods is a core software engineering practice (DRY — Don't Repeat Yourself). It also makes the logic easier to unit test in a future phase.
- **Solution:** Created `static string DetermineWinner(int playerTotal, int dealerTotal)` returning `"Win"`, `"Loss"`, or `"Tie"`. Conditions ordered most-specific to most-general to prevent case swallowing. The inline chain in `Main()` replaced with `string result = DetermineWinner(playerTotal, dealerTotal)`.
- **Future considerations:** `DetermineWinner()` is a candidate for moving into a shared `Core/` library when the casino platform architecture is implemented. It is game-agnostic enough to be reused in other card games with minor modification.

---

### [SECURITY] Password gate removed
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** The program opened with a hardcoded password (`"Password"`) visible in plain text in the source code. Anyone reading the GitHub repository could see the password instantly, making it security theater rather than actual protection.
- **Why it mattered:** For a public portfolio project, a visible hardcoded password is worse than no password — it signals poor security awareness to anyone reviewing the code. It also blocked public users from running the game at all.
- **Solution:** Removed `PasswordChecker()` method and the password gate block entirely from `Main()`. The program now opens directly to the welcome screen. The username system introduced in the same phase provides identity tracking without a shared password.
- **Future considerations:** Authentication for a future web version would use proper hashed passwords (bcrypt) stored in the database, never plain text. This would be implemented in a future authentication phase, not the current console version.

---

### [SECURITY] PII removal — real names and full DOB replaced
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** The original system collected first name, last name, and full date of birth (MM/DD/YYYY) and stored all of it in the CSV. For a public portfolio project with a publicly accessible database, this constituted a genuine PII (Personally Identifiable Information) risk.
- **Why it mattered:** Storing real names and birthdates in a public GitHub repository and future public database is bad data governance practice. It also creates negative recruiter optics — a data analytics professional who doesn't think about data privacy raises red flags.
- **Solution:** Replaced `PlayerInfo.FirstName` and `PlayerInfo.LastName` with `PlayerInfo.Username` — a player-chosen identifier with no real-world identity mapping. Full DOB is still collected for age verification but is calculated into an integer age and immediately discarded — the `dob` variable exists only within the verification while loop and never touches `PlayerInfo` or any stored record. Only `playerAge` (an int) persists. `Name` column in CSV renamed to `Username`.
- **Future considerations:** Age as an integer is still meaningful for analytics (age group segmentation, risk behavior by age cohort) while being non-identifying. The username system does not prevent someone from registering under a fake name — that is intentional and appropriate for a demo game. A production system would require email verification.

---

### [FEATURE] Username system with auto-registration
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** The real-name system created PII risks and duplicate identity problems (two people with the same name would share a balance).
- **Why it mattered:** A unique identifier per player is essential for the analytics pipeline. Without it, player history cannot be reliably tracked across sessions and the `Players` table in the future SQL database has no reliable primary key.
- **Solution:** Players choose a username (3-20 characters, stored lowercase via `.ToLower()` to prevent case-sensitivity duplicates). If the username exists in the CSV the player's last balance is loaded. If it doesn't exist they start with 100 tokens automatically — no explicit registration step needed. Username is used as the player identifier throughout the session and in every `SessionRecord`.
- **Future considerations:** Username uniqueness is not enforced in the CSV version — two players could theoretically use the same username. This will be fixed in Component 2 when the `Players` SQL table is created with a `UNIQUE` constraint on `Username`. The database itself will reject duplicate registrations at the INSERT level.

---

### [FEATURE] Proper blackjack deal order
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** The original deal flow showed the dealer's visible card before the player had any cards, then the player drew cards one at a time manually. This is not how blackjack works and created a poor UX — a player seeing a dealer King with no cards of their own has no reason to continue.
- **Why it mattered:** Gameplay realism directly affects data quality. If the deal order is wrong, player decisions are made in an unrealistic context and the behavioral analytics produced are not comparable to real blackjack play patterns.
- **Solution:** Implemented standard casino deal order: (1) player places bet, (2) player receives two cards face up automatically, (3) dealer receives one card face up and one hole card face down, (4) player sees their total and dealer's visible card, (5) player decides to hit/stand/double, (6) after player finishes dealer reveals hole card and draws to 17+. The opening two player cards and dealer starting hand are dealt automatically before the game loop starts.
- **Future considerations:** The `numberOfDraws` counter now counts only additional draws beyond the opening two cards. This makes `NumberOfDraws = 0` mean "player stood on opening hand" rather than "player never drew" — a more analytically meaningful definition.

---

### [FEATURE] Hole card reveal
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** The dealer's second card was not tracked or hidden — the dealer effectively played with one card throughout the hand.
- **Why it mattered:** The hole card is fundamental to blackjack strategy. A player's decision to hit or stand is partly based on what they can infer about the dealer's hidden card. Without it, the strategy analytics lose a key dimension.
- **Solution:** Added `dealerHoleCard` and `dealerHoleSuit` variables storing the dealer's second card at deal time. The hole card is not displayed during the player's turn — `Dealer hole card: [hidden]` is shown instead. After the player's turn ends, the dealer reveals: `Dealer reveals hole card: X of Y` before drawing to 17+. `dealerAcesStart` tracks whether either starting card was an Ace for correct soft Ace handling through the dealer draw phase.
- **Future considerations:** Future analytics could track how often the dealer's hole card would have made the dealer's total 17+ without drawing — "dealer stood on opening hand" scenarios. This is interesting data for strategy analysis.

---

### [FEATURE] Double down mechanic
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** Double down is a standard blackjack mechanic missing from the game. Its absence made the game less realistic and removed an important betting behavior data point.
- **Why it mattered:** Double down decisions are some of the most analytically interesting in blackjack — they represent high-conviction bets on strong opening hands. Tracking `DoubledDown` in the analytics data allows future analysis of whether players double down correctly and whether it correlates with wins.
- **Solution:** Added `[D]` key input handled only when `numberOfDraws == 0` (opening two cards, no additional draws taken) and `!doubledDown` (cannot double twice). Valid double down doubles `currentBet` using `*=`, deals exactly one additional card, applies soft Ace adjustment, and sets `gameOver = true` immediately — no further drawing. If the player lacks sufficient tokens to double, an error is shown and the loop continues. `DoubledDown` bool added to `SessionRecord` and CSV schema.
- **Future considerations:** Double down is only available on the opening two cards (standard casino rule). Some casinos allow doubling after splits — not implemented since split hands are not yet supported. `DoubledDown` in the analytics data will enable win rate comparison between doubled and non-doubled hands.

---

### [UI/UX] Strategy warning shown for high opening hands
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** The strategy warning only fired inside the draw branch — meaning a player dealt 20 on their opening two cards received no warning before hitting. The warning logic assumed the player had zero cards at game start.
- **Why it mattered:** A player hitting from 20 is a clear strategy violation. If strategy mode is on and no warning appears, the data records an `OverrodeSuggestion = false` for a hand where a suggestion should have been given — corrupting the strategy analytics.
- **Solution:** Added a strategy warning check immediately after the opening hand is displayed and before the game loop starts. If `strategyOn && playerTotal >= 17 && !gameOver`, the bust percentage warning is shown with the compact control header. The game loop then reads the next keypress normally.
- **Future considerations:** This warning currently uses the same 17+ threshold as the mid-hand warning. A more sophisticated version would show different thresholds based on the dealer's visible card — the foundation of real basic strategy tables.

---

### [UI/UX] Compact control header reprinted after strategy warnings
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** After a strategy warning, the original code printed "Press any key to continue..." which consumed the next keypress via `ReadKey(true)`. This meant the player had to press N twice to stand — once to dismiss the warning and once to actually stand.
- **Why it mattered:** Extra keypresses are confusing and feel like bugs. A player who presses N to stand and nothing happens will try again, and the second N will stand — but the experience is broken.
- **Solution:** Removed `ReadKey(true)` from all strategy warning blocks. Replaced "press any key" text with a compact control reminder showing `[ENTER] Draw anyway  [N] Stand  [ESC] Quit`. The game loop's own `ReadKey(true)` at the top of the next iteration captures the player's actual decision. The warning is purely informational.
- **Future considerations:** This pattern — informational display without consuming input — is the correct approach for non-blocking warnings. It keeps the input model consistent throughout the game loop.

---

### [UI/UX] Play again blended with betting prompt
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** After each hand, the player saw two separate prompts: "Play again? (Enter = yes, Escape = no)" followed by the betting prompt. This was redundant — placing a bet is itself confirmation of playing again.
- **Why it mattered:** Two prompts where one suffices adds unnecessary friction. It also made the session feel choppy between hands.
- **Solution:** Removed the standalone play again prompt. Replaced with a single line: "Type your bet to continue, or type 'exit' to quit." The betting loop appears immediately below it. Typing `exit` sets `sessionActive = false` and breaks out of the betting loop cleanly.
- **Future considerations:** The current exit mechanism requires typing "exit" because `Console.ReadLine()` cannot detect ESC natively. Component 2 will replace the betting input with a `ReadKey`-based flow that can respond to ESC directly, making this more intuitive.

---

### [UI/UX] End of session menu added
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** The session ended with a static summary and "press any key to exit" — no option to play again without restarting the entire program.
- **Why it mattered:** A dead end after every session reduces engagement and makes the game feel unfinished. It also means the player has to re-enter their username and DOB every time they want to play another session.
- **Solution:** Added a menu at the end of each session with `[P] Play again (new session)` and `[ESC] Exit`. Pressing P calls `Main()` recursively, allowing the player to log in again as the same or different user. Session summary now shows total hands, wins, losses, ties, strategy mode, and suggestions overridden.
- **Future considerations:** Recursive `Main()` is a simple solution that works but has a known limitation — deeply recursive calls would eventually cause a stack overflow after hundreds of sessions. Component 2 will replace this with a proper top-level game loop that doesn't recurse. For the current console version with typical session lengths this is not a practical concern.

---

### [DATA] DoubledDown column added to CSV schema
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** Double down was implemented as a game mechanic but not tracked in the analytics data.
- **Why it mattered:** An untracked mechanic produces no analytics value. `DoubledDown` enables future analysis of double down frequency, win rate when doubling, and whether players double on optimal hands.
- **Solution:** Added `public bool DoubledDown` to `SessionRecord`. Added `DoubledDown` to both the CSV header and data row in `WriteRecordToCSV()`. Column appended at the end of the schema to avoid shifting existing column indexes. `LoadPlayerBalance()` and `CheckDailyBonus()` unaffected since `TokensAfter` remains at index 13.
- **Future considerations:** `DoubledDown` in the SQL `GameSessions` table will enable queries like `SELECT AVG(CASE WHEN DoubledDown = 1 AND Result = 'Win' THEN 1.0 ELSE 0.0 END)` — double down win rate. This is a meaningful business intelligence metric.

---

### [BUG FIX] Game header border misalignment
- **Date:** May 2026
- **Phase:** Phase 2
- **Problem:** The GAME # header line used a format specifier that did not correctly pad to the box width, causing the right border `║` to be misaligned for single-digit game numbers.
- **Why it mattered:** Visual bugs undermine the professionalism of the UI, especially for a portfolio project where code is reviewed publicly.
- **Solution:** Replaced the format specifier with `stats.TotalGames.ToString().PadRight(30)`. The string `║  GAME #` is 8 characters, plus 30 padded characters = 38 total between borders, matching the box width exactly. Works correctly for game numbers 1 through 999.
- **Future considerations:** If game numbers ever exceed 3 digits (1000+ hands in a session), the padding would shift by one character. `PadRight(29)` would correct this. Not a practical concern for the current version.

---

---

# KNOWN LIMITATIONS AND TECHNICAL DEBT

The following items are documented as known limitations rather than bugs — they are intentional simplifications appropriate for the current phase that will be addressed in future components.

| Item | Phase introduced | Planned resolution |
|---|---|---|
| Username uniqueness not enforced | Phase 2 | Component 2 — SQL UNIQUE constraint |
| ESC during betting requires typing "exit" | Phase 2 | Component 2 — ReadKey betting flow |
| Recursive Main() for play again | Phase 2 | Component 2 — top-level game loop |
| CSV vulnerable to manual corruption | Phase 1 | Component 2 — SQLite replaces CSV |
| No card counting / deck depletion | Phase 1 | Future feature — multi-deck tracking |
| Strategy suggestions not context-aware | Phase 1 | Future feature — dealer card factored in |
| Split hands not implemented | Phase 2 | Future feature — post-Component 2 |
| No authentication for web version | Phase 2 | Future — authentication phase |

---

---

# UPCOMING — COMPONENT 2 (SQLite Integration)

The next major development phase. Entries will be added here as work is completed.

### Planned changes:
- Install `System.Data.SQLite` NuGet package
- Create `InitializeDatabase()` — creates `.db` file and tables on first run
- Create `RegisterOrLoginPlayer()` — replaces `LoadPlayerBalance()` + username entry
- Create `InsertGameRecord()` — replaces `WriteRecordToCSV()`
- Create `PrintQuerySummary()` — runs SQL SELECT queries at end of session
- Enforce `UNIQUE` constraint on `Username` in `Players` table
- Migrate `CheckDailyBonus()` to read from `Players.LastSeen` column
- Delete CSV dependency entirely
- Pre-seed database with 300-500 synthetic rows via Python script

---

*This document is updated at the end of each development phase.*  
*Next update: Component 2 Complete*
