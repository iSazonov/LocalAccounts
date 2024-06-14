// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Security.Principal;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The following is the base class for Get-LocalGroupMember, Add-LocalGroupMember and Remove-LocalGroupMember cmdlets.
    /// </summary>
    public abstract class BaseLocalGroupMemberCommand : Cmdlet, IDisposable
    {
        #region Instance Data
        /// <summary>
        /// The context used to search for domain users.
        /// </summary>
        // Explicitly point a domain name of the computer otherwise a domain name of current user would be used by default.
        protected internal  PrincipalContext _principalDomainContext = new PrincipalContext(ContextType.Domain, LocalHelpers.GetComputerDomainName());

        /// <summary>
        /// The context used to search for local users.
        /// </summary>
        // Explicitly point a domain name of the computer otherwise a domain name of current user would be used by default.
        protected internal  PrincipalContext _principalMachineContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());

        /// <summary>
        /// The group on which operations are performed.
        /// </summary>
        protected internal  GroupPrincipal? _groupPrincipal;

        /// <summary>
        /// The context used to search for local groups.
        /// </summary>
        // Explicitly point DNS computer name to avoid very slow NetBIOS name resolutions.
        protected internal  PrincipalContext _groupPrincipalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "Group".
        /// The security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Group")]
        [ValidateNotNull]
        public LocalGroup Group { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// The security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// The security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNullOrEmpty]
        public SecurityIdentifier SID { get; set; } = null!;
        #endregion Parameter Properties

        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(_groupPrincipal))]
        internal void GetGroup()
        {
            try
            {
                if (Group is not null)
                {
                    _groupPrincipal = Group.SID is not null
                        ? GroupPrincipal.FindByIdentity(_groupPrincipalContext, IdentityType.Sid, Group.SID.Value)
                        : GroupPrincipal.FindByIdentity(_groupPrincipalContext, Group.Name);
                }
                else if (Name is not null)
                {
                    _groupPrincipal = GroupPrincipal.FindByIdentity(_groupPrincipalContext, IdentityType.SamAccountName, Name);

                }
                else if (SID is not null)
                {
                    _groupPrincipal = GroupPrincipal.FindByIdentity(_groupPrincipalContext, IdentityType.Sid, SID.Value);
                }
            }
            catch (Exception ex)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "GroupNotFound", ErrorCategory.ObjectNotFound, Group ?? new LocalGroup(Name) { SID = SID }));
            }

            if (_groupPrincipal is null)
            {
                LocalGroup target = Group ?? new LocalGroup(Name) { SID = SID };
                ThrowTerminatingError(new ErrorRecord(new GroupNotFoundException(target.ToString(), target), "GroupNotFound", ErrorCategory.ObjectNotFound, target));
            }
        }

        #region IDisposable interface
        private bool _disposed;

        /// <summary>
        /// Dispose the command.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implementation of IDisposable for both manual Dispose() and finalizer-called disposal of resources.
        /// </summary>
        /// <param name="disposing">
        /// Specified as true when Dispose() was called, false if this is called from the finalizer.
        /// </param>
        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _groupPrincipal?.Dispose();
                    _groupPrincipalContext?.Dispose();
                    _principalDomainContext?.Dispose();
                    _principalMachineContext?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion
    }
}
