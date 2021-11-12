using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{

    public static UIController current;

    public Button PlayButton;
    public Button RecallButton;
    public Button ShuffleButton;

    public Label PlayerScore;
    public Label PlayerName;
    public Label OpponentScore;
    public Label OpponentName;

    void Awake() {
        if (current == null)
        {
            current = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
        }

        var root = GetComponent<UIDocument>().rootVisualElement;

        PlayButton = root.Q<Button>("play-button");
        RecallButton = root.Q<Button>("recall-button");
        ShuffleButton = root.Q<Button>("shuffle-button");
        PlayerScore = root.Q<Label>("player-score");
        PlayerName = root.Q<Label>("player-name");
        OpponentScore = root.Q<Label>("opponent-score");
        OpponentName = root.Q<Label>("opponent-name");
    }

}
