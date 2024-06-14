// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;
using System.Security.Principal;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Get-LocalGroup cmdlet gets local groups from the Windows Security
    /// Accounts manager.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "LocalGroup",
            DefaultParameterSetName = "Default",
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717974")]
    [Alias("glg")]
    public class GetLocalGroupCommand : Cmdlet, IDisposable
    {
        #region Instance Data
        // Explicitly point DNS computer name to avoid very slow NetBIOS name resolutions.
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the local groups to get from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNull]
        public string[] Name { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies a local group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNull]
        public SecurityIdentifier[] SID { get; set; } = null!;
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            ProcessAll();
            ProcessNames();
            ProcessSids();
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        /// <summary>
        /// Process all groups if both -Name and -SID are absent.
        /// </summary>
        private void ProcessAll()
        {
            if (Name is null && SID is null)
            {
                try
                {
                    foreach (LocalGroup LocalGroup in LocalHelpers.GetAllLocalGroups(_principalContext))
                    {
                        WriteObject(LocalGroup);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "InvalidLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: null));
                }

                return;
            }
        }

        /// <summary>
        /// Process groups requested by -Name.
        /// </summary>
        /// <remarks>
        /// All arguments to -Name will be treated as names,
        /// even if a name looks like a SID.
        /// Groups may be specified using wildcards.
        /// </remarks>
        private void ProcessNames()
        {
            if (Name is null)
            {
                return;
            }

            foreach (string name in Name)
            {
                if (name is null)
                {
                    continue;
                }

                try
                {
                    if (WildcardPattern.ContainsWildcardCharacters(name))
                    {
                        var pattern = new WildcardPattern(name, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);

                        foreach (LocalGroup localGroup in LocalHelpers.GetMatchingLocalGroups(userPrincipal => pattern.IsMatch(userPrincipal.Name), _principalContext))
                        {
                            WriteObject(localGroup);
                        }
                    }
                    else
                    {
                        SecurityIdentifier? sid = this.TrySid(name);
                        LocalGroup? group = sid is null
                            ? LocalHelpers.GetMatchingLocalGroupsByName(name, _principalContext)
                            : LocalHelpers.GetMatchingLocalGroupsBySID(sid, _principalContext);
                        if (group is not null)
                        {
                            WriteObject(group);
                        }
                        else
                        {
                            WriteError(new ErrorRecord(new GroupNotFoundException(name, name), "GroupNotFound", ErrorCategory.ObjectNotFound, name));
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "InvalidLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: name));
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

                try
                {
                    LocalGroup? group = LocalHelpers.GetMatchingLocalGroupsBySID(sid, _principalContext);
                    if (group is not null)
                    {
                        WriteObject(group);
                    }
                    else
                    {
                        WriteError(new ErrorRecord(new GroupNotFoundException(sid.Value, sid), "GroupNotFound", ErrorCategory.ObjectNotFound, sid));
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "InvalidLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: sid));
                }
            }
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
