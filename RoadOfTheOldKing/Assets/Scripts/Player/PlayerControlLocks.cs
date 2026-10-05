using System.Collections.Generic;
using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    // Lease-style gameplay input lock: menus, sequences and transitions lock by owner and unlock only
    // their own lease; input reads as inactive while any owner holds one. Owners never toggle input
    // components on and off, so overlapping locks cannot release each other. (Death still disables the
    // input components outright; that is permanent until the reload.)
    [DisallowMultipleComponent]
    public sealed class PlayerControlLocks : MonoBehaviour
    {
        private readonly HashSet<object> owners = new HashSet<object>();

        public bool IsLocked => owners.Count > 0;
        public bool IsLockedBy(object owner) => owner != null && owners.Contains(owner);
        public void Lock(object owner) { if (owner != null) owners.Add(owner); }
        public void Unlock(object owner) { if (owner != null) owners.Remove(owner); }
    }
}
