#Requires -Version 5.1
<#
.SYNOPSIS
    Gemeinsame Anmeldung der Seed-Skripte am Admin-Bereich.

.DESCRIPTION
    Seit Etappe 5.0 verlangen alle api/admin/* eine Anmeldung. Statt den Ablauf
    in jedem Seed-Skript zu wiederholen, steht er hier einmal und wird per
    Dot-Sourcing eingebunden.

    Zwei Fallstricke stecken darin, beide in CLAUDE.md §9 dokumentiert:

     1. Das Anmelde-Cookie ist "Secure". Der .NET-Cookie-Speicher schickt ein
        Secure-Cookie über http NICHT zurück - auch nicht an localhost.
     2. Einen von Hand gesetzten "Cookie"-Kopf verwirft Invoke-RestMethod
        stillschweigend. Man bekommt 401 und der Server sieht kaputt aus,
        obwohl er nur nie ein Cookie gesehen hat.

    Ausweg: das Cookie selbst bauen und in eine WebRequestSession legen. Ein
    selbst erzeugtes System.Net.Cookie ist standardmäßig nicht "Secure" und geht
    damit auch über http hinaus. Für ein Skript gegen den eigenen Rechner ist
    das in Ordnung - im Betrieb läuft ohnehin HTTPS.
#>

# Liest das Passwort aus dem Parameter oder aus der .env im Wurzelverzeichnis.
function Get-AdminPassword {
    param(
        [string]$AdminPassword,
        [Parameter(Mandatory)][string]$ScriptRoot
    )

    if ($AdminPassword) { return $AdminPassword }

    $envPfad = Join-Path $ScriptRoot '..\..\.env'

    if (Test-Path $envPfad) {
        foreach ($zeile in [System.IO.File]::ReadAllLines($envPfad, [System.Text.Encoding]::UTF8)) {
            if ($zeile -match '^\s*Admin__Password\s*=\s*(.+?)\s*$') { return $Matches[1] }
        }
    }

    throw 'Kein Admin-Passwort gefunden. Entweder -AdminPassword angeben oder Admin__Password in der .env setzen.'
}

# Meldet sich an und liefert eine WebRequestSession, die das Cookie mitschickt.
function Connect-Admin {
    param(
        [Parameter(Mandatory)][string]$ApiBaseUrl,
        [string]$AdminPassword,
        [Parameter(Mandatory)][string]$ScriptRoot
    )

    $passwort = Get-AdminPassword -AdminPassword $AdminPassword -ScriptRoot $ScriptRoot
    $rumpf = [System.Text.Encoding]::UTF8.GetBytes((@{ password = $passwort } | ConvertTo-Json))

    try {
        $antwort = Invoke-WebRequest -Method Post -Uri "$ApiBaseUrl/api/admin/auth/login" `
            -Body $rumpf -ContentType 'application/json; charset=utf-8' -UseBasicParsing
    }
    catch {
        throw "Anmeldung fehlgeschlagen. Laeuft das Backend, und stimmt Admin__Password? ($($_.Exception.Message))"
    }

    $gesetzt = $antwort.Headers['Set-Cookie']
    if (-not $gesetzt) { throw 'Der Server hat kein Anmelde-Cookie gesetzt.' }

    $paar = ($gesetzt -split ';')[0]
    $name = $paar.Substring(0, $paar.IndexOf('='))
    $wert = $paar.Substring($paar.IndexOf('=') + 1)

    $sitzung = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $sitzung.Cookies.Add((New-Object System.Net.Cookie($name, $wert, '/', ([System.Uri]$ApiBaseUrl).Host)))

    return $sitzung
}
