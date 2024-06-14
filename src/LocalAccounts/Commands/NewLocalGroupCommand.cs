// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;

using Microsoft.PowerShell.LocalAccounts;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The New-LocalGroup Cmdlet can be used to create a new local security group
    /// in the Windows Security Accounts Manager.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "LocalGroup",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717990")]
    [Alias("nlg")]
    public class NewLocalGroupCommand : Cmdlet, IDisposable
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
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public string Description { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// The group name for the local security group.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [ValidateLength(1, 256)]
        public string Name { get; set; } = null!;
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                if (CheckShouldProcess(Name))
                {
                    using GroupPrincipal groupPrincipal = new GroupPrincipal(_principalContext, Name)
                    {
                        Description = Description,
                        GroupScope = GroupScope.Local
                    };

                    groupPrincipal.Save();

                    LocalGroup group = new LocalGroup(Name)
                    {
                        Description = Description,
                        PrincipalSource = PrincipalSource.Local,
                        SID = groupPrincipal.Sid,

                    };

                    WriteObject(group);
                }
            }
            catch (UnauthorizedAccessException)
            {
                var exc = new AccessDeniedException(Strings.AccessDenied);

                ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: Name));
            }
            catch (PrincipalOperationException e) when (e.ErrorCode == -2147022694)
            {
                var exc = new InvalidNameException(Name, Name, e);

                WriteError(new ErrorRecord(exc, "InvalidName", ErrorCategory.ResourceExists, targetObject: Name));
            }
            catch (PrincipalOperationException ex) when (ex.ErrorCode == -2147023517)
            {
                var exc = new GroupExistsException(Name, Name);

                WriteError(new ErrorRecord(exc, "GroupExists", ErrorCategory.ResourceExists, targetObject: Name));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "InvalidLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: Name));
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        private bool CheckShouldProcess(string target)
        {
            return ShouldProcess(target, Strings.ActionNewGroup);
        }
        #endregion Private Methods

        #region IDisposable interface
        private bool _disposed;

        /// <summary>
        /// Dispose the DisableLocalUserCommand.
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
