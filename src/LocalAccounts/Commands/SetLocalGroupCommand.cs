// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Security.Principal;
using Microsoft.PowerShell.LocalAccounts;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Set-LocalGroup cmdlet modifies the properties of a local security group
    /// in the Windows Security Accounts Manager.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "LocalGroup",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717979")]
    [Alias("slg")]
    public class SetLocalGroupCommand : Cmdlet, IDisposable
    {
        #region Instance Data
        // Explicitly point DNS computer name to avoid very slow NetBIOS name resolutions.
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "Description".
        /// A descriptive comment.
        /// </summary>
        [Parameter(Mandatory = true)]
        [ValidateNotNull]
        public string Description { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies the local group account to modify in the local Security
        /// Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "InputObject")]
        [ValidateNotNull]
        public LocalGroup InputObject { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the local group to be renamed in the local Security Accounts
        /// Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNull]
        public string Name { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies a security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNull]
        public SecurityIdentifier SID { get; set; } = null!;
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            GroupPrincipal? groupPrincipal = null;
            try
            {
                LocalGroup group = InputObject;

                if (group is not null)
                {
                    if (CheckShouldProcess(InputObject.ToString()))
                    {
                        groupPrincipal = group.SID is not null
                            ? GroupPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, group.SID.Value)
                            : GroupPrincipal.FindByIdentity(_principalContext, group.Name);

                        if (groupPrincipal is null)
                        {
                            WriteError(new ErrorRecord(new GroupNotFoundException(Name, Name), "GroupNotFound", ErrorCategory.ObjectNotFound, Name));
                        }
                    }
                }
                else if (Name is not null)
                {
                    if (CheckShouldProcess(Name))
                    {
                        groupPrincipal = GroupPrincipal.FindByIdentity(_principalContext, IdentityType.SamAccountName, Name);

                        if (groupPrincipal is null)
                        {
                            WriteError(new ErrorRecord(new GroupNotFoundException(Name, Name), "GroupNotFound", ErrorCategory.ObjectNotFound, Name));
                        }
                    }
                }
                else if (SID is not null)
                {
                    groupPrincipal = GroupPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, SID.Value);

                    if (groupPrincipal is null)
                    {
                        WriteError(new ErrorRecord(new GroupNotFoundException(SID.Value, SID), "GroupNotFound", ErrorCategory.ObjectNotFound, SID));
                    }

                    if (groupPrincipal is not null && !CheckShouldProcess(groupPrincipal.Name))
                    {
                        groupPrincipal.Dispose();
                        groupPrincipal = null;
                    }
                }

                if (groupPrincipal is not null)
                {
                    groupPrincipal.Description = Description;
                    groupPrincipal.Save();
                }
            }
            catch (UnauthorizedAccessException)
            {
                var exc = new AccessDeniedException(Strings.AccessDenied);

                ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: LocalHelpers.GetTargetGroupObject(groupPrincipal)));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "InvalidLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: InputObject ?? new LocalGroup(Name) { SID = SID }));
            }
            finally
            {
                groupPrincipal?.Dispose();
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        private bool CheckShouldProcess(string? target)
        {
            return ShouldProcess(target, Strings.ActionSetGroup);
        }
        #endregion Private Methods

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
                    _principalContext?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion
    }
}
