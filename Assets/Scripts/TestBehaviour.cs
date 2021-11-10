using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBehaviour : MonoBehaviour
{
    string inputWord = "";

    void Update()
    {
        foreach (char c in Input.inputString) {
            if (c == '\b') { // backspace/delete pressed
                if (inputWord.Length != 0) {
                    inputWord = inputWord.Substring(0, inputWord.Length - 1);
                }
            }
            else if ((c == '\n') || (c == '\r')) { // enter/return
                print($"Checking: {inputWord}");
                if (inputWord.Length == 0) return;
                var inputWordUpper = inputWord.ToUpper();
                var wordIsValid = WordDictionary.WordIsValid(inputWordUpper);
                if (wordIsValid) {
                    print($"{inputWordUpper} EXISTS!");
                } else {
                    print($"{inputWordUpper} WAS NOT FOUND...");
                }
                inputWord = "";
            }
            else {
                inputWord += c;
            }
        }
    }
}
