using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoiseVisualizer : MonoBehaviour
{
    [SerializeField] private Image noise1image;
    [SerializeField] private Image noise2image;
    [SerializeField] private Image noise3image;
    [SerializeField] private Image noise4image;
    private Texture2D texture;
    private Color heightColor;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void SetImageNoise(float[,] mapArray, int imageIndex)
    {
        switch (imageIndex)
        {
            case 0:
                SetNoiseToTexture(mapArray, noise1image);
                break;
            case 1:
                SetNoiseToTexture(mapArray, noise2image);
                break;
            case 2:
                SetNoiseToTexture(mapArray, noise3image);
                break;
            case 3:
                SetNoiseToTexture(mapArray, noise4image);
                break;
        }
    }

    private void SetNoiseToTexture(float[,] mapArray, Image image)
    {
        int mapSize = mapArray.GetLength(0);
        //Debug.Log("mapsize: " + mapSize);
        texture = new Texture2D(mapSize, mapSize);

        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                heightColor = new Color(mapArray[x, y], mapArray[x, y], mapArray[x, y]);
                texture.SetPixel(x, y, heightColor);
                
            }
        }

        texture.Apply();

        image.sprite = Sprite.Create(texture, new Rect(0, 0, mapSize, mapSize), new Vector2(50,50));
    }
}
