Potřebné balíčky:
— Microsoft.EntityFrameworkCore.Sqlite | 10.0.5
— Microsoft.EntityFrameworkCore.Tools | 10.0.5
— BCrypt.Net.BCrypt | 4.1.0

!! pokud při přidávání nebo zobrazovaní dat hází error, tak musíte vymazat databázi ( app ) aby to vytvo5ilo správně tabulky. Dole je ukázka erroru.



2. Kontrolní bod
   Spasov:
   Funkce admina ( přidání assetů, risků) : 2H
   
   Manuylov:
   Přidání vkladů a výběru s validací u usera : 1.5H


   Error:
     Unhandled exception. Microsoft.Data.Sqlite.SqliteException (0x80004005): SQLite Error 1: 'no such table: Portfolios'.
   at Microsoft.Data.Sqlite.SqliteException.ThrowExceptionForRC(Int32 rc, sqlite3 db)
   at Microsoft.Data.Sqlite.SqliteCommand.PrepareAndEnumerateStatements()+MoveNext()
   at Microsoft.Data.Sqlite.SqliteCommand.GetStatements()+MoveNext()
   at Microsoft.Data.Sqlite.SqliteDataReader.NextResult()
   at Microsoft.Data.Sqlite.SqliteCommand.ExecuteReader(CommandBehavior behavior)
   at Microsoft.Data.Sqlite.SqliteCommand.ExecuteDbDataReader(CommandBehavior behavior)
   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReader(RelationalCommandParameterObject parameterObject)
   at Microsoft.EntityFrameworkCore.Query.Internal.SingleQueryingEnumerable`1.Enumerator.InitializeReader(Enumerator enumerator)
   at Microsoft.EntityFrameworkCore.Query.Internal.SingleQueryingEnumerable`1.Enumerator.<>c.<MoveNext>b__21_0(DbContext _, Enumerator enumerator)
   at Microsoft.EntityFrameworkCore.Storage.NonRetryingExecutionStrategy.Execute[TState,TResult](TState state, Func`3 operation, Func`3 verifySucceeded)
   at Microsoft.EntityFrameworkCore.Query.Internal.SingleQueryingEnumerable`1.Enumerator.MoveNext()
   at System.Linq.Enumerable.TryGetSingle[TSource](IEnumerable`1 source, Boolean& found)
   at lambda_method58(Closure, QueryContext)
   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.ExecuteCore[TResult](Expression query, Boolean async, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.Execute[TResult](Expression query)
   at Microsoft.EntityFrameworkCore.Query.Internal.EntityQueryProvider.Execute[TResult](Expression expression)
   at Program.<<Main>$>g__GetOrCreatePortfolio|0_7(Int32 userId, <>c__DisplayClass0_0&) in C:\Users\manuy\RiderProjects\Financeapp\Financeapp\Program.cs:line 158
   at Program.<<Main>$>g__DepositsAndWithdrawalsMenu|0_8(Int32 userId, <>c__DisplayClass0_0&) in C:\Users\manuy\RiderProjects\Financeapp\Financeapp\Program.cs:line 176
   at Program.<Main>$(String[] args) in C:\Users\manuy\RiderProjects\Financeapp\Financeapp\Program.cs:line 839
