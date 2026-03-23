using UnityEngine;

namespace MiniMap
{
    public static class MapConstants
    {
        // Map dimensions and zoom
        public const float DefaultMapSize = 250f;
        public const float ViewportPadding = 8f;
        public const float BlipSize = 24f;
        public const float DefaultZoom = 0.35f;
        public const float MinZoom = 0.1f;
        public const float MaxZoom = 5.0f;
        public const float ZoomStep = 0.1f;
        public const float ScrollZoomFactor = 0.1f;
        public const float KeyboardZoomFactor = 0.5f;

        // UI Positioning
        public static readonly Vector2 DefaultPosition = new(-14, 14);
        public const float BarHeight = 30f;
        public const float BarOffset = 252f;
        public const float TooltipOffset = 10f;
        public const float TooltipHeight = 30f;

        // Visual constants
        public const float BaseScale = 0.40f;
        public const float ZoomFactor = 0.20f;
        public const float ChevronOffset = 0.75f;
        public const float OffscreenScaleMultiplier = 0.8f;

        // Colors
        public static readonly Color BarBackground = new(0, 0, 0, 0.7f);
        public static readonly Color ContainerBackground = new(0, 0, 0, 0.6f);
        public static readonly Color TooltipBackground = new(0.1f, 0.1f, 0.1f, 0.95f);
        public static readonly Color LegendBackground = new(0.1f, 0.1f, 0.1f, 1.0f);
        public static readonly Color ButtonBackground = new(0.15f, 0.15f, 0.15f, 0.9f);
        public static readonly Color AccentColor = new(0.35f, 0.7f, 1.0f);

        // Blip Colors
        public static readonly Color PlayerColor = new(0.35f, 0.7f, 1.0f, 1.0f);
        public static readonly Color UnlockedCustomerColor = Color.green;
        public static readonly Color PotentialCustomerColor = new(0.6f, 0.1f, 0.8f);
        public static readonly Color PoliceEnemyColor = Color.red;
        public static readonly Color InactiveCustomerColor = new(0.5f, 0.5f, 0.5f);
        public static readonly Color QuestColor = new(0.1f, 0.4f, 1.0f);
        public static readonly Color PendingDealColor = new(1.0f, 0.0f, 1.0f);
        public static readonly Color OtherPlayerColor = new(1.0f, 0.8f, 0.2f);

        // Update Intervals
        public const float RadarUpdateInterval = 0.5f;
    }
}
