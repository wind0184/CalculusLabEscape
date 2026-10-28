#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CalculusLabEscape
{
    public sealed partial class MissionsPlaytest
    {
        RepairStationMotion Machinery=>Game!=null&&Game.Topic==1?Game.World.GetComponentInChildren<RepairStationMotion>(true):null;
        string MachineryPose()
        {
            var motion=Machinery;if(motion==null)return "";
            return motion.generatorRotor.localRotation.ToString("F5")+motion.winch.localRotation.ToString("F5")+string.Join("/",motion.channelFlows.Select(t=>t.localPosition.ToString("F5")));
        }
        IEnumerator MachineryCheck(string context,bool? expectedWinch=null)
        {
            var motion=Machinery;Check(motion!=null,"Saved machinery motion loaded: "+context);if(motion==null)yield break;
            if(Game.Stage==0&&!Game.CanvasSupportOpen)
            {
                motion.Sample(11,.2f,true);yield return new WaitForEndOfFrame();
                Check(Quaternion.Angle(motion.generatorRotor.localRotation,motion.generatorRest)<.01f&&Quaternion.Angle(motion.winch.localRotation,motion.winchRest)<.01f&&motion.channelFlows.All(t=>!t.GetComponent<Renderer>().enabled),"Play restores actual mission poses after a decorative preview");
            }
            int stage=Game.Stage,attempts=Game.Session.attempts.Count,hearts=Game.Session.heartsUsed,completed=Game.Session.completed.Count;
            bool advancing=!Game.CanvasSupportOpen&&(Game.State=="Explore"||Game.State=="Puzzle"||Game.State=="Finishing");
            bool running=stage>=1&&advancing,installing=(expectedWinch??(stage>=4&&Game.World.BridgeInstallationMoving))&&advancing;
            var rotor=motion.generatorRotor.localRotation;var winch=motion.winch.localRotation;var points=motion.channelFlows.Select(t=>t.localPosition).ToArray();
            yield return new WaitForSeconds(.16f);
            File.AppendAllText(Path.Combine(folder,"machinery-checks.txt"),context+" / stage="+stage+" / state="+Game.State+" / support="+Game.CanvasSupportOpen+" / rotorDelta="+Quaternion.Angle(rotor,motion.generatorRotor.localRotation)+" / winchDelta="+Quaternion.Angle(winch,motion.winch.localRotation)+" / flowing="+string.Join(",",motion.channelFlows.Select((t,i)=>Vector3.Distance(points[i],t.localPosition)>.001f))+"\n");
            Check((Quaternion.Angle(rotor,motion.generatorRotor.localRotation)>.1f)==running,"Generator follows installed progress: "+context);
            Check((Quaternion.Angle(winch,motion.winch.localRotation)>.1f)==installing,"Winch follows actual bridge lift, stays still for unrelated answers: "+context);
            for(int i=0;i<3;i++)
            {
                bool active=stage>=i+2;var renderer=motion.channelFlows[i].GetComponent<Renderer>();
                Check(renderer.enabled==active,"Only completed converter channels carry flow: "+i+" / "+context);
                Check((Vector3.Distance(points[i],motion.channelFlows[i].localPosition)>.001f)==(active&&advancing),"Converter flow follows channel state and help pause: "+i+" / "+context);
                if(active)Check(renderer.sharedMaterial==motion.channelSources[i].sharedMaterial,"Channel colour follows original lock state: "+context);
            }
            Check(Game.Stage==stage&&Game.Session.attempts.Count==attempts&&Game.Session.heartsUsed==hearts&&Game.Session.completed.Count==completed,"Machinery animation leaves assessment unchanged: "+context);
        }
        IEnumerator MachineryPauseCheck()
        {
            yield return Key(UnityEngine.InputSystem.Key.Escape);Check(Game.State=="Paused","Machinery pause uses normal pause page");
            string pose=MachineryPose();yield return new WaitForSeconds(.2f);Check(pose==MachineryPose(),"Pause freezes rotor, winch and channel flow");
            yield return Click("PauseResume");yield return MachineryCheck("resumed after pause",false);
        }
    }
}
#endif
