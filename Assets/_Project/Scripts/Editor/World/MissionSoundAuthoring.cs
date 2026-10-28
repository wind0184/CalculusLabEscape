#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    // Explicit Edit Mode import and scene wiring. Runtime does not author these assets.
    public static class MissionSoundAuthoring
    {
        const string AudioRoot="Assets/_Project/Audio/GeneratedMission/";
        static readonly string[] Names={"WoodBridgeInstall.wav","LaboratoryShutdown.wav"};
        static void Import(string name)
        {
            string path=AudioRoot+name;
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);
            importer.forceToMono=false;importer.loadInBackground=false;
            var settings=importer.defaultSampleSettings;
            settings.preloadAudioData=true;
            settings.loadType=AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=AudioCompressionFormat.PCM;
            settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        }
        [MenuItem("Calculus Lab/Audio/Apply supplied mission sounds")]
        public static void Apply()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save open scenes before applying mission sounds.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string name in Names)Import(name);
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(int topic in new[]{1,2})
                {
                    var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");
                    var world=Object.FindFirstObjectByType<Chapter4World>();
                    var audio=world.GetComponent<MissionAudio>()??world.gameObject.AddComponent<MissionAudio>();
                    var source=world.GetComponent<AudioSource>();
                    if(source==null)source=world.gameObject.AddComponent<AudioSource>();
                    source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.volume=.32f;
                    source.ignoreListenerVolume=false;source.ignoreListenerPause=false;source.pitch=1;
                    var sound=new SerializedObject(audio);
                    sound.FindProperty("bridgeInstallClip").objectReferenceValue=topic==1?AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot+Names[0]):null;
                    sound.FindProperty("shutdownClip").objectReferenceValue=topic==2?AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot+Names[1]):null;
                    sound.FindProperty("importedClipVolume").floatValue=.85f;sound.ApplyModifiedPropertiesWithoutUndo();
                    var data=new SerializedObject(world);
                    data.FindProperty("<CaseAudio>k__BackingField").objectReferenceValue=audio;
                    data.FindProperty("bridgeInstallSoundDelay").floatValue=.36f;data.ApplyModifiedPropertiesWithoutUndo();
                    if(topic==2){var sequence=world.GetComponent<CaseShutdownSequence>();sequence.soundStartProgress=.24f;EditorUtility.SetDirty(sequence);}
                    EditorUtility.SetDirty(source);EditorUtility.SetDirty(audio);EditorUtility.SetDirty(world);
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();Validate();
            }
            finally{if(setup.Any(s=>s.isLoaded))EditorSceneManager.RestoreSceneManagerSetup(setup);}
            Debug.Log("MISSION_SOUND_ASSETS_SAVED / two WAV clips, saved AudioSources and scene references");
        }
        public static void Validate()
        {
            foreach(int topic in new[]{1,2})
            {
                EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");
                var world=Object.FindFirstObjectByType<Chapter4World>();var audio=world.CaseAudio;
                if(audio==null||world.GetComponents<MissionAudio>().Length!=1||world.GetComponents<AudioSource>().Length!=1)throw new Exception("Missing or duplicate saved mission audio: "+topic);
                var source=audio.GetComponent<AudioSource>();
                if(source.playOnAwake||source.loop||source.ignoreListenerVolume||source.spatialBlend!=0)throw new Exception("Invalid saved playback settings: "+topic);
                var clip=topic==1?audio.BridgeInstallClip:audio.ShutdownClip;
                if(clip==null||clip.frequency!=48000||clip.channels!=2||Mathf.Abs(clip.length-(topic==1?1.2f:2f))>.015f)throw new Exception("Imported clip format or duration: "+topic);
                if(topic==2&&Mathf.Abs(world.GetComponent<CaseShutdownSequence>().soundStartProgress-.24f)>.001f)throw new Exception("Shutdown cue timing changed");
            }
            Debug.Log("MISSION_SOUND_REFERENCES_PASS / two authored clips and non-looping, master-controlled sources");
        }
        public static void ApplyAndBuild(){Apply();ManualEditingMigration.Validate();AssessmentTrackingChecks.Validate();ManualEditingMigration.BuildQA();}
        public static void BuildRelease(){Validate();BeginnerFixAuthoring.BuildRelease();}
    }
}
#endif
