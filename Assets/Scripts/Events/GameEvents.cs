using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEvents : MonoBehaviour
{
    public static GameEvents current;

    void Awake() {
        current = this;
    }

    public event Action<Tile> OnTilePlaced;
    public event Action<Tile> OnTileActivated;

    public void PlaceTile(Tile tile) {
        if (OnTilePlaced != null) {
            OnTilePlaced(tile);
        }
    }

    public void ActivateTile(Tile tile) {
        if (OnTileActivated != null) {
            OnTileActivated(tile);
        }
    }
}
