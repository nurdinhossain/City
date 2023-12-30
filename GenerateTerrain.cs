using System.Collections;
using System.Collections.Generic;
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

    // For each prefab, have an array of open and closed sides
    // 0 = closed, 1 = open
    // 0 = north, 1 = east, 2 = south, 3 = west
    public int[] fourwayOpen = { 1, 1, 1, 1 };
    public int[] landOpen = { 0, 0, 0, 0 };
    public int[] roadendOpen = { 1, 0, 0, 0 };
    public int[] roadstraightOpen = { 0, 1, 0, 1 };
    public int[] troadOpen = { 1, 1, 1, 0 };
    public int[] turnroadOpen = { 1, 0, 0, 1 };
 
    // Grid dimensions
    public int gridSizeX = 10;
    public int gridSizeZ = 10;

    // Array to store instantiated tiles
    private GameObject[,] terrainGrid;

    // Counter for backtracked tiles
    private int backtrackedTiles = 0;

    void Start()
    {
        // Initialize the prefabs array
        prefabs = new GameObject[] { fourway, land, roadend, roadstraight, troad, turnroad };

        // Initialize the array
        terrainGrid = new GameObject[gridSizeX, gridSizeZ];

        // Fill edge tiles with roadend tiles facing outwards
        /*for (int x = 0; x < gridSizeX; x++)
        {
            // Ignore the corners
            if (x == 0 || x == gridSizeX - 1) continue;

            // Instantiate the roadend tile facing north
            terrainGrid[x, 0] = Instantiate(roadend, new Vector3(x, 0, 0), Quaternion.Euler(-90, 0, 180));

            // Instantiate the roadend tile facing south
            terrainGrid[x, gridSizeZ - 1] = Instantiate(roadend, new Vector3(x, 0, gridSizeZ - 1), Quaternion.Euler(-90, 0, 0));
        }

        for (int z = 0; z < gridSizeZ; z++)
        {
            // Ignore the corners
            if (z == 0 || z == gridSizeZ - 1) continue;

            // Instantiate the roadend tile facing east
            terrainGrid[0, z] = Instantiate(roadend, new Vector3(0, 0, z), Quaternion.Euler(-90, 0, 270));

            // Instantiate the roadend tile facing west
            terrainGrid[gridSizeX - 1, z] = Instantiate(roadend, new Vector3(gridSizeX - 1, 0, z), Quaternion.Euler(-90, 0, 90));
        }

        // Fill corners with land tiles
        terrainGrid[0, 0] = Instantiate(land, new Vector3(0, 0, 0), Quaternion.Euler(-90, 0, 0));
        terrainGrid[0, gridSizeZ - 1] = Instantiate(land, new Vector3(0, 0, gridSizeZ - 1), Quaternion.Euler(-90, 0, 0));
        terrainGrid[gridSizeX - 1, 0] = Instantiate(land, new Vector3(gridSizeX - 1, 0, 0), Quaternion.Euler(-90, 0, 0));
        terrainGrid[gridSizeX - 1, gridSizeZ - 1] = Instantiate(land, new Vector3(gridSizeX - 1, 0, gridSizeZ - 1), Quaternion.Euler(-90, 0, 0));*/

        // Fill edge tiles with land tiles
        for (int x = 0; x < gridSizeX; x++)
        {
            // Instantiate the land tile facing north
            terrainGrid[x, 0] = Instantiate(land, new Vector3(x, 0, 0), Quaternion.Euler(-90, 0, 0));

            // Instantiate the land tile facing south
            terrainGrid[x, gridSizeZ - 1] = Instantiate(land, new Vector3(x, 0, gridSizeZ - 1), Quaternion.Euler(-90, 0, 0));
        }

        for (int z = 0; z < gridSizeZ; z++)
        {
            // Ignore the corners
            if (z == 0 || z == gridSizeZ - 1) continue;

            // Instantiate the land tile facing east
            terrainGrid[0, z] = Instantiate(land, new Vector3(0, 0, z), Quaternion.Euler(-90, 0, 0));

            // Instantiate the land tile facing west
            terrainGrid[gridSizeX - 1, z] = Instantiate(land, new Vector3(gridSizeX - 1, 0, z), Quaternion.Euler(-90, 0, 0));
        }

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

    // recursively generate the terrain grid with wave function collapse and backtracking
    void GenerateTerrainGrid()
    {
        // Get the lowest entropy position
        Vector2Int lowestEntropyPosition = GetLowestEntropyPosition();

        // If there are no more positions with 0 entropy, return
        if (lowestEntropyPosition.x == 0 && lowestEntropyPosition.y == 0) return;

        // Get the x and z coordinates of the lowest entropy position
        int x = lowestEntropyPosition.x;
        int z = lowestEntropyPosition.y;

        // Get the randomized prefabs and angles
        GameObject[] randomizedPrefabs = GetRandomizedPrefabs();
        int[] randomizedAngles = GetRandomizedAngles();

        // Check all prefabs
        for (int i = 0; i < randomizedPrefabs.Length; i++)
        {
            // Check all angles
            for (int j = 0; j < randomizedAngles.Length; j++)
            {
                // If the tile can be placed at the position
                if (CanPlaceTile(randomizedPrefabs[i], x, z, randomizedAngles[j]))
                {
                    // Instantiate the tile at the position
                    terrainGrid[x, z] = Instantiate(randomizedPrefabs[i], new Vector3(x, 0, z), Quaternion.Euler(-90, 0, randomizedAngles[j]));

                    // Recursively generate the terrain grid
                    GenerateTerrainGrid();

                    // If the terrain grid is complete, return
                    if (IsTerrainGridComplete()) return;

                    // Destroy the tile at the position
                    Destroy(terrainGrid[x, z]);

                    // Increment the backtracked tiles counter
                    backtrackedTiles++;
                }
            }
        }
    }

    // Check if the terrain grid is complete
    bool IsTerrainGridComplete()
    {
        // Check all positions
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                // If there is no tile at the position, return false
                if (terrainGrid[x, z] == null) return false;
            }
        }

        return true;
    }

    int[] GetOpenClosedArray(GameObject tile)
    {
        // if tile is null, return an array of all 2s
        if (tile == null) return new int[] { 2, 2, 2, 2 };

        // Get the Open/Closed array based on the tile type
        if (tile.name.Contains(fourway.name)) return fourwayOpen;
        if (tile.name.Contains(land.name)) return landOpen;
        if (tile.name.Contains(roadend.name)) return roadendOpen;
        if (tile.name.Contains(roadstraight.name)) return roadstraightOpen;
        if (tile.name.Contains(troad.name)) return troadOpen;
        if (tile.name.Contains(turnroad.name)) return turnroadOpen;

        // If the tile is not recognized, return an array of all 2s
        return new int[] { 2, 2, 2, 2 };
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
        // Get the open/closed array for the tile
        int[] tileArray = GetOpenClosedArray(tile);

        // Get the open/closed array for the surrounding tiles
        int east = x - 1, west = x + 1, north = z - 1, south = z + 1;
        int[] northArray = GetOpenClosedArray(terrainGrid[x, north]);
        int[] eastArray = GetOpenClosedArray(terrainGrid[east, z]);
        int[] southArray = GetOpenClosedArray(terrainGrid[x, south]);
        int[] westArray = GetOpenClosedArray(terrainGrid[west, z]);

        // Rotate the surrounding tile arrays based on angle of that tile (only if there is a tile there)
        if (terrainGrid[x, north] != null) northArray = RotateArray(northArray, (int)terrainGrid[x, north].transform.eulerAngles.y);
        if (terrainGrid[east, z] != null) eastArray = RotateArray(eastArray, (int)terrainGrid[east, z].transform.eulerAngles.y);
        if (terrainGrid[x, south] != null) southArray = RotateArray(southArray, (int)terrainGrid[x, south].transform.eulerAngles.y);
        if (terrainGrid[west, z] != null) westArray = RotateArray(westArray, (int)terrainGrid[west, z].transform.eulerAngles.y);

        // Rotate the tile array based on the angle
        tileArray = RotateArray(tileArray, angle);

        // Check if the tile can be placed
        if (tileArray[0] != northArray[2] && northArray[2] != 2) return false;
        if (tileArray[1] != eastArray[3] && eastArray[3] != 2) return false;
        if (tileArray[2] != southArray[0] && southArray[0] != 2) return false;
        if (tileArray[3] != westArray[1] && westArray[1] != 2) return false;

        return true;
    }

    // Get number of possible orientations for a tile in a given position
    int GetOrientations(GameObject tile, int x, int z)
    {
        int orientations = 0;

        // Check all 4 rotations
        for (int angle = 0; angle < 360; angle += 90)
        {
            if (CanPlaceTile(tile, x, z, angle)) orientations++;
        }

        return orientations;
    }

    // Get entropy for a position
    int GetEntropy(int x, int z)
    {
        int entropy = 0;

        // Check all prefabs
        for (int i = 0; i < prefabs.Length; i++)
        {
            entropy += GetOrientations(prefabs[i], x, z);
        }

        return entropy;
    }

    // Get lowest entropy position without a tile
    Vector2Int GetLowestEntropyPosition()
    {
        // Initialize the lowest entropy position
        Vector2Int lowestEntropyPosition = new Vector2Int(0, 0);

        // Initialize the lowest entropy
        int lowestEntropy = 1000;

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
                        entropy = prefabs.Length * 4;
                    }
                    else 
                    {
                        // Get the entropy for the position
                        entropy = GetEntropy(x, z);
                    }

                    // If the entropy is lower than the current lowest entropy
                    if (entropy < lowestEntropy)
                    {
                        // Update the lowest entropy and lowest entropy position
                        lowestEntropy = entropy;
                        lowestEntropyPosition = new Vector2Int(x, z);
                    }
                }
            }
        }

        return lowestEntropyPosition;
    }

    // Get randomized order of prefabs
    GameObject[] GetRandomizedPrefabs()
    {
        // Create a new array to store the randomized prefabs
        GameObject[] randomizedPrefabs = new GameObject[prefabs.Length];

        // Create a list of integers to store the indices of the prefabs
        List<int> indices = new List<int>();

        // Add all indices to the list
        for (int i = 0; i < prefabs.Length; i++)
        {
            indices.Add(i);
        }

        // Randomize the order of the indices
        for (int i = 0; i < prefabs.Length; i++)
        {
            // Get a random index
            int randomIndex = Random.Range(0, indices.Count);

            // Add the prefab at the random index to the randomized prefabs array
            randomizedPrefabs[i] = prefabs[indices[randomIndex]];

            // Remove the index from the list
            indices.RemoveAt(randomIndex);
        }

        return randomizedPrefabs;
    }

    // Get randomized order of angles
    int[] GetRandomizedAngles()
    {
        // Create a new array to store the randomized angles
        int[] randomizedAngles = new int[] { 0, 90, 180, 270 };

        // Randomize the order of the angles
        for (int i = 0; i < randomizedAngles.Length; i++)
        {
            // Get a random index
            int randomIndex = Random.Range(0, randomizedAngles.Length);

            // Swap the angle at the random index with the angle at the current index
            int temp = randomizedAngles[i];
            randomizedAngles[i] = randomizedAngles[randomIndex];
            randomizedAngles[randomIndex] = temp;
        }

        return randomizedAngles;
    }
}
