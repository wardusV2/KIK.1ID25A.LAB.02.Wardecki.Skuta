// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Text;
// using MathNet.Numerics.Distributions;
//
// namespace LAB2
// {
//     /// <summary>
//     /// Program do łamania szyfru Cezara metodą brute-force
//     /// z wykorzystaniem testu chi-kwadrat do oceny zgodności z językiem angielskim
//     /// </summary>
//     class Program
//     {
//         static void Main(string[] args)
//         {
//             // ===============================
//             // Deklaracja zmiennych konfiguracyjnych
//             // ===============================
//             string attackMode = "";      // Tryb ataku (np. "bf" - brute force)
//             string inputFile = "";       // Ścieżka do pliku z szyfrogramem
//             string outputFile = "";      // Ścieżka do pliku wyjściowego z wynikami
//             // Plik z częstościami występowania liter w języku angielskim
//             string monogramFile = "C:\\Users\\wardusV2\\Desktop\\LAB2_KiK\\LAB2\\LAB2\\english_monograms.txt";
//             
//             // Przykład użycia:
//             // dotnet run -- -a bf -i Encrypt.txt -o outputFile.txt // komenda do uruchomienia programu
//
//             // ===============================
//             // Parsowanie argumentów wiersza poleceń
//             // ===============================
//             for (int i = 0; i < args.Length; i++)
//             {
//                 switch (args[i])
//                 {
//                     case "-a":
//                         attackMode = args[++i]; // Odczyt trybu ataku (kolejny argument)
//                         break;
//                     case "-i":
//                         inputFile = args[++i];  // Odczyt pliku wejściowego
//                         break;
//                     case "-o":
//                         outputFile = args[++i]; // Odczyt pliku wyjściowego
//                         break;
//                 }
//             }
//
//             // ===============================
//             // Walidacja parametrów wejściowych
//             // ===============================
//             if (string.IsNullOrEmpty(attackMode) || string.IsNullOrEmpty(inputFile) || string.IsNullOrEmpty(outputFile))
//             {
//                 Console.WriteLine("Błąd: brak wymaganych argumentów!");
//                 Console.WriteLine("Użycie:");
//                 Console.WriteLine("  ./program -a bf -i szyfrogram.txt -o tekst_odszyfrowany.txt");
//                 return;
//             }
//
//             // Sprawdzenie, czy wybrano obsługiwany tryb ataku
//             if (attackMode != "bf")
//             {
//                 Console.WriteLine("Nieznany tryb ataku. Dostępne: bf (brute-force Cezara)");
//                 return;
//             }
//
//             // ===============================
//             // Wczytanie danych referencyjnych (częstości liter w języku angielskim)
//             // ===============================
//             if (!File.Exists(monogramFile))
//             {
//                 Console.WriteLine($"Nie znaleziono pliku z monogramami: {monogramFile}");
//                 return;
//             }
//
//             // Wczytanie prawdopodobieństw występowania liter z pliku
//             var englishProbs = LoadReferenceProbabilities(monogramFile);
//
//             if (englishProbs.Count == 0)
//             {
//                 Console.WriteLine("Nie udało się wczytać danych referencyjnych z pliku.");
//                 return;
//             }
//
//             Console.WriteLine($"Wczytano {englishProbs.Count} monogramów z pliku {monogramFile}\n");
//
//             // ===============================
//             // Wczytanie szyfrogramu z pliku
//             // ===============================
//             if (!File.Exists(inputFile))
//             {
//                 Console.WriteLine($"Nie znaleziono pliku '{inputFile}'.");
//                 return;
//             }
//
//             string cipherText = File.ReadAllText(inputFile, Encoding.UTF8);
//             if (string.IsNullOrWhiteSpace(cipherText))
//             {
//                 Console.WriteLine("Plik z szyfrogramem jest pusty.");
//                 return;
//             }
//
//             // ===============================
//             // Konfiguracja testu chi-kwadrat
//             // ===============================
//             double alpha = 0.05;  // Poziom istotności (5%)
//             int df = englishProbs.Count - 1;  // Stopnie swobody (liczba liter - 1)
//             // Wartość krytyczna rozkładu chi-kwadrat dla danego poziomu istotności
//             double criticalValue = ChiSquared.InvCDF(df, 1 - alpha);
//
//             Console.WriteLine($"Poziom istotności a={alpha}, df={df}, wartość krytyczna X2={criticalValue:F4}\n");
//
//             // Lista do przechowywania wyników dla wszystkich przesunięć
//             var results = new List<(int shift, double chi2, string text)>();
//
//             // ===============================
//             // Główna pętla brute-force - testowanie wszystkich 26 możliwych przesunięć
//             // ===============================
//             for (int shift = 0; shift < 26; shift++)
//             {
//                 // Odszyfrowanie tekstu z aktualnym przesunięciem
//                 string plain = CaesarDecrypt(cipherText, shift);
//
//                 // Zliczenie wystąpień poszczególnych liter w odszyfrowanym tekście
//                 var observed = CountMonograms(plain, englishProbs.Keys);
//
//                 // Obliczenie statystyki chi-kwadrat (miara zgodności z językiem angielskim)
//                 double chi2 = ComputeChiSquared(observed, englishProbs);
//
//                 // Zapisanie wyniku dla tego przesunięcia
//                 results.Add((shift, chi2, plain));
//                 
//                 // Wyświetlenie wyniku z informacją, czy tekst może być poprawnym angielskim
//                 Console.WriteLine($"Shift={shift:D2} | X2={chi2:F4} | {(chi2 < criticalValue ? "Może być angielski" : "Nie")} | {plain}");
//             }
//
//             // ===============================
//             // Wybór najlepszego wyniku (najmniejsza wartość chi-kwadrat)
//             // ===============================
//             var best = results.OrderBy(r => r.chi2).First();
//
//             Console.WriteLine("\n=========================================");
//             Console.WriteLine($"Najlepszy wynik:\nShift={best.shift} X2={best.chi2:F4}");
//             Console.WriteLine(best.text);
//             Console.WriteLine("=========================================");
//
//             // ===============================
//             // Zapis wyników do pliku wyjściowego
//             // ===============================
//             var sb = new StringBuilder();
//             sb.AppendLine($"Najlepszy wynik: Shift={best.shift} X2={best.chi2:F4}");
//             sb.AppendLine(best.text);
//             sb.AppendLine();
//             sb.AppendLine("Top 5 wyników:");
//             
//             // Zapisanie 5 najlepszych wyników (z najmniejszymi wartościami chi-kwadrat)
//             foreach (var r in results.OrderBy(r => r.chi2).Take(5))
//             {
//                 sb.AppendLine($"Shift={r.shift:D2} X2={r.chi2:F4}");
//                 sb.AppendLine(r.text);
//                 sb.AppendLine(new string('-', 60));
//             }
//
//             File.WriteAllText(outputFile, sb.ToString(), Encoding.UTF8);
//             Console.WriteLine($"\nZapisano wynik do pliku: {outputFile}");
//         }
//
//         // ==========================================================
//         /// <summary>
//         /// Wczytuje prawdopodobieństwa występowania n-gramów z pliku referencyjnego
//         /// </summary>
//         /// <param name="referenceFile">Ścieżka do pliku z danymi referencyjnymi</param>
//         /// <returns>Słownik: klucz = litera/n-gram, wartość = prawdopodobieństwo wystąpienia</returns>
//         // ==========================================================
//         static Dictionary<string, double> LoadReferenceProbabilities(string referenceFile)
//         {
//             var probabilities = new Dictionary<string, double>();
//             var lines = File.ReadAllLines(referenceFile, Encoding.UTF8);
//
//             // Parsowanie każdej linii pliku
//             foreach (var line in lines)
//             {
//                 // Pomijanie pustych linii i komentarzy
//                 if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
//                     continue;
//
//                 // Podział linii na części (n-gram i wartość)
//                 var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
//                 if (parts.Length >= 2)
//                 {
//                     string ngram = parts[0].ToUpper();  // Normalizacja do wielkich liter
//
//                     // Próba parsowania jako liczba całkowita (liczność)
//                     if (long.TryParse(parts[1], out long count))
//                     {
//                         probabilities[ngram] = count;
//                     }
//                     // Próba parsowania jako liczba zmiennoprzecinkowa (prawdopodobieństwo)
//                     else if (double.TryParse(parts[1].Replace(',', '.'),
//                         System.Globalization.NumberStyles.Float,
//                         System.Globalization.CultureInfo.InvariantCulture, out double prob))
//                     {
//                         probabilities[ngram] = prob;
//                     }
//                 }
//             }
//
//             // ===============================
//             // Konwersja liczności na prawdopodobieństwa (jeśli to konieczne)
//             // ===============================
//             double maxValue = probabilities.Values.Max();
//             
//             // Jeśli wartości są większe niż 1.0, to są to liczności, nie prawdopodobieństwa
//             if (maxValue > 1.0)
//             {
//                 double total = probabilities.Values.Sum();  // Suma wszystkich wystąpień
//                 var converted = new Dictionary<string, double>();
//
//                 // Normalizacja: każda liczność / suma wszystkich = prawdopodobieństwo
//                 foreach (var kvp in probabilities)
//                     converted[kvp.Key] = kvp.Value / total;
//
//                 Console.WriteLine($"Przekonwertowano liczności na prawdopodobieństwa (N={total})");
//                 return converted;
//             }
//
//             return probabilities;
//         }
//
//         // ==========================================================
//         /// <summary>
//         /// Odszyfrowuje tekst zaszyfrowany szyfrem Cezara
//         /// </summary>
//         /// <param name="input">Tekst zaszyfrowany</param>
//         /// <param name="shift">Wartość przesunięcia (0-25)</param>
//         /// <returns>Tekst odszyfrowany</returns>
//         // ==========================================================
//         static string CaesarDecrypt(string input, int shift)
//         {
//             var chars = input.ToCharArray();
//             
//             // Przetwarzanie każdego znaku
//             for (int i = 0; i < chars.Length; i++)
//             {
//                 char c = chars[i];
//                 
//                 // Odszyfrowanie wielkich liter (A-Z)
//                 if (char.IsUpper(c))
//                     chars[i] = (char)('A' + (c - 'A' - shift + 26) % 26);
//                 // Odszyfrowanie małych liter (a-z)
//                 else if (char.IsLower(c))
//                     chars[i] = (char)('a' + (c - 'a' - shift + 26) % 26);
//                 // Inne znaki (cyfry, spacje, interpunkcja) pozostają bez zmian
//             }
//             
//             return new string(chars);
//         }
//
//         // ==========================================================
//         /// <summary>
//         /// Zlicza wystąpienia monogramów (pojedynczych liter) w tekście
//         /// </summary>
//         /// <param name="text">Tekst do analizy</param>
//         /// <param name="validLetters">Zbiór poprawnych liter (z pliku referencyjnego)</param>
//         /// <returns>Słownik: klucz = litera, wartość = liczba wystąpień</returns>
//         // ==========================================================
//         static Dictionary<string, int> CountMonograms(string text, IEnumerable<string> validLetters)
//         {
//             var set = new HashSet<string>(validLetters);  // Zbiór dla szybkiego sprawdzania
//             var counts = new Dictionary<string, int>();
//             
//             // Zliczanie każdej litery w tekście
//             foreach (char c in text.ToUpper())
//             {
//                 if (char.IsLetter(c))
//                 {
//                     string s = c.ToString();
//                     
//                     // Zliczanie tylko liter występujących w danych referencyjnych
//                     if (set.Contains(s))
//                     {
//                         if (!counts.ContainsKey(s))
//                             counts[s] = 0;
//                         counts[s]++;
//                     }
//                 }
//             }
//             
//             return counts;
//         }
//
//         // ==========================================================
//         /// <summary>
//         /// Oblicza wartość statystyki testu chi-kwadrat
//         /// Porównuje obserwowane częstości liter z oczekiwanymi (dla języka angielskiego)
//         /// </summary>
//         /// <param name="ngrams">Obserwowane liczności liter w tekście</param>
//         /// <param name="referenceProbabilities">Prawdopodobieństwa referencyjne (język angielski)</param>
//         /// <returns>Wartość chi-kwadrat (im mniejsza, tym lepsze dopasowanie)</returns>
//         // ==========================================================
//         static double ComputeChiSquared(Dictionary<string, int> ngrams, Dictionary<string, double> referenceProbabilities)
//         {
//             // Całkowita liczba liter w tekście
//             double totalCount = ngrams.Values.Sum();
//             if (totalCount == 0) return double.MaxValue;  // Brak liter = najgorszy wynik
//
//             double chiSquared = 0.0;
//
//             // Obliczenie statystyki chi-kwadrat dla każdej litery
//             foreach (var kvp in ngrams)
//             {
//                 string ngram = kvp.Key;
//                 double Ci = kvp.Value;  // Liczność obserwowana (observed count)
//
//                 // Sprawdzenie, czy litera występuje w danych referencyjnych
//                 if (referenceProbabilities.TryGetValue(ngram, out double Pi))
//                 {
//                     double Ei = totalCount * Pi;  // Liczność oczekiwana (expected count)
//                     
//                     // Wzór chi-kwadrat: suma((Obserwowane - Oczekiwane)² / Oczekiwane)
//                     if (Ei > 0)
//                         chiSquared += Math.Pow(Ci - Ei, 2) / Ei;
//                 }
//             }
//
//             return chiSquared;
//         }
//     }
// }