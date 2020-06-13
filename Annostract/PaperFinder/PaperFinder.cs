using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annostract.PaperFinders.Crossref;
using Martijn.Extensions.Memory;
using Martijn.Extensions.Linq;
using System.Collections.Concurrent;
using System.Threading;

namespace Annostract.PaperFinders
{
    public static class PaperFinder
    {
        private static readonly SemaphoreSlim memoryLock = new SemaphoreSlim(1);
        private static readonly SemaphoreSlim internetLock = new SemaphoreSlim(1);
        private static readonly SemaphoreSlim consoleLock = new SemaphoreSlim(1);

        public static async Task<CrossRefSearchResult?> Find(string input)
        {
            Memory mem = new Memory()
            {
                Application = "Annostract",
                CreateDirectoryIfNotExists = true,
                WriteIndented = false
            };

            var dict = await mem.Read("academic-calls-crossref.json", new Dictionary<string, CrossRefSearchResult?>());

            if (dict.ContainsKey(input))
            {
                return dict[input];
            }
            
            CrossRefSearchResult? result = null;

            await internetLock.WaitAsync();
            var res = (await new CrossRefService().Find(input));

            if (TryMatch(res, input, out var result2))
            {
                result = result2 ?? throw new NullReferenceException();
            }

            internetLock.Release();

            if(result == null)
            {
                await consoleLock.WaitAsync();
                Console.WriteLine();
                Console.WriteLine($"Which paper are you looking for? [{input}]");
                Console.WriteLine($" [{0}] None of these");
                int num = 1;
                res.Select(i => i.Title?.CombineWithSpace() + " " + i.Subtitle?.CombineWithSpace()).Foreach((i) =>
                {
                    Console.WriteLine($" [{num}] {i}");
                    num++;
                });


                int chosenIndex = -100;
                do
                {
                    var nl = Console.ReadLine();
                    try
                    {
                        chosenIndex = int.Parse(nl);
                    }
                    catch
                    {
                        Console.WriteLine("Please write a number and press enter");
                    }
                } while (chosenIndex == -100);

                if (chosenIndex == 0)
                {
                    result = null;
                } else {
                    result = res[chosenIndex - 1];
                }

                consoleLock.Release();
            }

            await memoryLock.WaitAsync();
            dict = await mem.Read("academic-calls-crossref.json", new Dictionary<string, CrossRefSearchResult?>());
            dict[input] = result;
            await mem.Write("academic-calls-crossref.json", dict);
            memoryLock.Release();

            return result;
        }

        private static bool TryMatch(List<CrossRefSearchResult> res, string input, out CrossRefSearchResult? foundResult)
        {
            input = input.ToLower().Where(i => char.IsLetterOrDigit(i)).Select(i => i.ToString()).Combine((i, j) => $"{i}{j}");
            foreach (var result in res)
            {
                var test = $"{result.Title?.CombineWithSpace()}{result.Subtitle?.CombineWithSpace()}".ToLower().Where(i => char.IsLetterOrDigit(i)).Select(i => i.ToString()).Combine((i, j) => $"{i}{j}");
                if(input == test) {
                    foundResult = result;
                    return true;
                }
            }
            foundResult = null;
            return false;
        }
    }

    public static class DictionaryExtension
    {
        public static V GetOrCompute<T, V>(this Dictionary<T, V> dict, T key, Func<T, V> function)  
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            else
            {
                var value = function(key);
                dict.Add(key, value);
                return value;
            }
        }

        public static async Task<V> GetOrCompute<T, V>(this Dictionary<T, V> dict, T key, Func<T, Task<V>> function)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            else
            {
                var value = await function(key);
                dict.Add(key, value);
                return value;
            }
        }
    }
}
