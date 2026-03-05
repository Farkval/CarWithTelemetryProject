using Assets.Scripts.MapEditor.Controllers;
using UnityEngine;

public class MapInitializerTest : MonoBehaviour
{
    public MapTerrain terrain;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        terrain.Init(30);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
