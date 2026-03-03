using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TaskManagerDesktop.Data.Repositories;

namespace TaskManagerDesktop.Services
{
    // Кэш для справочных данных (приоритеты, категории)
    // Ссылок: 6
    public static class ReferenceDataCache
    {
        private static List<Priority> _priorities;
        private static List<Category> _categories;
        private static DateTime _prioritiesLastUpdate = DateTime.MinValue;
        private static DateTime _categoriesLastUpdate = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        private static readonly object _lock = new object();

        /// Получает все приоритеты (из кэша или БД)
        // Ссылок: 2
        public static async Task<ObservableCollection<Priority>> GetPrioritiesAsync()
        {
            lock (_lock)
            {
                if (_priorities != null && (DateTime.Now - _prioritiesLastUpdate) < CacheDuration)
                {
                    return new ObservableCollection<Priority>(_priorities);
                }
            }

            // Загружаем из БД
            using (var repository = new PriorityRepository())
            {
                var priorities = await repository.GetAllPrioritiesAsync();

                lock (_lock)
                {
                    _priorities = priorities;
                    _prioritiesLastUpdate = DateTime.Now;
                    return new ObservableCollection<Priority>(priorities);
                }
            }
        }

        // Получает все категории (из кэша или БД)
        // Ссылок: 2
        public static async Task<ObservableCollection<Category>> GetCategoriesAsync()
        {
            lock (_lock)
            {
                if (_categories != null && (DateTime.Now - _categoriesLastUpdate) < CacheDuration)
                {
                    return new ObservableCollection<Category>(_categories);
                }
            }

            // Загружаем из БД
            using (var repository = new CategoryRepository())
            {
                var categories = await repository.GetAllCategoriesAsync();

                lock (_lock)
                {
                    _categories = categories;
                    _categoriesLastUpdate = DateTime.Now;
                    return new ObservableCollection<Category>(categories);
                }
            }
        }

        // Получает приоритет по ID
        // Ссылок: 1
        public static async Task<Priority> GetPriorityByIdAsync(int id)
        {
            // Сначала проверяем кэш
            lock (_lock)
            {
                if (_priorities != null)
                {
                    var cached = _priorities.Find(p => p.Id == id);
                    if (cached != null) return cached;
                }
            }

            // Если нет в кэше, загружаем из БД
            using (var repository = new PriorityRepository())
            {
                var priority = await repository.GetPriorityByIdAsync(id);

                // Добавляем в кэш
                lock (_lock)
                {
                    if (_priorities == null) _priorities = new List<Priority>();
                    if (!_priorities.Exists(p => p.Id == id))
                    {
                        _priorities.Add(priority);
                    }
                }

                return priority;
            }
        }

        // Получает категорию по ID
        // Ссылок: 1
        public static async Task<Category> GetCategoryByIdAsync(int? id)
        {
            if (!id.HasValue) return null;

            // Сначала проверяем кэш
            lock (_lock)
            {
                if (_categories != null)
                {
                    var cached = _categories.Find(c => c.Id == id.Value);
                    if (cached != null) return cached;
                }
            }

            // Если нет в кэше, загружаем из БД
            using (var repository = new CategoryRepository())
            {
                var category = await repository.GetCategoryByIdAsync(id.Value);

                // Добавляем в кэш
                lock (_lock)
                {
                    if (_categories == null) _categories = new List<Category>();
                    if (category != null && !_categories.Exists(c => c.Id == category.Id))
                    {
                        _categories.Add(category);
                    }
                }

                return category;
            }
        }

        // Сбрасывает кэш приоритетов
        // Ссылок: 0
        public static void InvalidatePrioritiesCache()
        {
            lock (_lock)
            {
                _priorities = null;
                _prioritiesLastUpdate = DateTime.MinValue;
            }
        }

        // Сбрасывает кэш категорий
        // Ссылок: 0
        public static void InvalidateCategoriesCache()
        {
            lock (_lock)
            {
                _categories = null;
                _categoriesLastUpdate = DateTime.MinValue;
            }
        }
    }
}