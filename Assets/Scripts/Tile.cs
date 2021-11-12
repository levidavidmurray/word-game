using System;
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
    public float defaultScale = 1f;
    public float selectedScaleMultiplier = 1.55f;
    public float playedScale = 1f;

    public float scaleUpDuration = 0.1f;
    public float playTranslateDuration = 0.2f;

    public TileSpace shuffleSpace;
    public bool debugShuffleMovement = false;

    public Vector3 squashScale = new Vector3(1.05f, 0.95f, 1f);
    public Vector3 stretchScale = new Vector3(0.95f, 1.05f, 1f);
    public float squashTime = 0.15f;
    public float stretchTime = 0.15f;

    public bool isSquashed = false;
    public bool isStretched = false;

    private Transform _tileSprite;
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
    private int _shuffleTweenId = 0;

    private char _cachedLetter;
    private TileSpace _cachedSpace;
    private bool _cachedShowValidWordBadge = false;
    private bool _cachedValidWordBadgeIsValid = false;
    private bool _cachedIsLocked;
    private bool _cachedIsSquashed;
    private bool _cachedIsStretched;

    private LeanSelectableByFinger _leanSelectable;
    private LineRenderer _lr;

    void Awake() {
        _tileSprite = transform.Find("TileSprite");
        var canvas = _tileSprite.Find("Canvas");
        _canvas = canvas.GetComponent<Canvas>();
        _letterText = canvas.Find("TileLetter").GetComponent<TMP_Text>();
        _pointsText = canvas.Find("TilePoints").GetComponent<TMP_Text>();
        _validBadge = transform.Find("Valid");
        _invalidBadge = transform.Find("Invalid");
        _leanSelectable = GetComponent<LeanSelectableByFinger>();
        _sr = _tileSprite.GetComponent<SpriteRenderer>();
        _lr = GetComponent<LineRenderer>();

        SetScale(defaultScale);
    }

    void Update() {
        if (ShouldUpdate()) {
            UpdateTile();
        }

        if ((space && !gameObject.LeanIsTweening()) || IsLocked) {
            // transform.position = space.transform.position;
        }

        if (isSquashed && !_cachedIsSquashed) {
            Squash();
        }
        if (isStretched && !_cachedIsStretched) {
            Stretch();
        }
        if (!isSquashed && _cachedIsSquashed) {
            UndoSquashStretch();
        }
        if (!isStretched && _cachedIsStretched) {
            UndoSquashStretch();
        }

        _cachedLetter = letter;
        _cachedSpace = space;
        _cachedShowValidWordBadge = _showValidWordBadge;
        _cachedValidWordBadgeIsValid = _validWordBadgeIsValid;
        _cachedIsLocked = IsLocked;

        _cachedIsSquashed = isSquashed;
        _cachedIsStretched = isStretched;
    }

    public void HandleFingerUp(LeanFinger finger) {
        _sr.sortingOrder = 1;
        _canvas.sortingOrder = 1;

        LeanTween.cancel(_scaleTweenId);
        GameEvents.current.PlaceTile(this);
    }

    public void HandleSelected() {
        if (IsLocked) return;

        SetSortingOrder(3);

        print($"Tile Selected: {letter}");
        LeanTween.cancel(_scaleTweenId);
        var scale = defaultScale * selectedScaleMultiplier;
        var newScale = new Vector3(scale, scale, 1f);
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

    public bool IsRacked {
        get { return space?.isRackSpace ?? false; }
    }

    public void SetVisibility(bool visible) {
        _sr.enabled = visible;
        _canvas.enabled = visible;
    }

    public int UndoSquashStretch(Action onComplete = null) {
        return LeanTween.scale(_tileSprite.gameObject, Vector3.one, squashTime).setOnComplete(onComplete).id;
    }

    public int Squash(Action onComplete = null) {
        return LeanTween.scale(_tileSprite.gameObject, squashScale, squashTime).setOnComplete(onComplete).id;
    }

    public int Stretch(Action onComplete = null) {
        return LeanTween.scale(_tileSprite.gameObject, stretchScale, stretchTime).setOnComplete(onComplete).id;
    }

    public void CurveBetweenPoints(float time, float delay, Vector3 p0, Vector3 p1, Vector3 p2, Action onComplete) {
        if (_shuffleTweenId > 0) {
            LeanTween.cancel(_shuffleTweenId);
        }
        if (shuffleSpace) {
            transform.position = shuffleSpace.transform.position;
            _tileSprite.localScale = Vector3.one;
        }

        _shuffleTweenId = LeanTween.delayedCall(delay, () => {
            _shuffleTweenId = Squash(() => {
                Stretch();

                int i = 0;
                _lr.positionCount = 0;
                _shuffleTweenId = LeanTween.value(0f, 1f, time)
                    .setDelay(delay)
                    .setOnUpdate((float t) => {
                        i++;
                        transform.position = Bezier.CalculateQuadBezierPoint(t, p0, p1, p2);

                        if (debugShuffleMovement) {
                            _lr.positionCount = i;
                            _lr.SetPosition(i-1, transform.position);
                        }
                    })
                    .setOnComplete(() => {
                        _shuffleTweenId = Squash(() => {
                            _shuffleTweenId = UndoSquashStretch(onComplete);
                        });
                    }).id;
            });
        }).id;

    }

    public void SetSpaceAndEase(TileSpace _space, float placedScale) {
        space = _space;
        _translateTweenId = gameObject.LeanMove(
            space.transform.position, 
            playTranslateDuration
        ).id;
        _scaleTweenId = gameObject.LeanScale(
            new Vector3(placedScale, placedScale, 1f), 
            scaleUpDuration
        ).id;
    }

    public void SetValidWordBadge(bool showBadge, bool isValid = false) {
        _showValidWordBadge = showBadge;
        _validWordBadgeIsValid = isValid;
    }

    public void SetSortingOrder(int sortingOrder) {
        _sr.sortingOrder = sortingOrder;
        _canvas.sortingOrder = sortingOrder;
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
