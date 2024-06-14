// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;

namespace System.Management.Automation.SecurityAccountsManager
{
    /// <summary>
    /// Contains utility functions for formatting localizable strings.
    /// </summary>
    internal static class StringUtil
    {
        internal static string Format(string str)
        {
            return string.Format(CultureInfo.CurrentCulture, str);
        }

        internal static string Format(string fmt, string p0)
        {
            return string.Format(CultureInfo.CurrentCulture, fmt, p0);
        }

        internal static string Format(string fmt, string p0, string p1)
        {
            return string.Format(CultureInfo.CurrentCulture, fmt, p0, p1);
        }

        internal static string Format(string fmt, uint p0)
        {
            return string.Format(CultureInfo.CurrentCulture, fmt, p0);
        }

        internal static string Format(string fmt, int p0)
        {
            return string.Format(CultureInfo.CurrentCulture, fmt, p0);
        }
    }
}
