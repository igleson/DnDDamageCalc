# Development script with hot reloading
# This runs the app with dotnet watch for .NET hot reload + custom template/CSS hot reload

Write-Host "🔥 Starting D&D Damage Calculator with Hot Reload..." -ForegroundColor Green
Write-Host ""
Write-Host "Features enabled:" -ForegroundColor Yellow
Write-Host "  • .NET code hot reload (dotnet watch)" -ForegroundColor Cyan
Write-Host "  • Scriban template hot reload" -ForegroundColor Cyan  
Write-Host "  • CSS hot reload" -ForegroundColor Cyan
Write-Host "  • Browser auto-refresh on changes" -ForegroundColor Cyan
Write-Host ""
Write-Host "The app will be available at: http://localhost:5082" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host ""

# Set environment to Development
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Run with dotnet watch for .NET hot reload
Set-Location "src\DnDDamageCalc.Web"
dotnet watch run