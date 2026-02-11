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

namespace SAM.Core.Utilities;

/// <summary>
/// Helper class for translating Steam API errors to user-friendly messages.
/// </summary>
public static class SteamErrorHelper
{
    /// <summary>
    /// Gets a user-friendly error message for a ClientInitializeFailure.
    /// </summary>
    public static string GetUserFriendlyMessage(ClientInitializeFailure failure)
    {
        return failure switch
        {
            ClientInitializeFailure.GetInstallPath => 
                "Steam-Installation nicht gefunden. Bitte stelle sicher, dass Steam installiert ist.",
            ClientInitializeFailure.Load => 
                "Steam-Client konnte nicht geladen werden. Bitte starte Steam neu.",
            ClientInitializeFailure.CreateSteamClient => 
                "Verbindung zu Steam fehlgeschlagen. Ist Steam gestartet?",
            ClientInitializeFailure.CreateSteamPipe => 
                "Kommunikation mit Steam fehlgeschlagen. Bitte starte Steam neu.",
            ClientInitializeFailure.ConnectToGlobalUser => 
                "Nicht bei Steam angemeldet. Bitte melde dich in Steam an.",
            ClientInitializeFailure.AppIdMismatch => 
                "Spiel-ID stimmt nicht überein. Bitte versuche es erneut.",
            _ => 
                "Unbekannter Steam-Fehler. Bitte starte Steam neu und versuche es erneut."
        };
    }

    /// <summary>
    /// Gets a user-friendly error message from a ClientInitializeException.
    /// </summary>
    public static string GetUserFriendlyMessage(ClientInitializeException ex)
    {
        return GetUserFriendlyMessage(ex.Failure);
    }

    /// <summary>
    /// Tries to extract a user-friendly message from any Steam-related exception.
    /// </summary>
    public static string GetUserFriendlyMessage(Exception ex)
    {
        if (ex is ClientInitializeException clientEx)
        {
            return GetUserFriendlyMessage(clientEx.Failure);
        }

        // Check for common Steam-related errors
        var message = ex.Message.ToLowerInvariant();
        
        if (message.Contains("steam") && message.Contains("not running"))
        {
            return "Steam ist nicht gestartet. Bitte starte Steam zuerst.";
        }
        
        if (message.Contains("steam") && message.Contains("install"))
        {
            return "Steam-Installation nicht gefunden. Bitte installiere Steam.";
        }

        if (message.Contains("access denied") || message.Contains("permission"))
        {
            return "Zugriff verweigert. Bitte starte die Anwendung als Administrator.";
        }

        if (message.Contains("timeout"))
        {
            return "Zeitüberschreitung bei der Kommunikation mit Steam. Bitte versuche es erneut.";
        }

        if (message.Contains("not initialized"))
        {
            return "Steam wurde nicht initialisiert. Bitte starte die Anwendung neu.";
        }

        // Return original message if no specific translation
        return ex.Message;
    }
}
