/* Copyright (c) 2024-2026 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using SAM.API;
using SAM.Core.Utilities;

namespace SAM.Core.Tests.Utilities;

public class SteamErrorHelperTests
{
    #region GetUserFriendlyMessage(ClientInitializeFailure)

    [Theory]
    [InlineData(ClientInitializeFailure.GetInstallPath, "Steam-Installation nicht gefunden")]
    [InlineData(ClientInitializeFailure.Load, "Steam-Client konnte nicht geladen werden")]
    [InlineData(ClientInitializeFailure.CreateSteamClient, "Verbindung zu Steam fehlgeschlagen")]
    [InlineData(ClientInitializeFailure.CreateSteamPipe, "Kommunikation mit Steam fehlgeschlagen")]
    [InlineData(ClientInitializeFailure.ConnectToGlobalUser, "Nicht bei Steam angemeldet")]
    [InlineData(ClientInitializeFailure.AppIdMismatch, "Spiel-ID stimmt nicht überein")]
    public void GetUserFriendlyMessage_KnownFailure_ReturnsLocalizedMessage(
        ClientInitializeFailure failure, string expectedSubstring)
    {
        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(failure);

        // Assert
        Assert.Contains(expectedSubstring, message);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void GetUserFriendlyMessage_UnknownFailure_ReturnsFallbackMessage()
    {
        // Arrange — cast an undefined enum value
        var unknownFailure = (ClientInitializeFailure)255;

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(unknownFailure);

        // Assert
        Assert.Contains("Unbekannter Steam-Fehler", message);
    }

    [Fact]
    public void GetUserFriendlyMessage_DefaultUnknown_ReturnsFallbackMessage()
    {
        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ClientInitializeFailure.Unknown);

        // Assert
        Assert.Contains("Unbekannter Steam-Fehler", message);
    }

    [Theory]
    [InlineData(ClientInitializeFailure.GetInstallPath)]
    [InlineData(ClientInitializeFailure.Load)]
    [InlineData(ClientInitializeFailure.CreateSteamClient)]
    [InlineData(ClientInitializeFailure.CreateSteamPipe)]
    [InlineData(ClientInitializeFailure.ConnectToGlobalUser)]
    [InlineData(ClientInitializeFailure.AppIdMismatch)]
    public void GetUserFriendlyMessage_AllKnownFailures_NeverReturnsNullOrEmpty(
        ClientInitializeFailure failure)
    {
        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(failure);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    #endregion

    #region GetUserFriendlyMessage(ClientInitializeException)

    [Theory]
    [InlineData(ClientInitializeFailure.GetInstallPath)]
    [InlineData(ClientInitializeFailure.Load)]
    [InlineData(ClientInitializeFailure.CreateSteamClient)]
    [InlineData(ClientInitializeFailure.ConnectToGlobalUser)]
    public void GetUserFriendlyMessage_ClientInitializeException_DelegatesToFailureOverload(
        ClientInitializeFailure failure)
    {
        // Arrange
        var ex = new ClientInitializeException(failure);
        var expectedMessage = SteamErrorHelper.GetUserFriendlyMessage(failure);

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ex);

        // Assert
        Assert.Equal(expectedMessage, message);
    }

    [Fact]
    public void GetUserFriendlyMessage_ClientInitializeException_WithMessage_StillUsesFailure()
    {
        // Arrange
        var ex = new ClientInitializeException(
            ClientInitializeFailure.Load, 
            "Some internal error detail");

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ex);

        // Assert — should use the failure-based message, not the exception message
        Assert.Contains("Steam-Client konnte nicht geladen werden", message);
        Assert.DoesNotContain("Some internal error detail", message);
    }

    #endregion

    #region GetUserFriendlyMessage(Exception) — pattern matching

    [Fact]
    public void GetUserFriendlyMessage_Exception_ClientInitializeException_IsRecognized()
    {
        // Arrange
        Exception ex = new ClientInitializeException(ClientInitializeFailure.CreateSteamPipe);

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ex);

        // Assert
        Assert.Contains("Kommunikation mit Steam fehlgeschlagen", message);
    }

    [Fact]
    public void GetUserFriendlyMessage_Exception_SteamNotRunning_ReturnsLocalizedMessage()
    {
        // Arrange
        var ex = new Exception("Steam is not running on this machine");

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ex);

        // Assert
        Assert.Contains("Steam ist nicht gestartet", message);
    }

    [Fact]
    public void GetUserFriendlyMessage_Exception_SteamInstall_ReturnsLocalizedMessage()
    {
        // Arrange
        var ex = new Exception("Could not find Steam install path");

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ex);

        // Assert
        Assert.Contains("Steam-Installation nicht gefunden", message);
    }

    [Fact]
    public void GetUserFriendlyMessage_Exception_AccessDenied_ReturnsLocalizedMessage()
    {
        // Arrange
        var ex = new Exception("Access denied to Steam directory");

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ex);

        // Assert
        Assert.Contains("Zugriff verweigert", message);
    }

    [Fact]
    public void GetUserFriendlyMessage_Exception_Permission_ReturnsLocalizedMessage()
    {
        // Arrange
        var ex = new Exception("Insufficient permission to access resource");

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ex);

        // Assert
        Assert.Contains("Zugriff verweigert", message);
    }

    [Fact]
    public void GetUserFriendlyMessage_Exception_Timeout_ReturnsLocalizedMessage()
    {
        // Arrange
        var ex = new Exception("Operation timeout while waiting for Steam response");

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ex);

        // Assert
        Assert.Contains("Zeitüberschreitung", message);
    }

    [Fact]
    public void GetUserFriendlyMessage_Exception_NotInitialized_ReturnsLocalizedMessage()
    {
        // Arrange
        var ex = new Exception("Steam client not initialized");

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ex);

        // Assert
        Assert.Contains("Steam wurde nicht initialisiert", message);
    }

    [Fact]
    public void GetUserFriendlyMessage_Exception_UnknownMessage_ReturnsOriginalMessage()
    {
        // Arrange
        var originalMessage = "Something completely unexpected happened";
        var ex = new Exception(originalMessage);

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ex);

        // Assert — falls back to original exception message
        Assert.Equal(originalMessage, message);
    }

    [Fact]
    public void GetUserFriendlyMessage_Exception_CaseInsensitive_MatchesUpperCase()
    {
        // Arrange
        var ex = new Exception("STEAM IS NOT RUNNING");

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ex);

        // Assert
        Assert.Contains("Steam ist nicht gestartet", message);
    }

    [Fact]
    public void GetUserFriendlyMessage_Exception_CaseInsensitive_MatchesMixedCase()
    {
        // Arrange
        var ex = new Exception("Operation TIMEOUT occurred");

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ex);

        // Assert
        Assert.Contains("Zeitüberschreitung", message);
    }

    [Fact]
    public void GetUserFriendlyMessage_Exception_EmptyMessage_ReturnsEmptyString()
    {
        // Arrange
        var ex = new Exception("");

        // Act
        var message = SteamErrorHelper.GetUserFriendlyMessage(ex);

        // Assert — empty message doesn't match any pattern, returns original
        Assert.Equal("", message);
    }

    #endregion
}
