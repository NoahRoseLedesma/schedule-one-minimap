using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Il2CppInterop.Runtime;

namespace MiniMap
{
    public class UIManager
    {
        public GameObject MiniMapHUD { get; private set; }
        public GameObject ContainerObj { get; private set; }
        public GameObject BarObj { get; private set; }
        public RectTransform MapContainer { get; private set; }
        public Image MapImage { get; private set; }
        public Image PlayerBlip { get; private set; }
        public GameObject RotationRoot { get; private set; }
        public GameObject LegendObj { get; private set; }
        private GameObject tooltipObj;
        private Text tooltipText;
        private Image closeBtnIcon;
        private GameObject zoomInBtn;
        private GameObject zoomOutBtn;
        private GameObject compassBtn;
        private GameObject helpBtn;

        private Sprite chevronUp;
        private Sprite chevronDown;

        public void Create(Vector2 miniMapPos, bool isEnabled, Action onToggle, Action onZoomIn, Action onZoomOut, Action onToggleNorthUp, Action onToggleLegend)
        {
            MiniMapHUD = new GameObject("MiniMapHUD");
            UnityEngine.Object.DontDestroyOnLoad(MiniMapHUD);

            Canvas canvas = MiniMapHUD.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            MiniMapHUD.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();

            CreateBar(miniMapPos, isEnabled, onToggle, onZoomIn, onZoomOut, onToggleNorthUp, onToggleLegend);
            CreateContainer(miniMapPos);
            CreateTooltip();
            CreateLegend(onToggleLegend);
        }

        private void EnsureEventSystem()
        {
            if (GameObject.Find("EventSystem") == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private void CreateBar(Vector2 pos, bool isEnabled, Action onToggle, Action onIn, Action onOut, Action onNorth, Action onLegend)
        {
            BarObj = new GameObject("ButtonBar");
            BarObj.transform.SetParent(MiniMapHUD.transform);
            RectTransform barRect = BarObj.AddComponent<RectTransform>();
            barRect.anchorMin = barRect.anchorMax = barRect.pivot = new Vector2(1, 0);
            barRect.sizeDelta = new Vector2(MapConstants.DefaultMapSize, MapConstants.BarHeight);
            barRect.anchoredPosition = new Vector2(pos.x, pos.y + MapConstants.BarOffset);
            BarObj.AddComponent<Image>().color = MapConstants.BarBackground;

            chevronUp = SpriteFactory.CreateChevronIcon(32, false);
            chevronDown = SpriteFactory.CreateChevronIcon(32, true);

            var closeBtn = CreateButton("CloseBtn", isEnabled ? chevronDown : chevronUp, "Toggle Minimap", BarObj.transform, new Vector2(220, 0), onToggle);
            closeBtnIcon = closeBtn.transform.Find("Icon").GetComponent<Image>();

            zoomInBtn = CreateButton("ZoomIn", SpriteFactory.CreatePlusSprite(32), "Zoom In", BarObj.transform, new Vector2(188, 0), onIn);
            zoomOutBtn = CreateButton("ZoomOut", SpriteFactory.CreateMinusSprite(32), "Zoom Out", BarObj.transform, new Vector2(156, 0), onOut);
            compassBtn = CreateButton("Compass", SpriteFactory.CreateCompassSprite(32), "Toggle North Up", BarObj.transform, new Vector2(124, 0), onNorth);
            helpBtn = CreateButton("Help", SpriteFactory.CreateQuestionSprite(32), "Map Legend", BarObj.transform, new Vector2(92, 0), onLegend);

            UpdateHUDState(isEnabled);
        }

        private void CreateContainer(Vector2 pos)
        {
            ContainerObj = new GameObject("Container");
            ContainerObj.transform.SetParent(MiniMapHUD.transform);
            RectTransform contRect = ContainerObj.AddComponent<RectTransform>();
            contRect.anchorMin = contRect.anchorMax = contRect.pivot = new Vector2(1, 0);
            contRect.sizeDelta = new Vector2(MapConstants.DefaultMapSize, MapConstants.DefaultMapSize);
            contRect.anchoredPosition = pos;
            ContainerObj.AddComponent<Image>().color = MapConstants.ContainerBackground;

            GameObject viewport = new("Viewport");
            viewport.transform.SetParent(ContainerObj.transform);
            RectTransform viewRect = viewport.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.sizeDelta = new Vector2(-MapConstants.ViewportPadding, -MapConstants.ViewportPadding);
            viewRect.anchoredPosition = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            RotationRoot = new GameObject("RotationRoot");
            RotationRoot.transform.SetParent(viewport.transform);
            RectTransform rotRect = RotationRoot.AddComponent<RectTransform>();
            rotRect.anchorMin = rotRect.anchorMax = rotRect.pivot = new Vector2(0.5f, 0.5f);
            rotRect.sizeDelta = Vector2.zero;
            rotRect.anchoredPosition = Vector2.zero;

            GameObject mapObj = new("MapImage");
            mapObj.transform.SetParent(RotationRoot.transform);
            MapContainer = mapObj.AddComponent<RectTransform>();
            MapContainer.pivot = new Vector2(0.5f, 0.5f);
            MapImage = mapObj.AddComponent<Image>();
            MapImage.preserveAspect = true;
            MapImage.rectTransform.anchorMin = Vector2.zero;
            MapImage.rectTransform.anchorMax = Vector2.one;
            MapImage.rectTransform.sizeDelta = Vector2.zero;

            CreatePlayerBlip(viewport.transform);
        }

        private void CreatePlayerBlip(Transform parent)
        {
            GameObject blipObj = new("PlayerBlip");
            blipObj.transform.SetParent(parent);
            RectTransform rect = blipObj.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(30, 30);
            rect.anchoredPosition = Vector2.zero;

            PlayerBlip = blipObj.AddComponent<Image>();
            PlayerBlip.sprite = SpriteFactory.CreateBorderedCircle(64, MapConstants.PlayerColor, 0.8f);

            GameObject chevron = new("Chevron");
            chevron.transform.SetParent(blipObj.transform);
            RectTransform chevronRT = chevron.AddComponent<RectTransform>();
            chevronRT.anchoredPosition = new Vector2(0, 30 * MapConstants.ChevronOffset);
            chevronRT.sizeDelta = new Vector2(30, 30);
            chevron.AddComponent<Image>().sprite = SpriteFactory.CreateChevronSprite(64);
        }

        private void CreateTooltip()
        {
            tooltipObj = new GameObject("Tooltip");
            tooltipObj.transform.SetParent(MiniMapHUD.transform);
            tooltipObj.AddComponent<CanvasGroup>().blocksRaycasts = false;
            RectTransform trm = tooltipObj.AddComponent<RectTransform>();
            trm.anchorMin = trm.anchorMax = trm.pivot = new Vector2(0.5f, 0);
            trm.sizeDelta = new Vector2(150, MapConstants.TooltipHeight);
            tooltipObj.AddComponent<Image>().color = MapConstants.TooltipBackground;

            GameObject txtObj = new("Text");
            txtObj.transform.SetParent(tooltipObj.transform);
            tooltipText = txtObj.AddComponent<Text>();
            tooltipText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tooltipText.fontSize = 14;
            tooltipText.color = Color.white;
            tooltipText.alignment = TextAnchor.MiddleCenter;
            tooltipText.horizontalOverflow = HorizontalWrapMode.Overflow;
            tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform txtRT = txtObj.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.pivot = new Vector2(0.5f, 0.5f);
            txtRT.sizeDelta = Vector2.zero;
            txtRT.anchoredPosition = Vector2.zero;

            tooltipObj.SetActive(false);
        }

        private void CreateLegend(Action toggleAction)
        {
            LegendObj = new GameObject("LegendOverlay");
            LegendObj.transform.SetParent(MiniMapHUD.transform);
            LegendObj.transform.SetAsLastSibling();
            RectTransform rt = LegendObj.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280, 360);
            rt.anchoredPosition = Vector2.zero;
            LegendObj.AddComponent<Image>().color = MapConstants.LegendBackground;
            LegendObj.SetActive(false);

            GameObject titleObj = new("Title");
            titleObj.transform.SetParent(LegendObj.transform);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.text = "MiniMap Legend";
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = MapConstants.AccentColor;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = titleRT.anchorMax = titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.sizeDelta = new Vector2(240, 40);
            titleRT.anchoredPosition = new Vector2(0, -5);

            float y = 100;
            AddLegendItem("Customer", SpriteFactory.CreateBorderedCircle(32, MapConstants.UnlockedCustomerColor, 0.8f), ref y);
            AddLegendItem("Potential Customer", SpriteFactory.CreateBorderedCircle(32, MapConstants.PotentialCustomerColor, 0.8f), ref y);
            AddLegendItem("Police / Enemy", SpriteFactory.CreateBorderedCircle(32, MapConstants.PoliceEnemyColor, 0.8f), ref y);
            AddLegendItem("Inactive Customer", SpriteFactory.CreateBorderedCircle(32, MapConstants.InactiveCustomerColor, 0.8f), ref y);
            AddLegendItem("Active Quest / Dead Drop", SpriteFactory.CreateStarIcon(32), ref y);
            AddLegendItem("Pending Deal", SpriteFactory.CreateStarIcon(32, MapConstants.PendingDealColor), ref y);
            AddLegendItem("Other Player", SpriteFactory.CreateBorderedCircle(32, MapConstants.OtherPlayerColor, 0.8f), ref y);

            CreateButton("LegendClose", SpriteFactory.CreateXSprite(32), "", LegendObj.transform, new Vector2(245, 155), toggleAction);
        }

        private void AddLegendItem(string label, Sprite icon, ref float y)
        {
            GameObject item = new(label);
            item.transform.SetParent(LegendObj.transform);
            RectTransform rt = item.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(240, 32);
            rt.anchoredPosition = new Vector2(0, y);

            GameObject iconObj = new("Icon");
            iconObj.transform.SetParent(item.transform);
            RectTransform iconRT = iconObj.AddComponent<RectTransform>();
            iconRT.anchorMin = iconRT.anchorMax = iconRT.pivot = new Vector2(0, 0.5f);
            iconRT.sizeDelta = new Vector2(24, 24);
            iconRT.anchoredPosition = new Vector2(20, 0);
            iconObj.AddComponent<Image>().sprite = icon;

            GameObject textObj = new("Text");
            textObj.transform.SetParent(item.transform);
            Text t = textObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = label;
            t.fontSize = 16;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.pivot = new Vector2(0, 0.5f);
            textRT.offsetMin = new Vector2(60, 0);
            textRT.offsetMax = Vector2.zero;

            y -= 38;
        }

        private GameObject CreateButton(string name, Sprite icon, string tooltip, Transform parent, Vector2 pos, Action onClick)
        {
            GameObject btnObj = new(name);
            btnObj.transform.SetParent(parent);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 0.5f);
            rect.sizeDelta = new Vector2(30, 30);
            rect.anchoredPosition = pos;

            btnObj.AddComponent<Image>().color = MapConstants.ButtonBackground;
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(onClick));

            GameObject iconObj = new("Icon");
            iconObj.transform.SetParent(btnObj.transform);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(18, 18);
            iconRect.anchoredPosition = Vector2.zero;
            iconObj.AddComponent<Image>().sprite = icon;

            if (!string.IsNullOrEmpty(tooltip))
            {
                EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerEnter, () => ShowTooltip(tooltip, rect));
                AddTrigger(trigger, EventTriggerType.PointerExit, HideTooltip);
            }

            return btnObj;
        }

        private void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(DelegateSupport.ConvertDelegate<UnityAction<BaseEventData>>(new Action<BaseEventData>((data) => action())));
            trigger.triggers.Add(entry);
        }

        private void ShowTooltip(string text, RectTransform btnRect)
        {
            if (tooltipObj == null) return;
            tooltipText.text = text;
            tooltipObj.SetActive(true);

            Vector3[] corners = new Vector3[4];
            btnRect.GetWorldCorners(corners);
            tooltipObj.transform.position = new Vector3((corners[0].x + corners[2].x) / 2f, corners[1].y + MapConstants.TooltipOffset, 0);
            tooltipObj.GetComponent<RectTransform>().sizeDelta = new Vector2(tooltipText.preferredWidth + 20, MapConstants.TooltipHeight);
        }

        public void HideTooltip() => tooltipObj?.SetActive(false);

        public void UpdateHUDState(bool isEnabled)
        {
            if (ContainerObj != null) ContainerObj.SetActive(isEnabled);
            if (zoomInBtn != null) zoomInBtn.SetActive(isEnabled);
            if (zoomOutBtn != null) zoomOutBtn.SetActive(isEnabled);
            if (compassBtn != null) compassBtn.SetActive(isEnabled);
            if (helpBtn != null) helpBtn.SetActive(isEnabled);

            if (closeBtnIcon != null) closeBtnIcon.sprite = isEnabled ? chevronDown : chevronUp;
        }

        public void UpdatePositions(Vector2 pos)
        {
            if (ContainerObj != null) ContainerObj.GetComponent<RectTransform>().anchoredPosition = pos;
            if (BarObj != null) BarObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos.x, pos.y + MapConstants.BarOffset);
        }

        public void SetVisible(bool visible) => MiniMapHUD?.SetActive(visible);
    }
}
