import os
import re
import json
from pathlib import Path
from typing import Dict, List, Any, Optional
import datetime

def extract_asset_bundle_name(content: str) -> Optional[str]:
    """
    Извлекает значение m_AssetBundleName из содержимого файла.
    """
    try:
        # Ищем строку вида: string m_AssetBundleName = "значение"
        pattern = r'string m_AssetBundleName = "([^"]+)"'
        match = re.search(pattern, content)
        
        if match:
            return match.group(1)
        
        # Альтернативный вариант поиска
        pattern_alt = r'm_AssetBundleName\s*=\s*"([^"]+)"'
        match_alt = re.search(pattern_alt, content)
        
        if match_alt:
            return match_alt.group(1)
            
    except Exception as e:
        print(f"Ошибка при извлечении m_AssetBundleName: {e}")
    
    return None

def extract_dependencies_from_content(content: str) -> List[str]:
    """
    Извлекает список зависимостей из содержимого файла.
    Ищет секцию m_Dependencies и возвращает все cab-строки.
    """
    dependencies = []
    
    try:
        # Находим секцию m_Dependencies
        deps_start = content.find('vector m_Dependencies')
        if deps_start == -1:
            return dependencies
        
        # Ищем все строки с cab- зависимостями
        # Ищем строки вида: string data = "cab-..."
        cab_pattern = r'string data = "([^"]+)"'
        cab_matches = re.findall(cab_pattern, content[deps_start:])
        
        for cab in cab_matches:
            if cab.startswith('cab-'):
                dependencies.append(cab)
        
        # Альтернативный метод: ищем строки с cab- напрямую
        if not dependencies:
            cab_pattern = r'cab-[a-f0-9]+'
            dependencies = re.findall(cab_pattern, content[deps_start:])
            
    except Exception as e:
        print(f"Ошибка при извлечении зависимостей из содержимого: {e}")
    
    return dependencies

def process_file(file_path: Path) -> Dict[str, Any]:
    """
    Обрабатывает один файл и возвращает информацию о зависимостях.
    """
    result = {
        "dependencies": [],
        "file_name": file_path.name
    }
    
    try:
        # Читаем файл
        with open(file_path, 'r', encoding='utf-8') as file:
            content = file.read()
        
        # Извлекаем имя бандла
        asset_bundle_name = extract_asset_bundle_name(content)
        if asset_bundle_name:
            result["asset_bundle_name"] = asset_bundle_name
        else:
            # Если не нашли m_AssetBundleName, используем имя файла без расширения как запасной вариант
            file_stem = file_path.stem
            if file_stem.endswith('.bundle'):
                file_stem = file_stem[:-7]  # Удаляем .bundle
            result["asset_bundle_name"] = file_stem
            print(f"⚠ Для файла {file_path.name} не найдено m_AssetBundleName, используется имя файла: {file_stem}")
        
        # Извлекаем зависимости
        dependencies = extract_dependencies_from_content(content)
        result["dependencies"] = dependencies
        
    except UnicodeDecodeError:
        print(f"✗ Ошибка кодировки: {file_path.name}")
        return None
    except Exception as e:
        print(f"✗ Ошибка обработки {file_path.name}: {e}")
        return None
    
    return result

def process_folder(folder_path: str, file_extension: str = ".bundle.txt") -> Dict[str, Any]:
    """
    Обрабатывает все файлы с указанным расширением в папке.
    """
    folder = Path(folder_path)
    if not folder.exists():
        print(f"Папка {folder_path} не существует!")
        return {}
    
    if not folder.is_dir():
        print(f"{folder_path} не является папкой!")
        return {}
    
    # Получаем список файлов
    if file_extension == ".bundle.txt":
        # Ищем файлы с двойным расширением .bundle.txt
        files = list(folder.glob("*.bundle.txt"))
        # Также ищем файлы с простым .txt расширением, если не найдены .bundle.txt
        if not files:
            files = list(folder.glob("*.txt"))
    else:
        files = list(folder.glob(f"*{file_extension}"))
    
    if not files:
        print(f"Файлы с расширением {file_extension} не найдены в папке {folder_path}")
        return {}
    
    print(f"\nНайдено файлов: {len(files)}")
    print("=" * 60)
    
    results = {}
    successful = 0
    failed = 0
    missing_asset_names = 0
    
    # Обрабатываем каждый файл
    for file_path in files:
        if file_path.is_file():
            print(f"Обработка: {file_path.name}")
            
            file_result = process_file(file_path)
            
            if file_result is not None:
                # Используем asset_bundle_name в качестве ключа
                asset_bundle_key = file_result.get("asset_bundle_name")
                
                if not asset_bundle_key:
                    # Если вдруг asset_bundle_name отсутствует, используем имя файла
                    asset_bundle_key = file_path.stem
                    missing_asset_names += 1
                
                # Проверяем на дубликаты ключей
                if asset_bundle_key in results:
                    print(f"⚠ Внимание: дублирующийся ключ {asset_bundle_key}")
                    # Добавляем суффикс для уникальности
                    counter = 1
                    while f"{asset_bundle_key}_{counter}" in results:
                        counter += 1
                    asset_bundle_key = f"{asset_bundle_key}_{counter}"
                
                # Сохраняем результат с ключом asset_bundle_name
                # Удаляем asset_bundle_name из значения, так как оно уже является ключом
                result_value = {
                    "dependencies": file_result.get("dependencies", [])
                }
                results[asset_bundle_key] = result_value
                successful += 1
                
                deps_count = len(result_value.get("dependencies", []))
                if deps_count > 0:
                    print(f"✓ {asset_bundle_key}: найдено {deps_count} зависимостей")
                else:
                    print(f"✓ {asset_bundle_key}: зависимости не найдены")
            else:
                failed += 1
                print(f"✗ {file_path.name}: ошибка обработки")
    
    print("\n" + "=" * 60)
    print("РЕЗУЛЬТАТЫ ОБРАБОТКИ:")
    print("=" * 60)
    print(f"Успешно обработано: {successful}")
    print(f"С ошибками: {failed}")
    if missing_asset_names > 0:
        print(f"Файлов без m_AssetBundleName: {missing_asset_names}")
    
    return results

def save_results_to_json(results: Dict[str, Any], output_file: str = "dependencies_analysis.json"):
    """
    Сохраняет результаты анализа в JSON файл.
    """
    if not results:
        print("Нет данных для сохранения")
        return False
    
    try:
        # Подсчитываем статистику
        total_dependencies = sum(len(v["dependencies"]) for v in results.values())
        files_with_dependencies = sum(1 for v in results.values() if v["dependencies"])
        files_without_dependencies = sum(1 for v in results.values() if not v["dependencies"])
        
        # Добавляем метаданные в JSON
        enhanced_results = {
            "metadata": {
                "generated_at": datetime.datetime.now().isoformat(),
                "total_files": len(results),
                "total_dependencies": total_dependencies,
                "files_with_dependencies": files_with_dependencies,
                "files_without_dependencies": files_without_dependencies,
                "key_source": "m_AssetBundleName field (fallback: filename)"
            },
            "dependencies": results
        }
        
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(enhanced_results, f, indent=2, ensure_ascii=False)
        
        print(f"\n✓ Результаты сохранены в: {output_file}")
        print(f"  Всего файлов: {len(results)}")
        print(f"  Файлов с зависимостями: {files_with_dependencies}")
        print(f"  Файлов без зависимостей: {files_without_dependencies}")
        print(f"  Всего зависимостей: {total_dependencies}")
        print(f"  Ключи JSON: значения m_AssetBundleName из файлов")
        
        return True
    except Exception as e:
        print(f"✗ Ошибка при сохранении JSON: {e}")
        return False

def print_sample_json(results: Dict[str, Any], sample_size: int = 3):
    """
    Выводит пример структуры JSON.
    """
    if not results:
        return
    
    print("\n" + "=" * 60)
    print("ПРИМЕР СТРУКТУРЫ JSON:")
    print("=" * 60)
    
    sample_items = list(results.items())[:sample_size]
    
    sample_output = {
        "metadata": {
            "generated_at": datetime.datetime.now().isoformat(),
            "total_files": len(results),
            "total_dependencies": sum(len(v["dependencies"]) for v in results.values()),
            "key_source": "m_AssetBundleName field (fallback: filename)"
        },
        "dependencies": dict(sample_items)
    }
    
    print(json.dumps(sample_output, indent=2, ensure_ascii=False))
    
    if len(results) > sample_size:
        print(f"\n... и еще {len(results) - sample_size} файлов")

def generate_statistics(results: Dict[str, Any]) -> Dict[str, Any]:
    """
    Генерирует статистику по зависимостям.
    """
    if not results:
        return {}
    
    all_dependencies = []
    for file_key, file_data in results.items():
        all_dependencies.extend(file_data.get("dependencies", []))
    
    # Находим уникальные зависимости
    unique_deps = list(set(all_dependencies))
    
    # Считаем частоту использования каждой зависимости
    dep_frequency = {}
    for dep in all_dependencies:
        dep_frequency[dep] = dep_frequency.get(dep, 0) + 1
    
    # Сортируем по частоте использования
    most_common_deps = sorted(dep_frequency.items(), key=lambda x: x[1], reverse=True)
    
    statistics = {
        "total_unique_dependencies": len(unique_deps),
        "most_common_dependencies": most_common_deps[:10],  # Топ-10
        "files_by_dependency_count": {},
        "files_with_asset_bundle_keys": list(results.keys())[:20]  # Пример первых 20 ключей
    }
    
    # Группируем файлы по количеству зависимостей
    for file_key, file_data in results.items():
        dep_count = len(file_data.get("dependencies", []))
        statistics["files_by_dependency_count"][file_key] = dep_count
    
    return statistics

def save_statistics_to_file(statistics: Dict[str, Any], output_file: str = "dependencies_statistics.json"):
    """
    Сохраняет статистику в отдельный JSON файл.
    """
    try:
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(statistics, f, indent=2, ensure_ascii=False)
        print(f"✓ Статистика сохранена в: {output_file}")
    except Exception as e:
        print(f"✗ Ошибка при сохранении статистики: {e}")

def main():
    """
    Основная функция скрипта.
    """
    import argparse
    
    # Настройка аргументов командной строки
    parser = argparse.ArgumentParser(
        description='Анализ зависимостей в файлах AssetBundle'
    )
    parser.add_argument(
        'folder',
        nargs='?',
        default='.',
        help='Путь к папке с файлами (по умолчанию текущая папка)'
    )
    parser.add_argument(
        '-e', '--extension',
        default='.bundle.txt',
        help='Расширение файлов для анализа (по умолчанию .bundle.txt)'
    )
    parser.add_argument(
        '-o', '--output',
        default='dependencies_analysis.json',
        help='Имя выходного JSON файла (по умолчанию dependencies_analysis.json)'
    )
    parser.add_argument(
        '-s', '--statistics',
        action='store_true',
        help='Создать файл со статистикой'
    )
    parser.add_argument(
        '-v', '--verbose',
        action='store_true',
        help='Подробный вывод'
    )
    
    args = parser.parse_args()
    
    # Обрабатываем папку
    print(f"Анализ файлов в папке: {args.folder}")
    print(f"Расширение файлов: {args.extension}")
    print("Ключи в JSON будут созданы на основе поля m_AssetBundleName из файлов")
    print("=" * 60)
    
    results = process_folder(args.folder, args.extension)
    
    if not results:
        print("Нет данных для сохранения.")
        return
    
    # Сохраняем результаты в JSON
    json_saved = save_results_to_json(results, args.output)
    
    # Показываем пример структуры
    if args.verbose:
        print_sample_json(results)
    
    # Генерируем и сохраняем статистику
    if args.statistics and json_saved:
        statistics = generate_statistics(results)
        if statistics:
            save_statistics_to_file(statistics, "dependencies_statistics.json")
            
            # Выводим краткую статистику
            print("\n" + "=" * 60)
            print("КРАТКАЯ СТАТИСТИКА:")
            print("=" * 60)
            print(f"Уникальных зависимостей: {statistics['total_unique_dependencies']}")
            print(f"\nТоп-5 самых частых зависимостей:")
            for i, (dep, count) in enumerate(statistics['most_common_dependencies'][:5], 1):
                print(f"  {i}. {dep}: {count} файлов")
            
            # Файлы с наибольшим количеством зависимостей
            files_by_deps = sorted(
                statistics['files_by_dependency_count'].items(),
                key=lambda x: x[1],
                reverse=True
            )[:5]
            
            print(f"\nТоп-5 файлов с наибольшим количеством зависимостей:")
            for i, (file_key, count) in enumerate(files_by_deps, 1):
                print(f"  {i}. {file_key}: {count} зависимостей")

if __name__ == "__main__":
    main()