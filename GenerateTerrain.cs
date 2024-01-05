using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Diagnostics;

public class GenerateTerrain : MonoBehaviour
{
    // Reference to prefabs 
    public GameObject fourway;
    public GameObject land;
    public GameObject roadend;
    public GameObject roadstraight;
    public GameObject troad;
    public GameObject turnroad;

    // Array to store prefabs
    public GameObject[] prefabs;
 
    // Grid dimensions and starting position
    public int gridSizeX = 10;
    public int gridSizeZ = 10;
    public int startX = 0;
    public int startZ = 0;

    // Array to store instantiated tiles
    private GameObject[,] terrainGrid;

    // Dictionary for storing prefab open/closed arrays (all represented as integers)
    private Dictionary<string, int[]> terrainRestrictions = new Dictionary<string, int[]>();

    // Dictionary for storing possible prefabs and orientations for each position
    private Dictionary<Vector2Int, Dictionary<GameObject, List<int>>> possiblePrefabs = new Dictionary<Vector2Int, Dictionary<GameObject, List<int>>>();

    // Lists for storing positions and entropies
    private List<Vector2Int> positions = new List<Vector2Int>();
    private List<int> entropies = new List<int>();

    // Counter for backtracked tiles and attempts
    private int backtrackedTiles = 0;

    // Constants
    private const int CLOSED = -1;
    private const int INVALID = -2;
    private const int MAX_ENTROPY = 1000;

    void Start()
    {
        // Initialize the prefabs array
        prefabs = new GameObject[] { /*fourway, land, roadend, roadstraight, troad,*/ turnroad };

        // Initialize the terrain restrictions dictionary
        terrainRestrictions.Add(fourway.name, new int[] { 0, 0, 0, 0 }); // first value is terrain type, rest are allowed terrain types neighboring each side
        terrainRestrictions.Add(land.name, new int[] { CLOSED, CLOSED, CLOSED, CLOSED }); // 1 = land, -1 = closed
        terrainRestrictions.Add(roadend.name, new int[] { 0, CLOSED, CLOSED, CLOSED }); // 2nd value is 0 because it is open road, rest are closed
        terrainRestrictions.Add(roadstraight.name, new int[] { CLOSED, 0, CLOSED, 0 }); // 2nd and 4th values are 0 because they are open road, rest are closed
        terrainRestrictions.Add(troad.name, new int[] { 0, 0, 0, CLOSED }); // 2nd, 3rd, and 4th values are 0 because they are open road, last value is closed
        terrainRestrictions.Add(turnroad.name, new int[] { 0, CLOSED, CLOSED, 0 }); // 2nd and 5th values are 0 because they are open road, rest are closed

        // Initialize the possible prefabs dictionary
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                possiblePrefabs.Add(new Vector2Int(x, z), new Dictionary<GameObject, List<int>>());

                // Add all prefabs to the dictionary
                for (int i = 0; i < prefabs.Length; i++)
                {
                    possiblePrefabs[new Vector2Int(x, z)].Add(prefabs[i], new List<int>());
                }
            }
        }

        // Initialize the array
        terrainGrid = new GameObject[gridSizeX, gridSizeZ];

        // Fill edge tiles with land tiles
        for (int x = 0; x < gridSizeX; x++)
        {
            // Instantiate the land tile facing north
            terrainGrid[x, 0] = Instantiate(land, new Vector3(startX + x, 0, startZ), Quaternion.Euler(-90, 0, 0));

            // Instantiate the land tile facing south
            terrainGrid[x, gridSizeZ - 1] = Instantiate(land, new Vector3(startX + x, 0, startZ + gridSizeZ - 1), Quaternion.Euler(-90, 0, 0));
        }

        for (int z = 0; z < gridSizeZ; z++)
        {
            // Ignore the corners
            if (z == 0 || z == gridSizeZ - 1) continue;

            // Instantiate the land tile facing east
            terrainGrid[0, z] = Instantiate(land, new Vector3(startX, 0, startZ + z), Quaternion.Euler(-90, 0, 0));

            // Instantiate the land tile facing west
            terrainGrid[gridSizeX - 1, z] = Instantiate(land, new Vector3(startX + gridSizeX - 1, 0, startZ + z), Quaternion.Euler(-90, 0, 0));
        }
        
        // Generate ordered list of positions from lowest to highest entropy
        GenerateOrderedPositions();

        UnityEngine.Debug.Log("Ordered positions: " + positions.Count);

        // Generate the terrain grid
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        GenerateTerrainGrid();
        stopwatch.Stop();

        // Print the time taken to generate the terrain grid
        UnityEngine.Debug.Log("Time taken to generate terrain grid: " + stopwatch.ElapsedMilliseconds + "ms");

        // Print the number of backtracked tiles
        UnityEngine.Debug.Log("Backtracked tiles: " + backtrackedTiles);
    }

    // Generate the terrain grid with wave function collapse and backtracking
    void GenerateTerrainGrid()
    {
        // Get the lowest entropy position
        Vector2Int lowestEntropyPosition = positions[positions.Count - 1];
        int lowestEntropy = entropies[entropies.Count - 1];

        // If lowest entropy is 0, return
        if (lowestEntropy == 0) return;

        // Get the possible prefabs for the lowest entropy position
        Dictionary<GameObject, List<int>> possiblePrefabsForPosition = possiblePrefabs[lowestEntropyPosition];

        // Create a list of prefabs that can be placed at the position based on which prefabs have orientations
        List<GameObject> possiblePrefabsList = new List<GameObject>();
        foreach (KeyValuePair<GameObject, List<int>> possiblePrefab in possiblePrefabsForPosition)
        {
            if (possiblePrefab.Value.Count > 0) possiblePrefabsList.Add(possiblePrefab.Key);
        }

        // Randomize the list of prefabs
        possiblePrefabsList = possiblePrefabsList.OrderBy(prefab => Random.value).ToList();

        // Check all prefabs that can be placed at the position
        for (int i = 0; i < possiblePrefabsList.Count; i++)
        {
            // Get the prefab
            GameObject prefab = possiblePrefabsList[i];

            // Get the possible orientations for the prefab
            List<int> possibleOrientations = possiblePrefabsForPosition[prefab];

            // Randomize the list of orientations
            possibleOrientations = possibleOrientations.OrderBy(orientation => Random.value).ToList();

            // Check all orientations that can be placed at the position
            for (int j = 0; j < possibleOrientations.Count; j++)
            {
                // Get the orientation
                int orientation = possibleOrientations[j];

                // Instantiate the prefab at the position with the orientation
                GameObject tile = Instantiate(prefab, new Vector3(startX + lowestEntropyPosition.x, 0, startZ + lowestEntropyPosition.y), Quaternion.Euler(-90, orientation, 0));

                // Add the tile to the terrain grid
                terrainGrid[lowestEntropyPosition.x, lowestEntropyPosition.y] = tile;

                // If the terrain grid is complete, return
                if (IsTerrainGridComplete()) return;

                // If the terrain grid is not complete, make entropy of this position max value
                entropies[positions.Count - 1] = MAX_ENTROPY;

                // Update the ordered positions
                UpdateOrderedPositions(lowestEntropyPosition.x, lowestEntropyPosition.y);

                // Generate the terrain grid
                GenerateTerrainGrid();

                // If the terrain grid is complete, return
                if (IsTerrainGridComplete()) return;

                // If the terrain grid is not complete, destroy the tile
                Destroy(tile);

                // Remove the tile from the terrain grid
                terrainGrid[lowestEntropyPosition.x, lowestEntropyPosition.y] = null;

                // Set the entropy of this position back to the lowest entropy
                entropies[positions.Count - 1] = lowestEntropy;

                // Increment the number of backtracked tiles
                backtrackedTiles++;
            }
        }
    }

    // Check if the terrain grid is complete
    bool IsTerrainGridComplete()
    {
        // Check all positions
        for (int x = 1; x < gridSizeX-1; x++)
        {
            for (int z = 1; z < gridSizeZ-1; z++)
            {
                // If there is no tile at the position, return false
                if (terrainGrid[x, z] == null) return false;
            }
        }

        return true;
    }

    // Return updated open/closed array based on rotation angle
    int[] RotateArray(int[] array, int angle)
    {
        // Create a new array to store the rotated array
        int[] rotatedArray = new int[4];

        // Rotate the array based on the angle
        // 0 = north, 1 = east, 2 = south, 3 = west
        if (angle == 90)
        {
            rotatedArray[0] = array[3];
            rotatedArray[1] = array[0];
            rotatedArray[2] = array[1];
            rotatedArray[3] = array[2];
        }
        else if (angle == 180)
        {
            rotatedArray[0] = array[2];
            rotatedArray[1] = array[3];
            rotatedArray[2] = array[0];
            rotatedArray[3] = array[1];
        }
        else if (angle == 270)
        {
            rotatedArray[0] = array[1];
            rotatedArray[1] = array[2];
            rotatedArray[2] = array[3];
            rotatedArray[3] = array[0];
        }
        else
        {
            rotatedArray[0] = array[0];
            rotatedArray[1] = array[1];
            rotatedArray[2] = array[2];
            rotatedArray[3] = array[3];
        }

        return rotatedArray;
    }

    // Boolean to determine if tile can be placed at position given the open/closed arrays of the tile and the surrounding tiles and rotation
    bool CanPlaceTile(GameObject tile, int x, int z, int angle)
    {
        // Get the open/closed array for the tile (Take out "(Clone)" from the name)
        int[] tileArray = terrainRestrictions[tile.name.Replace("(Clone)", "")];

        // Get the open/closed array for the surrounding tiles
        int east = x - 1, west = x + 1, north = z - 1, south = z + 1;

        // If all surrounding tiles are empty, return true
        if (terrainGrid[x, north] == null && terrainGrid[east, z] == null && terrainGrid[x, south] == null && terrainGrid[west, z] == null) return true;

        System.Span<int> northArray = stackalloc int[4] { INVALID, INVALID, INVALID, INVALID };
        System.Span<int> eastArray = stackalloc int[4] { INVALID, INVALID, INVALID, INVALID };
        System.Span<int> southArray = stackalloc int[4] { INVALID, INVALID, INVALID, INVALID };
        System.Span<int> westArray = stackalloc int[4] { INVALID, INVALID, INVALID, INVALID };

        // Rotate the surrounding tile arrays based on angle of that tile (only if there is a tile there)
        if (terrainGrid[x, north] != null) northArray = RotateArray(terrainRestrictions[terrainGrid[x, north].name.Replace("(Clone)", "")], (int)terrainGrid[x, north].transform.eulerAngles.y);
        if (terrainGrid[east, z] != null) eastArray = RotateArray(terrainRestrictions[terrainGrid[east, z].name.Replace("(Clone)", "")], (int)terrainGrid[east, z].transform.eulerAngles.y);
        if (terrainGrid[x, south] != null) southArray = RotateArray(terrainRestrictions[terrainGrid[x, south].name.Replace("(Clone)", "")], (int)terrainGrid[x, south].transform.eulerAngles.y);
        if (terrainGrid[west, z] != null) westArray = RotateArray(terrainRestrictions[terrainGrid[west, z].name.Replace("(Clone)", "")], (int)terrainGrid[west, z].transform.eulerAngles.y);

        // Rotate the tile array based on the angle
        tileArray = RotateArray(tileArray, angle);

        // Check if the tile can be placed
        if (tileArray[0] != northArray[2] && northArray[2] != INVALID) return false;
        if (tileArray[1] != eastArray[3] && eastArray[3] != INVALID) return false;
        if (tileArray[2] != southArray[0] && southArray[0] != INVALID) return false;
        if (tileArray[3] != westArray[1] && westArray[1] != INVALID) return false;

        return true;
    }

    // Get number of possible orientations for a tile in a given position
    int GetOrientations(Dictionary<GameObject, List<int>> possiblePrefabs, GameObject tile, int x, int z)
    {
        int orientations = 0;

        // Clear the list of orientations
        possiblePrefabs[tile].Clear();

        // Check all 4 rotations
        for (int angle = 0; angle < 360; angle += 90)
        {
            if (CanPlaceTile(tile, x, z, angle)) 
            {
                // Add the orientation to the list
                possiblePrefabs[tile].Add(angle);
                
                // Increment the number of orientations
                orientations++;
            }
        }

        return orientations;
    }

    // Get entropy for a position
    int GetEntropy(Dictionary<GameObject, List<int>> possiblePrefabs, int x, int z)
    {
        int entropy = 0;

        // Check all prefabs
        for (int i = 0; i < prefabs.Length; i++)
        {
            entropy += GetOrientations(possiblePrefabs, prefabs[i], x, z);
        }

        return entropy;
    }

    // Update ordered positions, entropies, and possible prefabs if a tile is placed at a position
    void UpdateOrderedPositions(int x, int z)
    {
        // Check neighboring positions
        for (int i = 0; i < 4; i++)
        {
            // Get the neighboring position
            Vector2Int neighborPosition = new Vector2Int(x, z);
            if (i == 0) neighborPosition.y -= 1;
            else if (i == 1) neighborPosition.x += 1;
            else if (i == 2) neighborPosition.y += 1;
            else if (i == 3) neighborPosition.x -= 1;

            // If position is edge or filled, continue
            if (neighborPosition.x == 0 || neighborPosition.x == gridSizeX - 1 || neighborPosition.y == 0 || neighborPosition.y == gridSizeZ - 1 || terrainGrid[neighborPosition.x, neighborPosition.y] != null) continue;

            // Update the entropy for the neighboring position
            int entropy = GetEntropy(possiblePrefabs[neighborPosition], neighborPosition.x, neighborPosition.y);

            // Find the position in positions
            int index = positions.FindIndex(position => position == neighborPosition);

            // Update the entropy for the position
            entropies[index] = entropy;
        }

        // Sort the positions based on entropy
        var sortedSequence = positions.Zip(entropies, (position, entropy) => new { Position = position, Entropy = entropy })
                                    .OrderByDescending(item => item.Entropy)
                                    .ToList();

        positions = sortedSequence.Select(item => item.Position).ToList();
        entropies = sortedSequence.Select(item => item.Entropy).ToList();
    }

    // Generate ordered list of positions from lowest to highest entropy
    void GenerateOrderedPositions()
    {
        // Check all positions
        for (int x = 1; x < gridSizeX-1; x++)
        {
            for (int z = 1; z < gridSizeZ-1; z++)
            {
                // If there is no tile at the position
                if (terrainGrid[x, z] == null)
                {
                    // If tile is surrounded by empty tiles, entropy is max value
                    int entropy = 0;
                    if (terrainGrid[x, z - 1] == null && terrainGrid[x + 1, z] == null && terrainGrid[x, z + 1] == null && terrainGrid[x - 1, z] == null)
                    {
                        // Get the entropy for the position
                        entropy = prefabs.Length * 4;

                        // Add all orientations to the list
                        for (int i = 0; i < prefabs.Length; i++)
                        {
                            possiblePrefabs[new Vector2Int(x, z)][prefabs[i]].Add(0);
                            possiblePrefabs[new Vector2Int(x, z)][prefabs[i]].Add(90);
                            possiblePrefabs[new Vector2Int(x, z)][prefabs[i]].Add(180);
                            possiblePrefabs[new Vector2Int(x, z)][prefabs[i]].Add(270);
                        }
                    }
                    else 
                    {
                        // Get the entropy for the position
                        entropy = GetEntropy(possiblePrefabs[new Vector2Int(x, z)], x, z);
                    }

                    // Add the position to the ordered positions and entropy to the entropies
                    positions.Add(new Vector2Int(x, z));
                    entropies.Add(entropy);
                }
            }
        }

        // Sort the positions based on entropy (highest to lowest)
        var sortedSequence = positions.Zip(entropies, (position, entropy) => new { Position = position, Entropy = entropy })
                                    .OrderByDescending(item => item.Entropy)
                                    .ToList();

        positions = sortedSequence.Select(item => item.Position).ToList();
        entropies = sortedSequence.Select(item => item.Entropy).ToList();
    }
}
