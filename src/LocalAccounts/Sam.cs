// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.SecurityAccountsManager.Extensions;
using System.Management.Automation.SecurityAccountsManager.Native;
using System.Security.Principal;

using Microsoft.PowerShell.Commands;

[assembly:System.Runtime.Versioning.SupportedOSPlatform("windows")]

namespace System.Management.Automation.SecurityAccountsManager
{
    /// <summary>
    /// Provides methods for manipulating local Users and Groups.
    /// </summary>
    internal static class Sam
    {
        #region Utility Methods
        /// <summary>
        /// Determine the source of a user or group. Either local, Active Directory,
        /// or Azure AD.
        /// </summary>
        /// <param name="sid">
        /// A <see cref="SecurityIdentifier"/> object identifying the user or group.
        /// </param>
        /// <returns>
        /// One of the <see cref="PrincipalSource"/> enumerations identifying the
        /// source of the object.
        /// </returns>
        internal static PrincipalSource? GetPrincipalSource(SecurityIdentifier sid)
        {
            var bSid = new byte[sid.BinaryLength];

            sid.GetBinaryForm(bSid, 0);

            LSA_USER_ACCOUNT_TYPE type = LSA_USER_ACCOUNT_TYPE.UnknownUserAccountType;

            // Use LsaLookupUserAccountType for Windows 10 and later.
            // Earlier versions of the OS will leave the property NULL because
            // it is too error prone to attempt to replicate the decisions of
            // LsaLookupUserAccountType.
            if (Environment.OSVersion.Version.Major >= 10)
            {
                UInt32 status = Win32.LsaLookupUserAccountType(bSid, out type);
                if (NtStatus.IsError(status))
                {
                    type = LSA_USER_ACCOUNT_TYPE.UnknownUserAccountType;
                }

                switch (type)
                {
                    case LSA_USER_ACCOUNT_TYPE.ExternalDomainUserAccountType:
                    case LSA_USER_ACCOUNT_TYPE.PrimaryDomainUserAccountType:
                        return PrincipalSource.ActiveDirectory;

                    case LSA_USER_ACCOUNT_TYPE.LocalUserAccountType:
                        return PrincipalSource.Local;

                    case LSA_USER_ACCOUNT_TYPE.AADUserAccountType:
                        return PrincipalSource.AzureAD;

                    // Currently, there is no value returned by LsaLookupUserAccountType
                    // that corresponds to LSA_USER_ACCOUNT_TYPE.MSAUserAccountType,
                    // but there may be in the future, so we'll account for it here.
                    case LSA_USER_ACCOUNT_TYPE.MSAUserAccountType:
                    case LSA_USER_ACCOUNT_TYPE.LocalConnectedUserAccountType:
                        return PrincipalSource.MicrosoftAccount;

                    case LSA_USER_ACCOUNT_TYPE.InternetUserAccountType:
                        return sid.IsMsaAccount()
                            ? PrincipalSource.MicrosoftAccount
                            : PrincipalSource.Unknown;

                    case LSA_USER_ACCOUNT_TYPE.UnknownUserAccountType:
                    default:
                        return PrincipalSource.Unknown;
                }
            }
            else
            {
                return null;
            }
        }
        #endregion Utility Methods
    }
}
