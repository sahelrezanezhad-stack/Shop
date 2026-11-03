using Microsoft.AspNetCore.Identity;
using System.Text;

// Run locally in the owner's terminal. Do not put a plaintext password in a command,
// source file, browser form, process arguments, or shared chat.
if (Console.IsInputRedirected) throw new InvalidOperationException("Use an interactive terminal.");
Console.Write("Administrator password (10–128 characters; hidden): ");
var password = ReadHidden();
Console.Write("Confirm password: ");
var confirmation = ReadHidden();
if (password != confirmation || password.Length is < 10 or > 128)
{
    Console.Error.WriteLine("Passwords must match and contain 10–128 characters.");
    return 1;
}
Console.WriteLine("Store this value in the SallyAdmin__PasswordHash deployment secret:");
Console.WriteLine(new PasswordHasher<object>().HashPassword(new object(), password));
return 0;

static string ReadHidden()
{
    var value = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return value.ToString(); }
        if (key.Key == ConsoleKey.Backspace && value.Length > 0) value.Length--;
        else if (!char.IsControl(key.KeyChar) && value.Length < 129) value.Append(key.KeyChar);
    }
}
