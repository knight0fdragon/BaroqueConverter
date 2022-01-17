using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BaroqueConverter
{
    class Program
    {

        static Dictionary<string, int> CharacterToCode = File.ReadAllLines(Path.Combine(System.AppContext.BaseDirectory, "codeToCharacter.txt")).Select(s => s.Split('\t')).ToDictionary(k => k[1], v => int.Parse(v[0], NumberStyles.HexNumber));
        static Dictionary<int, string> CodeToCharacter = File.ReadAllLines(Path.Combine(System.AppContext.BaseDirectory, "codeToCharacter.txt")).Select(s => s.Split('\t')).ToDictionary(k => int.Parse(k[0], NumberStyles.HexNumber), v => v[1]);
        static async Task Main(string[] args)
        {
            var log = new List<string>();
            if (args.Length < 1)
            {
                Console.WriteLine("Only include 1 file name to convert.");
                return;
            }
            foreach (var arg in args)
            {
                if (Path.GetExtension(arg.ToLower()) == ".msb")
                {
                    var file = File.ReadAllBytes(arg);
                    var text = "";
                    var code = 0;
                    var pos = -1;
                    var writeOffset = true;
                    foreach (var b in file)
                    {
                        pos++;
                        if (writeOffset)
                        {
                            text += $"{pos:X4}\t";
                            writeOffset = false;
                        }
                        var value = (int)b;
                        if (code > 0)
                        {
                            value = (code << 8) | b;
                            if (value == 0xFA77)
                            {
                                value = 0xF8FB;
                            }
                            if (value == 0xFA9E)
                            {
                                value = 0xF9D8;
                            }
                            code = 0;
                        }
                        else if (b >= 0xF8 && b <= 0xFA)
                        {
                            code = b;
                            continue;
                        }
                        if (!CodeToCharacter.ContainsKey(value))
                        {
                            Console.WriteLine($"{value:X4} is not found, coding out");
                            log.Add($"{value:X4} is not found, spacing out");
                            text += $"{{Unknown Code:{value:X4} }}";
                            continue;
                        }
                        var character = CodeToCharacter[value].Replace("{\\n}", "\n");
                        text += character;
                        if (character.EndsWith("\n")) writeOffset = true;

                    }

                    var translatedFilePath = Path.ChangeExtension(arg, "eng.txt");
                    try
                    {

                        File.WriteAllText(Path.ChangeExtension(arg, "txt"), text);
                    }
                    catch
                    {
                        File.WriteAllText(Path.Combine(System.AppContext.BaseDirectory, Path.GetFileName(Path.ChangeExtension(arg, "txt"))), text);
                        translatedFilePath = Path.Combine(System.AppContext.BaseDirectory, Path.GetFileName(Path.ChangeExtension(arg, "eng.txt")));
                    }
                    var translated = await Translate(text);
                    File.WriteAllLines(translatedFilePath, translated);
                    File.WriteAllLines(Path.Combine(System.AppContext.BaseDirectory, Path.GetFileName(Path.ChangeExtension(arg, "log"))), log.ToArray());
                }
                else
                {
                    var file = File.ReadAllText(arg);
                    var bytes = new List<byte>();

                    foreach (var t in file)
                    {
                        var value = t.ToString();
                        if (!CharacterToCode.ContainsKey(value))
                        {
                            Console.WriteLine($"'{value}' is not found, spacing out");
                            value = " ";
                        }
                        var code = CharacterToCode[value.ToString()];
                        if (code > 0xFF)
                        {
                            bytes.Add((byte)(code >> 8));
                        }
                        bytes.Add((byte)code);
                    }
                    try
                    {
                        File.WriteAllBytes(Path.ChangeExtension(arg.ToUpper(), "MSB"), bytes.ToArray());
                    }
                    catch
                    {
                        File.WriteAllBytes(Path.Combine(System.AppContext.BaseDirectory, Path.GetFileName(Path.ChangeExtension(arg, "MSB")).ToUpper()), bytes.ToArray());
                    }
                }
            }

        }


        async static Task<string[]> Translate(string text)
        {
            var yandexTranslate = new string[text.Length];
            var yandexTranslateAsync = YandexTranslateText(text);
            try
            {
                yandexTranslate = (await yandexTranslateAsync).Split("\r\n");
                return yandexTranslate;
            }
            catch (Exception ex) { throw; }
        }
        async public static Task<string> YandexTranslateText(string input)
        {
            var strings = input.Split("\n");
            var requests = new List<string>();
            var output = "";
            var requestLine = "";
            foreach(var s in strings)
            {
                requestLine += s + "\n";
                if(requestLine.Length > 950)
                {
                    requests.Add(requestLine);
                    requestLine = "";
                }
            }
            requests.Add(requestLine);

            foreach (var request in requests)
            {
                try
                {
                    var ggg = new GTranslate.Translators.YandexTranslator();
                    var result = await ggg.TranslateAsync(request, "en", "ja");
                    output += result.Result;
                    
                }
                catch
                {
                    output += "Error here";
                }
            }
            return output;

        }
    }
}
