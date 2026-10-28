using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CalculusLabEscape
{
    public sealed partial class Chapter4World
    {
        [SerializeField] Material timber,edgeMetal,instrumentFace,riverMaterial,pineDark,pineLight,pathStone,artAccent;
        readonly List<Mesh> artMeshes=new List<Mesh>();
        [SerializeField] List<Renderer> bridgeMarkers=new List<Renderer>();

        void ConfigureMissionArt(Light key)
        {
            // Explicit ambient mode prevents the scene's default sky lighting washing out the palette.
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.reflectionIntensity=.18f;
            RenderSettings.fogMode=FogMode.Exponential;
            key.shadows=LightShadows.Soft;key.shadowStrength=.7f;key.shadowBias=.035f;
            key.color=topic==1?new Color(1,.92f,.76f):new Color(.72f,.85f,1);
            key.intensity=topic==1?.95f:.8f;
            RenderSettings.ambientLight=topic==1?new Color(.31f,.38f,.32f):new Color(.22f,.29f,.36f);
            RenderSettings.fogColor=topic==1?new Color(.34f,.48f,.48f):new Color(.045f,.072f,.10f);
            RenderSettings.fogDensity=topic==1?.012f:.004f;
            dark.color=topic==1?new Color(.10f,.14f,.14f):new Color(.035f,.055f,.08f);
            cyan.color=topic==1?new Color(.19f,.28f,.27f):new Color(.20f,.28f,.34f);
            stone.color=topic==1?new Color(.35f,.40f,.32f):new Color(.09f,.13f,.18f);
            leaf.color=new Color(.20f,.31f,.18f);amber.color=new Color(.29f,.32f,.31f);
            timber=Mat("Warm bridge timber",new Color(.39f,.24f,.12f));
            edgeMetal=Mat("Brushed steel edges",new Color(.28f,.37f,.42f));edgeMetal.SetFloat("_Metallic",.35f);edgeMetal.SetFloat("_Glossiness",.3f);
            instrumentFace=Mat("Readable dial face",new Color(.59f,.69f,.68f));
            riverMaterial=Mat("River teal",new Color(.055f,.31f,.37f));riverMaterial.SetFloat("_Glossiness",.45f);
            pineDark=Mat("Pine deep green",new Color(.075f,.22f,.14f));pineLight=Mat("Pine green tips",new Color(.15f,.34f,.19f));
            pathStone=Mat("Trail stone",new Color(.43f,.43f,.32f));
            artAccent=Mat("Wayfinding cyan",new Color(.09f,.47f,.54f),true);
        }

        GameObject Cone(string name,Vector3 position,float radius,float height,Material material)
        {
            const int sides=7;var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 p=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),q=new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
                int n=vertices.Count;vertices.Add(p);vertices.Add(Vector3.up*height);vertices.Add(q);vertices.Add(q);vertices.Add(Vector3.zero);vertices.Add(p);
                for(int j=0;j<6;j++)triangles.Add(n+j);
            }
            var mesh=new Mesh{name=name+" mesh"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();artMeshes.Add(mesh);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform);go.transform.position=position;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        void MakePine(Vector3 position,float size,int index)
        {
            Shape("Pine trunk",position+Vector3.up*1.4f*size,new Vector3(.30f,1.4f,.30f)*size,timber,PrimitiveType.Cylinder);
            for(int layer=0;layer<3;layer++)Cone("Pine canopy "+index+"-"+layer,position+Vector3.up*(1.35f+layer*.95f)*size,(1.6f-layer*.3f)*size,2.15f*size,layer==2?pineLight:pineDark);
        }
        void Trail(Vector3 start,Vector3 end)
        {
            int count=Mathf.CeilToInt(Vector3.Distance(start,end)/.85f);
            for(int i=0;i<=count;i++)
            {
                var p=Vector3.Lerp(start,end,(float)i/count);p.y=.025f;
                var slab=Shape("Walkway stone",p,new Vector3(.66f,.045f,.52f),pathStone,solid:false);slab.transform.rotation=Quaternion.Euler(0,(i%3-1)*13,0);
            }
        }
        void DressForest()
        {
            Trail(new Vector3(0,0,-5),new Vector3(-5,0,-.3f));Trail(new Vector3(-3.8f,0,-.7f),new Vector3(4,0,.8f));Trail(new Vector3(2.6f,0,.8f),new Vector3(1.6f,0,4.5f));
            for(int side=-1;side<=1;side+=2)for(int i=0;i<10;i++)
            {
                var p=new Vector3(side*(5.8f+i%3*.9f),.10f,-5+i*2.1f);
                var rock=Shape("Bank rock",p,new Vector3(.7f+i%2*.3f,.36f,.65f),pathStone,solid:false);rock.transform.rotation=Quaternion.Euler(7,i*37,12);
                for(int j=0;j<3;j++)Shape("Grass tuft",p+new Vector3(.65f+j*.10f,.10f,.35f),new Vector3(.065f,.35f+j*.04f,.06f),pineLight,solid:false).transform.rotation=Quaternion.Euler(0,0,(j-1)*17);
            }
            var foam=Mat("River ripples",new Color(.23f,.54f,.55f));
            for(int i=0;i<28;i++)
            {
                var ripple=Shape("River ripple",new Vector3(-12.8f+i*.95f,water.position.y+.106f,7.7f+(i%4)*1.1f),new Vector3(.45f+(i%3)*.25f,.01f,.035f),foam,solid:false);ripple.transform.SetParent(water,true);
            }
            for(int i=0;i<bridge.Count;i++)
            {
                bridge[i].GetComponent<Renderer>().sharedMaterial=timber;
                var marker=Shape("Bridge installed marker "+i,bridge[i].position+new Vector3(1.25f,.13f,0),new Vector3(.16f,.025f,.38f),repairComplete,solid:false).GetComponent<Renderer>();marker.transform.SetParent(bridge[i],true);marker.enabled=false;bridgeMarkers.Add(marker);
                for(int side=-1;side<=1;side+=2)
                {var bolt=Shape("Bridge fixing",bridge[i].position+new Vector3(side*1.1f,.125f,0),new Vector3(.06f,.025f,.06f),edgeMetal,PrimitiveType.Cylinder,false);bolt.transform.SetParent(bridge[i],true);}
            }
            for(int i=0;i<devices.Count;i++)
            {
                var p=devices[i].position;
                Shape("Repair station foot",new Vector3(p.x,.075f,p.z),new Vector3(2.15f,.15f,1.65f),edgeMetal,solid:false);
                for(int side=-1;side<=1;side+=2)Shape("Repair station frame",p+new Vector3(side*.92f,0,-.69f),new Vector3(.12f,1.32f,.12f),edgeMetal,solid:false);
                for(int slot=0;slot<4;slot++)Shape("Station front vent",p+new Vector3((slot-1.5f)*.25f,-.23f,-.758f),new Vector3(.13f,.43f,.025f),cyan,solid:false);
                var plaque=Shape("Device sign backing",repairSigns[i].transform.position+Vector3.forward*.035f,new Vector3(2.9f,.59f,.045f),dark,solid:false);plaque.transform.SetParent(repairSigns[i].transform,true);
                repairSigns[i].color=new Color(.82f,.95f,.92f);
            }
            for(int i=0;i<3;i++)Shape("Converter guide rail",devices[1].position+new Vector3((i-1)*.52f,.97f,.13f),new Vector3(.10f,.48f,.10f),edgeMetal,solid:false);
            Shape("Builder assembly rail",devices[2].position+new Vector3(0,.87f,.30f),new Vector3(1.65f,.12f,.13f),edgeMetal,solid:false);
            for(int i=0;i<3;i++)Shape("Builder timber stock",devices[2].position+new Vector3((i-1)*.43f,.86f,-.14f),new Vector3(.28f,.13f,.54f),timber,solid:false);
        }
        void DressLaboratory()
        {
            var lightPanel=Mat("Ceiling diffuser",new Color(.38f,.64f,.72f),true);
            Shape("Case identification plate",new Vector3(0,1.06f,.085f),new Vector3(2.65f,.42f,.025f),dark,solid:false);
            Shape("Lab room identification plate",new Vector3(0,6,20.04f),new Vector3(7,.85f,.08f),dark,solid:false);
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Lab guide strip",new Vector3(side*2.8f,.026f,5),new Vector3(.035f,.018f,21),artAccent,solid:false);
                for(int z=0;z<=16;z+=4)
                {
                    Shape("Wall light strip",new Vector3(side*10.7f,2.4f,z),new Vector3(.035f,.1f,2.2f),lightPanel,solid:false);
                    Shape("Ceiling service beam",new Vector3(side*5,7,z),new Vector3(4,.18f,.32f),dark,solid:false);
                    Shape("Ceiling task light",new Vector3(side*5,6.88f,z),new Vector3(2.4f,.04f,.23f),lightPanel,solid:false);
                }
                var fill=new GameObject("Case fill light").AddComponent<Light>();fill.transform.SetParent(transform);fill.transform.position=new Vector3(side*3.4f,4.5f,-.5f);fill.type=LightType.Point;fill.color=new Color(.5f,.8f,.92f);fill.intensity=.85f;fill.range=8;fill.shadows=LightShadows.None;
            }
            for(int i=0;i<4;i++)
            {
                float x=(i-1.5f)*1.1f;
                for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)Shape("Module screw",new Vector3(x+side*.40f,1.802f,1.13f+end*.60f),new Vector3(.055f,.016f,.055f),edgeMetal,PrimitiveType.Cylinder,false);
            }
            var lidText=Sign("INTEGRATION / FIELD GUIDE\n\nRAISE THE POWER  >  DIVIDE  >  ADD C\n\nComplete every module.\nThen lift the cover and deactivate.",new Vector3(0,1.513f,1.03f),.024f);lidText.alignment=TextAlignment.Center;lidText.color=new Color(.74f,.91f,.91f);lidText.transform.rotation=Quaternion.Euler(-90,0,0);lidText.transform.SetParent(caseLid,true);
            foreach(float x in new[]{-2.25f,2.25f})
            {
                var trim=Shape("Lid edge trim",new Vector3(x,1.67f,1.03f),new Vector3(.09f,.17f,2.3f),edgeMetal,solid:false);trim.transform.SetParent(caseLid,true);
                Shape("Case corner guard",new Vector3(x,1.5f,-.13f),new Vector3(.21f,.32f,.24f),edgeMetal,solid:false);
            }
        }
        void DressCow(Transform root,Vector3 position,float scale)
        {
            var patch=Mat(root.name+" brown patches",new Color(.22f,.13f,.065f));
            for(int side=-1;side<=1;side+=2)
            {
                var spot=Shape(root.name+" patch",position+new Vector3(side*.47f,1.12f,.15f)*scale,new Vector3(.17f,.46f,.7f)*scale,patch,PrimitiveType.Sphere,false);spot.transform.SetParent(root,true);
            }
            for(int i=0;i<4;i++)
            {
                var hoof=Shape(root.name+" hoof",position+new Vector3(i%2==0?-.35f:.35f,.09f,i<2?-.5f:.5f)*scale,new Vector3(.23f,.18f,.24f)*scale,patch,solid:false);hoof.transform.SetParent(root,true);
            }
        }
    }
}
