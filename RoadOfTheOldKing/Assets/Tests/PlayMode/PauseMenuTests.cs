// Ported 18 Nov from the Tools/Verification snippet PauseMenuChecks.cs.txt, logic unchanged: the test runner gives each
// test an empty Play Mode scene with no GameSession, the setup these checks always needed. Run it from
// Window > General > Test Runner > PlayMode. The body still reads like the snippet; tidy it when this area is next touched.
#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public sealed class PauseMenuTests
{
    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator PauseMenuAndLeases()
    {
        yield return CheckRun.Settle();
        CheckRun.Expect(Run());
    }

    private static object Run()
    {
        // Pause menu lifecycle (rewritten 21 Oct for MenuStack, owner-scoped control/pause leases and the current
        // Resume/Controls/Settings/Main Menu/Quit list). Empty, unsaved Play Mode scene without a GameSession; run through FixtureRunner
        // (Runner.Skip 0). Synchronous: the closing-frame shield is completed by running the menu's own UnlockNextFrame
        // enumerator in place of the next frame. Commands are injected where executing them would save, load or quit.
        if (!Application.isPlaying || RoadOfTheOldKing.Progression.GameSession.Instance != null || RoadOfTheOldKing.UI.MenuStack.IsAnyOpen ||
            RoadOfTheOldKing.UI.SimulationPause.IsPaused)
            throw new Exception("Use an empty Play Mode scene without a GameSession, open menus or a held pause (a hit-stop releases after a real frame).");
        var results=new List<string>();
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception("FAIL: "+label);results.Add(label);};
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
        var oldScale=Time.timeScale;var oldAudio=AudioListener.pause;
        var protectedKeys=new[]{"RoadOfTheOldKing.Save","RoadOfTheOldKing.Save.Resume","RoadOfTheOldKing.Tutorial.Save.v1","TheLostShrine.PrototypeLoop.Save.v1"};
        Func<string> snapshot=()=>string.Join("|",protectedKeys.Select(k=>k+"="+(PlayerPrefs.HasKey(k)?PlayerPrefs.GetString(k):"<none>")).ToArray());
        string savesBefore=snapshot();
        GameObject root=null, weaponRoot=null;
        UnityEngine.InputSystem.Keyboard keyboard=null;var oldKeyboard=UnityEngine.InputSystem.Keyboard.current;
        try {
            Time.timeScale=1;AudioListener.pause=false;
            root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
            var menu=root.GetComponent<RoadOfTheOldKing.UI.PauseMenuController>();
            var view=root.GetComponent<RoadOfTheOldKing.UI.PauseMenuView>();
            var locks=root.GetComponent<RoadOfTheOldKing.Player.PlayerControlLocks>();
            var combat=root.GetComponent<RoadOfTheOldKing.Player.PlayerCombatController>();
            var dialogue=root.GetComponent<RoadOfTheOldKing.UI.DialogueBox>();
            var restoring=menu.GetType().GetField("restoring",flags);
            // The next frame, in place: stop the scheduled shield and run the same enumerator to completion.
            Action nextFrame=()=>{
                var c=(Coroutine)restoring.GetValue(menu);if(c==null)return;menu.StopCoroutine(c);
                var e=(System.Collections.IEnumerator)menu.GetType().GetMethod("UnlockNextFrame",flags).Invoke(menu,null);while(e.MoveNext()){}
            };
            int resumes=0,quits=0;
            var actions=new[]{new RoadOfTheOldKing.UI.PauseMenuAction("Resume","Back",()=>resumes++),new RoadOfTheOldKing.UI.PauseMenuAction("Quit","Leave",()=>quits++)};

            // Default list: Resume, Controls, Settings, Main Menu, Quit. Without a session Main Menu is unavailable.
            var defaults=RoadOfTheOldKing.UI.PauseMenuCommands.CreateDefault(()=>{},()=>{});
            check(defaults.Length==5&&defaults[0].Label=="Resume"&&defaults[1].Label=="Controls"&&defaults[2].Label=="Settings"&&defaults[3].Label=="Main Menu"&&defaults[4].Label=="Quit","Default list is Resume, Controls, Settings, Main Menu, Quit");
            check(defaults[0].CloseOnExecute&&!defaults[1].CloseOnExecute&&!defaults[2].CloseOnExecute&&!defaults[3].Enabled,"Resume closes, Controls and Settings page, Main Menu disabled without a session");

            // Open: one modal, one pause lease, one control lease owned by the menu.
            check(!menu.IsOpen&&!view.IsVisible&&!locks.IsLocked,"Menu starts hidden with no leases");
            check(menu.Open()&&view.IsVisible&&Time.timeScale==0&&AudioListener.pause,"Open pauses simulation and audio and shows the menu");
            check(RoadOfTheOldKing.UI.MenuStack.Top==(RoadOfTheOldKing.UI.IModalMenu)menu&&locks.IsLockedBy(menu),"Open pushes onto MenuStack and takes its own control lease");
            check(!menu.Open(),"Repeated open cannot stack a second pause");
            // Close: the lease outlives the closing frame so the key/click cannot leak into gameplay.
            menu.Close();
            check(!menu.IsOpen&&!view.IsVisible&&Time.timeScale==1&&!AudioListener.pause&&!RoadOfTheOldKing.UI.MenuStack.IsAnyOpen,"Close restores clock/audio, hides and leaves MenuStack");
            check(menu.BlocksGameplay&&locks.IsLockedBy(menu),"Closing frame keeps the control lease (no input leakage)");
            check(!menu.Open(),"Closing frame cannot reopen");
            menu.Toggle();check(!menu.IsOpen,"Escape during the closing frame is ignored");
            nextFrame();check(!menu.BlocksGameplay&&!locks.IsLocked,"Next frame releases only the menu's lease");

            // Owner-scoped leases: another owner's lock survives the menu; the menu's survives the other's release.
            var other=new object();locks.Lock(other);menu.Open();locks.Unlock(other);
            check(locks.IsLockedBy(menu)&&!locks.IsLockedBy(other),"Another owner's unlock leaves the menu's lease");
            locks.Lock(other);menu.Close();nextFrame();
            check(locks.IsLockedBy(other)&&!locks.IsLockedBy(menu),"Menu release leaves another owner's lease");locks.Unlock(other);
            // Overlapping pause owners.
            var freeze=RoadOfTheOldKing.UI.SimulationPause.Acquire();menu.Open();freeze.Dispose();
            check(Time.timeScale==0,"Ending an overlapping freeze cannot unpause the menu");menu.Close();nextFrame();
            check(Time.timeScale==1,"Last pause owner restores the clock");
            menu.Open();freeze=RoadOfTheOldKing.UI.SimulationPause.Acquire();menu.Close();nextFrame();
            check(Time.timeScale==0,"Menu close respects another pause owner");freeze.Dispose();freeze.Dispose();
            check(Time.timeScale==1,"Pause lease disposal is idempotent");
            Time.timeScale=.25f;menu.Open();menu.Close();nextFrame();
            check(Time.timeScale==.25f,"Prior slow motion is restored");Time.timeScale=1;

            // MenuStack routing: with another modal on top, Escape goes to that modal and never opens pause.
            RoadOfTheOldKing.UI.MenuStack.Push(dialogue);menu.Toggle();
            check(!menu.IsOpen&&RoadOfTheOldKing.UI.MenuStack.Top==(RoadOfTheOldKing.UI.IModalMenu)dialogue,"Escape routes to the top modal instead of opening pause");
            check(!menu.Open(),"Pause refuses to open over another modal");RoadOfTheOldKing.UI.MenuStack.Remove(dialogue);
            menu.Toggle();check(menu.IsOpen,"Escape opens pause when nothing else is open");
            menu.Toggle();check(!menu.IsOpen,"Escape closes the open pause");nextFrame();

            // Settings paging with the default list: Back returns to the main page on the Settings row, then closes.
            menu.Open();menu.Navigate(2);menu.Submit();
            check(menu.IsOpen&&(bool)menu.GetType().GetField("inSettings",flags).GetValue(menu),"Settings opens a page without closing");
            // Settings rows step through their values and stay on their row; the player's own settings are put back after.
            var settingKeys=new[]{"RoadOfTheOldKing.Settings.Volume","RoadOfTheOldKing.Settings.MusicVolume","RoadOfTheOldKing.Settings.AmbienceVolume","RoadOfTheOldKing.Settings.EffectsVolume","RoadOfTheOldKing.Settings.FlashScale","RoadOfTheOldKing.Settings.ShakeScale"};
            var settingsBefore=settingKeys.Select(k=>PlayerPrefs.HasKey(k)?(float?)PlayerPrefs.GetFloat(k):null).ToArray();
            float flashBefore=RoadOfTheOldKing.UI.FeedbackSettings.FlashScale,shakeBefore=RoadOfTheOldKing.UI.FeedbackSettings.ShakeScale,listenerBefore=AudioListener.volume;
            try{
             var options=((RoadOfTheOldKing.UI.PauseMenuView)menu.GetType().GetField("view",flags).GetValue(menu)).Options;
             Func<List<string>> rowLabels=()=>options.Rows.Where(r=>r.Selectable).Select(r=>r.Label).ToList();
             var expected=new[]{"Master","Music","Ambience","Effects","Window","Atmosphere","Screen flashes","Screen shake","Vitals","Back"};
             check(rowLabels().SequenceEqual(expected)&&options.Rows.Count(r=>!r.Selectable)==3,"Settings: sound, display and comfort groups ("+string.Join(" | ",rowLabels().ToArray())+")");
             RoadOfTheOldKing.UI.SoundSettings.SetVolume(1f);RoadOfTheOldKing.UI.FeedbackSettings.SetFlash(1f);
             menu.Back();menu.Submit();
             check(options.SelectedRow!=null&&options.SelectedRow.Label=="Master","Settings opens on Master");
             menu.Adjust(-1);menu.Adjust(-1);
             check(Mathf.Approximately(RoadOfTheOldKing.UI.SoundSettings.Volume,.8f)&&Mathf.Approximately(AudioListener.volume,.8f),"Left steps the volume down 10% at a time and sets the listener");
             RoadOfTheOldKing.UI.SoundSettings.SetChannelVolume(RoadOfTheOldKing.Audio.AudioChannel.Music,1f);menu.Navigate(1);menu.Adjust(-1);
             float musicDb;var mixer=Resources.Load<UnityEngine.Audio.AudioMixer>("Audio/Mixer");
             check(options.SelectedRow.Label=="Music"&&Mathf.Approximately(RoadOfTheOldKing.UI.SoundSettings.ChannelVolume(RoadOfTheOldKing.Audio.AudioChannel.Music),.9f)&&mixer!=null&&mixer.GetFloat("MusicVolume",out musicDb)&&Mathf.Abs(musicDb-20f*Mathf.Log10(.9f))<.05f,"Music steps down 10% and sets the mixer channel in dB");
             RoadOfTheOldKing.UI.SoundSettings.SetChannelVolume(RoadOfTheOldKing.Audio.AudioChannel.Music,1f);
             menu.Navigate(1);menu.Navigate(1);menu.Navigate(1);menu.Navigate(1);menu.Navigate(1);menu.Adjust(-1);
             check(options.SelectedRow.Label=="Screen flashes"&&Mathf.Approximately(RoadOfTheOldKing.UI.FeedbackSettings.FlashScale,.9f)&&(bool)menu.GetType().GetField("inSettings",flags).GetValue(menu),"Down skips the group headings; left lowers flashes and stays on the page");
             menu.Navigate(1);menu.Navigate(1);menu.Navigate(1);check(options.SelectedRow.Label=="Back","The last row is Back");
             menu.Submit();check(menu.IsOpen&&!(bool)menu.GetType().GetField("inSettings",flags).GetValue(menu),"The Back row returns to the main page");
             menu.Submit();
            }finally{
             for(int i=0;i<settingKeys.Length;i++){if(settingsBefore[i].HasValue)PlayerPrefs.SetFloat(settingKeys[i],settingsBefore[i].Value);else PlayerPrefs.DeleteKey(settingKeys[i]);}
             PlayerPrefs.Save();RoadOfTheOldKing.UI.FeedbackSettings.FlashScale=flashBefore;RoadOfTheOldKing.UI.FeedbackSettings.ShakeScale=shakeBefore;AudioListener.volume=listenerBefore;
            }
            menu.Back();check(menu.IsOpen&&!(bool)menu.GetType().GetField("inSettings",flags).GetValue(menu),"Back from Settings returns to the main page");
            menu.Back();check(!menu.IsOpen,"Back on the main page closes");nextFrame();

            // Controls page: lists both devices from ControlLabels, returns by Back or its own row, and hides again on the main page.
            Func<UnityEngine.UIElements.VisualElement,string,UnityEngine.UIElements.VisualElement> find=(e,n)=>UnityEngine.UIElements.UQueryExtensions.Q(e,n,(string)null);
            Func<UnityEngine.UIElements.VisualElement> pageRoot=()=>root.GetComponentsInChildren<UnityEngine.UIElements.UIDocument>(true).First(d=>find(d.rootVisualElement,"pause-panel")!=null).rootVisualElement;
            var inControls=menu.GetType().GetField("inControls",flags);
            menu.Open();menu.Navigate(1);menu.Submit();
            check(menu.IsOpen&&(bool)inControls.GetValue(menu),"Controls opens a page without closing");
            var page=pageRoot();
            check(find(page,"pause-panel").ClassListContains("controls-open")&&((UnityEngine.UIElements.Label)find(page,"heading")).text=="CONTROLS","The page shows the table under its own heading");
            Func<string,List<string>> cells=c=>UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Label>(find(page,"controls"),(string)null,c).ToList().Select(l=>l.text).Where(t=>t!="").ToList();
            var names=cells("controls-action");var keys=cells("controls-keys");var pads=cells("controls-pad");
            foreach(var row in new[]{"Move","Sprint","Dodge","Light attack","Charged cleave","Throw","Aim","Drink flask","Interact","Inventory","Look around","Zoom","Pause","Select","Back"})check(names.Contains(row),"Lists "+row);
            check(keys.Contains(RoadOfTheOldKing.UI.ControlLabels.KeyboardLabel("dash"))&&pads.Contains(RoadOfTheOldKing.UI.ControlLabels.GamepadLabel("dash"))&&keys.Contains("Space")&&pads.Contains("B"),"Dodge reads Space and B from ControlLabels");
            check(keys.Count==pads.Count&&keys.Count==17,"All 15 rows have a key and a pad name, plus each block's column title");
            check(!names.Contains("Throw, Recall"),"Recall stays unnamed until it's earned");
            menu.Back();check(menu.IsOpen&&!(bool)inControls.GetValue(menu)&&!find(pageRoot(),"pause-panel").ClassListContains("controls-open"),"Back returns to the main page and drops the table");
            menu.Submit();check((bool)inControls.GetValue(menu),"Back left the selection on the Controls row");
            menu.Submit();check(menu.IsOpen&&!(bool)inControls.GetValue(menu),"The page's own Back row returns too");
            menu.Back();check(!menu.IsOpen,"The main page still closes");nextFrame();

            // Injected commands: dispatch once; disabled commands are inert.
            menu.ConfigureActions(actions);
            menu.Open();menu.Submit();menu.Submit();
            check(resumes==1&&!menu.IsOpen,"A command dispatches once and closes; the closing frame swallows a second submit");nextFrame();
            menu.Open();menu.Navigate(1);menu.Submit();check(quits==1,"Navigation selects the second command");nextFrame();
            menu.ConfigureActions(new[]{new RoadOfTheOldKing.UI.PauseMenuAction("Unavailable","Disabled",()=>quits++,false)});
            menu.Open();menu.Submit();check(menu.IsOpen&&quits==1,"Disabled command cannot execute");menu.Close();nextFrame();
            menu.ConfigureActions(actions);

            // Gameplay actions are cancelled by the lease on the next combat tick, and by focus loss.
            weaponRoot=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Axe.prefab"));
            var weapon=weaponRoot.GetComponent<RoadOfTheOldKing.Weapons.AxeWeapon>();check(combat.TryEquip(weapon),"Axe equipped");
            var activeField=combat.GetType().GetField("controlsActive",flags);var combatUpdate=combat.GetType().GetMethod("Update",flags);
            activeField.SetValue(combat,true);check(weapon.TryBeginCharge(),"Charge begins");
            menu.Open();combatUpdate.Invoke(combat,null);
            check(weapon.State==RoadOfTheOldKing.Weapons.AxeState.Held,"Open pause cancels a charge on the next combat tick");menu.Close();nextFrame();
            activeField.SetValue(combat,true);check(weapon.TryLightChop(Vector2.right),"Combo begins");
            menu.Open();combatUpdate.Invoke(combat,null);
            check(weapon.State==RoadOfTheOldKing.Weapons.AxeState.Held&&weapon.ComboTimeRemaining==0,"Open pause cancels a combo on the next combat tick");menu.Close();nextFrame();
            activeField.SetValue(combat,true);check(weapon.TryBeginCharge(),"Charge begins again");
            combat.GetType().GetMethod("OnApplicationFocus",flags).Invoke(combat,new object[]{false});
            check(weapon.State==RoadOfTheOldKing.Weapons.AxeState.Held&&!(bool)activeField.GetValue(combat),"Focus loss cancels a pending charge and marks controls inactive");

            // Real Input System keys through PauseMenuInput.
            var menuInput=root.GetComponent<RoadOfTheOldKing.UI.PauseMenuInput>();var poll=menuInput.GetType().GetMethod("PollInput",flags);
            keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            Action<UnityEngine.InputSystem.Key> press=key=>{
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());UnityEngine.InputSystem.InputSystem.Update();
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(key));UnityEngine.InputSystem.InputSystem.Update();
                poll.Invoke(menuInput,null);};
            press(UnityEngine.InputSystem.Key.Escape);check(menu.IsOpen,"Escape key opens pause");
            press(UnityEngine.InputSystem.Key.DownArrow);press(UnityEngine.InputSystem.Key.Enter);
            check(quits==2&&!menu.IsOpen,"Arrow and Enter keys select and execute");nextFrame();
            press(UnityEngine.InputSystem.Key.Escape);press(UnityEngine.InputSystem.Key.Escape);
            check(!menu.IsOpen&&resumes==1,"Second Escape closes without executing a command");nextFrame();

            // Disable releases every lease and the stack entry, including mid-shield.
            menu.Open();menu.enabled=false;
            check(!menu.IsOpen&&!view.IsVisible&&Time.timeScale==1&&!locks.IsLocked&&!RoadOfTheOldKing.UI.MenuStack.IsAnyOpen,"Disable releases pause, lease and MenuStack");
            menu.enabled=true;menu.Open();menu.Close();menu.enabled=false;
            check(!menu.BlocksGameplay&&!locks.IsLocked,"Disable during the closing frame releases the lease");menu.enabled=true;
        } finally {
            if(keyboard!=null){UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);if(oldKeyboard!=null)oldKeyboard.MakeCurrent();}
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);
            if(weaponRoot!=null)UnityEngine.Object.DestroyImmediate(weaponRoot);
            Time.timeScale=oldScale;AudioListener.pause=oldAudio;
        }
        check(snapshot()==savesBefore,"Protected save entries unchanged");
        check(!RoadOfTheOldKing.UI.MenuStack.IsAnyOpen&&!RoadOfTheOldKing.UI.SimulationPause.IsPaused,"No menu or pause lease left behind");
        return "PASS "+results.Count+"\n"+string.Join("\n",results.ToArray());
    }
}
#endif
