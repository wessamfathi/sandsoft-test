using System;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.Assertions;

public class GridGenerator : MonoBehaviour
{
    // Grid containing all the gems
    private GemType[,] Grid;

    public enum GemType
    {
        // Start as 1 so grid initial value of 0 is invalid
        Apple = 1,
        Pineapple,
        Banana,
        Orange,
        Strawberry,
        Kiwi
    }

    // Used to render gem icons
    public Texture2D[] GemIcons = new Texture2D[GemTypesMax];

    // Parent gameObject for all icons (organization)
    public GameObject GemIconsParent;

    public Material SelectedMaterial;

    private Material OriginalMaterial;

    private SpriteRenderer[] GridSprites;

    private const int GridWidth = 8;
    private const int GridHeight = 8;

    private const int GemTypesMin = (int)GemType.Apple;
    private const int GemTypesMax = (int)GemType.Kiwi;

    // Minimum number of similar neighbors for a match
    private const int MinNeighbors = 2;

    // Random as member to ease debugging by a fixed seed when needed
    System.Random GridRand = new System.Random();

    private GameObject SelectedGem = null;

    private Camera MainCamera;

    private bool IsSwappingGems = false;

    private GameObject OriginGem;
    private GameObject TargetGem;

    private Vector3 OriginGemPosition;
    private Vector3 TargetGemPosition;

    private const float SwapDuration = 0.5f;
    private const float NeighborDistance = 1.5f;
    private const float SelfDistance = 0.5f;

    private float SwapTime;
    private float SwapDistance;

    private bool IsHorizontalSwap;
    private bool IsVerticalSwap;

    private void Start()
    {
        // Save main camera for performance
        MainCamera = Camera.main;

        // Generate automatically on startup
        GenerateMatchGrid();
    }

    private void Update()
    {
        if (!IsSwappingGems)
        {
            Vector3? inputPosition = null;

            // Handle mouse click
            if (Input.GetMouseButtonDown(0))
            {
                inputPosition = Input.mousePosition;
            }

            // Handle screen touches.
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Ended)
                {
                    inputPosition = touch.position;
                }
            }

            if (inputPosition != null)
            {
                Debug.Log(inputPosition);

                Ray ray = MainCamera.ScreenPointToRay(inputPosition.Value);
                RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

                if (hit.collider != null)
                {
                    GameObject clickedGem = hit.collider.gameObject;

                    if (clickedGem != null && SelectedGem != null && SelectedGem != clickedGem)
                    {
                        // Swap the two gems if we already have a selected neighbor
                        Vector3 clickedPosition = clickedGem.transform.position;

                        float deltaX = Math.Abs(SelectedGem.transform.position.x - clickedPosition.x);
                        float deltaY = Math.Abs(SelectedGem.transform.position.y - clickedPosition.y);

                        Debug.Log("X: " + deltaX);
                        Debug.Log("Y: " + deltaY);

                        if (deltaX < NeighborDistance && deltaX > SelfDistance)
                        {
                            IsHorizontalSwap = true;
                            IsVerticalSwap = false;
                        }
                        else if (deltaY < NeighborDistance && deltaY > SelfDistance)
                        {
                            IsHorizontalSwap = false;
                            IsVerticalSwap = true;
                        }
                        else
                        {
                            IsHorizontalSwap = false;
                            IsVerticalSwap = false;
                        }

                        if (IsHorizontalSwap || IsVerticalSwap)
                        {
                            // Restore the selected gem's material
                            SelectedGem.GetComponent<SpriteRenderer>().material = OriginalMaterial;

                            // Start the swapping animation 
                            IsSwappingGems = true;

                            OriginGem = SelectedGem;
                            OriginGemPosition = OriginGem.transform.position;

                            TargetGem = clickedGem;
                            TargetGemPosition = TargetGem.transform.position;

                            SwapTime = 0;

                            if (IsHorizontalSwap)
                            {
                                SwapDistance = deltaX;
                            }
                            else if (IsVerticalSwap)
                            {
                                SwapDistance = deltaY;
                            }

                            // Clear the selected gem
                            SelectedGem = null;
                        }
                    }
                    else
                    {
                        // Otherwise, save it for later
                        SelectedGem = clickedGem;

                        // Change selected gem material
                        OriginalMaterial = SelectedGem.GetComponent<SpriteRenderer>().material;
                        SelectedGem.GetComponent<SpriteRenderer>().material = SelectedMaterial;
                    }
                }
            }
        }
        else
        {
            // We are swapping the two Gems, don't take any input until we're done
            float interpolationRatio = SwapTime / SwapDuration;
            SwapTime += Time.deltaTime;

            TargetGem.transform.position = Vector3.Lerp(TargetGemPosition, OriginGemPosition, interpolationRatio);
            OriginGem.transform.position = Vector3.Lerp(OriginGemPosition, TargetGemPosition, interpolationRatio);

            if (SwapTime > SwapDuration)
            {
                IsSwappingGems = false;
            }
        }
    }

    public void GenerateMatchGrid()
    {
        // Clear the selected gem
        SelectedGem = null;

        // We generate an initially solved grid
        GenerateInitialGrid();

        // Then, keep shuffling till conditions are satisfied
        ShuffleGridTiles();

        // Finally, generate the sprites for visual representation
        GenerateGridSprites();
    }

    private void GenerateGridSprites()
    {
        GridSprites = new SpriteRenderer[GridWidth * GridHeight];

        for (int i = 0; i < GemIconsParent.transform.childCount; i++)
        {
            Destroy(GemIconsParent.transform.GetChild(i).gameObject);
        }

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                int GridGemType = (int)Grid[x, y];
                Texture2D tex = GemIcons[GridGemType - 1];

                GameObject iconGameObject = new GameObject("Icon + " + x.ToString() + "," + y.ToString());
                iconGameObject.transform.SetParent(GemIconsParent.transform, false);
                iconGameObject.transform.position += new Vector3(x - (GridWidth / 2) + 0.5f, y - (GridHeight / 2) + 0.5f, 0);

                int spriteIndex = x * GridWidth + y;
                GridSprites[spriteIndex] = iconGameObject.AddComponent<SpriteRenderer>();
                GridSprites[spriteIndex].sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);

                iconGameObject.AddComponent<BoxCollider2D>();
            }
        }
    }

    // Generate a solved grid
    private void GenerateInitialGrid()
    {
        Grid = new GemType[GridWidth, GridHeight];

        int currentGem = 0;

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                if (Grid[x, y] == 0)
                {
                    // pick a random gem type
                    currentGem = GridRand.Next(GemTypesMin, GemTypesMax + 1);

                    // and a valid direction for the rest of the match
                    if (x + MinNeighbors < GridWidth)
                    {
                        // fill the tiles to the right
                        for (int i = 0; i <= MinNeighbors; i++)
                        {
                            Grid[x + i, y] = (GemType)currentGem;
                        }
                    }
                    else if (y + MinNeighbors < GridHeight)
                    {
                        // fill the tiles below
                        for (int i = 0; i <= MinNeighbors; i++)
                        {
                            Grid[x, y + i] = (GemType)currentGem;
                        }
                    }
                    else if (x - MinNeighbors >= 0)
                    {
                        // no more empty tiles to the right or below
                        // fill based on tiles from the left
                        Grid[x, y] = Grid[x - 1, y];
                    }
                    else
                    {
                        // should never reach here
                        Assert.IsTrue(false, "Error finding neighbors while generating grid");
                    }
                }
            }
        }

        Debug.Log("---------------------------------------");
        Debug.Log("Generating initial grid:");
        PrintGrid();
        Debug.Log("---------------------------------------");
    }

    // Prints a text representation of the grid
    private void PrintGrid()
    {
        StringBuilder stringBuilder = new StringBuilder();
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                // Use first letter as it's easier for debugging 
                stringBuilder.Append(Grid[x, y].ToString()[0] + " ");
            }
            stringBuilder.AppendLine();
        }

        Debug.Log(stringBuilder.ToString());
    }

    private void ShuffleGridTiles()
    {
        // Limit max number of iterations to prevent infinite loop.
        // in edge-cases, using random numbers takes too long to 
        // properly shuffle the grid. This is sub-optimal, as sometimes
        // the grid can still not be random enough.
        int iterationsCount = 0;

        do
        {
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    if (HasMatchingNeighbors(x, y))
                    {
                        // Swap the current element with a random, different element
                        int randomX = x;
                        int randomY = y;

                        do
                        {
                            randomX = GridRand.Next(GridWidth);
                            randomY = GridRand.Next(GridHeight);
                        } while (Grid[randomX, randomY] == Grid[x, y]);

                        GemType temp = Grid[x, y];
                        Grid[x, y] = Grid[randomX, randomY];
                        Grid[randomX, randomY] = temp;
                    }
                }
            }

            iterationsCount++;
        } while (HasTooManyNeighbors() && iterationsCount < 200);


        Debug.Log("---------------------------------------");
        Debug.Log("Final grid in: " + iterationsCount + " iterations");
        PrintGrid();
        Debug.Log("---------------------------------------");
    }

    private bool HasMatchingNeighbors(int x, int y)
    {
        int adjacentCount = 0;

        // Check horizontal neighbors
        for (int i = -MinNeighbors; i <= MinNeighbors; i++)
        {
            if (i != 0 && x + i >= 0 && x + i < GridWidth && Grid[x, y] == Grid[x + i, y])
            {
                adjacentCount++;
            }
        }

        if (adjacentCount >= 2)
        {
            return true;
        }

        adjacentCount = 0;

        // Check vertical neighbors
        for (int i = -MinNeighbors; i <= MinNeighbors; i++)
        {
            if (i != 0 && y + i >= 0 && y + i < GridHeight && Grid[x, y] == Grid[x, y + i])
            {
                adjacentCount++;
            }
        }

        if (adjacentCount >= 2)
        {
            return true;
        }

        return false;
    }

    private bool HasTooManyNeighbors()
    {
        // Check for more than two similar tiles adjacent to each other
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                // If one tile has too many neighbors, we still need to shuffle
                if (HasMatchingNeighbors(x, y))
                {
                    return true;
                }
            }
        }

        return false;
    }
}