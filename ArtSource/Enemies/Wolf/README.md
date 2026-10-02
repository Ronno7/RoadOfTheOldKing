# Wolf source art

Original wolf artwork generated with the built-in imagegen tool from the project wolf concept and the user's anatomy reference. Side.png contains eight poses in a 4x2 grid; Directions.png contains six south poses followed by six north poses in a 3x4 grid. Prompts are retained in Provenance.json.

The retired production output was Assets/Art/Sprites/Enemies/Wolf.png (deleted 1 Oct; in git history): twenty 48x48 sprites, 24 PPU, pivot (24,7.5), seven opaque colors plus transparency. The 48x48 revision redraws the north/south poses with an elevated view and separated fore/hind paws; 24 PPU preserves the original world scale. West mirrors east. Side order: idle, four trot steps, crouch, bite, dead. South/north order: idle, two steps, crouch, bite, dead.

Unity's WolfAssets exporter extracts the principal opaque animal in each source cell, rejects matte/adjacent-row fragments, applies one scale per source sheet, aligns ground contact, quantizes to the fixed palette and retains authored eye accents as native pixels. Native sprite IDs remain stable on rebuild. Sources are larger drawings; only the exported native sheet is used by the game.

The Wolf prefab and WolfAnimationSet provide production presentation. SimpleMeleeEnemy owns movement and damage; WolfView samples state/progress and actual travel. MeleeSentinel remains the prototype presentation.

`Idle/` and `metadata.json` hold the PixelLab export the game now uses (since 1 Oct): an eight-direction wolf at 16 pixels per unit with Idle, Fast_Walk, Run and Bark animations (Bark serves as the bite). The drawings and 20-sprite sheet described above are the retired previous wolf.
