using System;
using System.Security;

namespace RoyalApps.Community.ExternalApps.WinForms.Extensions;

internal static class SecureStringExtensions
{
    public static SecureString ConvertToSecureString(string password)
    {
        if (password == null)
            throw new ArgumentNullException(nameof(password));

        var returnString = new SecureString();
        foreach (var character in password.ToCharArray())
        {
            returnString.AppendChar(character);
        }

        return returnString;
    }
}