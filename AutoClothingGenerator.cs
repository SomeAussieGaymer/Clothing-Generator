using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class AutoClothingGenerator : EditorWindow
{
    private enum ClothingType { Shirt, Pants, Vest, Hat, Glasses, Mask, Backpack }
    private ClothingType clothingType = ClothingType.Shirt;
    private enum SelectionMode { Single, Multiple }
    private SelectionMode selectionMode = SelectionMode.Multiple;
    private string textureFolderPath = "Assets";
    private string singleTexturePath = "";
    private Mesh selectedMesh;
    private AnimationClip equipAnimation;
    private AnimationClip useAnimation;
    private PreviewRenderUtility previewRenderUtility;
    private GameObject previewInstance;
    private Material previewMaterial;
    private Vector2 previewEulerAngles = new Vector2(30f, 0f); 
    private const string PREFS_PREFIX = "ClothingGen_";
    private const string BASE_FOLDER_PATH = "Assets/Clothing";
    private static readonly int MATERIAL_MODE = Shader.PropertyToID("_Mode");
    private static readonly int MATERIAL_CUTOFF = Shader.PropertyToID("_Cutoff");
    private static readonly int MATERIAL_SRC_BLEND = Shader.PropertyToID("_SrcBlend");
    private static readonly int MATERIAL_DST_BLEND = Shader.PropertyToID("_DstBlend");
    private static readonly int MATERIAL_ZWRITE = Shader.PropertyToID("_ZWrite");

    [MenuItem("Tools/Clothing Generator")]
    public static void ShowWindow()
    {
        GetWindow<AutoClothingGenerator>("Clothing Generator");
    }

    private void OnEnable()
    {
        textureFolderPath = EditorPrefs.GetString($"{PREFS_PREFIX}TextureFolder", "Assets");
        singleTexturePath = EditorPrefs.GetString($"{PREFS_PREFIX}SingleTexture", "");
        clothingType = (ClothingType)EditorPrefs.GetInt($"{PREFS_PREFIX}Type", 0);
        selectedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(EditorPrefs.GetString($"{PREFS_PREFIX}MeshPath", ""));
        equipAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>(EditorPrefs.GetString($"{PREFS_PREFIX}EquipAnimPath", ""));
        useAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>(EditorPrefs.GetString($"{PREFS_PREFIX}UseAnimPath", ""));
        selectionMode = (SelectionMode)EditorPrefs.GetInt($"{PREFS_PREFIX}SelectionMode", 1);
        previewRenderUtility = new PreviewRenderUtility();
        previewRenderUtility.cameraFieldOfView = 60f;
    }

    private void OnDisable()
    {
        EditorPrefs.SetString($"{PREFS_PREFIX}TextureFolder", textureFolderPath);
        EditorPrefs.SetString($"{PREFS_PREFIX}SingleTexture", singleTexturePath);
        EditorPrefs.SetInt($"{PREFS_PREFIX}Type", (int)clothingType);
        EditorPrefs.SetString($"{PREFS_PREFIX}MeshPath", AssetDatabase.GetAssetPath(selectedMesh));
        EditorPrefs.SetString($"{PREFS_PREFIX}EquipAnimPath", AssetDatabase.GetAssetPath(equipAnimation));
        EditorPrefs.SetString($"{PREFS_PREFIX}UseAnimPath", AssetDatabase.GetAssetPath(useAnimation));
        EditorPrefs.SetInt($"{PREFS_PREFIX}SelectionMode", (int)selectionMode);

        if (previewRenderUtility != null)
        {
            previewRenderUtility.Cleanup();
            previewRenderUtility = null;
        }
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
        }
        if (previewMaterial != null)
        {
            DestroyImmediate(previewMaterial);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Clothing Generator Tool", EditorStyles.boldLabel);
        clothingType = (ClothingType)EditorGUILayout.EnumPopup("Item Type", clothingType);
        selectionMode = (SelectionMode)EditorGUILayout.EnumPopup("Selection Mode", selectionMode);
        switch (selectionMode)
        {
            case SelectionMode.Multiple:
                DisplayMultipleModeControls();
                break;
            case SelectionMode.Single:
                DisplaySingleModeControls();
                break;
        }

        selectedMesh = (Mesh)EditorGUILayout.ObjectField("Item", selectedMesh, typeof(Mesh), false);
        equipAnimation = (AnimationClip)EditorGUILayout.ObjectField("Equip Animation", equipAnimation, typeof(AnimationClip), false);
        useAnimation = (AnimationClip)EditorGUILayout.ObjectField("Use Animation", useAnimation, typeof(AnimationClip), false);

        if (selectionMode == SelectionMode.Single)
        {
            GUILayout.Space(10);
            GUILayout.Label("3D Preview (Drag to Rotate)", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Preview shows actual material settings as they will appear in-game", MessageType.Info);

            Rect previewRect = GUILayoutUtility.GetRect(300, 300, GUILayout.ExpandWidth(true));
            HandlePreviewDrag(previewRect);
            DrawPreview(previewRect);
        }

        if (GUILayout.Button("Generate Item"))
        {
            switch (selectionMode)
            {
                case SelectionMode.Multiple:
                    GenerateAllClothingFromFolder();
                    break;
                case SelectionMode.Single:
                    GenerateClothingFromSingle();
                    break;
            }
        }
    }

    private void DisplayMultipleModeControls()
    {
        textureFolderPath = EditorGUILayout.TextField("Texture Folder", textureFolderPath);
        if (GUILayout.Button("Browse Folder"))
        {
            string selected = EditorUtility.OpenFolderPanel("Select PNG Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selected) && selected.StartsWith(Application.dataPath))
            {
                textureFolderPath = "Assets" + selected.Substring(Application.dataPath.Length);
            }
        }
    }

    private void DisplaySingleModeControls()
    {
        Texture2D selectedTexture = (Texture2D)EditorGUILayout.ObjectField("Texture",
            AssetDatabase.LoadAssetAtPath<Texture2D>(singleTexturePath), typeof(Texture2D), false);
        if (selectedTexture != null)
        {
            singleTexturePath = AssetDatabase.GetAssetPath(selectedTexture);
        }

        if (GUILayout.Button("Browse Texture"))
        {
            string selected = EditorUtility.OpenFilePanel("Select PNG File", Application.dataPath, "png");
            if (!string.IsNullOrEmpty(selected) && selected.StartsWith(Application.dataPath))
            {
                singleTexturePath = "Assets" + selected.Substring(Application.dataPath.Length);
            }
        }
    }

    private void HandlePreviewDrag(Rect rect)
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDrag && rect.Contains(e.mousePosition))
        {
            previewEulerAngles += e.delta;
            e.Use();
            Repaint();
        }
    }

    private void DrawPreview(Rect rect)
    {
        Texture2D previewTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(singleTexturePath);
        if (previewTexture == null)
        {
            EditorGUI.LabelField(rect, "No texture selected for preview.");
            return;
        }

        Mesh previewMesh = selectedMesh != null ? selectedMesh : Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        if (previewMesh == null)
        {
            EditorGUI.LabelField(rect, "No mesh available for preview.");
            return;
        }

        UpdatePreviewMaterial(previewTexture);

        SetupPreviewInstance(previewMesh, previewMaterial);


        previewRenderUtility.BeginPreview(rect, GUIStyle.none);


        previewRenderUtility.camera.transform.position = new Vector3(0, 0, -3);
        previewRenderUtility.camera.transform.rotation = Quaternion.identity;
        previewRenderUtility.camera.nearClipPlane = 0.1f;
        previewRenderUtility.camera.farClipPlane = 100f;

       
        previewRenderUtility.camera.clearFlags = CameraClearFlags.Color;
        previewRenderUtility.camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1.0f); 

   
        previewRenderUtility.lights[0].type = LightType.Directional;
        previewRenderUtility.lights[0].intensity = 1.4f;
        previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);
        previewRenderUtility.lights[0].color = Color.white;

        previewRenderUtility.lights[1].type = LightType.Point;
        previewRenderUtility.lights[1].intensity = 0.7f;
        previewRenderUtility.lights[1].transform.position = new Vector3(0, -3, -2);
        previewRenderUtility.lights[1].color = new Color(0.7f, 0.7f, 0.8f); 

 
        Quaternion objectRotation = Quaternion.Euler(previewEulerAngles.y, previewEulerAngles.x, 0);

        if (EditorApplication.timeSinceStartup % 10 < 5) 
        {
            float angle = Mathf.Sin((float)(EditorApplication.timeSinceStartup * 0.5f)) * 5f;
            objectRotation *= Quaternion.Euler(0, angle, 0);
        }

        previewRenderUtility.DrawMesh(previewMesh, Matrix4x4.TRS(Vector3.zero, objectRotation, Vector3.one), previewMaterial, 0);
        previewRenderUtility.camera.Render();

        Texture resultRender = previewRenderUtility.EndPreview();
        GUI.DrawTexture(rect, resultRender, ScaleMode.StretchToFill, false);


        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), new Color(0.3f, 0.3f, 0.3f));
        EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), new Color(0.3f, 0.3f, 0.3f));
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), new Color(0.3f, 0.3f, 0.3f));
        EditorGUI.DrawRect(new Rect(rect.x + rect.width - 1, rect.y, 1, rect.height), new Color(0.3f, 0.3f, 0.3f));


        ClothingTypeInfo typeInfo = GetClothingTypeInfo(clothingType);
        string previewLabel = $"{typeInfo.FolderName} Preview";
        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.normal.textColor = Color.white;
        labelStyle.alignment = TextAnchor.LowerCenter;
        EditorGUI.DropShadowLabel(new Rect(rect.x, rect.y + rect.height - 20, rect.width, 20), previewLabel, labelStyle);
    }

    private void UpdatePreviewMaterial(Texture2D texture)
    {
     
        if (previewMaterial == null)
        {
            previewMaterial = new Material(Shader.Find("Standard"));
        }

        previewMaterial.mainTexture = texture;
        previewMaterial.SetFloat(MATERIAL_MODE, 1); 
        previewMaterial.SetOverrideTag("RenderType", "TransparentCutout");
        previewMaterial.SetInt(MATERIAL_SRC_BLEND, (int)UnityEngine.Rendering.BlendMode.One);
        previewMaterial.SetInt(MATERIAL_DST_BLEND, (int)UnityEngine.Rendering.BlendMode.Zero);
        previewMaterial.SetInt(MATERIAL_ZWRITE, 1);
        previewMaterial.DisableKeyword("_ALPHABLEND_ON");
        previewMaterial.EnableKeyword("_ALPHATEST_ON");
        previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        previewMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        previewMaterial.SetFloat(MATERIAL_CUTOFF, 0.5f);

       
        previewMaterial.EnableKeyword("_EMISSION");
        previewMaterial.SetColor("_EmissionColor", Color.white * 0.1f); 
        previewMaterial.SetFloat("_Glossiness", 0.2f); 
    }

    private void SetupPreviewInstance(Mesh mesh, Material material)
    {
        if (previewInstance == null)
        {
            previewInstance = new GameObject("PreviewInstance");
        }

        MeshFilter mf = previewInstance.GetComponent<MeshFilter>();
        if (mf == null)
            mf = previewInstance.AddComponent<MeshFilter>();

        MeshRenderer mr = previewInstance.GetComponent<MeshRenderer>();
        if (mr == null)
            mr = previewInstance.AddComponent<MeshRenderer>();

        mf.sharedMesh = mesh;
        mr.sharedMaterial = material;
    }

    private void GenerateAllClothingFromFolder()
    {
        string[] pngPaths = Directory.GetFiles(textureFolderPath, "*.png");
        if (pngPaths.Length == 0)
        {
            Debug.LogWarning("No PNG files found in selected folder.");
            return;
        }

        foreach (var path in pngPaths)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
                GenerateClothing(tex);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ Generated {pngPaths.Length} clothing item(s).");
    }

    private void GenerateClothingFromSingle()
    {
        if (string.IsNullOrEmpty(singleTexturePath))
        {
            Debug.LogWarning("No PNG file selected.");
            return;
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(singleTexturePath);
        if (tex == null)
        {
            Debug.LogWarning("Selected PNG could not be loaded.");
            return;
        }

        GenerateClothing(tex);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ Generated single clothing item.");
    }

    private void GenerateClothing(Texture2D selectedTexture)
    {
   
        EnsureTagExists("Item");
        EnsureLayerExists("Item");
        EnsureTagExists("Logic");
        EnsureLayerExists("Logic");

    
        if (RequiresEnemyTagLayer(clothingType))
        {
            EnsureTagExists("Enemy");
            EnsureLayerExists("Enemy");
        }

        int itemLayer = LayerMask.NameToLayer("Item");
        int logicLayer = LayerMask.NameToLayer("Logic");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        string originalPath = AssetDatabase.GetAssetPath(selectedTexture);
        string fileName = Path.GetFileNameWithoutExtension(originalPath);

        ClothingTypeInfo typeInfo = GetClothingTypeInfo(clothingType);
        string baseFolder = $"{BASE_FOLDER_PATH}/{typeInfo.FolderName}/{fileName}";

    
        CreateFolderStructure(typeInfo.FolderName, fileName);

        string newImagePath = $"{baseFolder}/{typeInfo.ImageName}";
        CopyAndConfigureTexture(originalPath, newImagePath);

        Texture2D copiedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newImagePath);


        Material mat = CreateMaterial(copiedTexture, $"{baseFolder}/{fileName}_Mat.mat");

    
        GameObject clothingObj = CreateClothingObject(fileName, itemLayer, mat);
        string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{baseFolder}/Item.prefab");
        PrefabUtility.SaveAsPrefabAsset(clothingObj, prefabPath);
        DestroyImmediate(clothingObj);

  
        CreateAnimationPrefab(baseFolder, logicLayer);

        
        if (RequiresEnemyTagLayer(clothingType))
        {
            CreateSpecialTypeItem(clothingType, fileName, enemyLayer, mat, baseFolder);
        }
    }

    private bool RequiresEnemyTagLayer(ClothingType type)
    {
        switch (type)
        {
            case ClothingType.Vest:
            case ClothingType.Hat:
            case ClothingType.Glasses:
            case ClothingType.Mask:
            case ClothingType.Backpack:
                return true;
            default:
                return false;
        }
    }

    private struct ClothingTypeInfo
    {
        public string FolderName;
        public string ImageName;

        public ClothingTypeInfo(string folderName, string imageName)
        {
            FolderName = folderName;
            ImageName = imageName;
        }
    }

    private ClothingTypeInfo GetClothingTypeInfo(ClothingType type)
    {
        switch (type)
        {
            case ClothingType.Shirt:
                return new ClothingTypeInfo("Shirts", "shirt.png");
            case ClothingType.Pants:
                return new ClothingTypeInfo("Pants", "pants.png");
            case ClothingType.Vest:
                return new ClothingTypeInfo("Vests", "vest.png");
            case ClothingType.Hat:
                return new ClothingTypeInfo("Hats", "Hat.png");
            case ClothingType.Glasses:
                return new ClothingTypeInfo("Glasses", "Glasses.png");
            case ClothingType.Mask:
                return new ClothingTypeInfo("Masks", "Mask.png");
            case ClothingType.Backpack:
                return new ClothingTypeInfo("Backpacks", "Backpack.png");
            default:
                return new ClothingTypeInfo("Items", "item.png");
        }
    }

    private void CreateFolderStructure(string typeFolder, string fileName)
    {
        if (!AssetDatabase.IsValidFolder(BASE_FOLDER_PATH))
        {
            string parentFolder = Path.GetDirectoryName(BASE_FOLDER_PATH);
            string baseFolderName = Path.GetFileName(BASE_FOLDER_PATH);
            AssetDatabase.CreateFolder(parentFolder, baseFolderName);
        }

        if (!AssetDatabase.IsValidFolder($"{BASE_FOLDER_PATH}/{typeFolder}"))
        {
            AssetDatabase.CreateFolder(BASE_FOLDER_PATH, typeFolder);
        }

        string baseFolder = $"{BASE_FOLDER_PATH}/{typeFolder}/{fileName}";
        if (!AssetDatabase.IsValidFolder(baseFolder))
        {
            Directory.CreateDirectory(baseFolder);
            AssetDatabase.Refresh();
        }
    }

    private void CopyAndConfigureTexture(string originalPath, string newPath)
    {
        File.Copy(originalPath, newPath, true);
        AssetDatabase.Refresh();

        TextureImporter textureImporter = AssetImporter.GetAtPath(newPath) as TextureImporter;
        if (textureImporter != null)
        {
            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            AssetDatabase.ImportAsset(newPath);
        }
    }

    private Material CreateMaterial(Texture2D texture, string assetPath)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.mainTexture = texture;
        mat.SetFloat(MATERIAL_MODE, 1);
        mat.SetOverrideTag("RenderType", "TransparentCutout");
        mat.SetInt(MATERIAL_SRC_BLEND, (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt(MATERIAL_DST_BLEND, (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt(MATERIAL_ZWRITE, 1);
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        mat.SetFloat(MATERIAL_CUTOFF, 0.5f);

        AssetDatabase.CreateAsset(mat, assetPath);
        return mat;
    }

    private GameObject CreateClothingObject(string name, int layer, Material material)
    {
        GameObject obj = selectedMesh != null
            ? new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer))
            : GameObject.CreatePrimitive(PrimitiveType.Quad);

        obj.name = name;
        obj.layer = layer;
        obj.tag = "Item";

        if (selectedMesh != null)
        {
            obj.GetComponent<MeshFilter>().sharedMesh = selectedMesh;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            obj.AddComponent<BoxCollider>();
        }
        else
        {
            obj.GetComponent<Renderer>().sharedMaterial = material;
        }

        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(obj.transform);
        icon.transform.localPosition = new Vector3(0, 0, 0.5f);
        icon.transform.localRotation = Quaternion.Euler(0f, 180f, 90f);
        icon.layer = layer;
        icon.tag = "Item";

        return obj;
    }

    private void CreateAnimationPrefab(string baseFolder, int logicLayer)
    {
        if (equipAnimation == null && useAnimation == null)
            return;

        GameObject animationObject = new GameObject("Animations");
        Animation anim = animationObject.AddComponent<Animation>();

        if (equipAnimation != null)
        {
            anim.AddClip(equipAnimation, "Equip");
            anim.Play("Equip");
        }

        if (useAnimation != null)
        {
            anim.AddClip(useAnimation, "Use");
        }

        animationObject.layer = logicLayer;
        animationObject.tag = "Logic";

        string animPrefabPath = AssetDatabase.GenerateUniqueAssetPath($"{baseFolder}/Animations.prefab");
        PrefabUtility.SaveAsPrefabAsset(animationObject, animPrefabPath);
        DestroyImmediate(animationObject);
    }

    private void CreateSpecialTypeItem(ClothingType type, string fileName, int enemyLayer, Material material, string baseFolder)
    {
        string typeName = type.ToString();

        GameObject specialObj = new GameObject(typeName);
        specialObj.tag = "Enemy";
        specialObj.layer = enemyLayer;

        BoxCollider boxCollider = specialObj.AddComponent<BoxCollider>();
        if (selectedMesh != null)
        {
            boxCollider.center = selectedMesh.bounds.center;
            boxCollider.size = selectedMesh.bounds.size;
        }

        GameObject modelChild = new GameObject("Model_0");
        modelChild.transform.SetParent(specialObj.transform);

        MeshFilter mf = modelChild.AddComponent<MeshFilter>();
        MeshRenderer mr = modelChild.AddComponent<MeshRenderer>();

        if (selectedMesh != null)
        {
            mf.sharedMesh = selectedMesh;
            mr.sharedMaterial = material;
        }

        string specialPrefabPath = AssetDatabase.GenerateUniqueAssetPath($"{baseFolder}/{typeName}.prefab");
        PrefabUtility.SaveAsPrefabAsset(specialObj, specialPrefabPath);
        DestroyImmediate(specialObj);
    }

    private void EnsureTagExists(string tag)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        if (!Enumerable.Range(0, tagsProp.arraySize).Any(i => tagsProp.GetArrayElementAtIndex(i).stringValue == tag))
        {
            tagsProp.InsertArrayElementAtIndex(0);
            tagsProp.GetArrayElementAtIndex(0).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }

    private void EnsureLayerExists(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        bool layerExists = false;

        for (int i = 8; i < layersProp.arraySize; i++)
        {
            SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
            if (sp.stringValue == layerName)
            {
                layerExists = true;
                break;
            }
        }

        if (!layerExists)
        {
            for (int i = 8; i < layersProp.arraySize; i++)
            {
                SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    break;
                }
            }
        }
    }
}
