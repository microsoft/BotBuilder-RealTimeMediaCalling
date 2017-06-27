# Error handling
trap
{
    Write-Output "Error Hit: $_"
    Write-Output "Error File: $($_.InvocationInfo.ScriptName)"
    Write-Output "Error Line #: $($_.InvocationInfo.ScriptLineNumber)"
    Write-Output ""
    Write-Output "Exception: $($_.Exception)"
    Write-Output ""
    Write-Output "Exception.InnerException: $($_.Exception.InnerException)"
    exit 1
}

Write-Output "Checking if Media Foundation is installed"
if((Get-WindowsFeature Server-Media-Foundation).Installed -eq 0)
{
    Write-Output "Installing Media Foundation."
    Add-WindowsFeature Server-Media-Foundation
    
    Write-Output "Rebooting VM for changes to take effect."
    Restart-Computer
}