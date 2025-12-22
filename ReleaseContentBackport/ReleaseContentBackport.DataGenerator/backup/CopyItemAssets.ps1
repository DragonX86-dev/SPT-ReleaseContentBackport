# Скрипт для копирования файлов с сохранением структуры каталогов
# Путь к JSON файлу
$jsonFilePath = "assets_paths.json"

# Путь к исходной папке assets (относительно текущей директории)
$sourceBasePath = "."

# Путь к целевой папке (куда будем копировать)
$targetBasePath = ".\output"

# Создаем целевую папку, если она не существует
if (-not (Test-Path $targetBasePath)) {
    New-Item -ItemType Directory -Path $targetBasePath -Force | Out-Null
    Write-Host "Создана целевая папка: $targetBasePath"
}

# Читаем JSON файл
try {
    $filePaths = Get-Content $jsonFilePath | ConvertFrom-Json
    Write-Host "Загружено путей из JSON: $($filePaths.Count)"
}
catch {
    Write-Host "Ошибка при чтении JSON файла: $_" -ForegroundColor Red
    exit 1
}

$successCount = 0
$errorCount = 0

# Обрабатываем каждый путь
foreach ($filePath in $filePaths) {
    # Формируем полный исходный путь
    $sourceFilePath = Join-Path $sourceBasePath $filePath
    
    # Проверяем, существует ли исходный файл
    if (Test-Path $sourceFilePath) {
        # Формируем полный целевой путь
        $targetFilePath = Join-Path $targetBasePath $filePath
        
        # Получаем целевую директорию (убираем имя файла из пути)
        $targetDir = Split-Path $targetFilePath -Parent
        
        # Создаем целевую директорию, если она не существует
        if (-not (Test-Path $targetDir)) {
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        }
        
        try {
            # Копируем файл
            Copy-Item -Path $sourceFilePath -Destination $targetFilePath -Force
            Write-Host "Скопировано: $filePath" -ForegroundColor Green
            $successCount++
        }
        catch {
            Write-Host "Ошибка при копировании $filePath : $_" -ForegroundColor Red
            $errorCount++
        }
    }
    else {
        Write-Host "Файл не найден: $sourceFilePath" -ForegroundColor Yellow
        $errorCount++
    }
}

# Выводим статистику
Write-Host "`n=== Статистика ===" -ForegroundColor Cyan
Write-Host "Успешно скопировано: $successCount файлов" -ForegroundColor Green
Write-Host "Ошибок/не найдено: $errorCount файлов" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Gray" })
Write-Host "Всего обработано: $($filePaths.Count) путей" -ForegroundColor Cyan