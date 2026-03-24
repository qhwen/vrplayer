# Download GitHub Actions APK artifact
$repoOwner = "qhwen"
$repoName = "vrplayer"
$downloadDir = "builds/downloads"

Write-Host "Fetching latest successful run..." -ForegroundColor Green
$runsUrl = "https://api.github.com/repos/$repoOwner/$repoName/actions/workflows/build.yml/runs?status=success&per_page=1"

try {
    $runsResponse = Invoke-RestMethod -Uri $runsUrl -Method Get -Headers @{
        "User-Agent" = "PowerShell"
        "Accept" = "application/vnd.github.v3+json"
    }
    
    if ($runsResponse.total_count -eq 0) {
        Write-Host "No successful builds found" -ForegroundColor Red
        exit 1
    }
    
    $latestRun = $runsResponse.workflow_runs[0]
    $runId = $latestRun.id
    $runNumber = $latestRun.run_number
    
    Write-Host "Latest build found:" -ForegroundColor Green
    Write-Host "  Run Number: #$runNumber" -ForegroundColor Yellow
    Write-Host "  Run ID: $runId" -ForegroundColor Yellow
    Write-Host "  Created: $($latestRun.created_at)" -ForegroundColor Yellow
    Write-Host "  URL: $($latestRun.html_url)" -ForegroundColor Cyan
    
    # Get artifacts
    $artifactsUrl = "https://api.github.com/repos/$repoOwner/$repoName/actions/runs/$runId/artifacts"
    $artifactsResponse = Invoke-RestMethod -Uri $artifactsUrl -Method Get -Headers @{
        "User-Agent" = "PowerShell"
        "Accept" = "application/vnd.github.v3+json"
    }
    
    $apkArtifact = $artifactsResponse.artifacts | Where-Object { $_.name -like "*APK*" -or $_.name -like "*apk*" }
    
    if (-not $apkArtifact) {
        Write-Host "APK artifact not found. Available artifacts:" -ForegroundColor Yellow
        foreach ($artifact in $artifactsResponse.artifacts) {
            Write-Host "  - $($artifact.name)" -ForegroundColor Yellow
        }
        exit 1
    }
    
    Write-Host "`nAPK artifact found:" -ForegroundColor Green
    Write-Host "  Name: $($apkArtifact.name)" -ForegroundColor Yellow
    Write-Host "  Size: $([math]::Round($apkArtifact.size_in_bytes / 1MB, 2)) MB" -ForegroundColor Yellow
    
    Write-Host "`nTo download, open this URL in browser (requires GitHub login):" -ForegroundColor Cyan
    Write-Host "https://github.com/$repoOwner/$repoName/actions/runs/$runId" -ForegroundColor Cyan
    Write-Host "`nOr visit Actions page:" -ForegroundColor Cyan
    Write-Host "https://github.com/$repoOwner/$repoName/actions" -ForegroundColor Cyan
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`nManual download:" -ForegroundColor Yellow
    Write-Host "https://github.com/$repoOwner/$repoName/actions" -ForegroundColor Cyan
    exit 1
}
