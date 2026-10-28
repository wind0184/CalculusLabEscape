#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static class GameAudioAuthoring
    {
        public const string Root="Assets/_Project/Audio/";
        public const string MixerPath=Root+"GameAudio.mixer",BankPath=Root+"MissionSounds.asset";
        static readonly string[] Groups={"Master","UI","Feedback","Correct","Retry","Hint","Robot","Ambience","River","Mechanisms","Portal","Bridge","Switch","Pressure","Shutdown","Completion","Briefing"};
        static readonly string[] Parents={"","Master","Master","Feedback","Feedback","Master","Master","Master","Ambience","Master","Mechanisms","Mechanisms","Mechanisms","Mechanisms","Mechanisms","Master","Master"};
        const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Flags).Invoke(target,args);
        static object Property(object target,string name)=>target.GetType().GetProperty(name,Flags).GetValue(target);
        static void Field(object target,string name,object value)=>target.GetType().GetField(name,Flags).SetValue(target,value);
        static AudioMixer EnsureMixer()
        {
            var existing=AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);if(existing!=null)return existing;
            var type=Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor.AudioModule")??AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.Audio.AudioMixerController")).First(t=>t!=null);
            var mixer=(AudioMixer)type.GetMethod("CreateMixerControllerAtPath",Flags).Invoke(null,new object[]{MixerPath});
            var groups=new Dictionary<string,object>{{"Master",Property(mixer,"masterGroup")}};
            for(int i=1;i<Groups.Length;i++){
                var group=Call(mixer,"CreateNewGroup",Groups[i],false);Call(mixer,"AddChildToParent",group,groups[Parents[i]]);groups.Add(Groups[i],group);
            }
            // Expose every fader for later volume controls; runtime never overwrites these values.
            var exposed=type.GetProperty("exposedParameters",Flags);var itemType=exposed.PropertyType.GetElementType();
            var parameters=Array.CreateInstance(itemType,Groups.Length);
            for(int i=0;i<Groups.Length;i++){
                var parameter=Activator.CreateInstance(itemType);Field(parameter,"name",Groups[i]+"Volume");Field(parameter,"guid",Call(groups[Groups[i]],"GetGUIDForVolume"));parameters.SetValue(parameter,i);
            }
            exposed.SetValue(mixer,parameters);
            // Make all saved channels visible together when the mixer is opened.
            var viewProperty=type.GetProperty("views",Flags);var viewType=viewProperty.PropertyType.GetElementType();var view=Activator.CreateInstance(viewType);
            Field(view,"name","All Sounds");var guidType=Property(groups["Master"],"groupID").GetType();var guids=Array.CreateInstance(guidType,Groups.Length);
            for(int i=0;i<Groups.Length;i++)guids.SetValue(Property(groups[Groups[i]],"groupID"),i);
            Field(view,"guids",guids);var views=Array.CreateInstance(viewType,1);views.SetValue(view,0);viewProperty.SetValue(mixer,views);
            foreach(var group in groups.Values)EditorUtility.SetDirty((Object)group);
            EditorUtility.SetDirty(mixer);AssetDatabase.SaveAssets();return mixer;
        }
        static AudioMixerGroup Group(AudioMixer mixer,string name)=>mixer.FindMatchingGroups(name).Single(g=>g.name==name);
        static MissionSoundBank EnsureBank(AudioMixer mixer)
        {
            var bank=AssetDatabase.LoadAssetAtPath<MissionSoundBank>(BankPath);
            if(bank==null){bank=ScriptableObject.CreateInstance<MissionSoundBank>();AssetDatabase.CreateAsset(bank,BankPath);}
            var cues=new List<MissionSoundBank.Cue>();
            foreach(string name in new[]{"Click","Correct","Retry","Hint","Robot","River","Switch","Pressure","Portal","Completion","Bridge","Shutdown"}){
                string path=name=="Bridge"?Root+"GeneratedMission/WoodBridgeInstall.wav":name=="Shutdown"?Root+"GeneratedMission/LaboratoryShutdown.wav":Root+"LibraryMission/"+name+".wav";
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
                settings.loadType=name=="River"?AudioClipLoadType.CompressedInMemory:AudioClipLoadType.DecompressOnLoad;
                settings.preloadAudioData=true;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                settings.compressionFormat=name=="River"?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;settings.quality=1;
                importer.forceToMono=false;importer.loadInBackground=false;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
                cues.Add(new MissionSoundBank.Cue{id=name.ToLowerInvariant(),clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path),output=Group(mixer,name=="Click"?"UI":name),volume=.85f});
            }
            bank.mixer=mixer;bank.cues=cues.ToArray();EditorUtility.SetDirty(bank);AssetDatabase.SaveAssets();return bank;
        }
        static void Buttons(GameObject root)
        {
            foreach(var button in root.GetComponentsInChildren<Button>(true)){
                var sound=button.GetComponent<ButtonAudio>()??button.gameObject.AddComponent<ButtonAudio>();
                sound.cue=button.name=="SupportOpen"||button.name=="PuzzleSupport"||button.name=="ManualOpen"?"hint":"click";
                bool bound=false;for(int i=0;i<button.onClick.GetPersistentEventCount();i++)if(button.onClick.GetPersistentTarget(i)==sound&&button.onClick.GetPersistentMethodName(i)=="Play")bound=true;
                if(!bound){UnityEventTools.AddPersistentListener(button.onClick,sound.Play);var data=new SerializedObject(button);var calls=data.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");calls.MoveArrayElement(calls.arraySize-1,0);data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(button);}
                EditorUtility.SetDirty(sound);
            }
        }
        static AudioSource Source(Transform parent,string name,AudioMixerGroup group)
        {
            var child=parent.Find(name);if(child==null){child=new GameObject(name).transform;child.SetParent(parent,false);}
            var source=child.GetComponent<AudioSource>();if(source==null)source=child.gameObject.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.volume=.85f;source.outputAudioMixerGroup=group;
            source.ignoreListenerVolume=false;source.ignoreListenerPause=false;EditorUtility.SetDirty(source);return source;
        }
        [MenuItem("Calculus Lab/Audio/Apply complete sound bank and mixer")]
        public static void Apply()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save open scenes before applying audio.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);var setup=EditorSceneManager.GetSceneManagerSetup();
            try{
                var mixer=EnsureMixer();var bank=EnsureBank(mixer);
                foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project/Prefabs/Resources/Production"})){
                    string path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);
                    try{Buttons(root);foreach(var briefing in root.GetComponentsInChildren<MissionBriefingVideo>(true))briefing.voice.outputAudioMixerGroup=Group(mixer,"Briefing");PrefabUtility.SaveAsPrefabAsset(root,path);}
                    finally{PrefabUtility.UnloadPrefabContents(root);}
                }
                foreach(string name in new[]{"MainMenu"}.Concat(Chapter4Game.Scenes)){
                    var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+name+".unity");var manager=Object.FindFirstObjectByType<GameManager>();
                    var audio=Object.FindFirstObjectByType<SceneSoundscape>();if(audio==null)audio=new GameObject("Game Audio").AddComponent<SceneSoundscape>();
                    if(name=="MainMenu"){
                        var listener=Object.FindFirstObjectByType<AudioListener>();
                        if(listener!=null&&listener.gameObject==manager.menuEntrance.lobbyCamera.gameObject){Object.DestroyImmediate(listener);listener=null;}
                        if(listener==null){listener=audio.gameObject.AddComponent<AudioListener>();EditorUtility.SetDirty(listener);}
                    }
                    audio.manager=manager;audio.bank=bank;audio.ui=Source(audio.transform,"UI and Hint",Group(mixer,"UI"));audio.hint=Source(audio.transform,"Hint",Group(mixer,"Hint"));audio.feedback=Source(audio.transform,"Answer Feedback",Group(mixer,"Correct"));
                    audio.robot=Source(audio.transform,"Robot",Group(mixer,"Robot"));audio.ambience=Source(audio.transform,"River Ambience",Group(mixer,"River"));
                    audio.effects=Source(audio.transform,"Mission Effects",Group(mixer,"Portal"));audio.completion=Source(audio.transform,"Completion",Group(mixer,"Completion"));
                    if((int)manager.mission==1){audio.ambience.clip=bank.Find("river").clip;audio.ambience.loop=true;EditorUtility.SetDirty(audio.ambience);}
                    if(manager.world!=null&&manager.world.CaseAudio!=null){
                        manager.world.CaseAudio.soundBank=bank;
                        var source=manager.world.CaseAudio.GetComponent<AudioSource>();source.outputAudioMixerGroup=Group(mixer,manager.world.topic==1?"Bridge":"Switch");EditorUtility.SetDirty(source);EditorUtility.SetDirty(manager.world.CaseAudio);
                    }
                    foreach(var root in scene.GetRootGameObjects())Buttons(root);
                    foreach(var briefing in Object.FindObjectsByType<MissionBriefingVideo>(FindObjectsInactive.Include,FindObjectsSortMode.None)){briefing.voice.outputAudioMixerGroup=Group(mixer,"Briefing");EditorUtility.SetDirty(briefing.voice);}
                    EditorUtility.SetDirty(audio);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();Validate();
            }finally{if(setup.Any(s=>s.isLoaded))EditorSceneManager.RestoreSceneManagerSetup(setup);}
            Debug.Log("GAME_AUDIO_SAVED / 12 cues, 17 mixer faders, four scenes");
        }
        public static void Validate()
        {
            var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);var bank=AssetDatabase.LoadAssetAtPath<MissionSoundBank>(BankPath);
            if(mixer==null||bank==null||bank.cues.Length!=12)throw new Exception("Missing complete mixer/sound bank");
            foreach(string name in Groups){if(Group(mixer,name)==null||!mixer.GetFloat(name+"Volume",out _))throw new Exception("Missing mixer fader: "+name);}
            if(bank.cues.Any(c=>c.clip==null||c.output==null||c.output.audioMixer!=mixer))throw new Exception("Unrouted sound bank cue");
            foreach(string name in new[]{"MainMenu"}.Concat(Chapter4Game.Scenes)){
                var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+name+".unity");var audio=Object.FindObjectsByType<SceneSoundscape>(FindObjectsSortMode.None);
                if(audio.Length!=1||audio[0].bank!=bank)throw new Exception("Missing saved soundscape: "+name);
                if(new[]{audio[0].ui,audio[0].hint,audio[0].feedback,audio[0].robot,audio[0].ambience,audio[0].effects,audio[0].completion}.Any(source=>source==null))throw new Exception("Missing saved playback source: "+name);
                if(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length!=1)throw new Exception("Scene needs exactly one active saved audio listener: "+name);
                var world=Object.FindFirstObjectByType<Chapter4World>();if(world!=null&&world.topic>0&&(world.CaseAudio==null||world.CaseAudio.soundBank!=bank))throw new Exception("Missing physical sound bank: "+name);
                foreach(var root in scene.GetRootGameObjects()){
                    foreach(var source in root.GetComponentsInChildren<AudioSource>(true))if(source.outputAudioMixerGroup==null||source.outputAudioMixerGroup.audioMixer!=mixer||source.ignoreListenerVolume)throw new Exception("Unrouted source: "+name+"/"+source.name);
                    foreach(var button in root.GetComponentsInChildren<Button>(true)){
                        if(button.GetComponents<ButtonAudio>().Length!=1)throw new Exception("Missing/duplicate button audio: "+name+"/"+button.name);
                        if((button.name=="SupportOpen"||button.name=="PuzzleSupport"||button.name=="ManualOpen")&&button.GetComponent<ButtonAudio>().cue!="hint")throw new Exception("Hint button routed as ordinary click: "+name);
                    }
                }
            }
            Debug.Log("GAME_AUDIO_VALIDATION_PASS / every source routed, every button saved, exposed controls present");
        }
        public static void ApplyAndBuild(){Apply();MissionSoundAuthoring.Validate();AssessmentTrackingChecks.Validate();ManualEditingMigration.BuildQA();}
        public static void BuildRelease(){Validate();MissionSoundAuthoring.BuildRelease();}
    }
}
#endif
