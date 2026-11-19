#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

// Shared plumbing for the checks ported from the Tools/Verification snippets. Each returns "PASS <count>" and one line
// per passed assertion, and either throws or writes a "FAIL" line when something breaks.
public static class CheckRun
{
    // The test runner plays every test in one Play session. Let a hit-stop or a menu left by the previous test run
    // out, so one check can't fail the next in the same frame (the old one-check-per-session rule).
    public static IEnumerator Settle()
    {
        yield return new WaitForSecondsRealtime(.25f);
        Assert.IsFalse(RoadOfTheOldKing.UI.SimulationPause.IsPaused, "the previous test left the game paused");
        Time.timeScale = 1f;
    }

    public static void Expect(object result)
    {
        string text = result as string;
        if (text == null && result is IEnumerable lines) text = string.Join("\n", lines.Cast<object>().Select(x => x.ToString()));
        Assert.IsNotNull(text, "the check returned nothing");
        var split = text.Split('\n');
        var failed = split.Where(l => l.StartsWith("FAIL")).ToArray();
        Assert.IsEmpty(failed, string.Join("\n", failed));
        Assert.IsTrue(split.Any(l => l.StartsWith("PASS")), text);
        Debug.Log(split[0]);
    }
}
#endif
