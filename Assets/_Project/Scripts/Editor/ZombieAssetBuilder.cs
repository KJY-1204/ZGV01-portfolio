// 좀비 애니메이터 컨트롤러와 프리팹을 외부 에셋 위에 어댑터로 만들어 주는 에디터 도구다. 외부 에셋 원본은 고치지 않는다.
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ZombieAssetBuilder
{
    private const string AnimationDir = "Assets/_Project/Animation/Zombie";
    private const string CreepFbx = "Assets/Creep Horror Creature/Meshes/Creep_mesh.fbx";
    private const string LocomotionDir = "Assets/Survivalist/StarterAssets/ThirdPersonController/Character/Animations/";

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Animation")) AssetDatabase.CreateFolder("Assets/_Project", "Animation");
        if (!AssetDatabase.IsValidFolder(AnimationDir)) AssetDatabase.CreateFolder("Assets/_Project/Animation", "Zombie");
    }

    private static AnimationClip FindClip(string fbxPath, string clipName)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (asset is AnimationClip clip && clip.name == clipName) return clip;
        return null;
    }

    // 변이체 원본 클립은 루프가 꺼져 있어서 복사본을 만들어 필요한 것만 루프로 켠다.
    private static AnimationClip CopyCreepClip(string clipName, bool loop)
    {
        string path = AnimationDir + "/Mutant_" + clipName + ".anim";
        AssetDatabase.DeleteAsset(path);
        AnimationClip copy = Object.Instantiate(FindClip(CreepFbx, "Creep|" + clipName));
        copy.name = "Mutant_" + clipName;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(copy);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(copy, settings);
        AssetDatabase.CreateAsset(copy, path);
        return copy;
    }

    private static AnimatorStateTransition Link(AnimatorState from, AnimatorState to, float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        return transition;
    }

    private static AnimatorStateTransition LinkAny(AnimatorStateMachine machine, AnimatorState to)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.1f;
        transition.canTransitionToSelf = true;
        return transition;
    }

    private static AnimatorController CreateController(string name)
    {
        string path = AnimationDir + "/" + name + ".controller";
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveZ", AnimatorControllerParameterType.Float);
        controller.AddParameter("Alert", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackType", AnimatorControllerParameterType.Int);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);
        return controller;
    }

    // 정지 <-> 이동 블렌드 두 상태를 만든다.
    private static AnimatorState AddIdleAndMove(AnimatorController controller, AnimationClip idle, (AnimationClip clip, float speed)[] moves)
    {
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idleState = machine.AddState("Idle");
        idleState.motion = idle;
        machine.defaultState = idleState;

        AnimatorState moveState = controller.CreateBlendTreeInController("Move", out BlendTree tree);
        tree.blendType = BlendTreeType.Simple1D;
        tree.useAutomaticThresholds = false;
        tree.blendParameter = "Speed";
        foreach ((AnimationClip clip, float speed) move in moves) tree.AddChild(move.clip, move.speed);

        Link(idleState, moveState, 0.15f).AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");
        Link(moveState, idleState, 0.15f).AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");
        return idleState;
    }

    [MenuItem("ZGV01/Zombie/Build Controllers")]
    public static void BuildControllers()
    {
        EnsureFolders();
        BuildMutantController();
        BuildArcherController();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }


    // 이미 만든 변이체 컨트롤러/프리팹을 지우지 않고 제자리에서 대기를 정지 포즈 + 숨쉬기로 바꾼다(GUID 유지).
    [MenuItem("ZGV01/Zombie/Apply Mutant Idle")]
    public static void ApplyMutantIdle()
    {
        EnsureFolders();
        AnimationClip idle = CopyCreepClip("Idle_Pose", true);

        string controllerPath = AnimationDir + "/Mutant.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
        {
            if (child.state.name != "Idle") continue;
            child.state.motion = idle;
            EditorUtility.SetDirty(controller);
        }

        string prefabPath = PrefabDir + "/Zombie_Mutant.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root.GetComponent<ZombieBreathing>() == null) root.AddComponent<ZombieBreathing>();
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
    }

    private static void BuildMutantController()
    {
        // 대기는 정지 포즈(Idle_Pose)이고 숨쉬기는 ZombieBreathing이 가슴/어깨에 입힌다(Idle1은 턱 등이 계속 움직인다).
        AnimationClip idle = CopyCreepClip("Idle_Pose", true);
        AnimationClip walk1 = CopyCreepClip("Walk1_Action", true);
        AnimationClip walk2 = CopyCreepClip("Walk2_Action", true);
        AnimationClip punch = CopyCreepClip("Punch_Action", false);
        AnimationClip bite = CopyCreepClip("Bite_Action", false);
        AnimationClip roar = CopyCreepClip("Roar_Action", false);
        AnimationClip death = CopyCreepClip("Death_Action", false);

        AnimatorController controller = CreateController("Mutant");
        AnimatorState idleState = AddIdleAndMove(controller, idle, new[] { (walk1, 0.5f), (walk2, 3.5f) });
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        AnimatorState punchState = machine.AddState("Punch");
        punchState.motion = punch;
        AnimatorState biteState = machine.AddState("Bite");
        biteState.motion = bite;
        AnimatorState roarState = machine.AddState("Roar");
        roarState.motion = roar;
        AnimatorState deadState = machine.AddState("Dead");
        deadState.motion = death;

        AnimatorStateTransition toPunch = LinkAny(machine, punchState);
        toPunch.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        toPunch.AddCondition(AnimatorConditionMode.Equals, 0, "AttackType");
        AnimatorStateTransition toBite = LinkAny(machine, biteState);
        toBite.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        toBite.AddCondition(AnimatorConditionMode.Equals, 1, "AttackType");
        LinkAny(machine, roarState).AddCondition(AnimatorConditionMode.If, 0, "Alert");
        LinkAny(machine, deadState).AddCondition(AnimatorConditionMode.If, 0, "Dead");

        foreach (AnimatorState state in new[] { punchState, biteState, roarState })
        {
            AnimatorStateTransition back = Link(state, idleState, 0.2f);
            back.hasExitTime = true;
            back.exitTime = 0.4f;
        }
        EditorUtility.SetDirty(controller);
    }

    private const string PrefabDir = "Assets/_Project/Prefabs/Characters";

    private struct ZombieSpec
    {
        public string name;
        public string modelPrefab;
        public string controller;
        public float maxHealth, speed, attackDamage, attackRange, alertPause;
        public float colliderRadius, colliderHeight;
        public int attackVariants;
        public bool tipOverOnDeath, archer, strike;
        public float attackCooldown, detectionRange, turnSpeed;
    }

    private static void SetFields(Object target, params (string name, object value)[] fields)
    {
        SerializedObject so = new SerializedObject(target);
        foreach ((string name, object value) field in fields)
        {
            SerializedProperty property = so.FindProperty(field.name);
            switch (field.value)
            {
                case float f: property.floatValue = f; break;
                case int i: property.intValue = i; break;
                case bool b: property.boolValue = b; break;
                case Object o: property.objectReferenceValue = o; break;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("ZGV01/Zombie/Build Prefabs")]
    public static void BuildPrefabs()
    {
        BuildArcherParts();
        BuildPrefab(new ZombieSpec
        {
            name = "Zombie_Archer",
            modelPrefab = "Assets/NewPunch/ShirtlessZombieFree/Prefabs/ShirtlessZombie_FREE_URP.prefab",
            controller = AnimationDir + "/Archer.controller",
            maxHealth = 100f, speed = 1.8f, attackDamage = 12f, attackRange = 14f, alertPause = 0f,
            colliderRadius = 0.4f, colliderHeight = 1.8f, attackVariants = 1, tipOverOnDeath = true,
            archer = true, attackCooldown = 3f, detectionRange = 20f,
        });
        BuildPrefab(new ZombieSpec
        {
            name = "Zombie_Mutant",
            modelPrefab = "Assets/Creep Horror Creature/Prefabs/Creep1.prefab",
            controller = AnimationDir + "/Mutant.controller",
            maxHealth = 300f, speed = 3.5f, attackDamage = 20f, attackRange = 2.4f, alertPause = 2f,
            colliderRadius = 0.8f, colliderHeight = 2.4f, attackVariants = 2, tipOverOnDeath = false,
        });
        AssetDatabase.SaveAssets();
    }


    // 근접 좀비용 컨트롤러(이동만): 기존 컨트롤러와 프리팹은 건드리지 않고 새 것만 만든다.
    [MenuItem("ZGV01/Zombie/Build Melee Zombies")]
    public static void BuildMeleeZombies()
    {
        EnsureFolders();
        BuildWalkerController();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        BuildPrefab(new ZombieSpec
        {
            name = "Zombie_Walker",
            modelPrefab = "Assets/NewPunch/ShirtlessZombieFree/Prefabs/ShirtlessZombie_FREE_URP.prefab",
            controller = AnimationDir + "/Walker.controller",
            maxHealth = 80f, speed = 2f, attackDamage = 10f, attackRange = 1.7f, alertPause = 0f,
            colliderRadius = 0.4f, colliderHeight = 1.8f, attackVariants = 1, tipOverOnDeath = true,
            strike = true, attackCooldown = 1.3f, detectionRange = 14f, turnSpeed = 200f,
        });
        BuildPrefab(new ZombieSpec
        {
            name = "Zombie_Runner",
            modelPrefab = "Assets/NewPunch/ShirtlessZombieFree/Prefabs/ShirtlessZombie_FREE_URP.prefab",
            controller = AnimationDir + "/Walker.controller",
            maxHealth = 50f, speed = 4.4f, attackDamage = 8f, attackRange = 1.7f, alertPause = 0f,
            colliderRadius = 0.4f, colliderHeight = 1.8f, attackVariants = 1, tipOverOnDeath = true,
            strike = true, attackCooldown = 0.9f, detectionRange = 18f, turnSpeed = 360f,
        });
        AssetDatabase.SaveAssets();
    }

    // 휴머노이드 근접 좀비는 이동 클립만 있다(공격은 ZombieStrikeAnimation, 사망은 ZombieAnimator가 쓰러뜨린다).
    private static void BuildWalkerController()
    {
        AnimationClip idle = FindClip(LocomotionDir + "Stand--Idle.anim.fbx", "Idle");
        AnimationClip walk = FindClip(LocomotionDir + "Locomotion--Walk_N.anim.fbx", "Walk_N");
        AnimationClip run = FindClip(LocomotionDir + "Locomotion--Run_N.anim.fbx", "Run_N");
        AnimatorController controller = CreateController("Walker");
        AddIdleAndMove(controller, idle, new[] { (walk, 1.4f), (run, 4.4f) });
        EditorUtility.SetDirty(controller);
    }

    private static void BuildPrefab(ZombieSpec spec)
    {
        GameObject root = new GameObject(spec.name);
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(spec.modelPrefab), root.transform);
        model.name = "Model";
        Animator animator = model.GetComponentInChildren<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(spec.controller);
        animator.applyRootMotion = false;

        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.radius = spec.colliderRadius;
        capsule.height = spec.colliderHeight;
        capsule.center = new Vector3(0f, spec.colliderHeight * 0.5f, 0f);

        UnityEngine.AI.NavMeshAgent agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.radius = spec.colliderRadius;
        agent.height = spec.colliderHeight;
        agent.speed = spec.speed;
        agent.angularSpeed = spec.turnSpeed > 0f ? spec.turnSpeed : 180f;
        agent.acceleration = 10f;

        SetFields(root.AddComponent<Health>(), ("maxHealth", spec.maxHealth));
        SetFields(root.AddComponent<ZombieAI>(),
            ("attackDamage", spec.attackDamage), ("attackRange", spec.attackRange), ("alertPause", spec.alertPause),
            ("attackCooldown", spec.attackCooldown > 0f ? spec.attackCooldown : 1.2f), ("detectionRange", spec.detectionRange > 0f ? spec.detectionRange : 12f));
        SetFields(root.AddComponent<ZombieLootDropper>(),
            ("dropTable", AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Data/DropTables/DropTable_Zombie_Basic.asset")),
            ("lootPickupPrefab", AssetDatabase.LoadAssetAtPath<Object>(FindPrefabPath("LootPickup"))));
        ZombieSunBurnBuilder.Attach(root);
        SetFields(root.AddComponent<ZombieAudio>(),
            ("audioSet", AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Data/Zombies/ZombieAudioSet_Basic.asset")));
        SetFields(root.AddComponent<ZombieFootsteps>(),
            ("footstepSet", AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Data/Audio/FootstepSet_Zombie.asset")));
        if (spec.archer) AddBow(root, animator);
        if (spec.strike) SetFields(root.AddComponent<ZombieStrikeAnimation>(), ("animator", animator));

        SetFields(root.AddComponent<ZombieAnimator>(),
            ("animator", animator), ("attackVariants", spec.attackVariants), ("tipOverOnDeath", spec.tipOverOnDeath));

        SavePrefab(root, PrefabDir + "/" + spec.name + ".prefab");
    }

    // 기존 프리팹을 덮어쓰면 참조(컨트롤러, 메시, 머티리얼)가 사라지는 경우가 있어 지우고 새로 저장한다.
    private static void SavePrefab(GameObject root, string path)
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.DeleteAsset(path);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static string FindPrefabPath(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:Prefab");
        return guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;
    }

    private const string BowDir = "Assets/Devion Games/Third Person Controller/Example/Art/Animations/Bow/";
    private const string ArtDir = "Assets/_Project/Art/Zombie";

    private static AnimationClip CopyHumanClip(string fbxName, string clipName, string assetName)
    {
        string path = AnimationDir + "/" + assetName + ".anim";
        AssetDatabase.DeleteAsset(path);
        AnimationClip copy = Object.Instantiate(FindClip(BowDir + fbxName + ".fbx", clipName));
        copy.name = assetName;
        // 원본 클립에 남은 Devion 애니메이션 이벤트(OnEndUse 등)는 받는 쪽이 없어 오류가 나므로 지운다.
        AnimationUtility.SetAnimationEvents(copy, new AnimationEvent[0]);
        AssetDatabase.CreateAsset(copy, path);
        return copy;
    }

    // 활 클립은 FBX(캐릭터 메시 포함 21MB)째로 추적하지 않으려고 필요한 클립만 복사해 둔다.
    // 이동 방향(앞/뒤/좌/우)에 따라 2D 블렌드로 섞고, 쏘기 -> 재장전 -> 이동으로 이어진다.
    private static void BuildArcherController()
    {
        AnimationClip idle = CopyHumanClip("Bow Idle", "Bow Idle", "Archer_Idle");
        AnimationClip walk = CopyHumanClip("Bow Walk", "Bow Walk", "Archer_Walk");
        AnimationClip back = CopyHumanClip("Bow Walk Backward", "Bow Walk Backward", "Archer_WalkBackward");
        AnimationClip strafeLeft = CopyHumanClip("Bow Strafe Left", "Bow Strafe Left", "Archer_StrafeLeft");
        AnimationClip strafeRight = CopyHumanClip("Bow Strafe Right", "Bow Strafe Right", "Archer_StrafeRight");
        AnimationClip shoot = CopyHumanClip("Bow Shoot", "Bow Shoot", "Archer_Shoot");
        AnimationClip reload = CopyHumanClip("Bow Reload", "Bow Reload", "Archer_Reload");

        AnimatorController controller = CreateController("Archer");
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        AnimatorState moveState = controller.CreateBlendTreeInController("Move", out BlendTree tree);
        tree.blendType = BlendTreeType.FreeformDirectional2D;
        tree.blendParameter = "MoveX";
        tree.blendParameterY = "MoveZ";
        tree.useAutomaticThresholds = false;
        tree.AddChild(idle, new Vector2(0f, 0f));
        tree.AddChild(walk, new Vector2(0f, 1.6f));
        tree.AddChild(back, new Vector2(0f, -1.3f));
        tree.AddChild(strafeLeft, new Vector2(-1.3f, 0f));
        tree.AddChild(strafeRight, new Vector2(1.3f, 0f));
        machine.defaultState = moveState;

        AnimatorState shootState = machine.AddState("Shoot");
        shootState.motion = shoot;
        AnimatorState reloadState = machine.AddState("Reload");
        reloadState.motion = reload;

        LinkAny(machine, shootState).AddCondition(AnimatorConditionMode.If, 0, "Attack");
        AnimatorStateTransition toReload = Link(shootState, reloadState, 0.1f);
        toReload.hasExitTime = true;
        toReload.exitTime = 0.9f;
        AnimatorStateTransition backToMove = Link(reloadState, moveState, 0.15f);
        backToMove.hasExitTime = true;
        backToMove.exitTime = 0.9f;
        EditorUtility.SetDirty(controller);
    }

    private static Material CreateLitMaterial(string name, string texturePath)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        AssetDatabase.CreateAsset(material, ArtDir + "/" + name + ".mat");
        return material;
    }

    private static Mesh CopyMesh(Mesh source, string name)
    {
        Mesh copy = Object.Instantiate(source);
        copy.name = name;
        AssetDatabase.CreateAsset(copy, ArtDir + "/" + name + ".asset");
        return copy;
    }

    // 화살과 활 메시/텍스처를 _Project로 복사해 두고 발사체 프리팹과 활 프리팹을 만든다.
    private static void BuildArcherParts()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Zombie")) AssetDatabase.CreateFolder("Assets/_Project/Art", "Zombie");
        foreach (string stale in new[] { "Arrow.asset", "Bow.asset", "Arrow.mat", "Bow.mat", "ArrowTrail.mat" }) AssetDatabase.DeleteAsset(ArtDir + "/" + stale);

        foreach (string file in new[] { "Textures/Arrow_DIFF.png", "Textures/Bow_DIFF.jpg" })
        {
            string target = ArtDir + "/" + file.Substring(file.LastIndexOf('/') + 1);
            AssetDatabase.DeleteAsset(target);
            AssetDatabase.CopyAsset(BowDir + file, target);
        }
        AssetDatabase.CopyAsset("Assets/Devion Games/Third Person Controller/Example/Sounds/Arrow.wav", ArtDir + "/Arrow.wav");
        AssetDatabase.Refresh();

        // 화살: 메시는 +Z가 앞이고 길이 0.75m다.
        GameObject arrowSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Devion Games/Inventory System/Examples/Prefabs/Items/Arrow.prefab");
        Mesh arrowMesh = CopyMesh(arrowSource.GetComponent<MeshFilter>().sharedMesh, "Arrow");
        GameObject arrow = new GameObject("Zombie_Arrow");
        GameObject arrowVisual = new GameObject("Visual", typeof(MeshFilter), typeof(MeshRenderer));
        arrowVisual.transform.SetParent(arrow.transform, false);
        arrowVisual.GetComponent<MeshFilter>().sharedMesh = arrowMesh;
        arrowVisual.GetComponent<MeshRenderer>().sharedMaterial = CreateLitMaterial("Arrow", ArtDir + "/Arrow_DIFF.png");
        // 4cm짜리 원본 화살은 날아가는 동안 거의 안 보여서 굵게 키우고 짧은 궤적선을 붙인다.
        arrowVisual.transform.localScale = new Vector3(2.5f, 2.5f, 1.2f);
        TrailRenderer trail = arrow.AddComponent<TrailRenderer>();
        trail.time = 0.15f;
        trail.widthMultiplier = 0.05f;
        trail.minVertexDistance = 0.1f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Material trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        trailMaterial.color = new Color(0.9f, 0.9f, 0.85f, 1f);
        AssetDatabase.CreateAsset(trailMaterial, ArtDir + "/ArrowTrail.mat");
        trail.sharedMaterial = trailMaterial;
        trail.colorGradient = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) },
        };
        arrow.AddComponent<ZombieProjectile>();
        SavePrefab(arrow, PrefabDir + "/Zombie_Arrow.prefab");

        // 활: FBX 루트의 회전만 옮긴다(루트 스케일 100은 단위 변환 흔적이고 메시 자체가 이미 1.37m다).
        GameObject bowSource = AssetDatabase.LoadAssetAtPath<GameObject>(BowDir + "Bow/Bow.fbx");
        Mesh bowMesh = CopyMesh(bowSource.GetComponentInChildren<MeshFilter>().sharedMesh, "Bow");
        GameObject bow = new GameObject("Zombie_Bow", typeof(MeshFilter), typeof(MeshRenderer));
        bow.GetComponent<MeshFilter>().sharedMesh = bowMesh;
        bow.GetComponent<MeshRenderer>().sharedMaterial = CreateLitMaterial("Bow", ArtDir + "/Bow_DIFF.jpg");
        bow.transform.localRotation = bowSource.transform.localRotation;
        SavePrefab(bow, PrefabDir + "/Zombie_Bow.prefab");
        AssetDatabase.SaveAssets();
    }

    private static void AddBow(GameObject root, Animator animator)
    {
        Transform hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        GameObject bow = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Zombie_Bow.prefab"), hand);
        bow.name = "Bow";
        // 조준 자세에서 활의 긴 축이 위를 향하도록 손 기준으로 맞춘 회전이다(play 중 눈으로 보고 정했다).
        bow.transform.localPosition = Vector3.zero;
        // 활 메시의 시위는 로컬 +Y 쪽이다. 시위가 쏘는 사람 쪽을 보도록 긴 축(Z) 기준으로 180도 돌려 둔다.
        bow.transform.localRotation = Quaternion.Euler(4.97f, 146.48f, 213.52f) * Quaternion.Euler(0f, 0f, 180f);

        SetFields(root.AddComponent<ZombieRangedAttack>(),
            ("projectilePrefab", AssetDatabase.LoadAssetAtPath<ZombieProjectile>(PrefabDir + "/Zombie_Arrow.prefab")),
            ("muzzle", bow.transform));
        SerializedObject so = new SerializedObject(root.GetComponent<ZombieRangedAttack>());
        SerializedProperty sounds = so.FindProperty("releaseSounds");
        sounds.FindPropertyRelative("clips").arraySize = 1;
        sounds.FindPropertyRelative("clips").GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ArtDir + "/Arrow.wav");
        sounds.FindPropertyRelative("volume").floatValue = 0.8f;
        sounds.FindPropertyRelative("pitchRange").vector2Value = new Vector2(0.95f, 1.05f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
