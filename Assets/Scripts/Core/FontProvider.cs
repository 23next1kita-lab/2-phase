using UnityEngine;

public static class FontProvider
{
    private static Font _font;
    public static Font GetFont()
    {
        if (_font == null)
            _font = Resources.Load<Font>("Fonts/NotoSansJP-VF")
                 ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return _font;
    }
}
