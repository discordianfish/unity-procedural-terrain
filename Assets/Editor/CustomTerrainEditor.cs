using UnityEngine;
using UnityEditor;
using EditorGUITable;

[CustomEditor(typeof(CustomTerrain))] // [1] Links editor to object so when inspecting object, it loads this editor (S02L09) 11:26
[CanEditMultipleObjects]


// Q:
// - what exactly is serializedObject and how is it linked to the CustomTerrain?
//
// GUILayout.Space(20)

public class CustomTerrainEditor : Editor {
    // properties -----
    SerializedProperty resetTerrain;

    SerializedProperty randomHeightRange;
    SerializedProperty heightMapScale;
    SerializedProperty heightMapImage;
    SerializedProperty perlinXScale;
    SerializedProperty perlinYScale;
    SerializedProperty perlinXOffset;
    SerializedProperty perlinYOffset;

    SerializedProperty perlinOctaves;
    SerializedProperty perlinPersistance;
    SerializedProperty perlinHeightScale;

    GUITableState perlinParameterTable;
    SerializedProperty perlinParameters;

    SerializedProperty voronoiPeaks;
    SerializedProperty voronoiMinHeight;
    SerializedProperty voronoiMaxHeight;
    SerializedProperty voronoiFallOff;
    SerializedProperty voronoiDropOff;
    SerializedProperty voronoiType;

    SerializedProperty mpdMinHeight;
    SerializedProperty mpdMaxHeight;
    SerializedProperty mpdDampenerPower;
    SerializedProperty mpdRoughness;

    SerializedProperty smoothAmount;

    GUITableState splatMapTable;
    SerializedProperty splatHeights;

    // folds out -----
    bool showRandom = false;
    bool showLoadHeights = false;
    bool showPerlin = false;
    bool showMultiplePerlin = false;
    bool showVoronoi = false;
    bool showMPD = false;
    bool showSmooth = false;
    bool showSplatMaps = false;

    void OnEnable()
    {
        resetTerrain = serializedObject.FindProperty("resetTerrain");

        randomHeightRange = serializedObject.FindProperty("randomHeightRange");
        heightMapScale = serializedObject.FindProperty("heightMapScale");
        heightMapImage = serializedObject.FindProperty("heightMapImage");
        perlinXScale = serializedObject.FindProperty("perlinXScale");
        perlinYScale = serializedObject.FindProperty("perlinYScale");
        perlinXOffset = serializedObject.FindProperty("perlinXOffset");
        perlinYOffset = serializedObject.FindProperty("perlinYOffset");
        perlinOctaves = serializedObject.FindProperty("perlinOctaves");
        perlinPersistance = serializedObject.FindProperty("perlinPersistance");
        perlinHeightScale = serializedObject.FindProperty("perlinHeightScale");

        perlinParameterTable = new GUITableState("perlinParametersTable");
        perlinParameters = serializedObject.FindProperty("perlinParameters");

        voronoiPeaks = serializedObject.FindProperty("voronoiPeaks");
        voronoiMinHeight = serializedObject.FindProperty("voronoiMinHeight");
        voronoiMaxHeight = serializedObject.FindProperty("voronoiMaxHeight");
        voronoiFallOff = serializedObject.FindProperty("voronoiFallOff");
        voronoiDropOff = serializedObject.FindProperty("voronoiDropOff");
        voronoiType = serializedObject.FindProperty("voronoiType");

        mpdMinHeight = serializedObject.FindProperty("mpdMinHeight");
        mpdMaxHeight = serializedObject.FindProperty("mpdMaxHeight");
        mpdDampenerPower = serializedObject.FindProperty("mpdDampenerPower");
        mpdRoughness = serializedObject.FindProperty("mpdRoughness");

        smoothAmount = serializedObject.FindProperty("smoothAmount");

        splatMapTable = new GUITableState("splatMapTable");
        splatHeights = serializedObject.FindProperty("splatHeights");
    }

    // Display loop for the inspector gui
    public override void OnInspectorGUI() {
        serializedObject.Update();

        CustomTerrain terrain = (CustomTerrain)target; // `target` is linked to [1] "a link to class"
        // terrain.randomHeightRange = .. is possible, but we use serialization because otherwise editing
        // the code would reset the state and we're loosing whatever we setup in the inspect
        EditorGUILayout.PropertyField(resetTerrain);
        showRandom = EditorGUILayout.Foldout(showRandom, "Random"); // show foldout toggle
        if (showRandom)
        {
            GUILayout.Label("Set Heights Between Random Values", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(randomHeightRange);
            if (GUILayout.Button("Random Heights"))
            {
                terrain.RandomTerrain();
            }
        }
        showLoadHeights = EditorGUILayout.Foldout(showLoadHeights, "Load Heights");
        if (showLoadHeights)
        {
            GUILayout.Label("Load Heights From Texture", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(heightMapImage);
            EditorGUILayout.PropertyField(heightMapScale);
            if (GUILayout.Button("Load Texture"))
            {
                terrain.LoadTexture();
            }
        }
        showPerlin = EditorGUILayout.Foldout(showPerlin, "Single Perlin Noise"); // show foldout toggle
        if (showPerlin)
        {
            EditorGUILayout.Slider(perlinXScale, 0, 0.1f, new GUIContent("X Scale"));
            EditorGUILayout.Slider(perlinYScale, 0, 0.1f, new GUIContent("Y Scale"));
            EditorGUILayout.IntSlider(perlinXOffset, 0, 10000, new GUIContent("Offset X"));
            EditorGUILayout.IntSlider(perlinYOffset, 0, 10000, new GUIContent("Offset Y"));
            EditorGUILayout.IntSlider(perlinOctaves, 1, 10, new GUIContent("Octaves"));
            EditorGUILayout.Slider(perlinPersistance, 0.1f, 10, new GUIContent("Persistance"));
            EditorGUILayout.Slider(perlinHeightScale, 0, 1, new GUIContent("Height Scale"));

            if (GUILayout.Button("Generate"))
            {
                terrain.Perlin();
            }
        }
        showMultiplePerlin = EditorGUILayout.Foldout(showMultiplePerlin, "Multple Perlin Noise"); // show foldout toggle
        if (showMultiplePerlin) {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            perlinParameterTable = GUITableLayout.DrawTable(perlinParameterTable,
                serializedObject.FindProperty("perlinParameters")); // why not the class attribute we mapped?
            GUILayout.Space(40);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewPerlin();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemovePerlin();
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Apply Multiple Perlin"))
            {
                terrain.MultiplePerlinTerrain();
            }
        }
        showVoronoi = EditorGUILayout.Foldout(showVoronoi, "Voronoi");
        if (showVoronoi)
        {
            EditorGUILayout.IntSlider(voronoiPeaks, 1, 10, new GUIContent("Peak Count"));
            EditorGUILayout.Slider(voronoiFallOff, 0, 10, new GUIContent("Falloff"));
            EditorGUILayout.Slider(voronoiDropOff, 0, 10, new GUIContent("Dropoff"));
            EditorGUILayout.Slider(voronoiMinHeight, 0, 1, new GUIContent("Min Height"));
            EditorGUILayout.Slider(voronoiMaxHeight, 0, 1, new GUIContent("Max Height"));
            EditorGUILayout.PropertyField(voronoiType);

            if (GUILayout.Button("Voronoi"))
            {
                terrain.Voronoi();
            }
        }
        showMPD = EditorGUILayout.Foldout(showMPD, "Midpoint Displacement");
        if (showMPD)
        {
            EditorGUILayout.PropertyField(mpdMinHeight);
            EditorGUILayout.PropertyField(mpdMaxHeight);
            EditorGUILayout.PropertyField(mpdDampenerPower);
            EditorGUILayout.PropertyField(mpdRoughness);

            if (GUILayout.Button("MPD"))
            {
                terrain.MidPointDisplacement();
            }
        }
        showSplatMaps = EditorGUILayout.Foldout(showSplatMaps, "Splat Maps");
        if (showSplatMaps)
        {
            perlinParameterTable = GUITableLayout.DrawTable(splatMapTable,
                serializedObject.FindProperty("splatHeights")); // why not the class attribute we mapped?
            GUILayout.Space(40);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewSplatHeight();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemoveSplatHeight();
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Apply SplatMaps"))
            {
                terrain.SplatMaps();
            }
        }
        showSmooth = EditorGUILayout.Foldout(showSmooth, "Smooth");
        if (showSmooth)
        {
            EditorGUILayout.IntSlider(smoothAmount, 1, 10, new GUIContent("Smooth amount"));
            if (GUILayout.Button("Smooth"))
            {
                terrain.SmoothN();
            }
        }

        if (GUILayout.Button("Reset Terrain"))
        {
            terrain.ResetTerrain();
        }
        serializedObject.ApplyModifiedProperties();
    }
   
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
