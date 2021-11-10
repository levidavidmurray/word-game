using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Lean.Touch;

public class Tile : MonoBehaviour
{
    public char letter = 'A';

    public TileSpace space;
    // tile is being moved
    public float defaultScale = 1.15f;
    public float selectedScale = 1.55f;

    public float scaleUpDuration = 0.1f;
    public float scaleDownDuration = 0.1f;

    public float playTranslateDuration = 0.2f;

    private SpriteRenderer _sr;
    private Canvas _canvas;
    private TMP_Text _letterText;
    private TMP_Text _pointsText;

    private Transform _validBadge;
    private Transform _invalidBadge;

    private bool _showValidWordBadge = false;
    private bool _validWordBadgeIsValid = false;

    private int _scaleTweenId;
    private int _translateTweenId = 0;

    private char _cachedLetter;
    private TileSpace _cachedSpace;
    private bool _cachedShowValidWordBadge = false;
    private bool _cachedValidWordBadgeIsValid = false;
    private bool _cachedIsLocked;

    private LeanSelectableByFinger _leanSelectable;

    void Awake() {
        var canvas = transform.Find("Canvas");
        _canvas = canvas.GetComponent<Canvas>();
        _letterText = canvas.Find("TileLetter").GetComponent<TMP_Text>();
        _pointsText = canvas.Find("TilePoints").GetComponent<TMP_Text>();
        _validBadge = transform.Find("Valid");
        _invalidBadge = transform.Find("Invalid");
        _leanSelectable = GetComponent<LeanSelectableByFinger>();
        _sr = GetComponent<SpriteRenderer>();

        SetScale(defaultScale);
    }

    void Update() {
        if (ShouldUpdate()) {
            UpdateTile();
        }

        if ((space && !IsTweening) || IsLocked) {
            transform.position = space.transform.position;
        }

        _cachedLetter = letter;
        _cachedSpace = space;
        _cachedShowValidWordBadge = _showValidWordBadge;
        _cachedValidWordBadgeIsValid = _validWordBadgeIsValid;
        _cachedIsLocked = IsLocked;
    }

    public void HandleFingerUp(LeanFinger finger) {
        _sr.sortingOrder = 1;
        _canvas.sortingOrder = 1;

        LeanTween.cancel(_scaleTweenId);
        var newScale = new Vector3(defaultScale, defaultScale, 1f);
        _scaleTweenId = gameObject.LeanScale(newScale, scaleDownDuration).id;

        GameEvents.current.PlaceTile(this);
    }

    public void HandleSelected() {
        if (IsLocked) return;

        _sr.sortingOrder = 3;
        _canvas.sortingOrder = 3;

        print($"Tile Selected: {letter}");
        LeanTween.cancel(_scaleTweenId);
        var newScale = new Vector3(selectedScale, selectedScale, 1f);
        _scaleTweenId = gameObject.LeanScale(newScale, scaleUpDuration).id;

        GameEvents.current.ActivateTile(this);
    }

    bool IsTweening {
        get {
            return _translateTweenId > 0 && LeanTween.isTweening(_translateTweenId);
        }
    }

    public bool IsLocked {
        get {
            return space && space.isLocked;
        }
    }

    public void SetSpaceAndEase(TileSpace _space) {
        space = _space;
        _translateTweenId = gameObject.LeanMove(
            space.transform.position, 
            playTranslateDuration
        ).id;
    }

    public void SetValidWordBadge(bool showBadge, bool isValid = false) {
        _showValidWordBadge = showBadge;
        _validWordBadgeIsValid = isValid;
    }

    void SetScale(float scale) {
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    void UpdateTile() {
        var points = TileBag.LetterPointsMap[letter];
        _pointsText.SetText(points.ToString());
        _letterText.SetText(letter.ToString());

        _invalidBadge.gameObject.SetActive(_showValidWordBadge && !_validWordBadgeIsValid);
        _validBadge.gameObject.SetActive(_showValidWordBadge && _validWordBadgeIsValid);
        _leanSelectable.enabled = !IsLocked;
    }

    bool ShouldUpdate() {
        return letter != _cachedLetter ||
            space != _cachedSpace || 
            _validWordBadgeIsValid != _cachedValidWordBadgeIsValid || 
            _showValidWordBadge != _cachedShowValidWordBadge ||
            IsLocked != _cachedIsLocked;
    }

}
