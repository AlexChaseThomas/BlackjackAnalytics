using System;

// without 'using System':
// System.Console.WriteLine("Hello");

// with 'using System':
// Console.WriteLine("Hello"); 

using System.Collections.Generic;
// provides List <T> and Dictionary <K,V>
using System.IO;
// provides File.ReadAllLines() = reading card deck file 
using System.Linq;
// provides .ToArray (), .Count(),.Where() - data querying 

namespace AlexThomasBlackJackProject2026
{
    /* 
    
    // This program has two different distinct uses of classes:  #1. model classes or POCOs (Plain Old C# Objects) 
    //What is a class? 
    // Class = blueprint = defines what something is (its data) and what it can do (its methods). 
    // Nothing exists until you CREATE an instance of it using new 
    // For Example: 
    // This is the blueprint — nothing exists yet
 

    class BlackjackGame
    {
        // fields = what it HAS
        // methods = what it CAN DO
    }

    // This creates an actual object from the blueprint

    BlackjackGame myGame = new BlackjackGame(); 

    // Analogy - a class is the architectural drawing of a house while the new keyword actually builds a house from that drawing
    // ... you can build as many house as you want from the same drawing; each one is a separate instance

     */

    /*
  
     * This program uses two (2) distinct types of classes:
    
     * #1 Data Class (Model Classes or POCOs - Plain Old C# Objects) 
     * This type of class just holds data - no logic, no methods. 
  
     * ***** INTERVIEW PREP = this is an example of object-oriented programming for data roles *****
     
     */

    // PlayerInfo = a data class that stores who the player is 

    // Phase 2: redesigned PlayerInfo to remove PII security risk 
    // username system replaces first/last name; username acts as a primary key in the database, linking all session records
    // BirthYear removed entirely - DOB is entered for verification only and immediately discarded
    // only the calculated age integer is kept, stored separately as playerAge in Main()
    public class PlayerInfo
    {
        public string Username; // unique identifier chosen by the player
        // no DOB, no real name - no PII stored anywhere in this class
    }

    // SessionRecord is a data class that stores what happened during a hand - it gets created fresh at the end of every single hand, filled in with that hand's results, written to the CSV, and then thrown away. Next hand, a new one is created
    public class SessionRecord
    {
        // basic session information fields
        public int SessionID;
        public string Username;   // replaces Name - matches the new identity system
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

    /* #2 Logic Class (the main game class) 
     * This is where behavior lives - this class does things rather than just storing values like the model class 
        */

    class BlackjackGame // this type of class is only accessible within this file/namespace (default = "internal")
    {
        // Single shared Random instance for the entire class; declared at class level = all methods share it 
        // ***** BUG RISK ***** 
        // if two calls of the draw method happen close together in time, they can get the same seed and produce the same card twice in a row
        // one shared instance eliminates that problem entirely
        static Random rand = new Random();

        // DICTIONARY: cardValues = maps each card name to its point value 
        // REPLACES: if/else chains that appeared three (3) times in Phase 1
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

        // methods live here - draw (), SuitAssigner(), Main(), etc. 

        // SuitAssigner = method 
        // method = a named block of code that does one specific job - you write it once
        // and can call it as many times as you want from anywhere in the program

        // static = belongs to the class itself, not an object made from the class 

        // string before SuitAssigner = this method returns a string 

        // METHOD: SuitAssigner
        public static string SuitAssigner() // the () means this method takes no input, it doesn't need any information from the outside to do its job
        {
            // List <string> suits creates a List - a resizable ordered collection where every item must be a string. 
            // List <string> is a generic type parameter - it's how you tell the List what kind of items (data type) it will hold. 
            List<string> suits = new List<string>() // () = initializer - a shortcut that lets you fill the list with values at the same moment you create it (rather than calling .Add() four times)
                { "Hearts", "Diamonds", "Clubs", "Spades" };

            // REMOVED: Random Number Generator 
            // rand is the shared class-level Random instance declared at the top of BlackjackGame
            // removing the local new Random() here prevents the seed collision bug

            return suits[rand.Next(0, suits.Count)];

            // suits [n] = retrives the item at index n 
            // suits [0] = hearts, suits [1] = diamonds...and so on

            // rand.Next(0, suits.Count) 
            // generates a random integer starting at 0 and going up to but not including suits.Count - .Count = a property on the List that tells you how many items it contains. counts the number of items in the list suits (so its equivalent to 4 as in next note)
            // since there are 4 suits, suits.Count is 4, so you get 0,1,2, or 3 - never 4. 
            // rand.Next() = putting this inside brackets = means you generate the random number and use it as 

            /* Alternative, less efficient method (for the purpose of this game)
             * 
             * List<string> suits = new List<string>(); // creates an empty list - no items yet
             * 
             * suits.Add("Hearts");
             * suits.Add("Diamonds");
             * suits.Add("Clubs");
             * suits.Add("Spades");
             * 
             * When you might use this alternative method:
             * 
             * The initializer {} that we used in our program ONLY works if you know all the values in your list the moment you make it (there are only 4 suits of cards so we used the initializer in this program)
             * 
             * Here is an example of when a method is being used to make a list that you do not know how long it will eventually be 
             * This method dynamically adds cards that have already been drawn to a list. 
             * You can't use the initializer there because you don't know what cards will be drawn ahead of time (its random).
             * 
             * List<string> drawnCards = new List<string>(); // starts empty 
             * 
             * drawnCards.Add(currentCard); // adds whatever card was just drawn
             */
        }   // closes SuitAssigner

        // METHOD: Draw
        static string Draw(string[] deck) // string[] = an array of strings 
                                          // whenever the code calls the method Draw(deck) it passes the entire deck array in and the method receives it under local name deck

        // Array vs. List

        // Array = fixed size at creation time, and cannot grow or shrink. 
        // Lists = dynamic 

        {
            // REMOVED: RANDOM NUMBER GENERATOR
            // rand is the shared class-level Random instance declared at the top of BlackjackGame

            // deck.Length = number of cards in the array; you use .Length for an array but .Count for a list - they do the same thing but the property name differs
            // int pick = declares a variable of type int to store the random index - storing it in a named variable (pick) before using it makes the code easier to read and debug; you could also print pick if you wanted to see what index was chosen.
            int pick = rand.Next(deck.Length); // randomly picks an index number from the deck and assigns it to the int variable pick
            return deck[pick]; // retrieves the card name at the assigned index
        }   // closes Draw

        // REMOVED: PasswordChecker
        // removed in Phase 2 - password was visible in source code and provided no real security
        // username system replaces it as the program entry point

        // REMOVED: CalculateAge method
        // removed in Phase 2 - age calculation is now done inline in Step 3 of Main()
        // the DOB is entered, age is calculated, DOB is immediately discarded
        // only playerAge (an integer) survives - no PII stored anywhere

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
            if (playerTotal > 21 && dealerTotal > 21) return "Tie";
            // both bust = tie - most specific case, must come first
            // if this wasn't first, the general bust checks below would catch it incorrectly

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
            // password gate removed in Phase 2 - password was visible in source code and provided no real security
            // username system replaces it as the program entry point
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
            // this will be fixed in Component 2 when SQLite is integrated
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

            // STEP 5 = BUILD THE DECK

            // string[] = a fixed array of strings 
            // the {} initializer fills the array immediately after creation
            // Why did we choose to use an array here instead of a list? Because our deck never needs to grow or shrink, it is fixed in size. 
            // element 0 = "Ace", element 1 = "King",...element 12 = "2"
            string[] deck = { "Ace", "King", "Queen", "Jack",
                               "10",  "9",    "8",     "7",
                               "6",   "5",    "4",     "3",  "2" };

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

                /// playerAces tracks how many Aces in the player's hand are currently counted as 11
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
                    // Component 2 will replace ReadLine betting with a proper ReadKey flow
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
                string openCard1 = Draw(deck);
                string openSuit1 = SuitAssigner();
                string openCard2 = Draw(deck);
                string openSuit2 = SuitAssigner();

                int openValue1 = cardValues[openCard1];
                int openValue2 = cardValues[openCard2];

                if (openCard1 == "Ace") playerAces++;
                if (openCard2 == "Ace") playerAces++;

                playerTotal = openValue1 + openValue2;

                // SOFT ACE ADJUSTMENT FOR OPENING HAND
                // if two Aces = 22, drop one to 1 = total becomes 12
                while (playerTotal > 21 && playerAces > 0)
                {
                    playerTotal -= 10;
                    playerAces--;
                }

                // DEALER'S TWO STARTING CARDS
                // first card = visible to the player
                // second card = hole card = hidden until dealer's turn
                string dealerVisibleCard = Draw(deck);
                string dealerVisibleSuit = SuitAssigner();
                string dealerHoleCard = Draw(deck);
                string dealerHoleSuit = SuitAssigner();

                int dealerVisibleValue = cardValues[dealerVisibleCard];
                int dealerHoleValue = cardValues[dealerHoleCard];

                // track Aces in dealer's starting hand for soft Ace handling later
                int dealerAcesStart = 0;
                if (dealerVisibleCard == "Ace") dealerAcesStart++;
                if (dealerHoleCard == "Ace") dealerAcesStart++;

                // dealer total starts with BOTH cards but player only sees one
                dealerTotal = dealerVisibleValue + dealerHoleValue;

                // SOFT ACE ADJUSTMENT FOR DEALER OPENING HAND
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
                Console.WriteLine("Dealer showing: " + dealerVisibleCard + " of " + dealerVisibleSuit);
                Console.WriteLine("Dealer hole card: [hidden]\n");
                Console.ResetColor();

                // show player's two starting cards
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("You were dealt: " + openCard1 + " of " + openSuit1 +
                                  " and " + openCard2 + " of " + openSuit2);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Your total:     " + playerTotal + "\n");
                Console.ResetColor();

                // check if player got blackjack on the opening deal
                if (playerTotal == 21)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("BLACKJACK! You hit 21 on the deal!\n");
                    Console.ResetColor();
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
                    Console.WriteLine("   Drawing now carries a " + bustChance + " chance of busting.");
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
                            string doubleCard = Draw(deck);
                            string doubleSuit = SuitAssigner();
                            int doubleValue = cardValues[doubleCard];

                            if (doubleCard == "Ace") playerAces++;
                            playerTotal += doubleValue;

                            // soft Ace adjustment
                            while (playerTotal > 21 && playerAces > 0)
                            {
                                playerTotal -= 10;
                                playerAces--;
                            }

                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("You drew:   " + doubleCard + " of " + doubleSuit);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Your total: " + playerTotal + "\n");
                            Console.ResetColor();

                            // hand ends automatically after double down
                            gameOver = true;
                        }
                    }
                    else
                        {
                        // player pressed Enter - draw one card for the player only
                        // dealer does NOT draw here - dealer draws after player stands
                        // this matches real blackjack dealer rules

                        numberOfDraws++;

                        // PLAYER DRAWS
                        // Draw(deck) returns one randomly selected card name as a string
                        // stored in playerCard so we can use it twice: once to look up the value, once to print it
                        string playerCard = Draw(deck);
                        string playerSuit = SuitAssigner();

                        // REMOVED: If/Else Chain
                        // cardValues[playerCard] looks up the point value in one line
                        // the Dictionary was declared at the top of BlackjackGame
                        int playerCardValue = cardValues[playerCard];

                        // track Aces separately for soft Ace handling
                        // Ace is always added as 11 first, then dropped to 1 if needed
                        if (playerCard == "Ace") playerAces++;

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
                        Console.WriteLine("You drew:     " + playerCard + " of " + playerSuit);
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
                        // runs once after the player's turn is completely finished
                        // dealer follows forced rules: hit on 16 or lower, stand on 17 or higher
                        // player busts skip this because result is already determined
                        if (playerTotal <= 21)
                        // only run dealer logic if the player didn't already bust
                        // if player busted, dealer wins regardless - no need to draw
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("── Dealer's turn ──────────────────────");
                            Console.ResetColor();

                            // dealer already has one visible card from the start of the hand
                            // dealer now continues drawing until reaching 17 or higher

                            // dealerAces tracks how many Aces are currently being counted as 11
                            // this allows soft Ace handling - if the dealer busts and has an Ace
                            // counted as 11, it drops to 1 instead (subtract 10 from total)
                            // example: Ace + Ace = 11 + 11 = 22, drops to 11 + 1 = 12, keeps drawing
                            // example: Ace + 6 = 17, stands (soft 17 rule - stands on all 17s here)

                            // dealerAcesStart was calculated when the opening hands were dealt
                            // it already accounts for both the visible card and the hole card
                            // we use it as the starting Ace count for the dealer draw phase
                            int dealerAces = dealerAcesStart;

                            // reveal the hole card now that the player's turn is over
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("Dealer reveals hole card: " + dealerHoleCard +
                                              " of " + dealerHoleSuit);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Dealer total: " + dealerTotal + "\n");
                            Console.ResetColor();

                            while (dealerTotal < 17)
                            {
                                string dealerCard = Draw(deck);
                                string dealerSuit = SuitAssigner();

                                // REMOVED: If/Else Chain
                                // Dictionary lookup - same pattern as player draw above
                                int dealerCardValue = cardValues[dealerCard];

                                // if this card is an Ace, count it as 11 for now
                                // and track that we have another Ace counted as 11
                                if (dealerCard == "Ace") dealerAces++;

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
                                Console.WriteLine("Dealer drew:  " + dealerCard + " of " + dealerSuit);
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Dealer total: " + dealerTotal + "\n");
                                Console.ResetColor();
                            }

                            // dealer has finished drawing
                            // dealerTotal is now either 17-21 (stood) or 22+ (bust)
                            // result determination below handles both cases correctly
                        }

                        // REMOVED: If/Else Chain
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

                        // playerTotal > 21 evaluates directly to true or false
                        // same concept as 'return input == "Password"' in PasswordChecker
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
                            Console.WriteLine("Type your bet to continue, or type 'exit' to quit.");
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
            Console.WriteLine("║         SESSION COMPLETE              ║");
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
                    // Component 2 will replace this with a proper game loop at the top level
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