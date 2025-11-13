// using System.Text;
// using System.Text.RegularExpressions;
//
// class Zad3
// {
//     static void Main(string[] args)
//     {
//         // Zmienne przechowujące parametry programu z linii poleceń
//         string mode = "";       // Tryb działania programu: "encrypt", "decrypt" lub "bruteforce"
//         string keyFile = "";    // Ścieżka do pliku zawierającego klucz (tablicę podstawień alfabetu)
//         string inputFile = "";  // Ścieżka do pliku wejściowego z tekstem do przetworzenia
//         string outputFile = ""; // Ścieżka do pliku wyjściowego, gdzie zostanie zapisany wynik
//
//         Console.WriteLine("Argumenty: " + string.Join(", ", args));
//         
//         // Przykłady uruchomienia:
//         // dotnet run -- -d -k keyFile.txt -i Encrypt.txt -o Decrypt.txt   // deszyfrowanie
//         // dotnet run -- -e -k keyFile.txt -i inputFile.txt -o Encrypt.txt // szyfrowanie
//
//         // Parsowanie argumentów wiersza poleceń (argumenty mogą występować w dowolnej kolejności)
//         for (int i = 0; i < args.Length; i++)
//         {
//             switch (args[i])
//             {
//                 case "-e":
//                     mode = "encrypt"; // Włączenie trybu szyfrowania
//                     break;
//                 case "-d":
//                     mode = "decrypt"; // Włączenie trybu deszyfrowania
//                     break;
//                 case "-k":
//                     keyFile = args[++i]; // Pobranie ścieżki do pliku z kluczem
//                     break;
//                 case "-i":
//                     inputFile = args[++i]; // Pobranie ścieżki do pliku wejściowego
//                     break;
//                 case "-o":
//                     outputFile = args[++i]; // Pobranie ścieżki do pliku wyjściowego
//                     break;
//             }
//         }
//
//         // Walidacja wymaganych parametrów
//         if (string.IsNullOrEmpty(mode) || string.IsNullOrEmpty(inputFile) || string.IsNullOrEmpty(outputFile))
//         {
//             Console.WriteLine("Błąd: brak wymaganych argumentów!");
//             Console.WriteLine("Użycie:");
//             Console.WriteLine("  Szyfrowanie:   program.exe -e -k keyfile -i inputfile -o outputfile");
//             Console.WriteLine("  Deszyfrowanie: program.exe -d -k keyfile -i inputfile -o outputfile");
//             Console.WriteLine("  Brute-force:   program.exe -a bf -i inputfile -o outputfile");
//             return;
//         }
//
//         // Dla trybu brute-force nie jest wymagany plik klucza
//         if (mode != "bruteforce" && string.IsNullOrEmpty(keyFile))
//         {
//             Console.WriteLine("Błąd: brak pliku klucza! (wymagany dla trybu -e i -d)");
//             return;
//         }
//
//         // Wyświetlenie podsumowania konfiguracji
//         Console.WriteLine($"Tryb: {mode}, KeyFile: {keyFile}, InputFile: {inputFile}, OutputFile: {outputFile}");
//
//         try
//         {
//             // Wczytanie zawartości pliku wejściowego w kodowaniu UTF-8
//             string text = File.ReadAllText(inputFile, Encoding.UTF8);
//             
//             
//                 // Przygotowanie tekstu do szyfrowania (normalizacja: tylko wielkie litery A-Z)
//                 if (mode == "encrypt")
//                 {
//                     text = PrepareText(text);
//                 }
//
//                 // Wczytanie tablicy podstawieniowej (klucza) z pliku
//                 (int a, int b) = LoadKey(keyFile);
//
//                 // Wykonanie operacji szyfrowania lub deszyfrowania
//                 string result = mode == "encrypt"
//                     ? Encrypt(text, a,b)
//                     : Decrypt(text, a,b);
//
//                 // Zapisanie wyniku operacji do pliku wyjściowego
//                 File.WriteAllText(outputFile, result, Encoding.UTF8);
//
//                 Console.WriteLine(mode == "encrypt"
//                     ? $"Zaszyfrowano tekst do pliku: {outputFile}"
//                     : $"Odszyfrowano tekst do pliku: {outputFile}");
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"Wystąpił błąd: {ex.Message}");
//         }
//     }
//     
//     /// <summary>
//     /// Przygotowuje tekst do szyfrowania poprzez normalizację:
//     /// - konwersję wszystkich liter na wielkie (A-Z)
//     /// - usunięcie wszystkich znaków niebędących literami
//     /// </summary>
//     static string PrepareText(string input)
//     {
//         string onlyLetters = Regex.Replace(input.ToUpper(), "[^A-Z]", "");
//         return onlyLetters;
//     }
//
//     /// <summary>
//     /// Wczytuje klucz do szyfru Afinicznego z pliku.
//     /// </summary>
//     static (int a, int b) LoadKey(string keyFile)
//     {
//         string content = File.ReadAllText(keyFile).Trim();
//         string[] parts = content.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
//
//         if (parts.Length != 2 || !int.TryParse(parts[0], out int a) || !int.TryParse(parts[1], out int b))
//         {
//             throw new FormatException("Plik klucza musi zawierać dwie liczby całkowite: a b (np. 5 8).");
//         }
//
//         a = ((a % 26) + 26) % 26;
//         b = ((b % 26) + 26) % 26;
//
//         if (Gcd(a, 26) != 1)
//         {
//             throw new Exception("Współczynnik a musi być względnie pierwszy z 26");
//         }
//         return (a, b);
//     }
//     static int Gcd(int a, int b)
//     {
//         while (b != 0)
//         {
//             int temp = a % b;
//             a = b;
//             b = temp;
//         }
//         return a;
//     }
//     static int ModInverse(int a, int m)
//     {
//         a = ((a % m) + m) % m;
//         for (int x = 1; x < m; x++)
//         {
//             if ((a * x) % m == 1)
//                 return x;
//         }
//         throw new Exception("Brak odwrotności modularnej dla 'a'.");
//     }
//     
//     /// <summary>
//     /// Szyfruje tekst szyfrem Afinicznym o zadane klucz.
//     /// </summary>
//     static string Encrypt(string text, int a, int b)
//     {
//         var sb = new StringBuilder();
//
//         foreach (var c in text)
//         {
//             if (c >= 'A' && c <= 'Z')
//             {
//                 int x = c - 'A';
//                 int enc = (a * x + b) % 26;
//                 sb.Append((char)('A' + enc));
//             }
//         }
//         return sb.ToString();
//     }
//
//     /// <summary>
//     /// Deszyfruje tekst zaszyfrowany szyfrem Afinicznym.
//     /// </summary>
//     static string Decrypt(string text, int a, int b)
//     {
//         var sb = new StringBuilder();
//         int aInv=ModInverse(a, 26);
//
//         foreach (var c in text)
//         {
//             if (c >= 'A' && c <= 'Z')
//             {
//                 int y = c - 'A';
//                 int dec = (aInv * (y - b + 26)) % 26;
//                 sb.Append((char)('A' + dec));
//             }
//         }
//         return sb.ToString();
//     }
// }