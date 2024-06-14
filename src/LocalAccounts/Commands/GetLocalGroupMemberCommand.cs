// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Collections.Generic;
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
    /// The Get-LocalGroupMember cmdlet gets the members of a local group.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "LocalGroupMember",
            DefaultParameterSetName = "Default",
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717988")]
    [Alias("glgm")]
    public class GetLocalGroupMemberCommand : BaseLocalGroupMemberCommand
    {
        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "Member".
        /// Specifies the name of the user or group that is a member of this group. If
        /// this parameter is not specified, all members of the specified group are
        /// returned. This accepts a name, SID, or wildcard string.
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty]
        public string Member { get; set; } = null!;
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            GetGroup();

            try
            {
                IEnumerable<LocalPrincipal>? principals = ProcessesMembership(MakeLocalPrincipals(_groupPrincipal!));

                if (principals is not null)
                {
                    WriteObject(principals, enumerateCollection: true);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "InvalidLocalGroupMemberOperation", ErrorCategory.InvalidOperation, targetObject: null));
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        private static IEnumerable<LocalPrincipal> MakeLocalPrincipals(GroupPrincipal groupPrincipal)
        {
            static string GetObjectClass(Principal p) => p switch
            {
                GroupPrincipal => Strings.ObjectClassGroup,
                UserPrincipal => Strings.ObjectClassUser,
                _ => Strings.ObjectClassOther
            };

            IEnumerator<Principal> members = groupPrincipal.GetMembers().GetEnumerator();
            bool hasItem = false;
            do
            {
                hasItem = false;
                LocalPrincipal? localPrincipal = null;

                try
                {
                    // Try to move on to next member.
                    // `GroupPrincipal.GetMembers()` and `GroupPrincipal.Members` throw if an group member account was removed.
                    // It is a reason why we don't use `foreach (Principal principal in group.GetMembers()) { ... }`
                    // and we are forced to deconstruct the foreach in order to silently ignore such error and continue.
                    hasItem = members.MoveNext();

                    if (hasItem)
                    {
                        Principal principal = members.Current;
                        localPrincipal = new LocalPrincipal()
                        {
                            // Get name as 'Domain\user'
                            Name = principal.Sid.Translate(typeof(NTAccount)).ToString(),
                            PrincipalSource = Sam.GetPrincipalSource(principal.Sid),
                            SID = principal.Sid,
                            ObjectClass = GetObjectClass(principal),
                        };

                        /*
                        // Follow code is more useful but
                        //    1. it is a breaking change (output UserPrincipal and GoupPrincipal types instead of LocalPrincipal type)
                        //    2. it breaks a table output.
                        if (principal is GroupPrincipal)
                        {
                            localPrincipal = new LocalPrincipal()
                            {
                                Name = principal.Name,
                                PrincipalSource = Sam.GetPrincipalSource(principal.Sid),
                                SID = principal.Sid,
                                ObjectClass = GetObjectClass(principal),
                            };
                        }
                        else if (principal is UserPrincipal userPrincipal)
                        {
                           localPrincipal = GetLocalUser(userPrincipal);
                        }
                        */
                    }
                }
                catch (PrincipalOperationException)
                {
                    // An error occurred in members.MoveNext() while enumerating the group membership. The member's SID could not be resolved.
                    hasItem = true;
                }
                catch (IdentityNotMappedException)
                {
                    // Ignore an error in principal.Sid.Translate() while getting a domain name of the member.
                }
                catch (SystemException)
                {
                    // Ignore an error in principal.Sid.Translate() while getting a domain name of the member.
                }

                if (localPrincipal is not null)
                {
                    // `yield` can not be in `try` with `catch` block.
                    yield return localPrincipal;
                }
            } while (hasItem);
        }

        private IEnumerable<LocalPrincipal> ProcessesMembership(IEnumerable<LocalPrincipal> membership)
        {
            List<LocalPrincipal> rv;

            // if no member filters are specified, return all.
            if (Member is null)
            {
                rv = new List<LocalPrincipal>(membership);
            }
            else
            {
                rv = new List<LocalPrincipal>();

                if (WildcardPattern.ContainsWildcardCharacters(Member))
                {
                    string name = Member.StartsWith('*') ? Member : '*' + Member;

                    var pattern = new WildcardPattern(name, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);

                    foreach (LocalPrincipal m in membership)
                    {
                        if (pattern.IsMatch(m.Name))
                        {
                            rv.Add(m);
                        }
                    }
                }
                else
                {
                    SecurityIdentifier? sid = this.TrySid(Member);

                    if (sid is not null)
                    {
                        foreach (LocalPrincipal m in membership)
                        {
                            if (m.SID == sid)
                            {
                                rv.Add(m);
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (LocalPrincipal m in membership)
                        {
                            if (m.Name is not null && m.Name.EndsWith(Member, StringComparison.CurrentCultureIgnoreCase))
                            {
                                rv.Add(m);
                                break;
                            }
                        }
                    }

                    if (rv.Count == 0)
                    {
                        var ex = new PrincipalNotFoundException(Member, Member);
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }

            // Sort the resulting principals by name.
            rv.Sort(static (p1, p2) => string.Compare(p1.Name, p2.Name, StringComparison.CurrentCultureIgnoreCase));

            return rv;
        }
        #endregion Private Methods
    }
}
