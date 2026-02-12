#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Nainstaluje certifikat pro aplikaci Kavopici.

.DESCRIPTION
    Tento skript nainstaluje podpisovy certifikat do uloziste
    "Trusted Root Certification Authorities" na lokalnim pocitaci,
    aby bylo mozne nainstalovat MSIX balicek aplikace Kavopici.

    Certifikat staci nainstalovat pouze jednou — vsechny budouci
    aktualizace se nainstaluji automaticky.

.NOTES
    Skript je nutne spustit jako Administrator.
#>

$ErrorActionPreference = 'Stop'

$certPath = Join-Path $PSScriptRoot "Kavopici.cer"

if (-not (Test-Path $certPath)) {
    Write-Host ""
    Write-Host "Soubor Kavopici.cer nebyl nalezen." -ForegroundColor Red
    Write-Host "Ujistete se, ze tento skript je ve stejne slozce jako Kavopici.cer" -ForegroundColor Red
    Write-Host ""
    Read-Host "Stisknete Enter pro ukonceni"
    exit 1
}

Write-Host ""
Write-Host "=== Instalace certifikatu pro Kavopici ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Tento skript nainstaluje podpisovy certifikat aplikace"
Write-Host "do uloziste 'Trusted Root Certification Authorities'."
Write-Host ""
Write-Host "Certifikat staci nainstalovat pouze jednou." -ForegroundColor Green
Write-Host ""

$confirm = Read-Host "Pokracovat? (A/N)"
if ($confirm -notin @('A', 'a', 'Y', 'y')) {
    Write-Host "Zruseno." -ForegroundColor Yellow
    exit 0
}

try {
    $cert = Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\LocalMachine\Root"
    Write-Host ""
    Write-Host "Certifikat uspesne nainstalovan!" -ForegroundColor Green
    Write-Host "Predmet:    $($cert.Subject)" -ForegroundColor Gray
    Write-Host "Otisk:      $($cert.Thumbprint)" -ForegroundColor Gray
    Write-Host "Platnost do: $($cert.NotAfter.ToString('dd.MM.yyyy'))" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Nyni muzete nainstalovat Kavopici pomoci souboru .msix" -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "Chyba pri instalaci certifikatu: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Ujistete se, ze skript spoustite jako Administrator." -ForegroundColor Yellow
}

Write-Host ""
Read-Host "Stisknete Enter pro ukonceni"
