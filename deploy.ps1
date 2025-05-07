

# Configuration
$projectName = "CommitActivityBot"
$solutionPath = "C:\Repos\CommitActivityBot"       
$publishPath = "C:\Services\CommitActivityBot"
$exePath = "$publishPath\$projectName.exe"
$logFile = "C:\Path\To\Your\Local\Repo\activity-log.txt"  
$serviceName = $projectName
$displayName = "Git Commit Scheduler"

# Make sure the project path exists
if (-Not (Test-Path $solutionPath)) {
    Write-Error "Project path does not exist: $solutionPath"
    exit 1
}

Set-Location $solutionPath

# Build and publish
Write-Output "🛠 Building and publishing the project..."
dotnet clean
dotnet publish -c Release -o $publishPath

if (-Not (Test-Path $exePath)) {
    Write-Error "❌ Build failed. Executable not found: $exePath"
    exit 1
}

# Uninstall previous service if it exists
Write-Output "♻️ Checking for existing service..."
$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Output "🧹 Stopping and removing existing service..."
    Stop-Service $serviceName -Force
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2
}

# Install the service
Write-Output "🚀 Installing the service..."
sc.exe create $serviceName binPath= "`"$exePath`"" start= auto DisplayName= "`"$displayName`""

# Start the service
Write-Output "✅ Starting the service..."
Start-Service $serviceName

# Show log if it exists
if (Test-Path $logFile) {
    Write-Output "`n📜 Last 10 lines of log:"
    Get-Content $logFile -Tail 10 | ForEach-Object { Write-Output "  $_" }
} else {
    Write-Output "ℹ️ Log file does not exist yet: $logFile"
}

# Wait for user
Read-Host "`n🔁 Press ENTER to exit..."
