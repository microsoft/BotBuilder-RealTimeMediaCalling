REM --- Move to this scripts location ---
pushd "%~dp0"

REM --- Print out environment variables for debugging ---
set

REM--- Register media perf dlls ---
powershell .\skype_media_lib\MediaPlatformStartupScript.bat

REM --- Delete existing certificate bindings and URL ACL registrations ---
netsh http delete sslcert ipport=%InstanceIpAddress%:%PrivateDefaultCallControlPort%
netsh http delete sslcert ipport=%InstanceIpAddress%:%PrivateInstanceCallControlPort%
netsh http delete urlacl url=https://%InstanceIpAddress%:%PrivateDefaultCallControlPort%/
netsh http delete urlacl url=https://%InstanceIpAddress%:%PrivateInstanceCallControlPort%/

REM --- Delete new URL ACLs and certificate bindings ---
netsh http add urlacl url=https://%InstanceIpAddress%:%PrivateDefaultCallControlPort%/ user="NT AUTHORITY\NETWORK SERVICE"
netsh http add urlacl url=https://%InstanceIpAddress%:%PrivateInstanceCallControlPort%/ user="NT AUTHORITY\NETWORK SERVICE"
netsh http add sslcert ipport=%InstanceIpAddress%:%PrivateDefaultCallControlPort% clientcertnegotiation=enable "appid={00000000-0000-0000-0000-000000000001}" cert=%DefaultCertificate%
netsh http add sslcert ipport=%InstanceIpAddress%:%PrivateInstanceCallControlPort% clientcertnegotiation=enable "appid={00000000-0000-0000-0000-000000000001}" cert=%DefaultCertificate%

REM --- Disable strong-name validation.  This should not be needed once all external binaries are properly signed ---
REGEDIT /S %~dp0\DisableStrongNameVerification.reg

popd
exit /b 0