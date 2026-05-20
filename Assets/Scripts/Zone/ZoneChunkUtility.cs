using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ZoneChunkUtility
{
    public static Vector2Int WorldToChunkXZ(Vector3 worldPosition, Vector3 gridOrigin, float chunkSizeWorld)
    {
        if(chunkSizeWorld <= 0f)
        {
            return Vector2Int.zero;
        }

        float inv = 1f / chunkSizeWorld;
        float x = (worldPosition.x - gridOrigin.x) * inv;
        float z = (worldPosition.z - gridOrigin.z) * inv;

        int ix = Mathf.FloorToInt(x);
        int iz = Mathf.FloorToInt(z);

        return new Vector2Int(ix, iz);
    }

    //Chebyshev Distance between cells (square of side 2r+1 around the player chunk)
    public static int ChebyshevDistanceChunks(Vector2Int a, Vector2Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }
}
