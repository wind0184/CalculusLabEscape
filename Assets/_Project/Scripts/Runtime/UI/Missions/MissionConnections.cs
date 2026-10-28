using UnityEngine;
using UnityEngine.UI;
namespace CalculusLabEscape
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MissionConnections:MaskableGraphic
    {
        public bool[] locked=new bool[3];
        [Header("Saved socket references — lines follow the editable buttons")]
        public RectTransform[] sources=new RectTransform[3],targets=new RectTransform[5];
        Vector2 Anchor(RectTransform socket,bool right)
        {
            var rect=socket.rect;
            return rectTransform.InverseTransformPoint(socket.TransformPoint(new Vector3(right?rect.xMax:rect.xMin,rect.center.y)));
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();for(int n=0;n<3;n++)if(locked[n])
            {
                if(n>=sources.Length||n>=targets.Length||sources[n]==null||targets[n]==null)continue;
                var a=Anchor(sources[n],true);var b=Anchor(targets[n],false);var offset=new Vector2(-(b-a).y,(b-a).x).normalized*2;
                int i=vh.currentVertCount;var c=new Color(.35f,.92f,.59f);vh.AddVert(a-offset,c,Vector2.zero);vh.AddVert(a+offset,c,Vector2.zero);vh.AddVert(b+offset,c,Vector2.zero);vh.AddVert(b-offset,c,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
            }
        }
    }
}
