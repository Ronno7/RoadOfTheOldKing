using TheLostShrine.Progression;
using TheLostShrine.Weapons;
using TheLostShrine.World;
using UnityEngine;

namespace TheLostShrine.Prototype
{
    // Presentation only. The session validates location; progression owns spending and choices.
    public sealed class BonfireUpgradeMenu
    {
        private HatchetUpgrade pending;
        public void Reset() => pending = null;

        public void Draw(Rect area, CheckpointSession session, Bonfire fire, GUIStyle title, GUIStyle text)
        {
            float x = area.x + 18f, y = area.y + 16f, width = area.width - 36f;
            GUI.Label(new Rect(x, y, width, 28f), "HATCHET UPGRADE  |  Sun Shards: " + session.Progress.sunShards, title);
            y += 38f;
            var tier = session.Upgrades.NextTier;
            if (tier == null)
            {
                GUI.Label(new Rect(x, y, width, 38f), "All available tiers chosen.", text);
                foreach (var selected in session.Upgrades.Selected)
                {
                    y += 40f;
                    GUI.Label(new Rect(x, y, width, 50f), selected.displayName + " - " + selected.description, text);
                }
                return;
            }
            GUI.Label(new Rect(x, y, width, 44f), "Cost: " + tier.shardCost + " Sun Shards. Choose one; the other two are lost for this run.", text);
            y += 52f;
            if (pending != null)
            {
                GUI.Label(new Rect(x, y, width, 60f), "Choose " + pending.displayName + "?\n" + pending.description, text);
                GUI.enabled = session.Progress.sunShards >= tier.shardCost;
                if (GUI.Button(new Rect(x, y + 74f, width, 36f), "Spend " + tier.shardCost + " shards - keep " + pending.displayName))
                {
                    session.TryPurchaseUpgrade(fire, pending);
                    pending = null;
                }
                GUI.enabled = true;
                if (GUI.Button(new Rect(x, y + 120f, width, 32f), "Back to choices")) pending = null;
                return;
            }
            foreach (var choice in tier.choices)
            {
                if (choice == null) continue;
                GUI.enabled = session.Progress.sunShards >= tier.shardCost;
                if (GUI.Button(new Rect(x, y, width, 30f), choice.displayName)) pending = choice;
                GUI.enabled = true;
                GUI.Label(new Rect(x, y + 32f, width, 38f), choice.description, text);
                y += 78f;
            }
        }
    }
}
