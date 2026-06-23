using UnityEngine;
using System.Collections.Generic; 

public class PipeNode : MonoBehaviour
{
    [Header("Pipe Mesh Variants")]
    public GameObject meshSingle;
    public GameObject meshStraight;
    public GameObject meshCorner;
    public GameObject meshTJunction;
    public GameObject meshCross;

    public void CheckNeighborsAndSetShape(Dictionary<Vector3, GameObject> grid, Vector3 myPos, float gridSize)
    {
        
        bool north = grid.ContainsKey(myPos + Vector3.forward * gridSize);
        bool south = grid.ContainsKey(myPos + Vector3.back * gridSize);
        bool east = grid.ContainsKey(myPos + Vector3.right * gridSize);
        bool west = grid.ContainsKey(myPos + Vector3.left * gridSize);

        if (meshSingle != null) meshSingle.SetActive(false);
        meshStraight.SetActive(false);
        meshCorner.SetActive(false);
        meshTJunction.SetActive(false);
        if (meshCross != null) meshCross.SetActive(false);

        int connectionCount = (north ? 1 : 0) + (south ? 1 : 0) + (east ? 1 : 0) + (west ? 1 : 0);

        switch (connectionCount)
        {
            case 4: 
                if (meshCross != null) meshCross.SetActive(true);
                else meshStraight.SetActive(true); 
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;

            case 3: 
                meshTJunction.SetActive(true);
                if (north && east && south) transform.rotation = Quaternion.Euler(0, 180, 0);  
                else if (east && south && west) transform.rotation = Quaternion.Euler(0, 270, 0); 
                else if (south && west && north) transform.rotation = Quaternion.Euler(0, 0, 0); 
                else if (west && north && east) transform.rotation = Quaternion.Euler(0, 90, 0);
                break;

            case 2: 
                if (north && south)
                {
                    meshStraight.SetActive(true);
                    transform.rotation = Quaternion.Euler(0, 90, 0);
                }
                else if (east && west)
                {
                    meshStraight.SetActive(true);
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                else if (north && east)
                {
                    meshCorner.SetActive(true);
                    transform.rotation = Quaternion.Euler(0, 90, 0);
                }
                else if (east && south)
                {
                    meshCorner.SetActive(true);
                    transform.rotation = Quaternion.Euler(0, 180, 0);
                }
                else if (south && west)
                {
                    meshCorner.SetActive(true);
                    transform.rotation = Quaternion.Euler(0, 270, 0);
                }
                else if (west && north)
                {
                    meshCorner.SetActive(true);
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                break;

            case 1: 
                if (meshStraight != null)
                {
                    meshStraight.SetActive(true);
                    if (north || south) transform.rotation = Quaternion.Euler(0, 90, 0);
                    else if (east || west) transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                break;

            case 0: 
            default:
                if (meshSingle != null) meshSingle.SetActive(true);
                else meshStraight.SetActive(true);
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
        }
    }
}