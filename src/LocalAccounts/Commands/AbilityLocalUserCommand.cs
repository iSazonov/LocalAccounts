// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Security.Principal;

using Microsoft.PowerShell.LocalAccounts;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// It is base class for Enable-LocalUser and Disable-LocalUser cmdlets.
    /// </summary>
    public abstract class AbilityLocalUserCommand : Cmdlet, IDisposable
    {
        // Explicitly point DNS computer name to avoid very slow NetBIOS name resolutions.
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());

        // Set to true to enable an user account.
        // Set to false to disable an user account.
        internal bool? _ability;

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies the of the local user accounts to disable in the local Security
        /// Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "InputObject")]
        [ValidateNotNullOrEmpty]
        public LocalUser[] InputObject { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the names of the local user accounts to disable in the local
        /// Security Accounts Manager.
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
        /// Specifies the LocalUser accounts to disable by
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

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            ProcessUsers();
            ProcessNames();
            ProcessSids();
        }

        #region Private Methods
        /// <summary>
        /// Process users requested by -Name.
        /// </summary>
        /// <remarks>
        /// All arguments to -Name will be treated as names,
        /// even if a name looks like a SID.
        /// </remarks>
        private void ProcessNames()
        {
            Debug.Assert(_ability is not null);

            if (Name is null)
            {
                return;
            }

            foreach (string name in Name)
            {
                try
                {
                    if (CheckShouldProcess(name))
                    {
                        using UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(_principalContext, name);
                        if (userPrincipal is not null)
                        {
                            if (_ability == userPrincipal.Enabled)
                            {
                                return;
                            }

                            userPrincipal.Enabled = _ability;
                            userPrincipal.Save();
                        }
                        else
                        {
                            WriteError(new ErrorRecord(new UserNotFoundException(name, name), "UserNotFound", ErrorCategory.ObjectNotFound, name));
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    var exc = new AccessDeniedException(Strings.AccessDenied);

                    ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: new LocalUser(name)));
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "InvalidOperation", ErrorCategory.InvalidOperation, targetObject: new LocalUser(name)));
                }
            }
        }

        /// <summary>
        /// Process users requested by -SID.
        /// </summary>
        private void ProcessSids()
        {
            Debug.Assert(_ability is not null);

            if (SID is null)
            {
                return;
            }

            foreach (SecurityIdentifier sid in SID)
            {
                try
                {
                    var sidString = sid.ToString();
                    if (CheckShouldProcess(sidString))
                    {
                        using UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, sidString);
                        if (userPrincipal is not null)
                        {
                            userPrincipal.Enabled = _ability;
                            userPrincipal.Save();
                        }
                        else
                        {
                            WriteError(new ErrorRecord(new UserNotFoundException(sid.Value, sid), "UserNotFound", ErrorCategory.ObjectNotFound, sid));
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    var exc = new AccessDeniedException(Strings.AccessDenied);

                    ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: new LocalUser() { SID = sid }));
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "InvalidOperation", ErrorCategory.InvalidOperation, targetObject: new LocalUser() { SID = sid }));
                }
            }
        }

        /// <summary>
        /// Process users requested by -InputObject.
        /// </summary>
        private void ProcessUsers()
        {
            Debug.Assert(_ability is not null);

            if (InputObject is null)
            {
                return;
            }

            foreach (LocalUser user in InputObject)
            {
                if (user is null)
                {
                    continue;
                }

                try
                {
                    if (CheckShouldProcess(user.Name))
                    {
                        using UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(_principalContext, user.Name);
                        if (userPrincipal is not null)
                        {
                            userPrincipal.Enabled = _ability;
                            userPrincipal.Save();
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    var exc = new AccessDeniedException(Strings.AccessDenied);

                    ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: user));
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "InvalidOperation", ErrorCategory.InvalidOperation, targetObject: user));
                }
            }
        }

        private bool CheckShouldProcess(string? target)
        {
            return ShouldProcess(target, Strings.ActionDisableUser);
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
