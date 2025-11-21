using System.Collections.Generic;
using UnityEngine;

namespace TempleRun
{
    /// <summary>
    /// 3D Infinite Tile generation similar to Temple Run. A certain...rn tile. Once the player turns new tiles are added to the scene.
    ///
    /// I recommend implementing an Object Pool to avoid instantiating/destroying the tiles over and over.
    /// Design Pattern: Object Pooling in Unity
    /// https://youtu.be/odYlL8aUinY
    /// </summary>
    public class TileSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("How many straight tiles to spawn at the beginning.")]
        private int tileStartCount = 10;
        [SerializeField, Tooltip("The minimum amount of straight tiles to spawn before another turn tile.")]
        private int minimumStraightTiles = 3;
        [SerializeField, Tooltip("The maximum amount of straight tiles to spawn before another turn tile.")]
        private int maximumStraightTiles = 15;
        [SerializeField, Tooltip("The prefab representing a straight tile.")]
        private GameObject[] startingTiles;                        // ⬅ เปลี่ยนเป็น Array
        [SerializeField, Tooltip("A list of the prefabs representing turn tiles.")]
        private List<GameObject> turnTiles;
        [SerializeField, Tooltip("A list of the prefabs representing obstacles.")]
        private List<GameObject> obstacles;

        #region Private Variables

        private Vector3 currentTileLocation = Vector3.zero;
        private Vector3 currentTileDirection = Vector3.forward;
        private GameObject prevTile;

        private List<GameObject> currentTiles;
        private List<GameObject> currentObstacles;

        #endregion Private Variables

        private void Start()
        {
            currentTiles = new List<GameObject>();
            currentObstacles = new List<GameObject>();

            // Initialize Unity's Random.
            Random.InitState(System.DateTime.Now.Millisecond);

            // Spawn the initial straight tiles without an obstacle.
            for (int i = 0; i < tileStartCount; ++i)
            {
                Tile straight = GetRandomStraightTile();           // ⬅ สุ่มจาก startingTiles
                if (straight != null) { SpawnTile(straight); }
            }

            // Spawn the initial turn tile.
            SpawnTile(SelectRandomGameObjectFromList(turnTiles).GetComponent<Tile>());
        }

        /// <summary>
        /// Spawn a tile at the next location. Spawns an obstacle if spawnObstacle is true.
        /// </summary>
        /// <param name="tile">The current tile to spawn.</param>
        /// <param name="spawnObstacle">Whether this tile should spawn an obstacle.</param>
        private void SpawnTile(Tile tile, bool spawnObstacle = false)
        {
            // Rotate and place the next tile in the correct location.
            Quaternion newTileRotation = tile.gameObject.transform.rotation * Quaternion.LookRotation(currentTileDirection, Vector3.up);
            prevTile = Instantiate(tile.gameObject, currentTileLocation, newTileRotation);
            currentTiles.Add(prevTile);

            // Whether this tile should spawn an obstacle.
            if (spawnObstacle)
                SpawnObstacle();

            // Offset to spawn the next straight tile.
            if (tile.type == TileType.STRAIGHT)
            {
                currentTileLocation += Vector3.Scale(prevTile.GetComponent<Renderer>().bounds.size, currentTileDirection);
            }
        }

        /// <summary>
        /// When the player turns, delete the previous tiles and obstacles to avoid clashing with them.
        /// </summary>
        private void DeletePreviousTiles()
        {
            while (currentTiles.Count != 1)
            {
                GameObject tile = currentTiles[0];
                currentTiles.RemoveAt(0);
                Destroy(tile);
            }

            while (currentObstacles.Count != 0)
            {
                GameObject obstacle = currentObstacles[0];
                currentObstacles.RemoveAt(0);
                Destroy(obstacle);
            }
        }

        /// <summary>
        /// Called when the player successfully turns in a new direction.
        /// </summary>
        /// <param name="direction">The new direction the player is turning in.</param>
        public void AddNewDirection(Vector3 direction)
        {
            // Set the new tile direction.
            currentTileDirection = direction;
            // Delete the previous tiles.
            DeletePreviousTiles();

            // Straight tiles have a length of ten. Find half of the length of the current turn tile, then add it to the straight tile length.
            Vector3 tilePlacementScale;
            Tile prevTileComponent = prevTile.GetComponent<Tile>();

            if (prevTileComponent.type == TileType.SIDEWAYS)
            {
                tilePlacementScale = Vector3.Scale(prevTile.GetComponent<Renderer>().bounds.size / 2 + (Vector3.one * 10 / 2), currentTileDirection);
            }
            else
            {
                // Left or right tiles
                tilePlacementScale = Vector3.Scale((prevTile.GetComponent<Renderer>().bounds.size - (Vector3.one * 2)) + (Vector3.one * 10 / 2), currentTileDirection);
            }

            // Add in the offset.
            currentTileLocation += tilePlacementScale;

            // Spawn a random number of straight tiles.
            int currentPathLength = Random.Range(minimumStraightTiles, maximumStraightTiles);
            for (int i = 0; i < currentPathLength; ++i)
            {
                Tile straight = GetRandomStraightTile();           // ⬅ ตรงนี้ก็สุ่มจาก startingTiles
                if (straight != null)
                {
                    SpawnTile(straight, (i == 0) ? false : true);  // tile แรกหลังเลี้ยวไม่ spawn obstacle
                }
            }

            // Spawn a random turn tile after the series of straight tiles.
            SpawnTile(SelectRandomGameObjectFromList(turnTiles).GetComponent<Tile>(), false);
        }

        /// <summary>
        /// Randomly spawns an obstacle on the tile.
        /// </summary>
        private void SpawnObstacle()
        {
            // 60% chance of spawning an obstacle.
            if (Random.value > 0.4f) return;

            GameObject obstaclePrefab = SelectRandomGameObjectFromList(obstacles);
            if (obstaclePrefab == null) return;

            Quaternion newObjectRotation = obstaclePrefab.gameObject.transform.rotation * Quaternion.LookRotation(currentTileDirection, Vector3.up);
            GameObject obstacle = Instantiate(obstaclePrefab, currentTileLocation, newObjectRotation);
            currentObstacles.Add(obstacle);
        }

        /// <summary>
        /// Returns a random straight Tile from the startingTiles array.
        /// </summary>
        private Tile GetRandomStraightTile()
        {
            if (startingTiles == null || startingTiles.Length == 0) return null;
            GameObject go = startingTiles[Random.Range(0, startingTiles.Length)];
            if (go == null) return null;
            return go.GetComponent<Tile>();
        }

        /// <summary>
        /// Helper function to randomly select an object from the provided list.
        /// </summary>
        /// <param name="list">The list to randomly select an object from.</param>
        /// <returns>The randomly selected object from the list.</returns>
        private GameObject SelectRandomGameObjectFromList(List<GameObject> list)
        {
            if (list.Count == 0) return null;
            return list[Random.Range(0, list.Count)];
        }
    }

}
