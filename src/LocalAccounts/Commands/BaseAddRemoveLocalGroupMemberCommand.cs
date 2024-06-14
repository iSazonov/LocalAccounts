// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;
using System.Security.Principal;

using Microsoft.PowerShell.LocalAccounts;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The following is the base class for Add-LocalGroupMember and Remove-LocalGroupMember cmdlets.
    /// </summary>
    public abstract class BaseAddRemoveLocalGroupMemberCommand : BaseLocalGroupMemberCommand
    {
        // Set to true to add a member to the group.
        // Set to false to remove a member from the group.
        internal bool? _adding;

        /// <summary>
        /// The following is the definition of the input parameter "Member".
        /// Specifies one or more users or groups to add to this local group. You can
        /// identify users or groups by specifying their names or SIDs, or by passing
        /// Microsoft.PowerShell.Commands.LocalPrincipal objects.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 1,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public LocalPrincipal[] Member { get; set; } = null!;

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            GetGroup();

            foreach (LocalPrincipal member in Member)
            {
                if (member is null)
                {
                    continue;
                }

                try
                {
                    using Principal? principal = MakePrincipal(_groupPrincipal.Name, member);
                    if (principal is not null)
                    {
                        Debug.Assert(_adding is not null);
                        if (_adding.Value)
                        {
                            _groupPrincipal.Members.Add(principal);
                        }
                        else
                        {
                            if (!_groupPrincipal.Members.Remove(principal))
                            {
                                var exc = new MemberNotFoundException(member.ToString(), _groupPrincipal.Name);
                                WriteError(new ErrorRecord(exc, "MemberNotFound", ErrorCategory.ObjectNotFound, targetObject: member.ToString()));
                            }
                        }

                        _groupPrincipal.Save();
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    var exc = new AccessDeniedException(member);
                    ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: LocalHelpers.GetTargetGroupObject(_groupPrincipal)));
                }
                catch (PrincipalExistsException)
                {
                    var exc = new MemberExistsException(member.ToString(), _groupPrincipal.Name, LocalHelpers.GetTargetGroupObject(_groupPrincipal));
                    WriteError(new ErrorRecord(exc, "MemberExists", ErrorCategory.ResourceExists, targetObject: LocalHelpers.GetTargetGroupObject(_groupPrincipal)));
                }
                catch (PrincipalNotFoundException)
                {
                    var exc = new MemberNotFoundException(member.ToString(), _groupPrincipal.Name);
                    WriteError(new ErrorRecord(exc, "PrincipalNotFound", ErrorCategory.ObjectNotFound, targetObject: LocalHelpers.GetTargetGroupObject(_groupPrincipal)));
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "InvalidLocalGroupMemberOperation", ErrorCategory.InvalidOperation, targetObject: LocalHelpers.GetTargetGroupObject(_groupPrincipal)));

                }
            }
        }

        /// <summary>
        /// Creates a <see cref="Principal"/> object ready to be processed by the cmdlet.
        /// </summary>
        /// <param name="groupId">
        /// Name or SID (as a string) of the group we'll be adding to.
        /// This string is used only for specifying the target in WhatIf scenarios.
        /// </param>
        /// <param name="member">
        /// <see cref="LocalPrincipal"/> object to be processed.
        /// </param>
        /// <returns>
        /// A <see cref="Principal"/> object to be added to the group.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <see cref="LocalPrincipal"/> objects in the Member parameter may not be complete,
        /// particularly those created from a name or a SID string given to the Member cmdlet parameter.
        /// The object returned from this method contains, at the very least, a valid SID.
        /// </para>
        /// <para>
        /// Any Member objects provided by name or SID string will be looked up to ensure that such an object exists.
        /// If an object is not found, null will be returned.
        /// </para>
        /// <para>
        /// This method also handles the WhatIf scenario. If the Cmdlet's
        /// ShouldProcess method returns false on any Member object,
        /// that object will not be included in the returned List.
        /// </para>
        /// </remarks>
        private Principal? MakePrincipal(string groupId, LocalPrincipal member)
        {
            Principal principal;

            // If the member has a SID, we can use it directly.
            if (member.SID is not null)
            {
                principal = Principal.FindByIdentity(_principalMachineContext, IdentityType.Sid, member.SID.Value) ?? Principal.FindByIdentity(_principalDomainContext, IdentityType.Sid, member.SID.Value);
            }
            else
            {
                // Otherwise it must have been constructed by name.
                SecurityIdentifier? sid = this.TrySid(member.Name);

                if (sid is not null)
                {
                    member.SID = sid;
                    principal = Principal.FindByIdentity(_principalMachineContext, IdentityType.Sid, member.SID.Value) ?? Principal.FindByIdentity(_principalDomainContext, IdentityType.Sid, member.SID.Value);
                }
                else
                {
                    principal = Principal.FindByIdentity(_principalMachineContext, IdentityType.SamAccountName, member.Name) ?? Principal.FindByIdentity(_principalDomainContext, IdentityType.SamAccountName, member.Name);
                }
            }

            if (principal is null)
            {
                // It is a breaking change. AccountManagement API can not add a member by a fake SID, Windows PowerShell can do.
                WriteError(new ErrorRecord(new PrincipalNotFoundException(member.ToString(), member), "PrincipalNotFound", ErrorCategory.ObjectNotFound, member));

                return null;
            }

            return CheckShouldProcess(principal, groupId) ? principal : null;
        }

        /// <summary>
        /// Determine if a principal should be processed.
        /// Just a wrapper around Cmdlet.ShouldProcess, with localized string formatting.
        /// </summary>
        /// <param name="principal">Name of the principal to be added.</param>
        /// <param name="groupName">
        /// Name of the group to which the members will be added.
        /// </param>
        /// <returns>
        /// True if the principal should be processed, false otherwise.
        /// </returns>
        private bool CheckShouldProcess(Principal principal, string groupName)
        {
            if (principal == null)
            {
                return false;
            }

            string msg = StringUtil.Format(Strings.ActionAddGroupMember, principal.Name);

            return ShouldProcess(groupName, msg);
        }
    }
}
