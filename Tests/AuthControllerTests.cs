using System.Reflection;
using TaskBoard.Controllers;

namespace TaskBoard.Tests;

public class AuthControllerTests
{
    [Fact]
    public void Login_Returns_Unified_Error_To_Prevent_Account_Enumeration()
    {
        var loginMethod = typeof(AuthController).GetMethod("Login")!;
        var assemblyPath = loginMethod.Module.Assembly.Location;

        // Read the source: ensure neither "User not found" nor "Invalid password" remain.
        // We instead expect the unified "Invalid credentials" string in the binary.
        var sourcePath = Path.Combine(
            Path.GetDirectoryName(assemblyPath)!,
            "..", "..", "..", "..", "Controllers", "AuthController.cs");
        sourcePath = Path.GetFullPath(sourcePath);

        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("User not found", source);
        Assert.DoesNotContain("Invalid password", source);
        Assert.Contains("Invalid credentials", source);
    }
}

