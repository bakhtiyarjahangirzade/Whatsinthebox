$ErrorActionPreference = 'Stop'
$name = 'Local\Whatsinthebox.WindowsHost.' + [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$mutex = $null
$signal = $null
$owned = $false
try {
    try { $mutex = [Threading.Mutex]::OpenExisting($name) }
    catch [Threading.WaitHandleCannotBeOpenedException] { exit 0 }
    $signal = [Threading.EventWaitHandle]::OpenExisting($name + '.stop')
    $signal.Set() | Out-Null
    try { $owned = $mutex.WaitOne(15000) }
    catch [Threading.AbandonedMutexException] { $owned = $true }
    if (!$owned) { exit 1 }
} catch { exit 1 }
finally {
    if ($owned) { $mutex.ReleaseMutex() }
    if ($mutex) { $mutex.Dispose() }
    if ($signal) { $signal.Dispose() }
}
