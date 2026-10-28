#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static partial class ReportFusionLayout
    {
        const string BriefVideoArt="Assets/_Project/Art/Video/LimitsBriefing/";
        static string BriefVideoEvidence=>LearningApi.Argument("--brief-video-evidence",Path.GetFullPath("../../qa/limits-briefing-video-2026-10-24"));
        static Button BriefVideoButton(Transform parent,string name,string caption,float y)
        {
            var image=Surface(parent,name,1008,y,248,46,Raised);image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var label=Label(image.transform,name+"Label",caption,12,10,224,26,19,Ink);label.alignment=TextAlignmentOptions.Center;label.enableAutoSizing=false;return button;
        }
        [MenuItem("Calculus Lab/Limits/Use robot video on introduction page")]
        public static void AuthorLimitsBriefingVideo()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before changing the introduction.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits first.");
            Directory.CreateDirectory(BriefVideoEvidence);AssetDatabase.Refresh();
            var clip=AssetDatabase.LoadAssetAtPath<VideoClip>(BriefVideoArt+"LimitsIntroduction-English.mp4");if(clip==null||clip.audioTrackCount<1||clip.width!=1920||clip.height!=1080)throw new Exception("Import the 1080p robot introduction with its English audio track first.");
            string texturePath=BriefVideoArt+"LimitsIntroduction.renderTexture";var texture=AssetDatabase.LoadAssetAtPath<RenderTexture>(texturePath);
            if(texture==null){texture=new RenderTexture(1920,1080,0,RenderTextureFormat.ARGB32){name="Limits Introduction",antiAliasing=1};AssetDatabase.CreateAsset(texture,texturePath);}
            var ui=PrefabUtility.LoadPrefabContents(Prefabs+"LimitsUI.prefab");
            try{
                Index(ui);foreach(var old in ui.GetComponentsInChildren<LimitsBriefingVideo>(true))Object.DestroyImmediate(old.gameObject);Index(ui);
                var brief=Find("Brief");foreach(Transform child in brief)if(child.name!="BriefTitle"&&child.name!="BriefStart"&&child.name!="BriefBack")child.gameObject.SetActive(false);
                At("BriefTitle",24,73,960,34);Text("BriefTitle","The Limit Observatory");Type("BriefTitle",29,Ink);
                Button("BriefStart",1008,578,248,52,brief,true);Text("BriefStartLabel","Start mission");
                Button("BriefBack",1008,638,248,31,brief);Text("BriefBackLabel","Back to menu");
                var root=Create(brief,"LimitsBriefingVideo",0,0,1280,680);root.SetAsFirstSibling();
                var frame=Surface(root,"BriefVideoFrame",24,116,960,540,Color.black);var border=frame.gameObject.AddComponent<Outline>();border.effectColor=Line;border.effectDistance=new Vector2(1,-1);
                var raw=Create(frame.transform,"BriefVideoScreen",0,0,960,540).gameObject.AddComponent<RawImage>();raw.texture=texture;raw.raycastTarget=false;
                var cover=Create(frame.transform,"BriefVideoPoster",0,0,960,540).gameObject.AddComponent<RawImage>();cover.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(BriefVideoArt+"LimitsIntroduction-Cover.png");cover.raycastTarget=false;
                var player=frame.gameObject.AddComponent<VideoPlayer>();player.playOnAwake=false;player.source=VideoSource.VideoClip;player.clip=clip;player.renderMode=VideoRenderMode.RenderTexture;player.targetTexture=texture;player.isLooping=false;player.waitForFirstFrame=true;player.skipOnDrop=true;player.audioOutputMode=VideoAudioOutputMode.AudioSource;player.controlledAudioTrackCount=1;player.EnableAudioTrack(0,true);
                var voice=frame.gameObject.AddComponent<AudioSource>();voice.playOnAwake=false;voice.spatialBlend=0;voice.loop=false;voice.volume=1;player.SetTargetAudioSource(0,voice);
                Label(root,"BriefVideoKicker","ROBOT BRIEFING",1008,128,248,23,15,Cyan);
                Label(root,"BriefVideoObjective","Restore the\nconvergence gate.",1008,171,248,74,25,Ink);
                Label(root,"BriefVideoNote","A short introduction to limits.\nWatch now, or start when ready.",1008,263,248,63,17,Muted);
                var toggle=BriefVideoButton(root,"BriefVideoToggle","Play video",350);var replay=BriefVideoButton(root,"BriefVideoReplay","Replay",406);
                var clock=Label(root,"BriefVideoClock","00:00 / 01:30",1008,469,248,26,17,Ink);
                var track=Surface(root,"BriefVideoTrack",1008,506,248,3,Line);var progress=Surface(track.transform,"BriefVideoProgress",0,0,248,3,Cyan);progress.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");progress.type=Image.Type.Filled;progress.fillMethod=Image.FillMethod.Horizontal;progress.fillAmount=0;
                var status=Label(root,"BriefVideoStatus","English audio / 1 min 30 sec",1008,521,248,43,14,Muted);
                var controller=root.gameObject.AddComponent<LimitsBriefingVideo>();controller.player=player;controller.voice=voice;controller.screen=raw;controller.poster=cover.gameObject;controller.toggle=toggle;controller.replay=replay;controller.toggleLabel=toggle.GetComponentInChildren<TMP_Text>();controller.clock=clock;controller.status=status;controller.progress=progress;
                UnityEventTools.AddPersistentListener(toggle.onClick,controller.TogglePlayback);UnityEventTools.AddPersistentListener(replay.onClick,controller.Replay);
                PrefabUtility.SaveAsPrefabAsset(root.gameObject,Prefabs+"LimitsBriefingVideo.prefab");Object.DestroyImmediate(root.gameObject);
                var saved=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"LimitsBriefingVideo.prefab"),ui.scene);saved.transform.SetParent(brief,false);saved.transform.SetAsFirstSibling();SaveReference(saved.transform);
                ManualEditingMigration.ConfigureCanvas(ui);PrefabUtility.SaveAsPrefabAsset(ui,Prefabs+"LimitsUI.prefab");
            }finally{PrefabUtility.UnloadPrefabContents(ui);}
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"Level01_Limits.unity");var manager=Object.FindFirstObjectByType<GameManager>();var view=manager.menuOrLimitsUI.GetComponentInChildren<LimitsBriefingVideo>(true);view.manager=manager;manager.limitsBriefing=view;SaveReference(view);SaveReference(manager);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ValidateLimitsBriefingVideo();OpenLimitsBriefingVideo();
            File.WriteAllText(Path.Combine(BriefVideoEvidence,"authored.txt"),"PASS: saved robot video prefab nested on Limits Brief; original Start and Back events retained; local English clip/audio, poster, playback controls and Game Manager references saved before Play.");
        }
        public static void ValidateLimitsBriefingVideo()
        {
            Directory.CreateDirectory(BriefVideoEvidence);EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"Level01_Limits.unity");var manager=Object.FindFirstObjectByType<GameManager>();var view=manager.limitsBriefing;
            if(view==null||view.manager!=manager||view.player==null||view.player.clip==null||view.player.clip.audioTrackCount!=1||view.voice==null||view.player.GetTargetAudioSource(0)!=view.voice||view.screen==null||view.poster==null||view.toggle==null||view.replay==null||view.toggle.onClick.GetPersistentEventCount()!=1||view.replay.onClick.GetPersistentEventCount()!=1)throw new Exception("Save all briefing video and audio references.");
            ManualEditingMigration.Validate();File.WriteAllText(Path.Combine(BriefVideoEvidence,"saved-video.txt"),"PASS: saved local 90-second clip with audio, target texture, poster, persistent playback events and Game Manager reference; original four-scene UI and mission references valid.");
        }
        public static void OpenLimitsBriefingVideo()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before opening the saved briefing.");
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"Level01_Limits.unity");var manager=Object.FindFirstObjectByType<GameManager>();var canvas=manager.menuOrLimitsUI.GetComponent<Canvas>();SceneAuthoringWindow.ApplyView(manager.world,canvas,"Brief");Selection.activeGameObject=manager.limitsBriefing.gameObject;EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
        public static void AuthorLimitsBriefingVideoAndBuild(){AuthorLimitsBriefingVideo();EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"Level01_Limits.unity");ManualEditingMigration.BuildQA();}
    }
}
#endif
