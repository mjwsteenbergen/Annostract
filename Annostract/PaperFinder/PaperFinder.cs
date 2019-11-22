using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annostract.PaperFinder.Crossref;
using ApiLibs.General;
using Martijn.Extensions.Linq;

namespace Annostract.PaperFinder
{
    public class PaperFinder
    {
        public static async Task<CrossRefSearchResult> Find(string input)
        {
            Memory mem = new Memory()
            {
                Application = "Annostract"
            };

            var dict = await mem.Read("academic-calls-crossref.json", new Dictionary<string, CrossRefSearchResult>());

            var paper = await dict.GetOrCompute(input.ToLower(), async (input) =>
            {
                var res = (await new CrossRefService().Find(input));

                var match = TryMatch(res, input);
                if(match != null) {
                    return match;
                }

                Console.WriteLine();
                Console.WriteLine($"Which paper are you looking for? [{input}]");
                Console.WriteLine($" [{0}] None of these");
                int num = 1;
                res.Select(i => i.Title?.CombineWithSpace() + " " + i.Subtitle?.CombineWithSpace()).Foreach(i =>
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

                if(chosenIndex == 0) {
                    return null;
                }

                return res[chosenIndex-1];
            });

            await mem.Write("academic-calls-crossref.json", dict);

            return paper;
        }

        private static CrossRefSearchResult? TryMatch(List<CrossRefSearchResult> res, string input)
        {
            input = input.ToLower().Where(i => char.IsLetterOrDigit(i)).Select(i => i.ToString()).Combine((i, j) => $"{i}{j}");
            foreach (var result in res)
            {
                var test = $"{result.Title.CombineWithSpace()}{result.Subtitle?.CombineWithSpace()}".ToLower().Where(i => char.IsLetterOrDigit(i)).Select(i => i.ToString()).Combine((i, j) => $"{i}{j}");
                if(input == test) {
                    return result;
                }
            }

            return null;
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
