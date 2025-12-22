# Скрипт для преобразования assets_paths.json в формат bundles.json
$inputFile = "assets_paths.json"
$outputFile = "bundles.json"

# Проверяем существование входного файла
if (-not (Test-Path $inputFile)) {
    Write-Host "Файл $inputFile не найден!" -ForegroundColor Red
    exit 1
}

# Читаем исходный JSON файл
try {
    $assetPaths = Get-Content $inputFile -Raw | ConvertFrom-Json
    Write-Host "Загружено $($assetPaths.Count) путей из $inputFile"
}
catch {
    Write-Host ("Ошибка при чтении {0}: {1}" -f $inputFile, $_.Exception.Message) -ForegroundColor Red
    exit 1
}

# Создаем массив для результата
$manifest = @()

# Преобразуем каждый путь в объект с нужной структурой
foreach ($path in $assetPaths) {
    $item = [PSCustomObject]@{
        key = $path
        dependencyKeys = @(
			"assets/commonassets/physics/physicsmaterials.bundle",
			"cubemaps",
			"shaders"
		) 
    }
    $manifest += $item
}

# Создаем финальный объект с ключом "manifest"
$result = [PSCustomObject]@{
    manifest = $manifest
}

# Преобразуем в JSON с красивым форматированием
$jsonOutput = $result | ConvertTo-Json -Depth 10

# Сохраняем результат в файл
try {
    $jsonOutput | Out-File -FilePath $outputFile -Encoding UTF8
    Write-Host "Файл успешно преобразован!" -ForegroundColor Green
    Write-Host "Результат сохранен в: $outputFile" -ForegroundColor Cyan
    Write-Host "Создано записей: $($manifest.Count)" -ForegroundColor Green
}
catch {
    Write-Host ("Ошибка при сохранении {0}: {1}" -f $outputFile, $_.Exception.Message) -ForegroundColor Red
    exit 1
}