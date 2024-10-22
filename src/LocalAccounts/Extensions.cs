// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace LocalAccounts.Extensions
{
    /// <summary>
    /// Provides extension methods for the Cmdlet class.
    /// </summary>
    internal static class CmdletExtensions
    {
        /// <summary>
        /// Attempt to create a SID from a string.
        /// </summary>
        /// <param name="cmdlet">The cmdlet being extended with this method.</param>
        /// <param name="s">The string to be converted to a SID.</param>
        /// <param name="allowSidConstants">
        /// A boolean indicating whether SID constants, such as "BA", are considered.
        /// </param>
        /// <returns>
        /// A <see cref="SecurityIdentifier"/> object if the conversion was successful,
        /// null otherwise.
        /// </returns>
        internal static SecurityIdentifier? TrySid(this Cmdlet cmdlet,
                                                  string? s,
                                                  bool allowSidConstants = false)
        {
            if (s is null)
            {
                return null;
            }

            if (!allowSidConstants)
            {
                if (!(s.Length > 2 && s.StartsWith("S-", StringComparison.Ordinal) && char.IsDigit(s[2])))
                {
                    return null;
                }
            }

            SecurityIdentifier? sid = null;

            try
            {
                sid = new SecurityIdentifier(s);
            }
            catch (ArgumentException)
            {
                // do nothing here, just fall through to the return
            }

            return sid;
        }
    }

    /// <summary>
    /// Provides extension methods for the PSCmdlet class.
    /// </summary>
    internal static class PSExtensions
    {
        /// <summary>
        /// Determine if a given parameter was provided to the cmdlet.
        /// </summary>
        /// <param name="cmdlet">
        /// The <see cref="PSCmdlet"/> object to check.
        /// </param>
        /// <param name="parameterName">
        /// A string containing the name of the parameter. This should be in the
        /// same letter-casing as the defined parameter.
        /// </param>
        /// <returns>
        /// True if the specified parameter was given on the cmdlet invocation,
        /// false otherwise.
        /// </returns>
        internal static bool HasParameter(this PSCmdlet cmdlet, string parameterName)
        {
            InvocationInfo invocation = cmdlet.MyInvocation;
            if (invocation is not null)
            {
                Dictionary<string, object> parameters = invocation.BoundParameters;

                if (parameters is not null)
                {
                    // PowerShell sets the parameter names in the BoundParameters dictionary
                    // to their "proper" casing, so we don't have to do a case-insensitive search.
                    if (parameters.ContainsKey(parameterName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    internal static class SecureStringExtensions
    {
        /// <summary>
        /// Extension method to extract clear text from a
        /// <see cref="System.Security.SecureString"/> object.
        /// </summary>
        /// <param name="str">
        /// This SecureString object, containing encrypted text.
        /// </param>
        /// <returns>
        /// A string containing the SecureString object's original text.
        /// </returns>
        internal static string? AsString(this SecureString str)
        {
            // It was workaround for old .Net Core.
            // IntPtr buffer = SecureStringMarshal.SecureStringToCoTaskMemUnicode(str);
            // string? clear = Marshal.PtrToStringUni(buffer);
            // Marshal.ZeroFreeCoTaskMemUnicode(buffer);

            var bstr = Marshal.SecureStringToBSTR(str);
            string? clear = Marshal.PtrToStringAuto(bstr);
            Marshal.ZeroFreeBSTR(bstr);

            return clear;
        }
    }

    internal static class ExceptionExtensions
    {
        internal static ErrorRecord MakeErrorRecord(this Exception ex,
                                                    string errorId,
                                                    ErrorCategory errorCategory,
                                                    object? target = null)
        {
            return new ErrorRecord(ex, errorId, errorCategory, target);
        }

        internal static ErrorRecord MakeErrorRecord(this Exception ex, object? target = null)
        {
            // This part is somewhat less than beautiful, but it prevents
            // having to have multiple exception handlers in every cmdlet command.
            if (ex is LocalAccountsException exTemp)
            {
                return MakeErrorRecord(exTemp, target ?? exTemp.Target);
            }

            return new ErrorRecord(ex,
                                   Strings.UnspecifiedError,
                                   ErrorCategory.NotSpecified,
                                   target);
        }

        internal static ErrorRecord MakeErrorRecord(this LocalAccountsException ex, object? target = null)
        {
            return ex.MakeErrorRecord(ex.ErrorName, ex.ErrorCategory, target ?? ex.Target);
        }
    }
}
