using System.IO;
using TheLostShrine.Player;
using TheLostShrine.Weapons;
using UnityEditor;
using UnityEngine;

namespace TheLostShrine.Editor
{
    public sealed class RegisteredSpriteProofWindow : EditorWindow
    {
        private enum ActionView { Forehand, Throw }
        private enum LayerView { Combined, BodyOnly, WeaponOnly }
        private ActionView action;
        private LayerView layerView;
        private RegisteredActionSprites forehand, throwing;
        private HatchetSettings settings;
        private readonly System.Collections.Generic.Dictionary<string, Sprite> references = new System.Collections.Generic.Dictionary<string, Sprite>();
        // Source-only patches are loaded on demand; runtime assets never reference them.
        private readonly System.Collections.Generic.Dictionary<string, Texture2D> revealSheets = new System.Collections.Generic.Dictionary<string, Texture2D>();
        private readonly ThrowActionClock clock = new ThrowActionClock();
        private double previous;
        private int selectedCel, launches;
        private bool manual = true, paused, flight = true, launched, holding, gameplayTiming = true, showReference = true;
        private float flightTime, meleeTime, speed = 1f;
        private RegisteredActionSprites Sprites => action == ActionView.Throw ? throwing : forehand;

        [MenuItem("Road of the Old King/Animation/Registered Action Review")]
        public static void Open() => GetWindow<RegisteredSpriteProofWindow>("Action layers");
        [MenuItem("Road of the Old King/Animation/Registered Throw Review")]
        public static void OpenThrow()
        {
            var window = GetWindow<RegisteredSpriteProofWindow>("Action layers");
            window.action = ActionView.Throw; window.ResetPreview();
        }

        private void OnEnable()
        {
            forehand = AssetDatabase.LoadAssetAtPath<RegisteredActionSprites>(RegisteredSpriteProofBuilder.ForehandAssetPath);
            throwing = AssetDatabase.LoadAssetAtPath<RegisteredActionSprites>(RegisteredSpriteProofBuilder.ThrowAssetPath);
            settings = AssetDatabase.LoadAssetAtPath<HatchetSettings>("Assets/Settings/Weapons/TutorialHatchetSettings.asset");
            references.Clear();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Sprites/Player/PlayerSheet.png"))
                if (asset is Sprite sprite) references[sprite.name] = sprite;
            previous = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }
        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            ClearRevealSheets();
        }

        private void ClearRevealSheets()
        {
            foreach (var texture in revealSheets.Values)
                if (texture != null) DestroyImmediate(texture);
            revealSheets.Clear();
        }
        private void OnLostFocus() { if (holding) ResetPreview(); }

        private void Begin(bool hold)
        {
            if (settings == null) return;
            clock.Cancel();
            if (action == ActionView.Throw) { clock.Begin(settings.throwAction); if (!hold) clock.Commit(); }
            holding = action == ActionView.Throw && hold;
            manual = paused = launched = false; flightTime = meleeTime = 0f; launches = 0;
            previous = EditorApplication.timeSinceStartup;
        }
        private void ResetPreview()
        {
            ClearRevealSheets();
            clock.Cancel(); manual = true; selectedCel = 0; launched = holding = false; flightTime = meleeTime = 0f; launches = 0;
            Repaint();
        }
        private float MeleeDuration => gameplayTiming ? settings.lightDuration : forehand.Duration;
        private int CurrentCel => manual ? selectedCel : action == ActionView.Throw
            ? clock.IsActive ? clock.CelIndex : launched ? throwing.celCount-1 : 0
            : gameplayTiming ? forehand.SampleMeleeCel(meleeTime/settings.lightDuration, settings.lightWindupFraction, settings.lightSwingEndFraction)
            : forehand.SampleAtSeconds(meleeTime);

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - previous) * speed; previous = now;
            if (manual || paused || Sprites == null || settings == null) return;
            if (action == ActionView.Forehand) meleeTime = (meleeTime + dt) % (MeleeDuration + .55f);
            else
            {
                if (!clock.IsActive && flightTime >= .6f) return;
                bool wasActive = clock.IsActive;
                if (clock.Advance(dt, out float flightSeconds)) { launched = true; launches++; }
                if (launched) flightTime = Mathf.Min(.6f, flightTime + (wasActive ? flightSeconds : dt));
            }
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            action = (ActionView)EditorGUILayout.EnumPopup("Action", action);
            if (EditorGUI.EndChangeCheck()) ResetPreview();
            if (Sprites == null || settings == null)
            { EditorGUILayout.HelpBox("Import Registered Animations from Road of the Old King > Animation, then reopen this review.", MessageType.Info); return; }
            EditorGUILayout.LabelField("Registered layers - " + string.Join(" / ", System.Array.ConvertAll(Sprites.views, view => view.name)), EditorStyles.boldLabel);
            EditorGUILayout.LabelField("640 px canvas / 128 PPU / shared ground pivot / full-body drawings");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(action == ActionView.Throw ? "Quick tap E" : "Play forehand")) Begin(false);
            if (action == ActionView.Throw && GUILayout.Button(holding ? "Release E" : "Hold E"))
            { if (holding) { holding = false; clock.Commit(); } else Begin(true); }
            if (GUILayout.Button(paused ? "Resume" : "Pause")) paused = !paused;
            if (GUILayout.Button("Reset")) ResetPreview();
            EditorGUILayout.EndHorizontal();
            speed = EditorGUILayout.Slider("Playback speed", speed, .1f, 1f);
            EditorGUI.BeginChangeCheck();
            layerView = (LayerView)EditorGUILayout.EnumPopup("Layer view", layerView);
            if (EditorGUI.EndChangeCheck()) ClearRevealSheets();
            showReference = EditorGUILayout.Toggle("Show original character at same PPU", showReference);
            if (action == ActionView.Throw) flight = EditorGUILayout.Toggle("Independent flight after release", flight);
            else
            {
                EditorGUI.BeginChangeCheck();
                gameplayTiming = EditorGUILayout.Toggle("Match existing gameplay contact window", gameplayTiming);
                if (EditorGUI.EndChangeCheck()) ResetPreview();
            }
            int cel = CurrentCel;
            EditorGUI.BeginChangeCheck();
            selectedCel = EditorGUILayout.IntSlider("Cel", cel + 1, 1, Sprites.celCount) - 1;
            if (EditorGUI.EndChangeCheck()) { manual = true; clock.Cancel(); holding = false; cel = selectedCel; }
            string status = action == ActionView.Throw ? (manual ? "Scrub" : clock.Phase.ToString()) + " / launches: " + launches
                : (cel >= forehand.contactCel && cel < forehand.recoveryCel ? "Contact cel" : "Anticipation / recovery") +
                  (gameplayTiming ? " / gameplay: " + (settings.lightDuration*1000).ToString("F0") + " ms, contact " +
                      (settings.lightDuration*settings.lightWindupFraction*1000).ToString("F0") + "-" +
                      (settings.lightDuration*settings.lightSwingEndFraction*1000).ToString("F0") + " ms"
                   : " / authored study: " + (forehand.Duration*1000).ToString("F0") + " ms, contact " +
                      (forehand.CelStart(forehand.contactCel)*1000).ToString("F0") + "-" +
                      (forehand.CelStart(forehand.recoveryCel)*1000).ToString("F0") + " ms");
            EditorGUILayout.LabelField(status);
            var area = GUILayoutUtility.GetRect(100, 1400, 320, 900, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            float panelWidth = area.width / Sprites.views.Length;
            for (int i = 0; i < Sprites.views.Length; i++)
                DrawView(new Rect(area.x+i*panelWidth, area.y, panelWidth-4, area.height), Sprites.views[i], cel);
            if (GUILayout.Button("Open catch / spin / return review")) RegisteredReturnProofWindow.Open();
            EditorGUILayout.HelpBox("Review only. Forehand exposures map to existing gameplay contact timing; the throw uses ThrowActionClock. Held weapon layers include hand occlusion. Tutorial uses opening strikes and stationary throws in all four cardinal directions; other actions retain existing poses.", MessageType.None);
            var e = Event.current;
            if (action == ActionView.Throw && e.type == EventType.KeyDown && e.keyCode == KeyCode.E && !holding) { Begin(true); e.Use(); }
            if (e.type == EventType.KeyUp && e.keyCode == KeyCode.E && holding) { holding = false; clock.Commit(); e.Use(); }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { ResetPreview(); e.Use(); }
        }

        private void DrawView(Rect panel, RegisteredActionSprites.View view, int cel)
        {
            GUI.BeginGroup(panel);
            panel = new Rect(0,0,panel.width,panel.height);
            EditorGUI.DrawRect(panel, new Color(.19f,.25f,.20f));
            GUI.Label(new Rect(10,8,120,24), view.name);
            float scale = Mathf.Min(panel.width / 850f, panel.height / 760f);
            Vector2 origin = new Vector2(panel.center.x+(showReference ? 120 : -70)*scale, panel.yMax-64*scale);
            EditorGUI.DrawRect(new Rect(0,origin.y,panel.width,1), new Color(.55f,.53f,.36f));
            if (showReference && references.TryGetValue(view.name + "_00", out var reference))
            {
                Rect dst = new Rect(135*scale-reference.pivot.x*scale,
                    origin.y-(reference.rect.height-reference.pivot.y)*scale, reference.rect.width*scale, reference.rect.height*scale);
                DrawCel(dst,reference);
                GUI.Label(new Rect(30,origin.y+8,130,24),"Original reference");
            }
            Rect canvas = new Rect(origin.x-320*scale,origin.y-544*scale,640*scale,640*scale);
            if (layerView != LayerView.WeaponOnly)
            {
                if (layerView == LayerView.BodyOnly) DrawReveal(canvas, view, cel);
                DrawCel(canvas,view.body[cel]);
            }
            if (layerView != LayerView.BodyOnly)
            {
                if (action == ActionView.Throw && !manual && flight && launched)
                {
                    Vector2 motion = view.direction * (flightTime*settings.throwSpeed*128*scale);
                    canvas.position += new Vector2(motion.x,-motion.y);
                    int release = settings.throwAction.preparation.Length+settings.throwAction.aim.Length+settings.throwAction.launchCel;
                    DrawCel(canvas,view.weapon[release]);
                }
                else DrawCel(canvas,view.weapon[cel]);
            }
            GUI.EndGroup();
        }

        private void DrawReveal(Rect destination, RegisteredActionSprites.View view, int cel)
        {
            var texture = GetRevealSheet(view, cel);
            if (texture == null) return;
            var rect = view.body[cel].rect;
            GUI.DrawTextureWithTexCoords(destination, texture, new Rect(rect.x / texture.width,
                rect.y / texture.height, rect.width / texture.width, rect.height / texture.height), true);
        }

        private Texture2D GetRevealSheet(RegisteredActionSprites.View view, int cel)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../ArtSource/Player/Registered/Combat/" + action + "-" + view.name + "-Reveal.png"));
            if (!revealSheets.TryGetValue(path, out var texture))
            {
                texture = null;
                if (File.Exists(path))
                {
                    var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                    if (loaded.LoadImage(File.ReadAllBytes(path), true) &&
                        loaded.width == view.body[cel].texture.width && loaded.height == view.body[cel].texture.height)
                        texture = loaded;
                    else
                    {
                        DestroyImmediate(loaded);
                        Debug.LogWarning("Invalid review-only reveal sheet: " + path);
                    }
                }
                // Missing source art is allowed when reviewing a runtime-only checkout.
                revealSheets[path] = texture;
            }
            return texture;
        }

        private static void DrawCel(Rect destination, Sprite sprite)
        {
            if (sprite == null) return;
            var r = sprite.rect; var texture = sprite.texture;
            GUI.DrawTextureWithTexCoords(destination,texture,new Rect(r.x/texture.width,r.y/texture.height,r.width/texture.width,r.height/texture.height),true);
        }
    }
}
