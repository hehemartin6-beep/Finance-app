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
                Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
                string userChoice = Console.ReadLine().ToLower();

                if (HandleNavigation(userChoice)) { break; }

                if (userChoice == "1")      { Console.WriteLine("Manage portfolio selected"); }
                else if (userChoice == "2") { Console.WriteLine("Deposits and withdrawals selected"); }
                else if (userChoice == "3") { Console.WriteLine("Growth simulation selected"); }
                else if (userChoice == "4") { DisplayNews(); }
                else { Console.WriteLine("Invalid option. Please try again."); }
            }
        }
    }
    else if (entryChoice == "2") { Register(); }
    else { Console.WriteLine("Invalid option. Please try again."); }
}

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