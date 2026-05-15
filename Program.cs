using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SQLite;
using System.Runtime.InteropServices;
using System.Threading;



namespace AlexThomasBlackJackProject2026
{
    // ══════════════════════════════════════════════════════════════════
    // BLACKJACK ANALYTICS — C# Console Application
    // Author  : Alex Thomas
    // GitHub  : https://github.com/AlexChaseThomas/BlackjackAnalytics
    // Version : Phase 3 — 52-card deck, proper deal order, double down
    // ══════════════════════════════════════════════════════════════════
    //
    // DATA CLASSES (POCOs):
    //   PlayerInfo    — player identity (username only, no PII)
    //   Card          — single playing card with name and suit
    //   SessionRecord — one row of analytics data per hand
    //   GameStats     — session-level counters
    //
    // LOGIC CLASS:
    //   BlackjackGame — all methods and Main() entry point
    // ══════════════════════════════════════════════════════════════════

    // PlayerInfo = a data class that stores who the player is 
    public class PlayerInfo
    {
        public string Username; // unique identifier chosen by the player
        // no DOB, no real name - no PII stored anywhere in this class
    }

    // Card Class = represents a single playing card with a name and suit 
    // Phase 3: Card class replaces the separate Draw() and SuitAssigner() methods used in Phase 1 & 2
    // Why? - previously, Draw () would pick a random card from a 13-card name array with no memory of what was already drawn; meaning the same card could appear multiple times in one hand (impossible in a real 52-card deck)
    public class Card
    {
        public string Name; // "Ace", "King", "Queen" etc
        public string Suit; // "Hearts", "Diamonds", "Clubs", "Spades"

        // constructor - called with new Card("Ace", "Hearts")
        // sets both fields in one line when the card is created
        public Card(string name, string suit)
        {
            Name = name;
            Suit = suit;
        }

        // override ToString() so Console.WriteLine(card) prints "Ace of Hearts"
        // instead of the default class name
        public override string ToString()
        {
            return Name + " of " + Suit;
        }
    }

    // SessionRecord = a data class that stores what happened during a hand - it gets created fresh at the end of every single hand, filled in with that hand's results, written to the CSV, and then thrown away. Next hand, a new one is created
    public class SessionRecord
    {
        // basic session information fields
        public int SessionID;
        public string Username;   
        public int PlayerAge;  // calculated age only - full DOB is never stored
        public string LoginTime;
        public int GameNumber;
        public int PlayerTotal;
        public int DealerTotal;
        public string Result;
        public bool PlayerBusted;
        public bool DealerBusted;
        public int NumberOfDraws;

        // wagering fields - added for betting system
        // Together (3) these tell the story of each hand's financial outcome 
        public int BetAmount;    // how much the player wagered this hand 
        public int TokensBefore; // balance before the bet was placed 
        public int TokensAfter;  // balance after the hand resolved 
        public bool DoubledDown; // true if the player used double down this hand - tracked for analytics (i.e. did doubling down correlate with winning or losing?

        // strategy fields - added for basic strategy analytics 
        // StrategyMode = records whether suggestions were on or off for this hand 
        // OverrodeSuggestion = records whether the player ignored a warning this hand
        // Together (2) these help us compare decision quality and strategy groups
        public string StrategyMode;
        public bool OverrodeSuggestion;
    }

    public class GameStats
    {
        // Game results tracking 
        public int TotalGames = 0;
        public int PlayerWins = 0;
        public int DealerWins = 0;
        public int Ties = 0;
        public int PlayerBusts = 0;
        public int DealerBusts = 0;

        // strategy tracking - added for basic strategy analytics
        // StrategyModeOn = records whether this session used suggestions 
        // SuggestionsOverridden = records (counts) how many times the player ignored a warning 
        public bool StrategyModeOn = false;
        public int SuggestionsOverridden = 0;
    }
    class BlackjackGame // this type of class is only accessible within this file/namespace (default = "internal")
    {
        // Single shared Random instance for the entire class; declared at class level = all methods share it 
        // ***** BUG RISK ***** 
        // if two calls of the draw method happen close together in time, they can get the same seed and produce the same card twice in a row
        // one shared instance eliminates that problem entirely
        static Random rand = new Random();

        // DICTIONARY: cardValues = maps each card name to its point value 
        // Dictionary<string, int> = key is the card name (string), value is the point worth (int)
        // Similar to VLOOKUP in Excel or a JOIN in SQL 
        // give it a key ("Ace"), get back a value (11) instantly
        static Dictionary<string, int> cardValues = new Dictionary<string, int>()
        {
            { "Ace",   11 }, { "King",  10 }, { "Queen", 10 }, { "Jack", 10 },
            { "10",    10 }, { "9",      9 }, { "8",      8 }, { "7",     7 },
            { "6",      6 }, { "5",      5 }, { "4",      4 }, { "3",     3 },
            { "2",      2 }
        };


        // METHOD: BuildDeck = creates a full 52-card deck as a List of Card objects 
        // 13 card values * 4 suits = 52 unique cards 
        // called once to build, then ShuffleDeck() randomizes the order
        static List<Card> BuildDeck()
        {
            List<Card> deck = new List<Card>();

            string[] names = { "Ace", "King", "Queen", "Jack",
                                "10", "9", "8", "7",
                                "6", "5", "4", "3", "2" };

            string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };

            // nested loops = every name gets paired with every suit
            // 13 names x 4 suits = 52 cards total
            foreach (string name in names)
                foreach (string suit in suits)
                    deck.Add(new Card(name, suit));

            return deck;
        }   // closes BuildDeck

        // METHOD: ShuffleDeck = randomizes the order of cards in the deck using Fisher-Yates shuffle
        // Fisher-Yates = the standard, unbiased shuffle algorithim = works by moving backwards through the list and swapping each card with a randomly chosen card at or before its position
        // with a randomly chosen card at or before its position
        // result = every possible ordering of the deck is equally likely 
        static void ShuffleDeck(List<Card> deck)
        {
            // start at the last card and work backwards
            for (int i = deck.Count - 1; i > 0; i--)
            {
                // pick a random index from 0 to i (inclusive)
                int j = rand.Next(i + 1);

                // swap deck[i] and deck[j]
                Card temp = deck[i];
                deck[i] = deck[j];
                deck[j] = temp;
            }
            // after this loop every card is in a random position
            // no card appears twice - the same 52 cards just reordered
        }   // closes ShuffleDeck

        // METHOD: DealCard = takes the top card from the deck and removes it
        // if the deck runs low (i.e. fewer than 10 cards) it reshuffles automatically = prevents us from running out of cards mid-hand 
        static Card DealCard(List<Card> deck)
        {
            // reshuffle if deck is running low
            // 10 cards is a safe threshold - a hand can use at most ~8-9 cards
            if (deck.Count < 10)
            {
                // rebuild and reshuffle the full deck
                // in a real casino this would be a new shoe - here we just reset
                List<Card> freshDeck = BuildDeck();
                ShuffleDeck(freshDeck);
                deck.Clear();
                deck.AddRange(freshDeck);
                // AddRange() copies all cards from freshDeck into deck
            }

            // take the top card (index 0) and remove it from the deck
            Card card = deck[0];
            deck.RemoveAt(0);
            // RemoveAt(0) removes the first element and shifts everything down
            // this is how dealing from the top of a deck works
            return card;
        }   // closes DealCard

        // METHOD: CalculateBustChance = takes the player's current total, returns bust probability as a string 
        // Used by the strategy warning system to demonstrate informed risk to the player
        static string CalculateBustChance(int currentTotal)
        {
            // safeRoom = the highest card value that won't bust the player = any card with a value higher than safeRoom will cause a bust
            int safeRoom = 21 - currentTotal; // i.e. how many points away you are from busting

            // card values and how many of each exist in a STANDARD deck of 13 types 
            // 10 appears 4 times because Jack, Queen, King, and 10 all are valued at 10
            // every other value only appears once 
            int[] cardValues = { 11, 9, 8, 7, 6, 5, 4, 3, 10, 2 };
            int[] cardCounts = { 1, 1, 1, 1, 1, 1, 1, 1, 4, 1 };

            // These are parallel arrays - index 0 in cardValues matches index 0 in cardCounts
            // cardValues[0] = 11  →  cardCounts[0] = 1   (Ace, appears once)
            // cardValues[8] = 10  →  cardCounts[8] = 4   (10 / J / Q / K, appears four times)
            // cardValues[9] = 2   →  cardCounts[9] = 1   (2, appears once)
            // totalCards = 13 because if you add up all the counts: 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 4 + 1 = 13.
            // Ace=11 (x1), 9(x1), 8(x1), 7(x1), 6(x1), 5(x1), 4(x1), 3(x1), 10-value(x4), 2(x1)

            int totalCards = 13;
            int bustCards = 0;

            // COUNT HOW MANY CARDS WOULD BUST YOU 
            // each index passes through the for loop. Each pass through the loop, i is a different index (0,1,2,3...9) 
            // at each index, it asks: "Does this card value exceed my safe room?" - if yes, it adds that card's count to bustCards
            for (int i = 0; i < cardValues.Length; i++)
            {
                if (cardValues[i] > safeRoom)
                    bustCards += cardCounts[i];
            }

            double bustChance = (double)bustCards / totalCards * 100;
            // (double) cast prevents integer division losing the decimal

            return Math.Round(bustChance) + "%";
            // Math.Round() rounds to the nearest whole number 
            // returned as a string so it prints cleanly e.g. "77%"
        }   // closes CalculateBustChance

        // METHOD: Initialize Database = creates the SQLite database file and both tables on the first run 
        // IF NOT EXISTS means this is safe to call every time the program starts
        // if the tables already exist = nothing happens = no data lost
        static void InitializeDatabase(string dbPath)
        {
           // SQLiteConnection = the bridge between C# and the SQLite database file
           // "Data Source=" tells SQLite where the .db file lives 
           // using statement = connection closes automatically when the block finishes 
           using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open(); // establish the connection to the database = nothing can happen until this is called 

                // SQLiteCommand = a SQL statement that runs against the open connection.
                using (var cmd = new SQLiteCommand(connection))
                {
                    // CREATE TABLE: Players = stores identity & current token balance 
                    // Username = UNIQUE (database enforces no duplicates)
                    // REPLACES: CSV-based username 'trust' system from Phase 2

                    // @" = verbatim string literal = lets the string span multiple lines without needing special characters = formatting convenience that makes SQL readable
                    cmd.CommandText = @" 
                        CREATE TABLE IF NOT EXISTS Players (
                            PlayerID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username TEXT UNIQUE NOT NULL,
                            PlayerAge INTEGER, 
                            FirstSeen TEXT,
                            LastSeen TEXT,
                            TokenBalance INTEGER
                    )";
                    // normalization = the identity data (players) lives in one place and the hand data references it by username (GameSessions)
                    // AUTOINCREMENT = the database automatically assigns a unique number to each new row (PlayerID 1, PlayerID 2, ...etc)
                    cmd.ExecuteNonQuery();
                    // ExecutionNonQuery = runs an SQL statement that does not return rows = used for CREATE, INSERT, UPDATE, DELETE
                    // Returns = # of rows affected (ignore it here)

                    // CREATE TABLE: GameSessions = One Row Per Hand Played
                    // username = Foreign Key linking back to the Players table 
                    // NORMALIZED STRUCTURE 
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS GameSessions (
                            RecordID           INTEGER PRIMARY KEY AUTOINCREMENT, 
                            SessionID          INTEGER,
                            Username           TEXT    REFERENCES Players(Username),
                            PlayerAge          INTEGER,
                            LoginTime          TEXT,
                            GameNumber         INTEGER,
                            PlayerTotal        INTEGER,
                            DealerTotal        INTEGER,
                            Result             TEXT,
                            PlayerBusted       INTEGER,
                            DealerBusted       INTEGER,
                            NumberOfDraws      INTEGER,
                            BetAmount          INTEGER,
                            TokensBefore       INTEGER,
                            TokensAfter        INTEGER,
                            StrategyMode       TEXT,
                            OverrodeSuggestion INTEGER,
                            DoubledDown        INTEGER
                        )";
                    cmd.ExecuteNonQuery();
                    // PlayerBusted, DealerBusted, OverrodeSuggestion, DoubledDown = stored as INTEGER (0 or 1) because SQLite has no bool type
                    // 0 = false, 1 = true - we handle the conversion in C#


                }
            }
        } // closes InitializeDatabase

        // METHOD: LoadPlayerBalance
        // reads the CSV to find the last recorded token balance for this player
        // new players or first run ever returns 100 as the starting balance
        // this is how balance persists across sessions without a database
        // NOTE: column indexes - Username=1, PlayerAge=2, LoginTime=3, TokensAfter=13
        static int LoadPlayerBalance(string username, string csvPath)
        {
            // if the CSV doesn't exist yet = this is a brand new, first run 
            if (!File.Exists(csvPath))
                return 100; // NEW PLAYERS = start with 100 tokens 

            // File.ReadAllLines() reads the entire CSV into a string array
            // Each element = one row of the file 
            string[] lines = File.ReadAllLines(csvPath);

            // Need at least 2 lines = header row + at least one data row 
            if (lines.Length < 2)
                return 100;

            // loop backwards through the file from bottom to top = the last row matching this player = their most recent balance 
            for (int i = lines.Length - 1; i >= 1; i--)
            // i starts at last line, i >= 1 skips the header at index 0
            // i-- counts downwards toward the top of the file 
            {
                // .Split(',') breaks the row string into an array of field values 
                // splits wherever it finds a comma - same structure as the row that we wrote
                string[] fields = lines[i].Split(',');

                // fields[1] = Username column (second column, index 1)
                if (fields[1] == username)
                {
                    // fields[13] = TokensAfter column (14th column, index 13)
                    // int.TryParse safely converts the string to an int 
                    if (int.TryParse(fields[13], out int balance))
                        return balance;
                }
            }
            // username not found anywhere in the file = brand new player
            return 100;
        }   // closes LoadPlayerBalance

        // METHOD: CheckDailyBonus
        // The CSV already records LoginTime for every row. To check if 24 hours have passed, we read the player's most recent LoginTime from the CSV and compare it to right now. If the difference is 24 hours or more, they get the bonus.
        static int CheckDailyBonus(string username, string csvPath, int currentBalance, out double hoursUntilBonus)
        // 'out double hoursUntilBonus' = an output parameter
        // out = the method writes a value directly into this variable from the outside
        // same 'out' concept you already know from int.TryParse and DateTime.TryParse
        // this lets the method return TWO pieces of information at once:
        // the int return value = the (possibly updated) balance
        // the out parameter = hours remaining until next bonus (0 if bonus was awarded)
        {
            hoursUntilBonus = 0;
            // default value = 0 = either bonus was awarded or no data found
            // out parameters MUST be assigned before the method returns
            // C# requires this - it won't compile if any code path leaves it unset

            // if no CSV exists yet = first ever run = no bonus applicable 
            if (!File.Exists(csvPath))
                return currentBalance;

            string[] lines = File.ReadAllLines(csvPath);

            // need at least a header and one data row to find a login time 
            if (lines.Length < 2)
                return currentBalance;

            // loop backwards to find the most recent row for this player
            // same pattern as LoadPlayerBalance - backwards = most recent first 
            for (int i = lines.Length - 1; i >= 1; i--)
            {
                string[] fields = lines[i].Split(',');

                // fields[1] = Username column - check this row belongs to our player first
                if (fields[1] == username)
                {
                    // fields[3] = LoginTime column (fourth column, index 3)
                    // Username=1, PlayerAge=2, LoginTime=3 - same indexes as before
                    // DateTime.TryParse converts the stored string back into a DateTime
                    // same pattern as DOB validation 
                    if (DateTime.TryParse(fields[3], out DateTime lastLogin))
                    {
                        // DateTime.Now - lastLogin gives a TimeSpan
                        // TimeSpan = built-in C# type that represents a duration of time 
                        // .TotalHours gives the total number of hours in that duration as a double 
                        TimeSpan timeSinceLastLogin = DateTime.Now - lastLogin;

                        // **** subtracting two DateTimes ALWAYS produces a TimeSpan ****
                        // e.g. if now is 3pm and lastLogin was 1pm, timeSinceLastLogin = 2 hours

                        if (timeSinceLastLogin.TotalHours >= 24)
                        {
                            // 24 hours have passed - award the bonus
                            int newBalance = currentBalance + 50;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("╔══════════════════════════════════════╗");
                            Console.WriteLine("║       🎁  DAILY BONUS AWARDED!        ║");
                            Console.WriteLine("║    +50 tokens added to your balance  ║");
                            Console.WriteLine("╚══════════════════════════════════════╝");
                            Console.WriteLine("Previous balance : " + currentBalance + " tokens");
                            Console.WriteLine("New balance      : " + newBalance + " tokens\n");
                            Console.ResetColor();
                            return newBalance;
                            // return the updated balance with bonus applied
                        }
                        else
                        {
                            // less than 24 hours have passed = no bonus yet 
                            // calculate how long until they can get the next one
                            hoursUntilBonus = 24 - timeSinceLastLogin.TotalHours;
                            // write the hours remaining into the out parameter
                            // Main() can now read this value after calling the method

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Daily bonus available in: "
                                   + Math.Round(hoursUntilBonus, 1) + " hours.\n");
                            // Math.Round(value, 1) rounds to 1 decimal place
                            // e.g. 3.7333 hours becomes "3.7 hours"
                            Console.ResetColor();
                            return currentBalance;
                            // return balance unchanged - no bonus yet
                        }
                    }

                    // JUST IN CASE
                    // if we found the player row but could not parse the date (i.e. the date in csv was invalid)
                    return currentBalance;
                }
            }

            // player not found in CSV at all = new player = no bonus applicable
            return currentBalance;
        }   // closes CheckDailyBonus

        // METHOD: DetermineWinner
        // takes the player's final total and the dealer's final total = returns the result as a plain string: "Win", "Loss", or "Tie"
        // extracted from Main() so the logic all lives in one place = DRY principle 
        // Win/Loss Changes = only need to be changed once here 
        // Order Matters: most specific cases come first so that they aren't missed by more general conditions being placed at the start 
        static string DetermineWinner(int playerTotal, int dealerTotal)
        {
            if (playerTotal == 21 && dealerTotal == 21) return "Tie";
            // both hit 21 = push - must come before the individual 21 checks below
            // without this the player 21 check would fire first and incorrectly return Win

            if (playerTotal > 21 && dealerTotal > 21) return "Tie";
            // both bust = tie - most specific case, must come first
            // if this wasn't close to the top, the general bust checks below would catch it incorrectly

            if (playerTotal == 21) return "Win";
            // player hit exactly 21 - Blackjack

            if (dealerTotal == 21) return "Loss";
            // dealer hit exactly 21 - dealer Blackjack

            if (playerTotal > 21) return "Loss";
            // player busted

            if (dealerTotal > 21) return "Win";
            // dealer busted

            if (playerTotal > dealerTotal) return "Win";
            // neither busted, player has higher total

            if (dealerTotal > playerTotal) return "Loss";
            // neither busted, dealer has higher total

            return "Tie";
            // final case = no condition needed = only possibility left is equal totals
        }   // closes DetermineWinner

        // METHOD: WriteRecordToCSV
        static void WriteRecordToCSV(SessionRecord record, string csvPath)
        {
            bool fileExists = File.Exists(csvPath);
            // File.Exists() checks whether the CSV already exists
            // returns true or false - same bool pattern you already know

            using (StreamWriter writer = new StreamWriter(csvPath, true))
            // StreamWriter writes text to a file line by line
            // 'true' = append mode - each new row adds to the bottom
            // 'using' statement = automatically closes and saves the file
            // when the block finishes, even if something goes wrong
            {
                if (!fileExists)
                // write the header row on the very first run only
                // after that fileExists = true and this is skipped forever
                {
                    writer.WriteLine(
                        "SessionID,Username,PlayerAge,LoginTime," +
                        "GameNumber,PlayerTotal,DealerTotal," +
                        "Result,PlayerBusted,DealerBusted,NumberOfDraws," +
                        "BetAmount,TokensBefore,TokensAfter," +
                        "StrategyMode,OverrodeSuggestion,DoubledDown"
                    // ***** order must match the data row below - EXACTLY *****
                    );
                }

                // write the data row
                // each field separated by a comma = CSV (comma separated values)
                // this is all a CSV file is - plain text with commas between values
                writer.WriteLine(
                    record.SessionID + "," +
                    record.Username + "," +
                    record.PlayerAge + "," +
                    record.LoginTime + "," +
                    record.GameNumber + "," +
                    record.PlayerTotal + "," +
                    record.DealerTotal + "," +
                    record.Result + "," +
                    record.PlayerBusted + "," +
                    record.DealerBusted + "," +
                    record.NumberOfDraws + "," +
                    record.BetAmount + "," +
                    record.TokensBefore + "," +
                    record.TokensAfter + "," +
                    record.StrategyMode + "," +
                    record.OverrodeSuggestion + "," +
                    record.DoubledDown
                );
            }   // StreamWriter closes and saves automatically here
        }   // closes WriteRecordToCSV

        static void Main() // This is the entry point of every C# program, when you run your program, C# scans your code looking specifically for a method called Main (C# STARTS EXECUTING HERE)
        {
            // STEP 1 = WELCOME SCREEN
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║      C# BLACKJACK ANALYTICS          ║");
            Console.WriteLine("║         Alex Thomas  2026            ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.ResetColor();

            // STEP 2 = USERNAME ENTRY
            // username is the player's unique identifier throughout the system
            // if the username exists in the CSV = returning player, balance loaded
            // if the username doesn't exist = new player, starts with 100 tokens
            // no passwords collected - username alone identifies the player
            // no real names or dates of birth stored - no PII stored anywhere
            PlayerInfo player = new PlayerInfo();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Enter your username: ");
            Console.ResetColor();
            player.Username = Console.ReadLine().Trim().ToLower();
            // .ToLower() converts the username to lowercase
            // this means "AlexT" and "alext" are treated as the same username
            // prevents duplicate accounts from different capitalizations

            // username validation - must be between 3 and 20 characters
            while (player.Username.Length < 3 || player.Username.Length > 20)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Username must be between 3 and 20 characters. Try again.");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Enter your username: ");
                Console.ResetColor();
                player.Username = Console.ReadLine().Trim().ToLower();
            }

            // NOTE: username uniqueness is not enforced in Phase 2 (CSV version)
            // two players could theoretically register the same username
            // this will be fixed in Phase 3 when SQLite is integrated
            // the Players table will have a UNIQUE constraint on the Username column
            // which means the database itself will reject duplicate usernames at the INSERT level
            // for now the CSV version operates on trust - whoever types a username gets that balance

            // STEP 3 = AGE VERIFICATION
            // player enters their full date of birth for verification purposes only
            // the DOB is used to calculate their exact age, then immediately discarded
            // only the calculated age integer is stored in the session data
            // this means the data cannot be reverse-engineered back to a specific person
            // full DOB = PII (personally identifiable information) - we never store it
            // age alone = not PII - cannot identify a specific individual
            bool validDate = false;
            int playerAge = 0;
            // playerAge stores the calculated age after verification
            // this is what gets written to SessionRecord, not the DOB itself

            while (!validDate)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("(21+) Enter your date of birth (MM/DD/YYYY): ");
                Console.ResetColor();
                string dobInput = Console.ReadLine().Trim();

                // DateTime.TryParse converts the string to a DateTime object
                // if the user types "abc" or "13/45/2000" it won't crash - just returns false
                // Phase 2: Security Update - only use player DOB to check age is over 21
                // we no longer store the full date data, only their calculated age
                // 'out DateTime dob' writes the result into a temporary variable
                // we use a local variable here - NOT stored in PlayerInfo
                // the DOB exists only for the duration of this while loop
                if (!DateTime.TryParse(dobInput, out DateTime dob))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Something is not right. Please use MM/DD/YYYY format and try again.");
                    Console.ResetColor();
                    continue;
                }

                // reject future dates
                if (dob > DateTime.Today)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Date of birth cannot be in the future.");
                    Console.ResetColor();
                    continue;
                }

                // calculate exact age from the DOB
                // same logic as the old CalculateAge method - now inline since DOB is temporary
                playerAge = DateTime.Today.Year - dob.Year;
                if (dob.Month > DateTime.Today.Month ||
                    dob.Month == DateTime.Today.Month && dob.Day > DateTime.Today.Day)
                {
                    playerAge--;
                    // birthday hasn't happened yet this year - subtract 1 to correct
                }

                // reject ages over 120 - almost certainly a data entry error
                if (playerAge > 120)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Please enter a valid date of birth.");
                    Console.ResetColor();
                    continue;
                }

                // enforce 21+ age restriction
                if (playerAge < 21)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("You must be 21 or older to play. You are " + playerAge + ".");
                    Console.ResetColor();
                    continue;
                }

                // DOB verified and age calculated
                // validDate flips to true - loop exits
                // dob goes out of scope here and is discarded - never stored anywhere
                validDate = true;
            }
            // at this point playerAge holds the verified age as a plain integer
            // the full date of birth no longer exists anywhere in the program

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nAge verified! Welcome, " + player.Username + "!\n");
            Console.ResetColor();

            // STEP 4 = SESSION SETUP 

            // these variables will belong to the entire session, not any one hand 
            // these variables get created once here and are referenced throughout every hand below 

            // DateTime.Now = the exact current date AND time including hours, minutes, seconds
            // .Ticks = a property on DateTime that expresses the current moment as a very large integer 
            // Every tick = one ten-millionth of a second, so no two sessions ever share the same value 
            // % 1000000 = the modulo operator - gives you the remainder after dividing by 1000000
            // this trims the very large Ticks number down to a readable 6 digit number
            // example: 18374628390000000 % 1000000 = 390000 (just the last 6 digits)
            int sessionID = (int)(DateTime.Now.Ticks % 1000000);

            // DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") converts the DateTime object into text 
            // yyyy = 4 digit year, MM = 2 digit month, dd = 2 digit day 
            // HH = 24 hour clock hour, mm = minutes, ss = seconds 
            // example output: "2026-05-05 14:32:01"
            string loginTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // AppDomain.CurrentDomain.BaseDirectory = the folder the program is currently running from 
            // Path.Combine() joins the folder path and filename together safely - handles the backslash between them automatically
            // ***** CSV will always appear right next to the .exe file *****
            string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blackjack_sessions.csv");

            // database path - lives next to the .exe just like the CSV did
            // Phase 3: SQLite replaces CSV as the primary data store
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blackjack.db");

            // initialize the database - creates file and tables if they don't exist
            // safe to call every run - IF NOT EXISTS prevents overwriting existing data
            InitializeDatabase(dbPath);

            // create one GameStats instance for the whole session
            // every hand will update these counters + then they get printed at the end 
            GameStats stats = new GameStats();

            // LoadPlayerBalance reads the CSV for this player's last recorded TokensAfter
            // if no record exists = returns 100 as starting balance
            // player.Username is the identifier - same username = same player history loaded
            int tokenBalance = LoadPlayerBalance(player.Username, csvPath);

            tokenBalance = CheckDailyBonus(player.Username, csvPath, tokenBalance, out double hoursUntilBonus);
            // 'out double hoursUntilBonus' declares the variable AND receives the value in one line
            // same pattern as 'out int balance' in LoadPlayerBalance
            // after this line, hoursUntilBonus holds either 0 (bonus awarded or new player)
            // or the decimal hours remaining until their next bonus

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Session ID           : " + sessionID);
            Console.WriteLine("Session started      : " + loginTime);
            Console.WriteLine("Data saving to       : " + csvPath + "\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Token balance        : " + tokenBalance + " tokens\n");
            Console.ResetColor();

            // GUARD CLAUSE (i.e. if they are already at 0 tokens from a previous session, exit now) 
            if (tokenBalance < 5)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Not enough tokens to play.");
                if (hoursUntilBonus > 0)
                {
                    Console.WriteLine("Come back in " + Math.Round(hoursUntilBonus, 1) + " hours for your daily bonus.");
                    // only shows the bonus message if there is actually a bonus countdown running
                    // hoursUntilBonus = 0 means either they are a new player or the bonus was just awarded
                    // in both those cases the balance would be 100 so they wouldn't hit this block anyway
                }
                Console.WriteLine("Thanks for playing.");
                Console.ResetColor();
                Console.ReadKey();
                return;
                // same guard clause pattern as the old password gate
                // return inside Main() exits the entire program immediately
            }

            // STRATEGY MODE SELECTION
            // presented to the player once at the start of each session
            // their choice is recorded in every SessionRecord for this session
            // this lets us compare decision quality between strategy groups in analytics
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║       BASIC STRATEGY MODE            ║");
            Console.WriteLine("╠══════════════════════════════════════╣");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("║  ON  = suggestions shown during play ║");
            Console.WriteLine("║  OFF = no suggestions                ║");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Press S for suggestions ON, any other key for OFF.");
            Console.ResetColor();

            ConsoleKeyInfo strategyKey = Console.ReadKey(true);
            bool strategyOn = strategyKey.Key == ConsoleKey.S;
            // strategyOn = true if they pressed S, false for anything else
            // this single bool controls all strategy logic for the whole session

            stats.StrategyModeOn = strategyOn;
            // store the choice in GameStats so it appears in the end of session summary

            Console.ForegroundColor = strategyOn ? ConsoleColor.Green : ConsoleColor.Yellow;
            // ternary operator - if strategyOn is true use Green, otherwise Yellow
            // condition ? valueIfTrue : valueIfFalse
            Console.WriteLine(strategyOn
                ? "Basic strategy suggestions ON.\n"
                : "Basic strategy suggestions OFF.\n");
            Console.ResetColor();

            // STEP 5 = BUILD AND SHUFFLE THE DECK
            // BuildDeck() creates all 52 cards - 13 values x 4 suits
            // ShuffleDeck() randomizes the order using Fisher-Yates algorithm
            // the deck is built once per session and dealt from throughout
            // DealCard() automatically reshuffles when the deck runs low

            List<Card> deck = BuildDeck();
            ShuffleDeck(deck);

            // STEP 6 = SESSION LOOP
            // outer loop = keeps the session alive across multiple hands
            // sessionActive starts as true so the loop begins immediately 
            // only flips to false when the player escapes or runs out of tokens
            bool sessionActive = true;

            while (sessionActive)
            {
                // these variables track ONE hand at a time 
                // declared INSIDE the session loop so that they reset to zero every new hand 
                // ***** if declared outside they would carry over from the previous hand *****

                int playerTotal = 0;
                int dealerTotal = 0;
                int numberOfDraws = 0;
                bool gameOver = false;
                bool overrodeSuggestion = false;
                int playerAces = 0;

                // overrodeSuggestion declared here so it is accessible both in the draw branch where it gets set
                // AND outside the draw branch where it gets written to the SessionRecord

                // playerAces tracks how many Aces in the player's hand are currently counted as 11
                // used for soft Ace handling - if the player draws and busts but has a soft Ace,
                // the Ace drops from 11 to 1 instead of causing an immediate bust

                stats.TotalGames++;
                // incremented here so the game number is correct from the first hand

                // BETTING PROMPT = player must bet BEFORE seeing their cards
                // minimum of 5 tokens, maximum of 100 tokens, cannot exceed their balance
                int currentBet = 0;
                bool validBet = false;

                while (!validBet)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("Tokens: " + tokenBalance);
                    Console.Write("Place your bet (min 5, max " + Math.Min(100, tokenBalance) + "): ");
                    Console.ResetColor();

                    string betInput = Console.ReadLine().Trim();

                    // allow the player to type "exit" to leave instead of placing a bet
                    // true ESC during ReadLine() isn't possible in a console app without
                    // switching to ReadKey - this is the simplest workaround for now
                    
                    if (betInput.ToLower() == "exit")
                    {
                        sessionActive = false;
                        break;
                        // break exits the betting while loop
                        // sessionActive = false exits the session loop on the next iteration
                    }

                    if (!int.TryParse(betInput, out currentBet))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Please enter a whole number.");
                        Console.ResetColor();
                        continue;
                    }

                    int maxBet = Math.Min(100, tokenBalance);

                    if (currentBet < 5 || currentBet > maxBet)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Bet must be between 5 and " + maxBet + ".");
                        Console.ResetColor();
                        continue;
                    }

                    validBet = true;
                }

                // if the player typed "exit" during betting, stop here
                // do not deal cards or enter the game loop
                if (!sessionActive) break;

                int tokensBefore = tokenBalance;
                // snapshot of the balance BEFORE this hand starts
                // stored in SessionRecord so we can see exactly what each hand lost or gained

                // DEAL OPENING HANDS
                // in real blackjack both player and dealer receive two cards before any decisions
                // player gets both cards face up - they can see their full starting total
                // dealer gets one card face up (visible) and one face down (hole card - hidden)
                // the hole card is stored but NOT shown until after the player finishes their turn
                // this is critical for realistic gameplay - the player makes decisions based on
                // their own hand and only ONE dealer card, not the dealer's full total

                // PLAYER'S TWO STARTING CARDS
                Card openCard1 = DealCard(deck);
                Card openCard2 = DealCard(deck);

                int openValue1 = cardValues[openCard1.Name];
                int openValue2 = cardValues[openCard2.Name];

                if (openCard1.Name == "Ace") playerAces++;
                if (openCard2.Name == "Ace") playerAces++;

                playerTotal = openValue1 + openValue2;

                // SOFT ACE ADJUSTMENT FOR OPENING HAND
                while (playerTotal > 21 && playerAces > 0)
                {
                    playerTotal -= 10;
                    playerAces--;
                }

                // DEALER'S TWO STARTING CARDS
                Card dealerVisibleCard = DealCard(deck);
                Card dealerHoleCard = DealCard(deck);

                int dealerVisibleValue = cardValues[dealerVisibleCard.Name];
                int dealerHoleValue = cardValues[dealerHoleCard.Name];

                int dealerAcesStart = 0;
                if (dealerVisibleCard.Name == "Ace") dealerAcesStart++;
                if (dealerHoleCard.Name == "Ace") dealerAcesStart++;

                dealerTotal = dealerVisibleValue + dealerHoleValue;

                while (dealerTotal > 21 && dealerAcesStart > 0)
                {
                    dealerTotal -= 10;
                    dealerAcesStart--;
                }

                // display game header THEN opening hands
                // player sees their two cards and the dealer's one visible card
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╔══════════════════════════════════════╗");
                Console.WriteLine("║  GAME #" + stats.TotalGames.ToString().PadRight(30) + "║");
                Console.WriteLine("╠══════════════════════════════════════╣");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("║  [ENTER] Hit                         ║");
                Console.WriteLine("║  [N]     Stand                       ║");
                Console.WriteLine("║  [D]     Double Down                 ║");
                Console.WriteLine("║  [ESC]   Quit + Forfeit Bet          ║");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╚══════════════════════════════════════╝");
                Console.ResetColor();

                // show dealer's visible card only - hole card stays hidden
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Dealer showing: " + dealerVisibleCard);
                Console.WriteLine("Dealer hole card: [hidden]\n");
                Console.ResetColor();

                // show player's two starting cards
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("You were dealt: " + openCard1 + " and " + openCard2);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Your total:     " + playerTotal + "\n");
                Console.ResetColor();

                // check if player got blackjack on the opening deal
                // if so we skip the game loop entirely and go straight to resolution
                // this requires handling the result here rather than inside the game loop
                if (playerTotal == 21)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("BLACKJACK! You hit 21 on the deal!\n");
                    Console.ResetColor();

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("\n── Dealer's turn ──────────────────────");
                    Console.ResetColor();

                    while (Console.KeyAvailable) Console.ReadKey(true);

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("Dealer revealing...");
                    Thread.Sleep(1500);
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write("                                        ");
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.WriteLine("Dealer reveals hole card: " + dealerHoleCard);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Dealer total: " + dealerTotal + "\n");
                    Console.ResetColor();

                    // dealer must still draw to 17 even when player has Blackjack
                    // the dealer does not concede early - house rules require drawing to 17
                    // if dealer also reaches 21 it becomes a tie, not a player win
                    int openingDealerAces = dealerAcesStart;
                    while (dealerTotal < 17)
                    {
                        Card dealerCard = DealCard(deck);
                        int dealerCardValue = cardValues[dealerCard.Name];
                        if (dealerCard.Name == "Ace") openingDealerAces++;

                        dealerTotal += dealerCardValue;

                        // soft Ace adjustment
                        while (dealerTotal > 21 && openingDealerAces > 0)
                        {
                            dealerTotal -= 10;
                            openingDealerAces--;
                        }

                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("Dealer drew:  " + dealerCard);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Dealer total: " + dealerTotal + "\n");
                        Console.ResetColor();
                    }

                    // determine result after dealer has finished drawing
                    string openingResult = DetermineWinner(playerTotal, dealerTotal);

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(player.Username + "'s total: " + playerTotal);
                    Console.WriteLine("Dealer's total:  " + dealerTotal);

                    if (openingResult == "Win")
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\n★  GAME OVER: " + openingResult + "  ★\n");
                        tokenBalance += currentBet;
                        Console.WriteLine("You won " + currentBet + " tokens! Balance: " + tokenBalance);
                    }
                    else
                    {
                        // tie - both player and dealer hit 21 on the deal
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\n★  GAME OVER: " + openingResult + "  ★\n");
                        Console.WriteLine("Tie — bet returned. Balance: " + tokenBalance);
                    }
                    Console.ResetColor();

                    // update stats
                    if (openingResult == "Win") { stats.PlayerWins++; }
                    else { stats.Ties++; }

                    // write the session record
                    SessionRecord openingRecord = new SessionRecord();
                    openingRecord.SessionID = sessionID;
                    openingRecord.Username = player.Username;
                    openingRecord.PlayerAge = playerAge;
                    openingRecord.LoginTime = loginTime;
                    openingRecord.GameNumber = stats.TotalGames;
                    openingRecord.PlayerTotal = playerTotal;
                    openingRecord.DealerTotal = dealerTotal;
                    openingRecord.Result = openingResult;
                    openingRecord.PlayerBusted = false;
                    openingRecord.DealerBusted = false;
                    openingRecord.NumberOfDraws = 0;
                    openingRecord.BetAmount = currentBet;
                    openingRecord.TokensBefore = tokensBefore;
                    openingRecord.TokensAfter = tokenBalance;
                    openingRecord.StrategyMode = strategyOn ? "On" : "Off";
                    openingRecord.OverrodeSuggestion = false;
                    openingRecord.DoubledDown = false;

                    WriteRecordToCSV(openingRecord, csvPath);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Hand saved.\n");
                    Console.ResetColor();

                    // check tokens before showing play again prompt
                    if (tokenBalance < 5)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\nNot enough tokens to continue. Game over.");
                        Console.ResetColor();
                        sessionActive = false;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("─────────────────────────────────────");
                        Console.WriteLine("Place a bet to continue, or type 'exit' to see your session summary.");
                        Console.ResetColor();
                    }

                    // skip the game loop entirely - hand is already resolved
                    gameOver = true;
                }

                // STRATEGY WARNING: HIGH OPENING HAND
                // if strategy mode is on and the player's opening two cards total 17 or higher,
                // warn them before the game loop starts - they haven't drawn yet but they should
                // know that hitting from this total carries significant bust risk
                // same bust percentage logic as the mid-hand warning

                if (strategyOn && playerTotal >= 17 && !gameOver)
                {
                    string bustChance = CalculateBustChance(playerTotal);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠  Strategy tip: your opening total is " + playerTotal + ".");
                    Console.WriteLine("   Drawing now carries a " + bustChance + " chance of busting.\n");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("   [ENTER] Hit anyway  [N] Stand  [D] Double Down  [ESC] Quit");
                    Console.ResetColor();
                }

                numberOfDraws = 0;
                // numberOfDraws starts at 0 AFTER the opening deal
                // the two starting cards are not counted as draws
                // draws = additional cards the player chose to take beyond the opening hand
                // this makes the analytics more meaningful - 0 draws = player stood on opening hand

                bool doubledDown = false;
                // doubledDown tracks whether the player used double down this hand
                // declared here so it is accessible both in the game loop and in the SessionRecord
                Console.ResetColor();

                // STEP 7 = GAME LOOP (inner loop - one hand) 
                // Lives INSIDE the session loop
                // Session loop = controls how many hands are played
                // Game Loop = controls what happens during a single hand
                // it keeps running until gameOver flips to true
                while (!gameOver)
                {
                    ConsoleKeyInfo keypress = Console.ReadKey(true);

                    // Console.ReadKey() captures a single keypress immediately - no Enter 
                    // 'true' = intercept mode = the key does NOT print to the screen 
                    // without 'true' = a character appears every time the user presses a key 

                    // ConsoleKeyInfo = built-in C# type that holds information about a keypress 
                    // .Key gives you back a ConsoleKey value 

                    // ConsoleKey = an enum = a special type representing a fixed set of named values
                    // instead of remembering that Escape = key code 27, you write ConsoleKey.Escape

                    string input = ""; // input starts as empty string
                    if (keypress.Key == ConsoleKey.Escape) input = "QUIT";
                    else if (keypress.Key == ConsoleKey.N) input = "N";
                    else if (keypress.Key == ConsoleKey.D && numberOfDraws == 0 && !doubledDown)
                        input = "DOUBLE";
                    // D key = double down
                    // only available on the first decision (numberOfDraws == 0)
                    // meaning the player has their two opening cards and hasn't drawn yet
                    // !doubledDown prevents doubling twice
                    // if numberOfDraws > 0 the D key falls through to the draw branch

                    // Escape = quit the session entirely 
                    // N = stand, end this hand only
                    // anything else (including Enter) = draw (empty string falls to else branch)

                    if (input == "QUIT")
                    {
                        // player pressed Escape mid-hand 
                        // WARNING + ASK FOR CONFIRMATION (before forfeiting) 
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\nYou have " + currentBet + " tokens on the line.");
                        Console.WriteLine("Press ESC again to forfeit your bet and quit.");
                        Console.WriteLine("Press any other key to keep playing.");
                        Console.ResetColor();

                        ConsoleKeyInfo confirm = Console.ReadKey(true);
                        // second ReadKey() - waits for confirmation keypress

                        if (confirm.Key == ConsoleKey.Escape)
                        {
                            // confirmed - forfeit the bet and exit both loops 
                            tokenBalance -= currentBet;
                            // -= subtracts the forfeited bet from the balance 
                            // same as: tokenBalance = tokenBalance - currentBet

                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Bet forfeited. Balance: " + tokenBalance + " tokens.");
                            Console.ResetColor();

                            // write a forfeit record so the CSV stays complete
                            // a missing row would make LoadPlayerBalance wrong next session
                            // because it reads TokensAfter from the last row to restore balance
                            SessionRecord forfeitRecord = new SessionRecord();
                            forfeitRecord.SessionID = sessionID;
                            forfeitRecord.Username = player.Username;
                            forfeitRecord.PlayerAge = playerAge;
                            // playerAge = calculated at login, DOB was discarded
                            forfeitRecord.LoginTime = loginTime;
                            forfeitRecord.GameNumber = stats.TotalGames;
                            forfeitRecord.PlayerTotal = playerTotal;
                            forfeitRecord.DealerTotal = dealerTotal;
                            forfeitRecord.Result = "Forfeit";
                            forfeitRecord.PlayerBusted = false;
                            forfeitRecord.DealerBusted = false;
                            forfeitRecord.NumberOfDraws = numberOfDraws;
                            forfeitRecord.BetAmount = currentBet;
                            forfeitRecord.TokensBefore = tokensBefore;
                            forfeitRecord.TokensAfter = tokenBalance;
                            forfeitRecord.StrategyMode = strategyOn ? "On" : "Off";
                            forfeitRecord.OverrodeSuggestion = overrodeSuggestion;
                            forfeitRecord.DoubledDown = doubledDown;

                            WriteRecordToCSV(forfeitRecord, csvPath);

                            gameOver = true;
                            sessionActive = false;
                            // gameOver exits the inner loop
                            // sessionActive = false exits the outer loop
                        }
                        // if they pressed anything other than Escape = do nothing = inner loop continues and they keep playing
                    }
                    else if (input == "N")
                    {
                        // STRATEGY WARNING: LOW STAND 
                        // only triggers if strategy mode is activated AND total is 11 or lower
                        // standing on 11 or less is statistically very weak
                        // player gets informed but not blocked - informational only
                        if (strategyOn && playerTotal <= 11)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("⚠  Strategy tip: you are standing on " + playerTotal + ".");
                            Console.WriteLine("   Standing this low gives the dealer a strong advantage.");
                            Console.ResetColor();
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("   [ENTER] Draw instead  [N] Stand anyway  [ESC] Quit");
                            Console.ResetColor();
                        }

                        // player chose to stand - end this hand only
                        // sessionActive stays true so the play again prompt still runs
                        gameOver = true;
                    }
                    else if (input == "DOUBLE")
                    {
                        // DOUBLE DOWN
                        // player doubles their bet and receives exactly one more card
                        // hand ends automatically after that card - no further drawing
                        // only available on opening two cards before any additional draws

                        if (currentBet * 2 > tokenBalance)
                        {
                            // player doesn't have enough tokens to double
                            // show message and let them choose hit or stand instead
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Not enough tokens to double down. Current bet: " +
                                              currentBet + ", Balance: " + tokenBalance);
                            Console.ResetColor();
                            // do not set gameOver - loop continues, player picks another action
                        }
                        else
                        {
                            // valid double down
                            currentBet *= 2;
                            // *= multiplies currentBet by 2
                            // shorthand for currentBet = currentBet * 2
                            doubledDown = true;

                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine("DOUBLE DOWN! Bet doubled to " + currentBet + " tokens.");
                            Console.ResetColor();

                            // deal exactly one more card
                            Card doubleCard = DealCard(deck);
                            int doubleValue = cardValues[doubleCard.Name];
                            if (doubleCard.Name == "Ace") playerAces++;

                            playerTotal += doubleValue;
                            // add the double down card value to the running total

                            // soft Ace adjustment
                            while (playerTotal > 21 && playerAces > 0)
                            {
                                playerTotal -= 10;
                                playerAces--;
                            }

                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("You drew:   " + doubleCard);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Your total: " + playerTotal + "\n");
                            Console.ResetColor();

                            // hand ends automatically after double down
                            gameOver = true;
                        }
                    }
                    else {
                        // player pressed Enter - draw one card for the player only
                        // dealer does NOT draw here - dealer draws after player stands
                        // this matches real blackjack dealer rules

                        numberOfDraws++;

                        // PLAYER DRAWS

                        Card playerCard = DealCard(deck);
                        int playerCardValue = cardValues[playerCard.Name];
                        // track Aces separately for soft Ace handling
                        if (playerCard.Name == "Ace") playerAces++;

                        playerTotal += playerCardValue;
                        // += adds the card value to the running total
                        // shorthand for playerTotal = playerTotal + playerCardValue
                        // accumulates each pass through the loop from its starting value of 0

                        // SOFT ACE ADJUSTMENT FOR PLAYER
                        // if drawing this card would bust the player AND they have a soft Ace,
                        // drop one Ace from 11 to 1 by subtracting 10
                        // this prevents an unfair bust when an Ace can legally be worth 1
                        while (playerTotal > 21 && playerAces > 0)
                        {
                            playerTotal -= 10;
                            // subtract 10 = convert one Ace from 11 to 1
                            playerAces--;
                            // one fewer Ace is being counted as 11
                        }

                        // print the card and total ONCE right after the draw
                        // the duplicate in the original was caused by printing here AND again after the strategy warning
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("You drew:     " + playerCard);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Your total:   " + playerTotal + "\n");
                        Console.ResetColor();

                        // check automatic ending conditions BEFORE showing strategy warning
                        // if the hand is already over there is no point warning about the next draw
                        if (playerTotal == 21)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("BLACKJACK! You hit 21.\n");
                            Console.ResetColor();
                            gameOver = true;
                        }
                        else if (playerTotal > 21)
                        {
                            // player busted - hand ends immediately
                            // no strategy warning needed since no further draw is possible
                            gameOver = true;
                        }

                        // STRATEGY WARNING: HIGH DRAW
                        // only triggers if strategy mode is on AND total is 17 or higher
                        // AND the hand isn't already over (hitting 21 or busting sets gameOver = true above)
                        // shows bust percentage - informational only, no gate
                        // warning appears AFTER the player sees the card they actually drew
                        // so they understand the consequence of drawing again from their current total
                        if (strategyOn && playerTotal >= 17 && !gameOver)
                        {
                            string bustChance = CalculateBustChance(playerTotal);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("⚠  Strategy tip: your total is " + playerTotal + ".");
                            Console.WriteLine("   Drawing now carries a " + bustChance + " chance of busting.");
                            Console.ResetColor();
                            // reprint compact controls so player knows exactly what to press next
                            // pressing Enter here will draw, N will stand, ESC will quit
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("   [ENTER] Draw anyway  [N] Stand  [ESC] Quit");
                            Console.ResetColor();
                            // do not need to call ReadKey here - the game loop already reads the next keypress
                            // the warning is purely informational, control returns to the game loop
                            // player presses any key to acknowledge and move on
                            overrodeSuggestion = true;
                            // true = a warning was shown this hand
                            // written to SessionRecord so we can analyze
                            // whether warned players busted more or less
                            stats.SuggestionsOverridden++;
                        }

                    }   // closes else (draw branch)

                    // STEP 8 = RESOLVE THE HAND
                    // only runs when gameOver is true AND session is still active
                    // the forfeit path already wrote its record and set sessionActive = false
                    // so this block correctly skips on forfeit

                    if (gameOver && sessionActive)
                    {

                        // DEALER DRAWING PHASE

                        // reveal the hole card before dealer draws
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("\n── Dealer's turn ──────────────────────");
                        Console.ResetColor();

                        // flush any buffered keypresses before the pause
                        // without this a buffered N or Enter from the player's last action
                        // gets consumed immediately and the pause fires too fast to see
                        while (Console.KeyAvailable) Console.ReadKey(true);

                        // prints the suspense line
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("Dealer revealing hole card...");

                        // pause for dramatic effect
                        Thread.Sleep(1500);

                        // move cursor back to the start of this line
                        Console.SetCursorPosition(0, Console.CursorTop);

                        // overwrite with blank spaces to clear the old text
                        // 40 spaces covers the full width of our UI
                        Console.Write("                                        ");

                        // move cursor back to start of line again
                        Console.SetCursorPosition(0, Console.CursorTop);

                        // now print the actual card reveal
                        Console.WriteLine("Dealer reveals hole card: " + dealerHoleCard);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Dealer total: " + dealerTotal + "\n");
                        Console.ResetColor();


                        // dealer draws only if player did not already bust
                        // result is already determined on a bust - no need to draw
                        if (playerTotal <= 21)
                        // only run dealer logic if the player didn't already bust
                        // if player busted, dealer wins regardless - no need to draw
                        {
                          

                            // dealerAces tracks how many Aces are currently being counted as 11
                            // this allows soft Ace handling - if the dealer busts and has an Ace
                            // counted as 11, it drops to 1 instead (subtract 10 from total)
                            // example: Ace + Ace = 11 + 11 = 22, drops to 11 + 1 = 12, keeps drawing
                            // example: Ace + 6 = 17, stands (soft 17 rule - stands on all 17s here)

                            // dealerAcesStart was calculated when the opening hands were dealt
                            // it already accounts for both the visible card and the hole card
                            // we use it as the starting Ace count for the dealer draw phase
                            int dealerAces = dealerAcesStart;



                            // dealer already has one visible card from the start of the hand
                            // dealer now continues drawing until reaching 17 or higher
                            while (dealerTotal < 17)
                            {
                                Card dealerCard = DealCard(deck);
                                int dealerCardValue = cardValues[dealerCard.Name];
                                if (dealerCard.Name == "Ace") dealerAces++;

                                dealerTotal += dealerCardValue;

                                // SOFT ACE ADJUSTMENT
                                // if the dealer busted AND has at least one Ace counted as 11
                                // drop one Ace from 11 to 1 by subtracting 10
                                // this is standard blackjack Ace handling
                                // keep doing this until either the total is <= 21 or no more soft Aces remain
                                while (dealerTotal > 21 && dealerAces > 0)
                                {
                                    dealerTotal -= 10;
                                    // subtract 10 = convert one Ace from 11 to 1
                                    // 11 - 10 = 1, net effect is the Ace is now worth 1
                                    dealerAces--;
                                    // one fewer Ace is being counted as 11
                                }

                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine("Dealer drew:  " + dealerCard);
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Dealer total: " + dealerTotal + "\n");
                                Console.ResetColor();
                            }

                            // dealer has finished drawing
                            // dealerTotal is now either 17-21 (stood) or 22+ (bust)
                            // result determination below handles both cases correctly
                        }

        
                        // DetermineWinner() extracts the result logic into its own method
                        // takes both totals, returns "Win", "Loss", or "Tie"
                        // the logic itself lives in the method above Main()
                        string result = DetermineWinner(playerTotal, dealerTotal);

                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("\n" + player.Username + "'s total: " + playerTotal);
                        Console.WriteLine("Dealer's total:  " + dealerTotal);

                        if (result == "Win")
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("\n★  GAME OVER: " + result + "  ★\n");
                        }
                        else if (result == "Loss")
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("\n★  GAME OVER: " + result + "  ★\n");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("\n★  GAME OVER: " + result + "  ★\n");
                        }
                        Console.ResetColor();

                        // update stats counters
                        if (result == "Win") stats.PlayerWins++;
                        else if (result == "Loss") stats.DealerWins++;
                        else stats.Ties++;
        
                        if (playerTotal > 21) stats.PlayerBusts++;
                        if (dealerTotal > 21) stats.DealerBusts++;

                        // TOKEN ADJUSTMENT
                        if (result == "Win")
                        {
                            tokenBalance += currentBet;
                            // player keeps their bet AND wins the same amount back
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("You won " + currentBet + " tokens! Balance: " + tokenBalance);
                            Console.ResetColor();
                        }
                        else if (result == "Loss")
                        {
                            tokenBalance -= currentBet;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("You lost " + currentBet + " tokens. Balance: " + tokenBalance);
                            Console.ResetColor();
                        }
                        else
                        {
                            // Tie - no tokens change hands
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Tie — bet returned. Balance: " + tokenBalance);
                            Console.ResetColor();
                        }

                        // auto end session if player runs out of tokens
                        if (tokenBalance < 5)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("\nNot enough tokens to continue. Game over.");
                            Console.ResetColor();
                            sessionActive = false;
                            // no play again prompt - session ends automatically
                        }

                        // STEP 9 = BUILD AND WRITE THE SESSION RECORD
                        // create a fresh SessionRecord for this hand
                        // filled with dot notation - same pattern as PlayerInfo
                        // once written it is thrown away, next hand creates a new one
                        SessionRecord record = new SessionRecord();
                        record.SessionID = sessionID;
                        record.Username = player.Username;
                        record.PlayerAge = playerAge;
                        // playerAge = calculated at login from DOB, DOB then discarded
                        record.LoginTime = loginTime;
                        record.GameNumber = stats.TotalGames;
                        record.PlayerTotal = playerTotal;
                        record.DealerTotal = dealerTotal;
                        record.Result = result;
                        record.PlayerBusted = playerTotal > 21;
                        record.DealerBusted = dealerTotal > 21;
                        record.NumberOfDraws = numberOfDraws;
                        record.BetAmount = currentBet;
                        record.TokensBefore = tokensBefore;
                        record.TokensAfter = tokenBalance;
                        // tokenBalance already updated by token adjustment above
                        // TokensAfter correctly reflects balance AFTER this hand resolved
                        record.StrategyMode = strategyOn ? "On" : "Off";
                        // ternary - writes "On" or "Off" as readable string in the CSV
                        record.OverrodeSuggestion = overrodeSuggestion;
                        // true if a warning was shown and acknowledged this hand
                        record.DoubledDown = doubledDown;
                        // true if the player doubled down this hand
                        // false for all normal hands

                        WriteRecordToCSV(record, csvPath);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Hand saved.\n");
                        Console.ResetColor();

                        // play again prompt - only shows if player still has tokens
                        // play again = place another bet
                        // if they want to play again they just place a bet naturally
                        // if they want to quit they press Escape
                        // this blends two prompts into one natural flow
                        if (sessionActive)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("─────────────────────────────────────");
                            Console.WriteLine("Place a bet to continue, or type 'exit' to see your session summary.");
                            Console.ResetColor();
                        }
                    }   // closes if (gameOver && sessionActive)

                }   // closes inner game loop (while !gameOver)

            }   // closes session loop (while sessionActive)


            // STEP 10 = END OF SESSION
            // both loops exited - session is over
            // simple menu gives the player options rather than just printing and closing
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n╔══════════════════════════════════════╗");
            Console.WriteLine("║       SESSION COMPLETE               ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Final token balance      : " + tokenBalance);
            Console.WriteLine("Strategy mode            : " + (stats.StrategyModeOn ? "On" : "Off"));
            Console.WriteLine("Suggestions overridden   : " + stats.SuggestionsOverridden);
            Console.WriteLine("Hands played             : " + stats.TotalGames);
            Console.WriteLine("Wins                     : " + stats.PlayerWins);
            Console.WriteLine("Losses                   : " + stats.DealerWins);
            Console.WriteLine("Ties                     : " + stats.Ties);
            Console.WriteLine("Full data saved to       : " + csvPath + "\n");
            Console.ResetColor();

            // end of session menu
            // gives the player options instead of just closing
            bool inMenu = true;
            while (inMenu)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╔══════════════════════════════════════╗");
                Console.WriteLine("║           WHAT'S NEXT?               ║");
                Console.WriteLine("╠══════════════════════════════════════╣");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("║  [P]  Play again (new session)       ║");
                Console.WriteLine("║  [ESC] Exit                          ║");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╚══════════════════════════════════════╝");
                Console.ResetColor();

                ConsoleKeyInfo menuKey = Console.ReadKey(true);

                if (menuKey.Key == ConsoleKey.P)
                {
                    // restart the entire program by calling Main() recursively
                    // this lets the player log in again as the same or different user
                    // in a future version this would return to a proper main menu
                    // for now recursive Main() achieves the same result simply
                    inMenu = false;
                    Main();
                    // NOTE: recursive Main() is a simple solution for now
                    // Phase 3 will replace this with a proper game loop at the top level
                }
                else if (menuKey.Key == ConsoleKey.Escape)
                {
                    inMenu = false;
                    // exits the menu loop - program falls through and closes naturally
                }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Thanks for playing. Press any key to exit.");
            Console.ResetColor();
            Console.ReadKey();

        }   // closes Main()

    }   // closes BlackjackGame

}   // closes namespace