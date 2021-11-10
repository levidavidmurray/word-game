using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

class WordEdge
{
    public char letter;
    bool terminal = false;
    List<WordEdge> edges;

    public WordEdge(string letters)
    {
        this.letter = letters[0];
        var pathLetters = letters.Remove(0,1);

        if (pathLetters.Length == 0) {
            this.terminal = true;
            return;
        }

        this.AddPath(pathLetters);
    }

    public void AddPath(string letters)
    {
        var edge = this.FindEdge(letters[0]);
        if (edge != null) {
            edge.AddPath(letters.Remove(0,1));
            return;
        }

        this.edges ??= new List<WordEdge>();
        this.edges.Add(new WordEdge(letters));
    }

    public WordEdge FindEdge(char letter)
    {
        return this.edges?.Find(edge => edge.letter == letter);
    }

    public bool HasEndOfPathTerminal(string letters)
    {
        if (letters.Length == 0) return this.terminal;
        var edge = this.FindEdge(letters[0]);
        if (edge == null) return false;
        return edge.HasEndOfPathTerminal(letters.Remove(0,1));
    }
}

public class WordDictionary : MonoBehaviour
{

    private static WordDictionary _instance;

    public static bool WordIsValid(string word)
    {
        return _instance.HasWord(word);
    }

    List<WordEdge> edges = new List<WordEdge>();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
            InitializeLexicon("Assets/Resources/words.txt");
        }
        else
        {
            Destroy(this);
        }
    }

    public bool HasWord(string word)
    {
        var edge = this.FindEdge(word[0]);
        if (edge == null) return false;

        return edge.HasEndOfPathTerminal(word.Remove(0,1));
    }

    private void InitializeLexicon(string filePath)
    {
        try {
            using (StreamReader sr = new StreamReader(filePath)) {
                string word;

                while ((word = sr.ReadLine()) != null) {
                    this.AddWord(word);
                }
            }
        }
        catch (System.Exception e) {
            print($"File could not be read: {filePath}");
            print(e.Message);
        }
    }

    private WordEdge FindEdge(char letter)
    {
        return this.edges.Find((edge) => edge.letter == letter);
    }

    private void AddWord(string word)
    {
        var edge = this.FindEdge(word[0]);
        if (edge != null) {
            edge.AddPath(word.Remove(0,1));
            return;
        }

        this.edges.Add(new WordEdge(word));
    }

}
