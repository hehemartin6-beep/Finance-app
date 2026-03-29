using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

// ─── Main Program ────────────────────────────────────────────────────────────

using var db = new AppDbContext();
db.Database.EnsureCreated();

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

// ─── Deposits & Withdrawals ───────────────────────────────────────────────────

Portfolio GetOrCreatePortfolio(int userId)
{
    var portfolio = db.Portfolios.FirstOrDefault(p => p.UserId == userId);
    if (portfolio == null)
    {
        portfolio = new Portfolio
        {
            UserId        = userId,
            Balance       = 0,
            MonthlyDeposit = 0,
            UpdatedAt     = DateTime.Now.ToString("dd/MM/yyyy")
        };
        db.Portfolios.Add(portfolio);
        db.SaveChanges();
    }
    return portfolio;
}

void DepositsAndWithdrawalsMenu(int userId)
{
    var portfolio = GetOrCreatePortfolio(userId);

    while (true)
    {
        Console.WriteLine($"\nCurrent balance: {portfolio.Balance:F2}");
        if (portfolio.MonthlyDeposit > 0)
            Console.WriteLine($"Monthly recurring deposit: {portfolio.MonthlyDeposit:F2}");

        Console.WriteLine("\n( 1 ) - One-time deposit");
        Console.WriteLine("( 2 ) - Set monthly recurring deposit");
        Console.WriteLine("( 3 ) - Withdrawal");
        Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");

        string choice = Console.ReadLine().ToLower();
        if (HandleNavigation(choice)) break;

        switch (choice)
        {
            case "1": OneTimeDeposit(portfolio);    break;
            case "2": SetMonthlyDeposit(portfolio); break;
            case "3": Withdrawal(portfolio); break;
            default:  Console.WriteLine("Invalid option. Please try again."); break;
        }
    }
}

void OneTimeDeposit(Portfolio portfolio)
{
    decimal amount = ReadPositiveDecimal("\nEnter deposit amount:");

    portfolio.Balance   += amount;
    portfolio.UpdatedAt  = DateTime.Now.ToString("dd/MM/yyyy");
    db.SaveChanges();

    Console.WriteLine($"Deposit of {amount:F2} successful. New balance: {portfolio.Balance:F2}");
}

void SetMonthlyDeposit(Portfolio portfolio)
{
    Console.WriteLine($"\nCurrent monthly deposit: {portfolio.MonthlyDeposit:F2}");
    Console.WriteLine("Enter new monthly deposit amount (0 to cancel recurring deposit):");

    string input = Console.ReadLine();
    if (!decimal.TryParse(input, out decimal amount) || amount < 0)
    {
        Console.WriteLine("Invalid amount. Must be 0 or a positive number.");
        return;
    }

    portfolio.MonthlyDeposit = amount;
    portfolio.UpdatedAt      = DateTime.Now.ToString("dd/MM/yyyy");
    db.SaveChanges();

    if (amount == 0)
        Console.WriteLine("Monthly recurring deposit cancelled.");
    else
        Console.WriteLine($"Monthly recurring deposit set to {amount:F2}.");
}

void Withdrawal(Portfolio portfolio)
{
    if (portfolio.Balance <= 0)
    {
        Console.WriteLine("\nYour balance is 0. Nothing to withdraw.");
        return;
    }

    Console.WriteLine($"\nAvailable balance: {portfolio.Balance:F2}");
    Console.WriteLine("Enter withdrawal amount:");

    string input = Console.ReadLine();
    if (!decimal.TryParse(input, out decimal amount) || amount <= 0)
    {
        Console.WriteLine("Invalid amount. Must be a positive number.");
        return;
    }

    if (amount > portfolio.Balance)
    {
        Console.WriteLine($"Insufficient funds. You requested {amount:F2} but your balance is only {portfolio.Balance:F2}.");
        return;
    }

    portfolio.Balance   -= amount;
    portfolio.UpdatedAt  = DateTime.Now.ToString("dd/MM/yyyy");
    db.SaveChanges();

    Console.WriteLine($"Withdrawal of {amount:F2} successful. Remaining balance: {portfolio.Balance:F2}");
}
// ─── News Functions ──────────────────────────────────────────────────────────

void AddNews()
{
    Console.WriteLine("\nEnter news title:");
    string title = Console.ReadLine();

    Console.WriteLine("Enter news description:");
    string description = Console.ReadLine();

    var news = new NewsItem
    {
        Title       = title,
        Description = description,
        Date        = DateTime.Now.ToString("dd/MM/yyyy")
    };

    db.News.Add(news);
    db.SaveChanges();
    Console.WriteLine("News added successfully.");
}

void DisplayNews()
{
    var newsList = db.News.ToList();

    if (newsList.Count == 0)
    {
        Console.WriteLine("No news available.");
        return;
    }

    for (int i = 0; i < newsList.Count; i++)
    {
        Console.WriteLine($"\n--- News {i + 1} (ID: {newsList[i].Id}) ---");
        Console.WriteLine($"Title       : {newsList[i].Title}");
        Console.WriteLine($"Description : {newsList[i].Description}");
        Console.WriteLine($"Date        : {newsList[i].Date}");
    }
}

void RemoveNews()
{
    DisplayNews();
    var newsList = db.News.ToList();
    if (newsList.Count == 0) return;

    Console.WriteLine("\nEnter the ID of the news to remove (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;

    if (int.TryParse(input, out int id))
    {
        var item = db.News.Find(id);
        if (item != null)
        {
            db.News.Remove(item);
            db.SaveChanges();
            Console.WriteLine("News removed successfully.");
        }
        else
        {
            Console.WriteLine("News item with that ID not found.");
        }
    }
    else
    {
        Console.WriteLine("Invalid ID.");
    }
}

// ─── Asset Functions ──────────────────────────────────────────────────────────

void CreateAsset()
{
    Console.WriteLine("\nEnter asset name (e.g. AAPL, BTC, Gold):");
    string name = Console.ReadLine();

    Console.WriteLine("Enter asset type (e.g. Stock, Crypto, ETF, Commodity):");
    string type = Console.ReadLine();

    decimal price = ReadPositiveDecimal("Enter current price:");

    Console.WriteLine("Enter description:");
    string description = Console.ReadLine();

    var asset = new Asset
    {
        Name        = name,
        Type        = type,
        Price       = price,
        Description = description,
        IsActive    = true,
        CreatedAt   = DateTime.Now.ToString("dd/MM/yyyy")
    };

    db.Assets.Add(asset);
    db.SaveChanges();
    Console.WriteLine($"Asset '{name}' created successfully.");
}

void DisplayAssets(bool showInactive = false)
{
    var assets = showInactive
        ? db.Assets.ToList()
        : db.Assets.Where(a => a.IsActive).ToList();

    if (assets.Count == 0)
    {
        Console.WriteLine("No assets found.");
        return;
    }

    foreach (var a in assets)
    {
        string status = a.IsActive ? "ACTIVE" : "INACTIVE";
        Console.WriteLine($"\n--- Asset (ID: {a.Id}) [{status}] ---");
        Console.WriteLine($"Name        : {a.Name}");
        Console.WriteLine($"Type        : {a.Type}");
        Console.WriteLine($"Price       : {a.Price:F2}");
        Console.WriteLine($"Description : {a.Description}");
        Console.WriteLine($"Created     : {a.CreatedAt}");
    }
}

void ManageAsset()
{
    DisplayAssets(showInactive: true);
    var assets = db.Assets.ToList();
    if (assets.Count == 0) return;

    Console.WriteLine("\nEnter the ID of the asset to edit (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;

    if (!int.TryParse(input, out int id)) { Console.WriteLine("Invalid ID."); return; }

    var asset = db.Assets.Find(id);
    if (asset == null) { Console.WriteLine("Asset not found."); return; }

    Console.WriteLine($"\nEditing asset: {asset.Name}");
    Console.WriteLine("Leave blank to keep current value.\n");

    Console.WriteLine($"Name [{asset.Name}]:");
    string name = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(name)) asset.Name = name;

    Console.WriteLine($"Type [{asset.Type}]:");
    string type = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(type)) asset.Type = type;

    Console.WriteLine($"Price [{asset.Price:F2}] (enter new or blank to skip):");
    string priceInput = Console.ReadLine();
    if (decimal.TryParse(priceInput, out decimal newPrice) && newPrice > 0)
        asset.Price = newPrice;

    Console.WriteLine($"Description [{asset.Description}]:");
    string desc = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(desc)) asset.Description = desc;

    db.SaveChanges();
    Console.WriteLine("Asset updated successfully.");
}

void DeactivateAsset()
{
    DisplayAssets(showInactive: false);
    var active = db.Assets.Where(a => a.IsActive).ToList();
    if (active.Count == 0) return;

    Console.WriteLine("\nEnter the ID of the asset to deactivate (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;

    if (!int.TryParse(input, out int id)) { Console.WriteLine("Invalid ID."); return; }

    var asset = db.Assets.Find(id);
    if (asset == null || !asset.IsActive) { Console.WriteLine("Active asset not found."); return; }

    asset.IsActive = false;
    db.SaveChanges();
    Console.WriteLine($"Asset '{asset.Name}' deactivated.");
}

void DeleteAsset()
{
    DisplayAssets(showInactive: true);
    var assets = db.Assets.ToList();
    if (assets.Count == 0) return;

    Console.WriteLine("\nEnter the ID of the asset to permanently delete (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;

    if (!int.TryParse(input, out int id)) { Console.WriteLine("Invalid ID."); return; }

    var asset = db.Assets.Find(id);
    if (asset == null) { Console.WriteLine("Asset not found."); return; }

    Console.WriteLine($"Are you sure you want to permanently delete '{asset.Name}'? (yes/no):");
    string confirm = Console.ReadLine().ToLower();
    if (confirm == "yes")
    {
        db.Assets.Remove(asset);
        db.SaveChanges();
        Console.WriteLine("Asset permanently deleted.");
    }
    else
    {
        Console.WriteLine("Deletion cancelled.");
    }
}

void DisplayAssetDetail()
{
    DisplayAssets(showInactive: true);
    var assets = db.Assets.ToList();
    if (assets.Count == 0) return;

    Console.WriteLine("\nEnter the ID of the asset to view details (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;

    if (!int.TryParse(input, out int id)) { Console.WriteLine("Invalid ID."); return; }

    var asset = db.Assets.Find(id);
    if (asset == null) { Console.WriteLine("Asset not found."); return; }

    var risks = db.Risks.Where(r => r.AssetId == asset.Id).ToList();

    Console.WriteLine($"\n══════════════════════════════════");
    Console.WriteLine($" Asset Detail — {asset.Name}");
    Console.WriteLine($"══════════════════════════════════");
    Console.WriteLine($" ID          : {asset.Id}");
    Console.WriteLine($" Type        : {asset.Type}");
    Console.WriteLine($" Price       : {asset.Price:F2}");
    Console.WriteLine($" Status      : {(asset.IsActive ? "Active" : "Inactive")}");
    Console.WriteLine($" Description : {asset.Description}");
    Console.WriteLine($" Created     : {asset.CreatedAt}");

    if (risks.Count > 0)
    {
        Console.WriteLine($"\n Risks ({risks.Count}):");
        foreach (var r in risks)
            Console.WriteLine($"  [{r.Severity.ToUpper()}] {r.Title} — {r.Description} (added {r.CreatedAt})");
    }
    else
    {
        Console.WriteLine("\n No risks associated with this asset.");
    }
}

// ─── Risk Functions ───────────────────────────────────────────────────────────

void AddRisk()
{
    DisplayAssets(showInactive: false);
    var active = db.Assets.Where(a => a.IsActive).ToList();

    int? assetId = null;
    if (active.Count > 0)
    {
        Console.WriteLine("\nEnter asset ID to link this risk to (or press Enter for a general risk):");
        string linkInput = Console.ReadLine().Trim();
        if (int.TryParse(linkInput, out int linkedId) && db.Assets.Any(a => a.Id == linkedId))
            assetId = linkedId;
        else if (!string.IsNullOrWhiteSpace(linkInput))
            Console.WriteLine("Asset not found. Creating general risk.");
    }

    Console.WriteLine("\nEnter risk title:");
    string title = Console.ReadLine();

    Console.WriteLine("Enter risk description:");
    string description = Console.ReadLine();

    Console.WriteLine("Enter severity ( low / medium / high ):");
    string severity = Console.ReadLine().ToLower();
    if (severity != "low" && severity != "medium" && severity != "high")
    {
        Console.WriteLine("Invalid severity. Defaulting to 'medium'.");
        severity = "medium";
    }

    var risk = new Risk
    {
        Title       = title,
        Description = description,
        Severity    = severity,
        AssetId     = assetId,
        CreatedAt   = DateTime.Now.ToString("dd/MM/yyyy")
    };

    db.Risks.Add(risk);
    db.SaveChanges();
    Console.WriteLine("Risk added successfully.");
}

void DisplayRisks()
{
    var risks = db.Risks.ToList();

    if (risks.Count == 0)
    {
        Console.WriteLine("No risks available.");
        return;
    }

    foreach (var r in risks)
    {
        string linkedTo = r.AssetId.HasValue ? $"Asset ID {r.AssetId}" : "General";
        Console.WriteLine($"\n--- Risk (ID: {r.Id}) ---");
        Console.WriteLine($"Title       : {r.Title}");
        Console.WriteLine($"Description : {r.Description}");
        Console.WriteLine($"Severity    : {r.Severity.ToUpper()}");
        Console.WriteLine($"Linked to   : {linkedTo}");
        Console.WriteLine($"Date        : {r.CreatedAt}");
    }
}

void RemoveRisk()
{
    DisplayRisks();
    if (!db.Risks.Any()) return;

    Console.WriteLine("\nEnter the ID of the risk to remove (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;

    if (int.TryParse(input, out int id))
    {
        var risk = db.Risks.Find(id);
        if (risk != null)
        {
            db.Risks.Remove(risk);
            db.SaveChanges();
            Console.WriteLine("Risk removed successfully.");
        }
        else
        {
            Console.WriteLine("Risk with that ID not found.");
        }
    }
    else
    {
        Console.WriteLine("Invalid ID.");
    }
}

// ─── Admin Menus ──────────────────────────────────────────────────────────────

void ManageMarketMenu()
{
    while (true)
    {
        Console.WriteLine("\n( 1 ) - Create asset");
        Console.WriteLine("( 2 ) - Edit asset");
        Console.WriteLine("( 3 ) - Deactivate asset");
        Console.WriteLine("( 4 ) - Delete asset permanently");
        Console.WriteLine("( 5 ) - Display asset detail");
        Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
        string choice = Console.ReadLine().ToLower();

        if (HandleNavigation(choice)) break;

        switch (choice)
        {
            case "1": CreateAsset();        break;
            case "2": ManageAsset();        break;
            case "3": DeactivateAsset();    break;
            case "4": DeleteAsset();        break;
            case "5": DisplayAssetDetail(); break;
            default:  Console.WriteLine("Invalid option. Please try again."); break;
        }
    }
}

void ManageNewsMenu()
{
    while (true)
    {
        Console.WriteLine("\n( 1 ) - Add news");
        Console.WriteLine("( 2 ) - View all news");
        Console.WriteLine("( 3 ) - Remove news");
        Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
        string choice = Console.ReadLine().ToLower();

        if (HandleNavigation(choice)) break;

        switch (choice)
        {
            case "1": AddNews();     break;
            case "2": DisplayNews(); break;
            case "3": RemoveNews();  break;
            default:  Console.WriteLine("Invalid option. Please try again."); break;
        }
    }
}

void ManageRisksMenu()
{
    while (true)
    {
        Console.WriteLine("\n( 1 ) - Add risk");
        Console.WriteLine("( 2 ) - View all risks");
        Console.WriteLine("( 3 ) - Remove risk");
        Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
        string choice = Console.ReadLine().ToLower();

        if (HandleNavigation(choice)) break;

        switch (choice)
        {
            case "1": AddRisk();      break;
            case "2": DisplayRisks(); break;
            case "3": RemoveRisk();   break;
            default:  Console.WriteLine("Invalid option. Please try again."); break;
        }
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

    if (HandleNavigation(entryChoice)) continue;

    if (entryChoice == "1")
    {
        User loggedInUser = Login();

        if (loggedInUser.Role == "admin")
        {
            while (true)
            {
                Console.WriteLine("\n( 1 ) - Manage market (assets)");
                Console.WriteLine("( 2 ) - Manage news");
                Console.WriteLine("( 3 ) - Manage risks");
                Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
                string adminChoice = Console.ReadLine().ToLower();

                if (HandleNavigation(adminChoice)) break;

                switch (adminChoice)
                {
                    case "1": ManageMarketMenu(); break;
                    case "2": ManageNewsMenu();   break;
                    case "3": ManageRisksMenu();  break;
                    default:  Console.WriteLine("Invalid option. Please try again."); break;
                }
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
                Console.WriteLine("( 5 ) - Risks");
                Console.WriteLine("( 6 ) - Market assets");
                Console.WriteLine("( 7 ) - Goal planning (saving up / passive income)");
                Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
                string userChoice = Console.ReadLine().ToLower();

                if (HandleNavigation(userChoice)) break;

                switch (userChoice)
                {
                    case "1": Console.WriteLine("Manage portfolio selected");         break;
                    case "2": DepositsAndWithdrawalsMenu(loggedInUser.Id);            break; 
                    case "3": Console.WriteLine("Growth simulation selected");        break;
                    case "4": DisplayNews();                                          break;
                    case "5": DisplayRisks();                                         break;
                    case "6": DisplayAssets(showInactive: false);                     break;
                    case "7": GoalMenu();                                             break;
                    default:  Console.WriteLine("Invalid option. Please try again."); break;
                }
            }
        }
    }
    else if (entryChoice == "2") Register();
    else Console.WriteLine("Invalid option. Please try again.");
}

// ─── Database Models ─────────────────────────────────────────────────────────

class User
{
    public int    Id           { get; set; }
    public string Username     { get; set; }
    public string PasswordHash { get; set; }
    public string Role         { get; set; }
}

class NewsItem
{
    public int    Id          { get; set; }
    public string Title       { get; set; }
    public string Description { get; set; }
    public string Date        { get; set; }
}

class Asset
{
    public int     Id          { get; set; }
    public string  Name        { get; set; }
    public string  Type        { get; set; }
    public decimal Price       { get; set; }
    public string  Description { get; set; }
    public bool    IsActive    { get; set; }
    public string  CreatedAt   { get; set; }
}

class Risk
{
    public int    Id          { get; set; }
    public string Title       { get; set; }
    public string Description { get; set; }
    public string Severity    { get; set; }   // low / medium / high
    public int?   AssetId     { get; set; }   // nullable — general risk if null
    public string CreatedAt   { get; set; }
}

class Portfolio
{
    public int     Id                  { get; set; }
    public int     UserId              { get; set; }
    public decimal Balance             { get; set; }
    public decimal MonthlyDeposit      { get; set; }  // 0 = not set
    public string  UpdatedAt           { get; set; }
}
class AppDbContext : DbContext
{
    public DbSet<User>     Users  { get; set; }
    public DbSet<NewsItem> News   { get; set; }
    public DbSet<Asset>    Assets { get; set; }
    public DbSet<Risk>     Risks  { get; set; }
    
    public DbSet<Portfolio> Portfolios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=app.db");
}