using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class AutoClothingGenerator : EditorWindow
{
    private enum SelectionMode { Single, Multiple }
    private SelectionMode selectionMode = SelectionMode.Multiple;

    private string textureFolderPath = "Assets";
    private string singleTexturePath = "";
    private Mesh selectedMesh;
    private AnimationClip selectedAnimation1;
    private AnimationClip selectedAnimation2;

    private enum ClothingType { Shirt, Pants }
    private ClothingType clothingType = ClothingType.Shirt;

    [MenuItem("Tools/Clothing Generator")]
    public static void ShowWindow()
    {
        GetWindow<AutoClothingGenerator>("Clothing Generator");
    }

    private void OnEnable()
    {
        textureFolderPath = EditorPrefs.GetString("ClothingGen_TextureFolder", "Assets");
        singleTexturePath = EditorPrefs.GetString("ClothingGen_SingleTexture", "");
        clothingType = (ClothingType)EditorPrefs.GetInt("ClothingGen_Type", 0);
        selectedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(EditorPrefs.GetString("ClothingGen_MeshPath", ""));
        selectedAnimation1 = AssetDatabase.LoadAssetAtPath<AnimationClip>(EditorPrefs.GetString("ClothingGen_Anim1Path", ""));
        selectedAnimation2 = AssetDatabase.LoadAssetAtPath<AnimationClip>(EditorPrefs.GetString("ClothingGen_Anim2Path", ""));
        selectionMode = (SelectionMode)EditorPrefs.GetInt("ClothingGen_SelectionMode", 1);
    }

    private void OnDisable()
    {
        EditorPrefs.SetString("ClothingGen_TextureFolder", textureFolderPath);
        EditorPrefs.SetString("ClothingGen_SingleTexture", singleTexturePath);
        EditorPrefs.SetInt("ClothingGen_Type", (int)clothingType);
        EditorPrefs.SetString("ClothingGen_MeshPath", AssetDatabase.GetAssetPath(selectedMesh));
        EditorPrefs.SetString("ClothingGen_Anim1Path", AssetDatabase.GetAssetPath(selectedAnimation1));
        EditorPrefs.SetString("ClothingGen_Anim2Path", AssetDatabase.GetAssetPath(selectedAnimation2));
        EditorPrefs.SetInt("ClothingGen_SelectionMode", (int)selectionMode);
    }

    private void OnGUI()
    {
        GUILayout.Label("Clothing Generator Tool", EditorStyles.boldLabel);

        selectionMode = (SelectionMode)EditorGUILayout.EnumPopup("Generation Mode", selectionMode);

        if (selectionMode == SelectionMode.Multiple)
        {
            textureFolderPath = EditorGUILayout.TextField("PNG Folder", textureFolderPath);
            if (GUILayout.Button("Browse Folder"))
            {
                string selected = EditorUtility.OpenFolderPanel("Select PNG Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(selected) && selected.StartsWith(Application.dataPath))
                {
                    textureFolderPath = "Assets" + selected.Substring(Application.dataPath.Length);
                }
            }
        }
        else
        {
            Texture2D selectedTexture = (Texture2D)EditorGUILayout.ObjectField("PNG Texture", AssetDatabase.LoadAssetAtPath<Texture2D>(singleTexturePath), typeof(Texture2D), false);

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

        clothingType = (ClothingType)EditorGUILayout.EnumPopup("Clothing Type", clothingType);
        selectedMesh = (Mesh)EditorGUILayout.ObjectField("Custom Mesh (Optional)", selectedMesh, typeof(Mesh), false);
        selectedAnimation1 = (AnimationClip)EditorGUILayout.ObjectField("First Animation (Optional)", selectedAnimation1, typeof(AnimationClip), false);
        selectedAnimation2 = (AnimationClip)EditorGUILayout.ObjectField("Second Animation (Optional)", selectedAnimation2, typeof(AnimationClip), false);

        if (GUILayout.Button("Generate Clothing"))
        {
            if (selectionMode == SelectionMode.Multiple)
                GenerateAllClothingFromFolder();
            else
                GenerateClothingFromSingle();
        }
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
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(singleTexturePath);
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

        int itemLayer = LayerMask.NameToLayer("Item");
        int logicLayer = LayerMask.NameToLayer("Logic");

        string originalPath = AssetDatabase.GetAssetPath(selectedTexture);
        string fileName = Path.GetFileNameWithoutExtension(originalPath);

        string baseFolder = $"Assets/Clothing/{(clothingType == ClothingType.Shirt ? "Shirts" : "Pants")}/{fileName}";
        string imageName = clothingType == ClothingType.Shirt ? "shirt.png" : "pants.png";

        if (!AssetDatabase.IsValidFolder($"Assets/Clothing/{(clothingType == ClothingType.Shirt ? "Shirts" : "Pants")}"))
        {
            AssetDatabase.CreateFolder("Assets/Clothing", clothingType == ClothingType.Shirt ? "Shirts" : "Pants");
        }

        if (!AssetDatabase.IsValidFolder(baseFolder))
        {
            Directory.CreateDirectory(baseFolder);
            AssetDatabase.Refresh();
        }

        string newImagePath = $"{baseFolder}/{imageName}";
        File.Copy(originalPath, newImagePath, true);
        AssetDatabase.Refresh();

        var textureImporter = AssetImporter.GetAtPath(newImagePath) as TextureImporter;
        if (textureImporter != null)
        {
            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            AssetDatabase.ImportAsset(newImagePath);
        }

        Texture2D copiedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newImagePath);

        Material mat = new Material(Shader.Find("Standard"));
        mat.mainTexture = copiedTexture;
        mat.SetFloat("_Mode", 1);
        mat.SetOverrideTag("RenderType", "TransparentCutout");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        mat.SetFloat("_Cutoff", 0.5f);

        AssetDatabase.CreateAsset(mat, $"{baseFolder}/{fileName}_Mat.mat");

        GameObject clothingObj = selectedMesh
            ? new GameObject(fileName, typeof(MeshFilter), typeof(MeshRenderer))
            : GameObject.CreatePrimitive(PrimitiveType.Quad);

        clothingObj.name = fileName;
        clothingObj.layer = itemLayer;
        clothingObj.tag = "Item";

        if (selectedMesh)
        {
            clothingObj.GetComponent<MeshFilter>().sharedMesh = selectedMesh;
            clothingObj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            clothingObj.AddComponent<BoxCollider>();
        }
        else
        {
            clothingObj.GetComponent<Renderer>().sharedMaterial = mat;
        }

        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(clothingObj.transform);
        icon.transform.localPosition = new Vector3(0, 0, 0.5f);
        icon.transform.LookAt(clothingObj.transform);
        icon.transform.Rotate(0, 180, 90);
        icon.layer = itemLayer;
        icon.tag = "Item";

        if (selectedAnimation1 || selectedAnimation2)
        {
            GameObject animationObject = new GameObject("Animations");
            Animation anim = animationObject.AddComponent<Animation>();
            if (selectedAnimation1)
            {
                anim.AddClip(selectedAnimation1, selectedAnimation1.name);
                anim.Play(selectedAnimation1.name);
            }
            if (selectedAnimation2)
            {
                anim.AddClip(selectedAnimation2, selectedAnimation2.name);
            }

            animationObject.layer = logicLayer;
            animationObject.tag = "Logic";

            string animPrefabPath = AssetDatabase.GenerateUniqueAssetPath($"{baseFolder}/Animations.prefab");
            PrefabUtility.SaveAsPrefabAsset(animationObject, animPrefabPath);
            DestroyImmediate(animationObject);
        }

        string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{baseFolder}/Item.prefab");
        PrefabUtility.SaveAsPrefabAsset(clothingObj, prefabPath);
        DestroyImmediate(clothingObj);
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
            if (sp.stringValue == layerName) { layerExists = true; break; }
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
