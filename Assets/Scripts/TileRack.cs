using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TileRack : MonoBehaviour
{
    public GameObject tilePrefab;
    public GameObject rackTileSpacePrefab;
    public int tileCount = 7;

    public float shuffleTransitionTime = 0.4f;
    public float shuffleMaxJumpHeight = 2f;
    public float shuffleMinJumpHeight = 0.5f;

    // var jumpHeights = new float[] {0.9f, 1.5f, 1f, 2f, 0.9f, 1f, 2f};
    // var jumpDelays = new float[] { 0f, 0.1f, 0.15f, 0.2f, 0.25f };

    public float[] shuffleJumpHeights = new float[] { 0.6f, 1.4f, 2f };
    public float[] shuffleTransitionTimes = new float[] { 0.4f, 0.45f, 0.5f, 0.55f };
    public float[] shuffleTransitionDelays = new float[] { 0f, 0.1f, 0.15f, 0.2f, 0.25f };

    private int _cachedTileCount;

    private TileSpace[] _spaces = new TileSpace[7];
    private bool _isFirstRack = true;

    public int isolatedTileIndex = 0;
    private int _cachedIsolatedTileIndex;

    public GameObject debugMarker1;
    public GameObject debugMarker2;
    public GameObject debugMarker3;

    void Start() {
        _cachedTileCount = tileCount;
        GenerateTileSpaces();

        if (Application.isPlaying) {
            GenerateTiles();
        }
    }

    void Update() {
        if (_cachedTileCount != tileCount) {
            GenerateTileSpaces();

            if (Application.isPlaying) {
                GenerateTiles();
            }
        }

        if (isolatedTileIndex != _cachedIsolatedTileIndex) {
            for (int i = 0; i < _spaces.Length; i++) {
                if (!_spaces[i].tile) return;
                _spaces[i].tile.SetVisibility(i == isolatedTileIndex || isolatedTileIndex < 0);
                _spaces[i].tile.debugShuffleMovement = i == isolatedTileIndex;
            }
        }

        _cachedIsolatedTileIndex = isolatedTileIndex;
        _cachedTileCount = tileCount;
    }

    public void Shuffle() {
        bool hasStationaryTile = false;

        var newTileOrder = new Tile[_spaces.Length];
        var newJumpHeights = new float[_spaces.Length];

        for (int i = 0; i < _spaces.Length; i++) {
            var newIndex = Random.Range(0, _spaces.Length);

            // Generate random index until that index hasn't been taken
            // Only one tile is allowed to remain in the same position
            while (newTileOrder[newIndex] || ((hasStationaryTile && newIndex == i) && i < _spaces.Length-1)) {
                newIndex = Random.Range(0, _spaces.Length);
            }

            var tile = _spaces[i].tile;
            var newSpace = _spaces[newIndex];

            tile.SetSortingOrder(newIndex + 5);

            hasStationaryTile = newIndex == i;
            newTileOrder[newIndex] = tile;
        }

        for (int i = 0; i < newTileOrder.Length; i++) {
            var newSpace = _spaces[i];
            var tile = newTileOrder[i];

            // var varianceSign = Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
            // var heightVariance = Random.Range(0.15f, 0.5f) * varianceSign;

            // var deltaIndex = Mathf.Abs(tile.space.rackIndex - newSpace.rackIndex);
            // var jumpHeight = shuffleMaxJumpHeight * ((deltaIndex + 1) / (float)_spaces.Length);
            // jumpHeight = Mathf.Clamp(jumpHeight, shuffleMinJumpHeight, shuffleMaxJumpHeight);
            // jumpHeight += heightVariance;

            var delayIndex = Random.Range(0, shuffleTransitionDelays.Length);
            var transitionIndex = Random.Range(0, shuffleTransitionTimes.Length);
            var jumpHeightIndex = Random.Range(0, shuffleJumpHeights.Length);

            Vector3 currentPos = tile.space.transform.position;
            Vector3 targetPos = newSpace.transform.position;

            float xDist = currentPos.x - targetPos.x;

            float xArchPoint = targetPos.x + (xDist / 2f);
            float yArchPoint = currentPos.y + shuffleJumpHeights[jumpHeightIndex];
            Vector3 archPos = new Vector3(xArchPoint, yArchPoint, 1f);

            newSpace.tile = tile;

            if (tile.debugShuffleMovement) {
                debugMarker1.transform.position = currentPos;
                debugMarker2.transform.position = archPos;
                debugMarker3.transform.position = targetPos;
            }

            tile.CurveBetweenPoints(
                shuffleTransitionTimes[transitionIndex],
                shuffleTransitionDelays[delayIndex],
                currentPos,
                archPos,
                targetPos,
                () => {
                    tile.shuffleSpace = null;
                    tile.space = newSpace;
                    tile.SetSortingOrder(2);
                }
            );

            tile.shuffleSpace = newSpace;

            // newTileOrder[i].JumpToRackSpace(
            //     _spaces[i],
            //     shuffleJumpHeights[jumpHeightIndex],
            //     shuffleTransitionTimes[transitionIndex],
            //     shuffleTransitionDelays[delayIndex]
            // );
        }

    }

    void GenerateTileSpaces() {
        DestroyTileSpaces();

        float screenHeight = Camera.main.orthographicSize * 2f;
        float screenWidth = screenHeight * Screen.width / Screen.height;
        float tileWidth = screenWidth / _spaces.Length;

        var halfRackCount = _spaces.Length / 2;
        var rackPos = transform.position;
        Vector2 startPos = new Vector2(-(tileWidth * halfRackCount), rackPos.y);

        for (int i = 0; i < _spaces.Length; i++) {
            var rackTileSpace = Instantiate(rackTileSpacePrefab);
            rackTileSpace.transform.SetParent(transform);
            rackTileSpace.transform.localScale = new Vector3(tileWidth, tileWidth, 1);
            rackTileSpace.transform.localPosition = new Vector3(startPos.x, 0, 1);

            var tileSpace = rackTileSpace.GetComponent<TileSpace>();
            tileSpace.isRackSpace = true;
            tileSpace.rackIndex = i;
            _spaces[i] = tileSpace;

            startPos.x += tileWidth;
        }

        var camPos = Camera.main.transform.position;
        rackPos.x = camPos.x;
        transform.position = rackPos;
    }

    void DestroyTileSpaces() {
        while (transform.childCount > 0) {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        _spaces = new TileSpace[tileCount];
    }

    public void SetTileInEmptySpace(Tile tile) {
        TileSpace closestSpace = null;
        var tilePos = tile.transform.position;
        foreach (var space in _spaces) {
            if (space.tile != null && space.tile != tile) continue;
            if (!closestSpace) {
                closestSpace = space;
                continue;
            }
            var closestPos = closestSpace.transform.position;
            var closestDist = Mathf.Abs(Vector3.Distance(tilePos, closestPos));

            var spacePos = space.transform.position;
            var dist = Mathf.Abs(Vector3.Distance(tilePos, spacePos));

            if (dist < closestDist) {
                closestSpace = space;
            }
        }
        tile.SetSpaceAndEase(closestSpace, tile.defaultScale);
        closestSpace.tile = tile;
    }

    public void GenerateTiles() {
        var vowelCount = _isFirstRack ? 2 : 0;
        for (int i = 0; i < _spaces.Length; i++) {
            var space = _spaces[i];
            // Don't generate tiles for rack spaces that have tiles
            if (space.tile != null) continue;

            var iterVowelCount = 0;
            if (vowelCount > 0) {
                iterVowelCount = 1;
                vowelCount--;
            }
            char[] letters = TileBag.GetRandomLetters(1, iterVowelCount);

            var tileObj = Instantiate(tilePrefab);
            var tile = tileObj.GetComponent<Tile>();
            tile.letter = letters[0];
            tileObj.transform.localScale = space.transform.localScale;
            tile.defaultScale = space.transform.localScale.x;

            tile.space = space;
            space.tile = tile;

            tileObj.transform.position = space.transform.position;
        }
    }
}
