using System;
using System.Collections.Generic;

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

List<(string Title, string Description, string Date)> newsList = new List<(string, string, string)>();

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

while (true)
{
    Console.WriteLine("\nAre you a User or an Admin? (ex! to exit)");
    string userRole = Console.ReadLine().ToLower();

    if (HandleNavigation(userRole)) { continue; }

    if (userRole == "admin")
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
    else if (userRole == "user")
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
    else
    {
        Console.WriteLine("Invalid role. Please type 'User' or 'Admin'.");
    }
}