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

    // PlayerInfo is a data class, it stores who the person is - it gets filled in once at the start of the session when the user enters their name and DOB, then the program references it throughout

    public class PlayerInfo // public class = accessible from anywhere 
    {
        public string FirstName; // user's first name
        public string LastName; // user's last name
        public DateTime DateOfBirth; // user's date of birth
    }

    // SessionRecord is a data class that stores what happened during a hand - it gets created fresh at the end of every single hand, filled in with that hand's results, written to the CSV, and then thrown away. Next hand, a new one is created
    public class SessionRecord
    {
        // basic session information fields
        public int SessionID;
        public string Name;
        public int PlayerAge;
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
            //REMOVED: RANDOM NUMBER GENERATOR
            // rand is the shared class-level Random instance declared at the top of BlackjackGame
       
            // deck.Length = number of cards in the array; you use .Length for an array but .Count for a list - they do the same thing but the property name differs
            // int pick = declares a variable of type int to store the random index - storing it in a named variable (pick) before using it makes the code easier to read and debug; you could also print pick if you wanted to see what index was chosen.
            int pick = rand.Next(deck.Length); // randomly picks an index number from the deck and assigns it to the int variable pick
            return deck[pick]; // retrieves the card name at the assigned index
        }   // closes Draw

        // METHOD: PasswordChecker
        // this method takes one string parameter called input and returns a bool (true or false) 
        // the expression input == "Password" is a comparison. == checks equality / compares two values. = assigns a value 
        // the password needs to be an exact match; meaning that the letter case matters
        static bool PasswordChecker(string input)
        {
            return input == "Password";
        }   // closes PasswordChecker

        // static is required here for the same reasons as the other methods - the runtime calls Main first without creating an instance of BlackjackGame yet
        // void means this method doesn't return anything - it is just a procedure 

        // METHOD: CalculateAge takes a date of birth and returns the person's current age as an int 
        // static = belongs to the class, no object needed to call it 
        // int before the name = this method returns a whole number 
        static int CalculateAge(DateTime dateOfBirth)
        {
            int age = DateTime.Today.Year - dateOfBirth.Year;

            // DateTime.Today = today's date with no time component 
            // .Year .Month .Day are properties on any DateTime object - they pull out individual pieces of data as plain integers

            // NOTE: the subtraction above is sometimes off by one (e.g. if the user's birthday has not happened yet this calendar year - they haven't actually turned that age yet)
            // example: today is May 5 2026, birthday is December 1 2005
            // 2026 - 2005 = 21, but they are actually still 20
            // this corrects it by subtracting 1 if the birthday is later in the year

            if (dateOfBirth.Month > DateTime.Today.Month || dateOfBirth.Month == DateTime.Today.Month && dateOfBirth.Day > DateTime.Today.Day)

            // says: if the user's birth month hasn't happened yet this calendar year
            // OR if we are currently IN their birth month but their actual birthday
            // hasn't occurred yet this month - then in either case, they haven't 
            // actually turned the age we calculated yet, so subtract 1 to correct it.

            // example 1: today is May 5 2026, birthday is August 1 2001
            // dateOfBirth.Month (8) > DateTime.Today.Month (5) = true
            // first condition alone makes the whole if true - age gets corrected

            // example 2: today is May 5 2026, birthday is May 20 2001
            // dateOfBirth.Month (5) > DateTime.Today.Month (5) = false - same month
            // so we check the second condition:
            // dateOfBirth.Month (5) == DateTime.Today.Month (5) = true
            // AND dateOfBirth.Day (20) > DateTime.Today.Day (5) = true
            // birthday is later this month so age gets corrected

            // example 3: today is May 5 2026, birthday is March 1 2001
            // dateOfBirth.Month (3) > DateTime.Today.Month (5) = false
            // birthday already passed this year - no correction needed, if block skipped

            // the || means OR - only ONE side needs to be true for the block to run
            // the && means AND - BOTH sides must be true for that condition to count
            // && is evaluated before || - so the right side is read as one complete thought
            {
                age--; // subtracts 1, the mirror image of ++ which adds 1
            }

            return age;
        }   // closes CalculateAge

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
        static int LoadPlayerBalance(string playerName, string csvPath)
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

                // fields[1] = Name column (second column, index 1)
                if (fields[1] == playerName)
                {
                    // fields[13] = TokensAfter column (14th column, index 13)
                    // int.TryParse safely converts the string to an int 
                    if (int.TryParse(fields[13], out int balance))
                        return balance;
                }
            }

            // player name not found anywhere in the file - brand new player 
            // i.e. if you run out of tokens, you can just change your name and it will create a new instance of you as a new player, even if you already have the csv
            return 100;
        }   // closes LoadPlayerBalance

        // METHOD: CheckDailyBonus
        // The CSV already records LoginTime for every row. To check if 24 hours have passed, we read the player's most recent LoginTime from the CSV and compare it to right now. If the difference is 24 hours or more, they get the bonus.
        static int CheckDailyBonus(string playerName, string csvPath, int currentBalance, out double hoursUntilBonus)
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

                // fields[1] = Name column - check this row belongs to our player first
                if (fields[1] == playerName)
                {
                    // fields[3] = LoginTime column (fourth column, index 3) 
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
                        "SessionID,Name,PlayerAge,LoginTime," +
                        "GameNumber,PlayerTotal,DealerTotal," +
                        "Result,PlayerBusted,DealerBusted,NumberOfDraws," +
                        "BetAmount,TokensBefore,TokensAfter," +
                        "StrategyMode,OverrodeSuggestion"
                    // ***** order must match the data row below - EXACTLY *****
                    );
                }

                // write the data row
                // each field separated by a comma = CSV (comma separated values)
                // this is all a CSV file is - plain text with commas between values
                writer.WriteLine(
                    record.SessionID + "," +
                    record.Name + "," +
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
                    record.OverrodeSuggestion
                );
            }   // StreamWriter closes and saves automatically here
        }   // closes WriteRecordToCSV

        static void Main() // This is the entry point of every C# program, when you run your program, C# scans your code looking specifically for a method called Main (C# STARTS EXECUTING HERE)
        {
            // STEP 1 = PASSWORD GATE

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║      C# BLACKJACK ANALYTICS          ║");
            Console.WriteLine("║         Alex Thomas  2026            ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Enter dealer password: ");
            Console.ResetColor();

            // Console.Write prints text without moving to a new line 

            // In Main, we ask the user to type the password for the game and send it over to the PasswordChecker() method to compare it against the real password.
            // PasswordChecker = boolean = comes back with either true or false 
            // The 'if' is part of Main(), it looks at what the password checker method returned (true or false) and, if it is false, prints "Incorrect password. Exiting." and shuts down the program. 
            // if the PasswordChecker() method used in Main() came back true, then just ignore this block and move onto the next thing. 

            // PasswordChecker() does the comparison, Main() decides what to do with the result.

            if (!PasswordChecker(Console.ReadLine().Trim())) // Console.ReadLine() pauses the program completely and waits for the user to type in their input and press Enter - whatever they type comes back to the program as a string 
                                                             // PasswordChecker(Console.ReadLine().Trim()) is METHOD CHAINING - the results of Console.ReadLine

            // The ! before PasswordChecker(...) means NOT 
            // If the password check does NOT return true - meaning the password was wrong - the block runs 

            // .Trim() is a method on any string that removes whitespace from the beginning and end. 
            // DEFENSIVE PROGRAMMING = .Trim() in this instance is being used to anticipate small ways users interact unexpectedly and handling them gracefully. 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Incorrect password. Exiting.");
                Console.ResetColor();
                return; // return inside Main() exits the entire program
                        // called a GUARD CLAUSE - it checks something at the top of the code, in this case, a password, and if it fails the check, it exits the program early 
                        // If you didn't have to do this, the rest of the program would have to live inside of an 'else' block - meaning IF the first condition passed, everything after would be after an else statement 
            }

            // STEP 2 = COLLECT PLAYER INFO

            PlayerInfo player = new PlayerInfo(); // declares a variable named player of type PlayerInfo; = new PlayerInfo()

            // PlayerInfo class was just a blueprint; after this line of code, player is a real object with three variables available to store data (e.g. FirstName, LastName, Age)

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Enter your first name: ");
            Console.ResetColor();
            player.FirstName = Console.ReadLine().Trim();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Enter your last name: ");
            Console.ResetColor();
            player.LastName = Console.ReadLine().Trim();

            // STEP 3 = DATE OF BIRTH VERIFICATION 

            bool validDate = false;

            // this is the flag variable that controls the while loop later 
            // starts off as false, that way the loop starts running immediately (i.e. if the loop repeats (starts again) if someone enters an invalid DOB, in order to get the loop to start the very first time, we set it as false and it runs as if someone just entered an invalid DOB)
            // only changes to true once we have a valid date AND the user is 21 years old 

            DateTime birthDate = new DateTime();

            // DateTime is a built-in C# data type that stores a complete date
            // new DateTime() creates an empty instance that gets filled by the user input inside the while loop

            while (!validDate) // while validDate is not true, keep looping until validDate flips to true 
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Enter your date of birth (MM/DD/YYYY): ");
                Console.ResetColor();
                string dobInput = Console.ReadLine().Trim();

                // DateTime.TryParse() tries to convert the string into a DateTime object
                // if the user types "abc" or "13/45/2000" it won't crash - just returns false
                // 'out birthDate' writes the converted result directly into birthDate

                if (!DateTime.TryParse(dobInput, out birthDate))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Something is not right. Please remember to use MM/DD/YYYY format and try again.");
                    Console.ResetColor();
                    continue;

                    // continue = skip everything below this line in the current pass through the loop and jump right back to the top of the while loop
                    // the user gets asked again
                    // DIFFERENT - if I had used break; here instead, it would have exited the loop entirely. 
                }

                // if the date was valid, we move to this point where we check the age 

                int age = CalculateAge(birthDate);

                // CalculateAge = method = takes the date of birth and returns the current age as an int 
                // we pull the logic out into its own method because we need it in more than one place = DRY (DON'T REPEAT YOURSELF)

                if (age < 21)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("You must be 21 or older to play. You are " + age + ".");
                    Console.ResetColor();
                    continue;
                }

                // if both the DOB is entered correctly and equates to an age of greater than 21, then both checks are passed

                validDate = true; // flips the while condition set at the start of the loop + exits the loop
                player.DateOfBirth = birthDate; // stored the verified DOB in the player object 
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nAge verified! Welcome, " + player.FirstName + " " + player.LastName + "!\n");
            Console.ResetColor();

            // STEP 4 = SESSION SETUP 

            // these variables will belong to the entire session, not any one hand 
            // these variables get created once here and are referenced throughout every hand below 

            // Generate a unique session ID automatically from the current timestamp

            // DateTime.Now = the exact current date AND time including hours, minutes, seconds

            // .Ticks = a property on DateTime that expresses the current moment as a very large integer 

            // Every tick = one ten-millionth of a second, so no two sessions ever share the same value 
            // % 1000000 = the modulo operator - gives you the remainder after dividing by 1000000
            // this trims the very large Ticks number down to a readable 6 digit number
            // example: 18374628390000000 % 1000000 = 390000 (just the last 6 digits)

            int sessionID = (int)(DateTime.Now.Ticks % 1000000);

            // record the exact moment this session started as a formatted string 
            // DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") converts the DateTime object into text 
            // yyyy = 4 digit year, MM = 2 digit month, dd = 2 digit day 
            // HH = 24 hour clock hour, mm = minutes, ss = seconds 
            // example output: "2026-05-05 14:32:01"

            string loginTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // build the CSV file path dynamically so it works on any computer 

            // AppDomain.CurrentDomain.BaseDirectory = the folder the program is currently running from 

            // Path.Combine() joins the folder path and filename together safely - handles the backslash between them automatically
            // ***** CSV will always appear right next to the .exe file *****

            string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blackjack_sessions.csv");

            // create one GameStats instance for the whole session
            // every hand will update these counters + then they get printed at the end 

            GameStats stats = new GameStats();

            // fullName combines first and last
            // used in SessionRecord and LoadPlayerBalance
            string fullName = player.FirstName + " " + player.LastName;

            // LoadPlayerBalance reads the CSV for this player's last recorded TokensAfter
            // if no record exists = returns 100 as starting balance
            int tokenBalance = LoadPlayerBalance(fullName, csvPath); // sets player balance to variable tokenBalance

            tokenBalance = CheckDailyBonus(fullName, csvPath, tokenBalance, out double hoursUntilBonus);
            // 'out double hoursUntilBonus' declares the variable AND receives the value in one line
            // same pattern as 'out int balance' in LoadPlayerBalance
            // after this line, hoursUntilBonus holds either 0 (bonus awarded or new player)
            // or the decimal hours remaining until their next bonus

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Session ID           : " + sessionID);
            Console.WriteLine("Session started      : " + loginTime);
            Console.WriteLine("GameStats saving to  : " + csvPath + "\n");
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
                // same guard clause pattern as the password gate
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
            // only flips to false when the player types N at the "play again?" prompt 

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

                // playerAces tracks how many Aces in the player's hand are currently counted as 11
                // used for soft Ace handling - if the player draws and busts but has a soft Ace,
                // the Ace drops from 11 to 1 instead of causing an immediate bust

                // DEALER STARTING HAND
                // dealer receives one visible card immediately at the start of the hand
                // this mirrors real blackjack where the player makes decisions based on visible dealer information
                // dealer still follows normal draw rules later during the resolve phase

                string dealerVisibleCard = Draw(deck);
                string dealerVisibleSuit = SuitAssigner();

                int dealerVisibleValue = 0;

                if (dealerVisibleCard == "Ace") dealerVisibleValue = 11;
                else if (dealerVisibleCard == "King") dealerVisibleValue = 10;
                else if (dealerVisibleCard == "Queen") dealerVisibleValue = 10;
                else if (dealerVisibleCard == "Jack") dealerVisibleValue = 10;
                else if (dealerVisibleCard == "10") dealerVisibleValue = 10;
                else if (dealerVisibleCard == "9") dealerVisibleValue = 9;
                else if (dealerVisibleCard == "8") dealerVisibleValue = 8;
                else if (dealerVisibleCard == "7") dealerVisibleValue = 7;
                else if (dealerVisibleCard == "6") dealerVisibleValue = 6;
                else if (dealerVisibleCard == "5") dealerVisibleValue = 5;
                else if (dealerVisibleCard == "4") dealerVisibleValue = 4;
                else if (dealerVisibleCard == "3") dealerVisibleValue = 3;
                else if (dealerVisibleCard == "2") dealerVisibleValue = 2;

                dealerTotal = dealerVisibleValue;
                // overrodeSuggestion declared here so it is accessible both in the draw branch where it gets set
                // AND outside the draw branch where it gets written to the SessionRecord

                stats.TotalGames++;

                // ++ = adds 1
                // Same as: stats.TotalGames = stats.TotalGames + 1
                // Incremented here so the game number is correct from the first hand 

                // BETTING PROMPT = player must bet BEFORE seeing their cards
                // minimum of 5 tokens, maximum of 100 tokens, cannot exceed their balance
                int currentBet = 0;
                bool validBet = false; // starts false to trigger while loop

                while (!validBet)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("Tokens: " + tokenBalance);
                    Console.Write("Place your bet (min 5, max " + Math.Min(100, tokenBalance) + "): ");
                    // Math.Min() returns the smaller of two values
                    // prevents player from betting more than they have
                    // if balance is 40 the max shown is 40, not 100
                    Console.ResetColor();

                    string betInput = Console.ReadLine().Trim();

                    if (!int.TryParse(betInput, out currentBet))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Please enter a whole number.");
                        Console.ResetColor();
                        continue;
                        // same continue pattern as DOB validation
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
                    // bet is valid - exit the betting loop
                }

                int tokensBefore = tokenBalance;
                // snapshot of the balance BEFORE this hand starts
                // stored in SessionRecord so we can see exactly what each hand lost or gained

                // game header box
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╔══════════════════════════════════════╗");
                Console.WriteLine($"║  GAME #{stats.TotalGames,-3}                          ║");
                Console.WriteLine("╠══════════════════════════════════════╣");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("║  [ENTER] Draw a Card                 ║");
                Console.WriteLine("║  [N]     Stand                       ║");
                Console.WriteLine("║  [ESC]   Quit + Forfeit Bet          ║");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╚══════════════════════════════════════╝");
                Console.ResetColor();

                // show the dealer's visible starting card before the player makes decisions
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Dealer showing: " + dealerVisibleCard + " of " + dealerVisibleSuit);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Dealer visible total: " + dealerTotal + "\n");
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
                            forfeitRecord.Name = fullName;
                            forfeitRecord.PlayerAge = CalculateAge(player.DateOfBirth);
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
                        // GUARD CLAUSE: prevent standing on 0
                        // a player must draw at least one card before they can stand
                        // standing on 0 is an invalid game state - it means the player
                        // pressed N without ever drawing, which produces meaningless data
                        // and can result in wins that were never earned
                        // this mirrors real blackjack where you always receive at least one card
                        if (numberOfDraws == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("You must draw at least one card before standing.");
                            Console.ResetColor();
                            // do NOT set gameOver = true
                            // the inner loop continues and waits for the player to draw
                        }
                        else
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
                                Console.WriteLine("   Press any key to stand anyway...");
                                Console.ResetColor();
                                Console.ReadKey(true);
                                stats.SuggestionsOverridden++;
                                // increment session counter each time a warning is acknowledged
                            }

                            // player chose to stand - end this hand only
                            // sessionActive stays true so the play again prompt still runs
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

                        // if/else chain assigns a point value to the drawn card
                        // C# checks top to bottom and stops at the first match

                        // ***** Phase 2 replaces this entire block with one Dictionary lookup line *****
                        int playerCardValue = 0;
                        if (playerCard == "Ace") playerCardValue = 11;
                        else if (playerCard == "King") playerCardValue = 10;
                        else if (playerCard == "Queen") playerCardValue = 10;
                        else if (playerCard == "Jack") playerCardValue = 10;
                        else if (playerCard == "10") playerCardValue = 10;
                        else if (playerCard == "9") playerCardValue = 9;
                        else if (playerCard == "8") playerCardValue = 8;
                        else if (playerCard == "7") playerCardValue = 7;
                        else if (playerCard == "6") playerCardValue = 6;
                        else if (playerCard == "5") playerCardValue = 5;
                        else if (playerCard == "4") playerCardValue = 4;
                        else if (playerCard == "3") playerCardValue = 3;
                        else if (playerCard == "2") playerCardValue = 2;

                        if (playerCard == "Ace") playerAces++;


                        playerTotal += playerCardValue;

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



                        // += adds the card value to the running total
                        // shorthand for playerTotal = playerTotal + playerCardValue
                        // accumulates each pass through the loop from its starting value of 0

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
                            Console.WriteLine("   Press any key to continue...");
                            Console.ResetColor();
                            Console.ReadKey(true);
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
                            int dealerAces = 0;

                            // check if the dealer's visible starting card was an Ace
                            // if so it was already counted as 11 so we need to track it
                            if (dealerVisibleCard == "Ace") dealerAces = 1;
                            // dealerVisibleCard is set at the top of the session loop
                            // dealerTotal was already set to dealerVisibleValue before the game loop

                            while (dealerTotal < 17)
                            {
                                string dealerCard = Draw(deck);
                                string dealerSuit = SuitAssigner();

                                int dealerCardValue = 0;
                                if (dealerCard == "Ace") dealerCardValue = 11;
                                else if (dealerCard == "King") dealerCardValue = 10;
                                else if (dealerCard == "Queen") dealerCardValue = 10;
                                else if (dealerCard == "Jack") dealerCardValue = 10;
                                else if (dealerCard == "10") dealerCardValue = 10;
                                else if (dealerCard == "9") dealerCardValue = 9;
                                else if (dealerCard == "8") dealerCardValue = 8;
                                else if (dealerCard == "7") dealerCardValue = 7;
                                else if (dealerCard == "6") dealerCardValue = 6;
                                else if (dealerCard == "5") dealerCardValue = 5;
                                else if (dealerCard == "4") dealerCardValue = 4;
                                else if (dealerCard == "3") dealerCardValue = 3;
                                else if (dealerCard == "2") dealerCardValue = 2;

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
                        // determine result - ordered most specific to most general
                        // order matters: a general condition placed first would swallow
                        // cases meant for a specific one below it
                        string result = "";
                        if (playerTotal > 21 && dealerTotal > 21) result = "Tie";
                        else if (playerTotal == 21) result = "Win";
                        else if (dealerTotal == 21) result = "Loss";
                        else if (playerTotal > 21) result = "Loss";
                        else if (dealerTotal > 21) result = "Win";
                        else if (playerTotal > dealerTotal) result = "Win";
                        else if (dealerTotal > playerTotal) result = "Loss";
                        else result = "Tie";
                        // final else = no condition = catches everything remaining
                        // the only case left at this point is equal totals = Tie

                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("\n" + player.FirstName + " " + player.LastName + "'s total: " + playerTotal);
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
                        record.Name = fullName;
                        record.PlayerAge = CalculateAge(player.DateOfBirth);
                        // CalculateAge called fresh rather than storing age
                        // we only store DateOfBirth in PlayerInfo - DRY principle
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

                        WriteRecordToCSV(record, csvPath);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Hand saved.\n");
                        Console.ResetColor();

                        // play again prompt - only shows if player still has tokens
                        if (sessionActive)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Play again? (Enter = yes, Escape = no)");
                            Console.ResetColor();

                            ConsoleKeyInfo playAgain = Console.ReadKey(true);
                            if (playAgain.Key == ConsoleKey.Escape)
                                sessionActive = false;
                            // flips outer loop to false
                            // inner loop already done because gameOver is true
                        }

                    }   // closes if (gameOver && sessionActive)

                }   // closes inner game loop (while !gameOver)

            }   // closes session loop (while sessionActive)


            // STEP 10 = END OF SESSION
            // both loops exited - session is over
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n╔══════════════════════════════════════╗");
            Console.WriteLine("║         SESSION COMPLETE              ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Final token balance      : " + tokenBalance);
            Console.WriteLine("Strategy mode            : " + (stats.StrategyModeOn ? "On" : "Off"));
            // ternary reads the bool and prints a readable label
            Console.WriteLine("Suggestions overridden   : " + stats.SuggestionsOverridden);
            Console.WriteLine("Full data saved to       : " + csvPath);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nThanks for playing. Press any key to exit.");
            Console.ResetColor();
            Console.ReadKey();

        }   // closes Main()

    }   // closes BlackjackGame

}   // closes namespace