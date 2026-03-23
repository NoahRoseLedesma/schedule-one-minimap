using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.UI.Phone.Map;
using Il2CppScheduleOne.DevUtilities;

[assembly: MelonInfo(typeof(MiniMap.Core), "MiniMap", "1.0.0", "N")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace MiniMap
{
    /// <summary>
    /// Core entry point for the MiniMap mod.
    /// Manages lifecycle, input handling, and coordinates between UI and Radar systems.
    /// </summary>
    public class Core : MelonMod
    {
        private UIManager ui;
        private RadarController radar;

        // State
        private float currentMapZoom = MapConstants.DefaultZoom;
        private bool isEnabled = true;
        private bool isNorthUp = true;
        private Vector2 miniMapPos = MapConstants.DefaultPosition;

        // Dragging state
        private bool isDragging = false;
        private Vector2 dragStartMousePos;
        private Vector2 dragStartUIPos;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
            ui = new UIManager();
        }

        public override void OnUpdate()
        {
            if (!CheckGameReady()) return;

            HandleInputs();
            HandleDragging();

            if (!isEnabled) return;

            UpdateSystems();
        }

        private bool CheckGameReady()
        {
            if (Player.Local == null || !MapPositionUtility.InstanceExists || !MapApp.InstanceExists)
            {
                if (ui.MiniMapHUD != null && ui.MiniMapHUD.activeSelf) ui.SetVisible(false);
                return false;
            }

            if (ui.MiniMapHUD == null || ui.ContainerObj == null) InitializeComponents();
            if (!ui.MiniMapHUD.activeSelf) ui.SetVisible(true);

            UpdateBarVisibility();
            return true;
        }

        private void InitializeComponents()
        {
            ui.Create(miniMapPos, isEnabled,
                onToggle: ToggleHUD,
                onZoomIn: () => ModifyZoom(MapConstants.ZoomStep),
                onZoomOut: () => ModifyZoom(-MapConstants.ZoomStep),
                onToggleNorthUp: () => isNorthUp = !isNorthUp,
                onToggleLegend: ToggleLegend
            );
            radar = new RadarController(ui.RotationRoot.transform);
        }

        private void UpdateBarVisibility()
        {
            if (ui.BarObj == null) return;

            bool showBar = Cursor.visible;
            if (ui.BarObj.activeSelf != showBar) ui.BarObj.SetActive(showBar);

            if (showBar && ui.ContainerObj != null && ui.ContainerObj.activeSelf != isEnabled) ui.UpdateHUDState(isEnabled);
        }

        private void HandleInputs()
        {
            if (Input.GetKeyDown(KeyCode.F10)) ToggleHUD();

            // Handle HUD container state
            if (ui.ContainerObj != null && ui.ContainerObj.activeSelf != isEnabled) ui.UpdateHUDState(isEnabled);

            // Scroll Zoom
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && IsMouseOverMiniMap())
            {
                ModifyZoom(scroll * MapConstants.ScrollZoomFactor);
            }

            // Keyboard Zoom
            if (Input.GetKey(KeyCode.PageUp)) ModifyZoom(Time.deltaTime * MapConstants.KeyboardZoomFactor);
            if (Input.GetKey(KeyCode.PageDown)) ModifyZoom(-Time.deltaTime * MapConstants.KeyboardZoomFactor);
        }

        private void HandleDragging()
        {
            if (!Cursor.visible) return;

            if (Input.GetMouseButtonDown(0) && IsMouseOverBar() && !IsMouseOverAnyButton())
            {
                isDragging = true;
                dragStartMousePos = Input.mousePosition;
                dragStartUIPos = miniMapPos;
            }

            if (isDragging)
            {
                if (Input.GetMouseButton(0))
                {
                    Vector2 delta = (Vector2)Input.mousePosition - dragStartMousePos;
                    miniMapPos = dragStartUIPos + delta;
                    ui.UpdatePositions(miniMapPos);
                }
                else
                {
                    isDragging = false;
                }
            }
        }

        private void UpdateSystems()
        {
            var util = MapPositionUtility.Instance;
            UpdateMapSprite(util);

            // Get player state
            var p = Player.Local;
            bool inVehicle = p.IsInVehicle && p.CurrentVehicle != null;
            Vector3 pos = inVehicle ? p.CurrentVehicle.transform.position : p.transform.position;
            Vector3 fwd = inVehicle ? p.CurrentVehicle.transform.forward : p.transform.forward;

            Vector2 playerMapPos = util.GetMapPosition(pos);
            float heading = Vector3.SignedAngle(Vector3.forward, fwd, Vector3.up);
            float mapRot = isNorthUp ? 0 : heading;

            // Update UI Transforms
            ui.RotationRoot.transform.localEulerAngles = new Vector3(0, 0, mapRot);
            ui.MapContainer.localScale = new Vector3(currentMapZoom, currentMapZoom, 1);
            ui.MapContainer.anchoredPosition = -playerMapPos * currentMapZoom;

            float adaptiveScale = MapConstants.BaseScale + (currentMapZoom * MapConstants.ZoomFactor);
            ui.PlayerBlip.rectTransform.localScale = new Vector3(adaptiveScale, adaptiveScale, 1);
            ui.PlayerBlip.rectTransform.localEulerAngles = new Vector3(0, 0, isNorthUp ? -heading : 0);
            ui.PlayerBlip.rectTransform.SetAsLastSibling();

            // Update Radar
            radar.Update(Time.deltaTime);
            radar.UpdateBlips(util, playerMapPos, currentMapZoom, adaptiveScale, mapRot);
        }

        private void UpdateMapSprite(MapPositionUtility util)
        {
            if (!MapApp.InstanceExists) return;

            Sprite currentMap = GameManager.IS_TUTORIAL ? MapApp.Instance.TutorialMapSprite : MapApp.Instance.MainMapSprite;
            if (ui.MapImage.sprite != currentMap)
            {
                ui.MapImage.sprite = currentMap;
                ui.MapContainer.sizeDelta = new Vector2(util.MapDimensions, util.MapDimensions);
            }
        }

        private void ModifyZoom(float delta)
        {
            currentMapZoom = Mathf.Clamp(currentMapZoom + delta, MapConstants.MinZoom, MapConstants.MaxZoom);
        }

        private void ToggleHUD() => isEnabled = !isEnabled;

        private void ToggleLegend()
        {
            if (ui.LegendObj != null)
            {
                bool nextState = !ui.LegendObj.activeSelf;
                ui.LegendObj.SetActive(nextState);
                if (!nextState) ui.HideTooltip();
            }
        }

        #region UI Interaction Helpers
        private bool IsMouseOverMiniMap()
        {
            if (ui.MiniMapHUD == null || !ui.MiniMapHUD.activeSelf) return false;
            if (ui.ContainerObj != null && ui.ContainerObj.activeSelf && IsMouseInRect(ui.ContainerObj)) return true;
            return IsMouseOverBar();
        }

        private bool IsMouseOverBar() => ui.BarObj != null && ui.BarObj.activeInHierarchy && IsMouseInRect(ui.BarObj);

        private bool IsMouseOverAnyButton()
        {
            if (ui.BarObj == null) return false;
            foreach (var btn in ui.BarObj.GetComponentsInChildren<Button>())
            {
                if (IsMouseInRect(btn.gameObject)) return true;
            }
            return false;
        }

        private bool IsMouseInRect(GameObject obj) => RectTransformUtility.RectangleContainsScreenPoint(obj.GetComponent<RectTransform>(), Input.mousePosition);
        #endregion
    }
}