using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

enum PlayDirection
{
    UNDECIDED,
    ACROSS,
    DOWN
}

[ExecuteAlways]
public class GameBoard : MonoBehaviour
{
    public int boardCount = 9;

    public GameObject tileSpacePrefab;
    public TileRack tileRack;
    public bool debugTilePosition = false;

    private TileSpace[,] tileSpaces;
    private TileSpace centerTileSpace;
    private Tile currentBadgeTile;

    private int _cachedBoardCount;
    private int _playerScore = 0;
    private int _turnScore = 0;

    private List<Vector2> _playableTiles;
    private List<Vector2> _tilesPlayed;

    private bool _invalidTilePlacement = false;
    private Tile _activatedTile;

    void Start()
    {
        _playableTiles = new List<Vector2>();
        _tilesPlayed = new List<Vector2>();

        if (Application.isPlaying)
        {
            SetScore(0);

            GameEvents.current.OnTilePlaced += OnTilePlaced;
            GameEvents.current.OnTileActivated += OnTileActivated;

            UIController.current.PlayButton.clicked += LockTiles;
            UIController.current.RecallButton.clicked += RecallTiles;
            UIController.current.ShuffleButton.clicked += tileRack.Shuffle;

            SetRecallState(false);
        }
    }

    void Update()
    {
        CheckBoardCount();
        CheckInput();

        if (_activatedTile && debugTilePosition)
        {
            GetClosestEmptyTileSpace(_activatedTile);
        }
    }

    void SetRecallState(bool enabled) {
        UIController.current.ShuffleButton.style.display = enabled ? DisplayStyle.None : DisplayStyle.Flex;
        UIController.current.RecallButton.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void CheckRecallState() {
        SetRecallState(GetTilesPlacedThisRound().Count > 0);
    }

    void SetScore(int score)
    {
        _playerScore = score;
        UIController.current.PlayerScore.text = score.ToString();
    }

    TileSpace GetClosestEmptyTileSpace(Tile tile)
    {
        TileSpace closestSpace = null;
        var pos = tile.transform.position;
        foreach (var space in tileSpaces)
        {
            if (space.tile != null && space.tile != tile) continue;
            if (!closestSpace)
            {
                closestSpace = space;
                continue;
            }
            var closestPos = closestSpace.transform.position;
            var closestDist = Mathf.Abs(Vector2.Distance(pos, closestPos));

            var spacePos = space.transform.position;
            var dist = Mathf.Abs(Vector2.Distance(pos, spacePos));

            if (dist < closestDist)
            {
                closestSpace = space;
            }
        }
        var closestSpacePos = closestSpace.transform.position;
        var tileDist = Mathf.Abs(Vector2.Distance(pos, closestSpacePos));

        if (debugTilePosition)
        {
            print($"Mouse Pos: {pos}, Space Pos: {closestSpacePos}, Tile: {closestSpace.tilePosition}, Dist: {tileDist}");
        }

        return tileDist < 1f ? closestSpace : null;
    }

    void CheckInput()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            LockTiles();
        }
    }

    public void LockTiles()
    {
        print("Play Button clicked!");
        if (_invalidTilePlacement)
        {
            print("Invalid tile placement... Cannot lock tiles!");
            return;
        }
        List<TileSpace> tileSpaces = GetTilesPlacedThisRound();
        tileSpaces.ForEach(tileSpace =>
        {
            tileSpace.isLocked = true;
            tileSpace.isPlayable = false;
        });
        currentBadgeTile.SetValidWordBadge(false, false);
        currentBadgeTile = null;

        SetScore(_playerScore + _turnScore);
        _turnScore = 0;

        tileRack.GenerateTiles();
        SetRecallState(false);
        print($"Locked tiles! Count: {tileSpaces.Count}, Score: {_playerScore}");
        SetPlayableTiles();
    }

    public void RecallTiles()
    {
        currentBadgeTile?.SetValidWordBadge(false, false);
        currentBadgeTile = null;
        SetRecallState(false);

        GetTilesPlacedThisRound().ForEach(space =>
        {
            Tile tile = space.tile;
            space.tile = null;
            tile.space = null;
            tileRack.SetTileInEmptySpace(tile);
        });

    }

    void OnTileActivated(Tile tile)
    {
        // Tile was placed in a previous turn, and is part of a played word.
        if (tile.IsLocked || !tile.space) return;

        if (currentBadgeTile && currentBadgeTile == tile)
        {
            currentBadgeTile.SetValidWordBadge(false, false);
            currentBadgeTile = null;
        }

        _activatedTile = tile;

        TileSpace space = tile.space;

        // Center tile removed on first turn
        if (tile.space && tile.space.isCenterTile)
        {
            currentBadgeTile?.SetValidWordBadge(true, false);
            ForEachTileHole(tileSpace =>
            {
                if (tileSpace.isPlayable)
                {
                    tileSpace.isPlayable = false;
                }
            });
            tile.space.isPlayable = true;
            tile.space.tile = null;
            tile.space = null;
            return;
        }

        if (tile.space && tile.space.tile)
        {
            print($"Removing tile {tile.letter} from {tile.space.tilePosition}");
            tile.space.tile = null;
        }

        tile.space = null;

        if (space && space.IsBoardSpace)
        {
            bool validTilePlacement = CheckPlacedTileWord(space);
            SetPlayableTiles(!validTilePlacement);
        }

    }

    public void OnTilePlaced(Tile tile)
    {
        TileSpace? currentSpace = GetClosestEmptyTileSpace(tile);

        _activatedTile = null;

        if (!currentSpace)
        {
            tileRack.SetTileInEmptySpace(tile);
            return;
        }

        if (currentSpace.tile == tile) return;

        if (currentSpace.tile)
        {
            var spaceSet = false;
            ForEachAdjacentTileSpace(currentSpace, PlayDirection.UNDECIDED, adjSpace =>
            {
                if (!adjSpace.tile && !spaceSet)
                {
                    currentSpace = adjSpace;
                    spaceSet = true;
                }
            });
        }

        tile.SetSpaceAndEase(currentSpace, tile.playedScale);
        currentSpace.tile = tile;

        print($"Tile '{tile.letter}' placed");

        bool validTilePlacement = CheckPlacedTileWord(currentSpace);
        SetPlayableTiles(!validTilePlacement);
        CheckRecallState();
    }

    bool CheckPlacedTileWord(TileSpace tileSpace)
    {
        if (currentBadgeTile)
        {
            currentBadgeTile.SetValidWordBadge(false, false);
        }

        _turnScore = 0;
        var tilePos = tileSpace.tilePosition;

        var horizontalRootSpace = FindWordRootTileSpace(tileSpace, PlayDirection.ACROSS);
        var verticalRootSpace = FindWordRootTileSpace(tileSpace, PlayDirection.DOWN);

        int MIN_WORD_LENGTH = 2;
        bool horizontalWordIsValid = true;
        bool verticalWordIsValid = true;
        var horizontalWord = "";
        var verticalWord = "";

        if (IsAdjacentToTile(horizontalRootSpace.tilePosition, PlayDirection.ACROSS))
        {
            horizontalWord += horizontalRootSpace.tile?.letter;
            var hTilePos = horizontalRootSpace.tilePosition;
            var newTilePos = new Vector2(hTilePos.x + 1, hTilePos.y);
            while (newTilePos.x < boardCount)
            {
                TileSpace rightTileSpace = GetTileSpaceByPos(newTilePos);
                if (!rightTileSpace.HasTile())
                {
                    break;
                }
                if (!horizontalRootSpace.tile)
                {
                    horizontalRootSpace = rightTileSpace;
                }
                horizontalWord += rightTileSpace.tile.letter;
                newTilePos.x++;
            }

            if (horizontalWord.Length > 1)
            {
                print($"Horizontal Word: {horizontalWord}");
            }
        }

        if (IsAdjacentToTile(verticalRootSpace.tilePosition, PlayDirection.DOWN))
        {
            verticalWord += verticalRootSpace.tile?.letter;
            var vTilePos = verticalRootSpace.tilePosition;
            var newTilePos = new Vector2(vTilePos.x, vTilePos.y + 1);
            while (newTilePos.y < boardCount)
            {
                TileSpace bottomTileSpace = GetTileSpaceByPos(newTilePos);
                if (!bottomTileSpace.HasTile())
                {
                    break;
                }
                if (!verticalRootSpace.tile)
                {
                    verticalRootSpace = bottomTileSpace;
                }
                verticalWord += bottomTileSpace.tile.letter;
                newTilePos.y++;
            }

            if (verticalWord.Length > 1)
            {
                print($"Vertical Word: {verticalWord}");
            }
        }

        // Account for first word placement over center
        if (verticalWord.Length < MIN_WORD_LENGTH && horizontalWord.Length < MIN_WORD_LENGTH)
        {
            if (horizontalRootSpace.tile)
            {
                currentBadgeTile = horizontalRootSpace.tile;
                currentBadgeTile.SetValidWordBadge(true, false);
            }
            return false;
        }

        if (horizontalWord.Length >= MIN_WORD_LENGTH)
        {
            horizontalWordIsValid = WordDictionary.WordIsValid(horizontalWord);
        }

        if (verticalWord.Length >= MIN_WORD_LENGTH)
        {
            verticalWordIsValid = WordDictionary.WordIsValid(verticalWord);
        }

        if (horizontalWord.Length >= verticalWord.Length)
        {
            currentBadgeTile = horizontalRootSpace.tile;
        }
        else
        {
            currentBadgeTile = verticalRootSpace.tile;
        }

        var tilePlacementIsValid = horizontalWordIsValid && verticalWordIsValid;
        currentBadgeTile.SetValidWordBadge(true, tilePlacementIsValid);

        var verticalWordScore = TileBag.GetPointsForWord(verticalWord);
        var horizontalWordScore = TileBag.GetPointsForWord(horizontalWord);

        _turnScore += verticalWordScore;
        _turnScore += horizontalWordScore;

        if (_turnScore > 0)
        {
            print($"Turn: {_turnScore}");
        }

        if (verticalWordIsValid)
        {
            print($"Vertical Word: {verticalWord} ({verticalWordScore})");
        }
        if (horizontalWordIsValid)
        {
            print($"Horizontal Word: {horizontalWord} ({horizontalWordScore})");
        }

        return tilePlacementIsValid;
    }

    TileSpace FindWordRootTileSpace(TileSpace tileSpace, PlayDirection playDirection)
    {
        var tilePos = tileSpace.tilePosition;

        if (playDirection == PlayDirection.ACROSS)
        {
            var newTilePos = new Vector2(tilePos.x - 1, tilePos.y);
            while (newTilePos.x >= 0)
            {
                TileSpace leftTile = GetTileSpaceByPos(newTilePos);
                if (!leftTile.HasTile())
                {
                    return GetTileSpaceByPos(newTilePos + Vector2.right);
                }
                newTilePos.x--;
            }
        }

        if (playDirection == PlayDirection.DOWN)
        {
            var newTilePos = new Vector2(tilePos.x, tilePos.y - 1);
            while (newTilePos.y >= 0)
            {
                TileSpace topTile = GetTileSpaceByPos(newTilePos);
                if (!topTile.HasTile())
                {
                    return GetTileSpaceByPos(newTilePos + Vector2.up);
                }
                newTilePos.y--;
            }
        }

        return tileSpace;
    }

    List<TileSpace> GetTilesPlacedThisRound()
    {
        List<TileSpace> tilesPlacedThisRound = new List<TileSpace>();

        ForEachTileHole(tileSpace =>
        {
            if (tileSpace.PlacedThisRound())
            {
                tilesPlacedThisRound.Add(tileSpace);
            }
        });

        return tilesPlacedThisRound;
    }

    // invalidTilePlacement is passed in as param from valid word check
    // should invalidate tiles if invalid word detected
    void SetPlayableTiles(bool invalidTilePlacement = false)
    {

        _invalidTilePlacement = false;
        List<TileSpace> tilesPlacedThisRound = new List<TileSpace>();
        PlayDirection playDirection = PlayDirection.UNDECIDED;
        int forcedColumn = -1;
        int forcedRow = -1;

        var numLockedTiles = 0;
        var tilesPlacedAdjacentToLocked = 0;

        ForEachTileHole(tileSpace =>
        {
            // Set to false and recompute (center tile should remain playable)
            tileSpace.isPlayable = tileSpace.isCenterTile;

            if (tileSpace.isLocked)
            {
                numLockedTiles++;
            }

            // Check if tile was placed this round
            if (tileSpace.PlacedThisRound() && !invalidTilePlacement)
            {
                tilesPlacedThisRound.Add(tileSpace);

                // if playing down, force tiles onto same column
                forcedColumn = (int)tileSpace.tilePosition.x;
                // if playing across, force tiles onto same row
                forcedRow = (int)tileSpace.tilePosition.y;

                var isAdjacentToLocked = false;
                ForEachAdjacentTileSpace(tileSpace, PlayDirection.UNDECIDED, adjHole =>
                {
                    if (isAdjacentToLocked) return;
                    isAdjacentToLocked = adjHole?.isLocked ?? false;
                });

                if (isAdjacentToLocked)
                {
                    tilesPlacedAdjacentToLocked++;
                }

                if (tilesPlacedThisRound.Count > 1)
                {
                    // Check if tiles placed are in adjacent positions
                    var _playDirection = GetRestrictedPlayingDirection(
                        tilesPlacedThisRound[tilesPlacedThisRound.Count - 2].tilePosition,
                        tilesPlacedThisRound[tilesPlacedThisRound.Count - 1].tilePosition
                    );

                    // playDirection not yet set. Set to calculated direction
                    if (playDirection == PlayDirection.UNDECIDED)
                    {
                        playDirection = _playDirection;
                        return;
                    }

                    // Invalidate all tiles if tiles are placed in more than one direction
                    invalidTilePlacement = _playDirection != playDirection;
                    return;
                }
            }
        });

        if (tilesPlacedAdjacentToLocked == 0 && numLockedTiles > 0)
        {
            invalidTilePlacement = true;
        }

        if (!invalidTilePlacement)
        {
            ForEachTileHole(tileSpace =>
            {
                if (tileSpace.isCenterTile || !tileSpace.HasTile() || tileSpace.isLocked) return;
                if (invalidTilePlacement) return;
                var pos = tileSpace.tilePosition;
                invalidTilePlacement = !IsAdjacentToTile(pos, playDirection);
            });
        }

        if (tilesPlacedThisRound.Count > 1 && !invalidTilePlacement)
        {
            // Ensure placed tiles are contiguous
            var placedTileIndex = 0;
            var tileStep = 1;
            while (placedTileIndex < tilesPlacedThisRound.Count - 1)
            {
                var placedTileHole = tilesPlacedThisRound[placedTileIndex];
                var pos = placedTileHole.tilePosition;
                TileSpace nextTileHole;
                if (playDirection == PlayDirection.ACROSS)
                {
                    nextTileHole = GetTileSpaceByPos(new Vector2(pos.x + tileStep, pos.y));
                }
                else
                {
                    nextTileHole = GetTileSpaceByPos(new Vector2(pos.x, pos.y + tileStep));
                }

                // Invalid placement! Empty tile between placed tiles
                if (!nextTileHole.HasTile())
                {
                    print("Found empty tile between placements!");
                    invalidTilePlacement = true;
                    break;
                }

                // Got to next placed tile. Increment placedTileIndex
                if (nextTileHole == tilesPlacedThisRound[placedTileIndex + 1])
                {
                    placedTileIndex++;
                    tileStep = 0;
                    continue;
                }

                tileStep++;
            }
        }

        if (!centerTileSpace.HasTile() || invalidTilePlacement)
        {
            _invalidTilePlacement = true;
            if (currentBadgeTile)
            {
                currentBadgeTile.SetValidWordBadge(true, false);
            }
            return;
        }

        ForEachTileHole(tileSpace =>
        {
            if (tileSpace.isCenterTile) return;
            var tilePosition = tileSpace.tilePosition;

            // isPlayable is based on tiles on the board
            var isAdjacent = IsAdjacentToTile(tilePosition, playDirection);

            if (playDirection == PlayDirection.ACROSS)
            {
                // Force tiles onto same row
                isAdjacent = isAdjacent && tilePosition.y == forcedRow;
            }
            if (playDirection == PlayDirection.DOWN)
            {
                // Force tiles onto same column
                isAdjacent = isAdjacent && tilePosition.x == forcedColumn;
            }
            if (isAdjacent && playDirection == PlayDirection.UNDECIDED && forcedColumn + forcedRow >= 0)
            {
                // Force tiles onto same column or row as placed tile
                isAdjacent = isAdjacent && (
                    tilePosition.x == forcedColumn ||
                    tilePosition.y == forcedRow
                );
            }

            tileSpace.isPlayable = isAdjacent;
        });
    }

    void ForEachAdjacentTileSpace(TileSpace tileSpace, PlayDirection playDirection, Action<TileSpace> adjTileHoleAction)
    {
        var pos = tileSpace.tilePosition;
        if (playDirection == PlayDirection.ACROSS || playDirection == PlayDirection.UNDECIDED)
        {
            var leftTileHole = GetTileSpaceByPos(new Vector2(pos.x - 1, pos.y));
            if (leftTileHole) adjTileHoleAction(leftTileHole);
            var rightTileHole = GetTileSpaceByPos(new Vector2(pos.x + 1, pos.y));
            if (rightTileHole) adjTileHoleAction(rightTileHole);
        }
        if (playDirection == PlayDirection.DOWN || playDirection == PlayDirection.UNDECIDED)
        {
            var bottomTileHole = GetTileSpaceByPos(new Vector2(pos.x, pos.y - 1));
            if (bottomTileHole) adjTileHoleAction(bottomTileHole);
            var topTileHole = GetTileSpaceByPos(new Vector2(pos.x, pos.y + 1));
            if (topTileHole) adjTileHoleAction(topTileHole);
        }
    }

    bool PositionsAreAdjacent(Vector2 pos1, Vector2 pos2, PlayDirection playDirection)
    {
        var xAdj = Mathf.Abs(pos2.x - pos1.x) == 1;
        var yAdj = Mathf.Abs(pos2.y - pos1.y) == 1;
        if (playDirection == PlayDirection.ACROSS) return xAdj;
        if (playDirection == PlayDirection.DOWN) return yAdj;
        return xAdj || yAdj;
    }

    bool IsAdjacentToTile(Vector2 pos, PlayDirection playDirection)
    {
        bool acrossIsAdjacent = false;
        bool downIsAdjacent = false;
        if (playDirection == PlayDirection.ACROSS || playDirection == PlayDirection.UNDECIDED)
        {
            acrossIsAdjacent = GetTileSpaceByPos(new Vector2(pos.x - 1, pos.y))?.HasTile() ?? false;
            if (!acrossIsAdjacent)
                acrossIsAdjacent = GetTileSpaceByPos(new Vector2(pos.x + 1, pos.y))?.HasTile() ?? false;
        }
        if (playDirection == PlayDirection.DOWN || playDirection == PlayDirection.UNDECIDED)
        {
            downIsAdjacent = GetTileSpaceByPos(new Vector2(pos.x, pos.y - 1))?.HasTile() ?? false;
            if (!downIsAdjacent)
                downIsAdjacent = GetTileSpaceByPos(new Vector2(pos.x, pos.y + 1))?.HasTile() ?? false;
        }

        return acrossIsAdjacent || downIsAdjacent;
    }

    PlayDirection GetRestrictedPlayingDirection(Vector2 pos1, Vector2 pos2)
    {
        return pos1.x - pos2.x == 0 ? PlayDirection.DOWN : PlayDirection.ACROSS;
    }

    bool IsTilePositionOutOfBounds(Vector2 pos)
    {
        return (pos.x < 0 || pos.x >= boardCount) || (pos.y < 0 || pos.y >= boardCount);
    }

    // Tile is a valid letter placement
    bool IsTilePositionPlayable(Vector2 pos)
    {
        return _playableTiles.Contains(pos);
    }

    void ForEachTileHole(Action<TileSpace> tileSpaceAction)
    {
        foreach (TileSpace tileSpace in tileSpaces)
        {
            tileSpaceAction(tileSpace);
        }
    }

    void CheckBoardCount()
    {
        if (boardCount != _cachedBoardCount)
        {
            GenerateTileSpaces();
        }

        _cachedBoardCount = boardCount;
    }

    TileSpace GetTileSpaceByPos(Vector2 pos)
    {
        if (IsTilePositionOutOfBounds(pos)) return null;
        return tileSpaces[(int)pos.x, (int)pos.y];
    }

    void GenerateTileSpaces()
    {
        print($"GenerateTileSpaces: boardCount {boardCount}");

        transform.localScale = Vector3.one;
        var cameraPos = Camera.main.transform.position;
        cameraPos.z = 1f;
        transform.position = cameraPos;

        DestroyTileSpaces();

        if (boardCount <= 0 || boardCount <= 0) return;

        var centerPos = GetCenterPosition();

        var halfBoardCount = boardCount / 2;
        Vector2 startPos = new Vector2(-halfBoardCount, halfBoardCount);
        tileSpaces = new TileSpace[boardCount, boardCount];

        int tilesCreated = 0;
        var startX = startPos.x;
        for (int row = 0; row < boardCount; row++)
        {
            startPos.x = startX;
            for (int col = 0; col < boardCount; col++)
            {
                tilesCreated++;
                GameObject tileSpaceObj = Instantiate(tileSpacePrefab);
                // Keep track of tiles in position vector space, unity space Y will be negative
                // e.g. (3,3) tile => (3,-3) unity pos
                tileSpaceObj.transform.SetParent(transform.Find("TileSpaces"));
                tileSpaceObj.transform.localPosition = startPos;
                TileSpace tileSpace = tileSpaceObj.GetComponent<TileSpace>();
                tileSpace.tilePosition = new Vector2(col, row);

                if (tileSpace.tilePosition == (centerPos - Vector2.one))
                {
                    tileSpace.isCenterTile = true;
                    tileSpace.isPlayable = true;
                    centerTileSpace = tileSpace;
                }

                tileSpaces[col, row] = tileSpace;
                startPos.x += 1;
            }
            startPos.y -= 1;
        }

        print($"Tiles created: {tilesCreated}");
        SetPlayableTiles();
        SetBoardSize();
    }

    void SetBoardSize()
    {
        float height = Camera.main.orthographicSize * 2f;
        float width = height * Screen.width / Screen.height;

        float ratio = width / boardCount;

        transform.localScale = new Vector3(ratio, ratio, 1f);
    }

    void DestroyTileSpaces()
    {
        var tileSpacesParent = transform.Find("TileSpaces");
        while (tileSpacesParent.childCount > 0)
        {
            DestroyImmediate(tileSpacesParent.GetChild(0).gameObject);
        }
    }

    Vector2 GetCenterPosition()
    {
        var centerRow = Mathf.Ceil(boardCount / 2f);
        var centerCol = Mathf.Ceil(boardCount / 2f);
        return new Vector2(centerCol, centerRow);
    }

}
