# PowerShell LocalAccounts module for Windows

PowerShell 7+ module is Windows-only module for local account management.

## What's it?

It is a port of Windows PowerShell `Microsoft.PowerShell.LocalAccounts` module to .Net (>= 8.0) for using with PowerShell 7.4+ based on [LocalAccounts-MS](https://github.com/iSazonov/LocalAccounts-MS).

While `LocalAccounts-MS` is a port of the original `Microsoft.PowerShell.LocalAccounts` module for maintaining maximum backward compatibility and it is frozen, this `LocalAccounts` project is open for development.

[Original documentation](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.localaccounts/?view=powershell-5.1)

[Original source code](https://github.com/PowerShell/PowerShell/tree/master/src/Microsoft.PowerShell.LocalAccounts)

## Why is the `Microsoft.PowerShell.LocalAccounts` original module not distributed with PowerShell 7?

The original module uses non-public APIs and therefore cannot be part of an open source project.

## How to install

 You can compile the code yourself or

- Download ZIP file from release page
- Unpack it to your PowerShell module folder `C:\Program Files\PowerShell\Modules`
- start `pwsh`
- run `Get-LocalUser`
- Check you get right module `(Get-Module -Name Microsoft.PowerShell.LocalAccounts).Path`

## Why do we need the ported module?

- The `Microsoft.PowerShell.LocalAccounts` original module is frozen. Microsoft has no plans to change anything in the original module.

- Meanwhile, this project is open for development.

- Currently, PowerShell 7 can use `Microsoft.PowerShell.LocalAccounts` only using `Compatibility feature`,
  i.e. cmdlets from this module are executed in Windows PowerShell, after which the results are returned in serialized form.
  This works well for simple scenarios, but in complex ones it may not work as expected.

  The module from this repository can be imported to `pwsh` directly. In addition, cmdlet objects can also be directly used in .Net applications.

- There are some issues in the original module, which is fixed in this repository:

  - [an annoying bug](https://github.com/PowerShell/PowerShell/issues/2996) ([the same](https://github.com/PowerShell/PowerShell/issues/15585)) (`Get-LocalGroupMember : Failed to compare two elements in the array`)

  - [an issue](https://github.com/PowerShell/PowerShell/issues/16049) (impossible to clear `Description` property)

  - [an issue](https://github.com/PowerShell/PowerShell/issues/2150) (`Description` property length limitation)

  - [an issue](https://github.com/PowerShell/PowerShell/issues/11965) (incorrect `PasswordRequired` property)

  - There are some reports about telemetry loading error, which are not present in this repository:

    - [Issue 21645](https://github.com/PowerShell/PowerShell/issues/21645)
      > Could not load type 'Microsoft.PowerShell.Telemetry.Internal.TelemetryAPI' from assembly 'System.Management.Automation, Version=7.4.2.500, Culture=neutral, PublicKeyToken=*****'.

    - [Issue 18624](https://github.com/PowerShell/PowerShell/issues/18624)
      > New-LocalUser: Could not load type 'Microsoft.PowerShell.Telemetry.Internal.TelemetryAPI' from assembly 'System.Management.Automation, Version=7.3.0.500, Culture=neutral, PublicKeyToken=31bf3856ad364e35'.

    - [Issue 18264](https://github.com/PowerShell/PowerShell/issues/18264)
      > New-LocalUser: Could not load type 'Microsoft.PowerShell.Telemetry.Internal.TelemetryAPI' from assembly 'System.Management.Automation, Version=7.2.6.500, Culture=neutral, PublicKeyToken=31bf3856ad364e35'.

## Backward compatibility with `Microsoft.PowerShell.LocalAccounts`

Obviously, there is no binary compatibility with `Microsoft.PowerShell.LocalAccounts`.

This project maintains backward compatibility with its predecessor at the script level, existing Windows PowerShell scripts should work unchanged in most cases. Nevertheless, the project is open to innovations that can be accepted even if backward compatibility cannot be ensured. Maintaining full backward compatibility is not a strict requirement if the changes fix bugs or introduce significant new features.

### `PrincipalSource` property was removed

There are two reasons for this:

- `PrincipalSource` property was based on _non-public_ API `LsaLookupUserAccountType` from `api-ms-win-security-lsalookup-l1-1-2.dll` (the official documentation does not contain a description of this point. In addition, the current version of this dll is listed in the documentation as `api-ms-win-security-lsalookup-l2-1-0.dll`).

- `PrincipalSource` property does not look useful. Microsoft added this property only when Windows 10 was introduced. The reason is unknown. Microsoft's comment in code saids that logic of the `LsaLookupUserAccountType` function is in question and too complex to reproduce on C#. But why do we need this information in a module that should work only with local accounts.

### Possible `PrincipalSource` property replacement

The only possible replacement in future versions could be the `Identifier Authority` value from SID.
See <https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dtyp/f992ad60-0fe4-4b87-9fed-beb478836861>
and <https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dtyp/c6ce4275-3d90-4890-ab3a-514745e4637e>

The problem here is that the values 10 (`MS Passport`),11 (`Microsoft`), and 12 (`AAD`) are not specified in the official documentation. Of course, this only matters if we want to replace the numeric value with its name.

## Additional information

Specific of `System.DirectoryServices.AccountManagement` API is that there may be delays due to name recognition on the network, since this API uses NetBIOS names inside. Disabling NetBIOS and LLMNR protocols on the computer could help to fix the problem.

## Future versions

This project is open for development.

## Is there any intention to distribute this ported module with PowerShell 7 or to bring it back to the PowerShell repository?

It is a question for PowerShell team. (There are no technical obstacles.)
