using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnemyAnimationGenerator
{
    private const string IdleSheet =
        "Assets/Enemy/Golems_Free_Version/Golems_Free_Version/Golem_1/Blue/No_Swoosh_VFX/Golem_1_idle.png";

    private const string WalkSheet =
        "Assets/Enemy/Golems_Free_Version/Golems_Free_Version/Golem_1/Blue/No_Swoosh_VFX/Golem_1_walk.png";

    private const string AnimationFolder = "Assets/Enemy/Animations";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Enemy/Generate Golem Animations")]
    public static void Generate()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Enemy enemy = UnityEngine.Object.FindFirstObjectByType<Enemy>();
        if (enemy == null)
            throw new InvalidOperationException("No Enemy component was found in SampleScene.");

        SpriteRenderer renderer = enemy.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(candidate => candidate.sprite != null);
        if (renderer == null)
            throw new InvalidOperationException("The Enemy has no child SpriteRenderer with a sprite.");

        string rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, enemy.transform);
        AnimationClip idle = CreateClip(IdleSheet, $"{AnimationFolder}/Enemy_Idle.anim", rendererPath, 8f);
        AnimationClip walk = CreateClip(WalkSheet, $"{AnimationFolder}/Enemy_Walk.anim", rendererPath, 10f);
        AnimatorController controller = CreateController(idle, walk);

        Animator animator = enemy.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        SerializedObject serializedEnemy = new SerializedObject(enemy);
        serializedEnemy.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        serializedEnemy.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(enemy);
        EditorUtility.SetDirty(animator);
        EditorSceneManager.MarkSceneDirty(enemy.gameObject.scene);
        EditorSceneManager.SaveScene(enemy.gameObject.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Generated Enemy_Idle, Enemy_Walk and Enemy.controller successfully.");
    }

    private static AnimationClip CreateClip(
        string sheetPath,
        string clipPath,
        string rendererPath,
        float frameRate)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            .OrderBy(sprite => GetFrameNumber(sprite.name))
            .ToArray();

        if (sprites.Length == 0)
            throw new InvalidOperationException($"No sliced sprites were found at {sheetPath}.");

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = System.IO.Path.GetFileNameWithoutExtension(clipPath) };
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = frameRate;
        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = rendererPath,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] frames = sprites
            .Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / frameRate,
                value = sprite
            })
            .ToArray();

        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateController(AnimationClip idle, AnimationClip walk)
    {
        string controllerPath = $"{AnimationFolder}/Enemy.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = FindOrCreateState(stateMachine, "Enemy_Idle");
        AnimatorState walkState = FindOrCreateState(stateMachine, "Enemy_Walk");
        idleState.motion = idle;
        walkState.motion = walk;
        stateMachine.defaultState = idleState;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState FindOrCreateState(AnimatorStateMachine stateMachine, string stateName)
    {
        AnimatorState existing = stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state.name == stateName);
        return existing != null ? existing : stateMachine.AddState(stateName);
    }

    private static int GetFrameNumber(string spriteName)
    {
        int underscoreIndex = spriteName.LastIndexOf('_');
        return underscoreIndex >= 0 && int.TryParse(spriteName[(underscoreIndex + 1)..], out int frame)
            ? frame
            : int.MaxValue;
    }
}
