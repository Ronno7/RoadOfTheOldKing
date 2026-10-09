using UnityEngine;
using UnityEngine.Rendering;

namespace RoadOfTheOldKing.World
{
    // Two bounded pools share a mesh and atlas. Smooth airborne motion is an intentional
    // presentation exception; no particle GameObjects, physics bodies or per-cut allocations.
    internal sealed class VegetationDebris
    {
        private const int BladeCapacity=64, MoteCapacity=128, Capacity=BladeCapacity+MoteCapacity;
        private readonly GameObject root;
        private readonly Mesh mesh;
        private readonly Material backMaterial, frontMaterial;
        private readonly MeshRenderer backRenderer, frontRenderer;
        private readonly Vector3[] vertices=new Vector3[Capacity*4];
        private readonly Vector2[] uv=new Vector2[Capacity*4];
        private readonly Vector4[] metadata=new Vector4[Capacity*4];
        private readonly Color32[] colors=new Color32[Capacity*4];
        private readonly int[] indices=new int[Capacity*6];
        private readonly Particle[] particles=new Particle[Capacity];
        private readonly LeafShape[] leaves=new LeafShape[2];
        private int nextBlade, nextMote, leafCount;
        private bool active;
        private int activeCount;
        public int ActiveCount => activeCount;
        private static readonly int WindowId=Shader.PropertyToID("_PlantDepthWindow");
        private struct LeafShape { public Vector2 size; public Rect uv; }
        private struct Particle
        {
            public Vector2 position,velocity,size;
            public float height,lift,age,lifetime,angle,spin,phase;
            public Color32 color;
            public int leaf;
            public bool bounced;
        }

        public VegetationDebris(Transform parent,VegetationLayout layout,Shader shader)
        {
            Texture atlas=Texture2D.whiteTexture;
            // Reuse two accepted grass leaves; flowers/reeds also shed these leafy fragments.
            foreach(var species in layout.species)
            {
                if(species==null||species.leaves==null)continue;
                foreach(var leaf in species.leaves)
                {
                    if(leaf==null||leaf.sprite==null)continue;
                    var sprite=leaf.sprite;
                    if(leafCount==0)atlas=sprite.texture;
                    if(sprite.texture!=atlas)continue;
                    var rect=sprite.textureRect;
                    leaves[leafCount++]=new LeafShape { size=sprite.rect.size/sprite.pixelsPerUnit,
                        uv=new Rect(rect.x/atlas.width,rect.y/atlas.height,rect.width/atlas.width,rect.height/atlas.height) };
                    if(leafCount==leaves.Length)break;
                }
                if(leafCount==leaves.Length)break;
            }
            root=new GameObject("Vegetation cut debris") { hideFlags=HideFlags.HideAndDontSave };
            root.transform.SetParent(parent,false);
            mesh=new Mesh { name="Vegetation cut debris", hideFlags=HideFlags.HideAndDontSave };mesh.MarkDynamic();
            backMaterial=MakeMaterial(shader,atlas,-1);
            frontMaterial=MakeMaterial(shader,atlas,1);
            backRenderer=MakeRenderer(root,backMaterial);
            var front=new GameObject("Vegetation cut debris front") { hideFlags=HideFlags.HideAndDontSave };
            front.transform.SetParent(root.transform,false);
            frontRenderer=MakeRenderer(front,frontMaterial);
            for(int i=0;i<Capacity;i++)
            {
                int v=i*4,j=i*6;
                indices[j]=v;indices[j+1]=v+1;indices[j+2]=v+2;
                indices[j+3]=v;indices[j+4]=v+2;indices[j+5]=v+3;
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.colors32=colors;mesh.SetUVs(1,metadata);
            mesh.SetTriangles(indices,0,0,0,false);
            SetSorting(SortingLayer.NameToID("Player"),0,float.NegativeInfinity);
        }

        private static Material MakeMaterial(Shader shader,Texture atlas,float side)
        {
            var material=new Material(shader) { name="Vegetation cut debris",hideFlags=HideFlags.HideAndDontSave,mainTexture=atlas };
            material.SetFloat("_CutDebris",1);material.SetFloat("_PlantDepthSide",side);
            return material;
        }

        private MeshRenderer MakeRenderer(GameObject owner,Material material)
        {
            owner.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=owner.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;renderer.enabled=false;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            return renderer;
        }

        public void SetSorting(int layer,int bodyOrder,float feet)
        {
            backRenderer.sortingLayerID=frontRenderer.sortingLayerID=layer;
            backRenderer.sortingOrder=bodyOrder-2;frontRenderer.sortingOrder=bodyOrder+4;
            var window=new Vector4(0,0,0,feet);
            backMaterial.SetVector(WindowId,window);frontMaterial.SetVector(WindowId,window);
        }

        private static float Random01(ref uint state)
        {
            state^=state<<13;state^=state>>17;state^=state<<5;
            return (state&0xffffffu)/16777216f;
        }

        public void Emit(Vector2 position,Vector2 direction,Color color,float seed)
        {
            uint random=unchecked((uint)(seed*100000f)+0x9e3779b9u);
            direction=direction.sqrMagnitude>.0001f?direction.normalized:Vector2.up;
            for(int i=0;i<7;i++)
            {
                bool blade=i<2;
                if(blade&&leafCount==0)continue;
                float angle=Random01(ref random)*Mathf.PI*2;
                var scatter=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                float scale=blade?Mathf.Lerp(.4f,.65f,Random01(ref random)):Mathf.Lerp(.07f,.14f,Random01(ref random));
                int leaf=blade?i%leafCount:-1;
                int slot=blade?nextBlade:BladeCapacity+nextMote;
                particles[slot]=new Particle {
                    position=position+scatter*.09f,velocity=direction*(blade?1.2f:1.8f)+scatter*(blade?.9f:1.4f),
                    height=blade?.3f:.2f,lift=Mathf.Lerp(blade?2.6f:1.5f,blade?3.8f:2.8f,Random01(ref random)),
                    size=blade?Vector2.Scale(leaves[leaf].size,new Vector2(1.65f,1))*scale:Vector2.one*scale,
                    lifetime=Mathf.Lerp(blade?.8f:.45f,blade?1.15f:.75f,Random01(ref random)),
                    angle=angle,spin=(Random01(ref random)>.5f?1:-1)*Mathf.Lerp(5,11,Random01(ref random)),
                    phase=Random01(ref random)*Mathf.PI*2,
                    color=blade?Color.white:Color.Lerp(color,new Color(.69f,.85f,.42f,1),.55f+Random01(ref random)*.45f),
                    leaf=leaf
                };
                if(blade)nextBlade=(nextBlade+1)%BladeCapacity;else nextMote=(nextMote+1)%MoteCapacity;
            }
            active=true;
        }

        public void Tick(float dt)
        {
            if(!active||dt<=0)return;
            int count=0;
            var min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,0);
            var max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,0);
            for(int i=0;i<Capacity;i++)
            {
                var p=particles[i];p.age+=dt;
                if(p.age>=p.lifetime){particles[i]=p;continue;}
                bool blade=p.leaf>=0;
                p.velocity*=Mathf.Exp(-(blade?1.7f:3.4f)*dt);
                p.position+=p.velocity*dt;
                p.lift-=8.5f*dt;p.height+=p.lift*dt;
                if(p.height<0)
                {
                    p.height=0;
                    if(blade&&!p.bounced){p.lift=-p.lift*.22f;p.bounced=true;p.velocity*=.5f;p.spin*=.5f;}
                    else p.lift=0;
                }
                p.angle+=p.spin*dt;particles[i]=p;
                // Ground position controls depth; elevation only lifts the visible piece.
                Vector3 center=root.transform.InverseTransformPoint(p.position+Vector2.up*p.height);
                float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(blade?.55f:.18f,1,p.age/p.lifetime));
                float flutter=blade?.9f+.1f*Mathf.Sin(p.age*15+p.phase):1;
                float shrink=blade?1:Mathf.Lerp(1,.25f,p.age/p.lifetime);
                float sine=Mathf.Sin(p.angle),cosine=Mathf.Cos(p.angle);
                var right=new Vector3(cosine,sine,0)*(p.size.x*.5f*flutter*shrink);
                var up=new Vector3(-sine,cosine,0)*(p.size.y*.5f*shrink);
                int v=count*4;
                vertices[v]=center-right-up;vertices[v+1]=center+right-up;
                vertices[v+2]=center+right+up;vertices[v+3]=center-right+up;
                var rect=blade?leaves[p.leaf].uv:new Rect(0,0,1,1);
                uv[v]=new Vector2(rect.xMin,rect.yMin);uv[v+1]=new Vector2(rect.xMax,rect.yMin);
                uv[v+2]=new Vector2(rect.xMax,rect.yMax);uv[v+3]=new Vector2(rect.xMin,rect.yMax);
                var color=p.color;color.a=(byte)Mathf.RoundToInt(255*fade);
                var data=new Vector4(p.position.x,p.position.y,blade?0:1,0);
                for(int j=0;j<4;j++)
                {
                    colors[v+j]=color;metadata[v+j]=data;
                    min=Vector3.Min(min,vertices[v+j]);max=Vector3.Max(max,vertices[v+j]);
                }
                count++;
            }
            activeCount=count;active=count>0;
            backRenderer.enabled=frontRenderer.enabled=active;
            mesh.SetTriangles(indices,0,count*6,0,false);
            if(!active)return;
            mesh.vertices=vertices;mesh.uv=uv;mesh.colors32=colors;mesh.SetUVs(1,metadata);
            mesh.bounds=new Bounds((min+max)*.5f,max-min+Vector3.forward*.1f);
        }

        public void Clear()
        {
            System.Array.Clear(particles,0,particles.Length);active=false;activeCount=0;nextBlade=nextMote=0;
            backRenderer.enabled=frontRenderer.enabled=false;mesh.SetTriangles(indices,0,0,0,false);
        }
        public void Dispose()
        {
            backRenderer.enabled=frontRenderer.enabled=false;
            InteractiveVegetation.Dispose(root);InteractiveVegetation.Dispose(mesh);
            InteractiveVegetation.Dispose(backMaterial);InteractiveVegetation.Dispose(frontMaterial);
        }
    }
}
