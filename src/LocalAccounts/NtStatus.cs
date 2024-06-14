// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.SecurityAccountsManager.Native
{
    internal static class NtStatus
    {
        #region Constants
        //
        //  These values are taken from ntstatus.h
        //

        //
        // Severity codes
        //
        public const UInt32 STATUS_SEVERITY_WARNING = 0x2;
        public const UInt32 STATUS_SEVERITY_SUCCESS = 0x0;
        public const UInt32 STATUS_SEVERITY_INFORMATIONAL = 0x1;
        public const UInt32 STATUS_SEVERITY_ERROR = 0x3;

        public const UInt32 STATUS_SUCCESS = 0x00000000;
        #endregion Constants

        #region Public Methods
        /// <summary>
        /// Determine if an NTSTATUS value indicates Success.
        /// </summary>
        /// <param name="ntstatus">The NTSTATUS value returned from native functions.</param>
        /// <returns>
        /// True if the NTSTATUS value indicates success, false otherwise.
        /// </returns>
        public static bool IsSuccess(UInt32 ntstatus)
        {
            return Severity(ntstatus) == STATUS_SEVERITY_SUCCESS;
        }

        /// <summary>
        /// Determine if an NTSTATUS value indicates an Error.
        /// </summary>
        /// <param name="ntstatus">The NTSTATUS value returned from native functions.</param>
        /// <returns>
        /// True if the NTSTATUS value indicates an error, false otherwise.
        /// </returns>
        public static bool IsError(UInt32 ntstatus)
        {
            return Severity(ntstatus) == STATUS_SEVERITY_ERROR;
        }

        /// <summary>
        /// Determine if an NTSTATUS value indicates a Warning.
        /// </summary>
        /// <param name="ntstatus">The NTSTATUS value returned from native functions.</param>
        /// <returns>
        /// True if the NTSTATUS value indicates a warning, false otherwise.
        /// </returns>
        public static bool IsWarning(UInt32 ntstatus)
        {
            return Severity(ntstatus) == STATUS_SEVERITY_WARNING;
        }

        /// <summary>
        /// Determine if an NTSTATUS value indicates that the value is Informational.
        /// </summary>
        /// <param name="ntstatus">The NTSTATUS value returned from native functions.</param>
        /// <returns>
        /// True if the NTSTATUS value indicates that it is informational, false otherwise.
        /// </returns>
        public static bool IsInformational(UInt32 ntstatus)
        {
            return Severity(ntstatus) == STATUS_SEVERITY_INFORMATIONAL;
        }

        /// <summary>
        /// Return the Severity part of an NTSTATUS value.
        /// </summary>
        /// <param name="ntstatus">The NTSTATUS value returned from native functions.</param>
        /// <returns>
        /// One of the STATUS_SEVERITY_* values
        /// </returns>
        public static uint Severity(UInt32 ntstatus)
        {
            return ntstatus >> 30;
        }
        #endregion Public Methods
    }
}
