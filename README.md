# PowerShell LocalAccounts module for Windows

PowerShell 7+ module for local account management on Windows.

## What's it?

It is a port of Windows PowerShell `Microsoft.PowerShell.LocalAccounts` module to .Net (>= 8.0) for using with PowerShell 7.4+ based on [LocalAccounts-MS](https://github.com/iSazonov/LocalAccounts-MS)

While `LocalAccounts-MS` is port of `Microsoft.PowerShell.LocalAccounts` for maintaining maximum backward compatibility and it is frozen this `LocalAccounts` project is open to the development of innovations.

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

- Currently, PowerShell 7 can use `Microsoft.PowerShell.LocalAccounts` only using `Compatibility feature`,
  i.e. cmdlets from this module are executed in Windows PowerShell, after which the results are returned in serialized form.
  This works well for simple scenarios, but in complex ones it may not work as expected.

  The module from this repository can be imported to `pwsh` directly. In addition, cmdlet objects can also be directly used in .Net applications.

- There is [an annoying bug](https://github.com/PowerShell/PowerShell/issues/2996) in the original module, which is fixed in this repository.

- This project is open to innovation.

## Backward compatibility with `Microsoft.PowerShell.LocalAccounts`

Obviously, there is no binary compatibility with `Microsoft.PowerShell.LocalAccounts`.

This project maintains backward compatibility with its predecessor at the script level, existing Windows PowerShell scripts should work unchanged in most cases. Nevertheless, the project is open to innovations that can be accepted even if backward compatibility cannot be ensured. Maintaining full backward compatibility is not a strict requirement if the changes fix bugs or introduce significant new features.

## Additional information

Specific of `System.DirectoryServices.AccountManagement` API is that there may be delays due to name recognition on the network, since this API uses NetBIOS names inside. Disabling NetBIOS and LLMNR protocols on the computer could help to fix the problem.

## Future versions

This project is open to innovation.

## Is there any intention to distribute this ported module with PowerShell 7 or to bring it back to the PowerShell repository?

 Yes, but here is open questions.

 The only API that I could not replace is `LsaLookupUserAccountType`.
 Although the code imports the function from `api-ms-win-security-lsalookup-l1-1-2.dll` the official documentation does not contain a description of this point.
 In addition, the current version of this dll is listed in the documentation as `api-ms-win-security-lsalookup-l2-1-0.dll`.

 So `LsaLookupUserAccountType` is rather not a public API. This can be a stopper for moving the code back to the PowerShell repository.

 The only possible replacement could be the `Identifier Authority` value from SID.
 See https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dtyp/f992ad60-0fe4-4b87-9fed-beb478836861
 and https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dtyp/c6ce4275-3d90-4890-ab3a-514745e4637e

 The problem here is that the values 10 (`MS Passport`),11 (`Microsoft`), and 12 (`AAD`) are not specified in the official documentation.

 A more general question is why do we need this value (Principal.Source) at all?
 Cmdlets never use it. It is only informational. But why do we need this information in a module that should work only with local accounts?

 So `Identifier Authority` from SID (exposed as int value since names is not public) looks more useful for future versions.
