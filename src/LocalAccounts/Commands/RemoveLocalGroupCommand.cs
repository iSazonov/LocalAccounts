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
    /// The Remove-LocalGroup cmdlet deletes a security group from the Windows
    /// Security Accounts manager.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "LocalGroup",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717975")]
    [Alias("rlg")]
    public class RemoveLocalGroupCommand : Cmdlet, IDisposable
    {
        #region Instance Data
        // Explicitly point DNS computer name to avoid very slow NetBIOS name resolutions.
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies security groups from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "InputObject")]
        [ValidateNotNullOrEmpty]
        public LocalGroup[] InputObject { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the local groups to be deleted from the local Security Accounts
        /// Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNullOrEmpty]
        public string[] Name { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies the LocalGroup accounts to remove by
        /// System.Security.Principal.SecurityIdentifier.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNullOrEmpty]
        public SecurityIdentifier[] SID { get; set; } = null!;
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            ProcessGroups();
            ProcessNames();
            ProcessSids();
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        /// <summary>
        /// Process groups requested by -Name.
        /// </summary>
        /// <remarks>
        /// All arguments to -Name will be treated as names,
        /// even if a name looks like a SID.
        /// </remarks>
        private void ProcessNames()
        {
            if (Name is null)
            {
                return;
            }

            foreach (var name in Name)
            {
                if (name is null)
                {
                    continue;
                }

                if (CheckShouldProcess(name))
                {
                    try
                    {
                        using GroupPrincipal groupPrincipal = GroupPrincipal.FindByIdentity(_principalContext, IdentityType.SamAccountName, name);
                        if (groupPrincipal is null)
                        {
                            WriteError(new ErrorRecord(new GroupNotFoundException(name, new LocalGroup(name)), "GroupNotFound", ErrorCategory.ObjectNotFound, name));
                        }
                        else
                        {
                            try
                            {
                                groupPrincipal.Delete();
                            }
                            catch (UnauthorizedAccessException)
                            {
                                var exc = new AccessDeniedException(Strings.AccessDenied);

                                ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: LocalHelpers.GetTargetGroupObject(groupPrincipal)));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(ex, "InvalidLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: new LocalGroup(name)));
                    }
                }
            }
        }

        /// <summary>
        /// Process groups requested by -SID.
        /// </summary>
        private void ProcessSids()
        {
            if (SID is null)
            {
                return;
            }

            foreach (SecurityIdentifier sid in SID)
            {
                if (sid is null)
                {
                    continue;
                }

                if (CheckShouldProcess(sid.Value))
                {
                    try
                    {
                        using GroupPrincipal groupPrincipal = GroupPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, sid.Value);
                        if (groupPrincipal is null)
                        {
                            WriteError(new ErrorRecord(new GroupNotFoundException(sid.Value, sid.Value), "GroupNotFound", ErrorCategory.ObjectNotFound, sid.Value));
                        }
                        else
                        {
                            try
                            {
                                groupPrincipal.Delete();
                            }
                            catch (UnauthorizedAccessException)
                            {
                                var exc = new AccessDeniedException(Strings.AccessDenied);

                                ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: LocalHelpers.GetTargetGroupObject(groupPrincipal)));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(ex, "InvalidLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: new LocalGroup() { SID = sid }));
                    }
                }
            }
        }

        /// <summary>
        /// Process groups given through -InputObject.
        /// </summary>
        private void ProcessGroups()
        {
            if (InputObject is null)
            {
                return;
            }

            foreach (LocalGroup group in InputObject)
            {
                if (group is null)
                {
                    continue;
                }

                if (CheckShouldProcess(group.ToString()))
                {
                    try
                    {
                        using GroupPrincipal groupPrincipal = group.SID is not null
                            ? GroupPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, group.SID.Value)
                            : GroupPrincipal.FindByIdentity(_principalContext, group.Name);
                        if (groupPrincipal is null)
                        {
                            WriteError(new ErrorRecord(new GroupNotFoundException(group.ToString()), "GroupNotFound", ErrorCategory.ObjectNotFound, group));
                        }
                        else
                        {
                            try
                            {
                                groupPrincipal.Delete();
                            }
                            catch (UnauthorizedAccessException)
                            {
                                var exc = new AccessDeniedException(Strings.AccessDenied);

                                ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: LocalHelpers.GetTargetGroupObject(groupPrincipal)));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(ex, "InvalidLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: new LocalGroup(group.Name) { SID = group.SID }));
                    }
                }
            }
        }

        private bool CheckShouldProcess(string target)
        {
            return ShouldProcess(target, Strings.ActionRemoveGroup);
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
