// 동물(초식 사냥감 돼지·양·소, 육식 맹수 표범·호랑이, 탈것 말)의 정의 에셋, 래퍼 프리팹, 소리 묶음을 만들고 스포너를 씬에 놓아 주는 에디터 도구다. 벤더 프리팹은 중첩해서 참조만 하고 수정하지 않는다.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

public static class AnimalAssetBuilder
{
    private const string VendorDir = "Assets/Animal pack deluxe v2/Prefabs/";
    private const string DefinitionDir = "Assets/_Project/Data/Animals/";
    private const string PrefabDir = "Assets/_Project/Prefabs/Animals/";
    private const string MeatItemPath = "Assets/_Project/Data/Items/Item_Food_RawMeat.asset";
    private const string HideItemPath = "Assets/_Project/Data/Items/Item_Resource_Leather.asset";
    private const string LootPickupPath = "Assets/_Project/Prefabs/World/LootPickup.prefab";

    // 초식 사냥감과 육식 맹수가 공유하는 사양이다.
    private struct Spec
    {
        public string id, fileName, displayName, vendorPrefab;
        public float health, walkSpeed, runSpeed, alertDistance, fleeSeconds, fleeDistance, agentRadius;
        public int meatMin, meatMax, hideMin, hideMax;
        public float weight;
        // 맹수 전용 수치다(predator가 true일 때만 쓴다). runSpeed가 추격 속도다.
        public bool predator;
        public float detectDistance, loseDistance, loseSeconds, attackRange, attackDamage, attackCooldown, attackDuration, attackHitDelay, roarSeconds;
    }

    // 수치는 임시 밸런스다. 소는 크고 둔하며 고기가 많고, 양은 겁이 많다.
    private static readonly Spec[] PreySpecs =
    {
        new Spec { id = "animal_pig", fileName = "Pig", displayName = "돼지", vendorPrefab = "Domestic_pig", health = 40f, walkSpeed = 1.1f, runSpeed = 4.2f, alertDistance = 8f, fleeSeconds = 5f, fleeDistance = 12f, agentRadius = 0.35f, meatMin = 2, meatMax = 3, hideMin = 1, hideMax = 2, weight = 1f },
        new Spec { id = "animal_sheep", fileName = "Sheep", displayName = "양", vendorPrefab = "Domestic_sheep", health = 30f, walkSpeed = 1.0f, runSpeed = 4.0f, alertDistance = 10f, fleeSeconds = 6f, fleeDistance = 14f, agentRadius = 0.4f, meatMin = 1, meatMax = 2, hideMin = 1, hideMax = 2, weight = 1f },
        new Spec { id = "animal_cattle", fileName = "Cattle", displayName = "소", vendorPrefab = "Longhorn_cattle_v1", health = 90f, walkSpeed = 0.9f, runSpeed = 3.6f, alertDistance = 7f, fleeSeconds = 5f, fleeDistance = 12f, agentRadius = 0.6f, meatMin = 4, meatMax = 6, hideMin = 2, hideMax = 3, weight = 0.6f },
    };

    // 표범은 빠르고 약하며, 호랑이는 느리지만 세고 질기다. 달리기 속도가 추격 속도다(플레이어 전력 질주는 약 5.3).
    // 공격 시간은 짧은 공격 클립(attack B) 길이에 맞췄고 타격 순간은 그 40% 지점으로 잡은 추정값이다.
    private static readonly Spec[] PredatorSpecs =
    {
        new Spec { predator = true, id = "predator_leopard", fileName = "Leopard", displayName = "표범", vendorPrefab = "Leopard", health = 60f, walkSpeed = 1.4f, runSpeed = 5.8f, agentRadius = 0.4f, meatMin = 3, meatMax = 4, hideMin = 1, hideMax = 2, weight = 1f,
            detectDistance = 15f, loseDistance = 40f, loseSeconds = 8f, attackRange = 2.3f, attackDamage = 12f, attackCooldown = 2.2f, attackDuration = 1.63f, attackHitDelay = 0.65f, roarSeconds = 1.2f },
        new Spec { predator = true, id = "predator_tiger", fileName = "Tiger", displayName = "호랑이", vendorPrefab = "Tiger", health = 120f, walkSpeed = 1.4f, runSpeed = 5.0f, agentRadius = 0.45f, meatMin = 5, meatMax = 7, hideMin = 2, hideMax = 3, weight = 0.7f,
            detectDistance = 18f, loseDistance = 40f, loseSeconds = 8f, attackRange = 2.5f, attackDamage = 18f, attackCooldown = 2.0f, attackDuration = 1.21f, attackHitDelay = 0.5f, roarSeconds = 1.2f },
    };

    // 탈것으로 쓰는 말이다. 다가가도 도망가지 않고(alertDistance 0) 맞았을 때만 도망간다. 걷기/달리기 속도는 AI가 돌아다닐 때와 도망칠 때의 값이다.
    private static readonly Spec[] HorseSpecs =
    {
        new Spec { id = "animal_horse", fileName = "Horse", displayName = "말", vendorPrefab = "Horse_v1", health = 120f, walkSpeed = 1.3f, runSpeed = 6f, alertDistance = 0f, fleeSeconds = 5f, fleeDistance = 20f, agentRadius = 0.5f, meatMin = 4, meatMax = 6, hideMin = 2, hideMax = 3, weight = 1f },
    };

    [MenuItem("ZGV01/Animals/Build Hunting Animals")]
    public static void Build()
    {
        EnsureFolder("Assets/_Project/Data", "Animals");
        EnsureFolder("Assets/_Project/Prefabs", "Animals");

        List<GameObject> prey = BuildGroup(PreySpecs);
        List<GameObject> predators = BuildGroup(PredatorSpecs);
        List<GameObject> horses = BuildGroup(HorseSpecs);
        foreach (GameObject horse in horses) AttachMountable(horse);
        AssetDatabase.SaveAssets();
        RegisterAnimals(prey, predators, horses);

        PlaceSpawner("AnimalSpawner", prey, PreySpecs, radius: 45f, maxAlive: 6, initialCount: 5, respawnInterval: 20f, minPlayerDistance: 14f);
        PlaceSpawner("PredatorSpawner", predators, PredatorSpecs, radius: 60f, maxAlive: 2, initialCount: 2, respawnInterval: 90f, minPlayerDistance: 35f);
        PlaceSpawner("HorseSpawner", horses, HorseSpecs, radius: 60f, maxAlive: 2, initialCount: 2, respawnInterval: 120f, minPlayerDistance: 20f);
        AttachRiderToPlayer();
        AssetDatabase.SaveAssets();
    }

    // 말 프리팹에 탈 수 있는 컴포넌트를 붙인다(프리팹은 그 자리에서 고쳐 GUID를 유지한다).
    private static void AttachMountable(GameObject prefab)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        if (contents.GetComponent<Mountable>() == null) contents.AddComponent<Mountable>();
        Mountable mountable = contents.GetComponent<Mountable>();
        SerializedObject so = new SerializedObject(mountable);
        // 점프 상태 이름은 모델마다 다르므로 컨트롤러에서 찾는다.
        so.FindProperty("jumpState").stringValue = FindState(BuildOwnController(HorseSpecs[0]), "jump");
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    // 열려 있는 씬의 플레이어에 말 타기 컴포넌트를 붙인다(이미 있으면 그대로 둔다).
    private static void AttachRiderToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || player.GetComponent<HorseRider>() != null) return;

        player.AddComponent<HorseRider>();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
    }

    // 저장 파일이 id로 동물 프리팹을 찾을 수 있게 Resources의 동물 DB를 채운다(없으면 만든다).
    private static void RegisterAnimals(List<GameObject> prey, List<GameObject> predators, List<GameObject> horses)
    {
        const string path = "Assets/_Project/Resources/AnimalDatabase.asset";
        AnimalDatabase database = LoadOrCreate<AnimalDatabase>(path);
        SerializedObject so = new SerializedObject(database);
        SerializedProperty list = so.FindProperty("animals");
        list.arraySize = PreySpecs.Length + PredatorSpecs.Length + HorseSpecs.Length;
        for (int i = 0; i < list.arraySize; i++)
        {
            Spec spec;
            GameObject prefab;
            if (i < PreySpecs.Length) { spec = PreySpecs[i]; prefab = prey[i]; }
            else if (i < PreySpecs.Length + PredatorSpecs.Length) { spec = PredatorSpecs[i - PreySpecs.Length]; prefab = predators[i - PreySpecs.Length]; }
            else { spec = HorseSpecs[i - PreySpecs.Length - PredatorSpecs.Length]; prefab = horses[i - PreySpecs.Length - PredatorSpecs.Length]; }
            SerializedProperty entry = list.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("id").stringValue = spec.id;
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }

    private static List<GameObject> BuildGroup(Spec[] specs)
    {
        List<GameObject> prefabs = new List<GameObject>();
        foreach (Spec spec in specs)
        {
            ScriptableObject definition = spec.predator ? BuildPredatorDefinition(spec) : BuildDefinition(spec);
            GameObject prefab = BuildPrefab(spec, definition);
            prefabs.Add(prefab);
            UseOwnController(spec, prefab);
            AttachAudio(prefab, BuildAudioSet(spec));
        }
        return prefabs;
    }

    // ---- 소리 ----

    private const string SoundDir = "Assets/Ultimate Animal Sounds/";

    // 소리 묶음 한 칸의 정의다. numbers가 있으면 폴더 안 "pattern" 파일의 번호 목록으로, 없으면 이름이 pattern으로 시작하는 모든 클립을 고른다.
    private struct Voice
    {
        public string folder, pattern;   // 예: "Pig", "Pig Squeals {0}.wav" 또는 접두어 "tiger roars"
        public int[] numbers;
        public float volume, pitchMin, pitchMax;
        public bool IsEmpty => string.IsNullOrEmpty(folder);
    }

    private struct AudioSpec
    {
        public Voice idle, alarm, attack, hurt, death, footstep;
        public float walkStride, runStride;
        public Vector2 idleCallInterval;
    }

    private static Voice V(string folder, string pattern, int[] numbers, float volume, float pitchMin, float pitchMax)
        => new Voice { folder = folder, pattern = pattern, numbers = numbers, volume = volume, pitchMin = pitchMin, pitchMax = pitchMax };

    // 이름이 prefix로 시작하는 폴더 안 모든 클립(대소문자 무시)을 쓴다.
    private static Voice Prefix(string folder, string prefix, float volume, float pitchMin, float pitchMax)
        => V(folder, prefix, null, volume, pitchMin, pitchMax);

    private static readonly int[] One5 = { 1, 2, 3, 4, 5 };

    // 음량·피치·간격은 임시 값이다(들어 보고 조절한다). 피치가 낮을수록 굵고 크게 들린다.
    private static AudioSpec AudioFor(string fileName)
    {
        switch (fileName)
        {
            case "Pig":
                return new AudioSpec
                {
                    idle = V("Pig", "Pig Snorts {0}.wav", One5, 0.7f, 0.9f, 1.1f),
                    alarm = V("Pig", "Pig Squeals {0}.wav", One5, 1f, 0.95f, 1.15f),
                    hurt = V("Pig", "Pig Squeals {0}.wav", new[] { 4, 5, 1 }, 1f, 1.05f, 1.25f),
                    death = V("Pig", "Pig Squeals {0}.wav", new[] { 1, 3 }, 1f, 0.75f, 0.9f),
                    footstep = V("Pig", "Pig Footsteps {0}.wav", One5, 0.5f, 0.9f, 1.1f),
                    walkStride = 0.8f, runStride = 1.3f, idleCallInterval = new Vector2(8f, 16f),
                };
            case "Sheep":
                return new AudioSpec
                {
                    idle = V("Sheep", "Sheep baas {0}.wav", One5, 0.7f, 0.95f, 1.1f),
                    alarm = V("Sheep", "Sheep baas {0}.wav", One5, 1f, 1.1f, 1.3f),
                    hurt = V("Sheep", "Sheep baas {0}.wav", new[] { 4, 1 }, 1f, 1.2f, 1.4f),
                    death = V("Sheep", "Sheep baas {0}.wav", new[] { 2, 3 }, 1f, 0.7f, 0.85f),
                    footstep = V("Sheep", "Sheep footsteps {0}.wav", One5, 0.5f, 0.95f, 1.1f),
                    walkStride = 0.9f, runStride = 1.4f, idleCallInterval = new Vector2(8f, 18f),
                };
            case "Leopard":
                // 표범 소리는 따로 없어 사자(Lion) 소리를 쓴다.
                return new AudioSpec
                {
                    idle = Prefix("Lion", "Lion Warning Call", 0.5f, 1.1f, 1.3f),
                    alarm = Prefix("Lion", "Lion roars", 1f, 1.15f, 1.35f),
                    attack = Prefix("Lion", "Lion attacks_bites", 1f, 1.1f, 1.3f),
                    hurt = Prefix("Lion", "Lion attacks_bites", 1f, 1.3f, 1.5f),
                    death = Prefix("Lion", "Lion roars", 1f, 0.8f, 0.95f),
                    footstep = Prefix("Medium Animal Footsteps", "Medium animal footsteps", 0.6f, 0.95f, 1.1f),
                    walkStride = 1.0f, runStride = 2.0f, idleCallInterval = new Vector2(14f, 28f),
                };
            case "Tiger":
                return new AudioSpec
                {
                    idle = Prefix("Tiger", "Tiger Roars", 0.45f, 1.1f, 1.25f),
                    alarm = Prefix("Tiger", "Tiger Roars", 1f, 0.9f, 1.05f),
                    attack = Prefix("Tiger", "Tiger Attacks", 1f, 0.95f, 1.1f),
                    hurt = Prefix("Tiger", "Tiger attack ", 1f, 1.05f, 1.2f),
                    death = Prefix("Tiger", "Tiger Roars", 1f, 0.7f, 0.85f),
                    footstep = Prefix("Large Animal Footsteps", "Large animal footsteps", 0.7f, 0.9f, 1.05f),
                    walkStride = 1.2f, runStride = 2.2f, idleCallInterval = new Vector2(14f, 28f),
                };
            case "Horse":
                return new AudioSpec
                {
                    idle = Prefix("Horse", "Horse squeals", 0.7f, 0.9f, 1.05f),
                    alarm = Prefix("Horse", "Horse squeals", 1f, 1.0f, 1.15f),
                    hurt = Prefix("Horse", "Horse squeals", 1f, 1.1f, 1.25f),
                    death = Prefix("Horse", "Horse squeals", 1f, 0.7f, 0.85f),
                    footstep = V("Horse/Horse Footseps Surfaces", "Horse Footsteps Grass {0}.wav", One5, 0.8f, 0.95f, 1.05f),
                    walkStride = 1.5f, runStride = 2.6f, idleCallInterval = new Vector2(15f, 30f),
                };
            default:   // Cattle
                return new AudioSpec
                {
                    idle = V("Cow Moos", "Cow moos {0}.wav", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, 0.8f, 0.95f, 1.05f),
                    alarm = V("Bull Moo", "Bull Moo {0}.wav", One5, 1f, 0.95f, 1.1f),
                    hurt = V("Bull Moo", "Bull Moo {0}.wav", new[] { 1, 2 }, 1f, 1.0f, 1.15f),
                    death = V("Bull Moo", "Bull Moo {0}.wav", new[] { 3, 4 }, 1f, 0.7f, 0.85f),
                    footstep = V("Large Animal Footsteps", "Large animal footsteps {0}.wav", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 0.7f, 0.8f, 0.95f),
                    walkStride = 1.5f, runStride = 2.2f, idleCallInterval = new Vector2(12f, 25f),
                };
        }
    }

    private static void ApplyVoice(SerializedProperty group, Voice voice)
    {
        SerializedProperty clips = group.FindPropertyRelative("clips");
        if (voice.IsEmpty)
        {
            clips.arraySize = 0;
            return;
        }

        List<AudioClip> found = new List<AudioClip>();
        if (voice.numbers != null)
        {
            foreach (int number in voice.numbers)
            {
                string path = SoundDir + voice.folder + "/" + string.Format(voice.pattern, number);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) Debug.LogWarning("소리 파일을 찾지 못했다: " + path);
                else found.Add(clip);
            }
        }
        else
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { SoundDir + voice.folder }))
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (clip != null && clip.name.StartsWith(voice.pattern, System.StringComparison.OrdinalIgnoreCase)) found.Add(clip);
            }
            found.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            if (found.Count == 0) Debug.LogWarning($"소리 파일을 찾지 못했다: {voice.folder}/{voice.pattern}*");
        }

        clips.arraySize = found.Count;
        for (int i = 0; i < found.Count; i++) clips.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
        group.FindPropertyRelative("volume").floatValue = voice.volume;
        group.FindPropertyRelative("pitchRange").vector2Value = new Vector2(voice.pitchMin, voice.pitchMax);
    }

    private static AnimalAudioSet BuildAudioSet(Spec spec)
    {
        string path = DefinitionDir + "AnimalAudio_" + spec.fileName + ".asset";
        AnimalAudioSet set = AssetDatabase.LoadAssetAtPath<AnimalAudioSet>(path);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<AnimalAudioSet>();
            AssetDatabase.CreateAsset(set, path);
        }

        AudioSpec audio = AudioFor(spec.fileName);
        SerializedObject so = new SerializedObject(set);
        ApplyVoice(so.FindProperty("idle"), audio.idle);
        ApplyVoice(so.FindProperty("alarm"), audio.alarm);
        ApplyVoice(so.FindProperty("attack"), audio.attack);
        ApplyVoice(so.FindProperty("hurt"), audio.hurt);
        ApplyVoice(so.FindProperty("death"), audio.death);
        ApplyVoice(so.FindProperty("footstep"), audio.footstep);
        so.FindProperty("walkStride").floatValue = audio.walkStride;
        so.FindProperty("runStride").floatValue = audio.runStride;
        so.FindProperty("idleCallInterval").vector2Value = audio.idleCallInterval;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(set);
        return set;
    }

    // 래퍼 프리팹에 소리 컴포넌트를 붙이고 소리 묶음을 연결한다. 프리팹은 그 자리에서 고쳐 GUID를 유지한다.
    private static void AttachAudio(GameObject prefab, AnimalAudioSet set)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        AnimalAudio audio = contents.GetComponent<AnimalAudio>();
        if (audio == null) audio = contents.AddComponent<AnimalAudio>();

        SerializedObject so = new SerializedObject(audio);
        so.FindProperty("audioSet").objectReferenceValue = set;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

    // ---- 정의 ----

    // 컨트롤러의 상태 이름에서 키워드가 든 첫 상태를 찾는다(모델마다 "idle A", "idle v1" 등 이름이 다르다).
    private static string FindState(AnimatorController controller, string keyword)
    {
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
            if (child.state.name.ToLowerInvariant().Contains(keyword)) return child.state.name;
        return string.Empty;
    }

    private static AnimatorController ControllerOf(Spec spec)
    {
        GameObject vendor = AssetDatabase.LoadAssetAtPath<GameObject>(VendorDir + spec.vendorPrefab + ".prefab");
        return vendor.GetComponentInChildren<Animator>().runtimeAnimatorController as AnimatorController;
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    // 초식/맹수가 함께 쓰는 AnimalDefinition 필드를 채운다.
    private static void ApplyCommon(SerializedObject so, Spec spec)
    {
        AnimatorController controller = ControllerOf(spec);
        so.FindProperty("stableId").stringValue = spec.id;
        so.FindProperty("displayName").stringValue = spec.displayName;
        so.FindProperty("walkSpeed").floatValue = spec.walkSpeed;
        so.FindProperty("runSpeed").floatValue = spec.runSpeed;
        so.FindProperty("meatItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(MeatItemPath);
        so.FindProperty("meatMin").intValue = spec.meatMin;
        so.FindProperty("meatMax").intValue = spec.meatMax;
        so.FindProperty("hideItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(HideItemPath);
        so.FindProperty("hideMin").intValue = spec.hideMin;
        so.FindProperty("hideMax").intValue = spec.hideMax;
        so.FindProperty("corpseSeconds").floatValue = 90f;
        so.FindProperty("idleState").stringValue = FindState(controller, "idle");
        so.FindProperty("walkState").stringValue = FindState(controller, "walk");
        so.FindProperty("runState").stringValue = FindState(controller, "run");
        so.FindProperty("eatState").stringValue = FindState(controller, "eat");
        so.FindProperty("dieState").stringValue = FindState(controller, "die");
    }

    private static AnimalDefinition BuildDefinition(Spec spec)
    {
        AnimalDefinition definition = LoadOrCreate<AnimalDefinition>(DefinitionDir + "Animal_" + spec.fileName + ".asset");
        SerializedObject so = new SerializedObject(definition);
        ApplyCommon(so, spec);
        so.FindProperty("alertDistance").floatValue = spec.alertDistance;
        so.FindProperty("fleeSeconds").floatValue = spec.fleeSeconds;
        so.FindProperty("fleeDistance").floatValue = spec.fleeDistance;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static PredatorDefinition BuildPredatorDefinition(Spec spec)
    {
        PredatorDefinition definition = LoadOrCreate<PredatorDefinition>(DefinitionDir + "Predator_" + spec.fileName + ".asset");
        SerializedObject so = new SerializedObject(definition);
        ApplyCommon(so, spec);
        // 맹수는 풀을 뜯지 않는다. 대기 중 가끔 먹는 동작은 쓰지 않는다.
        so.FindProperty("eatChance").floatValue = 0f;
        so.FindProperty("detectDistance").floatValue = spec.detectDistance;
        so.FindProperty("loseDistance").floatValue = spec.loseDistance;
        so.FindProperty("loseSeconds").floatValue = spec.loseSeconds;
        so.FindProperty("attackRange").floatValue = spec.attackRange;
        so.FindProperty("attackDamage").floatValue = spec.attackDamage;
        so.FindProperty("attackCooldown").floatValue = spec.attackCooldown;
        so.FindProperty("attackDuration").floatValue = spec.attackDuration;
        so.FindProperty("attackHitDelay").floatValue = spec.attackHitDelay;
        so.FindProperty("roarSeconds").floatValue = spec.roarSeconds;

        // 짧은 공격 클립(attack B)만 쓴다. 긴 attack A는 3초 안팎이라 연속 공격 템포에 맞지 않는다.
        AnimatorController controller = ControllerOf(spec);
        string attackB = string.Empty;
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
            if (child.state.name.ToLowerInvariant().Contains("attack b")) attackB = child.state.name;
        SerializedProperty attacks = so.FindProperty("attackStates");
        attacks.arraySize = 1;
        attacks.GetArrayElementAtIndex(0).stringValue = attackB;
        so.FindProperty("roarState").stringValue = FindState(controller, "roar");
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    // ---- 애니메이터 컨트롤러 ----

    private const string ControllerDir = DefinitionDir + "Controllers/";

    // 벤더 컨트롤러는 상태 사이에 조건 없는 자동 전환(예: walk 83%에서 idle, 표범 run 40%에서 die)이 있어서
    // CrossFade로 지정한 동작이 곧 다른 동작으로 바뀐다. 같은 상태와 클립을 가지고 전환만 없는 컨트롤러를 만들어 쓴다.
    private static AnimatorController BuildOwnController(Spec spec)
    {
        EnsureFolder("Assets/_Project/Data/Animals", "Controllers");
        string path = ControllerDir + "Anim_" + spec.fileName + ".controller";
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;

        AnimatorController source = ControllerOf(spec);
        AnimatorController created = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine machine = created.layers[0].stateMachine;
        AnimatorState defaultState = null;
        foreach (ChildAnimatorState child in source.layers[0].stateMachine.states)
        {
            AnimatorState state = machine.AddState(child.state.name);
            state.motion = child.state.motion;
            state.speed = child.state.speed;
            // 기본 상태는 처음 나오는 idle이다(없으면 처음 상태).
            bool isIdle = child.state.name.ToLowerInvariant().StartsWith("idle");
            if (defaultState == null || (isIdle && !defaultState.name.ToLowerInvariant().StartsWith("idle"))) defaultState = state;
        }
        if (defaultState != null) machine.defaultState = defaultState;
        EditorUtility.SetDirty(created);
        AssetDatabase.SaveAssets();
        return created;
    }

    // 래퍼 프리팹의 모델 애니메이터에 전환 없는 컨트롤러를 연결한다(프리팹 인스턴스의 값 덮어쓰기라 벤더 프리팹은 그대로다).
    private static void UseOwnController(Spec spec, GameObject prefab)
    {
        AnimatorController controller = BuildOwnController(spec);
        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        Animator animator = contents.GetComponentInChildren<Animator>();
        bool changed = animator != null && animator.runtimeAnimatorController != controller;
        if (changed)
        {
            animator.runtimeAnimatorController = controller;
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        PrefabUtility.UnloadPrefabContents(contents);
    }

    // ---- 프리팹 ----

    private static GameObject BuildPrefab(Spec spec, ScriptableObject definition)
    {
        string path = PrefabDir + "Animal_" + spec.fileName + ".prefab";
        // 이미 있는 프리팹은 다시 만들지 않는다(GUID가 바뀌면 씬/스포너 참조가 끊긴다).
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject("Animal_" + spec.fileName);
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VendorDir + spec.vendorPrefab + ".prefab"), root.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        Bounds bounds = RendererBounds(model);
        model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        Vector3 size = bounds.size;

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.size = new Vector3(Mathf.Max(0.3f, size.x * 0.9f), size.y, Mathf.Max(0.3f, size.z * 0.9f));
        box.center = new Vector3(0f, size.y * 0.5f, 0f);

        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.radius = spec.agentRadius;
        agent.height = size.y;
        agent.speed = spec.walkSpeed;
        agent.acceleration = spec.predator ? 20f : 12f;
        agent.angularSpeed = spec.predator ? 360f : 240f;
        agent.stoppingDistance = 0.3f;

        Health health = root.AddComponent<Health>();
        SerializedObject healthSo = new SerializedObject(health);
        healthSo.FindProperty("maxHealth").floatValue = spec.health;
        healthSo.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<ImpactSurfaceMarker>().Initialize(ImpactSurface.SoftBody);

        Component ai = spec.predator ? (Component)root.AddComponent<PredatorAI>() : root.AddComponent<AnimalAI>();
        SerializedObject aiSo = new SerializedObject(ai);
        aiSo.FindProperty("definition").objectReferenceValue = definition;
        aiSo.FindProperty("animator").objectReferenceValue = model.GetComponentInChildren<Animator>();
        aiSo.FindProperty("lootPickupPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(LootPickupPath);
        aiSo.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    private static Bounds RendererBounds(GameObject go)
    {
        bool any = false;
        Bounds bounds = new Bounds();
        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer) continue;
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    // ---- 스포너 ----

    // 열려 있는 씬에서 이름이 같은 스포너를 찾아(없으면 원점에 만들어) 동물 목록을 채운다. 씬 저장은 호출한 쪽이 한다.
    private static void PlaceSpawner(string objectName, List<GameObject> prefabs, Spec[] specs, float radius, int maxAlive, int initialCount, float respawnInterval, float minPlayerDistance)
    {
        AnimalSpawner spawner = null;
        foreach (AnimalSpawner candidate in Object.FindObjectsByType<AnimalSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (candidate.gameObject.name == objectName) spawner = candidate;

        bool created = spawner == null;
        if (created) spawner = new GameObject(objectName).AddComponent<AnimalSpawner>();

        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty list = so.FindProperty("animals");
        list.arraySize = specs.Length;
        for (int i = 0; i < specs.Length; i++)
        {
            SerializedProperty entry = list.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefabs[i];
            entry.FindPropertyRelative("weight").floatValue = specs[i].weight;
        }
        // 이미 있는 스포너의 구역/수는 사용자가 씬에서 조정했을 수 있으니 새로 만들 때만 정한다.
        if (created)
        {
            so.FindProperty("radius").floatValue = radius;
            so.FindProperty("maxAlive").intValue = maxAlive;
            so.FindProperty("initialCount").intValue = initialCount;
            so.FindProperty("respawnInterval").floatValue = respawnInterval;
            so.FindProperty("minPlayerDistance").floatValue = minPlayerDistance;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spawner);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
    }
}
