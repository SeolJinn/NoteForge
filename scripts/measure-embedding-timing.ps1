<#
.SYNOPSIS
    Measures how long a local Ollama instance takes to embed a vault of Markdown
    notes, one note at a time, reporting cumulative time at checkpoints.

.DESCRIPTION
    Reproduces the embedding workload of NoteForge's EmbeddingService (sequential
    /api/embeddings calls over filename + body) without needing the app, Visual
    Studio or any developer tools installed. Only a running Ollama with the
    embedding model pulled is required.

    Run this on each machine you want to compare. The first call includes the
    one-time model load (warm-up), so do NOT pre-run any embedding beforehand.

.EXAMPLE
    .\measure-embedding-timing.ps1
    .\measure-embedding-timing.ps1 -Vault "D:\notes\demo-vault" -Model nomic-embed-text
#>

[CmdletBinding()]
param(
    [string]$Vault   = (Join-Path $PSScriptRoot "..\demo-vault"),
    [string]$Model   = "nomic-embed-text",
    [string]$Url     = "http://localhost:11434/api/embeddings",
    [int[]] $Checkpoints = @(25, 50, 75, 100)
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $Vault)) {
    throw "Vault folder not found: $Vault. Pass -Vault with the demo-vault path."
}

$files = Get-ChildItem -Path $Vault -Recurse -Filter *.md | Sort-Object FullName
if ($files.Count -eq 0) {
    throw "No .md files found under $Vault."
}

Write-Host "Embedding $($files.Count) notes with '$Model' via $Url ..." -ForegroundColor Cyan
Write-Host "(the first checkpoint includes the one-time model load)" -ForegroundColor DarkGray

$sw   = [System.Diagnostics.Stopwatch]::StartNew()
$i    = 0
$rows = @()

function Add-Row([int]$n, [double]$seconds) {
    [pscustomobject]@{
        notes     = $n
        seconds   = [math]::Round($seconds, 1)
        'ms/note' = [math]::Round($seconds / $n * 1000, 0)
    }
}

foreach ($f in $files) {
    $title = [System.IO.Path]::GetFileNameWithoutExtension($f.Name)
    $body  = Get-Content -Raw -LiteralPath $f.FullName
    $text  = "$title`n`n$body"

    $payload = @{ model = $Model; prompt = $text } | ConvertTo-Json -Compress
    $bytes   = [System.Text.Encoding]::UTF8.GetBytes($payload)
    Invoke-RestMethod -Uri $Url -Method Post -Body $bytes -ContentType "application/json; charset=utf-8" | Out-Null

    $i++
    if ($Checkpoints -contains $i) {
        $rows += Add-Row $i $sw.Elapsed.TotalSeconds
    }
}

$rows += Add-Row $i $sw.Elapsed.TotalSeconds

Write-Host ""
$rows | Format-Table -AutoSize

$outPath = Join-Path (Get-Location) "embedding-timing.txt"
$header  = "=== Embedding timing run (cold cache) ===`r`nModel: $Model  Notes: $($files.Count)`r`nnotes`tseconds`tms/note"
$lines   = $rows | ForEach-Object { "$($_.notes)`t$($_.seconds)`t$($_.'ms/note')" }
($header, ($lines -join "`r`n"), "") -join "`r`n" | Out-File -FilePath $outPath -Append -Encoding utf8

Write-Host "Saved to $outPath" -ForegroundColor Green
