Stop-Process -Name AspireDemo.Blog -Force -ErrorAction SilentlyContinue
Stop-Process -Name AspireDemo.ApiService -Force -ErrorAction SilentlyContinue
Stop-Process -Name AspireDemo.Web -Force -ErrorAction SilentlyContinue
Stop-Process -Name AspireDemo.AppHost -Force -ErrorAction SilentlyContinue
Stop-Process -Name dcpctrl -Force -ErrorAction SilentlyContinue
Write-Host "All processes stopped"
