using UnityEngine;

namespace MiniMap
{
    public static class SpriteFactory
    {
        private static Texture2D CreateTexture(int size)
        {
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);
            return tex;
        }

        public static Sprite CreatePlusSprite(int size)
        {
            Texture2D tex = CreateTexture(size);
            int mid = size / 2;
            int thick = Mathf.Max(1, size / 16);
            for (int i = -thick; i <= thick; i++)
            {
                for (int j = mid - size / 3; j <= mid + size / 3; j++)
                {
                    tex.SetPixel(j, mid + i, Color.white);
                    tex.SetPixel(mid + i, j, Color.white);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public static Sprite CreateMinusSprite(int size)
        {
            Texture2D tex = CreateTexture(size);
            int mid = size / 2;
            int thick = Mathf.Max(1, size / 16);
            for (int i = -thick; i <= thick; i++)
            {
                for (int j = mid - size / 3; j <= mid + size / 3; j++)
                {
                    tex.SetPixel(j, mid + i, Color.white);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public static Sprite CreateQuestionSprite(int size)
        {
            Texture2D tex = CreateTexture(size);
            int mid = size / 2;
            int r = size / 2 - 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(mid, mid));
                    if (d < r && d > r - 2) tex.SetPixel(x, y, Color.white);
                }
            }
            // Draw '?'
            for (int i = -4; i <= 4; i++) for (int j = 3; j <= 6; j++) { if (j == 6 || (Mathf.Abs(i) == 4 && j > 3) || (i == 4 && j == 3)) tex.SetPixel(mid + i, mid + j, Color.white); }
            for (int j = 0; j <= 2; j++) tex.SetPixel(mid + 4, mid + j, Color.white);
            for (int x = 0; x <= 4; x++) tex.SetPixel(mid + x, mid, Color.white);
            for (int y = -3; y <= -1; y++) tex.SetPixel(mid, mid + y, Color.white);
            for (int i = -1; i <= 1; i++) for (int j = -6; j <= -5; j++) tex.SetPixel(mid + i, mid + j, Color.white);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public static Sprite CreateChevronIcon(int size, bool down)
        {
            Texture2D tex = CreateTexture(size);
            int mid = size / 2;
            int thick = Mathf.Max(1, size / 16);

            for (int i = 0; i < size / 3; i++)
            {
                for (int t = -thick; t <= thick; t++)
                {
                    int y = down ? (mid - size / 6 + i) : (mid + size / 6 - i);
                    tex.SetPixel(mid + i + t, y, Color.white);
                    tex.SetPixel(mid - i + t, y, Color.white);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public static Sprite CreateXSprite(int size)
        {
            Texture2D tex = CreateTexture(size);
            int thick = Mathf.Max(1, size / 16);
            int margin = size / 4;
            for (int i = margin; i < size - margin; i++)
            {
                for (int t = -thick; t <= thick; t++)
                {
                    int x1 = Mathf.Clamp(i + t, 0, size - 1);
                    int x2 = Mathf.Clamp(size - 1 - i + t, 0, size - 1);
                    tex.SetPixel(x1, i, Color.white);
                    tex.SetPixel(x2, i, Color.white);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public static Sprite CreateCompassSprite(int size)
        {
            Texture2D tex = CreateTexture(size);
            int mid = size / 2;
            int thick = Mathf.Max(1, size / 12);
            for (int i = 0; i < size / 2 - 2; i++)
            {
                int w = (int)Mathf.Lerp(thick, 0, (float)i / (size / 2 - 2));
                for (int j = -w; j <= w; j++)
                {
                    tex.SetPixel(mid + j, mid + i, Color.red);
                    tex.SetPixel(mid + j, mid - i, Color.white);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public static Sprite CreateBorderedCircle(int size, Color fill, float borderThickness)
        {
            Texture2D tex = CreateTexture(size);
            float center = size / 2f;
            float outerRad = size / 2f - 1f;
            float innerRad = outerRad * borderThickness;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float outerAlpha = Mathf.Clamp01(outerRad - dist + 0.5f);
                    float innerAlpha = Mathf.Clamp01(innerRad - dist + 0.5f);

                    if (outerAlpha <= 0) continue;

                    Color pixelColor = Color.Lerp(Color.white, fill, innerAlpha);
                    pixelColor.a = outerAlpha;
                    tex.SetPixel(x, y, pixelColor);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public static Sprite CreateTriangleSprite(int size, Color fill)
        {
            Texture2D tex = CreateTexture(size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / (size - 1); float ny = (float)y / (size - 1);
                    float dx = Mathf.Abs(nx - 0.5f);
                    float distToTip = (1.0f - dx * 1.8f) - ny;
                    float distToBase = ny - 0.1f;
                    float edgeSmoothing = 0.08f;
                    float alphaTip = Mathf.Clamp01(distToTip / edgeSmoothing + 0.5f);
                    float alphaBase = Mathf.Clamp01(distToBase / edgeSmoothing + 0.5f);
                    float totalAlpha = Mathf.Min(alphaTip, alphaBase);

                    if (totalAlpha <= 0) continue;

                    float borderTip = (0.92f - dx * 1.8f) - ny;
                    float borderBase = ny - 0.18f;
                    float fillAlpha = Mathf.Clamp01(Mathf.Min(borderTip, borderBase) / edgeSmoothing + 0.5f);

                    Color pixelColor = Color.Lerp(Color.white, fill, fillAlpha);
                    pixelColor.a = totalAlpha;
                    tex.SetPixel(x, y, pixelColor);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public static Sprite CreateStarIcon(int size, Color? backgroundColor = null)
        {
            Texture2D tex = CreateTexture(size);
            float center = size / 2f;
            float outerRad = size / 2f - 1f;
            float innerRad = outerRad * 0.85f;
            Color questBlue = backgroundColor ?? MapConstants.QuestColor;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float outerAlpha = Mathf.Clamp01(outerRad - dist + 0.5f);
                    if (outerAlpha <= 0) continue;

                    float innerAlpha = Mathf.Clamp01(innerRad - dist + 0.5f);
                    float nx = ((float)x / size) * 2 - 1;
                    float ny = ((float)y / size) * 2 - 1;
                    float angle = Mathf.Atan2(ny, nx);

                    float starR = 0.7f + 0.3f * Mathf.Cos(angle * 5f);
                    bool isStar = Vector2.SqrMagnitude(new Vector2(nx, ny)) < starR * starR * 0.35f;

                    Color c = Color.white;
                    if (innerAlpha > 0 && !isStar) c = Color.Lerp(Color.white, questBlue, innerAlpha);
                    c.a = outerAlpha;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public static Sprite CreateChevronSprite(int size)
        {
            Texture2D tex = CreateTexture(size);
            float p = 0.15f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / (size - 1); float ny = (float)y / (size - 1);
                    if (nx < p || nx > 1 - p || ny < p || ny > 1 - p) continue;
                    float pnx = (nx - p) / (1 - 2 * p); float pny = (ny - p) / (1 - 2 * p);
                    float dx = Mathf.Abs(pnx - 0.5f);
                    float distToTop = (1.0f - dx * 0.85f) - pny;
                    float distToBottom = pny - (0.6f - dx * 0.85f);
                    float alpha = Mathf.Clamp01(Mathf.Min(distToTop, distToBottom) / 0.15f + 0.5f);

                    if (alpha > 0)
                    {
                        Color c = Color.white;
                        c.a = alpha;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
