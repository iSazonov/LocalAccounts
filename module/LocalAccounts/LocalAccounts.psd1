@{
RootModule = 'LocalAccounts'
ModuleVersion = '1.0.0.0'
GUID = '8a5abd94-39e4-49d8-a711-3332a006f9c4'
Author = 'I.E.Sazonov'
Copyright = '© I.E.Sazonov. All rights reserved.'
Description = 'Provides cmdlets to work with local users and local groups'
PowerShellVersion = '7.4'
FormatsToProcess = @('LocalAccounts.format.ps1xml')
CmdletsToExport = @(
    'Add-LocalGroupMember',
    'Disable-LocalUser',
    'Enable-LocalUser',
    'Get-LocalGroup',
    'Get-LocalGroupMember',
    'Get-LocalUser',
    'New-LocalGroup',
    'New-LocalUser',
    'Remove-LocalGroup',
    'Remove-LocalGroupMember',
    'Remove-LocalUser',
    'Rename-LocalGroup',
    'Rename-LocalUser',
    'Set-LocalGroup',
    'Set-LocalUser'
    )
AliasesToExport= @( "algm", "dlu", "elu", "glg", "glgm", "glu", "nlg", "nlu", "rlg", "rlgm", "rlu", "rnlg", "rnlu", "slg", "slu")
HelpInfoURI = 'https://go.microsoft.com/fwlink/?LinkId=717973'
CompatiblePSEditions = @('Core')
}
