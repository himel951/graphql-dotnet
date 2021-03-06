using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<TK> Map<T, TK>(this IEnumerable<T> items, Func<T, TK> map)
        {
            var mappedItems = from T item in items select map(item);
            return mappedItems.ToList();
        }

        public static IEnumerable<T> Map<T>(this IEnumerable items, Func<object, T> map)
        {
            var mappedItems = from object item in items select map(item);
            return mappedItems.ToList();
        }

        public static Task<object[]> MapAsync(this IEnumerable items, Func<object, Task<object>> map)
        {
            var tasks = items
                .Cast<object>()
                .Select(map);
            return Task.WhenAll(tasks);
        }

        public static async Task<IEnumerable<object>> MapAsync(this IEnumerable enumerable, Func<int, object, Task<object>> mapFunction)
        {
            var index = 0;
            var results = new List<object>();

            foreach (var item in enumerable)
            {
                var result = await mapFunction(index++, item);

                results.Add(result);
            }

            return results;

            //return await enumerable
            //    .Cast<object>()
            //    .Select((item, index) => Tuple.Create(index, item))
            //    .MapAsync(async tuple =>
            //    {
            //        var data = (Tuple<int, object>)tuple;
            //        return await mapFunction(data.Item1, data.Item2).ConfigureAwait(false);
            //    })
            //    .ConfigureAwait(false);
        }

        public static void Apply<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items)
            {
                action(item);
            }
        }

        public static void Apply<T>(this IEnumerable<T> items, Action<T, int> action)
        {
            var count = 0;
            foreach (var item in items)
            {
                action(item, count);
                count++;
            }
        }

        public static void ApplyReverse<T>(this IEnumerable<T> items, Action<T> action)
        {
            var list = items.ToList();

            for (var i = list.Count - 1; i >= 0; i--)
            {
                action(list[i]);
            }
        }

        public static async Task<Dictionary<TKey, TValueVal>> ToDictionaryThreaded<TSource, TKey, TValue, TValueVal>(
            this IEnumerable<TSource> items,
            Func<TSource, TKey> keyFunc,
            Func<TSource, Task<TValue>> valueFunc) where TValue : ResolveFieldResult<TValueVal>
        {
            var tasks = items
                .Select(async item => new {
                    Key = keyFunc(item),
                    Result = await valueFunc(item).ConfigureAwait(false)
                })
                .ToArray();

            var keyValuePairs = await Task.WhenAll(tasks).ConfigureAwait(false);
            keyValuePairs = keyValuePairs.Where(x => !x.Result.Skip).ToArray();

            return keyValuePairs.ToDictionary(kvp => kvp.Key, kvp => kvp.Result.Value);
        }

        public static async Task<IDictionary<TKey, TValueVal>> ToDictionaryAsync<TSource, TKey, TValue, TValueVal>(
            this IEnumerable<TSource> items,
            Func<TSource, TKey> keyFunc,
            Func<TSource, Task<TValue>> valueFunc) where TValue : ResolveFieldResult<TValueVal>
        {
            var data = new ConcurrentDictionary<TKey, TValueVal>();

            foreach (var item in items)
            {
                var serialResults = await valueFunc(item).ConfigureAwait(false);

                if (serialResults.Skip)
                    continue;

                data.TryAdd(keyFunc(item), serialResults.Value);
            }

            return data;
        }

        public static bool All(this IEnumerable items, Func<object, bool> check)
        {
            foreach (var item in items)
            {
                if (!check(item))
                {
                    return false;
                }
            }

            return true;
        }

        public static void Fill<T>(this IList<T> items, T itemToAdd)
        {
            Fill(items, new[] { itemToAdd });
        }

        public static void Fill<T>(this IList<T> items, IEnumerable<T> itemsToAdd)
        {
            itemsToAdd.Apply(x =>
            {
                if (!items.Contains(x))
                {
                    items.Add(x);
                }
            });
        }
    }
}
