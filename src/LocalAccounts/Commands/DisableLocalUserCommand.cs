// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Disable-LocalUser cmdlet disables local user accounts. When a user
    /// account is disabled, the user is not permitted to log on. When a user
    /// account is enabled, the user is permitted to log on normally.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Disable, "LocalUser",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717986")]
    [Alias("dlu")]
    public class DisableLocalUserCommand : AbilityLocalUserCommand
    {
        /// <summary>
        /// Initialize the cmdlet.
        /// </summary>
        public DisableLocalUserCommand()
        {
            _ability = false;
        }

    }
}
