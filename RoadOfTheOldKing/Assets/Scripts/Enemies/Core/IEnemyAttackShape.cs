using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // What the current attack covers, for its drawing on the ground (EnemyAttackGround). Distances are world units.
    public interface IEnemyAttackShape
    {
        bool IsWindingUp { get; }
        bool IsStriking { get; }
        float WindupProgress { get; }
        // Tracking has ended: the cue to dodge.
        bool IsAttackLocked { get; }
        // A draw pulls the player in, with streaks racing across the floor.
        bool IsDrawing { get; }
        Vector2 AttackOrigin { get; }
        Vector2 AttackDirection { get; }
        // Above zero, the attack runs along a lane; otherwise it's a ring or wedge around the origin.
        float DrawnLaneLength { get; }
        float DrawnLaneWidth { get; }
        // How far along the lane the enemy itself ends up; the lane runs on by its reach.
        float DrawnLaneTravel { get; }
        float AttackReach { get; }
        float AttackArc { get; }
        float StrikeSeconds { get; }
    }
}
