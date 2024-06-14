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
    /// The Rename-LocalUser cmdlet renames a local user account in the Security
    /// Accounts Manager.
    /// </summary>
    [Cmdlet(VerbsCommon.Rename, "LocalUser",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkID=717983")]
    [Alias("rnlu")]
    public class RenameLocalUserCommand : Cmdlet, IDisposable
    {
        #region Instance Data
        // Explicitly point DNS computer name to avoid very slow NetBIOS name resolutions.
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies the of the local user account to rename in the local Security
        /// Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "InputObject")]
        [ValidateNotNull]
        public LocalUser InputObject { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the local user account to be renamed in the local Security
        /// Accounts Manager.
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
        /// Specifies the new name for the local user account in the Security Accounts
        /// Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 1)]
        [ValidateNotNullOrEmpty]
        public string NewName { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies the local user to rename.
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
            ProcessUser();
            ProcessName();
            ProcessSid();
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        /// <summary>
        /// Process user requested by -Name.
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
                    using UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(_principalContext, IdentityType.SamAccountName, Name);
                    if (userPrincipal is null)
                    {
                        WriteError(new ErrorRecord(new UserNotFoundException(Name, new LocalUser(Name)), "UserNotFound", ErrorCategory.ObjectNotFound, Name));
                    }
                    else
                    {
                        try
                        {
                            DirectoryEntry entry = (DirectoryEntry)userPrincipal.GetUnderlyingObject();
                            entry.Rename(NewName);
                            entry.CommitChanges();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            var exc = new AccessDeniedException(Strings.AccessDenied);

                            ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: LocalHelpers.GetTargetUserObject(userPrincipal)));
                        }
                        catch (System.Runtime.InteropServices.COMException e) when (e.ErrorCode == -2147022694)
                        {
                            var exc = new InvalidNameException(NewName, LocalHelpers.GetTargetUserObject(userPrincipal), e);

                            ThrowTerminatingError(new ErrorRecord(exc, "InvalidName", ErrorCategory.InvalidArgument, targetObject: LocalHelpers.GetTargetUserObject(userPrincipal)));
                        }
                        catch (System.Runtime.InteropServices.COMException e) when (e.ErrorCode == -2147022672)
                        {
                            var exc = new NameInUseException(NewName, LocalHelpers.GetTargetUserObject(userPrincipal), e);

                            ThrowTerminatingError(new ErrorRecord(exc, "NameInUse", ErrorCategory.InvalidArgument, targetObject: LocalHelpers.GetTargetUserObject(userPrincipal)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "InvalidLocalUserOperation", ErrorCategory.InvalidOperation, targetObject: new LocalUser(Name)));
                }
            }
        }

        /// <summary>
        /// Process user requested by -SID.
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
                    UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, SID.Value);
                    if (userPrincipal is null)
                    {
                        WriteError(new ErrorRecord(new UserNotFoundException(SID.Value, SID.Value), "UserNotFound", ErrorCategory.ObjectNotFound, SID.Value));
                    }
                    else
                    {
                        try
                        {
                            DirectoryEntry entry = (DirectoryEntry)userPrincipal.GetUnderlyingObject();
                            entry.Rename(NewName);
                            entry.CommitChanges();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            var exc = new AccessDeniedException(Strings.AccessDenied);

                            ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: LocalHelpers.GetTargetUserObject(userPrincipal)));
                        }
                        catch (System.Runtime.InteropServices.COMException e) when (e.ErrorCode == -2147022694)
                        {
                            var exc = new InvalidNameException(NewName, LocalHelpers.GetTargetUserObject(userPrincipal), e);

                            ThrowTerminatingError(new ErrorRecord(exc, "InvalidName", ErrorCategory.InvalidArgument, targetObject: LocalHelpers.GetTargetUserObject(userPrincipal)));
                        }
                        catch (System.Runtime.InteropServices.COMException e) when (e.ErrorCode == -2147022672)
                        {
                            var exc = new NameInUseException(NewName, LocalHelpers.GetTargetUserObject(userPrincipal), e);

                            ThrowTerminatingError(new ErrorRecord(exc, "NameInUse", ErrorCategory.InvalidArgument, targetObject: LocalHelpers.GetTargetUserObject(userPrincipal)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "InvalidLocalUserOperation", ErrorCategory.InvalidOperation, targetObject: new LocalUser() { SID = SID }));
                }
            }
        }

        /// <summary>
        /// Process user given through -InputObject.
        /// </summary>
        private void ProcessUser()
        {
            if (InputObject is null)
            {
                return;
            }

            LocalUser user = InputObject;
            if (CheckShouldProcess(user.ToString(), NewName))
            {
                try
                {
                    using UserPrincipal userPrincipal = user.SID is not null
                        ? UserPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, user.SID.Value)
                        : UserPrincipal.FindByIdentity(_principalContext, user.Name);
                    if (userPrincipal is null)
                    {
                        WriteError(new ErrorRecord(new UserNotFoundException(user.ToString(), user), "UserNotFound", ErrorCategory.ObjectNotFound, user));
                    }
                    else
                    {
                        try
                        {
                            DirectoryEntry entry = (DirectoryEntry)userPrincipal.GetUnderlyingObject();
                            entry.Rename(NewName);
                            entry.CommitChanges();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            var exc = new AccessDeniedException(Strings.AccessDenied);

                            ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: LocalHelpers.GetTargetUserObject(userPrincipal)));
                        }
                        catch (System.Runtime.InteropServices.COMException e) when (e.ErrorCode == -2147022694)
                        {
                            var exc = new InvalidNameException(NewName, LocalHelpers.GetTargetUserObject(userPrincipal), e);

                            ThrowTerminatingError(new ErrorRecord(exc, "InvalidName", ErrorCategory.InvalidArgument, targetObject: LocalHelpers.GetTargetUserObject(userPrincipal)));
                        }
                        catch (System.Runtime.InteropServices.COMException e) when (e.ErrorCode == -2147022672)
                        {
                            var exc = new NameInUseException(NewName, LocalHelpers.GetTargetUserObject(userPrincipal), e);

                            ThrowTerminatingError(new ErrorRecord(exc, "NameInUse", ErrorCategory.InvalidArgument, targetObject: LocalHelpers.GetTargetUserObject(userPrincipal)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "InvalidLocalUserOperation", ErrorCategory.InvalidOperation, targetObject: new LocalUser(user.Name) { SID = user.SID }));
                }
            }
        }

        /// <summary>
        /// Determine if a user should be processed.
        /// Just a wrapper around Cmdlet.ShouldProcess, with localized string
        /// formatting.
        /// </summary>
        /// <param name="userName">
        /// Name of the user to rename.
        /// </param>
        /// <param name="newName">
        /// New name for the user.
        /// </param>
        /// <returns>
        /// True if the user should be processed, false otherwise.
        /// </returns>
        private bool CheckShouldProcess(string? userName, string newName)
        {
            string msg = StringUtil.Format(Strings.ActionRenameUser, newName);

            return ShouldProcess(userName, msg);
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
