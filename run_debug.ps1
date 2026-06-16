try {
    & "bin\Debug\net8.0-windows\ClipyFlow.exe" 2>&1
} catch {
    Write-Host "CRASH: $_"
}
