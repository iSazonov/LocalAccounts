// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Remove-LocalGroupMember cmdlet removes one or more members (users or groups) from a local security group.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "LocalGroupMember",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717989")]
    [Alias("rlgm")]
    public class RemoveLocalGroupMemberCommand : BaseAddRemoveLocalGroupMemberCommand
    {
        /// <summary>
        /// Initialize the cmdlet.
        /// </summary>
        public RemoveLocalGroupMemberCommand()
        {
            _adding = false;
        }
    }
}
