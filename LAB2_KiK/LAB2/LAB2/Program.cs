// using System.Text;
// using System.Text.RegularExpressions;
//
// class LAB2
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
//         // dotnet run -- -a bf -i szyfrogram.txt -o odszyfrowany.txt       // brute-force
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
//                 int shift = LoadKey(keyFile);
//
//                 // Wykonanie operacji szyfrowania lub deszyfrowania
//                 string result = mode == "encrypt"
//                     ? Encrypt(text, shift)
//                     : Decrypt(text, shift);
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
//     /// Wczytuje przesunięcie szyfru Cezara z pliku.
//     /// </summary>
//     static int LoadKey(string keyFile)
//     {
//         string content = File.ReadAllText(keyFile).Trim();
//
//         if (!int.TryParse(content, out int shift))
//         {
//             throw new Exception("Plik klucza musi zawierać jedną liczbę całkowitą (np. 3).");
//         }
//
//         // Dla bezpieczeństwa przesunięcie w zakresie 0–25
//         shift = ((shift % 26) + 26) % 26;
//
//         return shift;
//     }
//
//     /// <summary>
//     /// Szyfruje tekst szyfrem Cezara o zadane przesunięcie.
//     /// </summary>
//     static string Encrypt(string text, int shift)
//     {
//         var sb = new StringBuilder();
//
//         foreach (var c in text)
//         {
//             if (c >= 'A' && c <= 'Z')
//             {
//                 char enc = (char)('A' + (c - 'A' + shift + 26) % 26);
//                 sb.Append(enc);
//             }
//         }
//         return sb.ToString();
//     }
//
//     /// <summary>
//     /// Deszyfruje tekst zaszyfrowany szyfrem Cezara.
//     /// </summary>
//     static string Decrypt(string text, int shift)
//     {
//         var sb = new StringBuilder();
//
//         foreach (var c in text)
//         {
//             if (c >= 'A' && c <= 'Z')
//             {
//                 char dec = (char)('A' + (c - 'A' - shift + 26) % 26);
//                 sb.Append(dec);
//             }
//         }
//         return sb.ToString();
//     }
// }