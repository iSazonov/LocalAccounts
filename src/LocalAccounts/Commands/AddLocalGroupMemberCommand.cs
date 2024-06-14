// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Add-LocalGroupMember cmdlet adds one or more users or groups to a local group.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "LocalGroupMember",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717987")]
    [Alias("algm")]
    public class AddLocalGroupMemberCommand : BaseAddRemoveLocalGroupMemberCommand
    {
        /// <summary>
        /// Initialize the cmdlet.
        /// </summary>
        public AddLocalGroupMemberCommand()
        {
            _adding = true;
        }
    }
}
