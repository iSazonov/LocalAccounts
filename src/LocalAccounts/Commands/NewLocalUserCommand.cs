// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;

using Microsoft.PowerShell.LocalAccounts;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The New-LocalUser cmdlet creates a new local user account.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "LocalUser",
            DefaultParameterSetName = "Password",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717981")]
    [Alias("nlu")]
    public class NewLocalUserCommand : PSCmdlet, IDisposable
    {
        #region Static Data
        // Names of object- and boolean-type parameters.
        // Switch parameters don't need to be included.
        private static string[] parameterNames = new string[]
            {
                "AccountExpires",
                "Description",
                "Disabled",
                "FullName",
                "Password",
                "UserMayNotChangePassword"
            };
        #endregion Static Data

        #region Instance Data
        // Explicitly point DNS computer name to avoid very slow NetBIOS name resolutions.
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "AccountExpires".
        /// Specifies when the user account will expire.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public DateTime AccountExpires { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "AccountNeverExpires".
        /// Specifies that the account will not expire.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter AccountNeverExpires { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Description".
        /// A descriptive comment for this user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public string Description { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "Disabled".
        /// Specifies whether this user account is enabled or disabled.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Disabled { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "FullName".
        /// Specifies the full name of the user account. This is different from the username of the user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public string FullName { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the user name for the local user account. This can be a local user
        /// account or a local user account that is connected to a Microsoft Account.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [ValidateLength(1, 20)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "Password".
        /// Specifies the password for the local user account. A password can contain up to 127 characters.
        /// </summary>
        [Parameter(Mandatory = true,
                   ParameterSetName = "Password",
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public System.Security.SecureString Password { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "PasswordChangeableDate".
        /// Specifies that the new User account has no password.
        /// </summary>
        [Parameter(Mandatory = true,
                   ParameterSetName = "NoPassword",
                   ValueFromPipelineByPropertyName = true)]
        public SwitchParameter NoPassword { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "PasswordNeverExpires".
        /// Specifies that the password will not expire.
        /// </summary>
        [Parameter(ParameterSetName = "Password",
                   ValueFromPipelineByPropertyName = true)]
        public SwitchParameter PasswordNeverExpires { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "UserMayNotChangePassword".
        /// Specifies whether the user is allowed to change the password on this account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter UserMayNotChangePassword { get; set; }
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
            try
            {
                if (CheckShouldProcess(Name))
                {
                    using UserPrincipal userPrincipal = new UserPrincipal(_principalContext)
                    {
                        Description = Description,
                        DisplayName = FullName,
                        Enabled = true,
                        Name = Name,
                        PasswordNeverExpires = PasswordNeverExpires.IsPresent,
                        SamAccountName = Name,
                        UserCannotChangePassword = false
                    };

                    foreach (string paramName in parameterNames)
                    {
                        if (this.HasParameter(paramName))
                        {
                            switch (paramName)
                            {
                                case "AccountExpires":
                                    userPrincipal.AccountExpirationDate = AccountExpires;
                                    break;

                                case "Disabled":
                                    userPrincipal.Enabled = !Disabled;
                                    break;

                                case "UserMayNotChangePassword":
                                    userPrincipal.UserCannotChangePassword = UserMayNotChangePassword;
                                    break;
                            }
                        }
                    }

                    if (AccountNeverExpires.IsPresent)
                    {
                        userPrincipal.AccountExpirationDate = null;
                    }

                    if (NoPassword.IsPresent)
                    {
                        try
                        {
                            // It is a breaking change.
                            //  Windows PowerShell ignores a domain password policy and can create the account without password.
                            //  AccountManagment API follows a domain password policy and can not create the account without password.
                            userPrincipal.PasswordNotRequired = true;
                            userPrincipal.Save();
                        }
                        catch (PasswordException)
                        {
                            userPrincipal.SetPassword(LocalHelpers.GenerateRandomString());
                            userPrincipal.Save();
                        }
                    }
                    else
                    {
                        userPrincipal.SetPassword(Password.AsString());
                        userPrincipal.Save();
                    }

                    LocalUser user = LocalHelpers.GetLocalUser(userPrincipal);

                    WriteObject(user);
                }
            }
            catch (UnauthorizedAccessException)
            {
                var exc = new AccessDeniedException(Strings.AccessDenied);

                ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: Name));
            }
            catch (PasswordException)
            {
                var exc = new InvalidPasswordException(Strings.InvalidPassword);

                ThrowTerminatingError(new ErrorRecord(exc, "InvalidPassword", ErrorCategory.InvalidData, targetObject: Password));
            }
            catch (PrincipalOperationException e) when (e.ErrorCode == -2147022694)
            {
                var exc = new InvalidNameException(Name, Name, e);

                WriteError(new ErrorRecord(exc, "InvalidName", ErrorCategory.ResourceExists, targetObject: Name));
            }
            catch (PrincipalExistsException e)
            {
                // It is a breaking change.
                // Windows PowerShell ignores the error and set password for exisisting user. This looks like Windows PowerShell bug.
                var exc = new UserExistsException(Name, Name, e);

                WriteError(new ErrorRecord(exc, "UserExists", ErrorCategory.ResourceExists, targetObject: Name));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "InvalidLocalUserOperation", ErrorCategory.InvalidOperation, targetObject: Name));
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        private bool CheckShouldProcess(string target)
        {
            return ShouldProcess(target, Strings.ActionNewUser);
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
