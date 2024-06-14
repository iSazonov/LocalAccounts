// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;
using System.Security.Principal;

using Microsoft.PowerShell.LocalAccounts;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Set-LocalUser cmdlet changes the properties of a user account in the
    /// local Windows Security Accounts Manager. It can also reset the password of a
    /// local user account.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "LocalUser",
            SupportsShouldProcess = true,
            DefaultParameterSetName = "Name",
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717984")]
    [Alias("slu")]
    public class SetLocalUserCommand : PSCmdlet, IDisposable
    {
        #region Static Data
        // Names of object- and boolean-type parameters.
        // Switch parameters don't need to be included.
        private static string[] parameterNames = new string[]
            {
                "AccountExpires",
                "Description",
                "FullName",
                "Password",
                "UserMayChangePassword",
                "PasswordNeverExpires"
            };
        #endregion Static Data

        #region Instance Data
        // Explicitly point DNS computer name to avoid very slow NetBIOS name resolutions.
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "AccountExpires".
        /// Specifies when the user account will expire. Set to null to indicate that
        /// the account will never expire. The default value is null (account never expires).
        /// </summary>
        [Parameter]
        public DateTime AccountExpires { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "AccountNeverExpires".
        /// Specifies that the account will not expire.
        /// </summary>
        [Parameter]
        public SwitchParameter AccountNeverExpires { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Description".
        /// A descriptive comment for this user account.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public string Description { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "FullName".
        /// Specifies the full name of the user account. This is different from the
        /// username of the user account.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public string FullName { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies the of the local user account to modify in the local Security Accounts Manager.
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
        /// Specifies the local user account to change.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Name")]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "Password".
        /// Specifies the password for the local user account.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public System.Security.SecureString Password { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "PasswordNeverExpires".
        /// Specifies that the password will not expire.
        /// </summary>
        [Parameter]
        public bool PasswordNeverExpires { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies a user from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNull]
        public SecurityIdentifier SID { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "UserMayChangePassword".
        /// Specifies whether the user is allowed to change the password on this  account.
        /// The default value is True.
        /// </summary>
        [Parameter]
        public bool UserMayChangePassword { get; set; }
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (this.HasParameter("AccountExpires") && AccountNeverExpires.IsPresent)
            {
                InvalidParametersException ex = new InvalidParametersException("AccountExpires", "AccountNeverExpires");
                ThrowTerminatingError(ex.MakeErrorRecord());
            }
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            UserPrincipal? userPrincipal = new UserPrincipal(_principalContext);
            try
            {
                if (InputObject is not null)
                {
                    LocalUser user = InputObject;
                    if (CheckShouldProcess(user.ToString()))
                    {
                        userPrincipal = user.SID is not null
                             ? UserPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, user.SID.Value)
                             : UserPrincipal.FindByIdentity(_principalContext, user.Name);

                        if (userPrincipal is null)
                        {
                            WriteError(new ErrorRecord(new UserNotFoundException(Name, Name), "UserNotFound", ErrorCategory.ObjectNotFound, Name));
                        }
                    }
                }
                else if (Name is not null)
                {
                    if (CheckShouldProcess(Name))
                    {
                        userPrincipal = UserPrincipal.FindByIdentity(_principalContext, IdentityType.SamAccountName, Name);

                        if (userPrincipal is null)
                        {
                            WriteError(new ErrorRecord(new UserNotFoundException(Name, Name), "UserNotFound", ErrorCategory.ObjectNotFound, Name));
                        }
                    }
                }
                else if (SID is not null)
                {
                    userPrincipal = UserPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, SID.Value);

                    if (userPrincipal is null)
                    {
                        WriteError(new ErrorRecord(new UserNotFoundException(SID.Value, SID), "UserNotFound", ErrorCategory.ObjectNotFound, SID));
                    }

                    if (!CheckShouldProcess(SID.ToString()))
                    {
                        userPrincipal = null;
                    }
                }

                if (userPrincipal is null)
                {
                    return;
                }

                foreach (var paramName in parameterNames)
                {
                    if (this.HasParameter(paramName))
                    {
                        switch (paramName)
                        {
                            case "AccountExpires":
                                userPrincipal.AccountExpirationDate = AccountExpires;
                                break;

                            case "Description":
                                userPrincipal.Description = Description;
                                break;

                            case "FullName":
                                userPrincipal.DisplayName = FullName;
                                break;

                            case "UserMayChangePassword":
                                userPrincipal.UserCannotChangePassword = !UserMayChangePassword;
                                break;

                            case "Password":
                                userPrincipal.SetPassword(Password.AsString());
                                break;

                            case "PasswordNeverExpires":
                                userPrincipal.PasswordNeverExpires = PasswordNeverExpires;
                                break;
                        }
                    }
                }

                if (AccountNeverExpires.IsPresent)
                {
                    userPrincipal.AccountExpirationDate = null;
                }

                userPrincipal.Save();
            }
            catch (UnauthorizedAccessException)
            {
                var exc = new AccessDeniedException(Strings.AccessDenied);

                ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: Name));
            }
            catch (PasswordException)
            {
                var exc = new InvalidPasswordException(Strings.InvalidPassword);

                ThrowTerminatingError(new ErrorRecord(exc, "InvalidPassword", ErrorCategory.InvalidData, targetObject: Name));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "InvalidLocalUserOperation", ErrorCategory.InvalidOperation, targetObject: Name));
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        private bool CheckShouldProcess(string? target)
        {
            return ShouldProcess(target, Strings.ActionSetUser);
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
