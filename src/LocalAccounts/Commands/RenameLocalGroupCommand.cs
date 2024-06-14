// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Security.Principal;

using Microsoft.PowerShell.LocalAccounts;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Rename-LocalGroup cmdlet renames a local security group in the Security
    /// Accounts Manager.
    /// </summary>
    [Cmdlet(VerbsCommon.Rename, "LocalGroup",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717978")]
    [Alias("rnlg")]
    public class RenameLocalGroupCommand : Cmdlet, IDisposable
    {
        #region Instance Data
        // Explicitly point DNS computer name to avoid very slow NetBIOS name resolutions.
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies the of the local group account to rename in the local Security
        /// Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "InputObject")]
        [ValidateNotNullOrEmpty]
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
        [ValidateNotNullOrEmpty]
        public string Name { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "NewName".
        /// Specifies the new name for the local security group in the Security Accounts
        /// Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 1)]
        [ValidateNotNullOrEmpty]
        public string NewName { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies a security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNullOrEmpty]
        public SecurityIdentifier SID { get; set; } = null!;
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            ProcessGroup();
            ProcessName();
            ProcessSid();
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        /// <summary>
        /// Process group requested by -Name.
        /// </summary>
        /// <remarks>
        /// Arguments to -Name will be treated as names,
        /// even if a name looks like a SID.
        /// </remarks>
        private void ProcessName()
        {
            if (Name is null)
            {
                return;
            }

            if (CheckShouldProcess(Name, NewName))
            {
                try
                {
                    using GroupPrincipal groupPrincipal = GroupPrincipal.FindByIdentity(_principalContext, IdentityType.SamAccountName, Name);
                    if (groupPrincipal is null)
                    {
                        WriteError(new ErrorRecord(new GroupNotFoundException(Name, new LocalGroup(Name)), "GroupNotFound", ErrorCategory.ObjectNotFound, Name));
                    }
                    else
                    {
                        try
                        {
                            DirectoryEntry entry = (DirectoryEntry)groupPrincipal.GetUnderlyingObject();
                            entry.Rename(NewName);
                            entry.CommitChanges();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            var exc = new AccessDeniedException(Strings.AccessDenied);

                            ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: LocalHelpers.GetTargetGroupObject(groupPrincipal)));
                        }
                        catch (System.Runtime.InteropServices.COMException e) when (e.ErrorCode == -2147022694)
                        {
                            var exc = new InvalidNameException(NewName, LocalHelpers.GetTargetGroupObject(groupPrincipal), e);

                            ThrowTerminatingError(new ErrorRecord(exc, "InvalidName", ErrorCategory.InvalidArgument, targetObject: LocalHelpers.GetTargetGroupObject(groupPrincipal)));
                        }
                        catch (System.Runtime.InteropServices.COMException e) when (e.ErrorCode == -2147023517)
                        {
                            var exc = new NameInUseException(NewName, LocalHelpers.GetTargetGroupObject(groupPrincipal), e);

                            ThrowTerminatingError(new ErrorRecord(exc, "NameInUse", ErrorCategory.InvalidArgument, targetObject: LocalHelpers.GetTargetGroupObject(groupPrincipal)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "InvalidLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: new LocalGroup(Name)));
                }
            }
        }

        /// <summary>
        /// Process group requested by -SID.
        /// </summary>
        private void ProcessSid()
        {
            if (SID is null)
            {
                return;
            }

            if (CheckShouldProcess(SID.Value, NewName))
            {
                try
                {
                    GroupPrincipal groupPrincipal = GroupPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, SID.Value);
                    if (groupPrincipal is null)
                    {
                        WriteError(new ErrorRecord(new GroupNotFoundException(SID.Value, SID.Value), "GroupNotFound", ErrorCategory.ObjectNotFound, SID.Value));
                    }
                    else
                    {
                        try
                        {
                            DirectoryEntry entry = (DirectoryEntry)groupPrincipal.GetUnderlyingObject();
                            entry.Rename(NewName);
                            entry.CommitChanges();
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
                    WriteError(new ErrorRecord(ex, "InvalidLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: new LocalGroup() { SID = SID }));
                }
            }
        }

        /// <summary>
        /// Process group given through -InputObject.
        /// </summary>
        private void ProcessGroup()
        {
            if (InputObject is null)
            {
                return;
            }

            LocalGroup group = InputObject;
            if (CheckShouldProcess(group.ToString(), NewName))
            {
                try
                {
                    using GroupPrincipal groupPrincipal = group.SID is not null
                        ? GroupPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, group.SID.Value)
                        : GroupPrincipal.FindByIdentity(_principalContext, group.Name);
                    if (groupPrincipal is null)
                    {
                        WriteError(new ErrorRecord(new GroupNotFoundException(group.ToString(), group), "GroupNotFound", ErrorCategory.ObjectNotFound, group));
                    }
                    else
                    {
                        try
                        {
                            DirectoryEntry entry = (DirectoryEntry)groupPrincipal.GetUnderlyingObject();
                            entry.Rename(NewName);
                            entry.CommitChanges();
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

        /// <summary>
        /// Determine if a group should be processed.
        /// Just a wrapper around Cmdlet.ShouldProcess, with localized string
        /// formatting.
        /// </summary>
        /// <param name="groupName">
        /// Name of the group to rename.
        /// </param>
        /// <param name="newName">
        /// New name for the group.
        /// </param>
        /// <returns>
        /// True if the group should be processed, false otherwise.
        /// </returns>
        private bool CheckShouldProcess(string? groupName, string newName)
        {
            string msg = StringUtil.Format(Strings.ActionRenameGroup, newName);

            return ShouldProcess(groupName, msg);
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
