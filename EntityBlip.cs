using UnityEngine;
using UnityEngine.UI;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Quests;
using Il2CppScheduleOne.Economy;

namespace MiniMap
{
    public enum BlipType
    {
        None,
        UnlockedCustomer,
        PotentialCustomer,
        Police,
        Enemy,
        Quest,
        DeadDrop,
        PendingDeal,
        CustomerInactive,
        OtherPlayer
    }

    /// <summary>
    /// Represents a single blip on the minimap tracking a specific entity.
    /// Handles visual updates, positioning, and clamping to map bounds.
    /// </summary>
    public class EntityBlip
    {
        public object Entity { get; }
        public GameObject BlipObj { get; private set; }
        public RectTransform Rect { get; private set; }
        public BlipType Type { get; set; }
        public Image Image { get; private set; }
        public GameObject ChevronObj { get; private set; }
        public float ScaleMultiplier { get; private set; } = 1.0f;
        public bool IsInside { get; private set; } = false;
        public bool IsOffscreen { get; private set; } = false;

        public EntityBlip(object entity, BlipType type, Transform parent)
        {
            Entity = entity;
            Type = type;
            BlipObj = new GameObject("EntityBlip_" + type);
            BlipObj.transform.SetParent(parent);
            Rect = BlipObj.AddComponent<RectTransform>();
            Rect.anchorMin = Rect.anchorMax = Rect.pivot = new Vector2(0.5f, 0.5f);
            Rect.sizeDelta = new Vector2(MapConstants.BlipSize, MapConstants.BlipSize);

            Image = BlipObj.AddComponent<Image>();
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            ScaleMultiplier = 1.0f;
            NPC npc = Entity as NPC ?? (Entity as Customer)?.NPC;
            IsInside = npc != null && npc.isInBuilding;

            switch (Type)
            {
                case BlipType.UnlockedCustomer:
                    Image.sprite = SpriteFactory.CreateBorderedCircle(32, MapConstants.UnlockedCustomerColor, 0.8f);
                    break;
                case BlipType.PotentialCustomer:
                    if (IsOffscreen)
                    {
                        Image.sprite = SpriteFactory.CreateTriangleSprite(32, MapConstants.PotentialCustomerColor);
                        ScaleMultiplier = 1.6f;
                    }
                    else Image.sprite = SpriteFactory.CreateBorderedCircle(32, MapConstants.PotentialCustomerColor, 0.8f);
                    break;
                case BlipType.Police:
                case BlipType.Enemy:
                    Image.sprite = SpriteFactory.CreateBorderedCircle(32, MapConstants.PoliceEnemyColor, 0.8f);
                    break;
                case BlipType.CustomerInactive:
                    Image.sprite = SpriteFactory.CreateBorderedCircle(32, MapConstants.InactiveCustomerColor, 0.8f);
                    break;
                case BlipType.Quest:
                    Image.sprite = SpriteFactory.CreateStarIcon(32);
                    ScaleMultiplier = 1.6f;
                    break;
                case BlipType.DeadDrop:
                    Image.sprite = SpriteFactory.CreateStarIcon(32, MapConstants.QuestColor);
                    ScaleMultiplier = 1.6f;
                    break;
                case BlipType.PendingDeal:
                    Image.sprite = SpriteFactory.CreateStarIcon(32, MapConstants.PendingDealColor);
                    ScaleMultiplier = 1.6f;
                    break;
                case BlipType.OtherPlayer:
                    Image.sprite = SpriteFactory.CreateBorderedCircle(32, MapConstants.OtherPlayerColor, 0.8f);
                    EnsureChevron();
                    break;
            }

            if (ChevronObj != null) ChevronObj.SetActive(Type == BlipType.OtherPlayer);
        }

        private void EnsureChevron()
        {
            if (ChevronObj != null) return;
            ChevronObj = new GameObject("Chevron");
            ChevronObj.transform.SetParent(BlipObj.transform);
            RectTransform arrowRT = ChevronObj.AddComponent<RectTransform>();
            arrowRT.anchoredPosition = new Vector2(0, MapConstants.BlipSize * MapConstants.ChevronOffset);
            arrowRT.sizeDelta = new Vector2(MapConstants.BlipSize, MapConstants.BlipSize);
            ChevronObj.AddComponent<Image>().sprite = SpriteFactory.CreateChevronSprite(32);
        }

        public void Update(MapPositionUtility util, Vector2 playerMapPos, float zoom, float adaptiveScale, float mapRot)
        {
            if (!TryGetWorldPosition(out Vector3 worldPos)) return;

            // Sync indoor status
            NPC npc = Entity as NPC ?? (Entity as Customer)?.NPC;
            bool currentlyInside = npc != null && npc.isInBuilding;
            if (currentlyInside != IsInside)
            {
                IsInside = currentlyInside;
                UpdateVisuals();
            }

            Vector2 entityMapPos = util.GetMapPosition(worldPos);
            Vector2 mapRelativePos = (entityMapPos - playerMapPos) * zoom;
            Vector2 screenPos = MathUtility.Rotate(mapRelativePos, mapRot);

            float hSize = (MapConstants.DefaultMapSize - MapConstants.ViewportPadding) / 2f;
            float paddingScale = adaptiveScale * ScaleMultiplier;
            if (IsOffscreen) paddingScale *= MapConstants.OffscreenScaleMultiplier;

            float iconPadding = (MapConstants.BlipSize * paddingScale) / 2f;
            float maxBound = hSize - iconPadding;

            bool currentlyOffscreen = Mathf.Abs(screenPos.x) > maxBound || Mathf.Abs(screenPos.y) > maxBound;

            if (currentlyOffscreen != IsOffscreen)
            {
                IsOffscreen = currentlyOffscreen;
                UpdateVisuals();
            }

            if (IsOffscreen)
            {
                if (!IsTrackableType())
                {
                    BlipObj.SetActive(false);
                    return;
                }
                float f = maxBound / Mathf.Max(Mathf.Abs(screenPos.x), Mathf.Abs(screenPos.y));
                screenPos *= f;
            }

            Vector2 relativePos = MathUtility.Rotate(screenPos, -mapRot);
            BlipObj.SetActive(true);
            Rect.anchoredPosition = relativePos;

            UpdateRotation(mapRelativePos, mapRot);

            float finalScale = adaptiveScale * ScaleMultiplier;
            if (IsOffscreen) finalScale *= MapConstants.OffscreenScaleMultiplier;
            Rect.localScale = new Vector3(finalScale, finalScale, 1);
        }

        private bool TryGetWorldPosition(out Vector3 worldPos)
        {
            worldPos = Vector3.zero;
            if (Entity == null || (Entity is UnityEngine.Object obj && obj == null)) return false;

            if (Entity is NPC n) worldPos = n.transform.position;
            else if (Entity is Customer c && c.NPC != null) worldPos = c.NPC.transform.position;
            else if (Entity is Player p)
            {
                bool inVeh = p.IsInVehicle && p.CurrentVehicle != null;
                worldPos = inVeh ? p.CurrentVehicle.transform.position : p.transform.position;
            }
            else if (Entity is POI poi) worldPos = poi.transform.position;
            else if (Entity is Contract contract && contract.DeliveryLocation?.CustomerStandPoint != null)
                worldPos = contract.DeliveryLocation.CustomerStandPoint.position;
            else return false;

            return true;
        }

        private bool IsTrackableType()
        {
            return Type == BlipType.Quest || Type == BlipType.DeadDrop ||
                   Type == BlipType.PendingDeal || Type == BlipType.PotentialCustomer;
        }

        private void UpdateRotation(Vector2 mapRelativePos, float mapRot)
        {
            if (IsOffscreen && Type == BlipType.PotentialCustomer)
            {
                float targetAngle = Mathf.Atan2(mapRelativePos.y, mapRelativePos.x) * Mathf.Rad2Deg - 90f;
                Rect.localEulerAngles = new Vector3(0, 0, targetAngle);
            }
            else if (Type == BlipType.OtherPlayer && Entity is Player otherP)
            {
                bool inVeh = otherP.IsInVehicle && otherP.CurrentVehicle != null;
                Vector3 otherFwd = inVeh ? otherP.CurrentVehicle.transform.forward : otherP.transform.forward;
                float otherHeading = Vector3.SignedAngle(Vector3.forward, otherFwd, Vector3.up);
                Rect.localEulerAngles = new Vector3(0, 0, -otherHeading);
            }
            else
            {
                Rect.localEulerAngles = new Vector3(0, 0, -mapRot);
            }
        }

        public void Destroy() => UnityEngine.Object.Destroy(BlipObj);
    }
}
