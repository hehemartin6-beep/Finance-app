using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

// ─── Main Program ─────────────────────────────────────────────────────────────

using var db = new AppDbContext();
db.Database.EnsureCreated();

// ─── Navigation ───────────────────────────────────────────────────────────────

void Exit()
{
    Console.WriteLine("Goodbye!");
    Environment.Exit(0);
}

bool HandleNavigation(string input)
{
    if (input == "ex!") Exit();
    if (input == "back") return true;
    return false;
}

// ─── Input Validation Helpers ─────────────────────────────────────────────────

decimal ReadPositiveDecimal(string prompt)
{
    while (true)
    {
        Console.WriteLine(prompt);
        string input = Console.ReadLine();
        if (decimal.TryParse(input, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal value) && value > 0)
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
            System.Globalization.DateTimeStyles.None, out DateTime date))
        {
            Console.WriteLine("Invalid date format. Please use dd.MM.yyyy (e.g. 25.12.2030).");
            continue;
        }
        if (date < DateTime.Today)
        {
            Console.WriteLine("Date must be in the present or future.");
            continue;
        }
        if (date > maxDate)
        {
            Console.WriteLine($"Date cannot be more than 150 years from today (max: {maxDate:dd.MM.yyyy}).");
            continue;
        }
        return date;
    }
}

// ─── Auth ─────────────────────────────────────────────────────────────────────

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

        db.Users.Add(new User
        {
            Username = username,
            PasswordHash = BC.HashPassword(password),
            Role = role
        });
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
        portfolio = new Portfolio { UserId = userId, Balance = 0, MonthlyDeposit = 0, UpdatedAt = DateTime.Now.ToString("dd/MM/yyyy") };
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
        Console.WriteLine("( 4 ) - View transaction history");
        Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");

        string choice = Console.ReadLine().ToLower();
        if (HandleNavigation(choice)) break;

        switch (choice)
        {
            case "1": OneTimeDeposit(portfolio, userId); break;
            case "2": SetMonthlyDeposit(portfolio); break;
            case "3": Withdrawal(portfolio, userId); break;
            case "4": ViewTransactionHistory(userId); break;
            default: Console.WriteLine("Invalid option. Please try again."); break;
        }
    }
}

void OneTimeDeposit(Portfolio portfolio, int userId)
{
    decimal amount = ReadPositiveDecimal("\nEnter deposit amount:");
    portfolio.Balance += amount;
    portfolio.UpdatedAt = DateTime.Now.ToString("dd/MM/yyyy");

    db.Transactions.Add(new Transaction
    {
        UserId = userId,
        Type = "deposit",
        Amount = amount,
        BalanceAfter = portfolio.Balance,
        Note = "One-time deposit",
        CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
    });

    db.SaveChanges();
    Console.WriteLine($"Deposit of {amount:F2} successful. New balance: {portfolio.Balance:F2}");
}

void SetMonthlyDeposit(Portfolio portfolio)
{
    Console.WriteLine($"\nCurrent monthly deposit: {portfolio.MonthlyDeposit:F2}");
    Console.WriteLine("Enter new monthly deposit amount (0 to cancel recurring deposit):");
    string input = Console.ReadLine();
    if (!decimal.TryParse(input, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal amount) || amount < 0)
    {
        Console.WriteLine("Invalid amount. Must be 0 or a positive number.");
        return;
    }
    portfolio.MonthlyDeposit = amount;
    portfolio.UpdatedAt = DateTime.Now.ToString("dd/MM/yyyy");
    db.SaveChanges();

    if (amount == 0)
        Console.WriteLine("Monthly recurring deposit cancelled.");
    else
        Console.WriteLine($"Monthly recurring deposit set to {amount:F2}.");
}

void Withdrawal(Portfolio portfolio, int userId)
{
    if (portfolio.Balance <= 0)
    {
        Console.WriteLine("\nYour balance is 0. Nothing to withdraw.");
        return;
    }

    Console.WriteLine($"\nAvailable balance: {portfolio.Balance:F2}");
    Console.WriteLine("Enter withdrawal amount:");
    string input = Console.ReadLine();
    if (!decimal.TryParse(input, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
    {
        Console.WriteLine("Invalid amount. Must be a positive number.");
        return;
    }
    if (amount > portfolio.Balance)
    {
        Console.WriteLine($"Insufficient funds. You requested {amount:F2} but your balance is only {portfolio.Balance:F2}.");
        return;
    }

    portfolio.Balance -= amount;
    portfolio.UpdatedAt = DateTime.Now.ToString("dd/MM/yyyy");

    db.Transactions.Add(new Transaction
    {
        UserId = userId,
        Type = "withdrawal",
        Amount = amount,
        BalanceAfter = portfolio.Balance,
        Note = "Withdrawal",
        CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
    });

    db.SaveChanges();
    Console.WriteLine($"Withdrawal of {amount:F2} successful. Remaining balance: {portfolio.Balance:F2}");
}

// ─── Transaction History ──────────────────────────────────────────────────────

void ViewTransactionHistory(int userId)
{
    var txs = db.Transactions
        .Where(t => t.UserId == userId)
        .OrderByDescending(t => t.Id)
        .ToList();

    if (txs.Count == 0)
    {
        Console.WriteLine("\nNo transactions yet.");
        return;
    }

    Console.WriteLine($"\n{"Date",-18} {"Type",-12} {"Amount",10} {"Balance after",14} {"Note",-20}");
    Console.WriteLine(new string('-', 76));
    foreach (var t in txs)
    {
        string sign = t.Type == "deposit" ? "+" : "-";
        Console.WriteLine($"{t.CreatedAt,-18} {t.Type,-12} {sign}{t.Amount,9:F2} {t.BalanceAfter,14:F2} {t.Note,-20}");
    }
}

// ─── News ─────────────────────────────────────────────────────────────────────

void AddNews()
{
    Console.WriteLine("\nEnter news title:");
    string title = Console.ReadLine();
    Console.WriteLine("Enter news description:");
    string description = Console.ReadLine();
    db.News.Add(new NewsItem { Title = title, Description = description, Date = DateTime.Now.ToString("dd/MM/yyyy") });
    db.SaveChanges();
    Console.WriteLine("News added successfully.");
}

void DisplayNews()
{
    var newsList = db.News.ToList();
    if (newsList.Count == 0) { Console.WriteLine("No news available."); return; }
    for (int i = 0; i < newsList.Count; i++)
    {
        Console.WriteLine($"\n--- News {i + 1} (ID: {newsList[i].Id}) ---");
        Console.WriteLine($"Title       : {newsList[i].Title}");
        Console.WriteLine($"Description : {newsList[i].Description}");
        Console.WriteLine($"Date        : {newsList[i].Date}");
    }
}

void EditNews()
{
    DisplayNews();
    var newsList = db.News.ToList();
    if (newsList.Count == 0) return;

    Console.WriteLine("\nEnter the ID of the news item to edit (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;
    if (!int.TryParse(input, out int id)) { Console.WriteLine("Invalid ID."); return; }

    var item = db.News.Find(id);
    if (item == null) { Console.WriteLine("News item not found."); return; }

    Console.WriteLine($"Title [{item.Title}] (leave blank to keep):");
    string title = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(title)) item.Title = title;

    Console.WriteLine($"Description [{item.Description}] (leave blank to keep):");
    string desc = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(desc)) item.Description = desc;

    db.SaveChanges();
    Console.WriteLine("News updated successfully.");
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
        if (item != null) { db.News.Remove(item); db.SaveChanges(); Console.WriteLine("News removed successfully."); }
        else Console.WriteLine("News item with that ID not found.");
    }
    else Console.WriteLine("Invalid ID.");
}

// ─── Assets ───────────────────────────────────────────────────────────────────

void CreateAsset()
{
    Console.WriteLine("\nEnter asset name (e.g. AAPL, BTC, Gold):");
    string name = Console.ReadLine();
    Console.WriteLine("Enter asset type (e.g. Stock, Crypto, ETF, Commodity):");
    string type = Console.ReadLine();
    decimal price = ReadPositiveDecimal("Enter current price:");

    Console.WriteLine("Enter expected annual return % (e.g. 7.0). Leave blank for default (6.0):");
    string retInput = Console.ReadLine().Trim();
    decimal annualReturn = decimal.TryParse(retInput, System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture, out decimal r) && r > 0 ? r : 6.0m;

    Console.WriteLine("Enter annual volatility % (e.g. 15.0). Leave blank for default (15.0):");
    string volInput = Console.ReadLine().Trim();
    decimal annualVol = decimal.TryParse(volInput, System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture, out decimal v) && v > 0 ? v : 15.0m;

    Console.WriteLine("Enter description:");
    string description = Console.ReadLine();

    db.Assets.Add(new Asset
    {
        Name = name,
        Type = type,
        Price = price,
        Description = description,
        AnnualReturnPercent = annualReturn,
        AnnualVolatilityPercent = annualVol,
        IsActive = true,
        CreatedAt = DateTime.Now.ToString("dd/MM/yyyy")
    });
    db.SaveChanges();
    Console.WriteLine($"Asset '{name}' created successfully.");
}

void DisplayAssets(bool showInactive = false, string filterName = null, string filterType = null)
{
    var query = showInactive ? db.Assets.AsQueryable() : db.Assets.Where(a => a.IsActive);
    if (!string.IsNullOrWhiteSpace(filterName))
        query = query.Where(a => a.Name.ToLower().Contains(filterName.ToLower()));
    if (!string.IsNullOrWhiteSpace(filterType))
        query = query.Where(a => a.Type.ToLower().Contains(filterType.ToLower()));

    var assets = query.ToList();
    if (assets.Count == 0) { Console.WriteLine("No assets found."); return; }

    foreach (var a in assets)
    {
        string status = a.IsActive ? "ACTIVE" : "INACTIVE";
        Console.WriteLine($"\n--- Asset (ID: {a.Id}) [{status}] ---");
        Console.WriteLine($"Name        : {a.Name}");
        Console.WriteLine($"Type        : {a.Type}");
        Console.WriteLine($"Price       : {a.Price:F2}");
        Console.WriteLine($"Exp. return : {a.AnnualReturnPercent:F1}% / year");
        Console.WriteLine($"Volatility  : {a.AnnualVolatilityPercent:F1}% / year");
        Console.WriteLine($"Description : {a.Description}");
        Console.WriteLine($"Created     : {a.CreatedAt}");
    }
}

void SearchAndDisplayAssets()
{
    Console.WriteLine("\nFilter by name (leave blank to skip):");
    string name = Console.ReadLine().Trim();
    Console.WriteLine("Filter by type (leave blank to skip):");
    string type = Console.ReadLine().Trim();
    DisplayAssets(showInactive: false,
        filterName: string.IsNullOrWhiteSpace(name) ? null : name,
        filterType: string.IsNullOrWhiteSpace(type) ? null : type);
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

    Console.WriteLine($"\nEditing asset: {asset.Name}. Leave blank to keep current value.\n");

    Console.WriteLine($"Name [{asset.Name}]:");
    string name = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(name)) asset.Name = name;

    Console.WriteLine($"Type [{asset.Type}]:");
    string type = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(type)) asset.Type = type;

    Console.WriteLine($"Price [{asset.Price:F2}]:");
    string priceInput = Console.ReadLine();
    if (decimal.TryParse(priceInput, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal newPrice) && newPrice > 0)
        asset.Price = newPrice;

    Console.WriteLine($"Expected annual return % [{asset.AnnualReturnPercent:F1}]:");
    string retInput = Console.ReadLine();
    if (decimal.TryParse(retInput, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal newRet) && newRet > 0)
        asset.AnnualReturnPercent = newRet;

    Console.WriteLine($"Annual volatility % [{asset.AnnualVolatilityPercent:F1}]:");
    string volInput = Console.ReadLine();
    if (decimal.TryParse(volInput, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal newVol) && newVol > 0)
        asset.AnnualVolatilityPercent = newVol;

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
    if (!db.Assets.Any()) return;

    Console.WriteLine("\nEnter the ID of the asset to permanently delete (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;
    if (!int.TryParse(input, out int id)) { Console.WriteLine("Invalid ID."); return; }

    var asset = db.Assets.Find(id);
    if (asset == null) { Console.WriteLine("Asset not found."); return; }

    Console.WriteLine($"Are you sure you want to permanently delete '{asset.Name}'? (yes/no):");
    if (Console.ReadLine().ToLower() == "yes")
    {
        db.Assets.Remove(asset);
        db.SaveChanges();
        Console.WriteLine("Asset permanently deleted.");
    }
    else Console.WriteLine("Deletion cancelled.");
}

void DisplayAssetDetail()
{
    DisplayAssets(showInactive: true);
    if (!db.Assets.Any()) return;

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
    Console.WriteLine($" Exp. return : {asset.AnnualReturnPercent:F1}% / year");
    Console.WriteLine($" Volatility  : {asset.AnnualVolatilityPercent:F1}% / year");
    Console.WriteLine($" Description : {asset.Description}");
    Console.WriteLine($" Created     : {asset.CreatedAt}");

    if (risks.Count > 0)
    {
        Console.WriteLine($"\n Risks ({risks.Count}):");
        foreach (var risk in risks)
            Console.WriteLine($"  [{risk.Severity.ToUpper()}] {risk.Title} — {risk.Description} (added {risk.CreatedAt})");
    }
    else Console.WriteLine("\n No risks associated with this asset.");
}

// ─── Risks ────────────────────────────────────────────────────────────────────

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

    db.Risks.Add(new Risk { Title = title, Description = description, Severity = severity, AssetId = assetId, CreatedAt = DateTime.Now.ToString("dd/MM/yyyy") });
    db.SaveChanges();
    Console.WriteLine("Risk added successfully.");
}

void DisplayRisks()
{
    var risks = db.Risks.ToList();
    if (risks.Count == 0) { Console.WriteLine("No risks available."); return; }
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
        if (risk != null) { db.Risks.Remove(risk); db.SaveChanges(); Console.WriteLine("Risk removed successfully."); }
        else Console.WriteLine("Risk with that ID not found.");
    }
    else Console.WriteLine("Invalid ID.");
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
            case "1": CreateAsset(); break;
            case "2": ManageAsset(); break;
            case "3": DeactivateAsset(); break;
            case "4": DeleteAsset(); break;
            case "5": DisplayAssetDetail(); break;
            default: Console.WriteLine("Invalid option. Please try again."); break;
        }
    }
}

void ManageNewsMenu()
{
    while (true)
    {
        Console.WriteLine("\n( 1 ) - Add news");
        Console.WriteLine("( 2 ) - View all news");
        Console.WriteLine("( 3 ) - Edit news");
        Console.WriteLine("( 4 ) - Remove news");
        Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
        string choice = Console.ReadLine().ToLower();
        if (HandleNavigation(choice)) break;
        switch (choice)
        {
            case "1": AddNews(); break;
            case "2": DisplayNews(); break;
            case "3": EditNews(); break;
            case "4": RemoveNews(); break;
            default: Console.WriteLine("Invalid option. Please try again."); break;
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
            case "1": AddRisk(); break;
            case "2": DisplayRisks(); break;
            case "3": RemoveRisk(); break;
            default: Console.WriteLine("Invalid option. Please try again."); break;
        }
    }
}

// ─── Admin: User Management ───────────────────────────────────────────────────

void UserManagementMenu()
{
    while (true)
    {
        Console.WriteLine("\n( 1 ) - List all users");
        Console.WriteLine("( 2 ) - Deactivate user");
        Console.WriteLine("( 3 ) - Reactivate user");
        Console.WriteLine("( 4 ) - Reset user password");
        Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
        string choice = Console.ReadLine().ToLower();
        if (HandleNavigation(choice)) break;
        switch (choice)
        {
            case "1": ListUsers(); break;
            case "2": DeactivateUser(); break;
            case "3": ReactivateUser(); break;
            case "4": ResetUserPassword(); break;
            default: Console.WriteLine("Invalid option. Please try again."); break;
        }
    }
}

void ListUsers()
{
    var users = db.Users.ToList();
    if (users.Count == 0) { Console.WriteLine("No users found."); return; }

    Console.WriteLine($"\n{"ID",-6} {"Username",-20} {"Role",-10} {"Status",-10}");
    Console.WriteLine(new string('-', 48));
    foreach (var u in users)
    {
        string status = u.IsActive ? "active" : "INACTIVE";
        Console.WriteLine($"{u.Id,-6} {u.Username,-20} {u.Role,-10} {status,-10}");
    }
}

void DeactivateUser()
{
    ListUsers();
    Console.WriteLine("\nEnter user ID to deactivate (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;
    if (!int.TryParse(input, out int id)) { Console.WriteLine("Invalid ID."); return; }

    var user = db.Users.Find(id);
    if (user == null) { Console.WriteLine("User not found."); return; }
    if (!user.IsActive) { Console.WriteLine("User is already inactive."); return; }

    user.IsActive = false;
    db.SaveChanges();
    Console.WriteLine($"User '{user.Username}' deactivated.");
}

void ReactivateUser()
{
    ListUsers();
    Console.WriteLine("\nEnter user ID to reactivate (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;
    if (!int.TryParse(input, out int id)) { Console.WriteLine("Invalid ID."); return; }

    var user = db.Users.Find(id);
    if (user == null) { Console.WriteLine("User not found."); return; }
    if (user.IsActive) { Console.WriteLine("User is already active."); return; }

    user.IsActive = true;
    db.SaveChanges();
    Console.WriteLine($"User '{user.Username}' reactivated.");
}

void ResetUserPassword()
{
    ListUsers();
    Console.WriteLine("\nEnter user ID to reset password (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;
    if (!int.TryParse(input, out int id)) { Console.WriteLine("Invalid ID."); return; }

    var user = db.Users.Find(id);
    if (user == null) { Console.WriteLine("User not found."); return; }

    Console.WriteLine("Enter new password:");
    string newPass = Console.ReadLine();
    Console.WriteLine("Confirm new password:");
    string confirm = Console.ReadLine();
    if (newPass != confirm) { Console.WriteLine("Passwords do not match. No changes made."); return; }

    user.PasswordHash = BC.HashPassword(newPass);
    db.SaveChanges();
    Console.WriteLine($"Password for '{user.Username}' reset successfully.");
}

// ─── Goals ────────────────────────────────────────────────────────────────────

void GoalMenu()
{
    while (true)
    {
        Console.WriteLine("\n( 1 ) - Set goal: Save X by date Y");
        Console.WriteLine("( 2 ) - Set goal: Passive income Z/month");
        Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
        string goalChoice = Console.ReadLine().ToLower();
        if (HandleNavigation(goalChoice)) break;
        if (goalChoice == "1") SetSavingsGoal();
        else if (goalChoice == "2") SetRentGoal();
        else Console.WriteLine("Invalid option. Please try again.");
    }
}

void SetSavingsGoal()
{
    decimal targetAmount = ReadPositiveDecimal("\nEnter target amount (X):");
    DateTime targetDate = ReadFutureDate("Enter target date (dd.MM.yyyy):");
    SimulateAndEvaluateSavings(targetAmount, targetDate);
}

void SetRentGoal()
{
    decimal monthlyRent = ReadPositiveDecimal("\nEnter desired monthly passive income (Z):");
    DateTime fromDate = ReadFutureDate("Enter start date for passive income (dd.MM.yyyy):");
    SimulateAndEvaluateRent(monthlyRent, fromDate);
}

// ─── Simulation helpers ───────────────────────────────────────────────────────

/// <summary>
/// Asks the user to pick simulation parameters.
/// If they have active assets, they can average the asset-level params.
/// Otherwise they enter manually.
/// </summary>
(decimal monthlyReturn, decimal monthlyVol) AskSimParams()
{
    var activeAssets = db.Assets.Where(a => a.IsActive).ToList();

    if (activeAssets.Count > 0)
    {
        Console.WriteLine("\nHow would you like to set simulation parameters?");
        Console.WriteLine("( 1 ) - Use average of all active assets");
        Console.WriteLine("( 2 ) - Enter manually");
        string pick = Console.ReadLine().Trim();

        if (pick == "1")
        {
            decimal avgAnnualReturn   = activeAssets.Average(a => a.AnnualReturnPercent);
            decimal avgAnnualVol      = activeAssets.Average(a => a.AnnualVolatilityPercent);
            decimal monthlyReturn     = avgAnnualReturn / 100m / 12m;
            decimal monthlyVol        = avgAnnualVol    / 100m / (decimal)Math.Sqrt(12);
            Console.WriteLine($"Using averaged values — annual return: {avgAnnualReturn:F1}%, annual volatility: {avgAnnualVol:F1}%");
            return (monthlyReturn, monthlyVol);
        }
    }

    Console.WriteLine("\nEnter expected annual return % (e.g. 7.0):");
    decimal annualRet = 6.0m;
    string retIn = Console.ReadLine().Trim();
    if (decimal.TryParse(retIn, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal parsedRet) && parsedRet > 0)
        annualRet = parsedRet;
    else Console.WriteLine($"Invalid input, using default {annualRet:F1}%.");

    Console.WriteLine("Enter annual volatility % (e.g. 15.0):");
    decimal annualVol = 15.0m;
    string volIn = Console.ReadLine().Trim();
    if (decimal.TryParse(volIn, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal parsedVol) && parsedVol > 0)
        annualVol = parsedVol;
    else Console.WriteLine($"Invalid input, using default {annualVol:F1}%.");

    return (annualRet / 100m / 12m, annualVol / 100m / (decimal)Math.Sqrt(12));
}

void SimulateAndEvaluateSavings(decimal target, DateTime targetDate)
{
    decimal startValue            = ReadPositiveDecimal("Enter starting portfolio value:");
    decimal monthlyContribution   = ReadPositiveDecimal("Enter monthly contribution:");
    var (meanReturn, volatility)  = AskSimParams();

    int months       = Math.Max(0, ((targetDate.Year - DateTime.Today.Year) * 12) + targetDate.Month - DateTime.Today.Month);
    int simulations  = 1000;
    int successCount = 0;
    Random rng       = new Random();

    for (int sim = 0; sim < simulations; sim++)
    {
        decimal value = startValue;
        for (int m = 0; m < months; m++)
        {
            decimal shock = (decimal)SampleNormal(rng, (double)meanReturn, (double)volatility);
            value = value * (1 + shock) + monthlyContribution;
        }
        if (value >= target) successCount++;
    }

    decimal probability = (decimal)successCount / simulations * 100;
    Console.WriteLine($"\nMonths simulated  : {months}");
    Console.WriteLine($"Likelihood of achieving goal: {probability:F1}%");
}

void SimulateAndEvaluateRent(decimal rent, DateTime fromDate)
{
    decimal startValue           = ReadPositiveDecimal("Enter starting portfolio value:");
    int months                   = ReadPositiveInt("Enter desired duration of passive income in months:");
    var (meanReturn, volatility) = AskSimParams();

    int simulations  = 1000;
    int successCount = 0;
    Random rng       = new Random();

    for (int sim = 0; sim < simulations; sim++)
    {
        decimal value   = startValue;
        bool survived   = true;
        for (int m = 0; m < months; m++)
        {
            decimal shock = (decimal)SampleNormal(rng, (double)meanReturn, (double)volatility);
            value = value * (1 + shock) - rent;
            if (value < 0) { survived = false; break; }
        }
        if (survived) successCount++;
    }

    decimal probability = (decimal)successCount / simulations * 100;
    Console.WriteLine($"\nMonths simulated  : {months}");
    Console.WriteLine($"Likelihood of passive income lasting the desired duration: {probability:F1}%");
}

// ─── Growth simulation (was a stub) ──────────────────────────────────────────

void GrowthSimulationMenu(int userId)
{
    var cashPortfolio = GetOrCreatePortfolio(userId);

    while (true)
    {
        Console.WriteLine("\n═══ Growth Simulation ═══");
        Console.WriteLine("( 1 ) - Simulate portfolio growth (lump sum)");
        Console.WriteLine("( 2 ) - Simulate with monthly contributions");
        Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
        string choice = Console.ReadLine().ToLower();
        if (HandleNavigation(choice)) break;

        switch (choice)
        {
            case "1": RunGrowthSim(cashPortfolio.Balance, withContributions: false); break;
            case "2": RunGrowthSim(cashPortfolio.Balance, withContributions: true);  break;
            default: Console.WriteLine("Invalid option. Please try again."); break;
        }
    }
}

void RunGrowthSim(decimal suggestedStart, bool withContributions)
{
    Console.WriteLine($"\nSuggested starting value from your cash balance: {suggestedStart:F2}");
    decimal startValue = ReadPositiveDecimal("Enter starting portfolio value (or use suggestion above):");

    decimal monthlyContribution = 0;
    if (withContributions)
        monthlyContribution = ReadPositiveDecimal("Enter monthly contribution:");

    int years = ReadPositiveInt("Simulate over how many years?");
    int months = years * 12;

    var (meanReturn, volatility) = AskSimParams();

    int simulations = 1000;
    Random rng      = new Random();

    var finalValues = new List<decimal>(simulations);
    int below0      = 0;

    for (int sim = 0; sim < simulations; sim++)
    {
        decimal value = startValue;
        for (int m = 0; m < months; m++)
        {
            decimal shock = (decimal)SampleNormal(rng, (double)meanReturn, (double)volatility);
            value = value * (1 + shock) + monthlyContribution;
        }
        if (value < 0) below0++;
        finalValues.Add(value);
    }

    finalValues.Sort();

    decimal p10   = finalValues[(int)(simulations * 0.10)];
    decimal p25   = finalValues[(int)(simulations * 0.25)];
    decimal p50   = finalValues[(int)(simulations * 0.50)];
    decimal p75   = finalValues[(int)(simulations * 0.75)];
    decimal p90   = finalValues[(int)(simulations * 0.90)];
    decimal avg   = finalValues.Average();

    Console.WriteLine($"\n══════════════════════════════════════════════════");
    Console.WriteLine($" Growth Simulation — {years} year(s), {simulations} runs");
    Console.WriteLine($"══════════════════════════════════════════════════");
    Console.WriteLine($" Starting value         : {startValue:F2}");
    if (withContributions)
        Console.WriteLine($" Monthly contribution   : {monthlyContribution:F2}");
    Console.WriteLine();
    Console.WriteLine($" Pessimistic (10th pct) : {p10:F2}");
    Console.WriteLine($" Lower quartile (25th)  : {p25:F2}");
    Console.WriteLine($" Median (50th pct)      : {p50:F2}");
    Console.WriteLine($" Upper quartile (75th)  : {p75:F2}");
    Console.WriteLine($" Optimistic (90th pct)  : {p90:F2}");
    Console.WriteLine($" Average outcome        : {avg:F2}");
    if (below0 > 0)
        Console.WriteLine($"\n ⚠  {below0} of {simulations} simulations went below zero.");
    Console.WriteLine($"══════════════════════════════════════════════════");
}

// ─── Net Worth ────────────────────────────────────────────────────────────────

void DisplayNetWorth(int userId)
{
    var cashPortfolio = GetOrCreatePortfolio(userId);

    var investmentPortfolios = db.InvestmentPortfolios
        .Where(p => p.UserId == userId)
        .ToList();

    decimal totalInvestmentValue = 0;

    Console.WriteLine("\n══════════════════════════════════════");
    Console.WriteLine(" Net Worth Overview");
    Console.WriteLine("══════════════════════════════════════");
    Console.WriteLine($" Cash balance : {cashPortfolio.Balance:F2}");

    if (investmentPortfolios.Count > 0)
    {
        Console.WriteLine("\n Investment portfolios:");
        foreach (var ip in investmentPortfolios)
        {
            var items = db.PortfolioAssets.Where(pa => pa.InvestmentPortfolioId == ip.Id).ToList();
            decimal ipValue = 0;
            foreach (var item in items)
            {
                var asset = db.Assets.Find(item.AssetId);
                if (asset != null) ipValue += asset.Price * item.Units;
            }
            totalInvestmentValue += ipValue;
            Console.WriteLine($"  {ip.Name,-25} : {ipValue:F2}");
        }
    }

    decimal total = cashPortfolio.Balance + totalInvestmentValue;
    Console.WriteLine($"\n Total net worth         : {total:F2}");
    Console.WriteLine("══════════════════════════════════════");
}

// ─── Portfolio Management ─────────────────────────────────────────────────────

void ManagePortfolioMenu(int userId)
{
    while (true)
    {
        var portfolios = db.InvestmentPortfolios.Where(p => p.UserId == userId).ToList();

        if (portfolios.Count == 0)
        {
            Console.WriteLine("\nYou have no investment portfolios yet.");
            Console.WriteLine("( 1 ) - Create new portfolio");
            Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
            string firstChoice = Console.ReadLine().ToLower();
            if (HandleNavigation(firstChoice)) break;
            if (firstChoice == "1") CreateInvestmentPortfolio(userId);
            else Console.WriteLine("Invalid option.");
            continue;
        }

        Console.WriteLine("\n═══ Your Investment Portfolios ═══");
        foreach (var p in portfolios)
        {
            string rebalLabel = p.RebalancingMode switch { "monthly" => "Monthly", "quarterly" => "Quarterly", _ => "Off" };
            Console.WriteLine($"  (ID: {p.Id}) {p.Name}  |  Rebalancing: {rebalLabel}  |  Created: {p.CreatedAt}");
        }

        Console.WriteLine("\n( 1 ) - Create new portfolio");
        Console.WriteLine("( 2 ) - Open a portfolio");
        Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");
        string choice = Console.ReadLine().ToLower();
        if (HandleNavigation(choice)) break;

        switch (choice)
        {
            case "1": CreateInvestmentPortfolio(userId); break;
            case "2": SelectAndOpenPortfolio(userId); break;
            default: Console.WriteLine("Invalid option. Please try again."); break;
        }
    }
}

void CreateInvestmentPortfolio(int userId)
{
    Console.WriteLine("\nEnter portfolio name:");
    string name = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(name)) { Console.WriteLine("Portfolio name cannot be empty."); return; }

    db.InvestmentPortfolios.Add(new InvestmentPortfolio
    {
        UserId = userId,
        Name = name,
        RebalancingMode = "off",
        CreatedAt = DateTime.Now.ToString("dd/MM/yyyy")
    });
    db.SaveChanges();
    Console.WriteLine($"Portfolio '{name}' created successfully.");
}

void SelectAndOpenPortfolio(int userId)
{
    var portfolios = db.InvestmentPortfolios.Where(p => p.UserId == userId).ToList();
    if (portfolios.Count == 0) { Console.WriteLine("No portfolios found."); return; }

    Console.WriteLine("\nEnter portfolio ID to open (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;
    if (!int.TryParse(input, out int id)) { Console.WriteLine("Invalid ID."); return; }

    var portfolio = portfolios.FirstOrDefault(p => p.Id == id);
    if (portfolio == null) { Console.WriteLine("Portfolio not found."); return; }

    OpenPortfolioMenu(portfolio);
}

void OpenPortfolioMenu(InvestmentPortfolio portfolio)
{
    while (true)
    {
        Console.WriteLine($"\n══════════════════════════════════");
        Console.WriteLine($" Portfolio: {portfolio.Name}");
        Console.WriteLine($"══════════════════════════════════");
        Console.WriteLine("\n( 1 ) - View portfolio detail");
        Console.WriteLine("( 2 ) - Add asset");
        Console.WriteLine("( 3 ) - Remove asset");
        Console.WriteLine("( 4 ) - Set asset weights (%)");
        Console.WriteLine("( 5 ) - Set rebalancing");
        Console.WriteLine("( back ) - Go back | ( ex! ) - Exit");

        string choice = Console.ReadLine().ToLower();
        if (HandleNavigation(choice)) break;

        switch (choice)
        {
            case "1": DisplayPortfolioDetail(portfolio); break;
            case "2": AddAssetToPortfolio(portfolio); break;
            case "3": RemoveAssetFromPortfolio(portfolio); break;
            case "4": SetAssetWeights(portfolio); break;
            case "5": SetRebalancing(portfolio); break;
            default: Console.WriteLine("Invalid option. Please try again."); break;
        }
    }
}

void DisplayPortfolioDetail(InvestmentPortfolio portfolio)
{
    var items = db.PortfolioAssets.Where(pa => pa.InvestmentPortfolioId == portfolio.Id).ToList();
    string rebalLabel = portfolio.RebalancingMode switch { "monthly" => "Monthly", "quarterly" => "Quarterly", _ => "Off" };

    Console.WriteLine($"\n══════════════════════════════════════════════");
    Console.WriteLine($" Portfolio Detail — {portfolio.Name}");
    Console.WriteLine($"══════════════════════════════════════════════");
    Console.WriteLine($" Rebalancing  : {rebalLabel}");
    Console.WriteLine($" Created      : {portfolio.CreatedAt}");

    if (items.Count == 0) { Console.WriteLine("\n No assets in this portfolio yet."); return; }

    decimal totalWeight = items.Sum(i => i.WeightPercent);
    decimal totalValue  = 0;

    Console.WriteLine($"\n {"Asset",-12} {"Type",-12} {"Weight%",8} {"Price",10} {"Units",8} {"Value",12}");
    Console.WriteLine($" {new string('-', 66)}");

    foreach (var item in items)
    {
        var asset = db.Assets.Find(item.AssetId);
        if (asset == null) continue;
        decimal value = asset.Price * item.Units;
        totalValue += value;
        Console.WriteLine($" {asset.Name,-12} {asset.Type,-12} {item.WeightPercent,7:F1}% {asset.Price,10:F2} {item.Units,8:F4} {value,12:F2}");
    }

    Console.WriteLine($" {new string('-', 66)}");
    Console.WriteLine($" {"TOTAL",-26} {totalWeight,7:F1}%  {"",10} {"",8} {totalValue,12:F2}");

    if (Math.Abs(totalWeight - 100m) > 0.01m)
        Console.WriteLine($"\n ⚠ Warning: weights sum to {totalWeight:F1}% (should be 100%).");
    else
        Console.WriteLine($"\n ✓ Weights are balanced (100%).");
}

void AddAssetToPortfolio(InvestmentPortfolio portfolio)
{
    var activeAssets = db.Assets.Where(a => a.IsActive).ToList();
    if (activeAssets.Count == 0) { Console.WriteLine("No active assets available to add."); return; }

    var existingIds = db.PortfolioAssets
        .Where(pa => pa.InvestmentPortfolioId == portfolio.Id)
        .Select(pa => pa.AssetId)
        .ToHashSet();

    var available = activeAssets.Where(a => !existingIds.Contains(a.Id)).ToList();
    if (available.Count == 0) { Console.WriteLine("All active assets are already in this portfolio."); return; }

    Console.WriteLine("\nAvailable assets:");
    foreach (var a in available)
        Console.WriteLine($"  (ID: {a.Id}) {a.Name} [{a.Type}]  Price: {a.Price:F2}");

    Console.WriteLine("\nEnter asset ID to add (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;
    if (!int.TryParse(input, out int assetId)) { Console.WriteLine("Invalid ID."); return; }

    var asset = available.FirstOrDefault(a => a.Id == assetId);
    if (asset == null) { Console.WriteLine("Asset not found or already in portfolio."); return; }

    Console.WriteLine($"Enter number of units for {asset.Name} (e.g. 1.5):");
    string unitsInput = Console.ReadLine();
    if (!decimal.TryParse(unitsInput, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal units) || units <= 0)
    {
        Console.WriteLine("Invalid units. Must be a positive number.");
        return;
    }

    db.PortfolioAssets.Add(new PortfolioAsset
    {
        InvestmentPortfolioId = portfolio.Id,
        AssetId = asset.Id,
        Units = units,
        WeightPercent = 0
    });
    db.SaveChanges();
    Console.WriteLine($"Asset '{asset.Name}' ({units} units) added to portfolio.");
    Console.WriteLine("Remember to update weights (option 4) so they sum to 100%.");
}

void RemoveAssetFromPortfolio(InvestmentPortfolio portfolio)
{
    var items = db.PortfolioAssets.Where(pa => pa.InvestmentPortfolioId == portfolio.Id).ToList();
    if (items.Count == 0) { Console.WriteLine("No assets in this portfolio."); return; }

    Console.WriteLine("\nAssets in portfolio:");
    foreach (var item in items)
    {
        var asset = db.Assets.Find(item.AssetId);
        if (asset != null)
            Console.WriteLine($"  (AssetID: {asset.Id}) {asset.Name}  Units: {item.Units:F4}  Weight: {item.WeightPercent:F1}%");
    }

    Console.WriteLine("\nEnter asset ID to remove (or 'back'):");
    string input = Console.ReadLine().ToLower();
    if (HandleNavigation(input)) return;
    if (!int.TryParse(input, out int assetId)) { Console.WriteLine("Invalid ID."); return; }

    var pa = items.FirstOrDefault(i => i.AssetId == assetId);
    if (pa == null) { Console.WriteLine("Asset not found in this portfolio."); return; }

    var assetName = db.Assets.Find(assetId)?.Name ?? assetId.ToString();
    Console.WriteLine($"Are you sure you want to remove '{assetName}'? (yes/no):");
    if (Console.ReadLine().ToLower() == "yes")
    {
        db.PortfolioAssets.Remove(pa);
        db.SaveChanges();
        Console.WriteLine($"Asset '{assetName}' removed. Remember to re-check weights (option 4).");
    }
    else Console.WriteLine("Removal cancelled.");
}

void SetAssetWeights(InvestmentPortfolio portfolio)
{
    var items = db.PortfolioAssets.Where(pa => pa.InvestmentPortfolioId == portfolio.Id).ToList();
    if (items.Count == 0) { Console.WriteLine("No assets in this portfolio. Add assets first."); return; }

    Console.WriteLine($"\nSetting weights for {items.Count} asset(s). Must sum to exactly 100%.");
    var newWeights = new Dictionary<int, decimal>();

    foreach (var item in items)
    {
        var asset = db.Assets.Find(item.AssetId);
        string assetName = asset?.Name ?? $"Asset {item.AssetId}";
        while (true)
        {
            Console.WriteLine($"  {assetName} (current: {item.WeightPercent:F1}%) — enter new weight %:");
            string input = Console.ReadLine().Trim();
            if (string.IsNullOrWhiteSpace(input)) { newWeights[item.AssetId] = item.WeightPercent; break; }
            if (decimal.TryParse(input, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal w) && w >= 0 && w <= 100)
            {
                newWeights[item.AssetId] = w;
                break;
            }
            Console.WriteLine("  Invalid. Enter a number between 0 and 100.");
        }
    }

    decimal total = newWeights.Values.Sum();
    if (Math.Abs(total - 100m) > 0.01m)
    {
        Console.WriteLine($"\n✗ Weights sum to {total:F1}% — must be exactly 100%. No changes saved.");
        return;
    }

    foreach (var item in items)
        item.WeightPercent = newWeights[item.AssetId];

    db.SaveChanges();
    Console.WriteLine($"\n✓ Weights saved successfully (total: {total:F1}%).");
}

void SetRebalancing(InvestmentPortfolio portfolio)
{
    Console.WriteLine($"\nCurrent rebalancing mode: {portfolio.RebalancingMode.ToUpper()}");
    Console.WriteLine("( 1 ) - Off\n( 2 ) - Monthly\n( 3 ) - Quarterly\n( back ) - Cancel");
    string choice = Console.ReadLine().ToLower();
    if (HandleNavigation(choice)) return;

    string newMode = choice switch { "1" => "off", "2" => "monthly", "3" => "quarterly", _ => null };
    if (newMode == null) { Console.WriteLine("Invalid option. No changes made."); return; }

    portfolio.RebalancingMode = newMode;
    db.SaveChanges();
    Console.WriteLine($"Rebalancing set to: {newMode.ToUpper()}.");
}

// ─── Box-Muller normal sampler ────────────────────────────────────────────────

double SampleNormal(Random rng, double mean, double stddev)
{
    double u1 = 1.0 - rng.NextDouble();
    double u2 = 1.0 - rng.NextDouble();
    double z  = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    return mean + stddev * z;
}

// ─── Entry Point ──────────────────────────────────────────────────────────────

while (true)
{
    Console.WriteLine("\n( 1 ) - Login");
    Console.WriteLine("( 2 ) - Register");
    Console.WriteLine("( ex! ) - Exit");
    string entryChoice = Console.ReadLine().ToLower();

    if (entryChoice == "ex!") Exit();
    if (entryChoice == "2") { Register(); continue; }
    if (entryChoice != "1") { Console.WriteLine("Invalid option. Please try again."); continue; }

    User loggedInUser = Login();

    // Block deactivated users
    if (!loggedInUser.IsActive)
    {
        Console.WriteLine("Your account has been deactivated. Please contact an administrator.");
        continue;
    }

    if (loggedInUser.Role == "admin")
    {
        while (true)
        {
            Console.WriteLine("\n( 1 ) - Manage market (assets)");
            Console.WriteLine("( 2 ) - Manage news");
            Console.WriteLine("( 3 ) - Manage risks");
            Console.WriteLine("( 4 ) - User management");
            Console.WriteLine("( back ) - Log out | ( ex! ) - Exit");
            string adminChoice = Console.ReadLine().ToLower();
            if (HandleNavigation(adminChoice)) break;
            switch (adminChoice)
            {
                case "1": ManageMarketMenu(); break;
                case "2": ManageNewsMenu(); break;
                case "3": ManageRisksMenu(); break;
                case "4": UserManagementMenu(); break;
                default: Console.WriteLine("Invalid option. Please try again."); break;
            }
        }
    }
    else
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
            Console.WriteLine("( 8 ) - Net worth overview");
            Console.WriteLine("( back ) - Log out | ( ex! ) - Exit");
            string userChoice = Console.ReadLine().ToLower();
            if (HandleNavigation(userChoice)) break;
            switch (userChoice)
            {
                case "1": ManagePortfolioMenu(loggedInUser.Id); break;
                case "2": DepositsAndWithdrawalsMenu(loggedInUser.Id); break;
                case "3": GrowthSimulationMenu(loggedInUser.Id); break;
                case "4": DisplayNews(); break;
                case "5": DisplayRisks(); break;
                case "6": SearchAndDisplayAssets(); break;
                case "7": GoalMenu(); break;
                case "8": DisplayNetWorth(loggedInUser.Id); break;
                default: Console.WriteLine("Invalid option. Please try again."); break;
            }
        }
    }
}

// ─── Models ───────────────────────────────────────────────────────────────────

class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }
    public bool IsActive { get; set; } = true;
}

class NewsItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Date { get; set; }
}

class Asset
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public string CreatedAt { get; set; }

    /// <summary>Expected annual return in percent (e.g. 7.0 = 7%). Used in simulations.</summary>
    public decimal AnnualReturnPercent { get; set; } = 6.0m;

    /// <summary>Annual volatility (std dev) in percent (e.g. 15.0 = 15%). Used in simulations.</summary>
    public decimal AnnualVolatilityPercent { get; set; } = 15.0m;
}

class Risk
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; }
    public int? AssetId { get; set; }
    public string CreatedAt { get; set; }
}

class Portfolio
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Balance { get; set; }
    public decimal MonthlyDeposit { get; set; }
    public string UpdatedAt { get; set; }
}

class Transaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    /// <summary>deposit | withdrawal</summary>
    public string Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Note { get; set; }
    public string CreatedAt { get; set; }
}

class InvestmentPortfolio
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; }
    public string RebalancingMode { get; set; } = "off";
    public string CreatedAt { get; set; }
    public List<PortfolioAsset> PortfolioAssets { get; set; } = new();
}

class PortfolioAsset
{
    public int Id { get; set; }
    public int InvestmentPortfolioId { get; set; }
    public int AssetId { get; set; }
    public decimal Units { get; set; }
    public decimal WeightPercent { get; set; }
    public InvestmentPortfolio InvestmentPortfolio { get; set; }
    public Asset Asset { get; set; }
}

// ─── DB Context ───────────────────────────────────────────────────────────────

class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<NewsItem> News { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<Risk> Risks { get; set; }
    public DbSet<Portfolio> Portfolios { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<InvestmentPortfolio> InvestmentPortfolios { get; set; }
    public DbSet<PortfolioAsset> PortfolioAssets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=app.db");
}