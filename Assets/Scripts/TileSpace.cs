using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TileSpace : MonoBehaviour
{
    public Vector2 tilePosition;
    public Tile tile;
    public bool isLocked = false;
    public bool isPlayable = false;
    public bool isCenterTile = false;
    public Sprite centerTileSprite;
    public bool isRackSpace = false;
    public int rackIndex = -1;

    private SpriteRenderer _sr;

    private Tile _cachedTile;
    private bool _cachedIsPlayable;
    private bool _cachedIsLocked;
    private bool _cachedIsCenterTile;

    void Awake() {
        _sr = GetComponent<SpriteRenderer>();
    }

    void Update() {
        if (ShouldUpdate()) {
            CheckCenterTile();
        }

        _cachedTile = tile;
        _cachedIsLocked = isLocked;
        _cachedIsPlayable = isPlayable;
        _cachedIsCenterTile = isCenterTile;
    }

    void CheckCenterTile() {
        if (isCenterTile) {
            _sr.sprite = centerTileSprite;
        }
    }

    bool ShouldUpdate() {
        return _cachedTile != tile ||
        _cachedIsLocked != isLocked ||
        _cachedIsPlayable != isPlayable ||
        _cachedIsCenterTile != isCenterTile;
    }

    public bool HasTile() {
        return this.tile != null;
    }

    public bool PlacedThisRound() {
        return HasTile() && !isLocked;
    }

    public bool IsBoardSpace {
        get {
            return !isRackSpace;
        }
    }

}
