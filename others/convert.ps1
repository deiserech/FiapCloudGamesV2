Get-ChildItem -Path .\src -Recurse -Filter *.cs | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $content = $content -replace "`r`n", "`n"
    Set-Content $_.FullName -Value $content -NoNewline
}