// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;

using Microsoft.PowerShell.Commands;

namespace System.Management.Automation.SecurityAccountsManager;

/// <summary>
/// Contains utility functions for user and group operations.
/// </summary>
internal static class LocalHelpers
{
    /// <summary>
    /// Get FQDN computer name.
    /// </summary>
    internal static string GetFullComputerName()
        => Net.Dns.GetHostEntry(Environment.MachineName).HostName;

    /// <summary>
    /// Get a domain name if the local computer.
    /// </summary>
    internal static string GetComputerDomainName()
        => System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;

    /// <summary>
    /// Get all local users.
    /// </summary>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{LocalUser}"/> object containing LocalUser objects.
    /// </returns>
    internal static IEnumerable<LocalUser> GetAllLocalUsers(PrincipalContext principalContext)
        => GetMatchingLocalUsers(static _ => true, principalContext);

    /// <summary>
    /// Get local user whose a name satisfy the specified name.
    /// </summary>
    /// <param name="name">
    /// A user name.
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="LocalUser"/> object for a user with the specified name.
    /// </returns>
    internal static LocalUser? GetMatchingLocalUsersByName(string name, PrincipalContext principalContext)
        => GetMatchingLocalUsers(userPrincipal => name.Equals(userPrincipal.Name, StringComparison.CurrentCultureIgnoreCase), principalContext).FirstOrDefault();

    /// <summary>
    /// Get local user whose a security identifier (SID) satisfy the specified SID.
    /// </summary>
    /// <param name="sid">
    /// A user a security identifier (SID).
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="LocalUser"/> object for a user with the specified security identifier (SID).
    /// </returns>
    internal static LocalUser? GetMatchingLocalUsersBySID(SecurityIdentifier sid, PrincipalContext principalContext)
        => GetMatchingLocalUsers(userPrincipal => sid.Equals(userPrincipal.Sid), principalContext).FirstOrDefault();

    /// <summary>
    /// Get all local users whose properties satisfy the specified predicate.
    /// </summary>
    /// <param name="principalFilter">
    /// Predicate that determines whether a user satisfies the conditions.
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{LocalUser}"/> object containing LocalUser
    /// objects that satisfy the predicate condition.
    /// </returns>
    internal static IEnumerable<LocalUser> GetMatchingLocalUsers(Predicate<UserPrincipal> principalFilter, PrincipalContext principalContext)
    {
        using var queryFilter = new UserPrincipal(principalContext);
        using var searcher = new PrincipalSearcher(queryFilter);
        foreach (UserPrincipal user in searcher.FindAll().Cast<UserPrincipal>())
        {
            using (user)
            {
                if (!principalFilter(user))
                {
                    continue;
                }

                yield return GetLocalUser(user);
            }
        }
    }

    internal static LocalUser GetLocalUser(UserPrincipal user)
    {
        DateTime? lastPasswordSet = null;
        DateTime? passwordChangeableDate = null;
        DateTime? passwordExpires = null;

        if (user.LastPasswordSet is DateTime lastPasswordSetValue)
        {
            DirectoryEntry entry = (DirectoryEntry)user.GetUnderlyingObject();
            int minPasswordAge = Convert.ToInt32(entry.Properties["MinPasswordAge"].Value);
            int maxPasswordAge = Convert.ToInt32(entry.Properties["MaxPasswordAge"].Value);

            lastPasswordSetValue = lastPasswordSetValue.ToLocalTime();

            lastPasswordSet = lastPasswordSetValue;
            passwordChangeableDate = lastPasswordSetValue.AddSeconds(minPasswordAge);
            passwordExpires = user.PasswordNeverExpires ? null : lastPasswordSetValue.AddSeconds(maxPasswordAge);
        }

        var localUser = new LocalUser()
        {
            AccountExpires = user.AccountExpirationDate,
            Description = user.Description ?? string.Empty,
            Enabled = user.Enabled ?? false,
            FullName = user.DisplayName ?? string.Empty,
            LastLogon = user.LastLogon,
            Name = user.Name,
            PasswordChangeableDate = passwordChangeableDate,
            PasswordExpires = passwordExpires,
            PasswordLastSet = lastPasswordSet,
            PasswordRequired = !user.PasswordNotRequired,
            PrincipalSource = Sam.GetPrincipalSource(user.Sid),
            SID = user.Sid,
            UserMayChangePassword = !user.UserCannotChangePassword,
        };

        return localUser;
    }

    /// <summary>
    /// Get all local groups.
    /// </summary>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{LocalGroup}"/> object containing LocalGroup objects.
    /// </returns>
    internal static IEnumerable<LocalGroup> GetAllLocalGroups(PrincipalContext principalContext)
        => GetMatchingLocalGroups(static _ => true, principalContext);

    /// <summary>
    /// Get local group whose a name satisfy the specified name.
    /// </summary>
    /// <param name="name">
    /// A group name.
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="LocalGroup"/> object for a group with the specified name.
    /// </returns>
    internal static LocalGroup? GetMatchingLocalGroupsByName(string name, PrincipalContext principalContext)
        => GetMatchingLocalGroups(userPrincipal => name.Equals(userPrincipal.Name, StringComparison.CurrentCultureIgnoreCase), principalContext).FirstOrDefault();

    /// <summary>
    /// Get local group whose a security identifier (SID) satisfy the specified SID.
    /// </summary>
    /// <param name="sid">
    /// A group a security identifier (SID).
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="LocalGroup"/> object for a group with the specified security identifier (SID).
    /// </returns>
    internal static LocalGroup? GetMatchingLocalGroupsBySID(SecurityIdentifier sid, PrincipalContext principalContext)
        => GetMatchingLocalGroups(userPrincipal => sid.Equals(userPrincipal.Sid), principalContext).FirstOrDefault();

    /// <summary>
    /// Get all local groups whose properties satisfy the specified predicate.
    /// </summary>
    /// <param name="principalFilter">
    /// Predicate that determines whether a group satisfies the conditions.
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{LocalGroup}"/> object containing LocalGroup
    /// objects that satisfy the predicate condition.
    /// </returns>
    internal static IEnumerable<LocalGroup> GetMatchingLocalGroups(Predicate<GroupPrincipal> principalFilter, PrincipalContext principalContext)
    {
        foreach (GroupPrincipal group in GetMatchingGroupPrincipals(principalFilter, principalContext))
        {
            using (group)
            {
                var localGroup = new LocalGroup()
                {
                    Description = group.Description,
                    Name = group.Name,
                    PrincipalSource = Sam.GetPrincipalSource(group.Sid),
                    SID = group.Sid,
                };

                yield return localGroup;
            }
        }
    }

    /// <summary>
    /// Get all local groups whose properties satisfy the specified predicate.
    /// </summary>
    /// <param name="principalFilter">
    /// Predicate that determines whether a group satisfies the conditions.
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{GroupPrincipal}"/> object containing GroupPrincipal
    /// objects that satisfy the predicate condition.
    /// </returns>
    internal static IEnumerable<GroupPrincipal> GetMatchingGroupPrincipals(Predicate<GroupPrincipal> principalFilter, PrincipalContext principalContext)
    {
        using var queryFilter = new GroupPrincipal(principalContext);
        using var searcher = new PrincipalSearcher(queryFilter);
        foreach (GroupPrincipal group in searcher.FindAll().Cast<GroupPrincipal>())
        {
            if (principalFilter(group))
            {
                yield return group;
            }
        }
    }

    /// <summary>
    /// Cretae new <see cref="LocalGroup"/> object from the GroupPrincipal object.
    /// </summary>
    /// <param name="group">
    /// An <see cref="GroupPrincipal"/> object.
    /// </param>
    /// <returns>
    /// An <see cref="LocalGroup"/> object corresponding to the GroupPrincipal parameter.
    /// </returns>
    internal static LocalGroup? GetTargetGroupObject(GroupPrincipal? group)
    => group is null ? null : new LocalGroup()
    {
        Description = group.Description,
        Name = group.Name,
        PrincipalSource = Sam.GetPrincipalSource(group.Sid),
        SID = group.Sid,
    };

    /// <summary>
    /// Cretae new <see cref="LocalGroup"/> object from the UserPrincipal object.
    /// </summary>
    /// <param name="user">
    /// An <see cref="UserPrincipal"/> object.
    /// </param>
    /// <returns>
    /// An <see cref="LocalGroup"/> object corresponding to the UserPrincipal parameter.
    /// </returns>
    internal static LocalUser GetTargetUserObject(UserPrincipal user)
    => new LocalUser()
    {
        Description = user.Description,
        Name = user.Name,
        PrincipalSource = Sam.GetPrincipalSource(user.Sid),
        SID = user.Sid,
    };

    /// <summary>
    /// Generate a string with random chars.
    /// </summary>
    /// <returns>
    /// A string with random chars.
    /// </returns>
    internal static string GenerateRandomString()
    {
        const int ResultLength = 32;
        const string CHARACTERS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_=+{}[]?<>.,\\";
        char[] random;

        while (true)
        {
            random = System.Security.Cryptography.RandomNumberGenerator.GetItems<char>(CHARACTERS, ResultLength);

            if (!random.AsSpan().ContainsAny(s_specialChars))
            {
                continue;
            }

            if (random.AsSpan().IndexOfAnyInRange('0', '9') < 0)
            {
                continue;
            }

            if (random.AsSpan().IndexOfAnyInRange('A', 'Z') < 0)
            {
                continue;
            }

            if (random.AsSpan().IndexOfAnyInRange('a', 'z') < 0)
            {
                continue;
            }

            break;
        }

        string randomString = random.ToString() ?? throw new InvalidPasswordException();

        return randomString;
    }

    private static readonly System.Buffers.SearchValues<char> s_specialChars = System.Buffers.SearchValues.Create("!@#$%^&*()-_=+{}[]?<>.,\\");
}
