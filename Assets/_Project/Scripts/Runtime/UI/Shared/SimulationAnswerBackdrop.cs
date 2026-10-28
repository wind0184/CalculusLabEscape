using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // Saved UI artwork: works in Edit Mode and uses the standard UI material.
    [AddComponentMenu("Calculus Lab/Simulation answer surface")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SimulationAnswerBackdrop : MaskableGraphic
    {
        public bool panel;
        [Min(0)] public float cornerRadius=18;
        public Color lightTint=new Color(.18f,.65f,.8f);
        public Color rim=new Color(.23f,.42f,.55f,.7f);
        public bool roomDetails,consoleDetails,previewFrame,emblem;
        [Range(0,2)] public int subject;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var bounds=rectTransform.rect;
            if(bounds.width<=0||bounds.height<=0)return;
            if(emblem){Emblem(mesh,bounds);return;}
            if(panel)
            {
                float w=bounds.width,h=bounds.height;
                Rounded(mesh,bounds,new Vector2(0,5),w,h,cornerRadius,new Color(0,0,0,.2f),false);
                Rounded(mesh,bounds,Vector2.zero,w,h,cornerRadius,rim,false);
                Rounded(mesh,bounds,new Vector2(1,1),w-2,h-2,Mathf.Max(0,cornerRadius-1),color,true);
                if(consoleDetails)ConsoleDetails(mesh,bounds);
                return;
            }
            const int columns=40,rows=24;
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
            {
                var a=new Vector2(x/(float)columns,y/(float)rows);var b=a+new Vector2(1f/columns,1f/rows);
                Quad(mesh,bounds,a,b);
            }
            // A quiet floor lattice remains at the edges, beneath the work surfaces.
            for(int i=-10;i<=10;i++)
            {
                var start=new Vector2(.5f+i*.012f,.55f);var end=new Vector2(.5f+i*.18f,1);
                Line(mesh,bounds,start,end,new Color(lightTint.r,lightTint.g,lightTint.b,.12f),.7f);
            }
            foreach(float y in new[]{.59f,.65f,.73f,.84f,.98f})
                Line(mesh,bounds,new Vector2(0,y),new Vector2(1,y),new Color(lightTint.r,lightTint.g,lightTint.b,.10f),.7f);
            // Architectural light seams frame the simulation room, away from text.
            Line(mesh,bounds,new Vector2(.007f,.16f),new Vector2(.007f,.86f),new Color(lightTint.r,lightTint.g,lightTint.b,.35f),1);
            Line(mesh,bounds,new Vector2(.993f,.16f),new Vector2(.993f,.86f),new Color(lightTint.r,lightTint.g,lightTint.b,.35f),1);
            if(roomDetails)
            {
                var ink=new Color(lightTint.r,lightTint.g,lightTint.b,.26f);
                // Data bridge in the gutter connects the scene monitor to the workspace.
                for(int i=0;i<38;i++)Line(mesh,bounds,new Vector2(399f/1280,.24f+i*.014f),new Vector2(399f/1280,.247f+i*.014f),ink,.7f);
                foreach(float y in new[]{.28f,.49f,.71f})
                {
                    var p=new Vector2(399f/1280,y);var dx=new Vector2(4f/1280,0);var dy=new Vector2(0,4f/720);
                    Line(mesh,bounds,p-dx,p-dy,ink,1);Line(mesh,bounds,p-dy,p+dx,ink,1);Line(mesh,bounds,p+dx,p+dy,ink,1);Line(mesh,bounds,p+dy,p-dx,ink,1);
                }
            }
        }
        Color Atmosphere(Vector2 p)
        {
            float glow=Mathf.Exp(-((p.x-.12f)*(p.x-.12f)*6+(p.y-.18f)*(p.y-.18f)*4));
            float blue=Mathf.Exp(-((p.x-.95f)*(p.x-.95f)*9+(p.y-.83f)*(p.y-.83f)*8));
            return new Color(.035f+lightTint.r*glow*.12f+blue*.025f,.075f+lightTint.g*glow*.12f+blue*.05f,.13f+lightTint.b*glow*.13f+blue*.09f,color.a);
        }
        void ConsoleDetails(VertexHelper mesh,Rect r)
        {
            var ink=new Color(lightTint.r,lightTint.g,lightTint.b,.32f);
            void Stroke(float x,float y,float xx,float yy,float strength=1)=>Line(mesh,r,new Vector2(x/r.width,y/r.height),new Vector2(xx/r.width,yy/r.height),new Color(ink.r,ink.g,ink.b,ink.a*strength),.8f);
            if(previewFrame)
            {
                foreach(float x in new[]{16f,343f})foreach(float y in new[]{79f,233f})
                {Stroke(x,y,x+(x<100?10:-10),y,2);Stroke(x,y,x,y+(y<100?10:-10),2);}
                for(int i=0;i<=32;i++)Stroke(18+i*10,238,18+i*10,238+(i%4==0?4:2),.7f);
                return;
            }
            for(int i=0;i<27;i++){float y=201+i*11;Stroke(r.width-8,y,r.width-(i%4==0?14:11),y,.7f);}
            Stroke(8,30,8,52);Stroke(8,30,11,30);
            Stroke(r.width-8,r.height-31,r.width-8,r.height-52);Stroke(r.width-8,r.height-31,r.width-11,r.height-31);
            if(subject!=0)Stroke(17,r.height-67,r.width-17,r.height-67,.45f);
            for(int i=0;i<7;i++)Stroke(r.width-72+i*7,r.height-9,r.width-68+i*7,r.height-9,i<3?1:.3f);
        }
        void Emblem(VertexHelper mesh,Rect r)
        {
            var ink=new Color(lightTint.r,lightTint.g,lightTint.b,.8f);
            void Edge(float x,float y,float xx,float yy)=>Line(mesh,r,new Vector2(x/40,y/32),new Vector2(xx/40,yy/32),ink,1.15f);
            if(subject==1)
            {
                Edge(8,7,8,28);Edge(32,7,32,28);Edge(1,24,39,24);
                var points=new[]{new Vector2(0,9),new Vector2(8,14),new Vector2(14,18),new Vector2(20,19),new Vector2(26,18),new Vector2(32,14),new Vector2(40,9)};
                for(int i=0;i<points.Length-1;i++)Edge(points[i].x,points[i].y,points[i+1].x,points[i+1].y);
                foreach(var p in points)if(p.x>8&&p.x<32)Edge(p.x,p.y,p.x,24);
            }
            else if(subject==2)
            {
                for(int i=0;i<6;i++){float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;Edge(20+Mathf.Cos(a)*10,16+Mathf.Sin(a)*10,20+Mathf.Cos(b)*10,16+Mathf.Sin(b)*10);}
                Edge(17,13,23,13);Edge(23,13,23,19);Edge(23,19,17,19);Edge(17,19,17,13);
                foreach(float y in new[]{9f,23f}){Edge(2,y,10,y);Edge(30,y,38,y);}
            }
            else
            {
                for(int ring=0;ring<2;ring++)for(int i=0;i<32;i++)
                {float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;Edge(20+Mathf.Cos(a)*(8+ring*3),16+Mathf.Sin(a)*(11+ring*3),20+Mathf.Cos(b)*(8+ring*3),16+Mathf.Sin(b)*(11+ring*3));}
                Edge(4,27,10,27);Edge(30,27,36,27);
            }
        }
        static Vector2 Point(Rect r,Vector2 p)=>new Vector2(r.xMin+p.x*r.width,r.yMax-p.y*r.height);
        static void Vertex(VertexHelper mesh,Vector2 p,Color c){var v=UIVertex.simpleVert;v.position=p;v.color=c;mesh.AddVert(v);}
        void Quad(VertexHelper mesh,Rect r,Vector2 a,Vector2 b)
        {
            int n=mesh.currentVertCount;var tr=new Vector2(b.x,a.y);var bl=new Vector2(a.x,b.y);
            Vertex(mesh,Point(r,a),Atmosphere(a));Vertex(mesh,Point(r,tr),Atmosphere(tr));Vertex(mesh,Point(r,b),Atmosphere(b));Vertex(mesh,Point(r,bl),Atmosphere(bl));
            mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
        static void Line(VertexHelper mesh,Rect r,Vector2 a,Vector2 b,Color c,float width)
        {
            a=Point(r,a);b=Point(r,b);var normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int n=mesh.currentVertCount;
            Vertex(mesh,a-normal,c);Vertex(mesh,a+normal,c);Vertex(mesh,b+normal,c);Vertex(mesh,b-normal,c);
            mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
        static void Rounded(VertexHelper mesh,Rect r,Vector2 offset,float w,float h,float radius,Color fill,bool gradient)
        {
            radius=Mathf.Min(radius,Mathf.Min(w,h)*.5f);var points=new List<Vector2>(28);
            var centers=new[]{new Vector2(radius,radius),new Vector2(w-radius,radius),new Vector2(w-radius,h-radius),new Vector2(radius,h-radius)};
            for(int corner=0;corner<4;corner++)for(int step=0;step<=6;step++)
            {
                float angle=(180+corner*90+step*15)*Mathf.Deg2Rad;
                points.Add(centers[corner]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);
            }
            int first=mesh.currentVertCount;
            Vector2 Local(Vector2 p)=>new Vector2(r.xMin+offset.x+p.x,r.yMax-offset.y-p.y);
            Vertex(mesh,Local(new Vector2(w/2,h/2)),fill);
            foreach(var p in points){var c=fill;if(gradient){float t=1-p.y/h;c.r+=.015f*t;c.g+=.022f*t;c.b+=.03f*t;}Vertex(mesh,Local(p),c);}
            for(int i=0;i<points.Count;i++)mesh.AddTriangle(first,first+1+i,first+1+(i+1)%points.Count);
        }
    }
}
