using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MathNet.Numerics.Distributions;

namespace LAB2
{
    /// <summary>
    /// Program do łamania szyfru afinicznego metodą brute-force
    /// Szyfr afiniczny: E(x) = (a*x + b) mod 26
    /// Deszyfrowanie: D(y) = a^(-1) * (y - b) mod 26
    /// gdzie a i 26 muszą być względnie pierwsze (gcd(a,26) = 1)
    /// </summary>
    class BfAttacks2
    {
        // Ścieżka do pliku z częstościami występowania liter w języku angielskim
        // (oznaczony jako readonly - wartość ustawiana raz, nie może być zmieniona)
        static readonly string monogramFile = "C:\\Users\\wardusV2\\Desktop\\LAB2_KiK\\LAB2\\LAB2\\english_monograms.txt";

        static void Main(string[] args)
        {
            // ===============================
            // Deklaracja zmiennych konfiguracyjnych
            // ===============================
            string mode = "";              // Tryb ataku (np. "bf" - brute force)
            string inputFileName = "";     // Plik z szyfrogramem
            string outputFileName = "";    // Plik wyjściowy z wynikami
            
            // Przykład użycia:
            // dotnet run -- -a bf -i Encrypt.txt -o outputFile.txt // komenda do uruchomienia programu

            // ===============================
            // Parsowanie argumentów wiersza poleceń
            // Obsługuje dwie formy:
            // 1. Standardową: "-a bf -i input.txt -o output.txt"
            // 2. Złączoną: "-abf -iinput.txt -ooutput.txt"
            // ===============================
            for (int i = 0; i < args.Length; i++)
            {
                string token = args[i];

                // --- Obsługa formy złączonej: flaga i wartość w jednym argumencie ---
                
                // Sprawdzenie formy "-abf" (wartość bezpośrednio po fladze)
                if (token.StartsWith("-a") && token.Length > 2)
                {
                    mode = token.Substring(2);  // Wyciągnięcie wartości po "-a"
                    continue;
                }
                // Sprawdzenie formy "-iinput.txt"
                if (token.StartsWith("-i") && token.Length > 2)
                {
                    inputFileName = token.Substring(2);
                    continue;
                }
                // Sprawdzenie formy "-ooutput.txt"
                if (token.StartsWith("-o") && token.Length > 2)
                {
                    outputFileName = token.Substring(2);
                    continue;
                }

                // --- Obsługa formy standardowej: flaga i wartość oddzielone spacją ---
                switch (token)
                {
                    case "-a":
                        // Odczyt wartości z kolejnego argumentu (jeśli istnieje)
                        if (i + 1 < args.Length) mode = args[++i];
                        break;
                    case "-i":
                        if (i + 1 < args.Length) inputFileName = args[++i];
                        break;
                    case "-o":
                        if (i + 1 < args.Length) outputFileName = args[++i];
                        break;
                    case "-h":
                    case "--help":
                        ShowUsage();  // Wyświetlenie instrukcji użycia
                        return;
                }
            }

            // ===============================
            // Walidacja parametrów wejściowych
            // ===============================
            if (string.IsNullOrEmpty(mode) || string.IsNullOrEmpty(inputFileName) || string.IsNullOrEmpty(outputFileName))
            {
                Console.WriteLine("Błąd: brak wymaganych argumentów!");
                ShowUsage();
                return;
            }

            // Sprawdzenie, czy wybrano obsługiwany tryb ataku
            if (!mode.Equals("bf", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Błąd: jedyny wspierany tryb ataku to 'bf' (brute-force).");
                ShowUsage();
                return;
            }

            // ===============================
            // Wczytanie danych referencyjnych (częstości liter w języku angielskim)
            // ===============================
            if (!File.Exists(monogramFile))
            {
                Console.WriteLine($"Nie znaleziono pliku z monogramami: {monogramFile}");
                return;
            }

            var englishProbs = LoadReferenceProbabilities(monogramFile);

            if (englishProbs.Count == 0)
            {
                Console.WriteLine("Nie udało się wczytać danych referencyjnych z pliku.");
                return;
            }

            Console.WriteLine($"Wczytano {englishProbs.Count} monogramów z pliku {monogramFile}\n");

            // ===============================
            // Wczytanie szyfrogramu z pliku
            // ===============================
            if (!File.Exists(inputFileName))
            {
                Console.WriteLine($"Nie znaleziono pliku '{inputFileName}'.");
                return;
            }

            string cipherText = File.ReadAllText(inputFileName, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(cipherText))
            {
                Console.WriteLine("Plik z szyfrogramem jest pusty.");
                return;
            }

            // ===============================
            // Konfiguracja testu chi-kwadrat
            // ===============================
            double alpha = 0.05;  // Poziom istotności (5%)
            int df = englishProbs.Count - 1;  // Stopnie swobody (liczba liter - 1)
            // Obliczenie wartości krytycznej rozkładu chi-kwadrat
            double criticalValue = ChiSquared.InvCDF(df, 1 - alpha);

            Console.WriteLine($"Poziom istotności a={alpha}, df={df}, wartość krytyczna X2={criticalValue:F4}\n");

            // Lista do przechowywania wyników dla wszystkich kombinacji (a, b)
            var results = new List<(int a, int b, double chi2, string text)>();

            // ===============================
            // Generowanie wszystkich poprawnych wartości parametru 'a'
            // Warunek: gcd(a, 26) = 1 (a i 26 muszą być względnie pierwsze)
            // Tylko wtedy istnieje odwrotność modularna a^(-1) mod 26
            // ===============================
            var validAs = Enumerable.Range(0, 26).Where(a => Gcd(a, 26) == 1).ToList();

            // ===============================
            // Główna pętla brute-force - testowanie wszystkich kombinacji (a, b)
            // ===============================
            foreach (int a in validAs)
            {
                int aInv;  // Odwrotność modularna parametru 'a'
                
                try
                {
                    // Obliczenie a^(-1) mod 26
                    aInv = ModInverse(a, 26);
                }
                catch
                {
                    // Teoretycznie nie powinno się zdarzyć, bo filtrowaliśmy przez gcd==1
                    // Ale dla bezpieczeństwa obsługujemy wyjątek
                    continue;
                }

                // Testowanie wszystkich możliwych wartości parametru 'b' (0-25)
                for (int b = 0; b < 26; b++)
                {
                    // Odszyfrowanie tekstu z aktualnymi parametrami (aInv, b)
                    string plain = AffineDecrypt(cipherText, aInv, b);

                    // Zliczenie wystąpień poszczególnych liter w odszyfrowanym tekście
                    var observed = CountMonograms(plain, englishProbs.Keys);

                    // Obliczenie statystyki chi-kwadrat (miara zgodności z językiem angielskim)
                    double chi2 = ComputeChiSquared(observed, englishProbs);

                    // Zapisanie wyniku dla tej kombinacji parametrów
                    results.Add((a, b, chi2, plain));

                    // Wyświetlenie skróconej informacji (pełny tekst będzie w pliku wynikowym)
                    Console.WriteLine($"a={a,2} b={b,2} | X2={chi2:F4} | {(chi2 < criticalValue ? "Może być angielski" : "Raczej nie")}");
                }
            }

            // ===============================
            // Wybór najlepszego wyniku (najmniejsza wartość chi-kwadrat)
            // ===============================
            var best = results.OrderBy(r => r.chi2).First();

            Console.WriteLine("\n=========================================");
            Console.WriteLine($"Najlepszy wynik: a={best.a} b={best.b}, X2={best.chi2:F4}");
            Console.WriteLine(best.text);
            Console.WriteLine("=========================================");

            // ===============================
            // Zapis top 10 wyników do pliku wyjściowego
            // ===============================
            var top10 = results.OrderBy(r => r.chi2).Take(10).ToList();
            var sb = new StringBuilder();
            sb.AppendLine("Top 10 wyników (Affine brute-force):");
            
            foreach (var r in top10)
            {
                sb.AppendLine($"a={r.a} b={r.b} X2={r.chi2:F4}");
                sb.AppendLine(r.text);
                sb.AppendLine(new string('-', 60));  // Separator
            }

            File.WriteAllText(outputFileName, sb.ToString(), Encoding.UTF8);
            Console.WriteLine($"\nZapisano wynik do pliku: {outputFileName}");
        }

        /// <summary>
        /// Wyświetla instrukcję użycia programu
        /// </summary>
        static void ShowUsage()
        {
            Console.WriteLine("Użycie:");
            Console.WriteLine("  program -a bf -i szyfrogram.txt -o tekst_odszyfrowany.txt");
            Console.WriteLine("Obsługiwane formy:");
            Console.WriteLine("  -a bf          (tryb brute-force)");
            Console.WriteLine("  -i <plik>      (plik z szyfrogramem)");
            Console.WriteLine("  -o <plik>      (plik wyjściowy z wynikami)");
            Console.WriteLine("Alternatywnie można użyć złączonych form, np. -abf -iinput.txt -ooutput.txt");
        }

        // ==========================================================
        /// <summary>
        /// Wczytuje prawdopodobieństwa występowania n-gramów z pliku referencyjnego
        /// </summary>
        /// <param name="referenceFile">Ścieżka do pliku z danymi referencyjnymi</param>
        /// <returns>Słownik: klucz = litera/n-gram, wartość = prawdopodobieństwo wystąpienia</returns>
        // ==========================================================
        static Dictionary<string, double> LoadReferenceProbabilities(string referenceFile)
        {
            var probabilities = new Dictionary<string, double>();
            var lines = File.ReadAllLines(referenceFile, Encoding.UTF8);

            // Parsowanie każdej linii pliku
            foreach (var line in lines)
            {
                // Pomijanie pustych linii i komentarzy
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                // Podział linii na części (n-gram i wartość)
                var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string ngram = parts[0].ToUpper();  // Normalizacja do wielkich liter

                    // Próba parsowania jako liczba całkowita (liczność)
                    if (long.TryParse(parts[1], out long count))
                    {
                        probabilities[ngram] = count;
                    }
                    // Próba parsowania jako liczba zmiennoprzecinkowa (prawdopodobieństwo)
                    else if (double.TryParse(parts[1].Replace(',', '.'),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double prob))
                    {
                        probabilities[ngram] = prob;
                    }
                }
            }

            // ===============================
            // Konwersja liczności na prawdopodobieństwa (jeśli to konieczne)
            // ===============================
            double maxValue = probabilities.Values.Max();
            
            // Jeśli wartości są większe niż 1.0, to są to liczności, nie prawdopodobieństwa
            if (maxValue > 1.0)
            {
                double total = probabilities.Values.Sum();  // Suma wszystkich wystąpień
                var converted = new Dictionary<string, double>();

                // Normalizacja: każda liczność / suma wszystkich = prawdopodobieństwo
                foreach (var kvp in probabilities)
                    converted[kvp.Key] = kvp.Value / total;

                Console.WriteLine($"Przekonwertowano liczności na prawdopodobieństwa (N={total})");
                return converted;
            }

            return probabilities;
        }

        // ==========================================================
        /// <summary>
        /// Odszyfrowuje tekst zaszyfrowany szyfrem afinicznym
        /// Wzór deszyfrowania: D(y) = a^(-1) * (y - b) mod 26
        /// </summary>
        /// <param name="input">Tekst zaszyfrowany</param>
        /// <param name="aInv">Odwrotność modularna parametru 'a' (a^(-1) mod 26)</param>
        /// <param name="b">Parametr przesunięcia 'b' (0-25)</param>
        /// <returns>Tekst odszyfrowany</returns>
        // ==========================================================
        static string AffineDecrypt(string input, int aInv, int b)
        {
            var chars = input.ToCharArray();
            
            // Przetwarzanie każdego znaku
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                
                // Odszyfrowanie wielkich liter (A-Z)
                if (char.IsUpper(c))
                {
                    int y = c - 'A';  // Konwersja litery na liczbę (0-25)
                    // Wzór: x = a^(-1) * (y - b) mod 26
                    // Dodajemy +26 aby uniknąć ujemnych wartości przed operacją modulo
                    int dec = (aInv * (y - b + 26)) % 26;
                    chars[i] = (char)('A' + dec);  // Konwersja liczby z powrotem na literę
                }
                // Odszyfrowanie małych liter (a-z)
                else if (char.IsLower(c))
                {
                    int y = c - 'a';
                    int dec = (aInv * (y - b + 26)) % 26;
                    chars[i] = (char)('a' + dec);
                }
                // Inne znaki (cyfry, spacje, interpunkcja) pozostają bez zmian
            }
            
            return new string(chars);
        }

        // ==========================================================
        /// <summary>
        /// Zlicza wystąpienia monogramów (pojedynczych liter) w tekście
        /// </summary>
        /// <param name="text">Tekst do analizy</param>
        /// <param name="validLetters">Zbiór poprawnych liter (z pliku referencyjnego)</param>
        /// <returns>Słownik: klucz = litera, wartość = liczba wystąpień</returns>
        // ==========================================================
        static Dictionary<string, int> CountMonograms(string text, IEnumerable<string> validLetters)
        {
            var set = new HashSet<string>(validLetters);  // Zbiór dla szybkiego sprawdzania
            var counts = new Dictionary<string, int>();
            
            // Zliczanie każdej litery w tekście
            foreach (char c in text.ToUpper())
            {
                if (char.IsLetter(c))
                {
                    string s = c.ToString();
                    
                    // Zliczanie tylko liter występujących w danych referencyjnych
                    if (set.Contains(s))
                    {
                        if (!counts.ContainsKey(s))
                            counts[s] = 0;
                        counts[s]++;
                    }
                }
            }
            
            return counts;
        }

        // ==========================================================
        /// <summary>
        /// Oblicza wartość statystyki testu chi-kwadrat
        /// Porównuje obserwowane częstości liter z oczekiwanymi (dla języka angielskiego)
        /// </summary>
        /// <param name="ngrams">Obserwowane liczności liter w tekście</param>
        /// <param name="referenceProbabilities">Prawdopodobieństwa referencyjne (język angielski)</param>
        /// <returns>Wartość chi-kwadrat (im mniejsza, tym lepsze dopasowanie)</returns>
        // ==========================================================
        static double ComputeChiSquared(Dictionary<string, int> ngrams, Dictionary<string, double> referenceProbabilities)
        {
            // Całkowita liczba liter w tekście
            double totalCount = ngrams.Values.Sum();
            if (totalCount == 0) return double.MaxValue;  // Brak liter = najgorszy wynik

            double chiSquared = 0.0;

            // Obliczenie statystyki chi-kwadrat dla każdej litery
            foreach (var kvp in ngrams)
            {
                string ngram = kvp.Key;
                double Ci = kvp.Value;  // Liczność obserwowana (observed count)

                // Sprawdzenie, czy litera występuje w danych referencyjnych
                if (referenceProbabilities.TryGetValue(ngram, out double Pi))
                {
                    double Ei = totalCount * Pi;  // Liczność oczekiwana (expected count)
                    
                    // Wzór chi-kwadrat: suma((Obserwowane - Oczekiwane)² / Oczekiwane)
                    if (Ei > 0)
                        chiSquared += Math.Pow(Ci - Ei, 2) / Ei;
                }
            }

            return chiSquared;
        }

        // ==========================================================
        // Funkcje pomocnicze do obliczeń matematycznych
        // ==========================================================
        
        /// <summary>
        /// Oblicza największy wspólny dzielnik (GCD) dwóch liczb
        /// Używa algorytmu Euklidesa
        /// </summary>
        /// <param name="a">Pierwsza liczba</param>
        /// <param name="b">Druga liczba</param>
        /// <returns>Największy wspólny dzielnik</returns>
        static int Gcd(int a, int b)
        {
            a = Math.Abs(a);  // Używamy wartości bezwzględnych
            b = Math.Abs(b);
            
            // Algorytm Euklidesa: powtarzamy dopóki b != 0
            while (b != 0)
            {
                int temp = a % b;  // Reszta z dzielenia
                a = b;
                b = temp;
            }
            return a;
        }

        /// <summary>
        /// Oblicza odwrotność modularną liczby 'a' modulo 'm'
        /// Znajduje liczbę x taką, że (a * x) mod m = 1
        /// </summary>
        /// <param name="a">Liczba, dla której szukamy odwrotności</param>
        /// <param name="m">Moduł</param>
        /// <returns>Odwrotność modularna a^(-1) mod m</returns>
        /// <exception cref="Exception">Gdy odwrotność nie istnieje (gcd(a,m) != 1)</exception>
        static int ModInverse(int a, int m)
        {
            // Normalizacja wartości 'a' do zakresu [0, m)
            a = ((a % m) + m) % m;
            
            // Metoda brute-force: sprawdzamy wszystkie możliwe wartości x
            for (int x = 1; x < m; x++)
            {
                // Szukamy x takiego, że (a * x) mod m = 1
                if ((a * x) % m == 1)
                    return x;
            }
            
            // Jeśli nie znaleziono odwrotności, rzucamy wyjątek
            throw new Exception("Brak odwrotności modularnej dla 'a'.");
        }
    }
}