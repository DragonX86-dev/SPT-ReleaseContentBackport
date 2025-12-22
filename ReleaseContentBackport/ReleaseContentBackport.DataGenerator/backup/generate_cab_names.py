import os
import json
from pathlib import Path

def find_cab_files_recursive(base_dir="."):
    """
    Рекурсивно находит файлы с именем, начинающимся на 'cab' во всех подкаталогах
    и возвращает словарь {имя_файла: относительный_путь_к_каталогу}
    """
    result = {}
    
    # Проверяем существование базовой директории
    base_path = Path(base_dir)
    if not base_path.exists():
        print(f"Ошибка: Директория '{base_dir}' не существует")
        return result
    
    # Используем os.walk для рекурсивного обхода всех подкаталогов
    for root, dirs, files in os.walk(base_dir):
        # Преобразуем путь в Path-объект для удобства
        current_dir = Path(root)
        
        # Ищем файлы, начинающиеся на 'cab' в текущей директории
        for filename in files:
            if filename.startswith('cab'):
                # Получаем относительный путь от базовой директории
                try:
                    relative_path = current_dir.relative_to(base_dir)
                except ValueError:
                    # Если базовый путь и текущий одинаковы (относительный путь будет ".")
                    relative_path = Path(".")
                
                # Преобразуем путь в строку и заменяем обратные слеши на прямые
                path_str = str(relative_path).replace('\\', '/')
                
                # Если путь пустой или текущая директория, используем пустую строку
                if path_str == ".":
                    path_str = ""
                
                # Добавляем в результат
                result[filename] = path_str
                break  # Предполагаем один файл на каталог
    
    return result

def find_cab_files_with_patterns(base_dir=".", output_file="result.json"):
    """
    Улучшенная версия с поддержкой детальной информации
    """
    result = {}
    stats = {
        "directories_scanned": 0,
        "files_found": 0,
        "directories_without_cab": 0
    }
    
    base_path = Path(base_dir)
    
    if not base_path.exists():
        print(f"Ошибка: Директория '{base_dir}' не существует")
        return result, stats
    
    print(f"Начинаю поиск файлов 'cab*' в '{base_dir}' и всех подкаталогах...")
    
    for root, dirs, files in os.walk(base_dir):
        current_dir = Path(root)
        stats["directories_scanned"] += 1
        
        # Ищем файлы, начинающиеся на 'cab'
        matching_files = [f for f in files if f.startswith("cab")]
        
        if matching_files:
            stats["files_found"] += 1
            
            # Берем первый найденный файл
            first_file = matching_files[0]
            
            # Получаем относительный путь
            try:
                relative_path = current_dir.relative_to(base_path)
            except ValueError:
                relative_path = Path(".")
            
            # Преобразуем путь: заменяем обратные слеши
            path_str = str(relative_path).replace('\\', '/')
            
            # Если путь - текущая директория, используем пустую строку
            if path_str == ".":
                path_str = ""
            
            # Добавляем в результат
            result[first_file] = path_str
        else:
            if files:  # Если в каталоге есть файлы, но не начинающиеся на 'cab'
                stats["directories_without_cab"] += 1
    
    return result, stats

def normalize_paths_in_json(input_file, output_file=None):
    """
    Функция для нормализации путей в уже существующем JSON файле
    Заменяет обратные слеши на прямые (без добавления префикса)
    """
    if output_file is None:
        output_file = input_file.replace('.json', '_normalized.json')
    
    try:
        # Читаем существующий JSON файл
        with open(input_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        # Обрабатываем каждый путь в значениях
        normalized_data = {}
        for key, value in data.items():
            # Заменяем обратные слеши на прямые
            normalized_value = value.replace('\\', '/')
            
            # Убираем префикс "assets/" если он есть
            if normalized_value.startswith('assets/'):
                normalized_value = normalized_value[7:]  # Убираем "assets/"
            elif normalized_value.startswith('./'):
                normalized_value = normalized_value[2:]  # Убираем "./"
            
            # Если путь стал пустым, оставляем пустую строку
            if normalized_value == ".":
                normalized_value = ""
            
            normalized_data[key] = normalized_value
        
        # Сохраняем нормализованные данные
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(normalized_data, f, ensure_ascii=False, indent=4)
        
        print(f"✓ Пути нормализованы и сохранены в: {output_file}")
        return normalized_data
    
    except FileNotFoundError:
        print(f"✗ Файл не найден: {input_file}")
        return {}
    except json.JSONDecodeError:
        print(f"✗ Ошибка чтения JSON файла: {input_file}")
        return {}
    except Exception as e:
        print(f"✗ Ошибка: {e}")
        return {}

def save_to_json(data, output_file="result.json"):
    """Сохраняет данные в JSON файл"""
    try:
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=4, sort_keys=True)
        print(f"✓ Результат сохранен в файл: {output_file}")
        return True
    except Exception as e:
        print(f"✗ Ошибка при сохранении файла: {e}")
        return False

def export_to_csv(data, output_file="result.csv"):
    """Экспорт результатов в CSV файл"""
    try:
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write("filename,directory\n")
            for filename, directory in data.items():
                # Если путь пустой, записываем как пустую строку
                directory_str = directory if directory else ""
                f.write(f'"{filename}","{directory_str}"\n')
        print(f"✓ Результаты также сохранены в CSV: {output_file}")
    except Exception as e:
        print(f"✗ Ошибка при сохранении CSV: {e}")

def print_results_table(data, title="НАЙДЕННЫЕ ФАЙЛЫ"):
    """Красивый вывод результатов в табличном формате"""
    if not data:
        print("Нет данных для отображения")
        return
    
    print(f"\n{title}")
    print("=" * 70)
    print(f"{'Имя файла':40} | {'Путь'}")
    print("-" * 70)
    
    for filename, directory in sorted(data.items()):
        # Обрезаем длинные строки для лучшего отображения
        display_filename = filename[:37] + "..." if len(filename) > 37 else filename
        
        # Если путь пустой, показываем "(текущая директория)"
        display_directory = directory if directory else "(текущая директория)"
        display_directory = display_directory[:25] + "..." if len(display_directory) > 25 else display_directory
        
        print(f"{display_filename:40} | {display_directory}")
    
    print("=" * 70)

def main():
    # Настройки
    base_directory = "."  # Текущая директория
    output_json = "cab_files.json"
    
    print("=" * 60)
    print("РЕКУРСИВНЫЙ ПОИСК ФАЙЛОВ, НАЧИНАЮЩИХСЯ НА 'cab'")
    print("(с заменой обратных слешей на прямые, без добавления префикса)")
    print("=" * 60)
    
    # Используем улучшенную версию с детальной статистикой
    result, stats = find_cab_files_with_patterns(base_directory)
    
    # Выводим статистику
    print("\n" + "=" * 60)
    print("СТАТИСТИКА ПОИСКА:")
    print(f"  Просмотрено каталогов: {stats['directories_scanned']}")
    print(f"  Найдено файлов 'cab*': {stats['files_found']}")
    print(f"  Каталогов без файлов 'cab': {stats['directories_without_cab']}")
    print("=" * 60)
    
    # Сохраняем результаты
    if result:
        # Сохраняем в JSON
        save_to_json(result, output_json)

        # Выводим результаты в виде таблицы
        print_results_table(result)
        
        # Выводим несколько примеров для проверки формата
        print("\n" + "=" * 60)
        print("ПРИМЕРЫ ФОРМАТИРОВАННЫХ ПУТЕЙ:")
        print("-" * 60)
        
        # Выбираем несколько примеров для демонстрации
        examples = list(result.items())[:min(5, len(result))]  # Первые 5 или меньше
        for filename, directory in examples:
            display_dir = directory if directory else "(текущая директория)"
            print(f"  Файл: {filename}")
            print(f"    Путь: {display_dir}")
            print()
        
        # Проверяем наличие обратных слешей
        has_backslashes = any('\\' in path for path in result.values())
        if has_backslashes:
            print("⚠️  ВНИМАНИЕ: В путях обнаружены обратные слеши!")
        else:
            print("✓ Все пути содержат только прямые слеши")
        
        # Считаем файлы в корневой директории
        root_files = sum(1 for path in result.values() if not path)
        if root_files > 0:
            print(f"✓ Найдено {root_files} файлов в корневой директории")
    
    else:
        print("\n❌ Файлы, начинающиеся на 'cab', не найдены в подкаталогах")
    
    print("\n" + "=" * 60)
    print("Поиск завершен!")

if __name__ == "__main__":
    main()