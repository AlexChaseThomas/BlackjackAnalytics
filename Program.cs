using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;



namespace AlexThomasBlackJackProject2026
{
    // ══════════════════════════════════════════════════════════════════
    // BLACKJACK ANALYTICS — C# Console Application
    // Author  : Alex Thomas
    // GitHub  : https://github.com/AlexChaseThomas/BlackjackAnalytics
    // Version : Phase 3 Complete — SQLite integration, live analytics, strategy engine
    // ══════════════════════════════════════════════════════════════════
    //
    // PURPOSE:
    //   End-to-end behavioral analytics pipeline. The game functions as a
    //   data collection layer — every hand, decision, and outcome is written
    //   to a local SQLite database in real time. Phase 4 will use Python to
    //   generate synthetic data at scale. Phase 5 will visualize insights
    //   in Power BI. This file is the C# layer of that pipeline.
    //
    // ARCHITECTURE:
    //   C# Console App → SQLite (blackjack.db) → Python → Power BI
    //
    // DATA CLASSES (POCOs):
    //   PlayerInfo     — player identity (username only, no PII stored)
    //   Card           — single playing card with name and suit
    //   SessionRecord  — one row of analytics data written per hand
    //   GameStats      — session-level counters accumulated across hands
    //   SessionSummary — one row per session written to the Sessions table
    //
    // DATABASE TABLES:
    //   Players      — one row per unique player, lifetime stats and balance
    //   Sessions     — one row per session, start/end balance, net profit
    //   GameSessions — one row per hand, 29 columns of behavioral telemetry
    //
    // LOGIC CLASS:
    //   BlackjackGame — all methods and Main() entry point
    //
    // KEY DESIGN DECISIONS:
    //   - Hand enumeration model: dealer win probability calculated dynamically
    //     from current game state rather than a static lookup table
    //   - Weighted probability tree traversal: SimulateDealerDraw uses recursive
    //     ref parameters to accumulate outcomes across all possible draw sequences
    //   - PII policy: full date of birth entered for age verification only,
    //     immediately discarded — only the calculated age integer is stored
    //   - Sessions INSERT at start, UPDATE at end: session exists in the database
    //     from the moment it begins, not just when it completes
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

        // additional fields added during phase 3 schema expansion
        public string DealerVisibleCard;

        public int DealerVisibleValue;
        public int OpeningPlayerTotal;
        public int OpeningDealerTotal;
        public bool PlayerHandWasSoft;
        public int HandDurationSeconds;
        public string OSVersion;
        // strategy recommendation fields = capture what advice was given and whether it was followed
        // enables recommendation adherence analysis, override heatmaps, and learning curve detection
        public string RecommendedAction;      // "HIT", "STAND", or "NONE" — what basic strategy said to do
        public bool RecommendationFollowed; // true if player did what was recommended
        public string RiskLevel;              // "LOW", "MODERATE", "HIGH", "VERY HIGH" — bust risk category
        public double DealerWinProbability;   // probability dealer beats current player total if player stands
    }

    public class GameStats
    {
        // Game results tracking = counters incremented after each hand resolves 
        public int TotalGames = 0;
        public int PlayerWins = 0;
        public int DealerWins = 0; // dealer wins = player losses
        public int Ties = 0;
        public int PlayerBusts = 0;
        public int DealerBusts = 0;

        // strategy tracking - added for basic strategy analytics
        // StrategyModeOn = records whether this session used suggestions 
        // SuggestionsOverridden = records (counts) how many times the player ignored a warning 
        public bool StrategyModeOn = false;
        public int SuggestionsOverridden = 0;
    }

    // SessionSummary = a data class that mirrors one row of the Sessions table
    // Captured once per session — start balance, end balance, net profit, total hands
    // Stored separately from GameSessions so session-level queries do not require aggregation across every hand row — direct reads from Sessions are faster and cleaner

    public class SessionSummary
    {
        public int SessionID;
        public string Username;
        public int PlayerID;
        public string StartTime;
        public string EndTime;
        public int TotalHands;
        public int StartBalance;
        public int EndBalance;
        public int NetProfit;
    }
    class BlackjackGame // this type of class is only accessible within this file/namespace (default = "internal")
    {
        // Single shared Random instance for the entire class; declared at class level = all methods share it 
  
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
        // If the deck runs low (i.e. fewer than 10 cards) it reshuffles automatically = prevents us from running out of cards mid-hand 
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
            int safeRoom = 21 - currentTotal;

            // Dictionary mapping each card name to its count in a standard deck
            // 10, Jack, Queen, King all have value 10 - there are 4 of each = 16 total
            // represented here as count 4 for the value 10
            // all other values appear once in each suit = count 4 total
            // but since we care about distinct VALUES not suits, we track per value:
            // Ace=4, 2=4, 3=4 ... 9=4, 10/J/Q/K=16 total for value 10
            // simplified to 13 distinct card types with their bust contribution weights
            Dictionary<int, int> valueCount = new Dictionary<int, int>()
            {
                { 2,  1 }, { 3,  1 }, { 4,  1 }, { 5,  1 },
                { 6,  1 }, { 7,  1 }, { 8,  1 }, { 9,  1 },
                { 10, 4 }, // 10, Jack, Queen, King all worth 10
                { 11, 1 }  // Ace — handled separately below for flexibility
            };
            // total = 13 distinct card type slots (weighted)
            // 10 counts as 4 because 10/J/Q/K are four separate card types all worth 10

            int totalTypes = 13;
            // 13 = 9 unique low values (2-9) + 4 ten-value types (10/J/Q/K) + Ace
            // this matches the probability denominator used throughout

            int bustCount = 0;

            foreach (var entry in valueCount) // foreach loop = iterates over every item in a collection one at a time
                // in a dictionary, each item is a key-balue pair (e.g. int cardValue paired with int cardWeight)
            {
                int cardValue = entry.Key; // the cards point value 
                int cardWeight = entry.Value; // how many card types share that value

                // Ace flexibility: if Ace as 11 would bust, count it as 1 instead
                if (cardValue == 11 && cardValue > safeRoom)
                    cardValue = 1;

                if (cardValue > safeRoom)
                    bustCount += cardWeight;
            }

            double bustChance = (double)bustCount / totalTypes * 100;
            return Math.Round(bustChance) + "%";
        }
        // closes CalculateBustChance

        // METHOD: CalculateBustChanceDouble =  same logic as CalculateBustChance() but returns a double instead of a formatted string = used by the strategy recommendation system which needs the raw number for category thresholds
        static double CalculateBustChanceDouble(int currentTotal)
        {
            int safeRoom = 21 - currentTotal;

            Dictionary<int, int> valueCount = new Dictionary<int, int>()
            {
                { 2,  1 }, { 3,  1 }, { 4,  1 }, { 5,  1 },
                { 6,  1 }, { 7,  1 }, { 8,  1 }, { 9,  1 },
                { 10, 4 },
                { 11, 1 }
            };

            int totalTypes = 13;
            int bustCount = 0;

            foreach (var entry in valueCount)
            {
                int cardValue = entry.Key;
                int cardWeight = entry.Value;

                if (cardValue == 11 && cardValue > safeRoom)
                    cardValue = 1;

                if (cardValue > safeRoom)
                    bustCount += cardWeight;
            }

            return (double)bustCount / totalTypes * 100;
        }   // closes CalculateBustChanceDouble

        // METHOD: InitializeDatabase = creates the blackjack.db file and all three tables on first run
        // Safe to call on every program start — IF NOT EXISTS means no data is ever overwritten
        // Execution order matters: Players must exist before Sessions (foreign key reference)
        // Sessions must exist before GameSessions (foreign key reference)
        // Called once at the top of Main() before any player data is read or written
        static void InitializeDatabase(string dbPath)
        {
            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();

                using (var cmd = new SQLiteCommand(connection))
                {
                    // TABLE: Players
                    // one row per unique username
                    // stores identity, balance, and lifetime stats
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Players (
                            PlayerID             INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username             TEXT    UNIQUE NOT NULL,
                            PlayerAge            INTEGER,
                            FirstSeen            TEXT,
                            LastSeen             TEXT,
                            TokenBalance         INTEGER CHECK(TokenBalance >= 0),
                            TotalHandsAllTime    INTEGER DEFAULT 0,
                            TotalWinsAllTime     INTEGER DEFAULT 0,
                            FavoriteStrategyMode TEXT    DEFAULT 'Unknown',
                            LongestWinStreak     INTEGER DEFAULT 0
                        )";
                    cmd.ExecuteNonQuery();

                    // TABLE: Sessions
                    // one row per session
                    // captures session-level analytics without requiring aggregation
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Sessions (
                            SessionID    INTEGER PRIMARY KEY,
                            PlayerID     INTEGER REFERENCES Players(PlayerID),
                            Username     TEXT    REFERENCES Players(Username),
                            StartTime    TEXT,
                            EndTime      TEXT,
                            TotalHands   INTEGER DEFAULT 0,
                            StartBalance INTEGER DEFAULT 0,
                            EndBalance   INTEGER DEFAULT 0,
                            NetProfit    INTEGER DEFAULT 0
                        )";
                    cmd.ExecuteNonQuery();

                    // TABLE: GameSessions
                    // one row per hand played
                    // primary analytics table — joins to Players via PlayerID
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS GameSessions (
                            RecordID           INTEGER PRIMARY KEY AUTOINCREMENT,
                            SessionID          INTEGER REFERENCES Sessions(SessionID),
                            PlayerID           INTEGER REFERENCES Players(PlayerID),
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
                            DoubledDown        INTEGER,
                            DealerVisibleCard  TEXT    DEFAULT 'Unknown',
                            DealerVisibleValue INTEGER DEFAULT 0,
                            OpeningPlayerTotal INTEGER DEFAULT 0,
                            OpeningDealerTotal INTEGER DEFAULT 0,
                            PlayerHandWasSoft  INTEGER DEFAULT 0,
                            HandDurationSeconds INTEGER DEFAULT 0,
                            OSVersion          TEXT    DEFAULT 'Unknown',
                            RecommendedAction    TEXT    DEFAULT 'NONE',
                            RecommendationFollowed INTEGER DEFAULT 0,
                            RiskLevel            TEXT    DEFAULT 'NONE',
                            DealerWinProbability REAL    DEFAULT 0.0
                        )";
                    cmd.ExecuteNonQuery();
                }
            }
        }   // closes InitializeDatabase

        // METHOD: DetermineWinner = takes the player's final total and the dealer's final total = returns the result as a plain string: "Win", "Loss", or "Tie"
        // Extracted from Main() so the logic all lives in one place = DRY principle 
        // Win/Loss Changes = only need to be changed once here 
        // Order Matters: most specific cases come first so that they aren't missed by more general conditions being placed at the start 
        static string DetermineWinner(int playerTotal, int dealerTotal)
        {
            if (playerTotal == 21 && dealerTotal == 21) return "Tie";
            // both hit 21 = push - must come before the individual 21 checks below
            // without this the player 21 check would fire first and incorrectly return Win

            if (playerTotal > 21) return "Loss";
            // In real blackjack, when the player busts, they always lose - it doesn't matter if the dealer busts after - the house does not share the loss

            if (playerTotal == 21) return "Win";
            // player hit exactly 21 - Blackjack

            if (dealerTotal == 21) return "Loss";
            // dealer hit exactly 21 - dealer Blackjack

            if (dealerTotal > 21) return "Win";
            // dealer busted

            if (playerTotal > dealerTotal) return "Win";
            // neither busted, player has higher total

            if (dealerTotal > playerTotal) return "Loss";
            // neither busted, dealer has higher total

            return "Tie";
            // final case = no condition needed = only possibility left is equal totals
        }   // closes DetermineWinner


        // METHOD: PrintPlayerHand = prints all cards in the player's current hand 
        // Method gets called after every draw so that the player can always view their full hand
        static void PrintPlayerHand(List<Card> hand, bool aceCountingAsOne = false)
        {
         
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Your hand:  ");
            for (int i = 0; i < hand.Count; i++)
            {
                Console.Write(hand[i].ToString());
                if (i < hand.Count - 1)
                    Console.Write("  |  ");
            }
            if (aceCountingAsOne)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("  (Ace counting as 1)");
            }
            Console.WriteLine();
            Console.ResetColor();
        }   // closes PrintPlayerHand

        // METHOD: PrintDealerHand = prints all cards in the dealer's current
        // Method gets called after a hole card reveal and after each dealer draw
        static void PrintDealerHand(List<Card> hand)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Dealer hand: ");
            for (int i = 0; i < hand.Count;i++)
            {
                Console.Write(hand[i].ToString());
                if (i < hand.Count - 1)
                    Console.Write(" | ");

            }
            
            Console.WriteLine();
            Console.ResetColor();
        } // closes PrintDealerHand

        // METHOD: RegisterOrLoginPlayer = checks if the username exits in the player table (yes = existing player = load TokenBalance, no = new player = insert a row with 100 starting tokens)
        // Returns a C# tuple — three values packed into one return statement:
        //   balance          = token balance to start the session with
        //   playerID         = primary key used to link GameSessions and Sessions rows
        //   longestWinStreak = loaded so the in-session counter can build on lifetime best
        // REPLACES: LoadPlayerBalance() which read balance from the CSV
        static (int balance, int playerID, int longestWinStreak) RegisterOrLoginPlayer(
             string username, int playerAge, string loginTime, string dbPath)
        {
            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();

                // check if username exists
                using (var checkCmd = new SQLiteCommand(connection))
                {
                    checkCmd.CommandText = @"
                        SELECT PlayerID, TokenBalance, LongestWinStreak
                        FROM Players
                        WHERE Username = @username";
                    checkCmd.Parameters.AddWithValue("@username", username);

                    using (var reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // returning player
                            int playerID = reader.GetInt32(0);
                            int balance = reader.GetInt32(1);
                            int longestWinStreak = reader.GetInt32(2);

                            
                            return (balance, playerID, longestWinStreak);
                        }
                    }
                }

                // new player — insert row
                using (var insertCmd = new SQLiteCommand(connection))
                {
                    insertCmd.CommandText = @"
                        INSERT INTO Players (Username, PlayerAge, FirstSeen, LastSeen, TokenBalance)
                        VALUES (@username, @playerAge, @loginTime, @loginTime, 100)";
                    insertCmd.Parameters.AddWithValue("@username", username);
                    insertCmd.Parameters.AddWithValue("@playerAge", playerAge);
                    insertCmd.Parameters.AddWithValue("@loginTime", loginTime);
                    insertCmd.ExecuteNonQuery();
                }

                // get the new PlayerID
                using (var idCmd = new SQLiteCommand(connection))
                {
                    idCmd.CommandText = "SELECT PlayerID FROM Players WHERE Username = @username";
                    idCmd.Parameters.AddWithValue("@username", username);
                    int newPlayerID = Convert.ToInt32(idCmd.ExecuteScalar());
                    return (100, newPlayerID, 0);
                }
            }
        }   // closes RegisterOrLoginPlayer

        // METHOD: InsertGameRecord = inserts one completed hand into the GameSessions table + updates the player's TokenBalance in the Player's table
        // Called once at the end of every hand including forfeits, blackjacks, and naturals
        // The two operations share one connection — open once, write both, close once
        // REPLACES: WriteRecordToCSV() from Phase 2
        static void InsertGameRecord(SessionRecord record, string dbPath, int playerID)
        {
            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();

                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"
                        INSERT INTO GameSessions (
                            SessionID, PlayerID, Username, PlayerAge, LoginTime,
                            GameNumber, PlayerTotal, DealerTotal,
                            Result, PlayerBusted, DealerBusted, NumberOfDraws,
                            BetAmount, TokensBefore, TokensAfter,
                            StrategyMode, OverrodeSuggestion, DoubledDown,
                            DealerVisibleCard, DealerVisibleValue,
                            OpeningPlayerTotal, OpeningDealerTotal,
                            PlayerHandWasSoft, HandDurationSeconds, OSVersion, 
                            RecommendedAction, RecommendationFollowed,
                            RiskLevel, DealerWinProbability
                        ) VALUES (
                            @sessionID, @playerID, @username, @playerAge, @loginTime,
                            @gameNumber, @playerTotal, @dealerTotal,
                            @result, @playerBusted, @dealerBusted, @numberOfDraws,
                            @betAmount, @tokensBefore, @tokensAfter,
                            @strategyMode, @overrodeSuggestion, @doubledDown,
                            @dealerVisibleCard, @dealerVisibleValue,
                            @openingPlayerTotal, @openingDealerTotal,
                            @playerHandWasSoft, @handDurationSeconds, @osVersion,
                            @recommendedAction, @recommendationFollowed,
                            @riskLevel, @dealerWinProbability
                        )";

                    cmd.Parameters.AddWithValue("@sessionID", record.SessionID);
                    cmd.Parameters.AddWithValue("@playerID", playerID);
                    cmd.Parameters.AddWithValue("@username", record.Username);
                    cmd.Parameters.AddWithValue("@playerAge", record.PlayerAge);
                    cmd.Parameters.AddWithValue("@loginTime", record.LoginTime);
                    cmd.Parameters.AddWithValue("@gameNumber", record.GameNumber);
                    cmd.Parameters.AddWithValue("@playerTotal", record.PlayerTotal);
                    cmd.Parameters.AddWithValue("@dealerTotal", record.DealerTotal);
                    cmd.Parameters.AddWithValue("@result", record.Result);
                    cmd.Parameters.AddWithValue("@playerBusted", record.PlayerBusted ? 1 : 0);
                    cmd.Parameters.AddWithValue("@dealerBusted", record.DealerBusted ? 1 : 0);
                    cmd.Parameters.AddWithValue("@numberOfDraws", record.NumberOfDraws);
                    cmd.Parameters.AddWithValue("@betAmount", record.BetAmount);
                    cmd.Parameters.AddWithValue("@tokensBefore", record.TokensBefore);
                    cmd.Parameters.AddWithValue("@tokensAfter", record.TokensAfter);
                    cmd.Parameters.AddWithValue("@strategyMode", record.StrategyMode);
                    cmd.Parameters.AddWithValue("@overrodeSuggestion", record.OverrodeSuggestion ? 1 : 0);
                    cmd.Parameters.AddWithValue("@doubledDown", record.DoubledDown ? 1 : 0);
                    cmd.Parameters.AddWithValue("@dealerVisibleCard", record.DealerVisibleCard);
                    cmd.Parameters.AddWithValue("@dealerVisibleValue", record.DealerVisibleValue);
                    cmd.Parameters.AddWithValue("@openingPlayerTotal", record.OpeningPlayerTotal);
                    cmd.Parameters.AddWithValue("@openingDealerTotal", record.OpeningDealerTotal);
                    cmd.Parameters.AddWithValue("@playerHandWasSoft", record.PlayerHandWasSoft ? 1 : 0);
                    cmd.Parameters.AddWithValue("@handDurationSeconds", record.HandDurationSeconds);
                    cmd.Parameters.AddWithValue("@osVersion", record.OSVersion);
                    cmd.Parameters.AddWithValue("@recommendedAction", record.RecommendedAction);
                    cmd.Parameters.AddWithValue("@recommendationFollowed", record.RecommendationFollowed ? 1 : 0);
                    cmd.Parameters.AddWithValue("@riskLevel", record.RiskLevel);
                    cmd.Parameters.AddWithValue("@dealerWinProbability", record.DealerWinProbability);

                    cmd.ExecuteNonQuery();
                }

                using (var updateCmd = new SQLiteCommand(connection))
                {
                    updateCmd.CommandText = @"
                        UPDATE Players
                        SET TokenBalance = @tokensAfter
                        WHERE Username = @username";
                    updateCmd.Parameters.AddWithValue("@tokensAfter", record.TokensAfter);
                    updateCmd.Parameters.AddWithValue("@username", record.Username);
                    updateCmd.ExecuteNonQuery();
                }
            }
        }   // closes InsertGameRecord

        // METHOD: InsertSessionRecord = writes one row to the Sessions table at the start of each session
        // Captures:  SessionID, PlayerID, Username, StartTime, and StartBalance
        // EndTime, TotalHands, EndBalance, and NetProfit are updated at session end via UpdateSessionRecord()
        // separating INSERT and UPDATE mirrors how real session tracking works = the session exists in the database from the moment it starts, not just when it ends
        static void InsertSessionRecord(int sessionID, int playerID, string username,
                                         string startTime, int startBalance, string dbPath)
        {
            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"
                        INSERT OR IGNORE INTO Sessions (
                            SessionID, PlayerID, Username, StartTime, StartBalance
                        ) VALUES (
                            @sessionID, @playerID, @username, @startTime, @startBalance
                        )";
                    // INSERT OR IGNORE = if this SessionID already exists (e.g. recursive Main() call)
                    // do nothing rather than throwing an error
                    cmd.Parameters.AddWithValue("@sessionID", sessionID);
                    cmd.Parameters.AddWithValue("@playerID", playerID);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@startTime", startTime);
                    cmd.Parameters.AddWithValue("@startBalance", startBalance);
                    cmd.ExecuteNonQuery();
                }
            }
        }   // closes InsertSessionRecord

        // METHOD: UpdateSessionRecord =  updates the Sessions row at the end of the session with final stats
        // Called once after both loops exit, before the session summary prints
        static void UpdateSessionRecord(int sessionID, string endTime, int totalHands,
                                         int endBalance, int netProfit, string dbPath)
        {
            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"
                        UPDATE Sessions
                        SET EndTime    = @endTime,
                            TotalHands = @totalHands,
                            EndBalance = @endBalance,
                            NetProfit  = @netProfit
                        WHERE SessionID = @sessionID";
                    cmd.Parameters.AddWithValue("@endTime", endTime);
                    cmd.Parameters.AddWithValue("@totalHands", totalHands);
                    cmd.Parameters.AddWithValue("@endBalance", endBalance);
                    cmd.Parameters.AddWithValue("@netProfit", netProfit);
                    cmd.Parameters.AddWithValue("@sessionID", sessionID);
                    cmd.ExecuteNonQuery();
                }
            }
        }   // closes UpdateSessionRecord

        // METHOD: UpdatePlayerLifetimeStats =  updates the Players table with cumulative lifetime stats at the end of each session
        // TotalHandsAllTime and TotalWinsAllTime use += so they accumulate across all sessions
        // FavoriteStrategyMode updates to the current session's choice
        // Called once per session after both loops exit
        static void UpdatePlayerLifetimeStats(string username, int handsThisSession,
                                               int winsThisSession, bool strategyOn, string dbPath)
        {
            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"
                        UPDATE Players
                        SET TotalHandsAllTime    = TotalHandsAllTime + @hands,
                            TotalWinsAllTime     = TotalWinsAllTime  + @wins,
                            FavoriteStrategyMode = @strategyMode
                        WHERE Username = @username";
                    // TotalHandsAllTime + @hands = SQL-side increment
                    // this is safer than read-modify-write in C# because it avoids
                    // race conditions if two sessions ever ran simultaneously
                    cmd.Parameters.AddWithValue("@hands", handsThisSession);
                    cmd.Parameters.AddWithValue("@wins", winsThisSession);
                    cmd.Parameters.AddWithValue("@strategyMode", strategyOn ? "On" : "Off");
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.ExecuteNonQuery();
                }
            }
        }   // closes UpdatePlayerLifetimeStats

        // METHOD: CheckDailyBonusDB = reads LastSeen from the Players table and compares it to the current login time
        // If 24 or more hours have passed since last login = award 50 tokens + update LastSeen to now 
        // If less than 24 hours have passed = show countdown to next bonus and update LastSeen to now 
        // Updating LastSeen on every login (bonus or not) = maintains the countdown's accuracy
        // REPLACES: CheckDailyBonus) which read LoginTime from the CSV
        static int CheckDailyBonusDB(string username, int currentBalance, string loginTime, string dbPath, out double hoursUntilBonus)
        {
            hoursUntilBonus = 0;

            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();

                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = "SELECT LastSeen FROM Players WHERE Username = @username";
                    cmd.Parameters.AddWithValue("@username", username);

                    var result = cmd.ExecuteScalar();

                    if (result == null) return currentBalance;

                    if (!DateTime.TryParse(result.ToString(), out DateTime lastSeen))
                        return currentBalance;

                    TimeSpan timeSinceLastLogin = DateTime.Now - lastSeen;

                    if (timeSinceLastLogin.TotalHours >= 24)
                    {
                        int newBalance = currentBalance + 50;

                        using (var updateCmd = new SQLiteCommand(connection))
                        {
                            updateCmd.CommandText = @"
                                UPDATE Players
                                SET TokenBalance = @newBalance, LastSeen = @loginTime
                                WHERE Username = @username";
                            updateCmd.Parameters.AddWithValue("@newBalance", newBalance);
                            updateCmd.Parameters.AddWithValue("@loginTime", loginTime);
                            updateCmd.Parameters.AddWithValue("@username", username);
                            updateCmd.ExecuteNonQuery();
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("╔══════════════════════════════════════╗");
                        Console.WriteLine("║       🎁  DAILY BONUS AWARDED!        ║");
                        Console.WriteLine("║    +50 tokens added to your balance  ║");
                        Console.WriteLine("╚══════════════════════════════════════╝");
                        Console.WriteLine("Previous balance : " + currentBalance + " tokens");
                        Console.WriteLine("New balance      : " + newBalance + " tokens\n");
                        Console.ResetColor();
                        return newBalance;
                    }
                    else
                    {
                        hoursUntilBonus = 24 - timeSinceLastLogin.TotalHours;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Daily bonus available in: " + Math.Round(hoursUntilBonus, 1) + " hours.\n");
                        Console.ResetColor();

                        // update LastSeen to now so the countdown is accurate next login
                        using (var updateCmd = new SQLiteCommand(connection))
                        {
                            updateCmd.CommandText = @"
                                UPDATE Players
                                SET LastSeen = @loginTime
                                WHERE Username = @username";
                            updateCmd.Parameters.AddWithValue("@loginTime", loginTime);
                            updateCmd.Parameters.AddWithValue("@username", username);
                            updateCmd.ExecuteNonQuery();
                        }

                        return currentBalance;
                    }
                }
            }
        }   // closes CheckDailyBonusDB

        // METHOD: GetStrategyRecommendation = takes the player's cirremt total and the dealer's visible card value = returns "Hit" or "Stand" according to blackjack basic strategy rules
        // Basic Strategy: when dealer is weak (2-6) the dealer busts often — stand on lower totals & when dealer is strong (7-Ace) the dealer rarely busts — hit more aggressively
        // Same logic used in published casino strategy cards BUT the difference is that we calculate the supporting probability in realtime rather than from a lookup table
        static string GetStrategyRecommendation(int playerTotal, int dealerVisibleValue)
        {
            // always stand on 17 or higher regardless of dealer card
            // the bust risk is too high to justify drawing in any scenario 
            if (playerTotal >= 17) return "STAND";

            // always hit on 11 or lower - cannot bust, drawing is always the correct move
            if (playerTotal <= 11) return "HIT";

            //dealer showing weak card (2-6) - dealer must draw and often busts 
            //stand on 13-16, stand on 12 vs 4-6
            if (dealerVisibleValue <= 6)
            {
                if (playerTotal >= 13) return "STAND";
                if (playerTotal == 12 && dealerVisibleValue >= 4) return "STAND";
                return "HIT";
                // player 12 vs dealer 2-3 = hit (dealer not weak enough to justify standing on 12)
            }

            // dealer showing strong card (7-Ace) - dealer rarely busts, player must draw
            // hit on 12-16 vs strong dealer 
            return "HIT";

        } // closes GetStrategyRecommendation

        // METHOD: CalculateDealerWinProbability = hand enumeration model = calculates the probability the dealer beats the player's current total
        // Assumes the player stands at their current total (does not draw again)
        // Enumerates all possible dealer hole cards weighted by deck frequency 
        // For each hole card = simulates the dealer drawing to 17 using expected value weighting 
        // Returns dealer win probability as a double 

        // WHY THIS APPROACH INSTEAD OF A BASIC STRATEGY TABLE:
        // Standard casino strategy card and many online gambling platforms implement recommendations as a static lookup table (e.g. player 16 vs dealer 10 = hit, regardless of the actual gamestate i.e. which specific combinations of cards were dealt) 
        // Those tables were derived from exhuastive simulations done in the past and published, the platform just looks up the answer, but no math happens at runtime. 
        // Here, we actually perform the underlying calculation dynamically from the current game state. Every recommendation is computed fresh based on the actual cards that are in play.
        // i.e. the player sees not only what to do, but the probabilistic reasoning behind that recommendation; the same reasoning that generated the casino tables, made transparent and live. 

        // The practical difference is small in a single-deck game with few cards dealt.
        // HOWEVER, the aarchitectural difference is significant: this is a decision support system, not just a lookup table.
        // That distinction mattters for the analytics layer: every recommendation is grounded in real probability derived from game state, not a static rule applied regardless of context. 
        static double CalculateDealerWinProbability(int playerTotal, int dealerVisibleValue)
        {
            // card weights — how many of each value exist in a standard deck
            // 10/Jack/Queen/King all have value 10 = weight 4
            // all other values = weight 1 each
            // Ace = value 11 (handled separately for soft total adjustment)
            Dictionary<int, int> cardWeights = new Dictionary<int, int>()
            {
                { 2,  1 }, { 3,  1 }, { 4,  1 }, { 5,  1 },
                { 6,  1 }, { 7,  1 }, { 8,  1 }, { 9,  1 },
                { 10, 4 }, // 10, Jack, Queen, King
                { 11, 1 }  // Ace
            };

            int totalWeight = 13;
            // 9 single-weight values (2-9) + 4-weight value (10) + Ace = 13 total weight units

            double dealerWins = 0;
            double totalOutcomes = 0;

            // OUTER LOOP — enumerate every possible dealer hole card
            foreach (var holeEntry in cardWeights)
            {
                int holeValue = holeEntry.Key;
                int holeWeight = holeEntry.Value;

                // calculate dealer's two-card starting total
                int dealerAces = 0;
                if (dealerVisibleValue == 11) dealerAces++;
                if (holeValue == 11) dealerAces++;

                int dealerTotal = dealerVisibleValue + holeValue;

                // soft Ace adjustment on opening hand
                while (dealerTotal > 21 && dealerAces > 0)
                {
                    dealerTotal -= 10;
                    dealerAces--;
                }

                // INNER LOOP — simulate dealer drawing to 17 using expected value weighting
                // instead of enumerating every possible draw sequence (exponential),
                // we weight each draw by its probability and accumulate expected outcomes
                // this gives accurate probability without combinatorial overload
                SimulateDealerDraw(playerTotal, dealerTotal, dealerAces,
                                   holeWeight, totalWeight, cardWeights,
                                   ref dealerWins, ref totalOutcomes);
            }

            if (totalOutcomes == 0) return 0;
            return Math.Round(dealerWins / totalOutcomes * 100, 1);
        }   // closes CalculateDealerWinProbability

        // METHOD: SimulateDealerDraw = recursive helper for CalculateDealerWinProbability = simulates the dealer drawing to 17 from the current state
        // Utilizes weighted card probabilities rather than enumerating every possible sequence 
        // weight = the cumulative probability weight of reaching this game state 

        // Parameters:
        // int playerTotal = the player's current hand total = passed through method so we know whether or not the dealer won = the dealer wins only if their final total is between 17 and 21 AND its higher than this number (playerTotal)
        // int dealerTotal = the dealer's current hand total = 1st call - this is the dealer's visible card plus one possible hole card = 2nd+ (recursive) calls - this is the dealer's total after drawing one more card. The method keeps drawing until this number reaches 17 or higher. 
        // int dealerAces = How many Aces the dealer currently holds that are still counting as 11 = mirrors soft Ace logic in Main() = if the dealer would bust but has a soft Ave, subtract 10 and decrement this counter - without tracking this the simulation would incorrectly bust the dealer on hands where an Ace rescue is avaliable.

        // double weight = this is the key to how the method works without enumerating billions of combinatitions = instead of simulating every possible sequence of cards as a separate path, each path is assigned a probability weight. = the first call starts with the weight of the hole card, when the dealer draws another card, the weight multiplie by that card's probability
        // EXAMPLE:In the first call, a 10-value card has weight 4/13 because the are 4 tem-value cards out of the 13 distinct card types. In the recursive calls (i.e. dealer draws another card) a path where the dealer's hole card is a 10, and the draws a 5 has weight 4/13*1/13 = the weight accumulates the probability of that entire sequence occuring.

        // int totalWeight = the denominator for probability calculations = always 13 in our model = 13 distinct card type slots (9 single-weight values, one 4-weight values for tens, one for Ace) = passed in so that recursive calls can use the same denominator without recalculating it.
        // Dictionary<int, int> cardWeights = the same card frequency table used throughout the program = maps each card value to how many of that type exist in the deck

        // ref double dealerWins = the running tally of proabbility-weighted outcomes where the dealer beat the player. ref means this varaible is passed by reference (the method writes directly into the variabel that was declared in CalculateDealerWinProbability() rather than working on a copy) = Everytime the simulation reaches a terminal state where the dealer wins, it adds weight of that path to its total. =  At the end, dealerWins holds the sum of all winning path probabilities 
        // ref double totalOutcomes = the running tally of all terminal outcomes regardless of who wins = everytime the simulation reaches a terminal state (e.g. dealer stands on 17+, dealer busts, any finished hand) = adds that path's weight here = totalOutcomes holds the total probability mass of all outcomes = dividing dealerWins by totalOutcomes gives the win probability

        // WHY REF INSTEAD OF RETURN VALUES? 
        // The method calls itself recursively - SimulateDealerDraw calls SimulateDealerDraw for each possible next card. A recursive method cannot return two accumualting values cleanly. 
        // Using ref paramaeters lets every level of the recursion write directly into the same two variables that were declared at the top = every branch of the probability tree updates the same counters regardless of how deep the recursion goes. 

        // WHAT DOES THE RECURSION LOOK LIKE IN PRACTICE? 
        // Start: dealer showing 6, hole card is a 10. dealerTotal = 16, weight = 4/13.
                      
        // Dealer must draw(16 < 17). Loop over all possible next cards:
             // Draw a 2 (weight 1/13): dealerTotal = 18, weight = 4/13 × 1/13. Dealer stands.Does 18 beat playerTotal? Add to tallies.
             // Draw a 5 (weight 1/13): dealerTotal = 21, weight = 4/13 × 1/13. Dealer stands.Does 21 beat playerTotal? Add to tallies.
             // Draw a 10 (weight 4/13): dealerTotal = 26, weight = 4/13 × 4/13. Dealer busts.Add weight to totalOutcomes only.
             // Draw an Ace (weight 1/13): dealerTotal = 27, soft Ace drops to 17, weight = 4 / 13 × 1/13. Dealer stands.Evaluate.
        // Each of those draws that did not yet reach 17 would recurse again. The tree keeps branching until every path terminates. The weights ensure that more probable path contribute more to the final probability than less probable paths. 
        
        // This method is WEIGHTED PROBABILITY TREE TRAVERSAL
        // ANSWERS: If the dealer has this total right now and draws to 17, across all possible ways that could unfold, weighted by their likelihood, what fraction of outcomes end with dealer beating the player = that fraction is the number we show to the player in the recommendation box
        static void SimulateDealerDraw(int playerTotal, int dealerTotal, int dealerAces, double weight, int totalWeight, Dictionary <int, int> cardWeights, ref double dealerWins, ref double totalOutcomes)
        {
            if (dealerTotal >= 17)
            {
                // dealer has finished drawing — evaluate outcome
                totalOutcomes += weight;
                if (dealerTotal <= 21 && dealerTotal > playerTotal)
                    dealerWins += weight;
                // dealer busts = player wins = not counted in dealerWins
                // dealer total <= playerTotal = player wins or ties = not counted
                return;
            }

            // dealer must draw — recurse for each possible next card
            foreach (var entry in cardWeights)
            {
                int cardValue = entry.Key;
                int cardWeight = entry.Value;
                int newTotal = dealerTotal + cardValue;
                int newAces = dealerAces + (cardValue == 11 ? 1 : 0);

                // soft Ace adjustment
                while (newTotal > 21 && newAces > 0)
                {
                    newTotal -= 10;
                    newAces--;
                }

                // recurse with updated state and scaled weight
                // weight scales by cardWeight/totalWeight at each draw level
                SimulateDealerDraw(playerTotal, newTotal, newAces,
                                   weight * cardWeight / totalWeight,
                                   totalWeight, cardWeights,
                                   ref dealerWins, ref totalOutcomes);
            }
        }   // closes SimulateDealerDraw

        // METHOD: GetRiskLevel = converts a bust percentage to a human-readable risk category 
        // Categories are processed faster than raw percentages for in-game decisions 
        // Raw percentage is still displayed alongside for analytical transparency
        static string GetRiskLevel(double bustChance)
        {
            if (bustChance <= 30) return "LOW";
            if (bustChance <= 55) return "MODERATE";
            if (bustChance <= 75) return "HIGH";
            return "VERY HIGH";
        }   // closes GetRiskLevel

        // METHOD: GetDealerStrength = converts dealer win probability to a human-readable strength category
        static string GetDealerStrength(double dealerWinProbability)
        {
            if (dealerWinProbability <= 30) return "WEAK";
            if (dealerWinProbability <= 50) return "MODERATE";
            return "STRONG";
        }   // closes GetDealerStrength

        // METHOD: PrintStrategyRecommendation
        // Two-tier display system based on decision stakes
        // Total 11 or lower  — no display, data still captured, player cannot bust
        // Total 12 or higher — inline tip with color-coded controls
        // green = recommended action, red = override action, cyan = quit
        // Mirrors how real strategy cards work — guidance only when the decision is meaningful
        static void PrintStrategyRecommendation(string recommendation, double bustChance,
                                                 double dealerWinProb, int dealerVisibleValue,
                                                 int playerTotal)
        {
            string riskLevel = GetRiskLevel(bustChance);
            string dealerStrength = GetDealerStrength(dealerWinProb);
            string bustDisplay = riskLevel + " (" + (int)bustChance + "%)";

            // TIER 1 — total 11 or lower — no display needed
            // player cannot bust, drawing is always correct, no guidance necessary
            if (playerTotal <= 11) return;

            // TIER 2 — total 12 or higher — inline tip with stats and color-coded controls
            Console.ForegroundColor = ConsoleColor.Yellow;
            if (recommendation == "HIT")
            {
                Console.WriteLine("💡 Tip! HIT. The dealer has a " + (int)dealerWinProb +
                                   "% chance of winning if you stand, the dealer's position is " +
                                   dealerStrength + ".");
            }
            else
            {
                int playerWinProb = 100 - (int)dealerWinProb;
                if (dealerVisibleValue >= 4 && dealerVisibleValue <= 6)
                {
                    double dealerBustProb = CalculateDealerBustProbability(dealerVisibleValue);
                    Console.WriteLine("💡 Tip! STAND. The dealer has a " + (int)dealerBustProb +
                                      "% chance of busting - hitting carries a " + bustDisplay + " bust risk.");
                }
                else
                {
                    Console.WriteLine("💡 Tip! STAND. You have a " + playerWinProb +
                          "% chance of winning if you stand — hitting carries a " + bustDisplay + " bust risk.");
                }
            }

            if (recommendation == "HIT")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("   [ENTER] HIT  ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[N] STAND  ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[ESC] QUIT");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("   [N] STAND  ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[ENTER] HIT  ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[ESC] QUIT");
            }
            Console.WriteLine();
            Console.ResetColor();
        }   // closes PrintStrategyRecommendation

        // METHOD: CalculateDealerBustProbability = calculates the probability the dealer busts from their visible card
        // Used in STAND recommendations to explain why standing is correct
        // A weak dealer card = high bust probability = good reason to stand
        static double CalculateDealerBustProbability(int dealerVisibleValue)
        {
            Dictionary<int, int> cardWeights = new Dictionary<int, int>()
            {
                { 2,  1 }, { 3,  1 }, { 4,  1 }, { 5,  1 },
                { 6,  1 }, { 7,  1 }, { 8,  1 }, { 9,  1 },
                { 10, 4 },
                { 11, 1 }
            };

            int totalWeight = 13;
            double dealerBusts = 0;
            double totalOutcomes = 0;

            foreach (var holeEntry in cardWeights)
            {
                int holeValue = holeEntry.Key;
                int holeWeight = holeEntry.Value;

                int dealerAces = 0;
                if (dealerVisibleValue == 11) dealerAces++;
                if (holeValue == 11) dealerAces++;

                int dealerTotal = dealerVisibleValue + holeValue;

                while (dealerTotal > 21 && dealerAces > 0)
                {
                    dealerTotal -= 10;
                    dealerAces--;
                }

                // simulate dealer drawing to 17, track bust outcomes
                SimulateDealerBust(dealerTotal, dealerAces,
                                   holeWeight, totalWeight, cardWeights,
                                   ref dealerBusts, ref totalOutcomes);
            }

            if (totalOutcomes == 0) return 0;
            return Math.Round(dealerBusts / totalOutcomes * 100, 1);
        }   // closes CalculateDealerBustProbability

        // METHOD: SimulateDealerBust = recursive helper for CalculateDealerBustProbability
        // Mirrors SimulateDealerDraw but tracks bust outcomes instead of win outcomes
        // Walks every possible dealer draw sequence weighted by card probability
        // When the dealer reaches 17 or higher, checks if they busted (total > 21)
        // Adds the path weight to dealerBusts if bust, totalOutcomes regardless
        static void SimulateDealerBust(int dealerTotal, int dealerAces,
                                        double weight, int totalWeight,
                                        Dictionary<int, int> cardWeights,
                                        ref double dealerBusts, ref double totalOutcomes)
        {
            if (dealerTotal >= 17)
            {
                totalOutcomes += weight;
                if (dealerTotal > 21)
                    dealerBusts += weight;
                return;
            }

            foreach (var entry in cardWeights)
            {
                int cardValue = entry.Key;
                int cardWeight = entry.Value;
                int newTotal = dealerTotal + cardValue;
                int newAces = dealerAces + (cardValue == 11 ? 1 : 0);

                while (newTotal > 21 && newAces > 0)
                {
                    newTotal -= 10;
                    newAces--;
                }

                SimulateDealerBust(newTotal, newAces,
                                   weight * cardWeight / totalWeight,
                                   totalWeight, cardWeights,
                                   ref dealerBusts, ref totalOutcomes);
            }
        }   // closes SimulateDealerBust

        // METHOD: PrintQuerySummary = runs 3 live SQL queries against blackjack.db at session end
        // Same reveal pattern as dealer card reveal
       
        // Query 1: current session metrics — hands, win rate, net profit, recommendation adherence
        // Query 2: strategy recommendation performance — win rate followed vs ignored (lifetime)
        // Query 3: decision latency analysis — win rate by decision speed bucket (lifetime)
        // Footer Query: total records, sessions, and players in the database across all time = giving user immediate context for statiscal weight of the numbers above
        static void PrintQuerySummary(int sessionID, string dbPath, int netProfit)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n╔══════════════════════════════════════╗");
            Console.WriteLine("║     LIVE DATABASE ANALYTICS          ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.ResetColor();

            // loading animation — makes the query process visible on screen recordings
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("\nAggregating session data from blackjack.db");
            Thread.Sleep(600);
            Console.Write(".");
            Thread.Sleep(400);
            Console.Write(".");
            Thread.Sleep(400);
            Console.WriteLine(".\n");
            Console.ResetColor();
            Thread.Sleep(400);

            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();

                // ── QUERY 1: CURRENT SESSION METRICS ──────────────────────────────
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("CURRENT SESSION METRICS");
                Console.ResetColor();

                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"
                        SELECT
                            COUNT(*) AS TotalHands,
                            ROUND(100.0 * SUM(CASE WHEN Result = 'Win' THEN 1 ELSE 0 END)
                                / COUNT(*), 1) AS WinRate,
                            ROUND(100.0 * SUM(CASE WHEN RecommendationFollowed = 1 THEN 1 ELSE 0 END)
                                / NULLIF(SUM(CASE WHEN RecommendedAction != 'NONE' THEN 1 ELSE 0 END), 0), 1)
                                AS FollowRate
                        FROM GameSessions
                        WHERE SessionID = @sessionID";
                    cmd.Parameters.AddWithValue("@sessionID", sessionID);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int hands = reader.GetInt32(0);
                            double winRate = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                            double followRate = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);

                            Thread.Sleep(300);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write("  Hands played             : ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(hands);

                            Thread.Sleep(300);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write("  Win rate                 : ");
                            Console.ForegroundColor = winRate >= 50 ? ConsoleColor.Green : ConsoleColor.Red;
                            Console.WriteLine(winRate + "%");

                            Thread.Sleep(300);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write("  Net profit               : ");
                            Console.ForegroundColor = netProfit >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
                            Console.WriteLine((netProfit >= 0 ? "+" : "") + netProfit + " tokens");

                            Thread.Sleep(300);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write("  Recommendations followed : ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(followRate + "%");
                        }
                    }
                }

                Thread.Sleep(300);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Query complete\n");
                Console.ResetColor();

                // ── QUERY 2: STRATEGY RECOMMENDATION PERFORMANCE ──────────────────
                Thread.Sleep(500);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("STRATEGY RECOMMENDATION PERFORMANCE");
                Console.ResetColor();

                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"
                        SELECT
                            CASE WHEN RecommendationFollowed = 1 THEN 'Followed' ELSE 'Ignored ' END AS Compliance,
                            COUNT(*) AS Games,
                            ROUND(100.0 * SUM(CASE WHEN Result = 'Win' THEN 1 ELSE 0 END)
                                / COUNT(*), 1) AS WinRate
                        FROM GameSessions
                        WHERE RecommendedAction != 'NONE' AND StrategyMode = 'On'
                        GROUP BY RecommendationFollowed
                        ORDER BY RecommendationFollowed DESC";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string compliance = reader.GetString(0);
                            int games = reader.GetInt32(1);
                            double winRate = reader.GetDouble(2);

                            Thread.Sleep(400);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write("  " + compliance + " : ");
                            Console.ForegroundColor = compliance.Trim() == "Followed"
                                ? ConsoleColor.Green : ConsoleColor.Red;
                            Console.Write(winRate + "% win rate");
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine("  (" + games + " hands)");
                        }
                    }
                }

                Thread.Sleep(300);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Query complete\n");
                Console.ResetColor();

                // ── QUERY 3: DECISION LATENCY ANALYSIS ────────────────────────────
                Thread.Sleep(500);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("DECISION LATENCY ANALYSIS");
                Console.ResetColor();

                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"
                        SELECT
                            CASE
                                WHEN HandDurationSeconds <= 5  THEN 'Fast     (0-5s) '
                                WHEN HandDurationSeconds <= 15 THEN 'Moderate (6-15s)'
                                ELSE                                'Slow     (15s+) '
                            END AS Speed,
                            COUNT(*) AS Games,
                            ROUND(100.0 * SUM(CASE WHEN Result = 'Win' THEN 1 ELSE 0 END)
                                / COUNT(*), 1) AS WinRate
                        FROM GameSessions
                        GROUP BY Speed
                        ORDER BY WinRate DESC";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string speed = reader.GetString(0);
                            int games = reader.GetInt32(1);
                            double winRate = reader.GetDouble(2);

                            Thread.Sleep(400);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write("  " + speed + " : ");
                            Console.ForegroundColor = winRate >= 55
                                ? ConsoleColor.Green
                                : winRate >= 45
                                    ? ConsoleColor.Yellow
                                    : ConsoleColor.Red;
                            Console.Write(winRate + "%");
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine("  (" + games + " hands)");
                        }
                    }
                }

                Thread.Sleep(300);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Query complete");
                Console.ResetColor();

                // ── FOOTER: TOTAL RECORDS ──────────────────────────────────────────
                Thread.Sleep(500);

                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"
                        SELECT COUNT(*) AS Records,
                               COUNT(DISTINCT SessionID) AS Sessions,
                               COUNT(DISTINCT PlayerID)  AS Players
                        FROM GameSessions";

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int records = reader.GetInt32(0);
                            int sessions = reader.GetInt32(1);
                            int players = reader.GetInt32(2);

                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine("\n══════════════════════════════════════");
                            Console.Write("  Queried from ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write(records + " records");
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write(" across ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write(sessions + " sessions");
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write(" and ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write(players + " players");
                            Console.WriteLine();
                            Console.ResetColor();
                        }
                    }
                }
            }
        }   // closes PrintQuerySummary

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//



        static void Main() // This is the entry point of every C# program, when you run your program, C# scans your code looking specifically for a method called Main (C# STARTS EXECUTING HERE)
        {
            Console.Clear();

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
            while (player.Username.Length < 3 || player.Username.Length > 20 || player.Username.Contains(" "))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Username must be between 3 and 20 characters and contain no spaces. Try again.");
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

            // database path - lives next to the .exe just like the CSV did
            // Phase 3: SQLite replaces CSV as the primary data store
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blackjack.db");

            // initialize the database - creates file and tables if they don't exist
            // safe to call every run - IF NOT EXISTS prevents overwriting existing data
            InitializeDatabase(dbPath);

            // capture OS version once per session — same for all hands
            string osVersion = Environment.OSVersion.ToString();

            // create one GameStats instance for the whole session
            // every hand will update these counters + then they get printed at the end 
            GameStats stats = new GameStats();

            // RegisterOrLoginPlayer queries the Players table for this username
            // returning player = loads their stored balance and longest win streak
            // new player = inserts a fresh row and returns 100 as starting balance

            int currentWinStreak = 0;

            var (tokenBalance, playerID, longestWinStreak) = RegisterOrLoginPlayer(
                player.Username, playerAge, loginTime, dbPath);

            tokenBalance = CheckDailyBonusDB(player.Username, tokenBalance, loginTime, dbPath, out double hoursUntilBonus);
            // 'out double hoursUntilBonus' declares the variable AND receives the value in one line
            // same pattern as 'out int balance' in LoadPlayerBalance
            // after this line, hoursUntilBonus holds either 0 (bonus awarded or new player)
            // or the decimal hours remaining until their next bonus

            int sessionStartBalance = tokenBalance;
            // snapshot taken after daily bonus is applied
            // used at session end to calculate NetProfit = endBalance - sessionStartBalance

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Session ID           : " + sessionID);
            Console.WriteLine("Session started      : " + loginTime);
            Console.WriteLine("Data saving to       : blackjack.db\n");
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
            Console.WriteLine("║  Get live blackjack suggestions      ║");
            Console.WriteLine("║  during gameplay.                    ║");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("╠══════════════════════════════════════╣");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("║  [1] ON  - enable suggestions        ║");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("║  [2] OFF - no strategy assistance    ║");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.ResetColor();

            ConsoleKeyInfo strategyKey = Console.ReadKey(true);
            bool strategyOn = strategyKey.Key == ConsoleKey.D1 || strategyKey.Key == ConsoleKey.NumPad1;
            // D1 = the 1 key on the main keyboard, NumPad1 = the 1 key on the number pad
            // either 1 key turns suggestions ON, anything else turns them OFF

            stats.StrategyModeOn = strategyOn;
            // store the choice in GameStats so it appears in the end of session summary

            Console.ForegroundColor = strategyOn ? ConsoleColor.Green : ConsoleColor.Yellow;
            // ternary operator - if strategyOn is true use Green, otherwise Yellow
            // condition ? valueIfTrue : valueIfFalse
            Console.WriteLine(strategyOn
                ? "Basic strategy suggestions ON.\n"
                : "Basic strategy suggestions OFF.\n");
            Console.ResetColor();

            // INSERT session row now — before first hand is dealt
            // EndTime, TotalHands, EndBalance, NetProfit filled in at session end
            InsertSessionRecord(sessionID, playerID, player.Username,
                                loginTime, tokenBalance, dbPath);

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
                // HAND VARIABLES 

                int playerTotal = 0;
                int dealerTotal = 0;
                int numberOfDraws = 0;
                bool gameOver = false;
                bool overrodeSuggestion = false;
                bool warningActive = false;
                bool lowStandWarningShown = false;
                int playerAces = 0;

                DateTime handStartTime = DateTime.Now;
                // captures the moment the hand begins
                // used to calculate HandDurationSeconds when the hand resolves

                List<Card> playerHand = new List<Card>();
                // stores every card the player has been dealt this hand = used to print the full hand display after each draw
                List<Card> dealerHand = new List<Card>();
                // stores every card the dealer has been dealt this hand

                // overrodeSuggestion declared here so it is accessible both in the draw branch where it gets set
                // AND outside the draw branch where it gets written to the SessionRecord

                // playerAces tracks how many Aces in the player's hand are currently counted as 11
                // used for soft Ace handling - if the player draws and busts but has a soft Ace,
                // the Ace drops from 11 to 1 instead of causing an immediate bust

                // BETTING PROMPT = player must bet BEFORE seeing their cards
                // minimum of 5 tokens, maximum of 100 tokens, cannot exceed their balance
                int currentBet = 0;
                bool validBet = false;
                bool aceDropped = false;
                // true when at least one Ace has been converted from 11 to 1
                // used to show the player that their Ace is counting as 1
                // strategy recommendation tracking — reset each hand
                string recommendedAction = "NONE";
                bool recommendationFollowed = false;
                string riskLevel = "NONE";
                double dealerWinProbability = 0.0;


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

                stats.TotalGames++;
                // incremented AFTER exit check so typing exit does not count as a hand

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

                playerHand.Add(openCard1);
                playerHand.Add(openCard2);
                // add player's two starting cards to the playerHand list = used for displaying cards

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
                    aceDropped = true;
                }

                // DEALER'S TWO STARTING CARDS
                Card dealerVisibleCard = DealCard(deck);
                Card dealerHoleCard = DealCard(deck);
                dealerHand.Add(dealerVisibleCard);
                dealerHand.Add(dealerHoleCard);

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

                // snapshot opening totals BEFORE any draws
                // used for analytics — what did the player and dealer start with
                int openingPlayerTotal = playerTotal;
                int openingDealerTotal = dealerTotal;

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
                PrintPlayerHand(playerHand, aceDropped);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Your total:   " + playerTotal + "\n");
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
                    Console.Write("Dealer revealing hole card...");
                    Thread.Sleep(1500);
                    // clear the revealing line — hole card is implied by its position in the hand
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, Console.CursorTop);
                    PrintDealerHand(dealerHand);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Dealer total: " + dealerTotal + "\n");
                    Console.ResetColor();
                    Thread.Sleep(800);

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

                        dealerHand.Add(dealerCard);
                        Console.ForegroundColor = ConsoleColor.White;
                        PrintDealerHand(dealerHand);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Dealer total: " + dealerTotal + "\n");
                        Console.ResetColor();
                        Thread.Sleep(1000);
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

                    if (openingResult == "Win")
                    {
                        currentWinStreak++;
                        if (currentWinStreak > longestWinStreak)
                        {
                            longestWinStreak = currentWinStreak;
                            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
                            {
                                connection.Open();
                                using (var cmd = new SQLiteCommand(connection))
                                {
                                    cmd.CommandText = @"
                                        UPDATE Players
                                        SET LongestWinStreak = @streak
                                        WHERE Username = @username";
                                    cmd.Parameters.AddWithValue("@streak", longestWinStreak);
                                    cmd.Parameters.AddWithValue("@username", player.Username);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    else { currentWinStreak = 0; }

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
                    openingRecord.DealerVisibleCard = dealerVisibleCard.Name;
                    openingRecord.DealerVisibleValue = dealerVisibleValue;
                    openingRecord.OpeningPlayerTotal = openingPlayerTotal;
                    openingRecord.OpeningDealerTotal = openingDealerTotal;
                    openingRecord.PlayerHandWasSoft = aceDropped;
                    openingRecord.HandDurationSeconds = (int)(DateTime.Now - handStartTime).TotalSeconds;
                    openingRecord.OSVersion = osVersion;
                    openingRecord.RecommendedAction = recommendedAction;
                    openingRecord.RecommendationFollowed = recommendationFollowed;
                    openingRecord.RiskLevel = riskLevel;
                    openingRecord.DealerWinProbability = dealerWinProbability;

                    
                    InsertGameRecord(openingRecord, dbPath, playerID);

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
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("\n══════════════════════════════════════");
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Place a bet to continue, or type 'exit' to see your session summary.");
                        Console.ResetColor();
                    }

                    // skip the game loop entirely - hand is already resolved
                    gameOver = true;

                }
                // check if dealer has 21 on opening deal (dealer natural)
                // if so resolve immediately - player does not get to draw
                // this mirrors real casino rules where dealer checks for natural before play begins
                if (dealerTotal == 21 && !gameOver)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("\n── Dealer's turn ──────────────────────");
                    Console.ResetColor();

                    while (Console.KeyAvailable) Console.ReadKey(true);

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("Dealer revealing hole card...");
                    Thread.Sleep(1500);
                    // clear the revealing line — hole card is implied by its position in the hand
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, Console.CursorTop);
                    PrintDealerHand(dealerHand);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Dealer total: " + dealerTotal + "\n");
                    Console.ResetColor();
                    Thread.Sleep(800);

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Dealer has 21! Hand over.\n");
                    Console.ResetColor();
                    Thread.Sleep(800);

                    string naturalResult = DetermineWinner(playerTotal, dealerTotal);

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(player.Username + "'s total: " + playerTotal);
                    Console.WriteLine("Dealer's total:  " + dealerTotal);

                    if (naturalResult == "Loss")
                    {
                        tokenBalance -= currentBet;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n★  GAME OVER: " + naturalResult + "  ★\n");
                        Console.WriteLine("You lost " + currentBet + " tokens. Balance: " + tokenBalance);
                        Console.ResetColor();
                        stats.DealerWins++;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\n★  GAME OVER: " + naturalResult + "  ★\n");
                        Console.WriteLine("Tie — bet returned. Balance: " + tokenBalance);
                        Console.ResetColor();
                        stats.Ties++;
                    }
                    // dealer natural is always Loss or Tie — never a Win
                    // so streak always resets here
                    currentWinStreak = 0;

                    SessionRecord naturalRecord = new SessionRecord();
                    naturalRecord.SessionID = sessionID;
                    naturalRecord.Username = player.Username;
                    naturalRecord.PlayerAge = playerAge;
                    naturalRecord.LoginTime = loginTime;
                    naturalRecord.GameNumber = stats.TotalGames;
                    naturalRecord.PlayerTotal = playerTotal;
                    naturalRecord.DealerTotal = dealerTotal;
                    naturalRecord.Result = naturalResult;
                    naturalRecord.PlayerBusted = false;
                    naturalRecord.DealerBusted = false;
                    naturalRecord.NumberOfDraws = 0;
                    naturalRecord.BetAmount = currentBet;
                    naturalRecord.TokensBefore = tokensBefore;
                    naturalRecord.TokensAfter = tokenBalance;
                    naturalRecord.StrategyMode = strategyOn ? "On" : "Off";
                    naturalRecord.OverrodeSuggestion = false;
                    naturalRecord.DoubledDown = false;
                    naturalRecord.DealerVisibleCard = dealerVisibleCard.Name;
                    naturalRecord.DealerVisibleValue = dealerVisibleValue;
                    naturalRecord.OpeningPlayerTotal = openingPlayerTotal;
                    naturalRecord.OpeningDealerTotal = openingDealerTotal;
                    naturalRecord.PlayerHandWasSoft = aceDropped;
                    naturalRecord.HandDurationSeconds = (int)(DateTime.Now - handStartTime).TotalSeconds;
                    naturalRecord.OSVersion = osVersion;
                    naturalRecord.RecommendedAction = recommendedAction;
                    naturalRecord.RecommendationFollowed = recommendationFollowed;
                    naturalRecord.RiskLevel = riskLevel;
                    naturalRecord.DealerWinProbability = dealerWinProbability;

                   
                    InsertGameRecord(naturalRecord, dbPath, playerID);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Hand saved.\n");
                    Console.ResetColor();

                    if (tokenBalance < 5)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\nNot enough tokens to continue. Game over.");
                        Console.ResetColor();
                        sessionActive = false;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("\n══════════════════════════════════════");
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Place a bet to continue, or type 'exit' to see your session summary.");
                        Console.ResetColor();
                    }

                    gameOver = true;
                }
                // STRATEGY WARNING: HIGH OPENING HAND
                // if strategy mode is on and the player's opening two cards total 17 or higher,
                // warn them before the game loop starts - they haven't drawn yet but they should
                // know that hitting from this total carries significant bust risk
                // same bust percentage logic as the mid-hand warning
                if (strategyOn && !gameOver)
                {
                    recommendedAction = GetStrategyRecommendation(playerTotal, dealerVisibleValue);
                    dealerWinProbability = CalculateDealerWinProbability(playerTotal, dealerVisibleValue);
                    double bustPct = CalculateBustChanceDouble(playerTotal);
                    riskLevel = GetRiskLevel(bustPct);
                    PrintStrategyRecommendation(recommendedAction, bustPct,
                                                dealerWinProbability, dealerVisibleValue, playerTotal);
                    warningActive = true;
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

                // flush any keypresses buffered during the dealer reveal or animations
                // without this a buffered Enter from watching the dealer draw
                // gets consumed instantly as a hit on the next hand
                while (Console.KeyAvailable) Console.ReadKey(true);

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

                    string input = "";
                    if (keypress.Key == ConsoleKey.Escape)
                        input = "QUIT";
                    else if (keypress.Key == ConsoleKey.N)
                        input = "N";
                    else if (keypress.Key == ConsoleKey.D && numberOfDraws == 0 && !doubledDown)
                        input = "DOUBLE";
                    else if (keypress.Key == ConsoleKey.Enter)
                        input = "HIT";
                    // any other key is ignored — only valid keys trigger actions
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

                            // write a forfeit record to GameSessions so the hand is not missing from analytics — forfeits are valid behavioral data points
                            // TokensAfter reflects the post-forfeit balance so the Players table update inside InsertGameRecord stays accurate

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
                            forfeitRecord.DealerVisibleCard = dealerVisibleCard.Name;
                            forfeitRecord.DealerVisibleValue = dealerVisibleValue;
                            forfeitRecord.OpeningPlayerTotal = openingPlayerTotal;
                            forfeitRecord.OpeningDealerTotal = openingDealerTotal;
                            forfeitRecord.PlayerHandWasSoft = aceDropped;
                            forfeitRecord.HandDurationSeconds = (int)(DateTime.Now - handStartTime).TotalSeconds;
                            forfeitRecord.OSVersion = osVersion;
                            forfeitRecord.RecommendedAction = recommendedAction;
                            forfeitRecord.RecommendationFollowed = recommendationFollowed;
                            forfeitRecord.RiskLevel = riskLevel;
                            forfeitRecord.DealerWinProbability = dealerWinProbability;

                            
                            InsertGameRecord(forfeitRecord, dbPath, playerID);

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
                        // Player presses N on a total of 12 or higher = no warning = hand ends 
                        // Player presses N on a total of 11 with strategy mode on = warning shows = hand does NOT end immediately 
                        // Player press N again = no warnning = total is still 11 or lower but they confirmed = hand ends 
                        // Player presses Enter = draw branch runs normally
                        if (strategyOn && playerTotal <= 11 && !lowStandWarningShown)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            double lowStandDealerWinProb = CalculateDealerWinProbability(playerTotal, dealerVisibleValue);
                            Console.WriteLine("💡 Tip! HIT. Standing on " + playerTotal +
                                              " gives the dealer a " + (int)lowStandDealerWinProb +
                                              "% chance of winning.");
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("   [ENTER] HIT  ");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("[N] STAND  ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("[ESC] QUIT \n");
                            Console.ResetColor();
                            lowStandWarningShown = true;
                        }
                        else
                        {
                            // no warning needed - player standing on a reasonable total
                            // end the hand immediately
                            gameOver = true;
                        }
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
                            playerHand.Add(doubleCard);
                            int doubleValue = cardValues[doubleCard.Name];
                            if (doubleCard.Name == "Ace") playerAces++;

                            playerTotal += doubleValue;
                            // add the double down card value to the running total

                            // soft Ace adjustment
                            while (playerTotal > 21 && playerAces > 0)
                            {
                                playerTotal -= 10;
                                playerAces--;
                                aceDropped = true;
                            }
                            Console.WriteLine();
                            PrintPlayerHand(playerHand, aceDropped);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Your total:     " + playerTotal + "\n");
                            Console.ResetColor();

                            // hand ends automatically after double down
                            gameOver = true;
                        }
                    }
                    else if (input == "HIT")
                            { 
                            // player pressed Enter - draw one card for the player only
                               // dealer does NOT draw here - dealer draws after player stands
                               // this matches real blackjack dealer rules

                            numberOfDraws++;

                            // if a warning was active from the previous draw, this draw is an override
                            // the player was warned and chose to draw anyway
                            if (warningActive)
                            {
                                overrodeSuggestion = true;
                                stats.SuggestionsOverridden++;
                                warningActive = false;
                                // reset warningActive - the override is recorded, warning consumed
                            }

                            // PLAYER DRAWS

                            Card playerCard = DealCard(deck);
                            playerHand.Add(playerCard);
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
                            aceDropped = true;
                            }

                        // print the card and total ONCE right after the draw
                        // the duplicate in the original was caused by printing here AND again after the strategy warning
                        PrintPlayerHand(playerHand, aceDropped);
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
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Bust! You went over 21.\n");
                            Console.ResetColor();
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
                        if (strategyOn && !gameOver)
                        {
                            recommendedAction = GetStrategyRecommendation(playerTotal, dealerVisibleValue);
                            dealerWinProbability = CalculateDealerWinProbability(playerTotal, dealerVisibleValue);
                            double bustPct = CalculateBustChanceDouble(playerTotal);
                            riskLevel = GetRiskLevel(bustPct);
                            PrintStrategyRecommendation(recommendedAction, bustPct,
                                                        dealerWinProbability, dealerVisibleValue,playerTotal);
                            warningActive = true;
                        }

                    }   // closes draw branch


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
                        Thread.Sleep(1500);
                        // clear the revealing line — hole card is implied by its position in the hand
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, Console.CursorTop);
                        PrintDealerHand(dealerHand);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Dealer total: " + dealerTotal + "\n");
                        Console.ResetColor();
                        Thread.Sleep(800);

                        // dealer always draws to 17 regardless of whether player busted
                        // this ensures DealerTotal in the CSV reflects the actual final hand
                        // and matches real casino rules where the dealer always completes their hand
                        int dealerAces = dealerAcesStart;

                        while (dealerTotal < 17)
                        {
                            Card dealerCard = DealCard(deck);
                            int dealerCardValue = cardValues[dealerCard.Name];
                            if (dealerCard.Name == "Ace") dealerAces++;

                            dealerTotal += dealerCardValue;

                            // soft Ace adjustment
                            while (dealerTotal > 21 && dealerAces > 0)
                            {
                                dealerTotal -= 10;
                                dealerAces--;
                            }
                            dealerHand.Add(dealerCard);
                            Console.ForegroundColor = ConsoleColor.White;
                            PrintDealerHand(dealerHand);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Dealer total: " + dealerTotal + "\n");
                            Console.ResetColor();
                            Thread.Sleep(1000);
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

                        if (result == "Win")
                        {
                            currentWinStreak++;
                            if (currentWinStreak > longestWinStreak)
                            {
                                longestWinStreak = currentWinStreak;
                                using (var connection = new SQLiteConnection("Data Source=" + dbPath))
                                {
                                    connection.Open();
                                    using (var cmd = new SQLiteCommand(connection))
                                    {
                                        cmd.CommandText = @"
                                            UPDATE Players
                                            SET LongestWinStreak = @streak
                                            WHERE Username = @username";
                                        cmd.Parameters.AddWithValue("@streak", longestWinStreak);
                                        cmd.Parameters.AddWithValue("@username", player.Username);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                        else { currentWinStreak = 0; }

                        if (playerTotal > 21) stats.PlayerBusts++;
                        if (dealerTotal > 21) stats.DealerBusts++;

                        // determine whether the player followed the strategy recommendation
                        // STAND = followed if player did not draw beyond opening hand
                        // HIT = followed if player drew at least once
                        // NONE = strategy mode was off, no recommendation applicable
                        if (recommendedAction == "STAND")
                            recommendationFollowed = (numberOfDraws == 0);
                        else if (recommendedAction == "HIT")
                            recommendationFollowed = (numberOfDraws > 0);
                        else
                            recommendationFollowed = false;

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
                        record.StrategyMode = strategyOn ? "On" : "Off";
                        record.OverrodeSuggestion = overrodeSuggestion;
                        record.DoubledDown = doubledDown;
                        record.DealerVisibleCard = dealerVisibleCard.Name;
                        record.DealerVisibleValue = dealerVisibleValue;
                        record.OpeningPlayerTotal = openingPlayerTotal;
                        record.OpeningDealerTotal = openingDealerTotal;
                        record.PlayerHandWasSoft = aceDropped;
                        record.HandDurationSeconds = (int)(DateTime.Now - handStartTime).TotalSeconds;
                        record.OSVersion = osVersion;
                        record.RecommendedAction = recommendedAction;
                        record.RecommendationFollowed = recommendationFollowed;
                        record.RiskLevel = riskLevel;
                        record.DealerWinProbability = dealerWinProbability;

                       
                        InsertGameRecord(record, dbPath, playerID);

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
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine("\n══════════════════════════════════════");
                            Console.ResetColor();
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Place a bet to continue, or type 'exit' to see your session summary.");
                            Console.ResetColor();
                        }
                    }   // closes if (gameOver && sessionActive)

                }   // closes inner game loop (while !gameOver)

            }   // closes session loop (while sessionActive)

            // UPDATE session row with final stats now that session is complete
            string sessionEndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            UpdateSessionRecord(sessionID, sessionEndTime, stats.TotalGames,
                                tokenBalance, tokenBalance - sessionStartBalance, dbPath);

            // UPDATE player's lifetime stats now that session is complete
            UpdatePlayerLifetimeStats(player.Username, stats.TotalGames,
                                       stats.PlayerWins, stats.StrategyModeOn, dbPath);

            PrintQuerySummary(sessionID, dbPath, tokenBalance - sessionStartBalance);
            // STEP 10 = END OF SESSION
            // both loops exited - session is over
            // simple menu gives the player options rather than just printing and closing
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n╔══════════════════════════════════════╗");
            Console.WriteLine("║       SESSION COMPLETE               ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Thread.Sleep(200);
            Console.WriteLine("Final token balance      : " + tokenBalance);
            Thread.Sleep(150);
            Console.WriteLine("Strategy mode            : " + (stats.StrategyModeOn ? "On" : "Off"));
            Thread.Sleep(150);
            Console.WriteLine("Suggestions overridden   : " + stats.SuggestionsOverridden);
            Thread.Sleep(150);
            Console.WriteLine("Hands played             : " + stats.TotalGames);
            Thread.Sleep(150);
            Console.WriteLine("Wins                     : " + stats.PlayerWins);
            Thread.Sleep(150);
            Console.WriteLine("Losses                   : " + stats.DealerWins);
            Thread.Sleep(150);
            Console.WriteLine("Ties                     : " + stats.Ties);
            Thread.Sleep(150);
            Console.WriteLine("Player busts             : " + stats.PlayerBusts);
            Thread.Sleep(150);
            Console.WriteLine("Dealer busts             : " + stats.DealerBusts);
            Thread.Sleep(150);
            Console.WriteLine("Longest win streak       : " + longestWinStreak);
            Thread.Sleep(150);
            Console.WriteLine("Data saving to           : blackjack.db\n");
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

           

        }   // closes Main()

    }   // closes BlackjackGame

}   // closes namespace