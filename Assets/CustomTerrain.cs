﻿using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]

public class CustomTerrain : MonoBehaviour {
    public Vector2 randomHeightRange = new Vector2(0, 0.1f);
    public Texture2D heightMapImage;
    public Vector3 heightMapScale = new Vector3(1, 1, 1);

    public bool resetTerrain = true;

    //SINGLE PERLIN NOISE --------------
    public float perlinXScale = 0.01f;
    public float perlinYScale = 0.01f;

    public int perlinXOffset = 0;
    public int perlinYOffset = 0;
    public int perlinOctaves = 0;
    public float perlinPersistance = 8;
    public float perlinHeightScale = 0.09f;

    //MULTIPLE PERLIN NOISE --------------
    [System.Serializable]
    public class PerlinParameters
    {
        public float XScale = 0.01f;
        public float YScale = 0.01f;

        public int XOffset = 0;
        public int YOffset = 0;
        public int octaves = 0;
        public float persistance = 8;
        public float heightScale = 0.09f;
        public bool remove = false;
    }
    public List<PerlinParameters> perlinParameters = new List<PerlinParameters>()
    {
        new PerlinParameters()
    };


    // VORONOI
    public int voronoiPeaks = 5;
    public float voronoiFallOff = 0.2f;
    public float voronoiDropOff = 0.6f;
    public float voronoiMinHeight = 0.1f;
    public float voronoiMaxHeight = 0.5f;
    public enum VoronoiType {  Linear = 0, Power = 1, Combined = 2, Round = 3};
    public VoronoiType voronoiType = VoronoiType.Linear;

    // MPD
    public float mpdMinHeight = -10f;
    public float mpdMaxHeight = 10f;
    public float mpdDampenerPower = 2f;
    public float mpdRoughness = 2f;

    // Smooth
    public int smoothAmount = 1;
    public Terrain terrain;
    public TerrainData terrainData;

    // SPLATMAPS

    [System.Serializable]
    public class SplatHeights
    {
        public Texture2D texture = null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 90;
        public Vector2 tileOffset = new Vector2(0, 0);
        public Vector2 tileSize = new Vector2(50, 50);
        public bool remove = false;

        public float splatOffset = 0.1f;
        public float splatScaleX = 0.01f;
        public float splatScaleY = 0.01f;
        public float splatScale = 0.1f;
    }
    public List<SplatHeights> splatHeights = new List<SplatHeights>()
    {
        new SplatHeights()
    };

    // Vegetation
    [System.Serializable]
    public class Vegetation
    {
        public GameObject mesh;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 90;
        public bool remove = false;
    }
    public List<Vegetation> vegetation = new List<Vegetation>()
    {
        new Vegetation()
    };
    public int vegTreesMax = 5000;
    public int vegTreesSpacing = 5;

    float[,]  GetHeightMap()
    {
        if (resetTerrain)
        {
            return new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
        }
        return terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
    }

    List<Vector2> GenerateNeighbours(Vector2 pos, int width, int height)
    {
        List<Vector2> neighbours = new List<Vector2>();
        for (int y = -1; y < 2; y++)
        {
            for (int x = -1; x < 2; x++)
            {
                if (x == 0 && y == 0)
                    continue;
                Vector2 nPos = new Vector2(Mathf.Clamp(pos.x + x, 0, width - 1),
                    Mathf.Clamp(pos.y + y, 0, height - 1));

                if (!neighbours.Contains(nPos))
                    neighbours.Add(nPos);
            }
        }
        return neighbours;
    }
    float GetSteepness(float[,] heightmap, int x, int y, int width, int height)
    {
        float h = heightmap[x, y];
        int nx = x + 1;
        int ny = y + 1;

        // on edge, find gradient from vector in other direction
        if (nx > width - 1) nx = nx - 1;
        if (ny > width - 1) ny = ny - 1;

        float dx = heightmap[nx, y] - h;
        float dy = heightmap[x, ny] - h;
        Vector2 gradient = new Vector2(dx, dy);

        return gradient.magnitude;
    }

    public void ApplyVegetation() {
        TreePrototype[] newTreePrototypes;
        newTreePrototypes = new TreePrototype[vegetation.Count];
        int i = 0;
        foreach(Vegetation t in vegetation)
        {
            newTreePrototypes[i] = new TreePrototype();
            newTreePrototypes[i].prefab = t.mesh;
            i++;
        }
        terrainData.treePrototypes = newTreePrototypes;

        List<TreeInstance> allVegetation = new List<TreeInstance>();

        // size = world coordinates
        for (int z = 0; z < terrainData.size.z; z += vegTreesSpacing)
        {
            for (int x = 0; x < terrainData.size.x; x += vegTreesSpacing)
            {
                for (int tp = 0; tp < terrainData.treePrototypes.Length; tp++)
                {
                    int tx = x + UnityEngine.Random.Range(-(vegTreesSpacing / 2), (vegTreesSpacing / 2));
                    int tz = z + UnityEngine.Random.Range(-(vegTreesSpacing / 2), (vegTreesSpacing / 2));
                    if (tz > terrainData.size.z || tx > terrainData.size.x)
                        continue;

                    float thisHeight = terrainData.GetHeight(tx, tz) / terrainData.size.y;

                    if ((thisHeight <= vegetation[tp].minHeight) || (thisHeight >= vegetation[tp].maxHeight))
                        continue;

                    TreeInstance instance = new TreeInstance();
                    instance.position = new Vector3(tx / terrainData.size.x,
                                                    thisHeight,
                                                    tz / terrainData.size.z);

                    // Why not using tx/tz?
                    // transform.position = instance.position is relative(?) I don't get it, see:
                    // 7/46; 5m50s
                    Vector3 treeWorldPos = new Vector3(instance.position.x * terrainData.size.x,
                                                       instance.position.y * terrainData.size.y,
                                                       instance.position.z * terrainData.size.z) +
                                                         this.transform.position;
                    RaycastHit hit;
                    int layerMask = 1 << terrainLayer; // << ???

                    if (Physics.Raycast(treeWorldPos, -Vector3.up, out hit, 100, layerMask) ||
                        Physics.Raycast(treeWorldPos, -Vector3.down, out hit, 100, layerMask))
                    {
                        float treeHeight = (hit.point.y - this.transform.position.y) / terrainData.size.y;
                        instance.position = new Vector3(instance.position.x,
                                                        treeHeight,
                                                        instance.position.z);
                    }
                    instance.rotation = UnityEngine.Random.Range(0, 360);
                    instance.prototypeIndex = tp;
                    instance.color = Color.white;
                    instance.lightmapColor = Color.white;
                    instance.heightScale = 0.95f;
                    instance.widthScale = 0.95f;
                    allVegetation.Add(instance);

                    if (allVegetation.Count >= vegTreesMax)
                        goto TREESDONE;
                }
            }
        }
    TREESDONE:
        terrainData.treeInstances = allVegetation.ToArray();
    }
    public void AddNewVegetation()
    {
        vegetation.Add(new Vegetation());
    }
    public void RemoveVegetation()
    {
        List<Vegetation> keptVegetation = new List<Vegetation>();
        for (int i = 0; i < vegetation.Count; i++)
        {
            if (!vegetation[i].remove)
            {
                keptVegetation.Add(vegetation[i]);
            }
        }
        if (keptVegetation.Count == 0)
        {
            keptVegetation.Add(vegetation[0]);
        }
        vegetation = keptVegetation;
    }

    public void SplatMaps()
    {
        SplatPrototype[] newSplatPrototypes;
        newSplatPrototypes = new SplatPrototype[splatHeights.Count]; // allow as many prototypes as we have splat heights
        int spindex = 0;
        foreach (SplatHeights sh in splatHeights)
        {
            newSplatPrototypes[spindex] = new SplatPrototype();
            newSplatPrototypes[spindex].texture = sh.texture;
            newSplatPrototypes[spindex].tileOffset = sh.tileOffset;
            newSplatPrototypes[spindex].tileSize = sh.tileSize;
            newSplatPrototypes[spindex].texture.Apply(true);
            spindex++;
        }
        terrainData.splatPrototypes = newSplatPrototypes;

        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        float[,,] splatMapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int x = 0; x < terrainData.alphamapWidth; x++)
        {
            for (int y = 0; y < terrainData.alphamapHeight; y++)
            {
                float[] splat = new float[terrainData.alphamapLayers];
                for (int i = 0; i < splatHeights.Count; i++)
                {
                    float noise = Mathf.PerlinNoise(x * splatHeights[i].splatScaleX,
                                                    y * splatHeights[i].splatScaleY) * splatHeights[i].splatScale;
                    float offset = splatHeights[i].splatOffset + noise;
                    float thisHeightStart = splatHeights[i].minHeight - offset;
                    float thisHeightStop = splatHeights[i].maxHeight + offset;
                    /*float steepness = GetSteepness(heightMap, x, y,
                                                   terrainData.heightmapWidth,
                                                   terrainData.heightmapWidth);
                    */
                    // For some reason?? the alpha map and height map is rotated 90 degrees:
                    float steepness = terrainData.GetSteepness(y/(float)terrainData.alphamapHeight, x/(float)terrainData.alphamapWidth);
                    if (heightMap[x, y] >= thisHeightStart &&
                        heightMap[x, y] <= thisHeightStop &&
                        steepness >= splatHeights[i].minSlope &&
                        steepness <= splatHeights[i].maxSlope)
                    {
                        splat[i] = 1;
                    }
                }
                NormalizeVector(splat);
                for (int j = 0; j < splatHeights.Count; j++)
                {
                    splatMapData[x, y, j] = splat[j];
                }
            }
        }
        terrainData.SetAlphamaps(0, 0, splatMapData);
    }
    public void AddNewSplatHeight() {
        splatHeights.Add(new SplatHeights());
    }
    public void RemoveSplatHeight() {
        List<SplatHeights> keptSplatHeights = new List<SplatHeights>();
        for (int i = 0; i<splatHeights.Count;i++)
        {
            if (!splatHeights[i].remove)
            {
                keptSplatHeights.Add(splatHeights[i]);
            }
        }
        if (keptSplatHeights.Count == 0)
        {
            keptSplatHeights.Add(splatHeights[0]);
        }
        splatHeights = keptSplatHeights;
    }
    void NormalizeVector(float[] v)
    {
        float total = 0;
        for (int i =0; i < v.Length; i++)
        {
            total += v[i];
        }
        for (int i = 0; i< v.Length; i++)
        {
            v[i] /= total;
        }
    }
    public void SmoothN()
    {
        for (int n = 0; n < smoothAmount; n++)
        {
            EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", n / smoothAmount);
            Smooth();
        }
        EditorUtility.ClearProgressBar();
    }
    public void Smooth()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                float avgHeight = heightMap[x, y];
                List<Vector2> neighbours = GenerateNeighbours(new Vector2(x, y),
                    terrainData.heightmapWidth,
                    terrainData.heightmapHeight);

                foreach (Vector2 n in neighbours)
                {
                    avgHeight += heightMap[(int)n.x, (int)n.y];
                }
                heightMap[x, y] = avgHeight / ((float)neighbours.Count + 1);
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    public void SmoothMy()
    {
        float[,] heightMap = GetHeightMap();

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                int div = 0;
                float sum = 0;
                for (int xi = x-1; xi <= x+1; xi++)
                {
                    if (xi < 0 || xi >= terrainData.heightmapWidth)
                        continue;

                    for (int yi = y-1; yi <= y+1; yi++)
                    {
                        if (yi < 0 || yi >= terrainData.heightmapHeight)
                            continue;

                        div++;
                        sum += heightMap[xi, yi];
                    }
                }
                // Debug.Log("x = " + x + ", y = " + y + ", sum = " + sum + ", div = " + div);
                heightMap[x, y] = sum / div;
                /*
                        heightMap[x, y] = (heightMap[x, y] +
                                           heightMap[x + 1, y] +
                                           heightMap[x - 1, y] +
                                           heightMap[x + 1, y + 1] +
                                           heightMap[x - 1, y - 1] +
                                           heightMap[x + 1, y - 1] +
                                           heightMap[x - 1, y + 1] +
                                           heightMap[x, y + 1] +
                                           heightMap[x, y - 1]) / 9.0f;*/
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    public void MidPointDisplacement()
    {
        float[,] heightMap = GetHeightMap();
        int width = terrainData.heightmapWidth - 1;
        int squareSize = width;
        float heightMin = mpdMinHeight;
        float heightMax = mpdMaxHeight;

        // float height = (float)squareSize / 2.0f * 0.01f;
        float heightDampener = (float)Mathf.Pow(mpdDampenerPower, -1 * mpdRoughness);

        int cornerX, cornerY;
        int midX, midY;
        // (x, cornerY)   (cornerX, cornerY)
        // +-------------------------------+
        // |                               |
        // |             x (midX, midY)    |
        // |                               |
        // +-------------------------------+
        // (x, y)               (cornerX, y)

        int pmidXL, pmidXR, pmidYU, pmidYD;

        // Add random corner heights
        /*
        heightMap[0, 0] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[0, terrainData.heightmapHeight - 2] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[terrainData.heightmapWidth - 2, 0] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[terrainData.heightmapWidth - 2, terrainData.heightmapHeight - 2] =
            UnityEngine.Random.Range(0f, 0.2f);*/
        // - 2 because heightMap is mesh size + 1

        while (squareSize > 0)
        {
            Debug.Log("squareSize = " + squareSize);
            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);
                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    // Set mid point to average of corner heights
                    heightMap[midX, midY] = (float)((heightMap[x, y] +
                                                    heightMap[cornerX, y] +
                                                    heightMap[x, cornerY] +
                                                    heightMap[cornerX, cornerY]) / 4.0f + 
                                                    UnityEngine.Random.Range(heightMin, heightMax));
                }
            }

            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);
                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    pmidXR = (int)(midX + squareSize);
                    pmidYU = (int)(midY + squareSize);
                    pmidXL = (int)(midX - squareSize);
                    pmidYD = (int)(midY - squareSize);

                    if (pmidXL <= 0 || pmidYD <= 0
                        || pmidXR >= width - 1 || pmidYU >= width - 1) continue;

                    heightMap[midX, y] = (float)((heightMap[x, y] +
                                                  heightMap[midX, midY] +
                                                  heightMap[cornerX, y] +
                                                  heightMap[midX, pmidYD]) / 4.0f +
                                                  UnityEngine.Random.Range(heightMin, heightMax));

                    heightMap[midX, cornerY] = (float)((heightMap[x, cornerY] +
                                                        heightMap[midX, pmidYU] +
                                                        heightMap[cornerX, cornerY] +
                                                        heightMap[midX, midY]) / 4.0f +
                                                        UnityEngine.Random.Range(heightMin, heightMax));

                    heightMap[x, midY] = (float)((heightMap[pmidXL, midY] +
                                                  heightMap[x, cornerY] +
                                                  heightMap[midX, midY] +
                                                  heightMap[x, y]) / 4.0f +
                                                  UnityEngine.Random.Range(heightMin, heightMax));

                    heightMap[cornerX, midY] = (float)((heightMap[midX, midY] +
                                                        heightMap[cornerX, cornerY] +
                                                        heightMap[pmidXR, midY] +
                                                        heightMap[cornerX, y]) / 4.0f +
                                                        UnityEngine.Random.Range(heightMin, heightMax));
                }
            }

            squareSize = (int)(squareSize / 2.0f);
            heightMin *= heightDampener;
            heightMax *= heightDampener;
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    public void Voronoi()
    {
        float[,] heightMap = GetHeightMap();
        float maxDistance = Vector2.Distance(new Vector2(0, 0), new Vector2(terrainData.heightmapWidth, terrainData.heightmapHeight));

        for (int i = 0; i < voronoiPeaks; i++)
        {   
            Vector3 peak = new Vector3(UnityEngine.Random.Range(0, terrainData.heightmapWidth),
                                       UnityEngine.Random.Range(voronoiMinHeight, voronoiMaxHeight),
                                       UnityEngine.Random.Range(0, terrainData.heightmapHeight)
                                      );

            if (heightMap[(int)peak.x, (int)peak.y] < peak.y)
                heightMap[(int)peak.x, (int)peak.z] = peak.y;
            else
                continue;

            Vector2 peakLocation = new Vector2(peak.x, peak.z);

            for (int x = 0; x < terrainData.heightmapWidth; x++)
            {
                for (int y = 0; y < terrainData.heightmapHeight; y++)
                {
                    if (!(x == peak.x && y == peak.z))
                    {
                        float distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, y)) / maxDistance;
                        float h = 0;
                        
                        switch(voronoiType)
                        {
                            case VoronoiType.Combined:
                                h = peak.y - distanceToPeak * voronoiFallOff -
                                    Mathf.Pow(distanceToPeak, voronoiDropOff);
                                break;
                            case VoronoiType.Power:
                                h = peak.y - Mathf.Pow(distanceToPeak, voronoiDropOff) * voronoiFallOff;
                                break;
                            case VoronoiType.Linear:
                                h = peak.y - distanceToPeak * voronoiFallOff;
                                break;
                            case VoronoiType.Round:
                                h = peak.y - Mathf.Pow(distanceToPeak * 3, voronoiFallOff) -
                                    Mathf.Sin(distanceToPeak * 2 * Mathf.PI) / voronoiDropOff;
                                break;
                        }

                        if (heightMap[x, y] < h )
                            heightMap[x, y] = h;
                    }
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    public void MultiplePerlinTerrain()
    {
        float[,] heightMap = GetHeightMap();

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                foreach(PerlinParameters p in perlinParameters)
                {
                    heightMap[x, y] += Utils.fBM(
                    (x + p.XOffset) * p.XScale,
                    (y + p.YOffset) * p.YScale,
                    p.octaves,
                    p.persistance) * p.heightScale;
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    public void AddNewPerlin()
    {
        perlinParameters.Add(new PerlinParameters());
    }
    public void RemovePerlin()
    {
        List<PerlinParameters> keptPerlinParameters = new List<PerlinParameters>();
        for (int i = 0; i< perlinParameters.Count;i++) // why not foreach?
        {
            if (!perlinParameters[i].remove)
            {
                keptPerlinParameters.Add(perlinParameters[i]);
            }
        }
        if (keptPerlinParameters.Count == 0) // why not check first?
        {
            keptPerlinParameters.Add(perlinParameters[0]);
        }
        perlinParameters = keptPerlinParameters;
    }
    public void Perlin()
    {
        float[,] heightMap = GetHeightMap();

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                heightMap[x, y] += Utils.fBM(
                    (x + perlinXOffset) * perlinXScale,
                    (y + perlinYOffset) * perlinYScale,
                    perlinOctaves,
                    perlinPersistance) * perlinHeightScale;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    public void PerlinSingle()
    {
        float[,] heightMap = GetHeightMap();

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                heightMap[x, y] += Mathf.PerlinNoise(
                    (x + perlinXOffset) * perlinXScale,
                    (y + perlinYOffset) * perlinYScale
                );
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    public void LoadTexture()
    {
        float[,] heightMap = GetHeightMap();
        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                heightMap[x, y] += heightMapImage.GetPixel(
                    (int)(x * heightMapScale.x),
                    (int)(y * heightMapScale.z)).grayscale * heightMapScale.y;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    public void ResetTerrain()
    {
        float[,] heightMap;
        heightMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
        terrainData.SetHeights(0, 0, heightMap);
    }
    public void RandomTerrain()
    {
        float[,] heightMap = GetHeightMap();

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                heightMap[x, y] += UnityEngine.Random.Range(randomHeightRange.x, randomHeightRange.y);
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    private void OnEnable()
    {
        Debug.Log("Initializing Terrain Data");
        terrain = this.GetComponent<Terrain>();
        terrainData = Terrain.activeTerrain.terrainData;
    }

    public enum TagType {  Tag = 0, Layer = 1}
    [SerializeField] // why not prop?
    int terrainLayer = -1;
    private void Awake()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        AddTag(tagsProp, "Terrain", TagType.Tag);
        AddTag(tagsProp, "Cloud", TagType.Tag);
        AddTag(tagsProp, "Shore", TagType.Tag);
        tagManager.ApplyModifiedProperties();

        SerializedProperty layerProp = tagManager.FindProperty("layers");
        terrainLayer = AddTag(layerProp, "Terrain", TagType.Layer);
        tagManager.ApplyModifiedProperties(); // why twice?

        this.gameObject.tag = "Terrain";
        this.gameObject.layer = terrainLayer;
    }

    int AddTag(SerializedProperty tagsProp, string newTag, TagType tType)
    {
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(newTag)) {
                return i;
            }
        }
        switch (tType)
        {
            case TagType.Tag:
                tagsProp.InsertArrayElementAtIndex(0);
                SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
                newTagProp.stringValue = newTag;
                return 0; // we always insert at index 0
                break;

            case TagType.Layer:
                for (int j = 8; j < tagsProp.arraySize; j++)
                {
                    SerializedProperty newLayer = tagsProp.GetArrayElementAtIndex(j);
                    if (newLayer.stringValue == "")
                    {
                        Debug.Log("Adding New Layer:" + newTag);
                        newLayer.stringValue = newTag;
                        return j;
                    }
                }
                break;
        }
        return -1;
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
