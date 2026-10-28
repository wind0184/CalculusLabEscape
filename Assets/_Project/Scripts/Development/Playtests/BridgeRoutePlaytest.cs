#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CalculusLabEscape
{
    public sealed partial class MissionsPlaytest
    {
        IEnumerator RouteWalk(Vector3 target)
        {
            float until=Time.realtimeSinceStartup+6;
            while(Time.realtimeSinceStartup<until)
            {
                var delta=target-Game.World.player.transform.position;delta.y=0;if(delta.magnitude<.35f)break;
                Game.World.player.transform.rotation=Quaternion.LookRotation(delta);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.W));yield return null;
            }
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
            var offset=target-Game.World.player.transform.position;offset.y=0;
            Check(offset.magnitude<.5f,"Route waypoint reached with real walking: "+target+" actual "+Game.World.player.transform.position);Check(Game.World.player.transform.position.y>-.15f&&Game.World.player.transform.position.y<.3f,"Player remains on the training ground");
        }
        IEnumerator RoutePush(Vector3 target)
        {
            float until=Time.realtimeSinceStartup+1;
            while(Time.realtimeSinceStartup<until){var delta=target-Game.World.player.transform.position;delta.y=0;Game.World.player.transform.rotation=Quaternion.LookRotation(delta);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.W));yield return null;}
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
        }
        IEnumerator BridgeRouteChecks()
        {
            yield return Click("HomeStart");yield return Click("Mission1");yield return new WaitForSeconds(.4f);yield return Click("BriefStart");yield return Key(UnityEngine.InputSystem.Key.G);
            Check(Game.Topic==1&&Game.State=="Explore","Route audit uses normal saved Bridge scene");yield return Shot("01-spawn-route");
            yield return RouteWalk(new Vector3(-5,0,-1.1f));yield return Shot("02-power-approach");yield return Key(UnityEngine.InputSystem.Key.E);Check(Game.State=="Puzzle","E opens active Power Core from the real front approach");yield return Click("PuzzleWorld");
            yield return RouteWalk(new Vector3(-12,0,-5.5f));yield return RoutePush(new Vector3(-16,0,-5.5f));var p=Game.World.player.transform.position;Check(p.x>=-13.8f&&p.x<-13.4f,"West visible fence aligns with collision");yield return Shot("03-west-limit");
            yield return RouteWalk(new Vector3(-12,0,-7));yield return RouteWalk(new Vector3(-1,0,-7));yield return RoutePush(new Vector3(-1,0,-12));p=Game.World.player.transform.position;Check(p.z>-8.8f&&p.z< -8.3f,"Rear visible fence aligns with collision");yield return Shot("04-rear-limit");
            yield return RouteWalk(new Vector3(-1,0,-6));yield return RouteWalk(new Vector3(4,0,-.3f));yield return Shot("05-converter-approach");yield return Key(UnityEngine.InputSystem.Key.E);Check(Game.State=="Explore","Locked converter cannot open before Power Core completion");
            yield return RouteWalk(new Vector3(1,0,-.3f));yield return RouteWalk(new Vector3(1,0,3.4f));yield return Shot("06-builder-approach");yield return RouteWalk(new Vector3(0,0,5.5f));yield return RoutePush(new Vector3(0,0,9));p=Game.World.player.transform.position;Check(p.z>6&&p.z<6.5f,"Visible closed rescue gate matches river safety collision");
            var gate=Game.World.GetComponentInChildren<BridgeBoundaryGate>();Check(gate!=null&&Quaternion.Angle(gate.left.localRotation,Quaternion.identity)<.1f,"Rescue gate is closed before rescue");yield return Shot("07-river-limit");
            yield return RouteWalk(new Vector3(0,0,-1.8f));yield return RouteWalk(new Vector3(8,0,-1.8f));yield return RouteWalk(new Vector3(12,0,-5.5f));yield return RoutePush(new Vector3(16,0,-5.5f));p=Game.World.player.transform.position;Check(p.x<=13.8f&&p.x>13.4f,"East visible fence aligns with collision");yield return Shot("08-east-limit");
            var colliders=Game.World.GetComponentsInChildren<Collider>(true);Check(colliders.Count(c=>c.name=="Visible trunk collision"&&c.enabled)==14&&!colliders.Any(c=>c.name=="Pine trunk"&&c.enabled),"Only visible imported tree trunks use trunk collision");
            Check(colliders.Any(c=>c is MeshCollider&&c.transform.GetComponentInParent<MeshFilter>()!=null),"Imported rocks have matching mesh collision");
            Check(Game.Stage==0&&Game.Session.attempts.Count==0&&Game.Session.heartsUsed==0&&Game.Session.completed.Count==0,"Route exploration does not submit answers or alter progress");
        }
    }
}
#endif
