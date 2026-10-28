using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CalculusLabEscape
{
    // A saved local lesson on Brief only. Start still owns assessment time and progress.
    public class MissionBriefingVideo : MonoBehaviour
    {
        public GameManager manager;
        public VideoPlayer player;
        public AudioSource voice;
        public RawImage screen;
        public GameObject poster;
        public Button toggle,replay;
        public TMP_Text toggleLabel,status,clock;
        public Image progress;
        public bool Finished { get; private set; }
        public bool Failed { get; private set; }
        public bool HasFrame { get; private set; }
        RenderTexture liveTexture;
        bool ready,leaving;
        public bool IsReady => ready;

        protected virtual void Awake()
        {
            liveTexture=new RenderTexture(player.targetTexture){name="Mission briefing live video"};
            liveTexture.Create();player.targetTexture=liveTexture;screen.texture=liveTexture;
            player.prepareCompleted+=Prepared;player.frameReady+=FirstFrame;player.loopPointReached+=Ended;player.errorReceived+=Error;
        }
        protected virtual void OnEnable()
        {
            if(!Application.isPlaying)return;
            ready=false;leaving=false;voice.mute=false;Finished=false;Failed=false;HasFrame=false;poster.SetActive(true);
            toggle.interactable=replay.interactable=false;progress.fillAmount=0;clock.text="00:00 / 01:30";status.text="Preparing video...";toggleLabel.text="Play video";
            StartCoroutine(PrepareWhenVisible());
        }
        IEnumerator PrepareWhenVisible()
        {
            // Let saved-session restoration select its page, and wait until the scene reveal finishes.
            yield return null;
            while(ScreenTransition.IsBusy)yield return null;
            if(leaving||manager==null||manager.State!="Brief")yield break;
            player.sendFrameReadyEvents=true;player.Prepare();
            double deadline=Time.realtimeSinceStartupAsDouble+15;
            while(!ready&&!Failed&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            if(!ready&&!Failed)Unavailable();
        }
        void Prepared(VideoPlayer source)
        {
            if(leaving||!isActiveAndEnabled||manager==null||manager.State!="Brief"||Failed)return;
            ready=true;toggle.interactable=replay.interactable=true;source.Play();RefreshControls();
        }
        void FirstFrame(VideoPlayer source,long frame)
        {
            if(leaving||!isActiveAndEnabled||manager==null||manager.State!="Brief"||Failed)return;
            HasFrame=true;poster.SetActive(false);source.sendFrameReadyEvents=false;
        }
        void Ended(VideoPlayer source)
        {
            if(leaving||!isActiveAndEnabled||manager==null||manager.State!="Brief")return;
            Finished=true;source.Pause();voice.Pause();RefreshControls();
        }
        void Error(VideoPlayer source,string message)
        {
            if(leaving||!isActiveAndEnabled||manager==null||manager.State!="Brief")return;
            Debug.LogWarning("Mission introduction could not play: "+message);Unavailable();
        }
        void Unavailable()
        {
            Failed=true;ready=false;player.Stop();voice.Stop();poster.SetActive(true);
            toggle.interactable=false;replay.interactable=true;status.text="Video unavailable. You can still start.";toggleLabel.text="Play video";
        }
        public void TogglePlayback()
        {
            if(leaving||!ready||Failed||manager==null||manager.State!="Brief")return;
            if(Finished){Replay();return;}
            if(player.isPlaying)player.Pause();else player.Play();RefreshControls();
        }
        public void Replay()
        {
            if(leaving||manager==null||manager.State!="Brief")return;
            if(Failed){StopAllCoroutines();OnEnable();return;}
            if(!ready)return;
            Finished=false;player.time=0;player.Play();RefreshControls();
        }
        void RefreshControls()
        {
            toggleLabel.text=Finished?"Play again":player.isPlaying?"Pause video":"Play video";
            status.text=Finished?"Ready when you are. Start the mission.":player.isPlaying?"Playing / English audio":"Paused";
        }
        protected virtual void LateUpdate()
        {
            if(manager!=null&&manager.State!="Brief"){if(player.isPlaying||player.isPrepared||ready)LeaveBriefing();return;}
            if(!ready||Failed)return;
            double duration=player.length>0?player.length:90,time=Finished?duration:System.Math.Max(0,player.time);
            progress.fillAmount=(float)(time/duration);clock.text=TimeLabel(time)+" / "+TimeLabel(duration);
        }
        static string TimeLabel(double seconds)
        {
            int total=Mathf.FloorToInt((float)seconds+.01f);return(total/60).ToString("00")+":"+(total%60).ToString("00");
        }
        public void LeaveBriefing()
        {
            // Native video stop can complete after this frame. Mute immediately and
            // reject queued prepare/frame callbacks throughout the closing transition.
            leaving=true;ready=false;StopAllCoroutines();
            if(voice!=null){voice.mute=true;voice.Stop();}
            if(player!=null){player.sendFrameReadyEvents=false;player.Stop();}
        }
        protected virtual void OnDisable(){if(Application.isPlaying)LeaveBriefing();}
        protected virtual void OnDestroy()
        {
            if(player!=null){player.prepareCompleted-=Prepared;player.frameReady-=FirstFrame;player.loopPointReached-=Ended;player.errorReceived-=Error;}
            if(liveTexture!=null){liveTexture.Release();Destroy(liveTexture);}
        }
    }
}
