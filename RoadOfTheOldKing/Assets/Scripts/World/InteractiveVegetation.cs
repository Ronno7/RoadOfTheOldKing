using System.Collections.Generic;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.Weapons;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoadOfTheOldKing.World
{
    // One field, no per-plant objects/colliders/Update. Shader wind is independent of field size.
    // Meshes are spatially culled; only touched chunks upload indices after an actual cut.
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(220)]
    public sealed class InteractiveVegetation : MonoBehaviour, IResetOnRest
    {
        public VegetationLayout layout;
        public Shader vegetationShader;
        public Transform interactor;
        private const int ChunkSize = 8, TouchCount = 8;
        private readonly Dictionary<Vector2Int, List<Chunk>> cells = new Dictionary<Vector2Int, List<Chunk>>();
        private readonly List<Chunk> chunks = new List<Chunk>();
        private readonly Dictionary<Texture, Material> materials = new Dictionary<Texture, Material>();
        private readonly Dictionary<Texture, Material> frontMaterials = new Dictionary<Texture, Material>();
        private readonly HashSet<Chunk> foreground = new HashSet<Chunk>();
        private readonly Vector4[] touches = new Vector4[TouchCount];
        private readonly float[] ages = new float[TouchCount];
        private readonly HashSet<Chunk> dirty = new HashSet<Chunk>();
        private bool[] cut;
        private VegetationLayout builtLayout;
        private Vector2 previousFeet;
        private bool hasFeet;
        private float clock, trailClock;
        private int nextTouch = 1;
        private VegetationDebris debris;
        private Transform depthInteractor;
        private SpriteRenderer depthBody;
        private Vector4 depthWindow = new Vector4(1,1,0,0);
        private Vector2Int depthMin, depthMax;
        private bool hasDepthCells;
        private float leafReach, rootOffset;
        private int depthLayer, depthOrder;
        private static readonly int ClockId = Shader.PropertyToID("_PlantClock");
        private static readonly int TouchId = Shader.PropertyToID("_PlantTouch");
        private static readonly int DepthWindowId = Shader.PropertyToID("_PlantDepthWindow");
        private static readonly int DepthSideId = Shader.PropertyToID("_PlantDepthSide");
        private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("Vegetation.Update");
        private static readonly ProfilerMarker CutMarker = new ProfilerMarker("Vegetation.Cut");

        public int PlantCount => layout != null ? layout.plants.Length : 0;
        public int CutCount { get; private set; }
        public int ChunkCount => chunks.Count;
        public int LastCandidates { get; private set; }
        public int LastCutCount { get; private set; }
        public int VisibleChunks { get { int n=0; foreach(var c in chunks) if(c.renderer.isVisible||c.front.isVisible)n++; return n; } }
        public bool IsCut(int index) => cut != null && index >= 0 && index < cut.Length && cut[index];

        private sealed class Chunk
        {
            public GameObject gameObject;
            public Mesh mesh;
            public MeshRenderer renderer, front;
            public bool hasGeometry;
            public Texture texture;
            public readonly List<int> plants = new List<int>();
            public int[] firstVertex, leafCount, indices;
        }

        private void OnEnable()
        {
            WeaponSweep.Raised += OnSweep;
            Rebuild();
        }

        private void OnDisable()
        {
            WeaponSweep.Raised -= OnSweep;
            Release();
        }

        public void Rebuild()
        {
            Release();
            builtLayout = layout;
            if(layout == null || vegetationShader == null) return;
            if(transform.position.sqrMagnitude>.00001f || transform.rotation!=Quaternion.identity || transform.lossyScale!=Vector3.one)
            {
                Debug.LogError("Vegetation fields use baked world coordinates: keep their transform at the origin, unrotated and unit scale.",this);
                return;
            }
            cut = new bool[layout.plants.Length];
            // Validate once per species, not once per plant. A leaf is never cut into atlas strips.
            var textures = new Texture[layout.species.Length];
            for(int i=0;i<layout.species.Length;i++)
            {
                var species=layout.species[i];
                if(species==null || species.leaves==null)continue;
                foreach(var leaf in species.leaves)
                {
                    if(leaf==null || leaf.sprite==null)continue;
                    if(textures[i]==null)textures[i]=leaf.sprite.texture;
                    else if(textures[i]!=leaf.sprite.texture)
                    {
                        Debug.LogError("All leaves in a vegetation species must share one atlas.",layout);
                        textures[i]=null;
                        break;
                    }
                }
            }
            for(int i=0;i<layout.plants.Length;i++)
            {
                var p=layout.plants[i];
                if(p.species<0 || p.species>=layout.species.Length)continue;
                Texture texture=textures[p.species];
                if(texture==null)continue;
                var key=Cell(p.position);
                if(!cells.TryGetValue(key,out var list)) { list=new List<Chunk>(2); cells.Add(key,list); }
                Chunk chunk=null;
                foreach(var c in list)if(c.texture==texture) { chunk=c; break; }
                if(chunk==null)
                {
                    chunk=new Chunk { texture=texture };
                    list.Add(chunk); chunks.Add(chunk);
                    if(!materials.ContainsKey(texture))
                    {
                        materials.Add(texture,MakeMaterial(texture,-1));
                        frontMaterials.Add(texture,MakeMaterial(texture,1));
                    }
                    chunk.gameObject=new GameObject("Vegetation chunk "+key.x+","+key.y) { hideFlags=HideFlags.HideAndDontSave };
                    chunk.gameObject.transform.SetParent(transform,false);
                    chunk.renderer=chunk.gameObject.AddComponent<MeshRenderer>();
                    chunk.renderer.sharedMaterial=materials[texture];
                    chunk.renderer.sortingLayerName="Ground"; chunk.renderer.sortingOrder=35;
                    chunk.renderer.shadowCastingMode=ShadowCastingMode.Off; chunk.renderer.receiveShadows=false;
                    chunk.renderer.lightProbeUsage=LightProbeUsage.Off; chunk.renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
                    chunk.mesh=new Mesh { name="Vegetation chunk", hideFlags=HideFlags.HideAndDontSave };
                    chunk.mesh.MarkDynamic();
                    chunk.gameObject.AddComponent<MeshFilter>().sharedMesh=chunk.mesh;
                    var frontObject=new GameObject("Vegetation foreground") { hideFlags=HideFlags.HideAndDontSave };
                    frontObject.transform.SetParent(chunk.gameObject.transform,false);
                    frontObject.AddComponent<MeshFilter>().sharedMesh=chunk.mesh;
                    chunk.front=frontObject.AddComponent<MeshRenderer>();
                    chunk.front.sharedMaterial=frontMaterials[texture];
                    chunk.front.shadowCastingMode=ShadowCastingMode.Off; chunk.front.receiveShadows=false;
                    chunk.front.lightProbeUsage=LightProbeUsage.Off; chunk.front.reflectionProbeUsage=ReflectionProbeUsage.Off;
                    chunk.front.enabled=false;
                }
                chunk.plants.Add(i);
            }
            foreach(var chunk in chunks)BuildMesh(chunk);
            if(Application.IsPlaying(gameObject))debris=new VegetationDebris(transform,layout,vegetationShader);
            SyncDepth();
            UploadMotion();
        }

        private Material MakeMaterial(Texture texture,float side)
        {
            var material=new Material(vegetationShader) { name=side<0?"Vegetation (shared atlas)":"Vegetation (foreground)",
                mainTexture=texture, hideFlags=HideFlags.HideAndDontSave };
            material.SetFloat(DepthSideId,side);
            return material;
        }

        private void BuildMesh(Chunk c)
        {
            // Sort north-to-south within each chunk. The two draws share this geometry and cut indices.
            c.plants.Sort((a,b)=>layout.plants[b].position.y.CompareTo(layout.plants[a].position.y));
            var vertices=new List<Vector3>(c.plants.Count*8);
            var uv=new List<Vector2>(c.plants.Count*8);
            var metadata=new List<Vector4>(c.plants.Count*8);
            var motion=new List<Vector2>(c.plants.Count*8);
            c.firstVertex=new int[c.plants.Count]; c.leafCount=new int[c.plants.Count];
            var bounds=new Bounds();
            bool hasBounds=false;
            for(int n=0;n<c.plants.Count;n++)
            {
                var plant=layout.plants[c.plants[n]]; var s=layout.species[plant.species];
                c.firstVertex[n]=vertices.Count;
                for(int index=0;index<s.leaves.Length;index++)
                {
                    var leaf=s.leaves[index];
                    if(leaf==null || leaf.sprite==null)continue;
                    var sprite=leaf.sprite;
                    var rect=sprite.rect;
                    var textureRect=sprite.textureRect;
                    Vector2 pivot=sprite.pivot;
                    float ppu=sprite.pixelsPerUnit;
                    float left=-pivot.x/ppu, right=(rect.width-pivot.x)/ppu;
                    float bottom=-pivot.y/ppu, top=(rect.height-pivot.y)/ppu;
                    if(leaf.flipX){left=-left;right=-right;}
                    Vector3 root=plant.position+leaf.offset;
                    vertices.Add(root+new Vector3(left,bottom));
                    vertices.Add(root+new Vector3(right,bottom));
                    vertices.Add(root+new Vector3(right,top));
                    vertices.Add(root+new Vector3(left,top));
                    float u0=textureRect.xMin/c.texture.width,u1=textureRect.xMax/c.texture.width;
                    float v0=textureRect.yMin/c.texture.height,v1=textureRect.yMax/c.texture.height;
                    uv.Add(new Vector2(u0,v0)); uv.Add(new Vector2(u1,v0));
                    uv.Add(new Vector2(u1,v1)); uv.Add(new Vector2(u0,v1));
                    float variation=Mathf.Sin(plant.phase*1.731f+index*2.17f);
                    var data=new Vector4(root.x,root.y,plant.phase+leaf.phaseOffset,s.flexibility);
                    var movement=new Vector2((leaf.restAngle+variation*3f)*Mathf.Deg2Rad,.9f+variation*.1f);
                    for(int k=0;k<4;k++){ metadata.Add(data); motion.Add(movement); }
                    // Vertex rotation can leave the unrotated rectangle. A root-centred radius
                    // includes every possible rotation, keeping edge-of-camera leaves visible.
                    float radius=Mathf.Sqrt(Mathf.Max(left*left,right*right)+Mathf.Max(bottom*bottom,top*top));
                    leafReach=Mathf.Max(leafReach,radius);
                    rootOffset=Mathf.Max(rootOffset,leaf.offset.magnitude);
                    var leafBounds=new Bounds(root,new Vector3(radius*2,radius*2,.1f));
                    if(hasBounds)bounds.Encapsulate(leafBounds);
                    else {bounds=leafBounds;hasBounds=true;}
                    c.leafCount[n]++;
                }
            }
            c.mesh.SetVertices(vertices); c.mesh.SetUVs(0,uv); c.mesh.SetUVs(1,metadata); c.mesh.SetUVs(2,motion);
            c.indices=new int[vertices.Count/4*6];
            UploadIndices(c); c.mesh.bounds=bounds;
        }

        private void UploadIndices(Chunk c)
        {
            int index=0;
            for(int n=0;n<c.plants.Count;n++)
            {
                if(cut[c.plants[n]])continue;
                for(int layer=0;layer<c.leafCount[n];layer++)
                {
                    int v=c.firstVertex[n]+layer*4;
                    c.indices[index++]=v; c.indices[index++]=v+1; c.indices[index++]=v+2;
                    c.indices[index++]=v; c.indices[index++]=v+2; c.indices[index++]=v+3;
                }
            }
            c.mesh.SetTriangles(c.indices,0,index,0,false);
            c.hasGeometry=index>0;
            c.renderer.enabled=c.hasGeometry;
            c.front.enabled=c.hasGeometry&&foreground.Contains(c);
        }

        private void LateUpdate()
        {
            if(builtLayout!=layout)Rebuild();
            if(layout==null)return;
            using(UpdateMarker.Auto())
            {
                bool depthChanged=SyncDepth();
                float dt=Application.IsPlaying(gameObject)?Time.deltaTime:0f;
                if(dt>0)
                {
                    clock+=dt;
                    UpdateTouches(dt); debris?.Tick(dt);
                }
                if(dt>0||depthChanged)UploadMotion();
                foreach(var c in dirty)UploadIndices(c);
                dirty.Clear();
            }
        }

        private void UpdateTouches(float dt)
        {
            for(int i=1;i<TouchCount;i++) { ages[i]+=dt; touches[i].z=Mathf.Max(0,1-ages[i]/.55f)*.8f; }
            if(interactor==null || !interactor.gameObject.activeInHierarchy)
            { touches[0]=Vector4.zero; hasFeet=false; return; }
            Vector2 feet=(Vector2)interactor.position+new Vector2(0,.0625f);
            if(hasFeet && (feet-previousFeet).sqrMagnitude>9f)ClearTouches(); // teleport, not a field-wide trail
            touches[0]=new Vector4(feet.x,feet.y,1,.95f);
            trailClock+=dt;
            if(hasFeet && trailClock>=.07f && (feet-previousFeet).sqrMagnitude>.0001f)
            {
                touches[nextTouch]=new Vector4(feet.x,feet.y,.8f,.85f); ages[nextTouch]=0;
                nextTouch=nextTouch%7+1; trailClock=0;
            }
            previousFeet=feet; hasFeet=true;
        }

        private void UploadMotion()
        {
            foreach(var m in materials.Values)UploadMaterialMotion(m);
            foreach(var m in frontMaterials.Values)UploadMaterialMotion(m);
        }

        private void UploadMaterialMotion(Material material)
        {
            material.SetFloat(ClockId,clock); material.SetVectorArray(TouchId,touches);
            material.SetVector(DepthWindowId,depthWindow);
        }

        private bool SyncDepth()
        {
            if(depthInteractor!=interactor)
            {
                depthInteractor=interactor;
                depthBody=interactor!=null?interactor.GetComponentInChildren<SpriteRenderer>():null;
            }
            bool active=interactor!=null&&interactor.gameObject.activeInHierarchy;
            var window=new Vector4(1,1,0,0); // inverted rectangle disables foreground selection
            int layer=depthBody!=null?depthBody.sortingLayerID:SortingLayer.NameToID("Player");
            int order=depthBody!=null?depthBody.sortingOrder+3:3; // over body, carried axe and grip fold
            debris?.SetSorting(layer,order-3,active?interactor.position.y+.0625f:float.NegativeInfinity);
            if(active)
            {
                float feet=interactor.position.y+.0625f;
                var bounds=depthBody!=null?depthBody.bounds:new Bounds(interactor.position,new Vector3(2,2,0));
                // Only roots that could overlap the player need the foreground draw. Distant grass
                // retains its Ground ordering against existing props and overheads.
                window=new Vector4(bounds.min.x-leafReach,bounds.min.y-leafReach,bounds.max.x+leafReach,feet);
            }
            bool changed=window!=depthWindow;
            depthWindow=window;
            var min=Cell(new Vector2(window.x-rootOffset,window.y-rootOffset));
            var max=Cell(new Vector2(window.z+rootOffset,window.w+rootOffset));
            if(active==hasDepthCells&&(!active||(min==depthMin&&max==depthMax&&layer==depthLayer&&order==depthOrder)))return changed;
            foreach(var c in foreground)c.front.enabled=false;
            foreground.Clear();
            hasDepthCells=active; depthMin=min; depthMax=max; depthLayer=layer; depthOrder=order;
            if(active)for(int y=min.y;y<=max.y;y++)for(int x=min.x;x<=max.x;x++)
            {
                if(!cells.TryGetValue(new Vector2Int(x,y),out var list))continue;
                foreach(var c in list)
                {
                    foreground.Add(c);
                    c.front.sortingLayerID=layer; c.front.sortingOrder=order;
                    c.front.enabled=c.hasGeometry;
                }
            }
            return true;
        }

        private void ClearTouches()
        {
            for(int i=0;i<TouchCount;i++){touches[i]=Vector4.zero;ages[i]=1;}
            hasFeet=false; trailClock=0; nextTouch=1;
        }

        private static Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x/ChunkSize),Mathf.FloorToInt(p.y/ChunkSize));

        private void OnSweep(WeaponSweep sweep)
        {
            if(!Application.IsPlaying(gameObject)||cut==null||interactor==null||sweep.Source==null||sweep.Source.scene!=gameObject.scene)return;
            // Only the player's axe cuts these fields; enemy combat remains independent.
            if(sweep.Source.transform!=interactor && !sweep.Source.transform.IsChildOf(interactor))return;
            using(CutMarker.Auto())
            {
                LastCandidates=LastCutCount=0;
                Vector2 a=transform.InverseTransformPoint(sweep.Origin),b=transform.InverseTransformPoint(sweep.End);
                float reach=sweep.Radius+.2f;
                var min=Cell(Vector2.Min(a,b)-Vector2.one*reach); var max=Cell(Vector2.Max(a,b)+Vector2.one*reach);
                for(int y=min.y;y<=max.y;y++)for(int x=min.x;x<=max.x;x++)
                {
                    if(!cells.TryGetValue(new Vector2Int(x,y),out var list))continue;
                    foreach(var chunk in list)foreach(int i in chunk.plants)
                    {
                        if(cut[i])continue;
                        LastCandidates++;
                        Vector2 point=transform.TransformPoint(layout.plants[i].position);
                        if(!Contains(sweep,point) || (sweep.CanReach!=null && !sweep.CanReach(point)))continue;
                        cut[i]=true; CutCount++; LastCutCount++; dirty.Add(chunk);
                        var species=layout.species[layout.plants[i].species];
                        debris?.Emit(point,sweep.Aim,species.debrisColor,layout.plants[i].phase);
                    }
                }
            }
        }

        public static bool Contains(WeaponSweep sweep, Vector2 point)
        {
            Vector2 delta=point-sweep.Origin;
            if(sweep.IsMelee)
            {
                Vector2 aim=sweep.Aim.normalized;
                if(sweep.LaneWidth>0)
                {
                    float forward=Vector2.Dot(delta,aim),side=Mathf.Abs(delta.x*aim.y-delta.y*aim.x);
                    return forward>=0 && forward<=sweep.Radius && side<=sweep.LaneWidth*.5f;
                }
                if(delta.sqrMagnitude>sweep.Radius*sweep.Radius)return false;
                return sweep.Arc>=360 || delta.sqrMagnitude<.001f ||
                    Vector2.Dot(aim,delta.normalized)>=Mathf.Cos(sweep.Arc*.5f*Mathf.Deg2Rad);
            }
            Vector2 segment=sweep.End-sweep.Origin;
            float t=segment.sqrMagnitude>.00001f?Mathf.Clamp01(Vector2.Dot(delta,segment)/segment.sqrMagnitude):0;
            return (delta-segment*t).sqrMagnitude<=sweep.Radius*sweep.Radius;
        }

        public void ResetOnRest()
        {
            if(cut==null)return;
            System.Array.Clear(cut,0,cut.Length); CutCount=0;
            foreach(var c in chunks)dirty.Add(c);
            ClearTouches(); debris?.Clear();
        }

        private void Release()
        {
            foreach(var c in chunks){c.renderer.enabled=false;c.front.enabled=false;Dispose(c.gameObject);Dispose(c.mesh);}
            foreach(var m in materials.Values)Dispose(m);
            foreach(var m in frontMaterials.Values)Dispose(m);
            chunks.Clear();cells.Clear();materials.Clear();frontMaterials.Clear();foreground.Clear();dirty.Clear();
            debris?.Dispose();debris=null;cut=null;CutCount=0;clock=0;ClearTouches();
            hasDepthCells=false;depthInteractor=null;depthBody=null;leafReach=rootOffset=0;
            depthWindow=new Vector4(1,1,0,0);
        }

        internal static void Dispose(Object item)
        {
            if(item==null)return;
            if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);
        }
    }
}
