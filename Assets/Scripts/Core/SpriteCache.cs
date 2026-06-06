using UnityEngine;

public static class SpriteCache
{
    public static Sprite TwoPhaseBlue { get; private set; }
    public static Sprite TwoPhaseRed { get; private set; }
    public static Sprite OnePhaseBlue { get; private set; }
    public static Sprite OnePhaseRed { get; private set; }
    public static Sprite Arrow { get; private set; }
    public static Sprite Background { get; private set; }
    public static bool Loaded { get; private set; }

    public static void Load()
    {
        var sprites = Resources.LoadAll<Sprite>("Design/2-phase blue");
        TwoPhaseBlue = sprites != null && sprites.Length > 0 ? sprites[0] : null;

        sprites = Resources.LoadAll<Sprite>("Design/2-phase red");
        TwoPhaseRed = sprites != null && sprites.Length > 0 ? sprites[0] : null;

        sprites = Resources.LoadAll<Sprite>("Design/1-phase blue");
        OnePhaseBlue = sprites != null && sprites.Length > 0 ? sprites[0] : null;

        sprites = Resources.LoadAll<Sprite>("Design/1-phase red");
        OnePhaseRed = sprites != null && sprites.Length > 0 ? sprites[0] : null;

        Arrow = CreateArrowSprite();
        sprites = Resources.LoadAll<Sprite>("Design/2-phase background");
        Background = sprites != null && sprites.Length > 0 ? sprites[0] : null;

        Loaded = TwoPhaseBlue != null;
    }

    private static Sprite CreateArrowSprite()
    {
        var sprites = Resources.LoadAll<Sprite>("Design/arrow");
        if (sprites == null || sprites.Length == 0) return null;
        var src = sprites[0];
        var tex = src.texture;
        var rect = src.rect;
        return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }
}
