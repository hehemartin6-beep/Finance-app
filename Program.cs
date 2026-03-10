while (true)
{
    try
    {
        Console.WriteLine("Are you a User or an Admin?");
        string userRole = Console.ReadLine().ToLower();

        if (userRole == "admin")
        {
            Console.WriteLine("( 1 ) - Manage market");
            string adminChoice = Console.ReadLine();

            if (adminChoice == "1") { Console.WriteLine("Manage market selected"); }
            else { throw new Exception("Invalid option"); }
        }
        else if (userRole == "user")
        {
            Console.WriteLine("( 1 ) - Manage portfolio");
            Console.WriteLine("( 2 ) - Deposits and withdrawals");
            Console.WriteLine("( 3 ) - Growth simulation");
            Console.WriteLine("( 4 ) - News");
            string userChoice = Console.ReadLine();

            if (userChoice == "1")      { Console.WriteLine("Manage portfolio selected"); }
            else if (userChoice == "2") { Console.WriteLine("Deposits and withdrawals selected"); }
            else if (userChoice == "3") { Console.WriteLine("Growth simulation selected"); }
            else if (userChoice == "4") { Console.WriteLine("News selected"); }
            else { throw new Exception("Invalid option"); }
        }
        else
        {
            throw new Exception("Invalid role");
        }

        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ {ex.Message}. Please try again.");
    }
}