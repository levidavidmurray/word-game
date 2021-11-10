using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileRack : MonoBehaviour
{
    public GameObject tilePrefab;

    private TileSpace[] _spaces = new TileSpace[7];

    void Start() {
        var i = 0;
        foreach(Transform child in transform) {
            _spaces[i] = child.GetComponent<TileSpace>();
            i++;
        }

        GenerateTiles();
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
        tile.SetSpaceAndEase(closestSpace);
        closestSpace.tile = tile;
    }

    public void GenerateTiles() {
        for (int i = 0; i < _spaces.Length; i++) {
            var space = _spaces[i];
            // Don't generate tiles for rack spaces that have tiles
            if (space.tile != null) continue;

            char[] letters = TileBag.GetRandomLetters(1);

            var tileObj = Instantiate(tilePrefab);
            var tile = tileObj.GetComponent<Tile>();
            tile.letter = letters[0];

            tile.space = space;
            space.tile = tile;

            tileObj.transform.position = space.transform.position;
        }
    }
}
