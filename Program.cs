using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

// ─── Main Program ────────────────────────────────────────────────────────────

using var db = new AppDbContext();
db.Database.EnsureCreated();

List<(string Title, string Description, string Date)> newsList = new();

// ─── Navigation Functions ────────────────────────────────────────────────────

void Exit()
{
    Console.WriteLine("Goodbye!");
    Environment.Exit(0);
}

bool HandleNavigation(string input)
{
    if (input == "ex!")  { Exit(); }
    if (input == "back") { return true; }
    return false;
}

// ─── Input Validation Helpers ─────────────────────────────────────────────────

decimal ReadPositiveDecimal(string prompt)
{
    while (true)
    {
        Console.WriteLine(prompt);
        string input = Console.ReadLine();
        if (decimal.TryParse(input, out decimal value) && value > 0)
            return value;
        Console.WriteLine("Invalid amount. Must be a positive number. Please try again.");
    }
}

int ReadPositiveInt(string prompt)
{
    while (true)
    {
        Console.WriteLine(prompt);
        string input = Console.ReadLine();
        if (int.TryParse(input, out int value) && value > 0)
            return value;
        Console.WriteLine("Invalid number. Must be a positive whole number. Please try again.");
    }
}

DateTime ReadFutureDate(string prompt)
{
    DateTime maxDate = DateTime.Today.AddYears(150);

    while (true)
    {
        Console.WriteLine(prompt);
        string input = Console.ReadLine();

        if (!DateTime.TryParseExact(input, "dd.MM.yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out DateTime date))
        {
            Console.WriteLine("Invalid date format or date does not exist. Please use dd.MM.yyyy (e.g. 25.12.2030).");
            continue;
        }

        if (date < DateTime.Today)
        {
            Console.WriteLine("Date must be in the present or future. Please try again.");
            continue;
        }

        if (date > maxDate)
        {
            Console.WriteLine($"Date cannot be more than 150 years from today (max: {maxDate:dd.MM.yyyy}). Please try again.");
            continue;
        }

        return date;
    }
}

// ─── Auth Functions ──────────────────────────────────────────────────────────

User Login()
{
    while (true)
    {
        Console.WriteLine("\nEnter username:");
        string username = Console.ReadLine();

        Console.WriteLine("Enter password:");
        string password = Console.ReadLine();

        User user = db.Users.FirstOrDefault(u => u.Username == username);

        if (user != null && BC.Verify(password, user.PasswordHash))
        {
            Console.WriteLine($"Welcome back, {user.Username}!");
            return user;
        }

        Console.WriteLine("Invalid username or password. Please try again.");
    }
}

void Register()
{
    while (true)
    {
        Console.WriteLine("\nChoose a username:");
        string username = Console.ReadLine();

        if (db.Users.Any(u => u.Username == username))
        {
            Console.WriteLine("Username already taken. Please try another.");
            continue;
        }

        Console.WriteLine("Choose a password:");
        string password = Console.ReadLine();

        Console.WriteLine("Confirm password:");
        string confirm = Console.ReadLine();

        if (password != confirm)
        {
            Console.WriteLine("Passwords do not match. Please try again.");
            continue;
        }

        Console.WriteLine("Register as ( 1 ) User or ( 2 ) Admin:");
        string roleChoice = Console.ReadLine();
        string role = roleChoice == "2" ? "admin" : "user";

        User newUser = new User
        {
            Username     = username,
            PasswordHash = BC.HashPassword(password),
            Role         = role
        };

        db.Users.Add(newUser);
        db.SaveChanges();
        Console.WriteLine("Account created successfully. Please log in.");
        break;
    }
}

// ─── News Functions ──────────────────────────────────────────────────────────

void AddNews()
{
    Console.WriteLine("\nEnter news title:");
    string title = Console.ReadLine();

    Console.WriteLine("Enter news description:");
    string description = Console.ReadLine();

    string date = DateTime.Now.ToString("dd/MM/yyyy");

    newsList.Add((title, description, date));
    Console.WriteLine("News added successfully.");
}

void DisplayNews()
{
    if (newsList.Count == 0)
    {
        Console.WriteLine("No news available.");
        return;
    }

    for (int i = 0; i < newsList.Count; i++)
    {
        Console.WriteLine($"\n--- News {i + 1} ---");
        Console.WriteLine($"Title       : {newsList[i].Title}");
        Console.WriteLine($"Description : {newsList[i].Description}");
        Console.WriteLine($"Date        : {newsList[i].Date}");
    }
}

// ─── Goal Functions ───────────────────────────────────────────────────────────

void GoalMenu()
{
    while (true)
    {
        Console.WriteLine("\n( 1 ) - Set goal: Save X by date Y");
        Console.WriteLine("( 2 ) - Set goal: Passive income Z/month");
        Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
        string goalChoice = Console.ReadLine().ToLower();

        if (HandleNavigation(goalChoice)) { break; }

        if (goalChoice == "1")      { SetSavingsGoal(); }
        else if (goalChoice == "2") { SetRentGoal(); }
        else { Console.WriteLine("Invalid option. Please try again."); }
    }
}

void SetSavingsGoal()
{
    decimal  targetAmount = ReadPositiveDecimal("\nEnter target amount (X):");
    DateTime targetDate   = ReadFutureDate("Enter target date (dd.MM.yyyy):");
    SimulateAndEvaluateSavings(targetAmount, targetDate);
}

void SetRentGoal()
{
    decimal  monthlyRent = ReadPositiveDecimal("\nEnter desired monthly passive income (Z):");
    DateTime fromDate    = ReadFutureDate("Enter start date for passive income (dd.MM.yyyy):");
    SimulateAndEvaluateRent(monthlyRent, fromDate);
}

void SimulateAndEvaluateSavings(decimal target, DateTime targetDate)
{
    decimal startValue           = ReadPositiveDecimal("Enter starting portfolio value:");
    decimal monthlyContribution  = ReadPositiveDecimal("Enter monthly contribution:");

    int months          = Math.Max(0, ((targetDate.Year - DateTime.Today.Year) * 12) + targetDate.Month - DateTime.Today.Month);
    decimal meanReturn  = 0.005m;
    decimal volatility  = 0.02m;
    int simulations     = 1000;
    int successCount    = 0;
    Random rng          = new Random();

    for (int sim = 0; sim < simulations; sim++)
    {
        decimal value = startValue;
        for (int m = 0; m < months; m++)
        {
            decimal randomShock = (decimal)SampleNormal(rng, (double)meanReturn, (double)volatility);
            value = value * (1 + randomShock) + monthlyContribution;
        }
        if (value >= target) successCount++;
    }

    decimal probability = (decimal)successCount / simulations * 100;
    Console.WriteLine($"Likelihood of achieving said goal: {probability:F1} %");
}

void SimulateAndEvaluateRent(decimal rent, DateTime fromDate)
{
    decimal startValue  = ReadPositiveDecimal("Enter starting portfolio value:");
    int     months      = ReadPositiveInt("Enter desired duration of passive income in months:");

    decimal meanReturn  = 0.005m;
    decimal volatility  = 0.02m;
    int simulations     = 1000;
    int successCount    = 0;
    Random rng          = new Random();

    for (int sim = 0; sim < simulations; sim++)
    {
        decimal value = startValue;
        bool survived = true;
        for (int m = 0; m < months; m++)
        {
            decimal randomShock = (decimal)SampleNormal(rng, (double)meanReturn, (double)volatility);
            value = value * (1 + randomShock) - rent;
            if (value < 0) { survived = false; break; }
        }
        if (survived) successCount++;
    }

    decimal probability = (decimal)successCount / simulations * 100;
    Console.WriteLine($"Likelihood of passive income lasting the desired duration: {probability:F1} %");
}

// ─── Box-Muller normal distribution sampler ──────────────────────────────────

double SampleNormal(Random rng, double mean, double stddev)
{
    double u1            = 1.0 - rng.NextDouble();
    double u2            = 1.0 - rng.NextDouble();
    double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    return mean + stddev * randStdNormal;
}

// ─── Entry Point ─────────────────────────────────────────────────────────────

while (true)
{
    Console.WriteLine("\n( 1 ) - Login");
    Console.WriteLine("( 2 ) - Register");
    Console.WriteLine("( ex! ) - Exit");
    string entryChoice = Console.ReadLine().ToLower();

    if (HandleNavigation(entryChoice)) { continue; }

    if (entryChoice == "1")
    {
        User loggedInUser = Login();

        if (loggedInUser.Role == "admin")
        {
            while (true)
            {
                Console.WriteLine("\n( 1 ) - Manage market");
                Console.WriteLine("( 2 ) - Add news");
                Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
                string adminChoice = Console.ReadLine().ToLower();

                if (HandleNavigation(adminChoice)) { break; }

                if (adminChoice == "1")
                {
                    while (true)
                    {
                        Console.WriteLine("\n( 1 ) - Create active");
                        Console.WriteLine("( 2 ) - Manage active");
                        Console.WriteLine("( 3 ) - Deactivate active");
                        Console.WriteLine("( 4 ) - Display detail of active");
                        Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
                        string manageMarketChoice = Console.ReadLine().ToLower();

                        if (HandleNavigation(manageMarketChoice)) { break; }

                        if (manageMarketChoice == "1")      { Console.WriteLine("Create active selected"); }
                        else if (manageMarketChoice == "2") { Console.WriteLine("Manage active selected"); }
                        else if (manageMarketChoice == "3") { Console.WriteLine("Deactivate active selected"); }
                        else if (manageMarketChoice == "4") { Console.WriteLine("Display detail of active selected"); }
                        else { Console.WriteLine("Invalid option. Please try again."); }
                    }
                }
                else if (adminChoice == "2") { AddNews(); }
                else { Console.WriteLine("Invalid option. Please try again."); }
            }
        }
        else if (loggedInUser.Role == "user")
        {
            while (true)
            {
                Console.WriteLine("\n( 1 ) - Manage portfolio");
                Console.WriteLine("( 2 ) - Deposits and withdrawals");
                Console.WriteLine("( 3 ) - Growth simulation");
                Console.WriteLine("( 4 ) - News");
                Console.WriteLine("( 5 ) - Goal planning (saving up / passive income)");
                Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
                string userChoice = Console.ReadLine().ToLower();

                if (HandleNavigation(userChoice)) { break; }

                if (userChoice == "1")      { Console.WriteLine("Manage portfolio selected"); }
                else if (userChoice == "2") { Console.WriteLine("Deposits and withdrawals selected"); }
                else if (userChoice == "3") { Console.WriteLine("Growth simulation selected"); }
                else if (userChoice == "4") { DisplayNews(); }
                else if (userChoice == "5") { GoalMenu(); }
                else { Console.WriteLine("Invalid option. Please try again."); }
            }
        }
    }
    else if (entryChoice == "2") { Register(); }
    else { Console.WriteLine("Invalid option. Please try again."); }
}

// ─── Database Models ─────────────────────────────────────────────────────────

class User
{
    public int    Id           { get; set; }
    public string Username     { get; set; }
    public string PasswordHash { get; set; }
    public string Role         { get; set; }
}

class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=app.db");
}