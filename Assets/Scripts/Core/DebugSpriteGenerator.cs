using UnityEngine;

public static class DebugSpriteGenerator
{
    public static Sprite CreateRectangleSprite(int width, int height, Color fill, Color border, float ppu = 32f)
    {
        Texture2D tex = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % width;
            int y = i / width;
            if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                pixels[i] = border;
            else
                pixels[i] = fill;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu);
    }

    public static Sprite CreateCircleSprite(int size, Color color, float ppu = 32f)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        int cx = size / 2, cy = size / 2;
        int r = size / 2 - 1;
        int r2 = r * r;
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % size;
            int y = i / size;
            int dx = x - cx, dy = y - cy;
            pixels[i] = (dx * dx + dy * dy <= r2) ? color : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
    }

    public static Sprite CreateDiamondSprite(int size, Color color, float ppu = 32f)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        int half = size / 2;
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % size;
            int y = i / size;
            int dx = Mathf.Abs(x - half);
            int dy = Mathf.Abs(y - half);
            pixels[i] = (dx + dy <= half - 1) ? color : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
    }

    public static Sprite CreateSquareSprite(int size, Color color, float ppu = 32f)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        int half = size / 2;
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % size;
            int y = i / size;
            int dx = Mathf.Abs(x - half);
            int dy = Mathf.Abs(y - half);
            pixels[i] = (dx <= half - 1 && dy <= half - 1) ? color : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
    }

    public static Sprite CreateTriangleSprite(int size, Color color, float ppu = 32f)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        int half = size / 2;
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % size;
            int y = i / size;
            int dx = Mathf.Abs(x - half);
            int fromTip = size - 1 - y;
            pixels[i] = (dx <= fromTip) ? color : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.0f), ppu);
    }

    public static Sprite CreateArrowSprite(int size, Color color, float ppu = 32f)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        int half = size / 2;
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % size;
            int y = i / size;
            int dx = Mathf.Abs(x - half);
            int fromBottom = y;
            int fromTop = size - 1 - y;
            bool isArrow = false;
            if (fromTop <= 1 && dx <= fromTop)
                isArrow = true;
            else if (fromBottom > 0 && dx <= 1)
                isArrow = true;
            pixels[i] = isArrow ? color : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.0f), ppu);
    }
}
