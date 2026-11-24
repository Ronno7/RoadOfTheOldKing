// Ported 18 Nov from the Tools/Verification snippet ResumePersistenceChecks.cs.txt, logic unchanged: the test runner gives each
// test an empty Play Mode scene with no GameSession, the setup these checks always needed. Run it from
// Window > General > Test Runner > PlayMode. The body still reads like the snippet; tidy it when this area is next touched.
#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public sealed class ResumePersistenceTests
{
    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator ResumeSurvivesStoreFailures()
    {
        yield return CheckRun.Settle();
        CheckRun.Expect(Run());
    }

    // 18 Nov: the Steam demo and the playtest build share PlayerPrefs on one Windows account. Each profile starting
    // over (New Game clears its store) must leave the other's progress, resume point and older keys alone. The keys
    // follow SaveProfile's rule on a disposable root, so no real save is read or written.
    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator SaveProfilesStayApart()
    {
        yield return CheckRun.Settle();
        string root = "RoadOfTheOldKing.Verification.Profile." + Guid.NewGuid().ToString("N"), legacy = root + ".Legacy";
        string playtestKey = RoadOfTheOldKing.Progression.SaveProfile.Key(root, false), demoKey = RoadOfTheOldKing.Progression.SaveProfile.Key(root, true);
        var playtest = new RoadOfTheOldKing.Progression.PlayerPrefsProgressStore(playtestKey, RoadOfTheOldKing.Progression.SaveProfile.LegacyKeys(new[] { legacy }, false));
        var demo = new RoadOfTheOldKing.Progression.PlayerPrefsProgressStore(demoKey, RoadOfTheOldKing.Progression.SaveProfile.LegacyKeys(new[] { legacy }, true));
        var touched = new[] { playtestKey, playtestKey + ".Resume", demoKey, demoKey + ".Resume", legacy };
        var protectedKeys = new[] { "RoadOfTheOldKing.Save", "RoadOfTheOldKing.Save.Resume", "RoadOfTheOldKing.Save.Demo", "RoadOfTheOldKing.Save.Demo.Resume", "RoadOfTheOldKing.Tutorial.Save.v1" };
        Func<string> snapshot = () => string.Join("|", protectedKeys.Select(k => k + "=" + (PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k) : "<none>")).ToArray());
        string before = snapshot();
        try
        {
            NUnit.Framework.Assert.AreNotEqual(playtestKey, demoKey, "the demo has its own progress key");
            NUnit.Framework.Assert.IsEmpty(RoadOfTheOldKing.Progression.SaveProfile.LegacyKeys(new[] { legacy }, true), "the demo migrates no older key");
            playtest.Save(new RoadOfTheOldKing.Progression.ProgressState { bronzeCoins = 7 });
            playtest.SaveResume(new RoadOfTheOldKing.Progression.ResumePoint { scenePath = "Playtest", x = 1f });
            demo.Save(new RoadOfTheOldKing.Progression.ProgressState { bronzeCoins = 3 });
            demo.SaveResume(new RoadOfTheOldKing.Progression.ResumePoint { scenePath = "Demo", x = 2f });
            PlayerPrefs.SetString(legacy, "older save");

            demo.Clear(); // the demo starts over
            NUnit.Framework.Assert.AreEqual(7, playtest.Load().bronzeCoins, "a demo New Game keeps the playtest progress");
            NUnit.Framework.Assert.AreEqual("Playtest", playtest.LoadResume().scenePath, "a demo New Game keeps the playtest resume point");
            NUnit.Framework.Assert.AreEqual("older save", PlayerPrefs.GetString(legacy), "a demo New Game keeps the older key");
            NUnit.Framework.Assert.AreEqual(0, demo.Load().bronzeCoins, "the demo starts fresh, not from the playtest run");
            NUnit.Framework.Assert.IsNull(demo.LoadResume(), "the demo can't Continue the playtest spot");

            demo.Save(new RoadOfTheOldKing.Progression.ProgressState { bronzeCoins = 3 });
            demo.SaveResume(new RoadOfTheOldKing.Progression.ResumePoint { scenePath = "Demo", x = 2f });
            playtest.Clear(); // the playtest build starts over
            NUnit.Framework.Assert.AreEqual(3, demo.Load().bronzeCoins, "a playtest New Game keeps the demo progress");
            NUnit.Framework.Assert.AreEqual("Demo", demo.LoadResume().scenePath, "a playtest New Game keeps the demo resume point");
            NUnit.Framework.Assert.AreEqual(0, playtest.Load().bronzeCoins, "the playtest run starts fresh");
        }
        finally
        {
            foreach (var key in touched) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
        NUnit.Framework.Assert.AreEqual(before, snapshot(), "real saves untouched");
        Debug.Log("PASS save profiles stay apart");
    }

    private static object Run()
    {
        // Resume persistence under store failure (21 Oct). Empty, unsaved Play Mode scene without a GameSession; run through
        // FixtureRunner (Runner.Skip 0). Builds the GameSession under an inactive holder so its key and legacy list are set
        // before its first Awake (Awake loads, and can migrate, the store), verifies that isolation, then swaps the store for
        // in-memory doubles. The real PlayerPrefsProgressStore is also driven over a split cache/durable backend whose flush
        // fails after the cache changed. Synchronous: no frames pass,
        // and the session's autosave tick is pushed out of reach. Checks the write/clear retry contract in GameSession:
        // pending vs stored state, the bounded automatic retry, forced attempts, supersession and focus/death flushing.
        if (!Application.isPlaying || RoadOfTheOldKing.Progression.GameSession.Instance != null)
            throw new Exception("Use an empty Play Mode scene without a GameSession.");
        var results=new List<string>();
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception("FAIL: "+label);results.Add(label);};
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
        var protectedKeys=new[]{"RoadOfTheOldKing.Save","RoadOfTheOldKing.Save.Resume","RoadOfTheOldKing.Tutorial.Save.v1","TheLostShrine.PrototypeLoop.Save.v1"};
        Func<string> snapshot=()=>string.Join("|",protectedKeys.Select(k=>k+"="+(PlayerPrefs.HasKey(k)?PlayerPrefs.GetString(k):"<none>")).ToArray());
        string before=snapshot();
        string isolated="RoadOfTheOldKing.Verification.Resume."+Guid.NewGuid().ToString("N");

        var storeType=typeof(ResumeFailingStore);
        var store=Activator.CreateInstance(storeType);
        Func<string,int> get=name=>(int)storeType.GetField(name).GetValue(store);
        Action<string,int> set=(name,value)=>storeType.GetField(name).SetValue(store,value);
        Func<RoadOfTheOldKing.Progression.ResumePoint> stored=()=>(RoadOfTheOldKing.Progression.ResumePoint)storeType.GetField("stored").GetValue(store);
        const int Forever=1000000;

        GameObject sessionRoot=null, playerRoot=null, holder=null;
        try {
            // Instantiate under an inactive holder: the copy is inactive in the hierarchy, so Awake has not run yet.
            holder=new GameObject("Inactive GameSession construction");holder.SetActive(false);
            sessionRoot=(GameObject)UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Progression/GameSession.prefab"),holder.transform);
            var game=sessionRoot.GetComponent<RoadOfTheOldKing.Progression.GameSession>();
            var type=game.GetType();
            check(!sessionRoot.activeInHierarchy&&type.GetField("store",flags).GetValue(game)==null&&RoadOfTheOldKing.Progression.GameSession.Instance==null,
                "Constructed inactive: no Awake, no store, no live session");
            type.GetField("saveKey",flags).SetValue(game,isolated);
            type.GetField("legacySaveKeys",flags).SetValue(game,new string[0]);
            check(game.StorageKey==isolated&&((string[])type.GetField("legacySaveKeys",flags).GetValue(game)).Length==0&&type.GetField("store",flags).GetValue(game)==null,
                "Isolated key and empty legacy list set before activation");
            sessionRoot.transform.SetParent(null,false); // A root again: activates, and Awake runs once with the isolated key.
            UnityEngine.Object.DestroyImmediate(holder);holder=null;
            var realStore=type.GetField("store",flags).GetValue(game);
            check(RoadOfTheOldKing.Progression.GameSession.Instance==game&&realStore is RoadOfTheOldKing.Progression.PlayerPrefsProgressStore&&
                (string)realStore.GetType().GetField("key",flags).GetValue(realStore)==isolated&&((string[])realStore.GetType().GetField("legacyKeys",flags).GetValue(realStore)).Length==0,
                "Awake built its store on the isolated key with no legacy keys");
            type.GetField("store",flags).SetValue(game,store);
            type.GetField("nextResumeCheck",flags).SetValue(game,float.MaxValue);
            playerRoot=(GameObject)UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
            var player=playerRoot.GetComponent<RoadOfTheOldKing.Player.PlayerHealth>();
            type.GetProperty("Player",flags).SetValue(game,player,null);
            var body=playerRoot.GetComponent<Rigidbody2D>();
            Action<Vector2> move=p=>{playerRoot.transform.position=p;body.position=p;Physics2D.SyncTransforms();};
            var periodic=type.GetMethod("RecordResume",flags,null,new[]{typeof(bool)},null);
            Action tick=()=>periodic.Invoke(game,new object[]{false});
            // Internal session members, reached by reflection (fixtures compile outside Assembly-CSharp).
            Action retry=()=>type.GetMethod("RetryResumePersistence",flags).Invoke(game,null);
            Action clearResume=()=>type.GetMethod("ClearResume",flags).Invoke(game,null);
            Func<bool> writePending=()=>(bool)type.GetProperty("ResumeWritePending",flags).GetValue(game,null);
            Func<bool> clearPending=()=>(bool)type.GetProperty("ResumeClearPending",flags).GetValue(game,null);
            var focus=type.GetMethod("OnApplicationFocus",flags);
            move(new Vector2(3,4));

            // 1. A failed write keeps the in-memory resume for Continue and stays pending.
            set("failSaves",1);game.RecordResume();
            check(game.HasResumePoint&&writePending()&&stored()==null&&get("saveCalls")==1,"Failed write keeps in-memory resume, marked pending, nothing stored");
            // 2. The original defect: an unchanged snapshot skipped all later writes. Now the tick's retry repeats it.
            tick();check(get("saveCalls")==1,"Unchanged periodic snapshot adds no extra write of its own");
            retry();check(!writePending()&&stored()!=null&&stored().x==3&&stored().y==4&&get("saveCalls")==2,"Autosave retry stores the pending resume");
            tick();retry();check(get("saveCalls")==2,"Stored, unchanged resume causes no further writes");

            // 3. Bounded: a persistently failing store gets a fixed number of automatic retries per change.
            set("failSaves",Forever);move(new Vector2(8,4));tick();
            int afterChange=get("saveCalls");
            check(afterChange==3&&writePending(),"A changed snapshot attempts a write once");
            for(int i=0;i<10;i++){tick();retry();}
            check(get("saveCalls")==afterChange+4&&writePending(),"Automatic retries stop after the per-change budget (4)");
            focus.Invoke(game,new object[]{false});
            check(get("saveCalls")==afterChange+6&&writePending(),"Focus loss still forces fresh attempts beyond the budget");
            set("failSaves",0);retry();
            check(!writePending()&&stored().x==8,"Recovered store receives the newest resume via the refilled budget");

            // 4. A failed clear (death/rest/travel) is not forgotten: a later clear tries again.
            set("failClears",1);clearResume();
            check(!game.HasResumePoint&&clearPending()&&stored()!=null&&get("clearCalls")==1,"Failed clear drops Continue's resume but stays pending with the stale record stored");
            clearResume();
            check(!clearPending()&&stored()==null&&get("clearCalls")==2,"A second clear request retries instead of returning early");
            clearResume();check(get("clearCalls")==2,"Nothing pending: a redundant clear does not touch the store");
            // ...and the autosave retry also finishes it, within the same bound.
            game.RecordResume();set("failClears",Forever);clearResume();int clears=get("clearCalls");
            for(int i=0;i<10;i++)retry();
            check(get("clearCalls")==clears+4&&clearPending(),"Clear retries are bounded too");
            set("failClears",0);clearResume();check(!clearPending()&&stored()==null,"Clear succeeds once the store recovers");

            // 5. Supersession: only the newest change is pending.
            game.RecordResume();set("failClears",Forever);clearResume();int saves=get("saveCalls");clears=get("clearCalls");
            move(new Vector2(12,4));game.RecordResume();
            check(!clearPending()&&!writePending()&&stored().x==12,"A new resume supersedes a pending clear by overwriting the stored record");
            for(int i=0;i<5;i++)retry();check(get("clearCalls")==clears,"Superseded clear is never retried");
            set("failClears",0);set("failSaves",Forever);move(new Vector2(16,4));game.RecordResume();
            check(writePending(),"Write pending while the store fails");
            clearResume();saves=get("saveCalls");
            check(!writePending()&&!clearPending()&&stored()==null,"A clear supersedes a pending write");
            for(int i=0;i<5;i++)retry();check(get("saveCalls")==saves,"Superseded write is never retried");
            set("failSaves",0);

            // 5b. A checkpoint (lighting, resting, travel, a scene exit) replaces Continue's spot only once it's saved: a failed
            // save keeps the last good spot (review, 23 Nov).
            Action clearIfSaved=()=>type.GetMethod("ClearResumeIfSaved",flags).Invoke(game,null);
            move(new Vector2(18,4));game.RecordResume();set("failProgressSaves",1);
            type.GetMethod("CaptureAndSave",flags).Invoke(game,new object[]{"Bonfire lit."});clearIfSaved();
            check(game.SaveFailed&&game.HasResumePoint&&stored()!=null&&stored().x==18,"A failed checkpoint save keeps the stored resume");
            type.GetMethod("CaptureAndSave",flags).Invoke(game,new object[]{"Bonfire lit."});clearIfSaved();
            check(!game.SaveFailed&&!game.HasResumePoint&&stored()==null,"A saved checkpoint clears it");

            // 6. Failure AFTER the cache changed: the real PlayerPrefsProgressStore over a split backend, which holds this
            // second key itself, so PlayerPrefs are never touched.
            var splitType=typeof(ResumeSplitPrefs);var split=Activator.CreateInstance(splitType);
            Func<Dictionary<string,string>> durable=()=>(Dictionary<string,string>)splitType.GetField("durable").GetValue(split);
            Func<Dictionary<string,string>> cache=()=>(Dictionary<string,string>)splitType.GetField("cache").GetValue(split);
            Action<int> failFlush=n=>splitType.GetField("failSaves").SetValue(split,n);
            string splitKey=isolated+".Split";string resumeKey=splitKey+".Resume";
            type.GetField("store",flags).SetValue(game,new RoadOfTheOldKing.Progression.PlayerPrefsProgressStore(splitKey,new string[0],(RoadOfTheOldKing.Progression.IPrefsBackend)split));
            move(new Vector2(20,4));game.RecordResume();
            check(!writePending()&&durable().ContainsKey(resumeKey),"Real store writes the resume through to durable storage");
            failFlush(1);clearResume();
            check(clearPending()&&!cache().ContainsKey(resumeKey)&&durable().ContainsKey(resumeKey),"Clear whose flush fails: cached key gone, durable resume remains, clear pending");
            retry();
            check(!clearPending()&&!durable().ContainsKey(resumeKey),"Retry flushes again although the cached key is absent; durable resume removed");
            move(new Vector2(24,4));failFlush(1);game.RecordResume();
            check(writePending()&&cache().ContainsKey(resumeKey)&&!durable().ContainsKey(resumeKey),"Write whose flush fails: cache updated, durable not, write pending");
            retry();
            check(!writePending()&&durable().ContainsKey(resumeKey)&&durable()[resumeKey].Contains("24"),"Retry writes the newest resume durably");
            // A relaunch sees only durable storage. (LoadResume would reject this record only because the unsaved test scene
            // has no path, so the durable JSON is read directly.)
            var reread=JsonUtility.FromJson<RoadOfTheOldKing.Progression.ResumePoint>(durable()[resumeKey]);
            check(reread!=null&&reread.x==24&&reread.y==4,"Durable storage holds the newest resume for a relaunch");
            type.GetField("store",flags).SetValue(game,store);

            // 7. Death: a pending clear is flushed when the window loses focus even though no resume can be recorded.
            game.RecordResume();set("failClears",1);clearResume();
            player.Health.DebugReceiveHit(new RoadOfTheOldKing.Combat.CombatHit(null,RoadOfTheOldKing.Combat.AttackKind.EnemyMelee,9999,Vector2.up,0,0));
            check(!player.IsAlive&&clearPending(),"Dead player with a pending clear");
            int savesBefore=get("saveCalls");focus.Invoke(game,new object[]{false});
            check(!clearPending()&&stored()==null&&get("saveCalls")==savesBefore,"Focus loss after death completes the clear and records nothing new");
        } finally {
            // The killing blow starts the global hit-stop, which waits for real frames; end it as its OnDisable would.
            var hitStop=UnityEngine.Object.FindFirstObjectByType<RoadOfTheOldKing.Combat.HitStop>(FindObjectsInactive.Include);
            if(hitStop!=null){hitStop.StopAllCoroutines();hitStop.GetType().GetMethod("Restore",flags).Invoke(hitStop,null);}
            if(playerRoot!=null)UnityEngine.Object.DestroyImmediate(playerRoot);
            if(sessionRoot!=null)UnityEngine.Object.DestroyImmediate(sessionRoot);
            if(holder!=null)UnityEngine.Object.DestroyImmediate(holder);
        }
        check(snapshot()==before,"Protected save entries unchanged");
        check(!RoadOfTheOldKing.UI.SimulationPause.IsPaused,"No pause lease left behind");
        check(!PlayerPrefs.HasKey(isolated)&&!PlayerPrefs.HasKey(isolated+".Resume")&&!PlayerPrefs.HasKey(isolated+".Split")&&!PlayerPrefs.HasKey(isolated+".Split.Resume"),
            "Isolated and split keys were never written to PlayerPrefs");
        return "PASS "+results.Count+"\n"+string.Join("\n",results.ToArray());
    }
}

// In-memory store double: counts calls and fails the next N saves/clears of the resume record.
public sealed class ResumeFailingStore : RoadOfTheOldKing.Progression.IProgressStore
{
    public int failSaves, failClears, failProgressSaves, saveCalls, clearCalls; public RoadOfTheOldKing.Progression.ResumePoint stored;
    public RoadOfTheOldKing.Progression.ProgressState Load() { return new RoadOfTheOldKing.Progression.ProgressState(); }
    public void Save(RoadOfTheOldKing.Progression.ProgressState s) { if (failProgressSaves > 0) { failProgressSaves--; throw new Exception("save failed"); } }
    public void Clear() { stored = null; }
    public RoadOfTheOldKing.Progression.ResumePoint LoadResume() { return stored; }
    public void SaveResume(RoadOfTheOldKing.Progression.ResumePoint r) { saveCalls++; if (failSaves > 0) { failSaves--; throw new Exception("write failed"); }
        stored = new RoadOfTheOldKing.Progression.ResumePoint { scenePath = r.scenePath, x = r.x, y = r.y, health = r.health, flasks = r.flasks }; }
    public void ClearResume() { clearCalls++; if (failClears > 0) { failClears--; throw new Exception("clear failed"); } stored = null; }
}

// PlayerPrefs-like backend: SetString/DeleteKey change the cache at once; Save() copies it to durable storage and can
// fail after the cache has already changed, as a disk flush might.
public sealed class ResumeSplitPrefs : RoadOfTheOldKing.Progression.IPrefsBackend
{
    public Dictionary<string, string> cache = new Dictionary<string, string>();
    public Dictionary<string, string> durable = new Dictionary<string, string>();
    public int failSaves, saveCalls;
    public bool HasKey(string k) { return cache.ContainsKey(k); }
    public string GetString(string k) { string v; return cache.TryGetValue(k, out v) ? v : ""; }
    public void SetString(string k, string v) { cache[k] = v; }
    public void DeleteKey(string k) { cache.Remove(k); }
    public void Save() { saveCalls++; if (failSaves > 0) { failSaves--; throw new Exception("flush failed"); }
        durable = new Dictionary<string, string>(cache); }
}
#endif
