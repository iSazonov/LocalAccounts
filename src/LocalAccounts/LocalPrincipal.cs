// Licensed under the MIT License.

using System.Security.Principal;

namespace LocalAccounts.Commands
{
    /// <summary>
    /// Represents a Principal. Serves as a base class for Local Users and Local Groups.
    /// </summary>
    public class LocalPrincipal
    {
        #region Public Properties
        /// <summary>
        /// The account name of the Principal.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The Security Identifier that uniquely identifies the Principal.
        /// </summary>
        public SecurityIdentifier? SID { get; set; }

        /// <summary>
        /// The object class that represents this principal.
        /// This can be User or Group.
        /// </summary>
        public string? ObjectClass { get; set; }
        #endregion Public Properties

        #region Construction
        /// <summary>
        /// Initializes a new LocalPrincipal object.
        /// </summary>
        public LocalPrincipal()
        {
        }

        /// <summary>
        /// Initializes a new LocalPrincipal object with the specified name.
        /// </summary>
        /// <param name="name">Name of the new LocalPrincipal.</param>
        public LocalPrincipal(string? name)
        {
            Name = name;
        }
        #endregion Construction

        #region Public Methods
        /// <summary>
        /// Provides a string representation of the Principal.
        /// </summary>
        /// <returns>
        /// A string, in SDDL form, representing the Principal.
        /// </returns>
        public override string ToString()
        {
            return Name ?? SID?.ToString() ?? string.Empty;
        }
        #endregion Public Methods
    }
}
