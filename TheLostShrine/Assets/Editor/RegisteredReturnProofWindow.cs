using TheLostShrine.Player;
using TheLostShrine.Weapons;
using UnityEditor;
using UnityEngine;

namespace TheLostShrine.Editor
{
    // A repeatable visual study; live gameplay confirms its own flight and possession.
    public static class RegisteredReturnStudy
    {
        public const float RecallAt = .8f, ArrivalAt = 1.4f, Duration = 2.25f;
        public struct Frame
        {
            public Sprite body, held, prop;
            public Vector2 propPosition;
            public float propScale;
            public bool propFlipX;
            public string phase;
        }

        public static Frame Sample(float seconds, int viewIndex, RegisteredActionSprites throwing,
            RegisteredActionSprites catching, AxeSpinSprites spin, ThrowActionTiming timing)
        {
            seconds = Mathf.Max(0, seconds);
            var view = throwing.views[viewIndex];
            var catchView = catching.views[viewIndex];
            var throwClock = new ThrowActionClock();
            throwClock.Begin(timing); throwClock.Commit();
            float flightSeconds;
            bool launched = throwClock.Advance(seconds, out flightSeconds);
            int cel = throwClock.IsActive ? throwClock.CelIndex : throwing.celCount-1;
            var frame = new Frame { body = view.body[cel], held = launched ? null : view.weapon[cel],
                propScale = view.freePropScale, propFlipX = view.spinFlipX,
                phase = launched ? "Outbound" : "Preparation / release" };
            // These are screen-space balance landmarks, not physics spawn offsets.
            Vector2 release = view.freePropOffset;
            Vector2 grip = catchView.freePropOffset;
            int startCel = view.spinStartCel;
            if (launched)
            {
                int spinCel = spin.Sample(flightSeconds,startCel);
                Vector2 far = release + view.direction*2f;
                if (seconds < RecallAt)
                    frame.propPosition = Vector2.Lerp(release,far,Mathf.Clamp01(flightSeconds/Mathf.Max(.001f,RecallAt-(seconds-flightSeconds))));
                else
                {
                    frame.phase = "Returning";
                    float approach = (seconds-RecallAt)/(ArrivalAt-RecallAt);
                    frame.propPosition = Vector2.Lerp(far,grip,approach);
                    frame.propScale = Mathf.Lerp(view.freePropScale,catchView.freePropScale,approach);
                    // Two terminal exposures align the complete prop to the receiving grip.
                    // No held layer is ever displayed alone in flight.
                    const float alignDuration = .0625f;
                    if (seconds >= ArrivalAt-alignDuration)
                    {
                        spinCel = seconds < ArrivalAt-alignDuration*.5f ? (catchView.spinStartCel+7)%8 : catchView.spinStartCel;
                        frame.propFlipX = catchView.spinFlipX;
                    }
                }
                frame.prop = spin.cels[spinCel];
            }
            if (seconds >= RecallAt)
            {
                var catchClock = new CatchPresentationClock();
                catchClock.BeginReach(catching);
                if (seconds >= ArrivalAt)
                {
                    // Explicit simulated arrival for this visual study ONLY.
                    catchClock.ConfirmCatch(); catchClock.Advance(seconds-ArrivalAt);
                    frame.prop = null; frame.phase = catchClock.IsActive ? "Grip / absorb / settle" : "Settled";
                }
                int catchCel = catchClock.IsActive ? catchClock.CelIndex : catching.celCount-1;
                frame.body = catching.views[viewIndex].body[catchCel];
                frame.held = catchClock.HasCaught ? catching.views[viewIndex].weapon[catchCel] : null;
            }
            return frame;
        }
    }

    public sealed class RegisteredReturnProofWindow : EditorWindow
    {
        private enum Study { ReturnLoop, CatchCels, SpinCels }
        private Study study;
        private RegisteredActionSprites throwing, catching;
        private AxeSpinSprites spin;
        private HatchetSettings settings;
        private bool playing = true, bodyOnly;
        private float seconds, speed = 1f;
        private int selectedCel;
        private double previous;

        [MenuItem("Road of the Old King/Animation/Return and Spin Review")]
        public static void Open() => GetWindow<RegisteredReturnProofWindow>("Return and spin");
        private void OnEnable()
        {
            throwing = AssetDatabase.LoadAssetAtPath<RegisteredActionSprites>(RegisteredSpriteProofBuilder.ThrowAssetPath);
            catching = AssetDatabase.LoadAssetAtPath<RegisteredActionSprites>(RegisteredSpriteProofBuilder.CatchAssetPath);
            spin = AssetDatabase.LoadAssetAtPath<AxeSpinSprites>(RegisteredSpriteProofBuilder.SpinAssetPath);
            settings = AssetDatabase.LoadAssetAtPath<HatchetSettings>("Assets/Settings/Weapons/TutorialHatchetSettings.asset");
            previous = EditorApplication.timeSinceStartup; EditorApplication.update += Tick;
        }
        private void OnDisable() => EditorApplication.update -= Tick;
        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (playing) seconds = (seconds+(float)(now-previous)*speed)%RegisteredReturnStudy.Duration;
            previous = now; if(playing) Repaint();
        }
        private void OnGUI()
        {
            if (throwing == null || catching == null || spin == null || settings == null)
            { EditorGUILayout.HelpBox("Import Registered Animations, then reopen this review.",MessageType.Info); return; }
            study = (Study)EditorGUILayout.EnumPopup("Study",study);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button(playing ? "Pause" : "Play")) playing = !playing;
            if(GUILayout.Button("Restart")) seconds = 0;
            EditorGUILayout.EndHorizontal();
            speed = EditorGUILayout.Slider("Playback speed",speed,.1f,1f);
            bodyOnly = EditorGUILayout.Toggle("Body only",bodyOnly);
            EditorGUI.BeginChangeCheck();
            if(study == Study.ReturnLoop) seconds = EditorGUILayout.Slider("Time",seconds,0,RegisteredReturnStudy.Duration);
            else selectedCel = EditorGUILayout.IntSlider("Cel",selectedCel+1,1,study == Study.CatchCels ? 4 : 8)-1;
            if(EditorGUI.EndChangeCheck()) playing = false;
            EditorGUILayout.LabelField("Four rotations/second • reach → confirmed grip → absorb → settle");
            var area = GUILayoutUtility.GetRect(100,1600,300,1100,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));
            int viewCount = Mathf.Min(throwing.views.Length,catching.views.Length);
            for(int v=0;v<viewCount;v++)
            {
                var frame = RegisteredReturnStudy.Sample(seconds,v,throwing,catching,spin,settings.throwAction);
                if(study == Study.CatchCels)
                {
                    int cel = playing ? catching.SampleAtSeconds(seconds%(.29f+.45f)) : Mathf.Min(selectedCel,3);
                    frame.body = catching.views[v].body[cel]; frame.held = catching.views[v].weapon[cel]; frame.prop = null;
                    frame.phase = "Catch cel "+(cel+1);
                }
                if(study == Study.SpinCels)
                {
                    frame.body = frame.held = null;
                    frame.prop = spin.cels[playing ? spin.Sample(seconds) : selectedCel];
                    frame.propPosition = Vector2.up*1.4f; frame.phase = "Complete free axe";
                }
                DrawPanel(new Rect(area.x+v*area.width/viewCount,area.y,area.width/viewCount-4,area.height),frame,v);
            }
            EditorGUILayout.HelpBox("Visual study only: the path and arrival are simulated. Tutorial confirms its live stationary catch from HatchetWeapon.ReturnedToHand; these screen-space landmarks must never move collision. Trails and streaks are off.",MessageType.None);
        }
        private void DrawPanel(Rect panel,RegisteredReturnStudy.Frame frame,int view)
        {
            GUI.BeginGroup(panel);
            EditorGUI.DrawRect(new Rect(0,0,panel.width,panel.height),new Color(.19f,.25f,.20f));
            GUI.Label(new Rect(10,8,panel.width-20,24),catching.views[view].name+" / "+frame.phase);
            float scale = Mathf.Min(panel.width/1100f,panel.height/1000f);
            Vector2 foot = new Vector2(panel.width*.3f,panel.height-70*scale);
            EditorGUI.DrawRect(new Rect(0,foot.y,panel.width,1),new Color(.55f,.53f,.36f));
            Draw(frame.body,foot,scale);
            if(!bodyOnly)
            {
                Draw(frame.held,foot,scale);
                Draw(frame.prop,foot+new Vector2(frame.propPosition.x,-frame.propPosition.y)*128*scale,scale*frame.propScale,frame.propFlipX);
            }
            GUI.EndGroup();
        }
        private static void Draw(Sprite sprite,Vector2 pivot,float scale,bool flipX = false)
        {
            if(sprite==null) return;
            var r=sprite.rect; var tex=sprite.texture;
            GUI.DrawTextureWithTexCoords(new Rect(pivot.x-sprite.pivot.x*scale,pivot.y-(r.height-sprite.pivot.y)*scale,r.width*scale,r.height*scale),
                tex,new Rect((flipX ? r.xMax : r.x)/tex.width,r.y/tex.height,
                    (flipX ? -r.width : r.width)/tex.width,r.height/tex.height),true);
        }
    }
}
