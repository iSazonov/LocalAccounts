// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Enable-LocalUser cmdlet enables local user accounts. When a user account
    /// is disabled, the user is not permitted to log on. When a user account is
    /// enabled, the user is permitted to log on normally.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Enable, "LocalUser",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717985")]
    [Alias("elu")]
    public class EnableLocalUserCommand : AbilityLocalUserCommand
    {
        /// <summary>
        /// Initialize the cmdlet.
        /// </summary>
        public EnableLocalUserCommand()
        {
            _ability = true;
        }
     }
}
