// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace System.Management.Automation.SecurityAccountsManager.Native
{
    #region Enums
    internal enum LSA_USER_ACCOUNT_TYPE
    {
        UnknownUserAccountType = 0,
        LocalUserAccountType,
        PrimaryDomainUserAccountType,
        ExternalDomainUserAccountType,
        LocalConnectedUserAccountType,  // Microsoft Account
        AADUserAccountType,
        InternetUserAccountType,        // Generic internet User (eg. if the SID supplied is MSA's internet SID)
        MSAUserAccountType      // !!! NOT YET IN THE ENUM SPECIFIED IN THE C API !!!

    }
    #endregion Enums

    internal static class Win32
    {
        #region Win32 Functions
        [DllImport("api-ms-win-security-lsalookup-l1-1-2.dll")]
        internal static extern UInt32 LsaLookupUserAccountType([MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
                                                               out LSA_USER_ACCOUNT_TYPE accountType);
        #endregion LSA Functions
    }
}
