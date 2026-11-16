using RoadOfTheOldKing.Player;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.World
{
    // A few discrete props use their fixed ground contact for player-relative depth.
    // Animation height must not move the sorting boundary.
    // On a tilemap (a field of low bushes) the ground line is the base row of the tiles the hero overlaps: in front of
    // it the map draws under him, behind it the map keeps its authored order (user, 15 Nov: the hero was hidden by the
    // forest-edge bushes he stood in front of). A groundAnchor fixes the line instead, for a prefab's overhead half.
    [DisallowMultipleComponent]
    public sealed class WorldPropDepth : MonoBehaviour
    {
        [SerializeField] private Transform groundAnchor;
        private Renderer body;
        private SpriteRenderer playerBody;
        private Tilemap tiles;
        private Transform player;
        private int authoredLayer, authoredOrder;

        // The hero's art is about a tile wide and two tall from the feet; a bush's art reaches two rows up from its base.
        private const float HalfWidth = .5f, Height = 2f, ArtHeight = 2f;

        private void Start()
        {
            body = GetComponent<Renderer>();
            tiles = body is TilemapRenderer ? GetComponent<Tilemap>() : null;
            var movement = FindFirstObjectByType<PlayerMovement>();
            if (body == null || movement == null) { enabled = false; return; }
            authoredLayer = body.sortingLayerID; authoredOrder = body.sortingOrder;
            player = movement.transform;
            playerBody = movement.GetComponentInChildren<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (body == null || player == null || playerBody == null) return;
            Vector2 feet = player.position;
            if (tiles != null && groundAnchor == null) { SortTiles(feet); return; }
            float ground = groundAnchor != null ? groundAnchor.position.y : transform.position.y;
            if (body is TilemapRenderer)
            {
                // Only while the hero overlaps the overhead art; elsewhere it stays as authored.
                var art = body.bounds;
                bool near = feet.x > art.min.x - HalfWidth && feet.x < art.max.x + HalfWidth && feet.y < art.max.y && feet.y + Height > art.min.y;
                Place(!near || feet.y > ground);
                return;
            }
            body.sortingLayerID = playerBody.sortingLayerID;
            body.sortingOrder = playerBody.sortingOrder + (feet.y > ground ? 5 : -3);
        }

        // Behind: the authored order (the map was drawn as overhead). In front: just under the hero.
        private void Place(bool behind)
        {
            body.sortingLayerID = behind ? authoredLayer : playerBody.sortingLayerID;
            body.sortingOrder = behind ? authoredOrder : playerBody.sortingOrder - 3;
        }

        // A base is a tile whose sprite is an object's bottom row (Name_col_0) or has nothing under it; its ground line
        // runs through the middle of that row, where its collision sits. Any base the hero overlaps from behind wins, so
        // a dense wall of stacked bushes still covers him.
        private void SortTiles(Vector2 feet)
        {
            var from = tiles.WorldToCell(new Vector3(feet.x - HalfWidth, feet.y - ArtHeight));
            var to = tiles.WorldToCell(new Vector3(feet.x + HalfWidth, feet.y + Height));
            bool any = false, behind = false;
            for (int x = from.x; x <= to.x && !behind; x++)
                for (int y = from.y; y <= to.y; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    var sprite = tiles.GetSprite(cell);
                    if (sprite == null || !(sprite.name.EndsWith("_0") || !tiles.HasTile(cell + Vector3Int.down))) continue;
                    float bottom = tiles.CellToWorld(cell).y;
                    // Only art that overlaps the hero decides; a bush he has already passed doesn't.
                    if (bottom + ArtHeight <= feet.y || bottom >= feet.y + Height) continue;
                    float ground = bottom + tiles.cellSize.y * .5f;
                    any = true;
                    if (feet.y > ground) { behind = true; break; }
                }
            // Away from every bush the map keeps its authored order, for enemies and everything else around it.
            Place(behind || !any);
        }
    }
}
