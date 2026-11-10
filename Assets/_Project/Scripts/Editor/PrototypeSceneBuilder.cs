#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Gimersia.Input;
using Gimersia.Gameplay;
using Gimersia.Juice;
using Gimersia.Audio;
using Gimersia.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gimersia.Editor
{
    [InitializeOnLoad]
    public static class PrototypeSceneBuilder
    {
        static PrototypeSceneBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                    GameObject.Find("--- GIMERSIA GRAYBOX PROTOTYPE ---") == null)
                {
                    BuildPlayableGrayboxArena();
                }
            };
        }

        [MenuItem("Gimersia/Build Playable Graybox Arena")]
        public static void BuildPlayableGrayboxArena()
        {
            EnsureAllMaterials();

            RenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/_Project/Settings/URP_PipelineAsset.asset");
            if (urpAsset != null)
            {
                GraphicsSettings.defaultRenderPipeline = urpAsset;
                for (int q = 0; q < QualitySettings.names.Length; q++)
                {
                    QualitySettings.SetQualityLevel(q, false);
                    QualitySettings.renderPipeline = urpAsset;
                }
            }

            CharacterMovementConfigSO moveCfg = CreateOrLoadAsset<CharacterMovementConfigSO>("Assets/_Project/Data/Configs/CFG_CharacterMovement.asset");
            InputReaderSO inputReader = CreateOrLoadAsset<InputReaderSO>("Assets/_Project/Data/Configs/InputReader.asset");

            // Clean up any old objects from previous builds
            GameObject existingRoot = GameObject.Find("--- GIMERSIA GRAYBOX PROTOTYPE ---");
            if (existingRoot != null) Object.DestroyImmediate(existingRoot);

            GameObject oldPivot = GameObject.Find("CameraRig_Pivot");
            if (oldPivot != null) Object.DestroyImmediate(oldPivot);
            GameObject oldPivot1 = GameObject.Find("CameraRig_Pivot_P1");
            if (oldPivot1 != null) Object.DestroyImmediate(oldPivot1);
            GameObject oldPivot2 = GameObject.Find("CameraRig_Pivot_P2");
            if (oldPivot2 != null) Object.DestroyImmediate(oldPivot2);

            GameObject root = new GameObject("--- GIMERSIA GRAYBOX PROTOTYPE ---");

            // =========================================================================
            // 1. CORE SYSTEMS & SERVICES
            // =========================================================================
            GameObject systemsGo = new GameObject("[Systems]");
            systemsGo.transform.SetParent(root.transform);
            systemsGo.AddComponent<TimeDilationManager>();
            systemsGo.AddComponent<AudioManager>();
            DualClimberOrchestrator orchestrator = systemsGo.AddComponent<DualClimberOrchestrator>();
            PrototypeGameplayHUD hud = systemsGo.AddComponent<PrototypeGameplayHUD>();

            // =========================================================================
            // 2. REAL OPEN-AIR SKYBOX, SUNLIGHT & ATMOSPHERIC VISTA (PEAK VIBES)
            // =========================================================================
            Material skyboxMat = CreateOrUpdateProceduralSkybox("Assets/_Project/Materials/MAT_PeakAlpineSkybox.mat");
            RenderSettings.skybox = skyboxMat;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.78f, 0.96f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.68f, 0.58f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.34f, 0.28f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.68f, 0.85f, 0.96f);
            RenderSettings.fogStartDistance = 135f;
            RenderSettings.fogEndDistance = 580f;

            Light dirLight = Object.FindFirstObjectByType<Light>();
            if (dirLight != null && dirLight.type == LightType.Directional)
            {
                dirLight.color = new Color(1.0f, 0.95f, 0.85f);
                dirLight.intensity = 1.45f;
                dirLight.shadows = LightShadows.Soft;
                dirLight.transform.rotation = Quaternion.Euler(44f, -34f, 0f);
            }

            // =========================================================================
            // 3. TRUE DUAL-CAMERA SPLIT-SCREEN RIGS (P1 LEFT, P2 RIGHT)
            // =========================================================================
            GameObject camRigA = new GameObject("CameraRig_Pivot_P1");
            camRigA.transform.SetParent(root.transform);
            camRigA.transform.position = new Vector3(-2.0f, 6.5f, -34f);

            Camera camA = Camera.main;
            if (camA == null)
            {
                GameObject camAObj = new GameObject("Camera_P1");
                camAObj.tag = "MainCamera";
                camA = camAObj.AddComponent<Camera>();
                camAObj.AddComponent<AudioListener>();
            }
            camA.name = "Camera_P1";
            camA.transform.SetParent(camRigA.transform);
            camA.transform.localPosition = Vector3.zero;
            camA.transform.localRotation = Quaternion.identity;
            camA.clearFlags = CameraClearFlags.Skybox;
            camA.backgroundColor = new Color(0.45f, 0.74f, 0.95f, 1f);
            camA.farClipPlane = 850f;
            camA.depth = 0f;
            camA.rect = new Rect(0f, 0f, 1f, 1f);

            if (!camA.TryGetComponent(out TraumaCameraShaker _))
            {
                camA.gameObject.AddComponent<TraumaCameraShaker>();
            }
            UniversalAdditionalCameraData urpCamDataA = camA.GetComponent<UniversalAdditionalCameraData>();
            if (urpCamDataA == null) urpCamDataA = camA.gameObject.AddComponent<UniversalAdditionalCameraData>();
            urpCamDataA.renderPostProcessing = true;

            GameObject camRigB = new GameObject("CameraRig_Pivot_P2");
            camRigB.transform.SetParent(root.transform);
            camRigB.transform.position = new Vector3(2.0f, 6.5f, -34f);

            GameObject camBObj = new GameObject("Camera_P2");
            camBObj.transform.SetParent(camRigB.transform);
            camBObj.transform.localPosition = Vector3.zero;
            camBObj.transform.localRotation = Quaternion.identity;
            Camera camB = camBObj.AddComponent<Camera>();
            camB.clearFlags = CameraClearFlags.Skybox;
            camB.backgroundColor = new Color(0.45f, 0.74f, 0.95f, 1f);
            camB.farClipPlane = 850f;
            camB.depth = 1f;
            camB.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            UniversalAdditionalCameraData urpCamDataB = camB.gameObject.AddComponent<UniversalAdditionalCameraData>();
            urpCamDataB.renderPostProcessing = true;
            camBObj.SetActive(false);

            BuildCinematicPostProcessVolume(root.transform);

            // =========================================================================
            // 4. LOAD KENNEY 3D NATURE ASSETS, VFX & SFX
            // =========================================================================
            GameObject cliffBlockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/cliff_block_rock.fbx");
            GameObject cliffLargePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/cliff_large_rock.fbx");
            GameObject cliffStepsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/cliff_steps_rock.fbx");
            GameObject cliffSlopePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/cliff_blockSlope_rock.fbx");
            GameObject cliffCornerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/cliff_corner_rock.fbx");
            GameObject cliffTopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/cliff_top_rock.fbx");
            GameObject rockLargeAPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/rock_largeA.fbx");
            GameObject rockLargeBPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/rock_largeB.fbx");
            GameObject rockTallAPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/rock_tallA.fbx");
            GameObject rockTallBPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/rock_tallB.fbx");
            GameObject stoneLargePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/stone_largeA.fbx");
            GameObject stoneTallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/stone_tallA.fbx");
            GameObject bridgeWoodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/bridge_woodRound.fbx");
            GameObject bridgeStonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/bridge_stone.fbx");
            GameObject statueRingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/statue_ring.fbx");
            GameObject statueObeliskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/statue_obelisk.fbx");
            GameObject statueColumnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/statue_column.fbx");
            GameObject treeHealthyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/tree_pineDefaultA.fbx");
            GameObject treeTallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/tree_pineTallA.fbx");
            GameObject treeScorchedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/tree_blocks_dark.fbx");
            GameObject bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/plant_bush.fbx");
            GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/grass.fbx");
            GameObject flowerRedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/flower_redA.fbx");
            GameObject flowerYellowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/flower_yellowA.fbx");
            GameObject mushroomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/mushroom_red.fbx");
            GameObject tentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/tent_smallOpen.fbx");
            GameObject campfirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/campfire_stones.fbx");
            GameObject signPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/sign.fbx");

            AudioClip sfxFootstep0 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/footstep_grass_000.ogg");
            AudioClip sfxFootstep1 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/footstep_grass_001.ogg");
            AudioClip sfxFootstep2 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/footstep_grass_002.ogg");
            AudioClip sfxJump = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/pluck_001.ogg");
            AudioClip sfxBouncePad = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/pluck_002.ogg");
            AudioClip sfxThrow = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/impactGlass_light_000.ogg");
            AudioClip sfxSweetSpot = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/impactGlass_heavy_000.ogg");
            AudioClip sfxSlowMo = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/maximize_001.ogg");
            AudioClip sfxPerfectCatch = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/impactBell_heavy_000.ogg");
            AudioClip sfxBonk = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/impactPunch_heavy_000.ogg");
            AudioClip sfxBounce = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/footstep_concrete_000.ogg");
            AudioClip sfxRestore = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/confirmation_001.ogg");
            AudioClip sfxCheckpoint = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/confirmation_002.ogg");
            AudioClip sfxSwitch = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/select_001.ogg");

            Texture2D texSpark = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/VFX/spark_01.png");
            Texture2D texCircle = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/VFX/circle_05.png");
            Texture2D texSmoke = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/VFX/smoke_01.png");
            Texture2D texFire = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/VFX/fire_01.png");

            // =========================================================================
            // 5. OPEN ISLAND WILDERNESS & ORGANIC SCULPTED 3D MOUNTAIN (125M PEAK)
            // =========================================================================
            GameObject envRoot = new GameObject("[PEAK_OpenIslandWilderness_125m]");
            envRoot.transform.SetParent(root.transform);

            BuildPeakOpenIslandWorld(
                envRoot.transform,
                cliffBlockPrefab,
                cliffLargePrefab,
                cliffStepsPrefab,
                cliffSlopePrefab,
                cliffCornerPrefab,
                cliffTopPrefab,
                rockLargeAPrefab,
                rockLargeBPrefab,
                rockTallAPrefab,
                rockTallBPrefab,
                stoneLargePrefab,
                stoneTallPrefab,
                bridgeWoodPrefab,
                bridgeStonePrefab,
                statueRingPrefab,
                statueObeliskPrefab,
                statueColumnPrefab,
                treeScorchedPrefab,
                treeHealthyPrefab,
                treeTallPrefab,
                bushPrefab,
                grassPrefab,
                flowerRedPrefab,
                flowerYellowPrefab,
                mushroomPrefab,
                tentPrefab,
                campfirePrefab,
                signPrefab,
                texCircle,
                texFire,
                texSpark,
                texSmoke,
                sfxRestore,
                sfxCheckpoint,
                sfxBouncePad,
                sfxSwitch,
                sfxSweetSpot,
                sfxPerfectCatch,
                sfxBonk
            );

            // =========================================================================
            // 6. STYLIZED HIKER CHARACTERS (SAHABAT A & SAHABAT B) + TERRA CORE BALL
            // =========================================================================
            AudioClip[] footsteps = new AudioClip[] { sfxFootstep0, sfxFootstep1, sfxFootstep2 };
            BeanClimberController climberA = CreateStylizedClimber(
                root.transform,
                "Sahabat_A (Biru)",
                new Vector3(-2.2f, 1.5f, -24.0f),
                new Color(0.06f, 0.72f, 0.95f),
                isExplorerHat: true,
                moveCfg,
                footsteps,
                sfxJump,
                sfxBonk
            );

            BeanClimberController climberB = CreateStylizedClimber(
                root.transform,
                "Sahabat_B (Oranye)",
                new Vector3(2.2f, 1.5f, -23.5f),
                new Color(0.98f, 0.54f, 0.12f),
                isExplorerHat: false,
                moveCfg,
                footsteps,
                sfxJump,
                sfxBonk
            );

            TerraCoreBall ball = CreateTerraCoreBall(
                root.transform,
                new Vector3(-2.2f, 2.4f, -23.2f),
                texSpark,
                texCircle,
                sfxThrow,
                sfxSweetSpot,
                sfxSlowMo,
                sfxPerfectCatch,
                sfxBounce
            );

            // Wire Orchestrator Serialized References
            SerializedObject orchSo = new SerializedObject(orchestrator);
            orchSo.FindProperty("_inputReader").objectReferenceValue = inputReader;
            orchSo.FindProperty("_movementConfig").objectReferenceValue = moveCfg;
            orchSo.FindProperty("_climberA").objectReferenceValue = climberA;
            orchSo.FindProperty("_climberB").objectReferenceValue = climberB;
            orchSo.FindProperty("_terraCoreBall").objectReferenceValue = ball;
            orchSo.FindProperty("_cameraA").objectReferenceValue = camA;
            orchSo.FindProperty("_cameraB").objectReferenceValue = camB;
            orchSo.FindProperty("_cameraRigA").objectReferenceValue = camRigA.transform;
            orchSo.FindProperty("_cameraRigB").objectReferenceValue = camRigB.transform;
            orchSo.FindProperty("_summitTargetHeight").floatValue = 125f;
            orchSo.FindProperty("_isSplitScreenMode").boolValue = false;
            orchSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject hudSo = new SerializedObject(hud);
            hudSo.FindProperty("_orchestrator").objectReferenceValue = orchestrator;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        // =============================================================================
        // PROCEDURAL SKYBOX & MATERIALS
        // =============================================================================
        private static Material CreateOrUpdateProceduralSkybox(string assetPath)
        {
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader == null)
            {
                skyShader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(skyShader);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else
            {
                mat.shader = skyShader;
            }

            mat.SetFloat("_SunDisk", 2f);
            mat.SetFloat("_SunSize", 0.045f);
            mat.SetFloat("_SunSizeConvergence", 5f);
            mat.SetFloat("_AtmosphereThickness", 0.92f);
            mat.SetColor("_SkyTint", new Color(0.28f, 0.62f, 0.98f, 1f));
            mat.SetColor("_GroundColor", new Color(0.22f, 0.48f, 0.62f, 1f));
            mat.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void BuildCinematicPostProcessVolume(Transform parent)
        {
            GameObject volGo = new GameObject("Global_URP_PostProcessingVolume");
            volGo.transform.SetParent(parent);
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.weight = 1f;
            vol.priority = 10f;

            string profilePath = "Assets/_Project/Settings/VP_PeakCinematicProfile.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            if (!profile.TryGet(out Tonemapping tonemap))
            {
                tonemap = profile.Add<Tonemapping>(true);
                AssetDatabase.AddObjectToAsset(tonemap, profile);
            }
            tonemap.active = true;
            tonemap.mode.Override(TonemappingMode.ACES);

            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
                AssetDatabase.AddObjectToAsset(bloom, profile);
            }
            bloom.active = true;
            bloom.threshold.Override(0.90f);
            bloom.intensity.Override(0.85f);
            bloom.scatter.Override(0.68f);

            if (!profile.TryGet(out Vignette vignette))
            {
                vignette = profile.Add<Vignette>(true);
                AssetDatabase.AddObjectToAsset(vignette, profile);
            }
            vignette.active = true;
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.42f);

            if (!profile.TryGet(out ColorAdjustments colorAdj))
            {
                colorAdj = profile.Add<ColorAdjustments>(true);
                AssetDatabase.AddObjectToAsset(colorAdj, profile);
            }
            colorAdj.active = true;
            colorAdj.postExposure.Override(0.15f);
            colorAdj.contrast.Override(14f);
            colorAdj.saturation.Override(12f);

            EditorUtility.SetDirty(profile);
            vol.sharedProfile = profile;
        }

        private static Material s_matOcean;
        private static Material s_matSand;
        private static Material s_matGrass;
        private static Material s_matRock;
        private static Material s_matWoodBark;
        private static Material s_matFlowerRed;
        private static Material s_matFlowerYellow;
        private static Material s_matScorchedAsh;
        private static Material s_baseUrpLitMat;

        // Particle Materials (Zero-Magenta URP Unlit Particles)
        private static Material s_matCloud;
        private static Material s_matParticleDefault;
        private static Material s_matParticleAdditive;
        private static Material s_matParticleDust;

        // Character Materials (Persistent Serialized Materials - Zero Graybox Lost Colors)
        private static Material s_matCharKalaBlue;
        private static Material s_matCharSuryaOrange;
        private static Material s_matCharSkin;
        private static Material s_matCharEye;
        private static Material s_matCharScoutHatBrim;
        private static Material s_matCharScoutHatCrown;
        private static Material s_matCharBeanieRed;
        private static Material s_matCharBeaniePom;
        private static Material s_matCharScarfRed;
        private static Material s_matCharScarfCyan;
        private static Material s_matCharPackBrown;
        private static Material s_matCharPackNavy;
        private static Material s_matCharSleepingBag;
        private static Material s_matCharBoot;
        private static Material s_matCharGoldCore;

        private static void EnsureAllMaterials()
        {
            s_matOcean = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_PeakOceanWater.mat", new Color(0.08f, 0.56f, 0.74f, 1f), 0.82f);
            s_matSand = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_PeakBeachSand.mat", new Color(0.86f, 0.78f, 0.58f, 1f), 0.10f);
            s_matGrass = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_PeakAlpineGrass.mat", new Color(0.32f, 0.58f, 0.30f, 1f), 0.12f);
            s_matRock = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_PeakMountainRock.mat", new Color(0.46f, 0.43f, 0.39f, 1f), 0.18f);
            s_matWoodBark = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_PeakWoodBark.mat", new Color(0.38f, 0.26f, 0.18f, 1f), 0.10f);
            s_matFlowerRed = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_PeakFlowerRed.mat", new Color(0.92f, 0.20f, 0.16f, 1f), 0.15f);
            s_matFlowerYellow = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_PeakFlowerYellow.mat", new Color(0.98f, 0.76f, 0.14f, 1f), 0.15f);
            s_matScorchedAsh = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_PeakScorchedAsh.mat", new Color(0.18f, 0.17f, 0.16f, 1f), 0.05f);
            s_baseUrpLitMat = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_StylizedPrimitiveBase.mat", Color.white, 0.2f);

            // VFX & Particle Materials
            Texture2D smokeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/VFX/smoke_01.png");
            Texture2D circleTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/VFX/circle_05.png");
            Texture2D sparkTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/VFX/spark_01.png");

            s_matCloud = GetOrCreateParticleMaterial("Assets/_Project/Materials/MAT_PeakAlpineCloud.mat", new Color(1f, 1f, 1f, 0.40f), smokeTex != null ? smokeTex : circleTex, false);
            s_matParticleDefault = GetOrCreateParticleMaterial("Assets/_Project/Materials/MAT_UrpParticleDefault.mat", Color.white, circleTex, false);
            s_matParticleAdditive = GetOrCreateParticleMaterial("Assets/_Project/Materials/MAT_UrpParticleAdditive.mat", Color.white, sparkTex != null ? sparkTex : circleTex, true);
            s_matParticleDust = GetOrCreateParticleMaterial("Assets/_Project/Materials/MAT_PeakDust.mat", new Color(0.85f, 0.80f, 0.70f, 0.60f), smokeTex != null ? smokeTex : circleTex, false);

            // Stylized Hiker Colors
            s_matCharKalaBlue = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_Kala_Blue.mat", new Color(0.06f, 0.58f, 0.88f, 1f), 0.22f);
            s_matCharSuryaOrange = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_Surya_Orange.mat", new Color(0.96f, 0.48f, 0.12f, 1f), 0.22f);
            s_matCharSkin = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_SkinTone.mat", new Color(0.96f, 0.86f, 0.75f, 1f), 0.12f);
            s_matCharEye = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_EyeDark.mat", new Color(0.08f, 0.09f, 0.12f, 1f), 0.35f);
            s_matCharScoutHatBrim = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_ScoutHat_Brim.mat", new Color(0.86f, 0.66f, 0.22f, 1f), 0.15f);
            s_matCharScoutHatCrown = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_ScoutHat_Crown.mat", new Color(0.90f, 0.72f, 0.26f, 1f), 0.15f);
            s_matCharBeanieRed = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_Beanie_Red.mat", new Color(0.86f, 0.20f, 0.16f, 1f), 0.10f);
            s_matCharBeaniePom = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_Beanie_Pom.mat", new Color(0.96f, 0.94f, 0.88f, 1f), 0.05f);
            s_matCharScarfRed = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_Scarf_Red.mat", new Color(0.90f, 0.24f, 0.20f, 1f), 0.10f);
            s_matCharScarfCyan = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_Scarf_Cyan.mat", new Color(0.16f, 0.76f, 0.86f, 1f), 0.10f);
            s_matCharPackBrown = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_Pack_Brown.mat", new Color(0.72f, 0.45f, 0.18f, 1f), 0.15f);
            s_matCharPackNavy = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_Pack_Navy.mat", new Color(0.22f, 0.38f, 0.60f, 1f), 0.15f);
            s_matCharSleepingBag = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_SleepingBagGreen.mat", new Color(0.22f, 0.74f, 0.42f, 1f), 0.15f);
            s_matCharBoot = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_HikerBoot.mat", new Color(0.25f, 0.18f, 0.14f, 1f), 0.20f);
            s_matCharGoldCore = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_Char_GoldenCore.mat", new Color(0.96f, 0.82f, 0.22f, 1f), 0.45f);
        }

        private static Material GetOrCreateSurfaceMaterial(string path, Color albedo, float smoothness = 0.15f)
        {
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != urpLit && urpLit != null)
            {
                mat.shader = urpLit;
            }
            mat.color = albedo;
            mat.SetColor("_BaseColor", albedo);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateParticleMaterial(string path, Color color, Texture2D texture = null, bool isAdditive = false)
        {
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader && shader != null)
            {
                mat.shader = shader;
            }

            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            if (texture != null)
            {
                mat.SetTexture("_BaseMap", texture);
                mat.SetTexture("_MainTex", texture);
            }

            mat.SetFloat("_Surface", 1.0f); // Transparent
            if (isAdditive)
            {
                mat.SetFloat("_Blend", 2.0f); // Additive
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_BLENDMODE_ADD");
            }
            else
            {
                mat.SetFloat("_Blend", 0.0f); // Alpha
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_BLENDMODE_ADD");
            }
            mat.SetFloat("_ZWrite", 0.0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // =============================================================================
        // SCULPTED ORGANIC 3D MOUNTAIN ISLAND + KENNEY 3D DRESSING + TRANSPARENT BARRIERS
        // =============================================================================
        private static float EvaluateMountainTrailHeight(float z)
        {
            // Smooth natural mountain elevation profile from Z = -40 (0m Beach) to Z = +300 (125m Summit)
            if (z <= -15f) return 0f;
            if (z >= 295f) return 125f;

            float t = Mathf.InverseLerp(-15f, 295f, z);
            float baseAscent = t * 125f;
            // Add natural terraced plateaus at each of the 6 zones so gameplay & puzzles feel grounded
            float terraceWave = Mathf.Sin(t * Mathf.PI * 10f) * 1.4f;
            return Mathf.Clamp(baseAscent + terraceWave, 0f, 125f);
        }

        private static float EvaluateIslandSurfaceHeight(float x, float z)
        {
            float spineY = EvaluateMountainTrailHeight(z);
            float absX = Mathf.Abs(x);

            // Playable mountain ridge width tapers naturally from 32m at the beach to 18m at the summit
            float tZ = Mathf.InverseLerp(-30f, 300f, z);
            float flatRidgeHalfWidth = Mathf.Lerp(26f, 16f, tZ);

            // In Zone 3 (Z = 88 to 128), carve a natural Twin Canyon Gorge down the center (|x| < 5.5m)
            float canyonDip = 0f;
            if (z > 88f && z < 128f && absX < 6.0f)
            {
                float zWeight = Mathf.Sin(Mathf.InverseLerp(88f, 128f, z) * Mathf.PI);
                float xWeight = 1f - Mathf.Clamp01(absX / 6.0f);
                canyonDip = zWeight * xWeight * 9.5f;
            }

            if (absX <= flatRidgeHalfWidth)
            {
                float subtleMound = Mathf.Sin(x * 0.18f) * Mathf.Cos(z * 0.12f) * 0.45f;
                return (spineY - canyonDip) + subtleMound;
            }

            // Outside the playable ridge, slope organically down into the island cliffs & ocean (-3.5m)
            float outerT = Mathf.Clamp01((absX - flatRidgeHalfWidth) / 55f);
            float smoothDrop = outerT * outerT * (3f - 2f * outerT);
            float cragNoise = Mathf.Sin(x * 0.14f + z * 0.11f) * 3.2f * (1f - outerT);
            return Mathf.Lerp(spineY + cragNoise, -3.5f, smoothDrop);
        }

        private static void BuildPeakOpenIslandWorld(
            Transform envRoot,
            GameObject cliffBlockPrefab,
            GameObject cliffLargePrefab,
            GameObject cliffStepsPrefab,
            GameObject cliffSlopePrefab,
            GameObject cliffCornerPrefab,
            GameObject cliffTopPrefab,
            GameObject rockLargeAPrefab,
            GameObject rockLargeBPrefab,
            GameObject rockTallAPrefab,
            GameObject rockTallBPrefab,
            GameObject stoneLargePrefab,
            GameObject stoneTallPrefab,
            GameObject bridgeWoodPrefab,
            GameObject bridgeStonePrefab,
            GameObject statueRingPrefab,
            GameObject statueObeliskPrefab,
            GameObject statueColumnPrefab,
            GameObject treeScorchedPrefab,
            GameObject treeHealthyPrefab,
            GameObject treeTallPrefab,
            GameObject bushPrefab,
            GameObject grassPrefab,
            GameObject flowerRedPrefab,
            GameObject flowerYellowPrefab,
            GameObject mushroomPrefab,
            GameObject tentPrefab,
            GameObject campfirePrefab,
            GameObject signPrefab,
            Texture2D texCircle,
            Texture2D texFire,
            Texture2D texSpark,
            Texture2D texSmoke,
            AudioClip sfxRestore,
            AudioClip sfxCheckpoint,
            AudioClip sfxBouncePad,
            AudioClip sfxSwitch,
            AudioClip sfxShatter,
            AudioClip sfxVictory,
            AudioClip sfxBoulder)
        {
            Material matOcean = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_PeakOceanWater.mat", new Color(0.08f, 0.56f, 0.74f, 1f), 0.82f);
            Material matSand = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_PeakBeachSand.mat", new Color(0.86f, 0.78f, 0.58f, 1f), 0.10f);
            Material matGrass = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_PeakAlpineGrass.mat", new Color(0.32f, 0.58f, 0.30f, 1f), 0.12f);
            Material matRock = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_PeakMountainRock.mat", new Color(0.46f, 0.43f, 0.39f, 1f), 0.18f);

            // 1. INFINITE TROPICAL/ALPINE OCEAN HORIZON
            GameObject oceanPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            oceanPlane.name = "OceanHorizon_SeaSurface";
            Object.DestroyImmediate(oceanPlane.GetComponent<Collider>());
            oceanPlane.transform.SetParent(envRoot);
            oceanPlane.transform.position = new Vector3(0f, -1.6f, 130f);
            oceanPlane.transform.localScale = new Vector3(180f, 1f, 180f);
            oceanPlane.GetComponent<Renderer>().sharedMaterial = matOcean;

            // Sandy Beach Shoreline Ring at Basecamp South
            GameObject beachShore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beachShore.name = "IslandCoast_SandyBeach";
            Object.DestroyImmediate(beachShore.GetComponent<Collider>());
            beachShore.transform.SetParent(envRoot);
            beachShore.transform.position = new Vector3(0f, -1.1f, -22f);
            beachShore.transform.localScale = new Vector3(95f, 1.1f, 65f);
            beachShore.GetComponent<Renderer>().sharedMaterial = matSand;

            // 2. CONTINUOUS SCULPTED 3D ISLAND MOUNTAIN TERRAIN MESH (NO STACKED BOXES!)
            GameObject terrainGo = BuildSculptedIslandTerrainMesh(envRoot, matGrass, matRock);

            // 3. TRANSPARENT / INVISIBLE BOUNDARY BARRIER COLLIDERS (Open 360 View, Safe Bounds)
            CreateInvisibleIslandBoundaryBarriers(envRoot);

            // 4. DISTANT BACKGROUND ISLANDS & VOLUMETRIC CLOUDS AROUND THE SUMMIT
            BuildDistantArchipelagoAndClouds(envRoot, rockTallAPrefab, rockLargeBPrefab, statueRingPrefab, texSmoke);

            // 5. NATURAL KENNEY 3D CLIFF & ROCK FORMATIONS ALONG THE MOUNTAIN FLANKS
            GameObject naturalCliffsRoot = new GameObject("NaturalMountainCliffs_And_Forests");
            naturalCliffsRoot.transform.SetParent(envRoot);

            for (int row = 0; row < 28; row++)
            {
                float t = row / 27f;
                float z = Mathf.Lerp(-28f, 295f, t);
                float y = EvaluateMountainTrailHeight(z);
                float ridgeEdgeX = Mathf.Lerp(25f, 15.5f, t);

                // Left & Right Natural Jagged Cliff Formations framing the open island trail
                GameObject cliffModelL = (row % 3 == 0) ? cliffLargePrefab : ((row % 2 == 0) ? cliffSlopePrefab : rockTallAPrefab);
                GameObject cliffModelR = (row % 3 == 0) ? cliffLargePrefab : ((row % 2 == 0) ? rockTallBPrefab : cliffBlockPrefab);

                SpawnEnvMesh(cliffModelL, naturalCliffsRoot.transform, new Vector3(-ridgeEdgeX - 4.5f, y - 2.5f, z), new Vector3(0f, 75f + (row * 19f), 0f), new Vector3(6.5f, 7.5f, 6.5f));
                SpawnEnvMesh(cliffModelR, naturalCliffsRoot.transform, new Vector3(ridgeEdgeX + 4.5f, y - 2.5f, z), new Vector3(0f, -75f - (row * 23f), 0f), new Vector3(6.5f, 7.5f, 6.5f));

                // Lush Pine Trees, Bushes, Grass & Wildflowers along the open mountain slopes
                if (row % 2 == 0)
                {
                    float treeX = (row % 4 == 0 ? -1f : 1f) * (ridgeEdgeX - 3.5f);
                    float treeY = EvaluateIslandSurfaceHeight(treeX, z);
                    SpawnEnvMesh(treeTallPrefab, naturalCliffsRoot.transform, new Vector3(treeX, treeY - 0.2f, z), new Vector3(0f, row * 47f, 0f), new Vector3(2.8f, 3.4f, 2.8f));
                    SpawnEnvMesh(grassPrefab, naturalCliffsRoot.transform, new Vector3(treeX * 0.7f, EvaluateIslandSurfaceHeight(treeX * 0.7f, z), z + 1.5f), Vector3.zero, new Vector3(2.5f, 2.5f, 2.5f));
                    SpawnEnvMesh((row % 4 == 0) ? flowerRedPrefab : flowerYellowPrefab, naturalCliffsRoot.transform, new Vector3(-treeX * 0.65f, EvaluateIslandSurfaceHeight(-treeX * 0.65f, z), z - 1.2f), Vector3.zero, new Vector3(2.4f, 2.4f, 2.4f));
                }

                // Restorable Scorched Pine Trees along the climb (50+ Restorable Flora)
                float restX = ((row % 2 == 0) ? -1f : 1f) * Mathf.Lerp(7.5f, 11.5f, (row % 3) * 0.5f);
                float restY = EvaluateIslandSurfaceHeight(restX, z);
                CreateRestorableTreeNode(
                    naturalCliffsRoot.transform,
                    $"RestorableTree_{row:00}",
                    new Vector3(restX, restY, z),
                    treeScorchedPrefab,
                    treeHealthyPrefab,
                    bushPrefab,
                    mushroomPrefab,
                    texCircle,
                    sfxRestore
                );
            }

            // =========================================================================
            // 6. SIX MOUNTAIN ZONES, CHECKPOINT CAMPS & 5 CO-OP BALL/CLIMBING PUZZLES
            // =========================================================================
            GameObject puzzlesRoot = new GameObject("MountainZones_Checkpoints_And_Puzzles");
            puzzlesRoot.transform.SetParent(envRoot);

            // --- ZONA 1: PANTAI & LEMBAH AWAL (Z = -24, Y = 0m -> 18m) ---
            CreateCheckpointCamp(puzzlesRoot.transform, "Checkpoint_Zone1", new Vector3(-7.5f, EvaluateIslandSurfaceHeight(-7.5f, -22f), -22f), 1, "Zona 1: Pantai & Lembah Awal (0m)", tentPrefab, campfirePrefab, signPrefab, texFire, sfxCheckpoint);

            // Puzzle 1 (Z = 22, Y = 15m): Natural Rock Outcrop Wall-Climb + Co-op Bridge Switch + Bounce Mushroom
            float z1CliffY = EvaluateMountainTrailHeight(22f);
            GameObject z1RockWall = SpawnEnvMeshWithRoot(cliffLargePrefab, puzzlesRoot.transform, "Z1_NaturalClimbOutcrop", new Vector3(0f, z1CliffY - 0.5f, 22f), Vector3.zero, new Vector3(8.5f, 4.2f, 5.5f));
            SpawnEnvMesh(cliffStepsPrefab, puzzlesRoot.transform, new Vector3(-9.5f, z1CliffY - 0.5f, 22f), Vector3.zero, new Vector3(5.0f, 3.8f, 5.0f));
            SpawnEnvMesh(cliffBlockPrefab, puzzlesRoot.transform, new Vector3(9.5f, z1CliffY - 0.5f, 22f), Vector3.zero, new Vector3(5.0f, 3.8f, 5.0f));
            CreateCoopBridgeObstacle(puzzlesRoot.transform, new Vector3(6.5f, z1CliffY + 3.6f, 22.5f), 180f, bridgeStonePrefab, sfxSwitch);
            CreateBounceMushroom(puzzlesRoot.transform, "Z1_HelperBounceMushroom", new Vector3(-13.5f, EvaluateIslandSurfaceHeight(-13.5f, 18f), 18f), mushroomPrefab, sfxBouncePad);

            // --- ZONA 2: LERENG HUTAN & GERBANG SOKET KRISTAL (Z = 34 -> 80, Y = 20m -> 42m) ---
            CreateCheckpointCamp(puzzlesRoot.transform, "Checkpoint_Zone2", new Vector3(-8.5f, EvaluateIslandSurfaceHeight(-8.5f, 34f), 34f), 2, "Zona 2: Lereng Hutan & Gerbang Kristal (22m)", tentPrefab, campfirePrefab, signPrefab, texFire, sfxCheckpoint);

            float z2GateY = EvaluateMountainTrailHeight(76f);
            CreateCrystalSocketGatePuzzle(
                puzzlesRoot.transform,
                "Z2_AncientCrystalRockGate",
                gatePos: new Vector3(0f, z2GateY + 3.5f, 76f),
                socketPos: new Vector3(-8.5f, z2GateY + 4.2f, 71f),
                cliffLargePrefab,
                statueRingPrefab,
                statueColumnPrefab,
                texSpark,
                sfxVictory
            );

            // --- ZONA 3: NGARAI KEMBAR & BARIKADE BATU (Z = 84 -> 140, Y = 42m -> 65m) ---
            CreateCheckpointCamp(puzzlesRoot.transform, "Checkpoint_Zone3", new Vector3(9.0f, EvaluateIslandSurfaceHeight(9.0f, 84f), 84f), 3, "Zona 3: Ngarai Kembar & Barikade Batu (42m)", tentPrefab, campfirePrefab, signPrefab, texFire, sfxCheckpoint);

            // Natural Wood Bridge across the Twin Canyon Gorge + Recovery Mushrooms in the gorge
            float z3MidY = EvaluateMountainTrailHeight(106f);
            SpawnEnvMesh(bridgeWoodPrefab, puzzlesRoot.transform, new Vector3(0f, z3MidY - 0.2f, 106f), new Vector3(0f, 90f, 0f), new Vector3(5.2f, 3.8f, 5.2f));
            CreateBounceMushroom(puzzlesRoot.transform, "Z3_GorgeMushroom_A", new Vector3(0f, EvaluateIslandSurfaceHeight(0f, 98f), 98f), mushroomPrefab, sfxBouncePad);
            CreateBounceMushroom(puzzlesRoot.transform, "Z3_GorgeMushroom_B", new Vector3(0f, EvaluateIslandSurfaceHeight(0f, 118f), 118f), mushroomPrefab, sfxBouncePad);

            // Sweet-Spot Breakable Stone Barricade at Z = 136
            float z3BarY = EvaluateMountainTrailHeight(136f);
            SpawnEnvMesh(cliffLargePrefab, puzzlesRoot.transform, new Vector3(-13f, z3BarY - 1f, 136f), Vector3.zero, new Vector3(6.5f, 5.5f, 4.5f));
            SpawnEnvMesh(cliffLargePrefab, puzzlesRoot.transform, new Vector3(13f, z3BarY - 1f, 136f), new Vector3(0f, 180f, 0f), new Vector3(6.5f, 5.5f, 4.5f));
            CreateBreakableBarricade(puzzlesRoot.transform, "Z3_SweetSpotRockBarricade", new Vector3(0f, z3BarY + 2.2f, 136f), 0f, rockLargeAPrefab, texSpark, sfxShatter);

            // --- ZONA 4: TEBING ANGIN & BATU MELAYANG (Z = 144 -> 204, Y = 65m -> 90m) ---
            CreateCheckpointCamp(puzzlesRoot.transform, "Checkpoint_Zone4", new Vector3(-8.5f, EvaluateIslandSurfaceHeight(-8.5f, 144f), 144f), 4, "Zona 4: Tebing Angin & Batu Melayang (65m)", tentPrefab, campfirePrefab, signPrefab, texFire, sfxCheckpoint);

            float z4MoveY1 = EvaluateMountainTrailHeight(174f) + 2.5f;
            float z4MoveY2 = EvaluateMountainTrailHeight(190f) + 2.5f;
            CreateMovingRockLedge(puzzlesRoot.transform, "Z4_MovingCliffStone_Left", new Vector3(-6.5f, z4MoveY1, 174f), new Vector3(-6.5f, z4MoveY2, 190f), cliffTopPrefab, matRock);
            CreateMovingRockLedge(puzzlesRoot.transform, "Z4_MovingCliffStone_Right", new Vector3(6.5f, z4MoveY2, 190f), new Vector3(6.5f, z4MoveY1, 174f), cliffTopPrefab, matRock);

            float z4GateY = EvaluateMountainTrailHeight(202f);
            CreateCrystalSocketGatePuzzle(
                puzzlesRoot.transform,
                "Z4_HighAltitudeCrystalGate",
                gatePos: new Vector3(0f, z4GateY + 3.5f, 202f),
                socketPos: new Vector3(8.5f, z4GateY + 4.2f, 197f),
                cliffLargePrefab,
                statueRingPrefab,
                statueColumnPrefab,
                texSpark,
                sfxVictory
            );

            // --- ZONA 5: LERENG GUGURAN BATU VULKANIK (Z = 208 -> 265, Y = 90m -> 114m) ---
            CreateCheckpointCamp(puzzlesRoot.transform, "Checkpoint_Zone5", new Vector3(-8.5f, EvaluateIslandSurfaceHeight(-8.5f, 209f), 209f), 5, "Zona 5: Lereng Guguran Batu (90m)", tentPrefab, campfirePrefab, signPrefab, texFire, sfxCheckpoint);

            for (int c = 0; c < 4; c++)
            {
                float cz = Mathf.Lerp(218f, 252f, (c + 0.5f) / 4f);
                float cx = (c % 2 == 0) ? -6.0f : 6.0f;
                float cy = EvaluateIslandSurfaceHeight(cx, cz);
                SpawnEnvMesh(rockLargeBPrefab, puzzlesRoot.transform, new Vector3(cx, cy - 0.3f, cz), new Vector3(0f, c * 45f, 0f), new Vector3(4.5f, 4.2f, 4.5f));
            }

            float z5TopY = EvaluateMountainTrailHeight(258f);
            CreateRollingBoulderSpawner(puzzlesRoot.transform, "RollingBoulder_Left", new Vector3(-4.5f, z5TopY + 3.5f, 258f), rockLargeAPrefab, texSmoke, sfxBoulder);
            CreateRollingBoulderSpawner(puzzlesRoot.transform, "RollingBoulder_Right", new Vector3(4.5f, z5TopY + 3.5f, 255f), rockLargeAPrefab, texSmoke, sfxBoulder);
            CreateBreakableBarricade(puzzlesRoot.transform, "Z5_SummitPassBarricade", new Vector3(0f, EvaluateMountainTrailHeight(264f) + 2.2f, 264f), 0f, rockLargeAPrefab, texSpark, sfxShatter);

            // --- ZONA 6: PUNCAK MAHKOTA HARMONI — ALTAR SURYA KENCANA 125M (Z = 270 -> 300, Y = 115m -> 125m) ---
            CreateCheckpointCamp(puzzlesRoot.transform, "Checkpoint_Zone6", new Vector3(-7.5f, EvaluateIslandSurfaceHeight(-7.5f, 272f), 272f), 6, "Zona 6: Altar Surya Kencana (125m)", tentPrefab, campfirePrefab, signPrefab, texFire, sfxCheckpoint);

            Vector3 finalSummitPos = new Vector3(0f, 125.0f, 296f);
            SpawnEnvMesh(statueRingPrefab, puzzlesRoot.transform, finalSummitPos, Vector3.zero, new Vector3(6.2f, 6.2f, 6.2f));
            SpawnEnvMesh(statueObeliskPrefab, puzzlesRoot.transform, finalSummitPos + new Vector3(-8.5f, -0.3f, 4.5f), Vector3.zero, new Vector3(4.5f, 5.2f, 4.5f));
            SpawnEnvMesh(statueObeliskPrefab, puzzlesRoot.transform, finalSummitPos + new Vector3(8.5f, -0.3f, 4.5f), Vector3.zero, new Vector3(4.5f, 5.2f, 4.5f));
            SpawnEnvMesh(statueColumnPrefab, puzzlesRoot.transform, finalSummitPos + new Vector3(-8.5f, -0.3f, -4.5f), Vector3.zero, new Vector3(3.8f, 4.5f, 3.8f));
            SpawnEnvMesh(statueColumnPrefab, puzzlesRoot.transform, finalSummitPos + new Vector3(8.5f, -0.3f, -4.5f), Vector3.zero, new Vector3(3.8f, 4.5f, 3.8f));

            // Stylized Candi Bentar Nusantara Megalithic Split Gate at Summit Entrance
            SpawnEnvMesh(statueObeliskPrefab, puzzlesRoot.transform, finalSummitPos + new Vector3(-4.2f, -0.4f, -8.5f), new Vector3(0f, 0f, -4f), new Vector3(3.8f, 6.4f, 3.2f));
            SpawnEnvMesh(statueObeliskPrefab, puzzlesRoot.transform, finalSummitPos + new Vector3(4.2f, -0.4f, -8.5f), new Vector3(0f, 0f, 4f), new Vector3(3.8f, 6.4f, 3.2f));

            // Towering Natural Peak Spire Behind the Altar
            SpawnEnvMesh(rockTallAPrefab, puzzlesRoot.transform, finalSummitPos + new Vector3(0f, -2f, 16f), Vector3.zero, new Vector3(18f, 28f, 18f));
            SpawnEnvMesh(rockTallBPrefab, puzzlesRoot.transform, finalSummitPos + new Vector3(-16f, -4f, 14f), new Vector3(0f, 35f, 0f), new Vector3(14f, 22f, 14f));
            SpawnEnvMesh(rockTallBPrefab, puzzlesRoot.transform, finalSummitPos + new Vector3(16f, -4f, 14f), new Vector3(0f, -35f, 0f), new Vector3(14f, 22f, 14f));

            CreateSummitGoalTrigger(puzzlesRoot.transform, finalSummitPos + new Vector3(0f, 1.4f, 0f), texSpark, sfxVictory);
        }

        private static GameObject BuildSculptedIslandTerrainMesh(Transform parent, Material grassMat, Material rockMat)
        {
            const int xSteps = 64;
            const int zSteps = 140;
            const float minX = -85f;
            const float maxX = 85f;
            const float minZ = -48f;
            const float maxZ = 335f;

            int vertCount = (xSteps + 1) * (zSteps + 1);
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            for (int iz = 0; iz <= zSteps; iz++)
            {
                float tz = iz / (float)zSteps;
                float z = Mathf.Lerp(minZ, maxZ, tz);

                for (int ix = 0; ix <= xSteps; ix++)
                {
                    float tx = ix / (float)xSteps;
                    float x = Mathf.Lerp(minX, maxX, tx);

                    float y = EvaluateIslandSurfaceHeight(x, z);
                    // Taper South (-Z) and North (+Z) ends into the ocean naturally
                    if (z < -28f)
                    {
                        float southDrop = Mathf.InverseLerp(-28f, minZ, z);
                        y = Mathf.Lerp(y, -3.5f, southDrop * southDrop);
                    }
                    else if (z > 308f)
                    {
                        float northDrop = Mathf.InverseLerp(308f, maxZ, z);
                        y = Mathf.Lerp(y, -3.5f, northDrop * northDrop);
                    }

                    int idx = iz * (xSteps + 1) + ix;
                    vertices[idx] = new Vector3(x, y, z);
                    uvs[idx] = new Vector2(tx * 24f, tz * 48f);
                }
            }

            List<int> grassTris = new List<int>(xSteps * zSteps * 3);
            List<int> rockTris = new List<int>(xSteps * zSteps * 3);

            for (int iz = 0; iz < zSteps; iz++)
            {
                for (int ix = 0; ix < xSteps; ix++)
                {
                    int i0 = iz * (xSteps + 1) + ix;
                    int i1 = i0 + 1;
                    int i2 = (iz + 1) * (xSteps + 1) + ix;
                    int i3 = i2 + 1;

                    Vector3 v0 = vertices[i0];
                    Vector3 v1 = vertices[i1];
                    Vector3 v2 = vertices[i2];
                    Vector3 n = Vector3.Cross(v2 - v0, v1 - v0).normalized;

                    // Assign steep cliffs or high volcanic altitude to Rock submesh, gentle meadows to Grass submesh
                    bool isRock = (n.y < 0.78f) || (Mathf.Abs(v0.x) > 22f) || (v0.y > 78f && Mathf.Abs(v0.x) > 8f);
                    List<int> targetList = isRock ? rockTris : grassTris;

                    targetList.Add(i0);
                    targetList.Add(i2);
                    targetList.Add(i1);

                    targetList.Add(i1);
                    targetList.Add(i2);
                    targetList.Add(i3);
                }
            }

            Mesh islandMesh = new Mesh();
            islandMesh.name = "Mesh_PeakIslandTerrain";
            islandMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            islandMesh.vertices = vertices;
            islandMesh.uv = uvs;
            islandMesh.subMeshCount = 2;
            islandMesh.SetTriangles(grassTris, 0);
            islandMesh.SetTriangles(rockTris, 1);
            islandMesh.RecalculateNormals();
            islandMesh.RecalculateBounds();

            string meshAssetPath = "Assets/_Project/Models/Environment/Mesh_PeakIslandTerrain.asset";
            Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
            if (existingMesh != null)
            {
                EditorUtility.CopySerialized(islandMesh, existingMesh);
                AssetDatabase.SaveAssets();
                islandMesh = existingMesh;
            }
            else
            {
                AssetDatabase.CreateAsset(islandMesh, meshAssetPath);
            }

            GameObject terrainGo = new GameObject("IslandMountain_OrganicTerrainMesh");
            terrainGo.transform.SetParent(parent);
            terrainGo.transform.position = Vector3.zero;

            MeshFilter mf = terrainGo.AddComponent<MeshFilter>();
            mf.sharedMesh = islandMesh;

            MeshRenderer mr = terrainGo.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { grassMat, rockMat };

            MeshCollider mc = terrainGo.AddComponent<MeshCollider>();
            mc.sharedMesh = islandMesh;

            return terrainGo;
        }

        private static void CreateInvisibleIslandBoundaryBarriers(Transform envRoot)
        {
            GameObject barrierRoot = new GameObject("InvisibleBoundaryBarriers_Transparent");
            barrierRoot.transform.SetParent(envRoot);

            // Left, Right, South & North Transparent Colliders (Zero MeshRenderer -> 100% Open View!)
            CreateInvisibleColliderWall(barrierRoot.transform, "InvisibleBarrier_WestCoast", new Vector3(-34f, 65f, 135f), new Vector3(4f, 160f, 380f));
            CreateInvisibleColliderWall(barrierRoot.transform, "InvisibleBarrier_EastCoast", new Vector3(34f, 65f, 135f), new Vector3(4f, 160f, 380f));
            CreateInvisibleColliderWall(barrierRoot.transform, "InvisibleBarrier_SouthOcean", new Vector3(0f, 65f, -38f), new Vector3(80f, 160f, 4f));
            CreateInvisibleColliderWall(barrierRoot.transform, "InvisibleBarrier_NorthSummitBack", new Vector3(0f, 125f, 314f), new Vector3(80f, 120f, 4f));
        }

        private static void CreateInvisibleColliderWall(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            BoxCollider bc = wall.AddComponent<BoxCollider>();
            bc.size = size;
        }

        private static void BuildDistantArchipelagoAndClouds(
            Transform envRoot,
            GameObject rockTallPrefab,
            GameObject rockLargePrefab,
            GameObject statueRingPrefab,
            Texture2D cloudTex)
        {
            GameObject vistaRoot = new GameObject("DistantArchipelago_And_SummitBeacon");
            vistaRoot.transform.SetParent(envRoot);

            // Distant Sea Stacks & Islands in the Ocean for true PEAK Island atmosphere
            Vector3[] seaIslands = new Vector3[]
            {
                new Vector3(-140f, -2f, 40f),
                new Vector3(155f, -2f, 65f),
                new Vector3(-165f, -2f, 195f),
                new Vector3(175f, -2f, 215f),
                new Vector3(-95f, -2f, -95f),
                new Vector3(110f, -2f, -85f)
            };

            for (int i = 0; i < seaIslands.Length; i++)
            {
                SpawnEnvMesh(rockTallPrefab, vistaRoot.transform, seaIslands[i], new Vector3(0f, i * 52f, 0f), new Vector3(24f, 32f + (i * 4f), 24f));
                SpawnEnvMesh(rockLargePrefab, vistaRoot.transform, seaIslands[i] + new Vector3(16f, 0f, -10f), new Vector3(0f, i * 30f, 0f), new Vector3(18f, 14f, 18f));
            }

            // Glowing Summit Beacon visible all the way from the beach
            GameObject beaconGo = new GameObject("Summit125m_CelestialBeaconLight");
            beaconGo.transform.SetParent(vistaRoot.transform);
            beaconGo.transform.position = new Vector3(0f, 132f, 296f);
            Light l = beaconGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.15f, 0.98f, 0.72f);
            l.range = 180f;
            l.intensity = 8.0f;

            // Soft Alpine Clouds Drifting Around the Mountain Mid-Slope & Summit
            CreateDriftingCloudRingVfx(vistaRoot.transform, new Vector3(0f, 55f, 120f), cloudTex);
            CreateDriftingCloudRingVfx(vistaRoot.transform, new Vector3(0f, 112f, 265f), cloudTex);
        }

        private static void CreateDriftingCloudRingVfx(Transform parent, Vector3 pos, Texture2D tex)
        {
            GameObject psGo = new GameObject("AlpineDriftingClouds_VFX");
            psGo.transform.SetParent(parent);
            psGo.transform.position = pos;

            ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = psGo.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                psr.sharedMaterial = s_matCloud;
            }
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(4.5f, 9.5f);
            main.startColor = new Color(0.95f, 0.98f, 1.0f, 0.28f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 10f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(95f, 18f, 95f);
        }

        // =============================================================================
        // STYLIZED HIKER CHARACTERS (NON-GRAYBOX MULTI-PART VISUALS)
        // =============================================================================
        private static BeanClimberController CreateStylizedClimber(
            Transform parent,
            string name,
            Vector3 position,
            Color jacketColor,
            bool isExplorerHat,
            CharacterMovementConfigSO cfg,
            AudioClip[] footsteps,
            AudioClip jumpClip,
            AudioClip bonkClip)
        {
            GameObject actor = new GameObject(name);
            actor.transform.SetParent(parent);
            actor.transform.position = position;

            Rigidbody rb = actor.AddComponent<Rigidbody>();
            rb.mass = 1.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            CapsuleCollider col = actor.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.42f;
            col.height = 1.8f;

            GameObject visualRoot = new GameObject("Visual_Mesh");
            visualRoot.transform.SetParent(actor.transform);
            visualRoot.transform.localPosition = Vector3.zero;

            // Check if Anggota 1 has already exported the custom Blender FBX
            string customFbxPath = isExplorerHat
                ? "Assets/_Project/Models/Characters/SK_Char_SahabatA.fbx"
                : "Assets/_Project/Models/Characters/SK_Char_SahabatB.fbx";
            GameObject customFbx = AssetDatabase.LoadAssetAtPath<GameObject>(customFbxPath);

            if (customFbx != null)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(customFbx, visualRoot.transform);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // Rich Stylized Multi-Part Hiker Model (Vibrant Colors, Hat/Beanie, Scarf, Backpack & Boots!)
                Material jacketMat = isExplorerHat ? s_matCharKalaBlue : s_matCharSuryaOrange;
                GameObject bodyMesh = CreatePart(visualRoot.transform, PrimitiveType.Capsule, "ClimberTorso", new Vector3(0f, 0.92f, 0f), new Vector3(0.84f, 0.86f, 0.84f), jacketMat);

                // Warm Face Visor / Skin Tone
                CreatePart(visualRoot.transform, PrimitiveType.Sphere, "FaceHead", new Vector3(0f, 1.24f, 0.14f), new Vector3(0.68f, 0.56f, 0.62f), s_matCharSkin);

                // Expressive Eyes
                CreatePart(visualRoot.transform, PrimitiveType.Sphere, "Eye_L", new Vector3(-0.15f, 1.27f, 0.41f), new Vector3(0.12f, 0.16f, 0.08f), s_matCharEye);
                CreatePart(visualRoot.transform, PrimitiveType.Sphere, "Eye_R", new Vector3(0.15f, 1.27f, 0.41f), new Vector3(0.12f, 0.16f, 0.08f), s_matCharEye);

                // Explorer Sun Hat (Sahabat A / Kala) or Cozy Mountain Beanie (Sahabat B / Surya)
                if (isExplorerHat)
                {
                    CreatePart(visualRoot.transform, PrimitiveType.Cylinder, "ScoutHat_Brim", new Vector3(0f, 1.56f, 0.02f), new Vector3(0.96f, 0.05f, 0.96f), s_matCharScoutHatBrim);
                    CreatePart(visualRoot.transform, PrimitiveType.Sphere, "ScoutHat_Crown", new Vector3(0f, 1.66f, 0.02f), new Vector3(0.62f, 0.34f, 0.62f), s_matCharScoutHatCrown);
                }
                else
                {
                    CreatePart(visualRoot.transform, PrimitiveType.Sphere, "Beanie_Dome", new Vector3(0f, 1.54f, 0f), new Vector3(0.74f, 0.42f, 0.74f), s_matCharBeanieRed);
                    CreatePart(visualRoot.transform, PrimitiveType.Sphere, "Beanie_PomPom", new Vector3(0f, 1.78f, -0.04f), new Vector3(0.24f, 0.24f, 0.24f), s_matCharBeaniePom);
                }

                // Red/Cyan Mountain Scarf
                CreatePart(visualRoot.transform, PrimitiveType.Cylinder, "NeckScarf", new Vector3(0f, 1.02f, 0.03f), new Vector3(0.78f, 0.09f, 0.78f), isExplorerHat ? s_matCharScarfRed : s_matCharScarfCyan);

                // Detailed Hiker Backpack + Sleeping Bag Roll on Top
                Material packMat = isExplorerHat ? s_matCharPackBrown : s_matCharPackNavy;
                CreatePart(visualRoot.transform, PrimitiveType.Cube, "HikerBackpack", new Vector3(0f, 0.92f, -0.40f), new Vector3(0.56f, 0.66f, 0.34f), packMat);
                GameObject roll = CreatePart(visualRoot.transform, PrimitiveType.Cylinder, "SleepingBagRoll", new Vector3(0f, 1.32f, -0.40f), new Vector3(0.22f, 0.30f, 0.22f), s_matCharSleepingBag);
                roll.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                // Climbing Hands & Boots
                CreatePart(visualRoot.transform, PrimitiveType.Sphere, "Hand_L", new Vector3(-0.46f, 0.88f, 0.18f), new Vector3(0.22f, 0.22f, 0.22f), s_matCharSkin);
                CreatePart(visualRoot.transform, PrimitiveType.Sphere, "Hand_R", new Vector3(0.46f, 0.88f, 0.18f), new Vector3(0.22f, 0.22f, 0.22f), s_matCharSkin);
                CreatePart(visualRoot.transform, PrimitiveType.Cube, "Boot_L", new Vector3(-0.20f, 0.12f, 0.08f), new Vector3(0.24f, 0.22f, 0.36f), s_matCharBoot);
                CreatePart(visualRoot.transform, PrimitiveType.Cube, "Boot_R", new Vector3(0.20f, 0.12f, 0.08f), new Vector3(0.24f, 0.22f, 0.36f), s_matCharBoot);
            }

            GameObject holdPoint = new GameObject("BallHoldPoint");
            holdPoint.transform.SetParent(actor.transform);
            holdPoint.transform.localPosition = new Vector3(0f, 1.35f, 0.68f);

            LineRenderer line = actor.AddComponent<LineRenderer>();
            line.startWidth = 0.14f;
            line.endWidth = 0.03f;
            line.useWorldSpace = true;
            line.enabled = false;
            line.sharedMaterial = s_matParticleAdditive;

            Texture2D dustTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/VFX/smoke_01.png");
            if (dustTex == null) dustTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/VFX/circle_05.png");
            ParticleSystem dustPs = CreateOneShotBurstParticles(actor.transform, "FootstepDust_VFX", new Color(0.88f, 0.84f, 0.75f, 0.65f), dustTex, 12, false);
            dustPs.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            BeanClimberController controller = actor.AddComponent<BeanClimberController>();
            actor.AddComponent<CompanionFollowerAI>();

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("_config").objectReferenceValue = cfg;
            so.FindProperty("_visualModelRoot").objectReferenceValue = visualRoot.transform;
            so.FindProperty("_ballHoldPoint").objectReferenceValue = holdPoint.transform;
            so.FindProperty("_trajectoryLine").objectReferenceValue = line;
            so.FindProperty("_footstepDustVfx").objectReferenceValue = dustPs;
            so.FindProperty("_jumpClip").objectReferenceValue = jumpClip;
            so.FindProperty("_bonkStumbleClip").objectReferenceValue = bonkClip;

            SerializedProperty footstepsProp = so.FindProperty("_footstepClips");
            footstepsProp.arraySize = footsteps.Length;
            for (int i = 0; i < footsteps.Length; i++)
            {
                footstepsProp.GetArrayElementAtIndex(i).objectReferenceValue = footsteps[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static GameObject CreatePart(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
            }
            return go;
        }

        // =============================================================================
        // PUZZLE & ENVIRONMENT HELPERS (USING KENNEY 3D MODELS)
        // =============================================================================
        private static void CreateCrystalSocketGatePuzzle(
            Transform parent,
            string name,
            Vector3 gatePos,
            Vector3 socketPos,
            GameObject cliffLargePrefab,
            GameObject statueRingPrefab,
            GameObject statueColumnPrefab,
            Texture2D sparkTex,
            AudioClip unlockClip)
        {
            GameObject puzzleRoot = new GameObject(name);
            puzzleRoot.transform.SetParent(parent);

            GameObject gateWall = new GameObject("AncientRockGate_Barrier");
            gateWall.transform.SetParent(puzzleRoot.transform);
            gateWall.transform.position = gatePos;
            BoxCollider gateCol = gateWall.AddComponent<BoxCollider>();
            gateCol.size = new Vector3(36f, 9.5f, 3.0f);

            SpawnEnvMesh(cliffLargePrefab, gateWall.transform, gatePos + new Vector3(-9f, -4.5f, 0f), Vector3.zero, new Vector3(7.5f, 4.8f, 3.5f));
            SpawnEnvMesh(cliffLargePrefab, gateWall.transform, gatePos + new Vector3(9f, -4.5f, 0f), new Vector3(0f, 180f, 0f), new Vector3(7.5f, 4.8f, 3.5f));
            SpawnEnvMesh(statueColumnPrefab, gateWall.transform, gatePos + new Vector3(-4.5f, -4.2f, -1.2f), Vector3.zero, new Vector3(3.5f, 4.5f, 3.5f));
            SpawnEnvMesh(statueColumnPrefab, gateWall.transform, gatePos + new Vector3(4.5f, -4.2f, -1.2f), Vector3.zero, new Vector3(3.5f, 4.5f, 3.5f));

            GameObject socketGo = new GameObject("CrystalSocket_TargetRing");
            socketGo.transform.SetParent(puzzleRoot.transform);
            socketGo.transform.position = socketPos;

            SphereCollider col = socketGo.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 2.6f;

            GameObject ringVis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ringVis.name = "SocketCoreOrb";
            Object.DestroyImmediate(ringVis.GetComponent<Collider>());
            ringVis.transform.SetParent(socketGo.transform);
            ringVis.transform.localPosition = Vector3.zero;
            ringVis.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            Renderer orbRenderer = ringVis.GetComponent<Renderer>();
            SetRendererColor(orbRenderer, new Color(1.0f, 0.48f, 0.12f, 1f));

            if (statueRingPrefab != null)
            {
                GameObject sRing = (GameObject)PrefabUtility.InstantiatePrefab(statueRingPrefab, socketGo.transform);
                sRing.transform.localPosition = new Vector3(0f, -1.6f, 0f);
                sRing.transform.localScale = new Vector3(2.8f, 2.8f, 2.8f);
                foreach (Renderer r in sRing.GetComponentsInChildren<Renderer>())
                {
                    SetRendererAllMaterials(r, s_matRock);
                }
            }

            Light sockLight = socketGo.AddComponent<Light>();
            sockLight.type = LightType.Point;
            sockLight.range = 12f;
            sockLight.intensity = 3.5f;
            sockLight.color = new Color(1.0f, 0.52f, 0.15f);

            AudioSource aSrc = socketGo.AddComponent<AudioSource>();
            aSrc.playOnAwake = false;
            aSrc.clip = unlockClip;

            ParticleSystem unlockPs = CreateOneShotBurstParticles(socketGo.transform, "SocketUnlock_VFX", new Color(0.2f, 1f, 0.65f, 1f), sparkTex, 36, true);

            CrystalSocketPedestal csp = socketGo.AddComponent<CrystalSocketPedestal>();
            SerializedObject so = new SerializedObject(csp);
            so.FindProperty("_gateWallTransform").objectReferenceValue = gateWall.transform;
            so.FindProperty("_openOffset").vector3Value = new Vector3(0f, -11.5f, 0f);
            so.FindProperty("_ringRenderer").objectReferenceValue = orbRenderer;
            so.FindProperty("_socketLight").objectReferenceValue = sockLight;
            so.FindProperty("_unlockParticles").objectReferenceValue = unlockPs;
            so.FindProperty("_unlockAudio").objectReferenceValue = aSrc;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateMovingRockLedge(Transform parent, string name, Vector3 pointA, Vector3 pointB, GameObject cliffTopPrefab, Material rockMat)
        {
            GameObject plat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plat.name = name;
            Object.DestroyImmediate(plat.GetComponent<Collider>());
            BoxCollider platCol = plat.AddComponent<BoxCollider>();
            platCol.size = new Vector3(5.8f, 0.65f, 5.8f);
            plat.transform.SetParent(parent);
            plat.transform.position = pointA;
            plat.transform.localScale = new Vector3(5.8f, 0.65f, 5.8f);
            SetRendererAllMaterials(plat.GetComponent<Renderer>(), rockMat);

            if (cliffTopPrefab != null)
            {
                GameObject rockVis = (GameObject)PrefabUtility.InstantiatePrefab(cliffTopPrefab, plat.transform);
                rockVis.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                rockVis.transform.localScale = new Vector3(0.65f, 1.2f, 0.65f);
                foreach (Renderer r in rockVis.GetComponentsInChildren<Renderer>())
                {
                    SetRendererAllMaterials(r, rockMat);
                }
            }

            MovingLedgePlatform mlp = plat.AddComponent<MovingLedgePlatform>();
            SerializedObject so = new SerializedObject(mlp);
            so.FindProperty("_pointA").vector3Value = pointA;
            so.FindProperty("_pointB").vector3Value = pointB;
            so.FindProperty("_speed").floatValue = 0.48f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateRollingBoulderSpawner(
            Transform parent,
            string name,
            Vector3 spawnPos,
            GameObject rockPrefab,
            Texture2D smokeTex,
            AudioClip impactClip)
        {
            GameObject boulderGo = new GameObject(name);
            boulderGo.transform.SetParent(parent);
            boulderGo.transform.position = spawnPos;

            SphereCollider sc = boulderGo.AddComponent<SphereCollider>();
            sc.radius = 1.45f;

            if (rockPrefab != null)
            {
                GameObject rVis = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, boulderGo.transform);
                rVis.transform.localPosition = new Vector3(0f, -1.1f, 0f);
                rVis.transform.localScale = new Vector3(2.8f, 2.8f, 2.8f);
                foreach (Renderer r in rVis.GetComponentsInChildren<Renderer>())
                {
                    SetRendererAllMaterials(r, s_matRock);
                }
            }

            Rigidbody rb = boulderGo.AddComponent<Rigidbody>();
            rb.mass = 16f;

            AudioSource aSrc = boulderGo.AddComponent<AudioSource>();
            aSrc.playOnAwake = false;
            aSrc.spatialBlend = 1f;
            aSrc.clip = impactClip;

            ParticleSystem dustPs = CreateOneShotBurstParticles(boulderGo.transform, "BoulderShatter_VFX", new Color(0.95f, 0.45f, 0.15f, 1f), smokeTex, 24, true);

            RollingBoulderHazard rbh = boulderGo.AddComponent<RollingBoulderHazard>();
            SerializedObject so = new SerializedObject(rbh);
            so.FindProperty("_rollDirection").vector3Value = new Vector3(0f, -0.45f, -1f);
            so.FindProperty("_rollAcceleration").floatValue = 13.0f;
            so.FindProperty("_maxSpeed").floatValue = 15.0f;
            so.FindProperty("_resetLifetime").floatValue = 6.0f;
            so.FindProperty("_rumbleAudio").objectReferenceValue = aSrc;
            so.FindProperty("_dustTrailParticles").objectReferenceValue = dustPs;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCheckpointCamp(
            Transform parent,
            string name,
            Vector3 pos,
            int zoneIndex,
            string zoneTitle,
            GameObject tentPrefab,
            GameObject campfirePrefab,
            GameObject signPrefab,
            Texture2D fireTex,
            AudioClip clip)
        {
            GameObject campGo = new GameObject(name);
            campGo.transform.SetParent(parent);
            campGo.transform.position = pos;

            SphereCollider col = campGo.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 4.5f;

            if (campfirePrefab != null)
            {
                GameObject cf = (GameObject)PrefabUtility.InstantiatePrefab(campfirePrefab, campGo.transform);
                cf.transform.localPosition = Vector3.zero;
                cf.transform.localScale = new Vector3(2.6f, 2.6f, 2.6f);
                foreach (Renderer r in cf.GetComponentsInChildren<Renderer>())
                {
                    SetRendererAllMaterials(r, s_matWoodBark);
                }
            }

            if (tentPrefab != null)
            {
                GameObject tent = (GameObject)PrefabUtility.InstantiatePrefab(tentPrefab, campGo.transform);
                tent.transform.localPosition = new Vector3(-2.4f, 0f, 1.5f);
                tent.transform.localRotation = Quaternion.Euler(0f, 135f, 0f);
                tent.transform.localScale = new Vector3(2.8f, 2.8f, 2.8f);
                foreach (Renderer r in tent.GetComponentsInChildren<Renderer>())
                {
                    SetRendererAllMaterials(r, s_matSand);
                }
            }

            if (signPrefab != null)
            {
                GameObject sign = (GameObject)PrefabUtility.InstantiatePrefab(signPrefab, campGo.transform);
                sign.transform.localPosition = new Vector3(1.9f, 0f, 1.2f);
                sign.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
                foreach (Renderer r in sign.GetComponentsInChildren<Renderer>())
                {
                    SetRendererAllMaterials(r, s_matWoodBark);
                }
            }

            GameObject lightGo = new GameObject("CampfireLight");
            lightGo.transform.SetParent(campGo.transform);
            lightGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.65f, 0.2f);
            l.range = 12f;
            l.intensity = 3.8f;

            ParticleSystem firePs = CreateOneShotBurstParticles(campGo.transform, "CampfireBurst_VFX", new Color(1f, 0.65f, 0.15f, 1f), fireTex, 26, true);

            MountainCheckpointCamp cp = campGo.AddComponent<MountainCheckpointCamp>();
            SerializedObject so = new SerializedObject(cp);
            so.FindProperty("_zoneIndex").intValue = zoneIndex;
            so.FindProperty("_zoneName").stringValue = zoneTitle;
            so.FindProperty("_campfireLight").objectReferenceValue = l;
            so.FindProperty("_campfireFlameVfx").objectReferenceValue = firePs;
            so.FindProperty("_checkpointActivateClip").objectReferenceValue = clip;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateBounceMushroom(
            Transform parent,
            string name,
            Vector3 pos,
            GameObject mushroomPrefab,
            AudioClip bounceClip)
        {
            GameObject padGo = new GameObject(name);
            padGo.transform.SetParent(parent);
            padGo.transform.position = pos;

            SphereCollider col = padGo.AddComponent<SphereCollider>();
            col.radius = 1.35f;
            col.center = new Vector3(0f, 0.5f, 0f);

            GameObject visRoot = new GameObject("MushroomVisual");
            visRoot.transform.SetParent(padGo.transform);
            visRoot.transform.localPosition = Vector3.zero;

            if (mushroomPrefab != null)
            {
                GameObject mInst = (GameObject)PrefabUtility.InstantiatePrefab(mushroomPrefab, visRoot.transform);
                mInst.transform.localPosition = Vector3.zero;
                mInst.transform.localScale = new Vector3(4.8f, 4.0f, 4.8f);
                foreach (Renderer r in mInst.GetComponentsInChildren<Renderer>())
                {
                    SetRendererAllMaterials(r, s_matFlowerRed);
                }
            }

            BounceMushroomPad pad = padGo.AddComponent<BounceMushroomPad>();
            SerializedObject so = new SerializedObject(pad);
            so.FindProperty("_visualRoot").objectReferenceValue = visRoot.transform;
            so.FindProperty("_bounceClip").objectReferenceValue = bounceClip;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateBreakableBarricade(
            Transform parent,
            string name,
            Vector3 pos,
            float yaw,
            GameObject rockPrefab,
            Texture2D sparkTex,
            AudioClip shatterClip)
        {
            GameObject barGo = new GameObject(name);
            barGo.transform.SetParent(parent);
            barGo.transform.position = pos;
            barGo.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            BoxCollider col = barGo.AddComponent<BoxCollider>();
            col.size = new Vector3(8.5f, 4.8f, 2.2f);

            GameObject vis = new GameObject("BarricadeRocks_Visual");
            vis.transform.SetParent(barGo.transform);
            vis.transform.localPosition = Vector3.zero;

            if (rockPrefab != null)
            {
                for (int i = -1; i <= 1; i++)
                {
                    GameObject rInst = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, vis.transform);
                    rInst.transform.localPosition = new Vector3(i * 2.6f, -1.8f, 0f);
                    rInst.transform.localScale = new Vector3(3.6f, 4.2f, 3.2f);
                    foreach (Renderer r in rInst.GetComponentsInChildren<Renderer>())
                    {
                        SetRendererAllMaterials(r, s_matRock);
                    }
                }
            }

            ParticleSystem shatterPs = CreateOneShotBurstParticles(barGo.transform, "Shatter_VFX", new Color(1f, 0.55f, 0.2f, 1f), sparkTex, 40, true);

            BreakableStoneBarricade bsb = barGo.AddComponent<BreakableStoneBarricade>();
            SerializedObject so = new SerializedObject(bsb);
            so.FindProperty("_shatterVfx").objectReferenceValue = shatterPs;
            so.FindProperty("_shatterClip").objectReferenceValue = shatterClip;
            so.FindProperty("_visualRoot").objectReferenceValue = vis;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCoopBridgeObstacle(Transform parent, Vector3 pos, float yaw, GameObject bridgeStonePrefab, AudioClip switchClip)
        {
            GameObject bridgeHolder = new GameObject("CoopBridge_Mechanism");
            bridgeHolder.transform.SetParent(parent);
            bridgeHolder.transform.position = pos;
            bridgeHolder.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            GameObject bridgePlank = new GameObject("ExtendableStoneBridge");
            bridgePlank.transform.SetParent(bridgeHolder.transform);
            bridgePlank.transform.localPosition = new Vector3(0f, -1.6f, 4.2f);
            bridgePlank.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
            BoxCollider bCol = bridgePlank.AddComponent<BoxCollider>();
            bCol.size = new Vector3(4.2f, 0.5f, 10.5f);

            if (bridgeStonePrefab != null)
            {
                GameObject bMesh = (GameObject)PrefabUtility.InstantiatePrefab(bridgeStonePrefab, bridgePlank.transform);
                bMesh.transform.localPosition = Vector3.zero;
                bMesh.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                bMesh.transform.localScale = new Vector3(4.2f, 3.5f, 4.2f);
                foreach (Renderer r in bMesh.GetComponentsInChildren<Renderer>())
                {
                    SetRendererAllMaterials(r, s_matRock);
                }
            }

            GameObject switchPad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            switchPad.name = "CoopPressureSwitch";
            switchPad.transform.SetParent(bridgeHolder.transform);
            switchPad.transform.localPosition = new Vector3(-2.4f, 0.15f, -1.0f);
            switchPad.transform.localScale = new Vector3(1.8f, 0.16f, 1.8f);
            Material switchMat = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_CoopSwitchPlate.mat", new Color(0.98f, 0.80f, 0.15f), 0.35f);
            switchPad.GetComponent<Renderer>().sharedMaterial = switchMat;

            Object.DestroyImmediate(switchPad.GetComponent<Collider>());
            BoxCollider trig = switchPad.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.size = new Vector3(1.4f, 3.0f, 1.4f);

            CoopBridgeSwitch cbs = switchPad.AddComponent<CoopBridgeSwitch>();
            SerializedObject so = new SerializedObject(cbs);
            so.FindProperty("_bridgeToExtend").objectReferenceValue = bridgePlank.transform;
            so.FindProperty("_activateClip").objectReferenceValue = switchClip;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateSummitGoalTrigger(Transform parent, Vector3 pos, Texture2D sparkTex, AudioClip victoryClip)
        {
            GameObject goalGo = new GameObject("SummitAltar_VictoryGoal");
            goalGo.transform.SetParent(parent);
            goalGo.transform.position = pos;

            SphereCollider col = goalGo.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 4.2f;

            Light beacon = goalGo.AddComponent<Light>();
            beacon.type = LightType.Point;
            beacon.color = new Color(0.15f, 0.95f, 0.65f);
            beacon.range = 24f;
            beacon.intensity = 5.5f;

            ParticleSystem victoryPs = CreateOneShotBurstParticles(goalGo.transform, "SummitVictory_VFX", new Color(0.2f, 1f, 0.6f, 1f), sparkTex, 65, true);

            SummitAltarGoal goal = goalGo.AddComponent<SummitAltarGoal>();
            SerializedObject so = new SerializedObject(goal);
            so.FindProperty("_victoryFinaleVfx").objectReferenceValue = victoryPs;
            so.FindProperty("_altarBeaconLight").objectReferenceValue = beacon;
            so.FindProperty("_victoryChimeClip").objectReferenceValue = victoryClip;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static GameObject SpawnEnvMeshWithRoot(GameObject prefab, Transform parent, string name, Vector3 pos, Vector3 euler, Vector3 scale)
        {
            GameObject holder = new GameObject(name);
            holder.transform.SetParent(parent);
            holder.transform.position = pos;
            SpawnEnvMesh(prefab, holder.transform, pos, euler, scale);
            return holder;
        }

        private static void SpawnEnvMesh(GameObject prefab, Transform parent, Vector3 pos, Vector3 euler, Vector3 scale)
        {
            if (prefab == null) return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = pos;
            instance.transform.rotation = Quaternion.Euler(euler);
            instance.transform.localScale = scale;

            string lower = prefab.name.ToLowerInvariant();
            Material targetMat = s_matRock;
            if (lower.Contains("flower_red")) targetMat = s_matFlowerRed;
            else if (lower.Contains("flower_yellow")) targetMat = s_matFlowerYellow;
            else if (lower.Contains("grass") || lower.Contains("bush") || lower.Contains("tree_tall") || lower.Contains("mushroom")) targetMat = s_matGrass;
            else if (lower.Contains("scorched") || lower.Contains("burned")) targetMat = s_matScorchedAsh;
            else if (lower.Contains("tree") || lower.Contains("wood") || lower.Contains("log")) targetMat = s_matWoodBark;

            foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
            {
                SetRendererAllMaterials(r, targetMat);
            }

            foreach (MeshFilter mf in instance.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
        }

        private static void CreateRestorableTreeNode(
            Transform parent,
            string name,
            Vector3 pos,
            GameObject scorchedPrefab,
            GameObject healthyPrefab,
            GameObject bushPrefab,
            GameObject mushroomPrefab,
            Texture2D burstTex,
            AudioClip restoreClip)
        {
            GameObject node = new GameObject(name);
            node.transform.SetParent(parent);
            node.transform.position = pos;

            SphereCollider triggerCol = node.AddComponent<SphereCollider>();
            triggerCol.isTrigger = true;
            triggerCol.radius = 1.6f;
            triggerCol.center = new Vector3(0f, 1.0f, 0f);

            GameObject scorchedGo = new GameObject("Scorched_Visual");
            scorchedGo.transform.SetParent(node.transform);
            scorchedGo.transform.localPosition = Vector3.zero;
            if (scorchedPrefab != null)
            {
                GameObject sInst = (GameObject)PrefabUtility.InstantiatePrefab(scorchedPrefab, scorchedGo.transform);
                sInst.transform.localPosition = Vector3.zero;
                sInst.transform.localScale = new Vector3(2.8f, 2.8f, 2.8f);
                foreach (Renderer r in sInst.GetComponentsInChildren<Renderer>())
                {
                    SetRendererAllMaterials(r, s_matScorchedAsh);
                }
            }

            GameObject healthyGo = new GameObject("Healthy_Visual");
            healthyGo.transform.SetParent(node.transform);
            healthyGo.transform.localPosition = Vector3.zero;
            if (healthyPrefab != null)
            {
                GameObject hInst = (GameObject)PrefabUtility.InstantiatePrefab(healthyPrefab, healthyGo.transform);
                hInst.transform.localPosition = Vector3.zero;
                hInst.transform.localScale = new Vector3(3.2f, 3.2f, 3.2f);
                foreach (Renderer r in hInst.GetComponentsInChildren<Renderer>())
                {
                    SetRendererAllMaterials(r, s_matGrass);
                }
            }
            if (bushPrefab != null)
            {
                GameObject bInst = (GameObject)PrefabUtility.InstantiatePrefab(bushPrefab, healthyGo.transform);
                bInst.transform.localPosition = new Vector3(0.5f, 0f, 0.4f);
                bInst.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
                foreach (Renderer r in bInst.GetComponentsInChildren<Renderer>())
                {
                    SetRendererAllMaterials(r, s_matGrass);
                }
            }
            healthyGo.SetActive(false);

            ParticleSystem burstPs = CreateOneShotBurstParticles(node.transform, "RestoreBurst_VFX", new Color(0.2f, 0.95f, 0.55f, 1f), burstTex, 16, true);

            RestorableFoliage rf = node.AddComponent<RestorableFoliage>();
            SerializedObject so = new SerializedObject(rf);
            so.FindProperty("_scorchedModelVisual").objectReferenceValue = scorchedGo;
            so.FindProperty("_healthyModelVisual").objectReferenceValue = healthyGo;
            so.FindProperty("_restoreBurstVfx").objectReferenceValue = burstPs;
            so.FindProperty("_restoreChimeClip").objectReferenceValue = restoreClip;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TerraCoreBall CreateTerraCoreBall(
            Transform parent,
            Vector3 position,
            Texture2D sparkTex,
            Texture2D circleTex,
            AudioClip throwClip,
            AudioClip sweetSpotClip,
            AudioClip slowMoClip,
            AudioClip catchClip,
            AudioClip bounceClip)
        {
            GameObject ballGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballGo.name = "TerraCore_CrystalBall";
            ballGo.transform.SetParent(parent);
            ballGo.transform.position = position;
            ballGo.transform.localScale = new Vector3(0.58f, 0.58f, 0.58f);
            Material ballMat = GetOrCreateSurfaceMaterial("Assets/_Project/Materials/MAT_MustikaPertiwi_Crystal.mat", new Color(0.08f, 0.95f, 0.75f, 1f), 0.90f);
            ballGo.GetComponent<Renderer>().sharedMaterial = ballMat;

            // Inner Golden Core Ring for Stylized Polish
            CreatePart(ballGo.transform, PrimitiveType.Cylinder, "CoreGoldenRing", Vector3.zero, new Vector3(1.18f, 0.08f, 1.18f), s_matCharGoldCore);

            Rigidbody rb = ballGo.AddComponent<Rigidbody>();
            rb.mass = 0.8f;

            Light glowLight = ballGo.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(0f, 0.92f, 1f);
            glowLight.range = 9.5f;
            glowLight.intensity = 3.2f;

            GameObject auraRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            auraRing.name = "SanctuaryAura_RingVisual";
            Object.DestroyImmediate(auraRing.GetComponent<Collider>());
            auraRing.transform.SetParent(parent);
            auraRing.transform.position = position;
            auraRing.transform.localScale = new Vector3(7.5f, 0.04f, 7.5f);
            Material auraMat = GetOrCreateParticleMaterial("Assets/_Project/Materials/MAT_SanctuaryAura.mat", new Color(0.12f, 0.92f, 0.65f, 0.35f), circleTex, true);
            auraRing.GetComponent<Renderer>().sharedMaterial = auraMat;

            TrailRenderer trail = ballGo.AddComponent<TrailRenderer>();
            trail.time = 0.38f;
            trail.startWidth = 0.35f;
            trail.endWidth = 0.02f;
            trail.emitting = false;
            trail.sharedMaterial = s_matParticleAdditive;

            ParticleSystem flightSparks = CreateOneShotBurstParticles(ballGo.transform, "FlightSparks_VFX", new Color(0f, 0.95f, 1f, 1f), sparkTex, 24, true);
            ParticleSystem catchShockwave = CreateOneShotBurstParticles(ballGo.transform, "CatchShockwave_VFX", new Color(1f, 0.88f, 0.25f, 1f), circleTex, 32, true);

            TerraCoreBall ball = ballGo.AddComponent<TerraCoreBall>();
            SerializedObject so = new SerializedObject(ball);
            so.FindProperty("_auraVisualSphere").objectReferenceValue = auraRing.transform;
            so.FindProperty("_flightSparkParticles").objectReferenceValue = flightSparks;
            so.FindProperty("_catchShockwaveParticles").objectReferenceValue = catchShockwave;
            so.FindProperty("_ballTrail").objectReferenceValue = trail;
            so.FindProperty("_throwLaunchClip").objectReferenceValue = throwClip;
            so.FindProperty("_sweetSpotLaunchClip").objectReferenceValue = sweetSpotClip;
            so.FindProperty("_slowMoEnterClip").objectReferenceValue = slowMoClip;
            so.FindProperty("_perfectCatchClip").objectReferenceValue = catchClip;
            so.FindProperty("_bounceImpactClip").objectReferenceValue = bounceClip;
            so.ApplyModifiedPropertiesWithoutUndo();

            return ball;
        }

        private static ParticleSystem CreateOneShotBurstParticles(Transform parent, string name, Color color, Texture2D tex, int burstCount, bool isAdditive = false)
        {
            GameObject psGo = new GameObject(name);
            psGo.transform.SetParent(parent);
            psGo.transform.localPosition = Vector3.zero;

            ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = psGo.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                psr.sharedMaterial = isAdditive ? s_matParticleAdditive : s_matParticleDefault;
            }
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;

            return ps;
        }

        private static void SetRendererColor(Renderer r, Color color)
        {
            if (r == null) return;
            string hex = ColorUtility.ToHtmlStringRGB(color);
            string path = $"Assets/_Project/Materials/Generated/MAT_Color_{hex}.mat";
            Material m = GetOrCreateSurfaceMaterial(path, color, 0.2f);
            SetRendererAllMaterials(r, m);
        }

        private static void SetRendererAllMaterials(Renderer r, Material mat)
        {
            if (r == null || mat == null) return;
            int count = Mathf.Max(1, r.sharedMaterials.Length);
            Material[] mats = new Material[count];
            for (int i = 0; i < count; i++)
            {
                mats[i] = mat;
            }
            r.sharedMaterials = mats;
        }
    }
}
#endif
