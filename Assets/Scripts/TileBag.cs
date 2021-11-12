using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileBag : MonoBehaviour
{
    public static Dictionary<char, int> LetterPointsMap = new Dictionary<char, int> {
        {'A', 1}, {'B', 3}, {'C', 3}, {'D', 2}, {'E', 1},
        {'F', 4}, {'G', 2}, {'H', 4}, {'I', 1}, {'J', 8},
        {'K', 5}, {'L', 1}, {'M', 3}, {'N', 1}, {'O', 1},
        {'P', 3}, {'Q', 10}, {'R', 1}, {'S', 1}, {'T', 1},
        {'U', 1}, {'V', 4}, {'W', 4}, {'X', 8}, {'Y', 4}, {'Z', 10},
    };

    public static Dictionary<char, int> LetterCountMap = new Dictionary<char, int> {
        {'A', 9}, {'B', 2}, {'C', 2}, {'D', 4}, {'E', 12},
        {'F', 2}, {'G', 3}, {'H', 2}, {'I', 9}, {'J', 1},
        {'K', 1}, {'L', 4}, {'M', 2}, {'N', 6}, {'O', 8},
        {'P', 2}, {'Q', 1}, {'R', 6}, {'S', 4}, {'T', 6},
        {'U', 4}, {'V', 2}, {'W', 2}, {'X', 1}, {'Y', 2}, {'Z', 1}
    };

    public static char[] Alphabet = new char[26] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

    public static char[] Vowels = new char[5] { 'A', 'E', 'I', 'O', 'U' };

    private static TileBag _instance;
    private Dictionary<char, int> _bag;

    void Awake()
    {
        _bag = LetterCountMap.ToDictionary(entry => entry.Key, entry => entry.Value);
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    public static char[] GetRandomLetters(int letterCount, int forcedVowelCount = 0) {
        char[] letters = new char[letterCount];
        // TODO: Ensure there are enough letters available

        var vowelCount = 0;
        while (letterCount > 0) {
            var letter = Alphabet[Random.Range(0, 25)];
            if (_instance._bag[letter] <= 0) continue;

            if (forcedVowelCount > 0 && vowelCount != forcedVowelCount) {
                if (!Vowels.Contains(letter)) continue;
                vowelCount++;
            }
            letters[letters.Length - letterCount] = letter;
            _instance._bag[letter]--;
            letterCount--;
        }

        return letters;
    }

    public static int GetPointsForWord(string word) {
        var points = 0;
        foreach(var letter in word) {
            points += LetterPointsMap[letter];
        }
        return points;
    }

}
