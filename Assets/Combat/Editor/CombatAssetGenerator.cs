using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CombatAssetGenerator
{
    private const string Folder = "Assets/Combat/Prefabs";

    [MenuItem("Tools/Combat/Create Default Prefabs")]
    public static void CreateAssets()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        Sprite orb = CreateSprite("SpellOrb", 32, 32, true);
        Sprite wand = CreateSprite("Wand", 16, 32, false);
        AnimatorController weaponController = CreateWeaponController();

        string spellPath = Folder + "/SpellProjectile.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(spellPath) == null)
        {
            GameObject spell = new GameObject("SpellProjectile");
            SpriteRenderer renderer = spell.AddComponent<SpriteRenderer>();
            renderer.sprite = orb;
            renderer.sortingOrder = 6;
            SetSpriteMaterial(renderer);
            Rigidbody2D body = spell.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            CircleCollider2D collider = spell.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.2f;
            spell.AddComponent<SpellProjectile>();
            PrefabUtility.SaveAsPrefabAsset(spell, spellPath);
            Object.DestroyImmediate(spell);
        }

        string weaponPath = Folder + "/WeaponPickup.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(weaponPath) == null)
        {
            GameObject weapon = new GameObject("WeaponPickup");
            SpriteRenderer renderer = weapon.AddComponent<SpriteRenderer>();
            renderer.sprite = wand;
            renderer.sortingOrder = 5;
            SetSpriteMaterial(renderer);
            BoxCollider2D collider = weapon.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.65f, 1.1f);
            Weapon component = weapon.AddComponent<Weapon>();
            weapon.GetComponent<Animator>().runtimeAnimatorController = weaponController;
            SerializedObject settings = new SerializedObject(component);
            settings.FindProperty("spellPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(spellPath).GetComponent<SpellProjectile>();
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(weapon, weaponPath);
            Object.DestroyImmediate(weapon);
        }
        else
        {
            GameObject weapon = PrefabUtility.LoadPrefabContents(weaponPath);
            try
            {
                Animator animator = weapon.GetComponent<Animator>();
                bool changed = animator == null;
                if (animator == null)
                    animator = weapon.AddComponent<Animator>();
                if (animator.runtimeAnimatorController == null)
                {
                    animator.runtimeAnimatorController = weaponController;
                    changed = true;
                }
                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(weapon, weaponPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(weapon);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Combat prefabs ready: " + Folder);
    }

    private static AnimatorController CreateWeaponController()
    {
        string animationFolder = "Assets/Combat/Animations";
        Directory.CreateDirectory(animationFolder);
        AssetDatabase.Refresh();

        AnimationClip idle = CreateWeaponClip(animationFolder + "/Weapon_Idle.anim", "Weapon_Idle", AnimationCurve.Constant(0f, 1f, 0f), true);
        AnimationCurve castPose = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.04f, -55f),
            new Keyframe(0.10f, -55f), new Keyframe(0.15f, 0f));
        AnimationClip cast = CreateWeaponClip(animationFolder + "/Weapon_Cast.anim", "Weapon_Cast", castPose, false);

        string path = animationFolder + "/Weapon.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idleState = machine.AddState("Weapon_Idle", new Vector3(250f, 80f));
            idleState.motion = idle;
            AnimatorState castState = machine.AddState("Weapon_Cast", new Vector3(250f, 160f));
            castState.motion = cast;
            machine.defaultState = idleState;
        }
        return controller;
    }

    private static AnimationClip CreateWeaponClip(string path, string clipName, AnimationCurve pose, bool loop)
    {
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null)
            return existing;

        AnimationClip clip = new AnimationClip { name = clipName, frameRate = 60f };
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw.z"), pose);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static Sprite CreateSprite(string spriteName, int width, int height, bool orb)
    {
        string path = Folder + "/" + spriteName + ".png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null)
            return existing;
        if (!File.Exists(path))
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color color = Color.clear;
                if (orb)
                {
                    float radius = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    if (radius < 14f)
                        color = radius > 11f ? new Color(0.1f, 0.4f, 0.9f) : Color.Lerp(new Color(0.2f, 0.7f, 1f), Color.white, 1f - radius / 11f);
                }
                else
                {
                    if (x >= 6 && x <= 9 && y >= 1 && y <= 23)
                        color = new Color(0.45f, 0.25f, 0.1f);
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 25f)) < 6f)
                        color = new Color(0.2f, 0.8f, 1f);
                }
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void SetSpriteMaterial(SpriteRenderer renderer)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        if (material != null)
            renderer.sharedMaterial = material;
    }
}
