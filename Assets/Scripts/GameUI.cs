using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{

    private TMP_Text ScoreText;
    private static GameUI _instance;

    void Awake() {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
        }
        ScoreText = transform.Find("Players").Find("Player").Find("ScoreText").GetComponent<TMP_Text>();
    }

    public static void SetScore(int score) {
        _instance.ScoreText.SetText(score.ToString());
    }

}
