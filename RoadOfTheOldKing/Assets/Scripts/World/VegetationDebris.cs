using UnityEngine;
using UnityEngine.Rendering;

namespace RoadOfTheOldKing.World
{
    // A bounded leaf-fleck mesh, reused for every cut. No particle objects or per-cut allocations.
    internal sealed class VegetationDebris
    {
        private const int Capacity=96;
        private readonly GameObject root;
        private readonly Mesh mesh;
        private readonly Material material;
        private readonly MeshRenderer renderer;
        private readonly Vector3[] vertices=new Vector3[Capacity*4];
        private readonly Color32[] colors=new Color32[Capacity*4];
        private readonly int[] indices=new int[Capacity*6];
        private readonly Fleck[] flecks=new Fleck[Capacity];
        private int next;
        private bool active;
        private struct Fleck { public Vector2 position,velocity; public float age,lifetime; public Color32 color; }

        public VegetationDebris(Transform parent)
        {
            root=new GameObject("Vegetation cut flecks") { hideFlags=HideFlags.HideAndDontSave };
            root.transform.SetParent(parent,false);
            mesh=new Mesh { name="Vegetation cut flecks", hideFlags=HideFlags.HideAndDontSave };mesh.MarkDynamic();
            root.AddComponent<MeshFilter>().sharedMesh=mesh;
            renderer=root.AddComponent<MeshRenderer>();
            material=new Material(Shader.Find("RoadOfTheOldKing/EnemyFlash")) { hideFlags=HideFlags.HideAndDontSave, mainTexture=Texture2D.whiteTexture };
            renderer.sharedMaterial=material;
            var properties=new MaterialPropertyBlock();
            properties.SetTexture("_MainTex",Texture2D.whiteTexture);
            renderer.SetPropertyBlock(properties);
            renderer.sortingLayerName="World";renderer.sortingOrder=3;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            var uv=new Vector2[Capacity*4];
            for(int i=0;i<Capacity;i++)
            {
                int v=i*4,j=i*6;
                uv[v]=Vector2.zero;uv[v+1]=Vector2.right;uv[v+2]=Vector2.one;uv[v+3]=Vector2.up;
                indices[j]=v;indices[j+1]=v+1;indices[j+2]=v+2;indices[j+3]=v;indices[j+4]=v+2;indices[j+5]=v+3;
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.colors32=colors;mesh.SetTriangles(indices,0,0,0,false);
            renderer.enabled=false;
        }

        public void Emit(Vector2 position,Vector2 direction,Color color,float seed)
        {
            for(int i=0;i<3;i++)
            {
                float angle=seed*2.31f+i*2.1f;
                flecks[next]=new Fleck { position=position+Vector2.up*.25f,
                    velocity=direction*1.1f+new Vector2(Mathf.Sin(angle)*1.3f,1.3f+Mathf.Cos(angle)*.8f),
                    lifetime=.26f+(Mathf.Sin(angle)+1)*.08f,color=color };
                next=(next+1)%Capacity;
            }
            active=true;
        }

        public void Tick(float dt)
        {
            if(!active)return;
            int count=0;
            for(int i=0;i<Capacity;i++)
            {
                var f=flecks[i];f.age+=dt;
                if(f.age>=f.lifetime){flecks[i]=f;continue;}
                f.velocity.y-=5*dt;f.position+=f.velocity*dt;flecks[i]=f;
                Vector3 p=root.transform.InverseTransformPoint(f.position);
                p.x=Mathf.Round(p.x*16)/16;p.y=Mathf.Round(p.y*16)/16;
                float width=f.age<f.lifetime*.5f?.125f:.0625f;
                int v=count*4;
                vertices[v]=p;vertices[v+1]=p+Vector3.right*width;
                vertices[v+2]=p+new Vector3(width,.0625f);vertices[v+3]=p+Vector3.up*.0625f;
                for(int j=0;j<4;j++)colors[v+j]=f.color;
                count++;
            }
            active=count>0;renderer.enabled=active;
            if(!active)return;
            mesh.vertices=vertices;mesh.colors32=colors;mesh.SetTriangles(indices,0,count*6,0,false);mesh.RecalculateBounds();
        }

        public void Clear(){System.Array.Clear(flecks,0,flecks.Length);active=false;renderer.enabled=false;}
        public void Dispose(){InteractiveVegetation.Dispose(root);InteractiveVegetation.Dispose(mesh);InteractiveVegetation.Dispose(material);}
    }
}
