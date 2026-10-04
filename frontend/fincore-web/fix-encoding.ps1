$strictUtf8 = New-Object System.Text.UTF8Encoding($false, $true)
$plainUtf8 = New-Object System.Text.UTF8Encoding($false)
$turkishAnsi = [System.Text.Encoding]::GetEncoding(1254)

$files = @()
$files += Get-ChildItem -Path "src" -Recurse -Include *.ts,*.tsx,*.css
$files += Get-Item "index.html"
$files += Get-Item "vite.config.ts"

foreach ($file in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    try {
        # Gecerli UTF-8 ise dokunma
        $null = $strictUtf8.GetString($bytes)
    }
    catch {
        # Gecerli degilse Turkce ANSI olarak oku, UTF-8 olarak geri yaz
        $text = $turkishAnsi.GetString($bytes)
        [System.IO.File]::WriteAllText($file.FullName, $text, $plainUtf8)
        Write-Host "Converted: $($file.FullName)"
    }
}

Write-Host "Done."