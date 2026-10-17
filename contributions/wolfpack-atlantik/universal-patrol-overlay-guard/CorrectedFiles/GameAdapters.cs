using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DWS.Common.InjectionFramework;
using DWS.Common.Resources;
using UBOAT.Game;
using UBOAT.Game.Core;
using UBOAT.Game.Core.Data;
using UBOAT.Game.Core.Time;
using UBOAT.Game.Core.AI;
using UBOAT.Game.Core.AI.GroupBehaviours;
using UBOAT.Game.Sandbox;
using UBOAT.Game.Sandbox.Messages;
using UBOAT.Game.Sandbox.Missions;
using UBOAT.Game.Sandbox.Devices;
using UBOAT.Game.Scene;
using UBOAT.Game.Scene.Characters;
using UBOAT.Game.Scene.Camera;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.Scene.Utilities;
using UBOAT.Game.UI.Map;
using UnityEngine;
using UnityEngine.AI;

namespace WolfpackAtlantik
{
    public static class CampaignIdentityAdapter
    {
        public static string CurrentKey()
        {
            try
            {
                var crew = InjectionFramework.Instance.GetInstance<PlayerCrew>();
                var skipper = crew != null ? crew.Skipper : null;
                var data = skipper != null ? skipper.Data : null;
                if (data == null) return null;

                string name = SafeString(ReadMember(data, "Name", "name"));
                // PlayableCharacterData stores these as long ticks (birthDateTicks / enrollmentTicks).
                // v1.0.0 looked for DateTime members that do not exist, so every career with the same
                // skipper name hashed to the same key and shared one state file.
                long birth = DateTicks(ReadMember(data, "birthDateTicks", "BirthDateTicks", "BirthDate", "birthDate", "DateOfBirth", "dateOfBirth"));
                long enrolled = DateTicks(ReadMember(data, "enrollmentTicks", "EnrollmentTicks", "EnrollmentDate", "enrollmentDate", "EnlistmentDate", "enlistmentDate"));
                string raw = name + "|" + birth + "|" + enrolled;
                return "career-" + StableHash(raw).ToString("x8");
            }
            catch { return null; }
        }

        // The v1.0.0 key (name only). Used once to migrate an existing state folder to the new key.
        public static string LegacyKey()
        {
            try
            {
                var crew = InjectionFramework.Instance.GetInstance<PlayerCrew>();
                var skipper = crew != null ? crew.Skipper : null;
                var data = skipper != null ? skipper.Data : null;
                if (data == null) return null;
                string name = SafeString(ReadMember(data, "Name", "name"));
                return "career-" + StableHash(name + "|0|0").ToString("x8");
            }
            catch { return null; }
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in value ?? string.Empty)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return hash;
            }
        }

        private static object ReadMember(object obj, params string[] names)
        {
            if (obj == null) return null;
            Type t = obj.GetType();
            const BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (string name in names)
            {
                try
                {
                    PropertyInfo p = t.GetProperty(name, f);
                    if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(obj, null);
                    FieldInfo field = t.GetField(name, f);
                    if (field != null) return field.GetValue(obj);
                }
                catch { }
            }
            return null;
        }

        private static long DateTicks(object value)
        {
            if (value is DateTime) return ((DateTime)value).Ticks;
            if (value is long) return (long)value;
            return 0L;
        }

        private static string SafeString(object value) { return value != null ? value.ToString() : "skipper"; }
    }

    public static class GameTimeAdapter
    {
        private static readonly DateTime SafeFallback = new DateTime(1939, 9, 1, 0, 0, 0, DateTimeKind.Unspecified);
        private static DateTime _lastResolvedDate = SafeFallback;
        private static GameTime _gameTime;
        private static bool _warnedFallback;

        public static DateTime CurrentCampaignDate
        {
            get
            {
                DateTime date;
                if (TryResolveCampaignDate(out date))
                {
                    _lastResolvedDate = ClampWarDate(date);
                    return _lastResolvedDate;
                }
                if (!_warnedFallback)
                {
                    _warnedFallback = true;
                    WolfpackAtlantikLog.Info("Campaign clock not resolved yet; career simulation is pinned to 1939-09-01 rather than real-world time.");
                }
                return _lastResolvedDate;
            }
        }

        public static long CurrentTicks { get { return CurrentCampaignDate.Ticks; } }
        public static long HoursToTicks(float hours) { return TimeSpan.FromHours(hours).Ticks; }
        public static bool HasLiveCampaignClock
        {
            get
            {
                DateTime ignored;
                return TryResolveCampaignDate(out ignored);
            }
        }

        private static bool TryResolveCampaignDate(out DateTime date)
        {
            date = default(DateTime);
            try
            {
                if (_gameTime == null || !_gameTime)
                {
                    try { _gameTime = InjectionFramework.Instance.GetInstance<GameTime>(); }
                    catch { _gameTime = null; }
                    if (_gameTime == null || !_gameTime)
                        _gameTime = UnityEngine.Object.FindObjectOfType<GameTime>();
                }
                if (_gameTime != null && _gameTime)
                {
                    date = _gameTime.CurrentDateTime;
                    if (IsPlausible(date)) return true;
                }

                var career = ScriptableObjectSingleton.LoadSingleton<PlayerCareer>();
                if (career == null) return false;

                string[] names = { "CurrentDate", "currentDate", "CampaignDate", "campaignDate", "GameDate", "gameDate", "Date", "date" };
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                Type t = career.GetType();
                foreach (string name in names)
                {
                    try
                    {
                        PropertyInfo p = t.GetProperty(name, flags);
                        if (p != null && p.PropertyType == typeof(DateTime))
                        {
                            date = (DateTime)p.GetValue(career, null);
                            if (IsPlausible(date)) return true;
                        }
                        FieldInfo f = t.GetField(name, flags);
                        if (f != null && f.FieldType == typeof(DateTime))
                        {
                            date = (DateTime)f.GetValue(career);
                            if (IsPlausible(date)) return true;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }

        private static bool IsPlausible(DateTime value) { return value.Year >= 1939 && value.Year <= 1946; }
        private static DateTime ClampWarDate(DateTime value)
        {
            if (value < SafeFallback) return SafeFallback;
            DateTime end = new DateTime(1945, 12, 31, 23, 59, 59, value.Kind);
            return value > end ? end : value;
        }
    }

    public static class GameEconomyAdapter
    {
        public static string LastFailure { get; private set; }

        public static bool TrySpend(int money, int researchPoints)
        {
            LastFailure = null;
            if (money < 0 || researchPoints < 0)
            {
                LastFailure = "The quoted cost was invalid; nothing was deducted.";
                return false;
            }
            try
            {
                PlayerCareer career = ScriptableObjectSingleton.LoadSingleton<PlayerCareer>();
                if (career == null)
                {
                    LastFailure = "The live campaign economy is not ready; nothing was deducted.";
                    return false;
                }

                int currentBudget = career.Budget;
                int currentRp = career.ReputationPoints;
                if (currentBudget < money)
                {
                    LastFailure = "The commission costs " + money.ToString("N0") + " RM; available budget is " + currentBudget.ToString("N0") + " RM.";
                    return false;
                }
                if (currentRp < researchPoints)
                {
                    LastFailure = "The commission costs " + researchPoints + " reputation; available reputation is " + currentRp + ".";
                    return false;
                }
                try
                {
                    career.ReputationPoints = currentRp - researchPoints;
                    career.AddBudgetOperation(-money, WolfpackText.Unlocalized("Wolfpack - Atlantik commission and doctrine"));
                }
                catch
                {
                    career.ReputationPoints = currentRp;
                    LastFailure = "The campaign rejected the transaction; nothing was deducted.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                WolfpackAtlantikLog.Error("Economy spend failed", ex);
                LastFailure = "The campaign economy raised an error; nothing was deducted.";
                return false;
            }
        }

        public static void Refund(int money, int researchPoints)
        {
            if (money <= 0 && researchPoints <= 0) return;
            try
            {
                PlayerCareer career = ScriptableObjectSingleton.LoadSingleton<PlayerCareer>();
                if (career == null) return;
                career.AddBudgetOperation(Math.Max(0, money), WolfpackText.Unlocalized("Wolfpack - Atlantik cancelled commission refund"));
                career.ReputationPoints = career.ReputationPoints + Math.Max(0, researchPoints);
            }
            catch (Exception ex) { WolfpackAtlantikLog.Error("Economy refund failed", ex); }
        }
    }

    public static partial class WorldBoatAdapter
    {
        private sealed class CommissionedPresentation
        {
            public string boatNumber;
            public string emblemId;
            public bool isPlayerCommanded = true;
        }

        public sealed class HomePortChoice
        {
            public string id;
            public string displayName;
        }

        public sealed class PatrolFormationCell
        {
            public int offsetX;
            public int offsetY;
            public string gridName;
            public int landMask;
            public bool valid;
            // Native chart geometry, normalized into the visible chart window (y increases north).
            // Unlike logical formation offsets these preserve half-width/offset/irregular regions.
            public Rect normalizedBounds;
            public Vector2[] normalizedCorners;
            public Vector2 normalizedCenter;
        }

        public sealed class PatrolFormationPlan
        {
            public string centerGrid;
            public string description;
            public bool missionAssigned;
            public PatrolFormationCell[] cells = new PatrolFormationCell[81];
            public PatrolFormationCell[] chartCells = new PatrolFormationCell[0];
            public Vector2 chartWorldMin;
            public Vector2 chartWorldMax;

            public PatrolFormationCell CellAt(int offsetX, int offsetY)
            {
                if (offsetX < -4 || offsetX > 4 || offsetY < -4 || offsetY > 4) return null;
                return cells[(4 - offsetY) * 9 + (offsetX + 4)];
            }
        }

        private sealed class MapCommandRelay : MonoBehaviour
        {
            private Coroutine _pending;

            public void QueueKnown(Vector2 position, bool openMap)
            {
                if (_pending != null) StopCoroutine(_pending);
                _pending = StartCoroutine(FocusKnownWhenClear(position, openMap));
            }

            private IEnumerator FocusKnownWhenClear(Vector2 position, bool openMap)
            {
                while (Input.GetMouseButton(0)) yield return null;
                yield return null;
                WolfpackCommanderWindow.Close();
                yield return null;
                FocusKnownPosition(position, openMap);
                _pending = null;
            }

        }

        private const string EntityPrefix = "WA-";
        private static readonly Dictionary<string, SandboxEntity> EntitiesById = new Dictionary<string, SandboxEntity>(StringComparer.Ordinal);
        private static readonly Dictionary<string, SandboxEntity> DocksById = new Dictionary<string, SandboxEntity>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<SandboxDock> LiveDockSnapshot = new List<SandboxDock>();
        private static float _nextDockSnapshotRefresh;
        private static readonly HashSet<string> LoggedPortGridAudits = new HashSet<string>(StringComparer.Ordinal);
        private static Texture2D _approximateChartLand;
        private static Vector2 _approximateChartMin;
        private static Vector2 _approximateChartMax;
        private static readonly Dictionary<int, Vector2> LastObservedPositions = new Dictionary<int, Vector2>();
        private static readonly Dictionary<string, float> MissingBoatRepairAfter = new Dictionary<string, float>(StringComparer.Ordinal);
        private static readonly Dictionary<string, float> ReturnRouteRetryAfter = new Dictionary<string, float>(StringComparer.Ordinal);
        private static readonly HashSet<string> NavigationRecoveryProcessed = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, float> FormationRefreshAfter = new Dictionary<string, float>(StringComparer.Ordinal);
        private static readonly Dictionary<int, Vector2> ReturnApproaches = new Dictionary<int, Vector2>();
        private static readonly Dictionary<string, CommissionedPresentation> PendingPresentations = new Dictionary<string, CommissionedPresentation>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Resource<GameObject>> CommissionedEmblemPrefabs = new Dictionary<string, Resource<GameObject>>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> PhysicalEmblemAttempts = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> MissingBoatDiagnostics = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<int> TrackedGroups = new HashSet<int>();
        private static readonly Dictionary<string, float> LastReportedHealth = new Dictionary<string, float>(StringComparer.Ordinal);
        private static readonly Dictionary<string, BoatOperationalState> LastReportedStates = new Dictionary<string, BoatOperationalState>(StringComparer.Ordinal);
        private static readonly Dictionary<string, long> LastContactReportTicks = new Dictionary<string, long>(StringComparer.Ordinal);
        private static Sandbox _sandbox;
        private static SandboxPlayerWolfpack _wolfpack;
        private static PlayerShip _playerShip;
        private static EntityTypesDatabase _entityTypes;
        private static INavalGrid _navalGrid;
        private static WorldNavMesh _worldNavMesh;
        private static MainCamera _mainCamera;
        private static ISelectionManager _selectionManager;
        private static GroupMarkersManager _markerManager;
        private static EntityMarkersManager _entityMarkerManager;
        // Cache the native map marker manager even while its GameObject is inactive. Marker UI
        // mutation is still gated on activeInHierarchy so a closed map can be cached safely.
        private static float _nextEntityMarkerManagerResolveTime;
        private static MapCommandRelay _mapCommandRelay;
        private static readonly MethodInfo CreateMapMarkerMethod = typeof(GroupMarkersManager).GetMethod(
            "CreateMarker", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo CreatePlayerEntityMarkerMethod = typeof(EntityMarkersManager).GetMethod(
            "CreateMarker", BindingFlags.Instance | BindingFlags.NonPublic, null,
            new Type[] { typeof(SandboxEntity), typeof(EntityMarkerUI) }, null);
        private static readonly FieldInfo PlayerShipMarkerPrefabField = typeof(EntityMarkersManager).GetField(
            "playerShipPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo EntityMarkersMapField = typeof(EntityMarkersManager).GetField(
            "markersMap", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo SandboxEntityNameField = typeof(SandboxEntity).GetField(
            "name", BindingFlags.Instance | BindingFlags.NonPublic);
        // SandboxMobileGroup.Route_Changed assumes this private field is populated. Some AI groups
        // recovered from campaign state have a Route but no cached body, so the first native
        // FindPathTo call throws while writing velocity. Repair the same invariant StartGroup owns
        // before any commissioned boat is given a movement behaviour.
        private static readonly FieldInfo MobileRigidBodyField = typeof(SandboxMobileGroup).GetField(
            "rigidBody2D", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo GroupAiField = typeof(SandboxGroup).GetField(
            "ai", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo GroupHasAiField = typeof(SandboxGroup).GetField(
            "hasAi", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ReturnGroupField = typeof(ReturnToPortBehaviour).GetField(
            "group", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ReturnDockField = typeof(ReturnToPortBehaviour).GetField(
            "dock", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo ReturnSpawnedMethod = typeof(ReturnToPortBehaviour).GetMethod(
            "Group_Spawned", BindingFlags.Instance | BindingFlags.NonPublic);
        private static float _nextNavigationRecovery;
        private static readonly FieldInfo EmblemAffectedSlotsField = typeof(Emblem).GetField(
            "affectedEquipmentSlots", BindingFlags.Instance | BindingFlags.NonPublic);
        private static EquipmentDatabase _equipmentDatabase;
        private static ResourceManager _resourceManager;
        private static WolfpackCampaignState _campaign;
        private static SandboxEntity _lastPlayerDock;
        private static long _lastPlayerLaunchTicks;
        private static long _lastPlayerHitTicks;
        private static float _nextResolveTime;
        private static bool _lastResolveSucceeded;

        public static string LastActionMessage { get; private set; }

        public static bool LiveRecruitmentAvailable { get { return ResolveBindings(false); } }
        public static bool LiveOrdersAvailable { get { return ResolveBindings(false); } }
        public static bool LiveMapAvailable { get { return ResolveBindings(false) && _mainCamera != null; } }
        public static bool IsMapOpen
        { get { return ResolveBindings(false) && _mainCamera != null && _mainCamera.CurrentMode == CameraMode.Map; } }
        public static SandboxEntity CurrentDock
        { get { return ResolveBindings(false) && _playerShip != null && _playerShip ? _playerShip.CurrentDock : null; } }
        public static bool LivePortCameraAvailable { get { return LiveMapAvailable && IsPlayerDocked; } }
        public static bool LiveRefitAvailable { get { return ResolveBindings(false) && IsPlayerDocked && _equipmentDatabase != null; } }

        public static Sprite FormationBoatSprite(bool commissioned)
        {
            if (!ResolveBindings(false)) return null;
            Sprite submarine = WolfpackArtwork.SubmarineMapMarker();
            if (submarine != null)
                return commissioned ? WolfpackCommissionedMapMarker.TealBoatSprite(submarine) : submarine;

            // Last-resort fallback for installations where the vanilla category sprite could
            // not be resolved. This must not be the normal path: the prefab is unbound here and
            // can still contain its editor-time placeholder ship.
            if (_entityMarkerManager == null || !_entityMarkerManager)
                ResolveEntityMarkerManager(false);
            EntityMarkerUI prefab = _entityMarkerManager != null && PlayerShipMarkerPrefabField != null
                ? PlayerShipMarkerPrefabField.GetValue(_entityMarkerManager) as EntityMarkerUI : null;
            return commissioned ? WolfpackCommissionedMapMarker.TealPlayerBoatSprite(prefab)
                : WolfpackCommissionedMapMarker.PlayerBoatSprite(prefab);
        }

        public static Sprite FormationBoatSprite(RecruitedBoatRecord boat)
        {
            Sprite source = WolfpackArtwork.SubmarineMapMarker();
            if (source == null) return FormationBoatSprite(boat == null || boat.isPlayerCommanded);
            string colour = boat != null ? WolfpackMapVisibility.Colour(_campaign, boat) : "teal";
            return WolfpackCommissionedMapMarker.ColourBoatSprite(source, colour);
        }

        public static bool TryGetFormationPlotCellForBoat(RecruitedBoatRecord boat, PatrolFormationPlan formation,
            out int offsetX, out int offsetY)
        {
            offsetX = offsetY = 0;
            if (boat == null || formation == null || _navalGrid == null) return false;
            NavalGridSquare center = _navalGrid.GetSquare(formation.centerGrid);
            if (center == null) return false;
            Vector2 position; float heading;
            if (!TryGetVisibleBoatPosition(boat, out position, out heading)) return false;
            NavalGridSquare actual = _navalGrid.GetSquare(position, center.Depth);
            if (actual == null) return false;
            for (int i = 0; formation.cells != null && i < formation.cells.Length; i++)
            {
                PatrolFormationCell cell = formation.cells[i];
                if (cell == null || !cell.valid || !string.Equals(cell.gridName, actual.Name, StringComparison.OrdinalIgnoreCase)) continue;
                offsetX = cell.offsetX;
                offsetY = cell.offsetY;
                return true;
            }
            return false; // Off-board boats are not clamped into an invented nearby patrol cell.
        }

        public static bool TryGetBoatPlotPosition(RecruitedBoatRecord boat, PatrolFormationPlan formation, out Vector2 normalized)
        {
            normalized = Vector2.zero;
            if (boat == null || formation == null) return false;
            Vector2 position; float heading;
            if (!TryGetVisibleBoatPosition(boat, out position, out heading)) return false;
            Vector2 size = formation.chartWorldMax - formation.chartWorldMin;
            if (!IsFinite(position) || size.x <= 0f || size.y <= 0f) return false;
            normalized = new Vector2((position.x - formation.chartWorldMin.x) / size.x,
                (position.y - formation.chartWorldMin.y) / size.y);
            return normalized.x >= 0f && normalized.x <= 1f && normalized.y >= 0f && normalized.y <= 1f;
        }

        public static bool TryGetFlagPlotPosition(PatrolFormationPlan formation, out Vector2 normalized)
        {
            normalized = Vector2.zero;
            if (formation == null || !ResolveBindings(false)) return false;
            Vector2 position = PlayerStrategicPosition();
            Vector2 size = formation.chartWorldMax - formation.chartWorldMin;
            if (!IsFinite(position) || size.x <= 0f || size.y <= 0f) return false;
            normalized = new Vector2((position.x - formation.chartWorldMin.x) / size.x,
                (position.y - formation.chartWorldMin.y) / size.y);
            return normalized.x >= 0f && normalized.x <= 1f && normalized.y >= 0f && normalized.y <= 1f;
        }

        public static Texture2D ApproximateChartLand(PatrolFormationPlan formation)
        {
            if (formation == null || _worldNavMesh == null || _navalGrid == null
                || !_navalGrid.AllowCachingReferences) return null;
            Vector2 min = formation.chartWorldMin;
            Vector2 max = formation.chartWorldMax;
            if (!IsFinite(min) || !IsFinite(max) || max.x <= min.x || max.y <= min.y) return null;
            if (_approximateChartLand != null && _approximateChartLand
                && min == _approximateChartMin && max == _approximateChartMax) return _approximateChartLand;

            // Deliberately approximate land/water context, NOT the native coastline asset.
            // User-approved whole-chart mask: boolean water probes only, once per viewport.
            // No path solver, camera capture, readback or per-frame resampling is involved.
            const int resolution = 72;
            double maskStartedAt = Time.realtimeSinceStartupAsDouble; // not Stopwatch: System.Diagnostics is denied to mods
            Color32[] pixels = new Color32[resolution * resolution];
            int landSamples = 0;
            Vector2 span = max - min;
            for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 point = new Vector2(min.x + (x + 0.5f) / resolution * span.x,
                        min.y + (y + 0.5f) / resolution * span.y);
                    bool water = _worldNavMesh.IsOnNavMesh(point);
                    if (!water) landSamples++;
                    pixels[y * resolution + x] = water
                        ? new Color32(255, 255, 255, 0) : new Color32(255, 255, 255, 255);
                }
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.name = "WA Approximate navigable-water coastline";
            texture.hideFlags = HideFlags.DontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            if (_approximateChartLand != null && _approximateChartLand) UnityEngine.Object.Destroy(_approximateChartLand);
            _approximateChartLand = texture;
            _approximateChartMin = min;
            _approximateChartMax = max;
            WolfpackAtlantikLog.Info("Built approximate chart coast mask: 72x72 bounded water probes in "
                + ((Time.realtimeSinceStartupAsDouble - maskStartedAt) * 1000.0).ToString("F1") + " ms; " + landSamples + "/" + pixels.Length
                + " land samples; world-km " + min + " to " + max + "; cached for this viewport.");
            return texture;
        }

        public static string GetMarkerColourId(SandboxEntity entity, bool isPlayerCommanded)
        {
            RecruitedBoatRecord mapped = MapRecord(entity);
            if (mapped != null) return WolfpackCommissionedMapMarker.NormalizeMarkerColour(
                WolfpackMapVisibility.Colour(_campaign, mapped), isPlayerCommanded ? "teal" : "darkgreen");
            if (entity != null && _campaign != null)
            {
                for (int i = 0; i < _campaign.boats.Count; i++)
                {
                    RecruitedBoatRecord boat = _campaign.boats[i];
                    if (boat != null && string.Equals(boat.linkedWorldEntityId, entity.Name, StringComparison.OrdinalIgnoreCase))
                        return WolfpackCommissionedMapMarker.NormalizeMarkerColour(WolfpackMapVisibility.Colour(_campaign, boat),
                            boat.isPlayerCommanded ? "teal" : "darkgreen");
                }
            }
            return isPlayerCommanded ? "teal" : "darkgreen";
        }

        public static void SetMarkerColour(RecruitedBoatRecord boat, string colourId)
        {
            if (boat == null || !WolfpackCommissionedMapMarker.IsMarkerColour(colourId)) return;
            boat.markerColourId = WolfpackCommissionedMapMarker.NormalizeMarkerColour(colourId, "teal");
            if (_campaign != null) WolfpackSaveStore.Save(_campaign);
            SandboxEntity entity = FindEntityForRecord(boat);
            if (entity != null) EnsureCommissionedPresentation(entity, boat.boatNumber, boat.emblemId, boat.isPlayerCommanded);
        }
        public static bool LiveSupplyExchangeAvailable { get { return ResolveBindings(false) && !IsPlayerDocked; } }

        public static string CurrentPlayerHullVariant
        {
            get
            {
                try
                {
                    if (!ResolveBindings(false) || _playerShip == null || !_playerShip || _playerShip.Blueprint == null || _playerShip.Blueprint.Type == null)
                        return "Type VIIC";
                    string name = _playerShip.Blueprint.Type.Name ?? "Type VIIC";
                    name = name.Replace(" (Player)", string.Empty).Replace("(Player)", string.Empty).Trim();
                    string canonical = CanonicalEntityTypeName(name);
                    return string.Equals(canonical, "Type VIIC41", StringComparison.Ordinal) ? "Type VIIC/41" : canonical;
                }
                catch { return "Type VIIC"; }
            }
        }

        public static bool CanRecruitHullExactly(string hullVariant)
        {
            try
            {
                if (!ResolveBindings(false) || _entityTypes == null || _wolfpack == null) return false;
                string canonical = CanonicalEntityTypeName(hullVariant);
                EntityType type = _entityTypes.GetEntityType(canonical);
                return type != null && type.GetCountryBlueprint(_wolfpack.Country) != null;
            }
            catch { return false; }
        }

        public static bool IsSameHullType(string first, string second)
        {
            return string.Equals(CanonicalEntityTypeName(first), CanonicalEntityTypeName(second),
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanChangeCommandHull(RecruitedBoatRecord boat)
        {
            if (boat == null || !boat.isPlayerCommanded || boat.operationalState != BoatOperationalState.InPort
                || !ResolveBindings(false) || !IsPlayerDocked) return false;
            SandboxEntity entity = FindEntityForRecord(boat);
            SandboxDock alliedDock = FindDockContaining(entity) as SandboxDock;
            SandboxDock playerDock = _playerShip != null && _playerShip ? _playerShip.CurrentDock as SandboxDock : null;
            return entity != null && alliedDock != null && playerDock != null
                && (alliedDock == playerDock || string.Equals(alliedDock.Name, playerDock.Name,
                    StringComparison.OrdinalIgnoreCase));
        }

        public static bool TryChangeCommandHull(RecruitedBoatRecord boat, string targetHull)
        {
            if (!CanChangeCommandHull(boat))
            {
                LastActionMessage = "Hull reassignment requires the selected U-boat and flag boat to be berthed together.";
                return false;
            }
            string canonicalTarget = CanonicalEntityTypeName(targetHull);
            if (IsSameHullType(boat.hullVariantId, canonicalTarget))
            {
                LastActionMessage = (boat.boatNumber ?? "The selected command") + " already uses " + targetHull + ".";
                return false;
            }
            EntityType type = _entityTypes != null ? _entityTypes.GetEntityType(canonicalTarget) : null;
            EntityBlueprint blueprint = type != null && _wolfpack != null ? type.GetCountryBlueprint(_wolfpack.Country) : null;
            if (blueprint == null || !HasNativeNpcHull(type))
            {
                LastActionMessage = targetHull + " has no installed AI vessel prefab. No hull or RP was changed.";
                return false;
            }

            SandboxEntity oldEntity = FindEntityForRecord(boat);
            SandboxDock dock = FindDockContaining(oldEntity) as SandboxDock;
            if (oldEntity == null || dock == null)
            {
                LastActionMessage = "The selected command has no confirmed berth for a safe hull transfer.";
                return false;
            }
            if (!GameEconomyAdapter.TrySpend(0, 1))
            {
                LastActionMessage = GameEconomyAdapter.LastFailure ?? "One reputation point is required for a command hull reassignment.";
                return false;
            }

            SandboxDock.DockPoint oldPoint = null;
            SandboxDock.DockPoint[] points = dock.DockPoints;
            for (int i = 0; points != null && i < points.Length; i++)
                if (points[i].DockedEntity == oldEntity) { oldPoint = points[i]; break; }

            SandboxEntity replacement = null;
            string oldWorldId = oldEntity.Name;
            string newWorldId = EntityPrefix + BoatRecordId(boat) + "-hull-"
                + StableEntityHash(canonicalTarget + "|" + GameTimeAdapter.CurrentTicks).ToString("x8");
            try
            {
                if (oldPoint != null) oldPoint.DockedEntity = null;
                replacement = SandboxEntity.Create(newWorldId, blueprint);
                replacement.IsAnonymousUnit = true;
                replacement.Country = _wolfpack.Country;
                replacement.Position = dock.Position;
                replacement.IconOverride = null;
                replacement.IsPublic = false;
                string display = !string.IsNullOrEmpty(boat.boatNumber) ? boat.boatNumber : "BdU auxiliary U-boat";
                // TryDock owns initial group/position assignment. A temporary mobile group at
                // the port origin used to become a spawned hull on land before this succeeded.
                if (!TryDockCommissionedWithPriority(dock, replacement))
                    throw new InvalidOperationException("No compatible submarine pen accepted the replacement hull.");
                NormalizeWolfpackIdentity(replacement, display);
                ApplyRecruitEmblem(replacement, boat.emblemId);

                EntitiesById.Remove(oldWorldId);
                oldEntity.Destroy(false);
                boat.hullVariantId = string.Equals(canonicalTarget, "Type VIIC41", StringComparison.Ordinal)
                    ? "Type VIIC/41" : canonicalTarget;
                boat.linkedWorldEntityId = replacement.Name;
                boat.operationalState = BoatOperationalState.InPort;
                boat.isVirtualRecordOnly = false;
                TrackEntity(replacement);
                RefreshRecordFromWorld(boat, replacement);
                EnsureCommissionedPresentation(replacement, boat.boatNumber, boat.emblemId, true);
                if (_campaign != null) WolfpackSaveStore.Save(_campaign);
                LastActionMessage = (boat.boatNumber ?? "The selected command") + " transferred to "
                    + boat.hullVariantId + " for 1 RP.";
                WolfpackNativeUI.ShowToast(LastActionMessage);
                return true;
            }
            catch (Exception ex)
            {
                if (replacement != null)
                {
                    try { replacement.Destroy(false); } catch { }
                    EntitiesById.Remove(newWorldId);
                }
                if (oldPoint != null && oldPoint.DockedEntity == null) oldPoint.DockedEntity = oldEntity;
                GameEconomyAdapter.Refund(0, 1);
                WolfpackAtlantikLog.Error("Command hull reassignment failed", ex);
                LastActionMessage = "Hull reassignment failed safely; the original U-boat remains berthed and 1 RP was refunded.";
                return false;
            }
        }

        public static string AssignRecruitmentPort(string preferredPortId, string hullVariant)
        {
            if (!ResolveBindings(false)) return null;
            EntityType requestedType = _entityTypes.GetEntityType(CanonicalEntityTypeName(hullVariant));
            if (!HasNativeNpcHull(requestedType))
            {
                LastActionMessage = hullVariant + " has no installed AI vessel prefab. Choose a supported hull; no money or RP was deducted.";
                return null;
            }
            SandboxDock preferred = FindDock(preferredPortId) as SandboxDock;
            SandboxDock assigned = FindAvailableRecruitDock(preferred, null);
            if (assigned == null)
            {
                LastActionMessage = "BdU cannot commission another U-boat: every registered submarine pen is already assigned to a recruited command.";
                return null;
            }
            string display = assigned.Group != null ? assigned.Group.DisplayName.ToString() : assigned.Name;
            RememberHomePort(assigned.Name, display);
            return assigned.Name;
        }

        private static SandboxDock FindAvailableRecruitDock(SandboxDock preferred, SandboxDock excluded)
        {
            List<SandboxDock> candidates = new List<SandboxDock>();
            HashSet<SandboxDock> seen = new HashSet<SandboxDock>();
            foreach (SandboxEntity value in DocksById.Values)
            {
                SandboxDock dock = value as SandboxDock;
                if (dock == null || dock == excluded || !seen.Add(dock) || RecruitBerthCapacity(dock) <= 0) continue;
                if (_wolfpack != null && _wolfpack.Country != null && dock.Country != null
                    && dock.Country.GetRelationWith(_wolfpack.Country) == Country.Relation.Enemy) continue;
                if (CountAssignedRecruits(dock.Name) >= RecruitBerthCapacity(dock)) continue;
                candidates.Add(dock);
            }
            if (preferred != null && candidates.Contains(preferred)) return preferred;
            Vector2 origin = preferred != null ? preferred.Position
                : (_lastPlayerDock != null ? _lastPlayerDock.Position : (_wolfpack != null ? _wolfpack.Position : Vector2.zero));
            candidates.Sort(delegate(SandboxDock a, SandboxDock b)
            {
                float da = (a.Position - origin).sqrMagnitude;
                float db = (b.Position - origin).sqrMagnitude;
                int distance = da.CompareTo(db);
                return distance != 0 ? distance : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            return candidates.Count > 0 ? candidates[0] : null;
        }

        private static int RecruitBerthCapacity(SandboxDock dock)
        {
            int count = 0;
            SandboxDock.DockPoint[] points = dock != null ? dock.DockPoints : null;
            for (int i = 0; points != null && i < points.Length; i++)
                if (points[i] != null && (points[i].AllowedShipCategories & EntityTypeCategory.Submarine) != 0) count++;
            return count;
        }

        private static int CountAssignedRecruits(string portId)
        {
            if (_campaign == null || string.IsNullOrEmpty(portId)) return 0;
            int count = 0;
            for (int i = 0; i < _campaign.boats.Count; i++)
            {
                RecruitedBoatRecord boat = _campaign.boats[i];
                if (boat != null && boat.operationalState != BoatOperationalState.Lost
                    && string.Equals(boat.homePortId, portId, StringComparison.OrdinalIgnoreCase)) count++;
            }
            for (int i = 0; i < _campaign.pendingRecruitments.Count; i++)
            {
                PendingRecruitmentRecord pending = _campaign.pendingRecruitments[i];
                if (pending != null && string.Equals(pending.destinationPortId, portId, StringComparison.OrdinalIgnoreCase)) count++;
            }
            return count;
        }

        private static bool EnsureAssignedPortWithinCapacity(RecruitedBoatRecord boat)
        {
            if (boat == null || _campaign == null) return false;
            SandboxDock current = FindDock(boat.homePortId) as SandboxDock;
            if (current == null) return false;
            int capacity = RecruitBerthCapacity(current);
            int rank = 0;
            for (int i = 0; i < _campaign.boats.Count; i++)
            {
                RecruitedBoatRecord candidate = _campaign.boats[i];
                if (candidate == null || candidate.operationalState == BoatOperationalState.Lost
                    || !string.Equals(candidate.homePortId, current.Name, StringComparison.OrdinalIgnoreCase)) continue;
                if (candidate == boat) break;
                rank++;
            }
            if (rank < capacity) return false;
            SandboxDock overflow = FindAvailableRecruitDock(current, current);
            if (overflow == null) return false;
            boat.homePortId = overflow.Name;
            RememberHomePort(overflow.Name, overflow.Group != null ? overflow.Group.DisplayName.ToString() : overflow.Name);
            WolfpackAtlantikLog.Info("Reassigned overflow command " + (boat.boatNumber ?? boat.persistentBoatId)
                + " from " + current.Name + " to " + overflow.Name + " using submarine-pen capacity.");
            return true;
        }

        public static bool IsPlayerDocked
        {
            get
            {
                if (!ResolveBindings(false) || (_playerShip == null || !_playerShip)) return false;
                // PlayerShip.Docked is driven by the land-passage GameObject. With large
                // mod lists that object can remain active after sailing, while CurrentDock
                // is still correctly cleared by PlayerShip.Undock(). Require both so the
                // command UI never advertises port-only purchases while actually at sea.
                bool docked = _playerShip.Docked && _playerShip.CurrentDock != null;
                if (docked) _lastPlayerDock = _playerShip.CurrentDock;
                return docked;
            }
        }

        public static string CurrentPortId
        {
            get
            {
                if (!ResolveBindings(false)) return null;
                SandboxEntity dock = _playerShip != null && _playerShip ? _playerShip.CurrentDock : null;
                if (dock != null) _lastPlayerDock = dock;
                dock = dock ?? _lastPlayerDock;
                return dock != null ? dock.Name : null;
            }
        }

        public static IList<HomePortChoice> GetAvailableHomePorts()
        {
            List<HomePortChoice> result = new List<HomePortChoice>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool registerChanged = false;

            // Seed from the persistent BdU register first. Port entities are scene-local and are
            // legitimately absent after sailing, but base assignments must remain editable at sea.
            if (_campaign != null)
            {
                if (_campaign.knownHomePorts == null) _campaign.knownHomePorts = new List<KnownHomePortRecord>();
                for (int i = 0; i < _campaign.knownHomePorts.Count; i++)
                {
                    KnownHomePortRecord known = _campaign.knownHomePorts[i];
                    if (known == null || string.IsNullOrEmpty(known.id) || !seen.Add(known.id)) continue;
                    result.Add(new HomePortChoice { id = known.id,
                        displayName = string.IsNullOrEmpty(known.displayName) ? known.id : known.displayName });
                }
                // A v0.4.9 campaign may already own boats but have no persisted register. Never hide
                // their assigned base while waiting to rediscover the remaining friendly docks.
                for (int i = 0; i < _campaign.boats.Count; i++)
                {
                    RecruitedBoatRecord boat = _campaign.boats[i];
                    if (boat == null || string.IsNullOrEmpty(boat.homePortId) || !seen.Add(boat.homePortId)) continue;
                    result.Add(new HomePortChoice { id = boat.homePortId, displayName = boat.homePortId });
                }
            }

            if (ResolveBindings(false))
            {
                foreach (SandboxEntity entity in DocksById.Values)
                {
                    SandboxDock dock = entity as SandboxDock;
                    if (dock == null || string.IsNullOrEmpty(dock.Name)) continue;
                    if (_wolfpack != null && _wolfpack.Country != null && dock.Country != null
                        && dock.Country.GetRelationWith(_wolfpack.Country) == Country.Relation.Enemy) continue;
                    string display = dock.Group != null ? dock.Group.DisplayName.ToString() : null;
                    if (string.IsNullOrEmpty(display)) display = dock.Name;
                    if (seen.Add(dock.Name)) result.Add(new HomePortChoice { id = dock.Name, displayName = display });
                    registerChanged |= RememberHomePort(dock.Name, display);
                }
            }
            result.Sort(delegate(HomePortChoice a, HomePortChoice b)
            {
                return string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase);
            });
            if (registerChanged && _campaign != null) WolfpackSaveStore.Save(_campaign);
            return result;
        }

        private static bool RememberHomePort(string id, string displayName)
        {
            if (_campaign == null || string.IsNullOrEmpty(id)) return false;
            if (_campaign.knownHomePorts == null) _campaign.knownHomePorts = new List<KnownHomePortRecord>();
            for (int i = 0; i < _campaign.knownHomePorts.Count; i++)
            {
                KnownHomePortRecord known = _campaign.knownHomePorts[i];
                if (known == null || !string.Equals(known.id, id, StringComparison.OrdinalIgnoreCase)) continue;
                string display = string.IsNullOrEmpty(displayName) ? id : displayName;
                if (string.Equals(known.displayName, display, StringComparison.Ordinal)) return false;
                known.displayName = display;
                return true;
            }
            _campaign.knownHomePorts.Add(new KnownHomePortRecord
            {
                id = id,
                displayName = string.IsNullOrEmpty(displayName) ? id : displayName
            });
            return true;
        }

        // Display name of the port the flag boat is (or was last) berthed in.
        public static string CurrentPortDisplayName
        {
            get
            {
                string id = CurrentPortId;
                if (string.IsNullOrEmpty(id)) return null;
                try
                {
                    IList<HomePortChoice> choices = GetAvailableHomePorts();
                    for (int i = 0; i < choices.Count; i++)
                        if (string.Equals(choices[i].id, id, StringComparison.OrdinalIgnoreCase)) return choices[i].displayName;
                }
                catch { }
                return id;
            }
        }

        // A Wolfpack-owned boat whose live entity is still a Type II hull (no AI prefab here)
        // is rebuilt in place onto the policy hull. Native SetBlueprint keeps identity/position.
        private static readonly HashSet<string> HullRepairAttempted = new HashSet<string>(StringComparer.Ordinal);
        private static bool RepairAiHull(RecruitedBoatRecord boat, SandboxEntity entity)
        {
            if (boat == null || entity == null || entity.IsPlayerShip || entity.Type == null
                || entity.Group is SandboxPlayerWolfpack || _entityTypes == null || _wolfpack == null) return false;
            if (!WolfpackHullPolicy.IsTypeII(entity.Type.Name) || HullRepairAttempted.Contains(entity.Name)) return false;
            HullRepairAttempted.Add(entity.Name);
            try
            {
                string target = WolfpackHullPolicy.ForAi(boat.hullVariantId);
                EntityType type = _entityTypes.GetEntityType(CanonicalEntityTypeName(target));
                EntityBlueprint blueprint = type != null ? type.GetCountryBlueprint(_wolfpack.Country) : null;
                if (blueprint == null || !HasNativeNpcHull(type))
                {
                    WolfpackAtlantikLog.Info("AI hull repair skipped for " + (boat.boatNumber ?? entity.Name) + ": no " + target + " NPC blueprint/prefab.");
                    return false;
                }
                entity.SetBlueprint(blueprint);
                boat.hullVariantId = target;
                WolfpackAtlantikLog.Info("AI HULL REPAIR: " + (boat.boatNumber ?? entity.Name) + " " + entity.Name
                    + " changed to " + target + " in place (Type II hulls have no AI prefab).");
                return true;
            }
            catch (Exception ex) { WolfpackAtlantikLog.Error("AI hull repair failed for " + (boat.boatNumber ?? entity.Name), ex); return false; }
        }

        public static string GetHomePortDisplayName(RecruitedBoatRecord boat)
        {
            if (boat == null || string.IsNullOrEmpty(boat.homePortId)) return "UNASSIGNED";
            IList<HomePortChoice> choices = GetAvailableHomePorts();
            for (int i = 0; i < choices.Count; i++)
                if (string.Equals(choices[i].id, boat.homePortId, StringComparison.OrdinalIgnoreCase))
                    return choices[i].displayName;
            return boat.homePortId;
        }

        public static bool SetHomePort(RecruitedBoatRecord boat, string portId)
        {
            if (boat == null || string.IsNullOrEmpty(portId)) return false;
            if (!WolfpackPatrolAccess.CanReassignGrid(_campaign, boat))
            { LastActionMessage = "BdU retains this boat's base assignment."; return false; }
            SandboxEntity dock = FindDock(portId);
            if (dock == null)
            {
                bool known = false;
                IList<HomePortChoice> choices = GetAvailableHomePorts();
                for (int i = 0; i < choices.Count; i++)
                    if (string.Equals(choices[i].id, portId, StringComparison.OrdinalIgnoreCase)) { known = true; break; }
                if (!known)
                {
                    LastActionMessage = "The selected flotilla base is not in the BdU register.";
                    return false;
                }
            }
            boat.homePortId = dock != null ? dock.Name : portId;
            bool berthTransferred = true;
            if (boat.operationalState == BoatOperationalState.InPort || boat.operationalState == BoatOperationalState.Preparing)
                berthTransferred = dock != null && EnsureDockedAtPort(boat);
            if (_campaign != null) WolfpackSaveStore.Save(_campaign);
            LastActionMessage = (boat.boatNumber ?? "The U-boat") + " is now assigned to " + GetHomePortDisplayName(boat)
                + (berthTransferred ? ". Its berth and future return orders use this base."
                    : ". The berth transfer is queued until the harbour master reports a free pen.");
            return true;
        }

        public static bool OpenQuartermasterUpgradeMode()
        {
            if (!WolfpackPreferences.EnableQuartermasterUpgradeMode)
            {
                LastActionMessage = "Quartermaster upgrade service is disabled in Wolfpack Atlantik options.";
                return false;
            }
            if (!IsPlayerDocked)
            {
                LastActionMessage = "Quartermaster upgrade service is available only while docked in port.";
                return false;
            }
            try
            {
                GameState state = InjectionFramework.Instance.GetInstance<GameState>();
                if (state == null)
                {
                    LastActionMessage = "The quartermaster service is not ready.";
                    return false;
                }
                WolfpackCommanderWindow.Close();
                state.UpgradeMode = true;
                LastActionMessage = "Quartermaster refit service opened for the flag boat.";
                return true;
            }
            catch (Exception ex)
            {
                WolfpackAtlantikLog.Error("Quartermaster upgrade mode failed to open", ex);
                LastActionMessage = "The quartermaster service could not be opened safely.";
                return false;
            }
        }

        public static bool TryGetCurrentMissionPatrolGrid(out string grid, out string description)
        {
            grid = null;
            description = null;
            NavalGridSquare selected;
            return TryGetCurrentMissionPatrolSquare(out selected, out grid, out description);
        }

        public static bool TryBuildPatrolFormation(out PatrolFormationPlan plan)
        {
            plan = null;
            try
            {
                if (!ResolveBindings(false) || _navalGrid == null) return false;
                NavalGridSquare center;
                string grid;
                string description;
                bool mission = TryGetCurrentMissionPatrolSquare(out center, out grid, out description);
                if (!mission)
                {
                    center = _navalGrid.GetSquare(PlayerStrategicPosition(), 3);
                    if (center == null || string.IsNullOrEmpty(center.Name)) return false;
                    grid = center.Name.ToUpperInvariant();
                    description = "FLAG BOAT CURRENT GRID • " + grid;
                }

                // Patrol assignments are usually broad one-digit mission sectors (for example AE8),
                // but the B.d.U. plot needs the next historical 3x3 subdivision (AE81..AE89).
                // Resolve the actual INavalGrid child containing the mission centre instead of
                // inventing a rectangular alphabetic grid around the coarse assignment.
                NavalGridSquare detailedCenter = _navalGrid.GetSquare(center.Center, 3);
                if (detailedCenter != null) center = detailedCenter;

                plan = new PatrolFormationPlan
                {
                    centerGrid = center.Name.ToUpperInvariant(),
                    description = description + " • DETAIL " + center.Name.ToUpperInvariant(),
                    missionAssigned = mission
                };
                for (int y = 4; y >= -4; y--)
                {
                    for (int x = -4; x <= 4; x++)
                    {
                        NavalGridSquare square = OffsetSquare(center, x, y);
                        PatrolFormationCell cell = new PatrolFormationCell
                        {
                            offsetX = x,
                            offsetY = y,
                            gridName = square != null && !string.IsNullOrEmpty(square.Name)
                                ? square.Name.ToUpperInvariant() : "--",
                            landMask = SampleLandMask(square),
                            valid = square != null
                        };
                        plan.cells[(4 - y) * 9 + (x + 4)] = cell;
                    }
                }
                BuildNativeChartGeometry(plan, center);
                LogNativePortGridAudit();
                return true;
            }
            catch { return false; }
        }

        private static bool TryGetCurrentMissionPatrolSquare(out NavalGridSquare selected,
            out string grid, out string description)
        {
            selected = null;
            grid = null;
            description = null;
            try
            {
                PlayerCareer career = ScriptableObjectSingleton.LoadSingleton<PlayerCareer>();
                PatrolAssignment patrol = career != null ? career.CurrentAssignment as PatrolAssignment : null;
                GroupPatrolAssignment groupPatrol = career != null ? career.CurrentAssignment as GroupPatrolAssignment : null;
                if (patrol == null && groupPatrol != null && _navalGrid != null)
                {
                    // Native TargetArea already converts its explicit currentSquares bounds to km.
                    selected = _navalGrid.GetSquare(groupPatrol.TargetArea.center, 3);
                    if (selected == null) return false;
                    grid = selected.Name;
                    description = groupPatrol.CurrentPatrolAreaCodeName + " · " + grid;
                    return true;
                }
                NavalGridSquare[] squares = patrol != null ? patrol.Squares : null;
                if (squares == null || squares.Length == 0) return false;
                selected = squares[squares.Length / 2];
                if (selected == null || string.IsNullOrEmpty(selected.Name)) return false;
                grid = selected.Name.ToUpperInvariant();
                description = squares.Length == 1 ? grid
                    : squares[0].Name.ToUpperInvariant() + " - " + squares[squares.Length - 1].Name.ToUpperInvariant();
                return true;
            }
            catch { return false; }
        }

        private static NavalGridSquare OffsetSquare(NavalGridSquare center, int offsetX, int offsetY)
        {
            if (center == null || _navalGrid == null) return null;
            Vector2 size = center.SizeInKm;
            Vector2 position = center.Center + new Vector2(offsetX * Mathf.Max(1f, size.x),
                offsetY * Mathf.Max(1f, size.y));
            return _navalGrid.GetSquare(position, center.Depth);
        }

        private static void BuildNativeChartGeometry(PatrolFormationPlan plan, NavalGridSquare center)
        {
            // INavalGrid is populated from the installed Sandbox/World Grid sheet. Its explicitly
            // authored Children/Corners include the offset and irregular major regions. Never
            // infer geography by repeating GetSquare at equally spaced lattice sample centres.
            Vector2 span = center.SizeInKm * 9f;
            plan.chartWorldMin = center.Center - span * 0.5f;
            plan.chartWorldMax = center.Center + span * 0.5f;
            List<PatrolFormationCell> chart = new List<PatrolFormationCell>(100);
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<NavalGridSquare> roots = _navalGrid.TopLevelSquares;
            for (int i = 0; roots != null && i < roots.Count; i++)
                CollectNativeChartCells(roots[i], center.Depth, plan, chart, names);
            plan.chartCells = chart.ToArray();
        }

        private static void CollectNativeChartCells(NavalGridSquare square, int depth, PatrolFormationPlan plan,
            List<PatrolFormationCell> chart, HashSet<string> names)
        {
            if (square == null) return;
            // NavalGridSquare.Min/Max/Corners are METRES; Center and entity.Position are km.
            Vector2 min = square.Min * 0.001f;
            Vector2 max = square.Max * 0.001f;
            if (min.x >= plan.chartWorldMax.x || max.x <= plan.chartWorldMin.x
                || min.y >= plan.chartWorldMax.y || max.y <= plan.chartWorldMin.y) return;
            if (square.Depth < depth)
            {
                // This is the native lazy child-construction method used by the game's own
                // map renderer, not a mod-authored German grid. Explicit sheet children win.
                if (square.Children == null) square.Children = _navalGrid.BuildSubSquares(square);
                for (int i = 0; square.Children != null && i < square.Children.Length; i++)
                    CollectNativeChartCells(square.Children[i], depth, plan, chart, names);
                return;
            }
            if (!names.Add(square.Name) || square.Corners == null || square.Corners.Length < 3) return;
            Vector2 span = plan.chartWorldMax - plan.chartWorldMin;
            Vector2[] corners = new Vector2[square.Corners.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 point = square.Corners[i] * 0.001f;
                corners[i] = new Vector2((point.x - plan.chartWorldMin.x) / span.x,
                    (point.y - plan.chartWorldMin.y) / span.y);
            }
            PatrolFormationCell cell = new PatrolFormationCell
            {
                gridName = square.Name,
                valid = true,
                normalizedBounds = Rect.MinMaxRect((min.x - plan.chartWorldMin.x) / span.x,
                    (min.y - plan.chartWorldMin.y) / span.y, (max.x - plan.chartWorldMin.x) / span.x,
                    (max.y - plan.chartWorldMin.y) / span.y),
                normalizedCorners = corners,
                normalizedCenter = new Vector2((square.Center.x - plan.chartWorldMin.x) / span.x,
                    (square.Center.y - plan.chartWorldMin.y) / span.y)
            };
            // Keep logical station offsets available for existing order semantics without using
            // them as visual cell geometry. The nearest sampled station is merely an order key.
            float closest = float.PositiveInfinity;
            for (int i = 0; i < plan.cells.Length; i++)
            {
                PatrolFormationCell logical = plan.cells[i];
                if (logical == null || !string.Equals(logical.gridName, cell.gridName, StringComparison.OrdinalIgnoreCase)) continue;
                float distance = logical.offsetX * logical.offsetX + logical.offsetY * logical.offsetY;
                if (distance >= closest) continue;
                closest = distance;
                cell.offsetX = logical.offsetX;
                cell.offsetY = logical.offsetY;
            }
            if (float.IsPositiveInfinity(closest))
            {
                // An authored half-width region can lie between the old logical samples. It
                // still renders and selects by its actual gridName; only the optional relative
                // formation offset uses the nearest logical station.
                cell.offsetX = Mathf.Clamp(Mathf.RoundToInt(cell.normalizedCenter.x * 9f - 4.5f), -4, 4);
                cell.offsetY = Mathf.Clamp(Mathf.RoundToInt(cell.normalizedCenter.y * 9f - 4.5f), -4, 4);
            }
            chart.Add(cell);
        }

        private static void LogNativePortGridAudit()
        {
            if (_campaign == null || _navalGrid == null) return;
            for (int i = 0; i < _campaign.boats.Count; i++)
            {
                RecruitedBoatRecord boat = _campaign.boats[i];
                if (boat == null || string.IsNullOrEmpty(boat.homePortId) || LoggedPortGridAudits.Contains(boat.homePortId)) continue;
                SandboxDock dock = FindDock(boat.homePortId) as SandboxDock;
                if (dock == null || dock.Group == null) continue;
                NavalGridSquare native = _navalGrid.GetSquare(dock.Position, 3);
                if (native == null) continue;
                LoggedPortGridAudits.Add(boat.homePortId);
                Vector2 latLong = Coordinates.ToLatLong(dock.Position);
                WolfpackAtlantikLog.Info("Native port/grid audit: assigned=" + boat.homePortId + "; entity=" + dock.Name
                    + "; entity-km=" + dock.Position.ToString("F3") + "; group-km=" + dock.Group.Position.ToString("F3")
                    + "; icon-km=" + dock.IconPosition.ToString("F3") + "; latitude/longitude=" + latLong.ToString("F5")
                    + "; native-depth3-grid=" + native.Name + ". Source: installed Sandbox/World Grid.");
            }
        }

        private static int SampleLandMask(NavalGridSquare square)
        {
            if (square == null || _worldNavMesh == null) return 0;
            int mask = 0;
            Vector2 size = square.SizeInKm;
            for (int row = 0; row < 3; row++)
            {
                float north = (1 - row) * size.y * 0.34f;
                for (int column = 0; column < 3; column++)
                {
                    float east = (column - 1) * size.x * 0.34f;
                    Vector2 sample = square.Center + new Vector2(east, north);
                    if (!_worldNavMesh.IsOnNavMesh(sample)) mask |= 1 << (row * 3 + column);
                }
            }
            return mask;
        }

        public static void BindCampaign(WolfpackCampaignState state)
        {
            if (_campaign != state) { MapRecords.Clear(); ManagedHullCheckAfter.Clear(); }
            _campaign = state;
            if (!ResolveBindings(true) || state == null) return;
            bool stateChanged = false;
            for (int i = 0; i < state.boats.Count; i++)
            {
                RecruitedBoatRecord boat = state.boats[i];
                if (boat != null && (boat.operationalState == BoatOperationalState.InPort
                    || boat.operationalState == BoatOperationalState.Preparing
                    || boat.operationalState == BoatOperationalState.Repairing))
                    stateChanged |= EnsureAssignedPortWithinCapacity(boat);
                SandboxEntity entity = FindEntityForRecord(boat);
                if (boat != null && entity == null) entity = RestoreMissingCommissionedBoat(boat, "campaign bind");
                if (boat == null || entity == null) continue;
                stateChanged |= NormalizeWolfpackIdentity(entity, boat.boatNumber);
                if (!string.Equals(boat.linkedWorldEntityId, entity.Name, StringComparison.Ordinal))
                {
                    WolfpackAtlantikLog.Info("Rebound legacy recruited boat " + (boat.boatNumber ?? boat.persistentBoatId)
                        + " from " + (boat.linkedWorldEntityId ?? "<none>") + " to " + entity.Name + ".");
                    boat.linkedWorldEntityId = entity.Name;
                    stateChanged = true;
                }
                boat.isVirtualRecordOnly = false;
                stateChanged |= CheckManagedHullPlacement(boat, entity);
                RefreshRecordFromWorld(boat, entity);
                EnsureCommissionedPresentation(entity, boat.boatNumber, boat.emblemId, boat.isPlayerCommanded);
            }
            stateChanged |= RecoverCommissionedNavigationOnce(state);
            stateChanged |= RecoverOrphanedWolfpackEntities(state);
            if (stateChanged)
            {
                WolfpackSaveStore.Save(state);
                WolfpackCommanderWindow.NotifyStateChanged();
            }
        }

        public static void UpdateCampaign(WolfpackCampaignState state)
        {
            if (!ResolveBindings(false) || state == null) return;
            UpdateNativeContactPresentation(state);
            bool playerDocked = _playerShip != null && _playerShip &&
                                _playerShip.Docked && _playerShip.CurrentDock != null;
            bool stateChanged = false;
            if (playerDocked && _playerShip.CurrentDock != null) _lastPlayerDock = _playerShip.CurrentDock;
            if (state.navigationRecoveryVersion < 2 && Time.realtimeSinceStartup >= _nextNavigationRecovery)
            {
                _nextNavigationRecovery = Time.realtimeSinceStartup + 15f;
                stateChanged |= RecoverCommissionedNavigationOnce(state);
            }

            for (int i = 0; i < state.boats.Count; i++)
            {
                RecruitedBoatRecord boat = state.boats[i];
                if (boat == null) continue;
                SandboxEntity entity = FindEntityForRecord(boat);
                if (entity == null) entity = RestoreMissingCommissionedBoat(boat, "world update");
                if (entity == null) continue;
                stateChanged |= RepairAiHull(boat, entity);
                stateChanged |= CheckManagedHullPlacement(boat, entity);
                stateChanged |= NormalizeWolfpackIdentity(entity, boat.boatNumber);
                if (!string.Equals(boat.linkedWorldEntityId, entity.Name, StringComparison.Ordinal))
                {
                    boat.linkedWorldEntityId = entity.Name;
                    stateChanged = true;
                }
                RefreshRecordFromWorld(boat, entity);
                EnsureCommissionedPresentation(entity, boat.boatNumber, boat.emblemId, boat.isPlayerCommanded);

                // An unavailable hull is retained as a named port/holding record, not sent on
                // a fictional patrol while its physical prefab cannot exist.
                if (IsUnavailableManagedHull(entity)) continue;

                if (!boat.isPlayerCommanded && boat.operationalState == BoatOperationalState.AtSea
                    && boat.orders != null && (boat.orders.patrolMode == PatrolMode.ShadowPlayer
                        || boat.orders.patrolMode == PatrolMode.AdjacentGrid))
                {
                    string recordId = BoatRecordId(boat);
                    float refreshAfter;
                    if (!FormationRefreshAfter.TryGetValue(recordId, out refreshAfter)
                        || Time.realtimeSinceStartup >= refreshAfter)
                    {
                        FormationRefreshAfter[recordId] = Time.realtimeSinceStartup + 10f;
                        ApplyOrders(boat);
                    }
                }

                bool activeFlotillaOrder = boat.joinPlayerNextPatrol
                    || (boat.orders != null && boat.orders.patrolMode == PatrolMode.ShadowPlayer);
                string departureExclusion;
                // Same rule as patrol selection (CanAssignToPlayerPatrol): a boat from a base that does not
                // serve this patrol area may sail when the player fills the quota from all bases. Otherwise
                // it was listed on the patrol but could never leave port.
                bool? departureTheatre = boat.operationalState == BoatOperationalState.InPort && activeFlotillaOrder
                    ? PatrolTheatreEligibility(boat, out departureExclusion) : null;
                if (boat.operationalState == BoatOperationalState.InPort && activeFlotillaOrder && !playerDocked
                    && boat.orders != null && !boat.orders.returnToPortRequested
                    && (departureTheatre == true
                        || (departureTheatre == false && WolfpackPreferences.FillPatrolQuotaFromAllBases)))
                {
                    if (LaunchBoatFromPort(boat, entity))
                    {
                        boat.patrolDepartureRecorded = true;
                        boat.operationalState = BoatOperationalState.AtSea;
                        boat.orders.returnToPortRequested = false;
                        string missionGrid;
                        string missionDescription;
                        if (boat.orders.patrolMode == PatrolMode.AdjacentGrid
                            && TryGetCurrentMissionPatrolGrid(out missionGrid, out missionDescription))
                            boat.orders.patrolGrid = missionGrid;
                        boat.orderSequence++;
                        ApplyOrders(boat);
                        stateChanged = true;
                        WolfpackNativeUI.ShowToast((boat.boatNumber ?? "U-boat") + " has sailed from "
                            + GetHomePortDisplayName(boat) + " to join the patrol.");
                    }
                }

                if (boat.operationalState == BoatOperationalState.Preparing && IsWolfpackGroup(entity.Group))
                {
                    if (EnsureDockedAtPort(boat)) stateChanged = true;
                }

                if (boat.orders != null && boat.orders.returnToPortRequested)
                    stateChanged |= UpdateVanillaReturn(boat, entity);

                if (boat.orders.supplyExchangeRequested && boat.operationalState == BoatOperationalState.AtSea)
                {
                    float readyDistance = SupplyReadyDistance(boat);
                    if (boat.distanceFromPlayerKm <= readyDistance && boat.supplyRendezvousState != SupplyRendezvousState.Ready)
                    {
                        boat.supplyRendezvousState = SupplyRendezvousState.Ready;
                        boat.supplyRendezvousNotified = true;
                        stateChanged = true;
                        WolfpackNativeUI.ShowToast((boat.boatNumber ?? "U-boat") + " is alongside. The transfer manifest is ready.");
                    }
                    else if (boat.distanceFromPlayerKm > readyDistance && boat.supplyRendezvousState == SupplyRendezvousState.Ready)
                    {
                        boat.supplyRendezvousState = SupplyRendezvousState.EnRoute;
                        stateChanged = true;
                    }
                }
                UpdateCommissionedReporting(boat, entity);
            }
            if (stateChanged)
            {
                WolfpackSaveStore.Save(state);
                WolfpackCommanderWindow.NotifyStateChanged();
            }
        }

        public static string TrySpawnRecruitEnRoute(PendingRecruitmentRecord pending)
        {
            if (pending == null || !ResolveBindings(true) || !IsPlayerDocked) return null;
            try
            {
                string requestedHull = string.IsNullOrEmpty(pending.hullVariantId) ? "Type VIIC" : pending.hullVariantId;
                string resolvedHull;
                EntityBlueprint blueprint;
                if (!TryResolveRecruitBlueprint(requestedHull, out resolvedHull, out blueprint))
                {
                    WolfpackAtlantikLog.Error("Live recruitment could not resolve an entity blueprint for hull '" + requestedHull + "'.");
                    return null;
                }
                // Keep the persistent record aligned with the actual sandbox entity. The shipped
                // registry's Type IXB profile maps to the game's closest early Type IX entity,
                // Type IXA, because vanilla/DLC data does not define a Type IXB entity at all.
                pending.hullVariantId = resolvedHull;

                SandboxDock dock = FindDock(pending.destinationPortId) as SandboxDock;
                if (dock == null) dock = _playerShip.CurrentDock as SandboxDock;
                if (dock == null) return null;
                if (_playerShip.CurrentDock == dock) _lastPlayerDock = dock;
                Vector2 position = ReturnApproach(dock, dock.Position);
                if (!IsFinite(position))
                {
                    LastActionMessage = "Recruitment held: no navigable harbour approach is available at " + dock.Name + ".";
                    return null;
                }

                string worldId = EntityPrefix + pending.recruitmentId;
                PendingPresentations[worldId] = new CommissionedPresentation
                    { boatNumber = pending.boatNumber, emblemId = pending.emblemId, isPlayerCommanded = true };
                // Anonymous units retain the mod's stable WA id. A normal SandboxEntity asks the
                // historical-unit database for an identity later and can silently rename itself to
                // an unrelated U-number, breaking save rebinds and map command ownership.
                SandboxEntity entity = SandboxEntity.Create(worldId, blueprint);
                entity.IsAnonymousUnit = true;
                entity.Position = position;
                // Let the vanilla blueprint provide its map icon. "U-boat" is not a valid resource
                // key and caused both missing-asset spam and broken distant representations.
                entity.IconOverride = null;
                entity.IsPublic = false;

                string display = !string.IsNullOrEmpty(pending.boatNumber) ? pending.boatNumber : "BdU auxiliary U-boat";
                SandboxMobileGroup group = SandboxGroup.Create<SandboxMobileGroup>(LocalizedString.CreateUnlocalized(display), "Submarine", position, _wolfpack.Country);
                group.AddEntity(entity);
                NormalizeWolfpackIdentity(entity, display);
                group.DetectedByPlayer = false;
                group.IsGroupPublic = false;
                group.UpdateObservationList = true;
                // CharacterAI must exist before AddGroup. Sandbox.AddGroup calls StartGroup,
                // which caches both the AI reference and the has-AI flag used by off-screen
                // strategic updates. Adding it afterwards leaves the boat asleep until proximity
                // spawns the physical submarine.
                CharacterAI ai = group.gameObject.AddComponent<CharacterAI>();
                _sandbox.AddGroup(group);
                ApplyRecruitEmblem(entity, pending.emblemId);
                TrackGroup(group);

                bool docked = TryDockCommissionedWithPriority(dock, entity);
                if (!docked)
                {
                    long waitTicks = Math.Max(TimeSpan.FromMinutes(1).Ticks, pending.arrivalGameTicks - GameTimeAdapter.CurrentTicks);
                    ai.AddBehaviour(new GroupWaitBehaviour(ai, 20f, waitTicks, pending.recruitmentId));
                    ai.UpdateAI();
                }
                EnsureCommissionedPresentation(entity, pending.boatNumber, pending.emblemId);

                WolfpackAtlantikLog.Info("Spawned live recruited " + requestedHull + " as " + resolvedHull + " (" + worldId + ") "
                    + (docked ? "in a priority pen at " : "awaiting a compatible pen near ") + dock.Name + ".");
                return worldId;
            }
            catch (Exception ex)
            {
                WolfpackAtlantikLog.Error("Live recruitment spawn failed", ex);
                return null;
            }
        }

        private static bool TryResolveRecruitBlueprint(string requestedHull, out string resolvedHull, out EntityBlueprint blueprint)
        {
            resolvedHull = CanonicalEntityTypeName(requestedHull);
            blueprint = null;
            EntityType type = _entityTypes.GetEntityType(resolvedHull);
            if (!HasNativeNpcHull(type)) return false;
            blueprint = type.GetCountryBlueprint(_wolfpack.Country);
            return blueprint != null;
        }

        private static string CanonicalEntityTypeName(string hull)
        {
            if (string.IsNullOrEmpty(hull)) return "Type VIIC";
            switch (hull.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_").Replace("/", string.Empty))
            {
                case "type_iia": return "Type IIA";
                case "type_iib": return "Type IIA";
                case "type_iid": return "Type IID";
                case "type_viib": return "Type VIIB";
                case "type_viic": return "Type VIIC";
                case "type_viic41":
                case "type_viic_41": return "Type VIIC41";
                case "type_ixb": return "Type IXA";
                case "type_ixc": return "Type IXC";
                case "type_ixc40":
                case "type_ixc_40": return "Type IXC40";
                case "type_xiv": return "Type XIV";
                default: return hull;
            }
        }

        public static bool EnsureDockedAtPort(RecruitedBoatRecord boat)
        {
            if (boat == null || !ResolveBindings(false)) return false;
            EnsureAssignedPortWithinCapacity(boat);
            SandboxEntity entity = FindEntityForRecord(boat);
            SandboxEntity dockEntity = FindDock(boat.homePortId) ?? _lastPlayerDock;
            if (entity == null || dockEntity == null || entity.Group == null) return false;
            try
            {
                SandboxEntity currentDock = FindDockContaining(entity);
                if (currentDock != null && (currentDock == dockEntity
                    || string.Equals(currentDock.Name, dockEntity.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    boat.operationalState = BoatOperationalState.InPort;
                    EnsureCommissionedPresentation(entity, boat.boatNumber, boat.emblemId, boat.isPlayerCommanded);
                    return true;
                }
                SandboxMobileGroup group = entity.Group as SandboxMobileGroup;
                // A boat holding for a berth under a return order keeps its native return
                // behaviour; clearing it here every tick forced a fresh order each second.
                if (group != null && !(boat.orders != null && boat.orders.returnToPortRequested))
                {
                    WolfpackBoatAgent agent = GetAgent(group);
                    agent.ClearManaged();
                }
                if (currentDock != null) ClearDockPoint(entity);
                SandboxDock dock = dockEntity as SandboxDock;
                bool docked = dock != null && TryDockCommissionedWithPriority(dock, entity);
                if (!docked && currentDock is SandboxDock)
                {
                    entity.Position = currentDock.Position;
                    TryDockCommissionedWithPriority((SandboxDock)currentDock, entity);
                }
                boat.operationalState = docked ? BoatOperationalState.InPort : BoatOperationalState.Preparing;
                boat.isVirtualRecordOnly = false;
                RefreshRecordFromWorld(boat, entity);
                EnsureCommissionedPresentation(entity, boat.boatNumber, boat.emblemId, boat.isPlayerCommanded);
                return docked;
            }
            catch (Exception ex)
            {
                WolfpackAtlantikLog.Error("Failed to berth recruited boat", ex);
                boat.operationalState = BoatOperationalState.Preparing;
                return false;
            }
        }

        private static bool LaunchBoatFromPort(RecruitedBoatRecord boat, SandboxEntity entity)
        {
            if (IsUnavailableManagedHull(entity)) return false;
            if (boat == null || entity == null || !ResolveBindings(false)) return false;
            try
            {
                // A boat may be recovered from an incorrect stock-AI berth. Launch from the dock
                // that physically contains it, then route to its separately recorded home base.
                SandboxEntity dockEntity = FindDockContaining(entity) ?? FindDock(boat.homePortId) ?? _lastPlayerDock;
                bool alreadyMobile = entity.Group is SandboxMobileGroup && IsWolfpackGroup(entity.Group);
                if (dockEntity == null && !alreadyMobile) return false;
                if (alreadyMobile)
                {
                    // Recruits are represented by a private mobile group while their transfer timer
                    // runs. Releasing the wait behaviour and clearing the berth is enough to sail;
                    // creating another group here produced duplicate/foreign-looking submarines.
                    if (EnsureGroupAI(entity.Group as SandboxMobileGroup) == null) return false;
                    ClearDockPoint(entity, dockEntity as SandboxDock);
                    return true;
                }
                // The flag boat's heading says nothing about this harbour's shoreline. Native
                // spawned ships leave from their actual berth; distant ships start at a verified
                // water approach rather than an arbitrary perpendicular offset onto the quay.
                bool physicallyPresent = entity.SpawnState == SandboxEntity.EntitySpawnState.Spawned
                    && entity.SpawnedTransform != null;
                Vector2 position = physicallyPresent
                    ? _sandbox.SceneToEncounterSpace(entity.SpawnedTransform.position)
                    : ReturnApproach(dockEntity as SandboxDock, dockEntity.Position);
                if (!IsFinite(position))
                {
                    LogMissingBoatDiagnostic(boat, "departure", "no navigable harbour approach");
                    return false;
                }
                ClearDockPoint(entity, dockEntity as SandboxDock);
                string display = !string.IsNullOrEmpty(boat.boatNumber) ? boat.boatNumber : "BdU auxiliary U-boat";
                SandboxMobileGroup group = SandboxGroup.Create<SandboxMobileGroup>(
                    LocalizedString.CreateUnlocalized(display), "Submarine", position, _wolfpack.Country);
                entity.Position = position;
                group.AddEntity(entity);
                NormalizeWolfpackIdentity(entity, display);
                group.DetectedByPlayer = false;
                group.IsGroupPublic = false;
                group.UpdateObservationList = boat.isPlayerCommanded;
                CharacterAI ai = group.gameObject.AddComponent<CharacterAI>();
                _sandbox.AddGroup(group);
                TrackGroup(group);
                ai.UpdateAI();
                RefreshRecordFromWorld(boat, entity);
                return true;
            }
            catch (Exception ex)
            {
                WolfpackAtlantikLog.Error("Failed to launch allied U-boat from home base", ex);
                return false;
            }
        }

        private static SandboxEntity FindDockContaining(SandboxEntity entity)
        {
            if (entity == null) return null;
            List<SandboxDock> docks = GetAllLiveDocks();
            for (int d = 0; d < docks.Count; d++)
            {
                SandboxDock dock = docks[d];
                if (dock == null) continue;
                SandboxDock.DockPoint[] points = dock.DockPoints;
                for (int i = 0; points != null && i < points.Length; i++)
                    if (points[i].DockedEntity == entity) return dock;
            }
            return null;
        }

        private static void ClearDockPoint(SandboxEntity entity, SandboxDock explicitDock = null)
        {
            if (entity == null) return;
            if (explicitDock != null)
            {
                SandboxDock.DockPoint[] explicitPoints = explicitDock.DockPoints;
                for (int i = 0; i < explicitPoints.Length; i++)
                    if (explicitPoints[i].DockedEntity == entity) explicitPoints[i].DockedEntity = null;
            }
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SandboxEntity dockEntity in DocksById.Values)
            {
                SandboxDock dock = dockEntity as SandboxDock;
                if (dock == null || string.IsNullOrEmpty(dock.Name) || !seen.Add(dock.Name)) continue;
                SandboxDock.DockPoint[] points = dock.DockPoints;
                for (int i = 0; i < points.Length; i++)
                    if (points[i].DockedEntity == entity) points[i].DockedEntity = null;
            }
        }

        private static bool TryDockCommissionedWithPriority(SandboxDock dock, SandboxEntity entity)
        {
            if (dock == null || entity == null) return false;
            if (dock.TryDock(entity)) return true;

            // The harbour capacity is the number of compatible submarine pens, not the number
            // left over after ambient AI traffic. A commissioned skipper may displace a normal
            // AI unit, but never the player or another commissioned command.
            SandboxDock.DockPoint displacedPoint = null;
            SandboxEntity displaced = null;
            SandboxDock.DockPoint[] points = dock.DockPoints;
            for (int i = 0; points != null && i < points.Length; i++)
            {
                SandboxDock.DockPoint point = points[i];
                SandboxEntity occupant = point != null ? point.DockedEntity : null;
                if (point == null || (point.AllowedShipCategories & entity.Type.Category) == 0
                    || occupant == null || occupant.IsPlayerShip || IsCommissionedEntity(occupant)) continue;
                displacedPoint = point;
                displaced = occupant;
                break;
            }
            if (displacedPoint == null) return false;
            Vector2 holdingWater = ReturnApproach(dock, dock.Position);
            if (!IsFinite(holdingWater)) return false;

            displacedPoint.DockedEntity = null;
            if (!dock.TryDock(entity))
            {
                displacedPoint.DockedEntity = displaced;
                return false;
            }
            RehomeDisplacedAmbientBoat(displaced, dock, holdingWater);
            WolfpackAtlantikLog.Info("Priority-berthed commissioned " + entity.Name + " at " + dock.Name
                + "; an ambient AI unit yielded the submarine pen.");
            return true;
        }

        private static void RehomeDisplacedAmbientBoat(SandboxEntity entity, SandboxDock source, Vector2 holdingWater)
        {
            if (entity == null) return;
            HashSet<SandboxDock> seen = new HashSet<SandboxDock>();
            foreach (SandboxEntity value in DocksById.Values)
            {
                SandboxDock dock = value as SandboxDock;
                if (dock == null || dock == source || !seen.Add(dock)) continue;
                // A nearby naval pen is acceptable; iterating every port previously teleported
                // displaced U-51 from Germany to Santos on the other side of the Atlantic.
                if ((dock.Position - source.Position).sqrMagnitude > 625f) continue;
                if (_wolfpack != null && _wolfpack.Country != null && dock.Country != null
                    && dock.Country.GetRelationWith(_wolfpack.Country) == Country.Relation.Enemy) continue;
                if (dock.TryDock(entity))
                {
                    WolfpackAtlantikLog.Info("Moved displaced ambient AI unit " + entity.Name + " to a free pen at " + dock.Name + ".");
                    return;
                }
            }
            // Preserve surplus traffic in verified water outside this harbour. It is not added
            // to the campaign command list and has no Wolfpack agent or command UI ownership.
            SandboxMobileGroup holding = SandboxGroup.Create<SandboxMobileGroup>(
                LocalizedString.CreateUnlocalized(entity.Name), "Submarine", holdingWater, entity.Country);
            entity.Position = holdingWater;
            holding.AddEntity(entity);
            CharacterAI ai = holding.gameObject.AddComponent<CharacterAI>();
            _sandbox.AddGroup(holding);
            Ship ship = entity.SpawnedEntity as Ship;
            if (ship != null && ship.AI != null)
            {
                UBOAT.Game.Core.AI.ShipBehaviours.ShipDockBehaviour docking =
                    ship.AI.GetBehaviour<UBOAT.Game.Core.AI.ShipBehaviours.ShipDockBehaviour>();
                if (docking != null) ship.AI.RemoveBehaviourImmediate(docking);
            }
            ai.AddBehaviour(new SailToBehaviour(ai, 1f, holdingWater));
            ai.UpdateAI(true);
            WolfpackAtlantikLog.Info("No alternate AI pen was free for " + entity.Name
                + "; retained it in verified water outside " + source.Name + ".");
        }

        public static void ApplyOrders(RecruitedBoatRecord boat)
        {
            if (boat == null || !ResolveBindings(false)) return;
            if (boat.orders != null && boat.orders.returnToPortRequested) boat.intendedStation = null;
            SandboxEntity entity = FindEntity(boat.linkedWorldEntityId);
            if (IsUnavailableManagedHull(entity)) return;
            SandboxMobileGroup group = entity != null ? entity.Group as SandboxMobileGroup : null;
            if (group == null)
            {
                // A committed shore briefing is still an intention, not a claim the boat is at
                // sea. The real departure lifecycle applies it when the native berth releases.
                if (boat.orders != null && boat.orders.issuedGameTicks > 0 && _navalGrid != null
                    && !boat.orders.returnToPortRequested)
                {
                    NavalGridSquare intended = boat.orders.patrolMode == PatrolMode.AdjacentGrid
                        ? ResolveMissionRelativeSquare(boat.orders)
                        : !string.IsNullOrEmpty(boat.orders.patrolGrid) ? _navalGrid.GetSquare(boat.orders.patrolGrid) : null;
                    if (intended != null) RecordIntendedStation(boat, null, intended.Center);
                }
                return;
            }
            if (EnsureGroupAI(group) == null) return;

            try
            {
                WolfpackBoatAgent agent = GetAgent(group);
                if (agent.LastOrderSequence != boat.orderSequence)
                {
                    agent.ReturnRouteFailures = 0;
                    SetNavigationIssue(boat, null);
                }
                ReturnToPortBehaviour existingReturn = group.AI.GetBehaviour<ReturnToPortBehaviour>();
                bool movingOrder = boat.orders != null && (boat.orders.patrolMode == PatrolMode.ShadowPlayer
                    || boat.orders.patrolMode == PatrolMode.AdjacentGrid);
                if (boat.orders != null && boat.orders.returnToPortRequested
                    && agent.LastOrderSequence == boat.orderSequence && existingReturn != null) return;
                if (boat.orders != null && !boat.orders.returnToPortRequested
                    && agent.LastOrderSequence == boat.orderSequence && !movingOrder) return;
                if (!EnsureMobileNavigationReady(group))
                {
                    LogMissingBoatDiagnostic(boat, "order routing", "mobile navigation body is unavailable");
                    return;
                }
                GroupWaitBehaviour wait = group.AI.GetBehaviour<GroupWaitBehaviour>();
                if (wait != null) group.AI.RemoveBehaviourImmediate(wait);
                Vector2 target = ResolveOrderTarget(boat, group);
                // A moving formation target is re-issued at most every 10 s; at time compression
                // the flag boat moves 3 km per one-second tick, which re-planned every tick.
                bool movingFormationTarget = (boat.orders.patrolMode == PatrolMode.ShadowPlayer
                    || boat.orders.patrolMode == PatrolMode.AdjacentGrid)
                    && (agent.LastTarget - target).sqrMagnitude > 9f
                    && Time.realtimeSinceStartup >= agent.NextFormationRefresh;

                if (boat.orders.returnToPortRequested)
                {
                    TryApplyVanillaReturn(boat, entity, group, agent);
                    return;
                }

                if (agent.LastOrderSequence == boat.orderSequence && !movingFormationTarget) return;

                agent.ClearManaged();
                agent.LastOrderSequence = boat.orderSequence;
                agent.LastTarget = target;
                agent.NextFormationRefresh = Time.realtimeSinceStartup + 10f;

                if (boat.orders.patrolMode == PatrolMode.ShadowPlayer)
                {
                    agent.AddManaged(new SailToBehaviour(group.AI, 12f, target));
                }
                else
                {
                    WeightedNavalGridSquare[] patrol = ResolvePatrolSquares(boat, group);
                    float huntWeight = ConvoyHuntWeight(boat);
                    if (patrol.Length > 0) agent.AddManaged(new ConvoyHuntBehaviour(group.AI, huntWeight, patrol));
                    else agent.AddManaged(new SailToBehaviour(group.AI, huntWeight, target));
                }

                float engageWeight = EngageWeight(boat);
                if (engageWeight > 0f) agent.AddManaged(new EngageBehaviour(group.AI, engageWeight, PostureVelocity(boat.orders.posture)));
                group.AI.UpdateAI(true);
                RecordIntendedStation(boat, group, target);
                RefreshRecordFromWorld(boat, entity);
            }
            catch (Exception ex) { WolfpackAtlantikLog.Error("Failed to apply orders for " + (boat.boatNumber ?? boat.persistentBoatId), ex); }
        }

        private static bool TryApplyVanillaReturn(RecruitedBoatRecord boat, SandboxEntity entity,
            SandboxMobileGroup group, WolfpackBoatAgent agent)
        {
            try
            {
                // Use the game's actual return behaviour so an unspawned commissioned group is
                // advanced by the same background-AI path as vanilla traffic. Harmony guards below
                // pin it to the recorded home base and replace vanilla's destructive full-port end.
                SandboxDock preferredDock = ResolveReturnDock(boat, group.Position);
                if (preferredDock == null)
                {
                    LogMissingBoatDiagnostic(boat, "return-to-port route", "no friendly live dock is available");
                    SetNavigationIssue(boat, "Assigned home port is unavailable. Choose an available friendly base and reissue orders.");
                    return false;
                }
                agent.ClearManaged();
                ReturnToPortBehaviour returning = new ReturnToPortBehaviour(group.AI, 5000f, preferredDock);
                agent.AddManaged(returning);
                agent.LastOrderSequence = boat.orderSequence;
                agent.LastTarget = ReturnApproach(preferredDock, group.Position);
                ClearMissingBoatDiagnostics(BoatRecordId(boat));
                // Keep the 30 s gate armed after issuing. Removing it here let a boat whose
                // behaviour was cleared again on the next tick re-issue (and log) once a second.
                ReturnRouteRetryAfter[BoatRecordId(boat)] = Time.realtimeSinceStartup + 30f;
                group.AI.UpdateAI(true);
                RefreshRecordFromWorld(boat, entity);
                WolfpackAtlantikLog.Info("Native return-to-port order assigned to "
                    + (boat.boatNumber ?? BoatRecordId(boat)) + "; commissioned berth: "
                    + preferredDock.Name + ".");
                return true;
            }
            catch (Exception ex)
            {
                agent.LastOrderSequence = int.MinValue;
                LogMissingBoatDiagnostic(boat, "vanilla return-to-port AI",
                    ex.GetType().Name + (string.IsNullOrEmpty(ex.Message) ? string.Empty : ": " + ex.Message));
                return false;
            }
        }

        public static float DistanceToPlayerKm(RecruitedBoatRecord boat)
        {
            if (boat == null || !ResolveBindings(false)) return float.MaxValue;
            SandboxEntity entity = FindEntity(boat.linkedWorldEntityId);
            return entity != null && entity.Group != null
                ? (entity.Group.Position - PlayerStrategicPosition()).magnitude : boat.distanceFromPlayerKm;
        }

        public static void CenterMapOnBoat(RecruitedBoatRecord boat)
        {
            CenterCameraOnBoat(boat);
        }

        public static bool CenterCameraOnBoat(RecruitedBoatRecord boat)
        {
            Vector2 position; float heading;
            if (!ResolveBindings(false) || !TryGetVisibleBoatPosition(boat, out position, out heading))
            {
                LastActionMessage = "No permitted map fix. Request a radio report; vanilla-only mode provides its grid in a letter.";
                return false;
            }
            EnsureMapCommandRelay();
            _mapCommandRelay.QueueKnown(position, true);
            LastActionMessage = "Centring on " + boat.boatNumber + (WolfpackMapVisibility.Mode(_campaign, boat)
                == WolfpackMapVisibilityMode.Historical ? "'s last radio report." : ".");
            return true;
        }

        public static bool FocusBoatIfMapOpen(RecruitedBoatRecord boat)
        {
            if (_mainCamera == null || _mainCamera.CurrentMode != CameraMode.Map) return false;
            Vector2 position; float heading;
            if (!TryGetVisibleBoatPosition(boat, out position, out heading)) return false;
            FocusKnownPosition(position, false);
            return true;
        }

        public static bool RequestPositionReport(RecruitedBoatRecord boat, bool focusMap)
        {
            return ReceiveRequestedPosition(boat, focusMap);
        }
        public static void SweepPortCameraToBoat(RecruitedBoatRecord boat)
        {
            if (boat == null || !LivePortCameraAvailable)
            {
                LastActionMessage = "Port map view is available only while the flag boat is docked.";
                return;
            }
            LastActionMessage = "Strategic port view centred on " + (boat.boatNumber ?? "the allied U-boat") + ".";
            CenterMapOnBoat(boat);
        }
        public static void OpenDefaultUpgradeScreenForBoat(RecruitedBoatRecord boat)
        {
            LastActionMessage = LiveRefitAvailable
                ? "Use the allied physical-refit cards below. They use researched equipment, compatible slots and campaign payment without rebinding the flag boat's refit window."
                : "Allied refit requires both U-boats to be in port.";
        }
        public static void BeginSupplyExchangeMission(RecruitedBoatRecord boat)
        {
            if (!WolfpackPatrolAccess.CanCommandBoat(_campaign, boat))
            { LastActionMessage = "This boat is not under your current patrol authority."; return; }
            if (boat == null || !ResolveBindings(false) || IsPlayerDocked)
            {
                LastActionMessage = "Supply rendezvous can only be ordered while both U-boats are at sea.";
                return;
            }
            SandboxEntity entity = FindEntity(boat.linkedWorldEntityId);
            if (entity == null || boat.operationalState != BoatOperationalState.AtSea)
            {
                LastActionMessage = "The selected allied U-boat is not available at sea.";
                return;
            }
            if (boat.supplyRendezvousState == SupplyRendezvousState.Ready)
            {
                LastActionMessage = "Transfer manifest ready: exchange fuel or torpedoes below.";
                return;
            }
            if (boat.supplyRendezvousState == SupplyRendezvousState.None)
            {
                boat.ordersBeforeSupplyRendezvous = CloneOrders(boat.orders);
                boat.supplyRendezvousRequestedTicks = GameTimeAdapter.CurrentTicks;
            }
            boat.supplyRendezvousState = SupplyRendezvousState.EnRoute;
            boat.supplyRendezvousNotified = false;
            boat.orders.supplyExchangeRequested = true;
            boat.orders.returnToPortRequested = false;
            boat.orders.patrolMode = PatrolMode.ShadowPlayer;
            // A supply rendezvous is the one shadow order that must close directly on the
            // flag boat rather than retain the boat's standing formation station.
            boat.orders.patrolOffsetX = 0;
            boat.orders.patrolOffsetY = 0;
            boat.orders.shadowDistanceKm = 2f;
            boat.orders.issuedGameTicks = GameTimeAdapter.CurrentTicks;
            boat.orderSequence++;
            ApplyOrders(boat);
            if (_campaign != null) WolfpackSaveStore.Save(_campaign);
            LastActionMessage = (boat.boatNumber ?? "Allied U-boat") + " is closing for rendezvous (" + boat.distanceFromPlayerKm.ToString("0.0") + " km).";
        }

        public static void EndSupplyExchangeMission(RecruitedBoatRecord boat)
        {
            if (boat == null) return;
            if (!WolfpackPatrolAccess.CanCommandBoat(_campaign, boat))
            { LastActionMessage = "This boat is not under your current patrol authority."; return; }
            bool completed = boat.supplyRendezvousState == SupplyRendezvousState.Ready;
            BoatOrders resume = boat.ordersBeforeSupplyRendezvous != null ? CloneOrders(boat.ordersBeforeSupplyRendezvous) : new BoatOrders();
            resume.supplyExchangeRequested = false;
            resume.issuedGameTicks = GameTimeAdapter.CurrentTicks;
            boat.orders = resume;
            boat.ordersBeforeSupplyRendezvous = null;
            boat.supplyRendezvousState = SupplyRendezvousState.None;
            boat.supplyRendezvousNotified = false;
            boat.supplyRendezvousRequestedTicks = 0L;
            boat.orderSequence++;
            if (completed) ProgressionService.AddExperience(boat, 25, "a completed supply rendezvous");
            ApplyOrders(boat);
            if (_campaign != null) WolfpackSaveStore.Save(_campaign);
            LastActionMessage = "Rendezvous complete; previous standing orders restored.";
        }

        public static bool TransferFuel(RecruitedBoatRecord boat, bool alliedToPlayer)
        {
            string error;
            if (!ValidateReadyRendezvous(boat, out error)) { LastActionMessage = error; return false; }
            SandboxEntity entity = FindEntityForRecord(boat);
            SandboxMobileGroup mobile = entity != null ? entity.Group as SandboxMobileGroup : null;
            Fuel[] tanks = _playerShip.FuelSources;
            if (mobile == null || mobile is SandboxPlayerWolfpack || entity.IsPlayerShip
                || mobile.Entities.Count != 1 || entity.Blueprint == null || tanks == null || tanks.Length == 0)
            {
                LastActionMessage = "Separate native fuel stores could not be confirmed for both U-boats.";
                return false;
            }
            mobile.UpdateGroup();
            double[] before = new double[tanks.Length];
            double[] capacities = new double[tanks.Length];
            for (int i = 0; i < tanks.Length; i++)
            {
                if (tanks[i] == null || tanks[i].Data == null || tanks[i].Storage == null || tanks[i].Data.Mass <= 0f)
                {
                    LastActionMessage = "The flag boat's physical fuel tanks are unavailable.";
                    return false;
                }
                before[i] = tanks[i].DoubleCount;
                capacities[i] = tanks[i].Storage.ItemsMassLimit / tanks[i].Data.Mass;
            }
            float step = HasUpgrade(boat, "aw_support_3_maintenance_tender") ? 15f : (HasUpgrade(boat, "aw_support_2_transfer_rig") ? 10f : 5f);
            float oldAlliedFuel = mobile.Fuel;
            WolfpackFuelTransferPlan plan;
            if (!WolfpackFuelTransferPlan.TryCreate(before, capacities, oldAlliedFuel,
                (float)entity.Blueprint.Range, step / 100d, alliedToPlayer, out plan))
            {
                LastActionMessage = "Fuel transfer blocked by actual source reserves or receiving tank capacity.";
                return false;
            }
            try
            {
                for (int i = 0; i < tanks.Length; i++) tanks[i].DoubleCount = plan.PlayerCounts[i];
                mobile.Fuel = (float)plan.AlliedFuel;
            }
            catch (Exception ex)
            {
                mobile.Fuel = oldAlliedFuel;
                for (int i = 0; i < tanks.Length; i++) tanks[i].DoubleCount = before[i];
                LastActionMessage = "Fuel transfer failed; both U-boats' reserves were restored.";
                WolfpackAtlantikLog.Error("Native fuel-store transaction failed", ex);
                return false;
            }
            // Native PlayerShip.UpdateFuel derives this display from the same physical tanks.
            // Updating it immediately is cosmetic; the tanks and independent mobile fuel above
            // remain authoritative through the next update and save/load.
            if (_playerShip.Fuel != null) _playerShip.Fuel.Amount = plan.PlayerFraction;
            RefreshRecordFromWorld(boat, entity);
            WolfpackSkipperContactEvents.RecordFuelAid(_campaign, boat, alliedToPlayer, oldAlliedFuel, mobile.Fuel);
            if (_campaign != null) WolfpackSaveStore.Save(_campaign);
            WolfpackCommanderWindow.NotifyStateChanged();
            LastActionMessage = step.ToString("0") + "% fuel transferred " + (alliedToPlayer ? "to the flag boat." : "to " + (boat.boatNumber ?? "the allied U-boat") + ".");
            return true;
        }

        public static bool TransferTorpedo(RecruitedBoatRecord boat, bool alliedToPlayer)
        {
            string error;
            if (!ValidateReadyRendezvous(boat, out error)) { LastActionMessage = error; return false; }
            TorpedoStorage[] stores = _playerShip.GetComponentsInChildren<TorpedoStorage>(true);
            if (alliedToPlayer)
            {
                if (boat.torpedoesReady <= 0) { LastActionMessage = "The allied boat has no transferable torpedoes."; return false; }
                SandboxEquipmentData data = SelectTransferTorpedo();
                if (data == null || _resourceManager == null) { LastActionMessage = "No researched vanilla torpedo prefab is available for transfer."; return false; }
                GameObject created = _resourceManager.InstantiatePrefab("Equipment/" + data.Name);
                Equipment equipment = created != null ? created.GetComponent<Equipment>() : null;
                bool stored = false;
                if (equipment != null)
                    for (int i = 0; i < stores.Length && !stored; i++)
                        if (stores[i] != null && stores[i].CheckSpaceForItem(data)) stored = stores[i].StoreItem(equipment);
                if (!stored)
                {
                    if (created != null) UnityEngine.Object.Destroy(created);
                    LastActionMessage = "The flag boat's torpedo rooms have no compatible free slot.";
                    return false;
                }
                boat.torpedoesReady--;
            }
            else
            {
                if (boat.torpedoesReady >= boat.torpedoCapacity) { LastActionMessage = "The allied torpedo rooms are already full."; return false; }
                ShipEquipmentSlot donor = null;
                for (int i = 0; i < stores.Length && donor == null; i++)
                    for (int s = 0; stores[i] != null && s < stores[i].SlotCount; s++)
                        if (stores[i][s] != null && stores[i][s].AssignedItem is StoredTorpedo) { donor = stores[i][s]; break; }
                if (donor == null) { LastActionMessage = "The flag boat has no stored torpedo to transfer."; return false; }
                donor.DestroyItem();
                boat.torpedoesReady++;
            }
            if (_campaign != null) WolfpackSaveStore.Save(_campaign);
            LastActionMessage = "One torpedo transferred " + (alliedToPlayer ? "to the flag boat." : "to " + (boat.boatNumber ?? "the allied U-boat") + ".");
            return true;
        }

        public static IList<AlliedRefitChoice> GetRefitChoices(RecruitedBoatRecord boat)
        {
            List<AlliedRefitChoice> result = new List<AlliedRefitChoice>();
            if (!CanRefitBoat(boat)) return result;
            SandboxEntity entity = FindEntity(boat.linkedWorldEntityId);
            if (entity == null || entity.Blueprint == null || _equipmentDatabase == null) return result;
            SandboxEquipmentData[] all = _equipmentDatabase.GetEquipmentDirect();
            EntityBlueprintSlot[] slots = entity.Blueprint.Slots;
            SandboxEquipment[] installed = entity.Equipment;
            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                EntityBlueprintSlot slot = slots[slotIndex];
                if (slot == null || slot.Type == SlotType.Cargo || string.IsNullOrEmpty(slot.EquipmentTag)) continue;
                SandboxEquipmentData current = installed != null && slotIndex < installed.Length && installed[slotIndex] != null ? installed[slotIndex].Data : null;
                List<SandboxEquipmentData> candidates = new List<SandboxEquipmentData>();
                for (int i = 0; i < all.Length; i++)
                {
                    SandboxEquipmentData data = all[i];
                    if (data == null || !data.Researched || data == current || !data.HasTag(slot.EquipmentTagID)) continue;
                    candidates.Add(data);
                }
                candidates.Sort(delegate(SandboxEquipmentData a, SandboxEquipmentData b)
                {
                    int price = a.Price.CompareTo(b.Price);
                    return price != 0 ? price : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });
                int start = Math.Max(0, candidates.Count - 4);
                for (int i = start; i < candidates.Count; i++)
                    result.Add(new AlliedRefitChoice
                    {
                        worldEntityId = boat.linkedWorldEntityId,
                        slotIndex = slotIndex,
                        slotName = !string.IsNullOrEmpty(slot.Name) ? slot.Name : slot.EquipmentTag,
                        currentName = current != null ? current.LocalizedName.ToString() : "Empty",
                        equipmentData = candidates[i],
                        displayName = candidates[i].LocalizedName.ToString(),
                        price = Math.Max(0, candidates[i].Price)
                    });
            }
            return result;
        }

        public static bool TryInstallRefit(RecruitedBoatRecord boat, AlliedRefitChoice choice)
        {
            if (boat == null || choice == null || choice.equipmentData == null || !CanRefitBoat(boat))
            {
                LastActionMessage = "Both U-boats must be together in port for an allied refit.";
                return false;
            }
            if (!string.Equals(choice.worldEntityId, boat.linkedWorldEntityId, StringComparison.Ordinal))
            {
                LastActionMessage = "The refit list is stale; reopen the selected boat.";
                return false;
            }
            SandboxEntity entity = FindEntity(boat.linkedWorldEntityId);
            if (entity == null || choice.slotIndex < 0 || choice.slotIndex >= entity.Blueprint.Slots.Length || !choice.equipmentData.Researched)
            {
                LastActionMessage = "The selected researched equipment is no longer available.";
                return false;
            }
            EntityBlueprintSlot slot = entity.Blueprint.Slots[choice.slotIndex];
            if (!choice.equipmentData.HasTag(slot.EquipmentTagID))
            {
                LastActionMessage = "That equipment is not compatible with the selected vanilla slot.";
                return false;
            }
            if (!GameEconomyAdapter.TrySpend(choice.price, 0))
            {
                LastActionMessage = "The campaign budget cannot cover this allied refit.";
                return false;
            }
            try
            {
                entity.SetEquipment(choice.slotIndex, choice.equipmentData, true, false);
                boat.physicalRefitHistory.Add(GameTimeAdapter.CurrentCampaignDate.ToString("yyyy-MM-dd") + " | " + choice.slotName + " | " + choice.equipmentData.Name);
                if (_campaign != null) WolfpackSaveStore.Save(_campaign);
                LastActionMessage = choice.displayName + " installed in " + choice.slotName + " aboard " + (boat.boatNumber ?? "the allied U-boat") + ".";
                WolfpackNativeUI.ShowToast(LastActionMessage);
                return true;
            }
            catch (Exception ex)
            {
                GameEconomyAdapter.Refund(choice.price, 0);
                WolfpackAtlantikLog.Error("Allied physical refit failed", ex);
                LastActionMessage = "Refit failed safely; the campaign payment was refunded.";
                return false;
            }
        }

        public static bool CanRefitBoat(RecruitedBoatRecord boat)
        {
            if (boat == null || !LiveRefitAvailable || boat.operationalState != BoatOperationalState.InPort) return false;
            SandboxEntity entity = FindEntity(boat.linkedWorldEntityId);
            if (entity == null || !IsDocked(entity)) return false;
            SandboxEntity playerDock = _playerShip != null && _playerShip ? _playerShip.CurrentDock : null;
            return playerDock != null && entity.Group != null && (entity.Group.Position - playerDock.Position).magnitude <= 5f;
        }

        public static float PlayerFuelPercent
        {
            get
            {
                if (!ResolveBindings(false) || _playerShip.Fuel == null || (float)_playerShip.Fuel.Capacity <= 0f) return 0f;
                return (float)(_playerShip.Fuel.Amount / (float)_playerShip.Fuel.Capacity * 100.0);
            }
        }

        public static int PlayerStoredTorpedoes
        {
            get
            {
                if (!ResolveBindings(false)) return 0;
                int count = 0;
                TorpedoStorage[] stores = _playerShip.GetComponentsInChildren<TorpedoStorage>(true);
                for (int i = 0; i < stores.Length; i++)
                    for (int s = 0; stores[i] != null && s < stores[i].SlotCount; s++)
                        if (stores[i][s] != null && stores[i][s].AssignedItem is StoredTorpedo) count++;
                return count;
            }
        }

        public static bool IsWolfpackGroup(SandboxGroup group)
        {
            if (group == null || !(group is SandboxMobileGroup) || group is SandboxPlayerWolfpack) return false;
            if (group.GetComponent<WolfpackBoatAgent>() != null) return true;
            for (int i = 0; i < group.Entities.Count; i++)
                if (group.Entities[i] != null && !string.IsNullOrEmpty(group.Entities[i].Name) && group.Entities[i].Name.StartsWith(EntityPrefix, StringComparison.Ordinal)) return true;
            return group.Entities.Count == 1 && LooksLikeLegacyWolfpackEntity(group.Entities[0]);
        }

        private static bool LooksLikeLegacyWolfpackEntity(SandboxEntity entity)
        {
            if (entity == null || entity.Group == null || !(entity.Group is SandboxMobileGroup)
                || entity.Group is SandboxPlayerWolfpack || entity.Group.Entities == null
                || entity.Group.Entities.Count != 1) return false;
            string display = entity.Group.DisplayName != null ? entity.Group.DisplayName.ToString() : string.Empty;
            return !entity.IsPublic && !entity.Group.UpdateObservationList
                && string.Equals(entity.IconOverride, "U-boat", StringComparison.OrdinalIgnoreCase)
                && display.StartsWith("U-", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(display, entity.Name, StringComparison.OrdinalIgnoreCase);
        }

        // The native EntityMarkersManager belongs to the map UI and is normally inactive while
        // the map is closed. FindObjectOfType<T>() ignores inactive objects, which previously
        // caused one expensive global lookup per commissioned boat on every presentation pass.
        // Cache the already-loaded inactive Main Scene manager once, with a one-second negative
        // retry if it genuinely does not exist. Callers that would mutate map UI must separately
        // require gameObject.activeInHierarchy before touching its marker dictionary or prefabs.
        private static EntityMarkersManager ResolveEntityMarkerManager(bool force)
        {
            if (_entityMarkerManager != null && _entityMarkerManager) return _entityMarkerManager;

            float now = Time.realtimeSinceStartup;
            if (!force && now < _nextEntityMarkerManagerResolveTime) return null;
            _nextEntityMarkerManagerResolveTime = now + 1f;

            EntityMarkersManager resolved = null;
            try { resolved = UnityEngine.Object.FindObjectOfType<EntityMarkersManager>(); }
            catch { }

            if (resolved == null || !resolved)
            {
                try
                {
                    EntityMarkersManager[] all = UnityEngine.Resources.FindObjectsOfTypeAll<EntityMarkersManager>();
                    EntityMarkersManager mainSceneFallback = null;
                    EntityMarkersManager loadedSceneFallback = null;
                    if (all != null)
                    {
                        for (int i = 0; i < all.Length; i++)
                        {
                            EntityMarkersManager candidate = all[i];
                            if (candidate == null || !candidate || candidate.gameObject == null) continue;
                            GameObject go = candidate.gameObject;
                            if (!go.scene.IsValid() || !go.scene.isLoaded) continue;

                            bool mainScene = string.Equals(go.scene.name, "Main Scene", StringComparison.Ordinal);
                            bool canonical = mainScene
                                && string.Equals(go.name, "Entity Markers", StringComparison.Ordinal)
                                && go.transform != null && go.transform.parent != null
                                && string.Equals(go.transform.parent.name, "World Canvas", StringComparison.Ordinal);
                            if (canonical)
                            {
                                resolved = candidate;
                                break;
                            }
                            if (mainScene && (mainSceneFallback == null || !mainSceneFallback))
                                mainSceneFallback = candidate;
                            if (loadedSceneFallback == null || !loadedSceneFallback)
                                loadedSceneFallback = candidate;
                        }
                    }
                    if (resolved == null || !resolved)
                        resolved = mainSceneFallback != null && mainSceneFallback ? mainSceneFallback : loadedSceneFallback;
                }
                catch { }
            }

            _entityMarkerManager = resolved != null && resolved ? resolved : null;
            if (_entityMarkerManager != null && _entityMarkerManager)
                _nextEntityMarkerManagerResolveTime = 0f;
            return _entityMarkerManager;
        }

        private static bool EntityMarkerManagerActive()
        {
            return _entityMarkerManager != null && _entityMarkerManager
                && _entityMarkerManager.gameObject != null
                && _entityMarkerManager.gameObject.activeInHierarchy;
        }

        private static bool ResolveBindings(bool force)
        {
            if (!force && Time.realtimeSinceStartup < _nextResolveTime) return _lastResolveSucceeded;
            _nextResolveTime = Time.realtimeSinceStartup + 1f;
            try
            {
                InjectionFramework injection = InjectionFramework.Instance;
                Sandbox sandbox = Sandbox.Instance ?? injection.GetInstance<Sandbox>();
                SandboxPlayerWolfpack wolfpack = injection.GetInstance<SandboxPlayerWolfpack>();
                PlayerShip player = injection.GetInstance<PlayerShip>();
                if (sandbox == null || !sandbox || wolfpack == null || !wolfpack || player == null || !player)
                {
                    _lastResolveSucceeded = false;
                    return false;
                }
                if (_sandbox != sandbox)
                {
                    if (_sandbox != null && _sandbox) _sandbox.GroupAdded -= OnGroupAdded;
                    _sandbox = sandbox;
                    _sandbox.GroupAdded += OnGroupAdded;
                    _wolfpack = wolfpack;
                    PhysicalEmblemAttempts.Clear();
                    RebuildCaches();
                }
                else _wolfpack = wolfpack;
                if (_playerShip != player)
                {
                    if (_playerShip != null && _playerShip)
                    {
                        _playerShip.FiredTorpedo -= OnPlayerFiredTorpedo;
                        _playerShip.Damaged -= OnPlayerDamaged;
                    }
                    _playerShip = player;
                    _playerShip.FiredTorpedo += OnPlayerFiredTorpedo;
                    _playerShip.Damaged += OnPlayerDamaged;
                }
                _entityTypes = ScriptableObjectSingleton.LoadSingleton<EntityTypesDatabase>();
                _equipmentDatabase = ScriptableObjectSingleton.LoadSingleton<EquipmentDatabase>();
                _resourceManager = ScriptableObjectSingleton.LoadSingleton<ResourceManager>();
                _navalGrid = injection.GetInstance<INavalGrid>();
                _worldNavMesh = ScriptableObjectSingleton.LoadSingleton<WorldNavMesh>();
                _mainCamera = injection.GetInstance<MainCamera>();
                _selectionManager = injection.GetInstance<ISelectionManager>();
                if (_markerManager == null || !_markerManager)
                    _markerManager = UnityEngine.Object.FindObjectOfType<GroupMarkersManager>();
                if (_entityMarkerManager == null || !_entityMarkerManager)
                    ResolveEntityMarkerManager(force);
                if (_playerShip.Docked && _playerShip.CurrentDock != null) _lastPlayerDock = _playerShip.CurrentDock;
                _lastResolveSucceeded = _entityTypes != null;
                return _lastResolveSucceeded;
            }
            catch
            {
                _lastResolveSucceeded = false;
                return false;
            }
        }

        private static void RebuildCaches()
        {
            MapRecords.Clear();
            PositionReportCooldown.Clear();
            WolfpackMissingDistantAssetDiagnostic.Reset();
            NativeMapEntities.Clear();
            _nextNativeContactRefresh = 0f;
            EntitiesById.Clear();
            DocksById.Clear();
            LiveDockSnapshot.Clear();
            _nextDockSnapshotRefresh = 0f;
            LoggedPortGridAudits.Clear();
            if (_approximateChartLand != null && _approximateChartLand) UnityEngine.Object.Destroy(_approximateChartLand);
            _approximateChartLand = null;
            LastObservedPositions.Clear();
            TrackedGroups.Clear();
            LastReportedHealth.Clear();
            LastReportedStates.Clear();
            LastContactReportTicks.Clear();
            ReturnApproaches.Clear();
            ReturnRouteRetryAfter.Clear();
            NavigationRecoveryProcessed.Clear();
            _nextNavigationRecovery = 0f;
            if (_sandbox == null || !_sandbox) return;
            List<SandboxGroup> groups = _sandbox.GetGroupsDirect();
            for (int i = 0; i < groups.Count; i++) TrackGroup(groups[i]);
        }

        private static void OnGroupAdded(SandboxGroup group) { TrackGroup(group); }

        private static void TrackGroup(SandboxGroup group)
        {
            if (group == null || !group || !TrackedGroups.Add(group.GetInstanceID())) return;
            group.EntityAdded += TrackEntity;
            group.EntityRemoved += UntrackEntity;
            group.Destroyed += UntrackGroup;
            group.ObservationAdded += OnCommissionedObservationAdded;
            bool managedLegacyGroup = IsWolfpackGroup(group);
            for (int i = 0; i < group.Entities.Count; i++)
            {
                SandboxEntity entity = group.Entities[i];
                TrackEntity(entity);
                // v0.4.8 and older entities could be renamed by SandboxEntity.RequestIdentity.
                // A WolfpackBoatAgent is mod-owned, so retain that entity in the cache even when
                // its old WA id has already been replaced with a historical U-number.
                if (managedLegacyGroup && entity != null && !string.IsNullOrEmpty(entity.Name))
                    EntitiesById[entity.Name] = entity;
            }
        }

        private static void UntrackGroup(SandboxGroup group)
        {
            if (group == null) return;
            int groupId = group.GetInstanceID();
            TrackedGroups.Remove(groupId);
            LastObservedPositions.Remove(groupId);
            group.EntityAdded -= TrackEntity;
            group.EntityRemoved -= UntrackEntity;
            group.Destroyed -= UntrackGroup;
            group.ObservationAdded -= OnCommissionedObservationAdded;
            List<string> remove = new List<string>();
            foreach (KeyValuePair<string, SandboxEntity> pair in EntitiesById)
                if (pair.Value != null && pair.Value.Group == group) remove.Add(pair.Key);
            for (int i = 0; i < remove.Count; i++) EntitiesById.Remove(remove[i]);
        }

        private static void TrackEntity(SandboxEntity entity)
        {
            if (entity == null || string.IsNullOrEmpty(entity.Name)) return;
            TrackNativeMapEntity(entity);
            if (entity.Name.StartsWith(EntityPrefix, StringComparison.Ordinal)
                || (entity.Group != null && IsWolfpackGroup(entity.Group)))
                EntitiesById[entity.Name] = entity;
            if (entity is SandboxDock)
            {
                SandboxEntity priorDock;
                if (!DocksById.TryGetValue(entity.Name, out priorDock) || priorDock != entity)
                    _nextDockSnapshotRefresh = 0f;
                DocksById[entity.Name] = entity;
                if (entity.Group != null)
                {
                    DocksById[entity.Group.Name] = entity;
                    DocksById[entity.Group.DisplayName.ToString()] = entity;
                }
            }
        }

        private static void UntrackEntity(SandboxEntity entity)
        {
            if (entity == null || string.IsNullOrEmpty(entity.Name)) return;
            NativeMapEntities.Remove(entity.Name);
            MapRecords.Remove(entity);
            EntitiesById.Remove(entity.Name);
            if (entity is SandboxDock)
            {
                DocksById.Remove(entity.Name);
                _nextDockSnapshotRefresh = 0f;
            }
        }

        private static void OnCommissionedObservationAdded(SandboxGroup observer, GroupObservation observation)
        {
            if (observer == null || observation == null || observation.Group == null || _campaign == null
                || _wolfpack == null || !_wolfpack) return;
            RecruitedBoatRecord boat = FindPlayerCommissionedBoat(observer);
            if (boat == null) return;

            SandboxGroup target = observation.Group;
            if (target == observer || target == _wolfpack || target.Country == null || _wolfpack.Country == null
                || target.Country.GetRelationWith(_wolfpack.Country) != Country.Relation.Enemy) return;

            string reportKey = BoatRecordId(boat) + "|" + target.GetInstanceID();
            long now = GameTimeAdapter.CurrentTicks;
            long previous;
            if (LastContactReportTicks.TryGetValue(reportKey, out previous)
                && now - previous < GameTimeAdapter.HoursToTicks(1f)) return;
            LastContactReportTicks[reportKey] = now;

            string contact = observation.PerceivedGroupName;
            if (string.IsNullOrEmpty(contact) || contact == "?")
                contact = target.DisplayName != null ? target.DisplayName.ToString() : "enemy contact";
            string grid = "unknown grid";
            if (_navalGrid != null)
            {
                NavalGridSquare square = _navalGrid.GetSquare(observation.LastKnownPosition, 3);
                if (square != null && !string.IsNullOrEmpty(square.Name)) grid = "grid " + square.Name;
            }

            GroupObservation playerPlot = null;
            if (HasUpgrade(boat, "aw_detection_2_contact_plot"))
            {
                playerPlot = _wolfpack.AddObservation(target,
                    GroupDetectionMethod.Headquarters | GroupDetectionMethod.Console);
                playerPlot.AddKnownData(observation);
                playerPlot.AddKnownData(GroupData.All, 0f, true);
                EnsureMapMarker(target, playerPlot);
            }

            PublishCommissionedReport(boat, FindCommissionedEntity(observer),
                "CONTACT REPORT  •  " + contact.ToUpperInvariant() + "  •  " + grid.ToUpperInvariant(),
                playerPlot ?? observation);
        }

        private static RecruitedBoatRecord FindPlayerCommissionedBoat(SandboxGroup group)
        {
            if (group == null || _campaign == null || _campaign.boats == null) return null;
            for (int e = 0; e < group.Entities.Count; e++)
            {
                SandboxEntity entity = group.Entities[e];
                if (entity == null) continue;
                for (int i = 0; i < _campaign.boats.Count; i++)
                {
                    RecruitedBoatRecord boat = _campaign.boats[i];
                    if (boat != null && boat.isPlayerCommanded
                        && string.Equals(boat.linkedWorldEntityId, entity.Name, StringComparison.OrdinalIgnoreCase))
                        return boat;
                }
            }
            return null;
        }

        private static SandboxEntity FindCommissionedEntity(SandboxGroup group)
        {
            if (group == null) return null;
            for (int i = 0; i < group.Entities.Count; i++)
            {
                SandboxEntity entity = group.Entities[i];
                string number;
                string emblem;
                bool playerCommanded;
                if (TryGetCommissionedPresentation(entity, out number, out emblem, out playerCommanded)
                    && playerCommanded) return entity;
            }
            return null;
        }

        private static void UpdateCommissionedReporting(RecruitedBoatRecord boat, SandboxEntity entity)
        {
            if (boat == null || !boat.isPlayerCommanded || entity == null) return;
            string id = BoatRecordId(boat);

            BoatOperationalState previousState;
            if (!LastReportedStates.TryGetValue(id, out previousState))
                LastReportedStates[id] = boat.operationalState;
            else if (previousState != boat.operationalState)
            {
                LastReportedStates[id] = boat.operationalState;
                PublishCommissionedReport(boat, entity, StatusReportText(boat), null);
            }

            Entity live = entity.SpawnedEntity;
            if (live == null || !live) return;
            float health = Mathf.Clamp01(live.Health);
            float previousHealth;
            if (!LastReportedHealth.TryGetValue(id, out previousHealth))
            {
                LastReportedHealth[id] = health;
                return;
            }
            if (health < previousHealth - 0.02f)
            {
                LastReportedHealth[id] = health;
                string severity = health <= 0.25f ? "HEAVY DAMAGE" : (health <= 0.6f ? "DAMAGE" : "MINOR DAMAGE");
                PublishCommissionedReport(boat, entity,
                    severity + "  •  HULL CONDITION " + Mathf.RoundToInt(health * 100f) + "%", null);
            }
            else if (health > previousHealth + 0.02f) LastReportedHealth[id] = health;
        }

        private static string StatusReportText(RecruitedBoatRecord boat)
        {
            switch (boat.operationalState)
            {
                case BoatOperationalState.AtSea: return "STATUS  •  UNDERWAY ON PATROL";
                case BoatOperationalState.Returning: return "STATUS  •  RETURNING TO " + GetHomePortDisplayName(boat).ToUpperInvariant();
                case BoatOperationalState.InPort: return "STATUS  •  BERTHED AT " + GetHomePortDisplayName(boat).ToUpperInvariant();
                case BoatOperationalState.Preparing: return "STATUS  •  PREPARING TO SAIL";
                case BoatOperationalState.Repairing: return "STATUS  •  UNDER REPAIR";
                case BoatOperationalState.Lost: return "STATUS  •  OVERDUE AND PRESUMED LOST";
                default: return "STATUS  •  AWAITING COMMISSION";
            }
        }

        private static void PublishCommissionedReport(RecruitedBoatRecord boat, SandboxEntity sender,
            string report, GroupObservation observation)
        {
            if (boat == null || !boat.isPlayerCommanded || sender == null || _playerShip == null || !_playerShip
                || _playerShip.SandboxEntity == null || string.IsNullOrEmpty(report)) return;
            try
            {
                CustomMessage message = new CustomMessage(
                    WolfpackText.Unlocalized(report),
                    WolfpackText.Unlocalized("COMMISSIONED U-BOAT REPORT"), sender);
                if (observation != null) message.Observations = new GroupObservation[] { observation };
                _playerShip.SandboxEntity.Journal.AddMessage(message);
                WolfpackNativeUI.ShowCommissionedReport(boat, report);
            }
            catch (Exception ex)
            {
                WolfpackAtlantikLog.Error("Failed to publish commissioned U-boat report", ex);
            }
        }

        private static SandboxEntity FindEntity(string worldId)
        {
            if (string.IsNullOrEmpty(worldId)) return null;
            SandboxEntity entity;
            if (!EntitiesById.TryGetValue(worldId, out entity)) return null;
            if (entity == null || entity.Group == null || !entity.Group)
            {
                EntitiesById.Remove(worldId);
                return null;
            }
            return entity;
        }

        private static SandboxEntity FindEntityForRecord(RecruitedBoatRecord boat)
        {
            if (boat == null) return null;
            SandboxEntity direct = FindEntity(boat.linkedWorldEntityId);
            if (direct != null) return direct;
            if (_sandbox == null || !_sandbox) return null;

            List<SandboxGroup> groups = _sandbox.GetGroupsDirect();
            for (int i = 0; i < groups.Count; i++)
            {
                SandboxGroup group = groups[i];
                if (group == null || !group || group.Entities == null || group.Entities.Count == 0) continue;
                for (int e = 0; e < group.Entities.Count; e++)
                {
                    SandboxEntity member = group.Entities[e];
                    if (member != null && !string.IsNullOrEmpty(boat.linkedWorldEntityId)
                        && string.Equals(member.Name, boat.linkedWorldEntityId, StringComparison.OrdinalIgnoreCase))
                    {
                        TrackEntity(member);
                        return member;
                    }
                }
                if (string.IsNullOrEmpty(boat.boatNumber)) continue;
                string display = group.DisplayName != null ? group.DisplayName.ToString() : string.Empty;
                if (!string.Equals(display, boat.boatNumber, StringComparison.OrdinalIgnoreCase)) continue;
                if (group.GetComponent<WolfpackBoatAgent>() == null
                    && (group.Entities[0] == null || (!group.Entities[0].Name.StartsWith(EntityPrefix, StringComparison.Ordinal)
                        && !LooksLikeLegacyWolfpackEntity(group.Entities[0]))))
                    continue;
                TrackEntity(group.Entities[0]);
                return group.Entities[0];
            }
            // Some vanilla docks retain the berthed entity on a dock point without exposing it as
            // a normal mobile-group member. Search those points by the persistent WA identity too.
            List<SandboxDock> liveDocks = GetAllLiveDocks();
            for (int d = 0; d < liveDocks.Count; d++)
            {
                SandboxDock dock = liveDocks[d];
                if (dock == null) continue;
                SandboxDock.DockPoint[] points = dock.DockPoints;
                for (int p = 0; points != null && p < points.Length; p++)
                {
                    SandboxEntity berthed = points[p].DockedEntity;
                    if (berthed != null && !string.IsNullOrEmpty(boat.linkedWorldEntityId)
                        && string.Equals(berthed.Name, boat.linkedWorldEntityId, StringComparison.OrdinalIgnoreCase))
                    {
                        TrackEntity(berthed);
                        return berthed;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// UBOAT serialises its own sandbox groups, but groups created by a dynamically compiled
        /// mod can be absent after a save reload even though the mod's commissioned-boat record
        /// survives. Reconcile the stable WA identity first; if it is genuinely absent, recreate
        /// one physical hull from the saved type and location. The per-boat cooldown makes this a
        /// lifecycle repair, not a per-frame spawn loop.
        /// </summary>
        private static SandboxEntity RestoreMissingCommissionedBoat(RecruitedBoatRecord boat, string source)
        {
            if (boat == null || boat.isVirtualRecordOnly || _sandbox == null || !_sandbox
                || _wolfpack == null || !_wolfpack || _entityTypes == null) return null;

            string recordId = !string.IsNullOrEmpty(boat.persistentBoatId)
                ? boat.persistentBoatId : (boat.boatNumber ?? "unknown");
            float retryAfter;
            if (MissingBoatRepairAfter.TryGetValue(recordId, out retryAfter)
                && Time.realtimeSinceStartup < retryAfter) return null;
            // A destroyed group used to be recreated every five seconds. If a return behaviour
            // failed to berth, this became an endless hull/marker/AI rebuild loop and dominated
            // both the UI and simulation thread. Recovery is a lifecycle safeguard, not a poll.
            MissingBoatRepairAfter[recordId] = Time.realtimeSinceStartup + 90f;

            // Re-scan after the cooldown gate. A group may have finished deserialising between the
            // original lookup and this repair call.
            SandboxEntity existing = FindEntityForRecord(boat);
            if (existing != null)
            {
                MissingBoatRepairAfter.Remove(recordId);
                return existing;
            }

            string requestedHull = string.IsNullOrEmpty(boat.hullVariantId) ? "Type VIIC" : boat.hullVariantId;
            string resolvedHull;
            EntityBlueprint blueprint;
            if (!TryResolveRecruitBlueprint(requestedHull, out resolvedHull, out blueprint))
            {
                SandboxEntity retained = RestoreUnavailableManagedHull(boat);
                if (retained != null) return retained;
                LogMissingBoatDiagnostic(boat, source, "no compatible blueprint for " + requestedHull);
                return null;
            }

            bool expectsBerth = boat.operationalState == BoatOperationalState.InPort
                || boat.operationalState == BoatOperationalState.Preparing
                || boat.operationalState == BoatOperationalState.Repairing;
            if (expectsBerth) EnsureAssignedPortWithinCapacity(boat);
            SandboxEntity assignedDock = FindDock(boat.homePortId) ?? _lastPlayerDock;
            if (expectsBerth && assignedDock == null)
            {
                LogMissingBoatDiagnostic(boat, source, "assigned dock '" + (boat.homePortId ?? "<none>") + "' is not ready");
                return null;
            }

            try
            {
                Vector2 position = expectsBerth
                    ? ReturnApproach(assignedDock as SandboxDock, assignedDock.Position)
                    : Coordinates.ToWorldPosition(boat.lastKnownLatitude, boat.lastKnownLongitude);
                if (!expectsBerth && Math.Abs(boat.lastKnownLatitude) < 0.001
                    && Math.Abs(boat.lastKnownLongitude) < 0.001)
                {
                    Vector2 anchor = assignedDock != null ? assignedDock.Position : PlayerStrategicPosition();
                    position = FindSafeOffshoreSpawn(anchor, StableEntityHash(recordId));
                }
                Vector2 safePosition;
                if (!TryProjectToNavigableWater(position, 25f, out safePosition))
                {
                    LogMissingBoatDiagnostic(boat, source, "last position has no navigable water within 25 km; restoration deferred");
                    return null;
                }
                position = safePosition;

                string worldId = !string.IsNullOrEmpty(boat.linkedWorldEntityId)
                    ? boat.linkedWorldEntityId : EntityPrefix + recordId;
                SandboxEntity entity = SandboxEntity.Create(worldId, blueprint);
                entity.IsAnonymousUnit = true;
                entity.Position = position;
                entity.IconOverride = null;
                entity.IsPublic = false;

                string display = !string.IsNullOrEmpty(boat.boatNumber) ? boat.boatNumber : "BdU auxiliary U-boat";
                SandboxMobileGroup group = SandboxGroup.Create<SandboxMobileGroup>(
                    LocalizedString.CreateUnlocalized(display), "Submarine", position, _wolfpack.Country);
                group.AddEntity(entity);
                group.DetectedByPlayer = false;
                group.IsGroupPublic = false;
                group.UpdateObservationList = boat.isPlayerCommanded;
                CharacterAI ai = group.gameObject.AddComponent<CharacterAI>();
                _sandbox.AddGroup(group);
                if (!EnsureMobileNavigationReady(group))
                    throw new InvalidOperationException("Restored mobile group has no navigation body.");
                TrackGroup(group);
                NormalizeWolfpackIdentity(entity, display);
                ApplyRecruitEmblem(entity, boat.emblemId);
                ai.UpdateAI();

                bool docked = false;
                SandboxDock dock = assignedDock as SandboxDock;
                if (expectsBerth && dock != null)
                {
                    docked = TryDockCommissionedWithPriority(dock, entity);
                    boat.operationalState = docked ? BoatOperationalState.InPort : BoatOperationalState.Preparing;
                }
                else if (!expectsBerth)
                {
                    boat.operationalState = boat.operationalState == BoatOperationalState.Returning
                        ? BoatOperationalState.Returning : BoatOperationalState.AtSea;
                }

                boat.hullVariantId = resolvedHull;
                boat.linkedWorldEntityId = entity.Name;
                boat.isVirtualRecordOnly = false;
                TrackEntity(entity);
                RefreshRecordFromWorld(boat, entity);
                EnsureCommissionedPresentation(entity, display, boat.emblemId, boat.isPlayerCommanded);
                MissingBoatRepairAfter.Remove(recordId);
                ClearMissingBoatDiagnostics(recordId);
                if (_campaign != null) WolfpackSaveStore.Save(_campaign);
                WolfpackAtlantikLog.Info("Restored missing commissioned " + display + " (" + entity.Name + ") from "
                    + source + (docked ? " at " + GetHomePortDisplayName(boat) : " at its last reported position") + ".");
                WolfpackCommanderWindow.NotifyStateChanged();
                return entity;
            }
            catch (Exception ex)
            {
                LogMissingBoatDiagnostic(boat, source, "recreation threw " + ex.GetType().Name + ": " + ex.Message);
                return null;
            }
        }

        private static void LogMissingBoatDiagnostic(RecruitedBoatRecord boat, string source, string detail)
        {
            string recordId = boat != null && !string.IsNullOrEmpty(boat.persistentBoatId)
                ? boat.persistentBoatId : (boat != null ? boat.boatNumber : "unknown");
            string key = recordId + "|" + detail;
            if (!MissingBoatDiagnostics.Add(key)) return;
            WolfpackAtlantikLog.Info("Commissioned-boat reconciliation pending for "
                + (boat != null ? boat.boatNumber : "unknown U-boat") + " from " + source + ": " + detail
                + "; stable id=" + (boat != null ? boat.linkedWorldEntityId : "<none>") + ".");
        }

        private static void ClearMissingBoatDiagnostics(string recordId)
        {
            if (string.IsNullOrEmpty(recordId) || MissingBoatDiagnostics.Count == 0) return;
            List<string> remove = new List<string>();
            foreach (string key in MissingBoatDiagnostics)
                if (key.StartsWith(recordId + "|", StringComparison.Ordinal)) remove.Add(key);
            for (int i = 0; i < remove.Count; i++) MissingBoatDiagnostics.Remove(remove[i]);
        }

        private static bool NormalizeWolfpackIdentity(SandboxEntity entity, string commissionedName)
        {
            if (entity == null || _wolfpack == null || !_wolfpack) return false;
            bool changed = false;
            bool legacyRandomIdentity = !string.IsNullOrEmpty(entity.Name)
                && !entity.Name.StartsWith(EntityPrefix, StringComparison.Ordinal)
                && LooksLikeLegacyWolfpackEntity(entity);
            if (!entity.IsAnonymousUnit) { entity.IsAnonymousUnit = true; changed = true; }
            if (legacyRandomIdentity && SandboxEntityNameField != null)
            {
                string oldName = entity.Name;
                string stableName = EntityPrefix + "legacy-" + StableEntityHash(oldName + "|" + (commissionedName ?? string.Empty)).ToString("x8");
                SandboxEntityNameField.SetValue(entity, stableName);
                EntitiesById.Remove(oldName);
                EntitiesById[stableName] = entity;
                changed = true;
                WolfpackAtlantikLog.Info("Replaced legacy random identity " + oldName + " with stable id " + stableName
                    + " for " + (commissionedName ?? "recovered U-boat") + ".");
            }
            if (entity.Country != _wolfpack.Country) { entity.Country = _wolfpack.Country; changed = true; }
            if (entity.FalseFlagCountry != _wolfpack.Country) { entity.FalseFlagCountry = _wolfpack.Country; changed = true; }
            if (string.Equals(entity.IconOverride, "U-boat", StringComparison.OrdinalIgnoreCase))
            {
                entity.IconOverride = null;
                changed = true;
            }
            // Commissioned boats are exact BdU-controlled units, not uncertain contacts. Keep
            // both the entity and its independent mobile group public so the native entity-marker
            // manager treats them like the player's own submarine at every strategic-map range.
            entity.IsPublic = true;

            SandboxGroup group = entity.Group;
            if (group != null && group && IsWolfpackGroup(group))
            {
                if (group.GetComponent<WolfpackBoatAgent>() == null)
                    group.gameObject.AddComponent<WolfpackBoatAgent>();
                if (group.Country != _wolfpack.Country) { group.Country = _wolfpack.Country; changed = true; }
                if (!string.IsNullOrEmpty(commissionedName))
                    group.DisplayName = LocalizedString.CreateUnlocalized(commissionedName);
                group.IsGroupPublic = true;
            }
            return changed;
        }

        public static bool TryGetCommissionedPresentation(SandboxEntity entity, out string boatNumber, out string emblemId)
        {
            bool unused;
            return TryGetCommissionedPresentation(entity, out boatNumber, out emblemId, out unused);
        }

        public static bool TryGetCommissionedPresentation(SandboxEntity entity, out string boatNumber,
            out string emblemId, out bool isPlayerCommanded)
        {
            boatNumber = null;
            emblemId = null;
            isPlayerCommanded = true;
            if (entity == null || string.IsNullOrEmpty(entity.Name)) return false;

            CommissionedPresentation pendingPresentation;
            if (PendingPresentations.TryGetValue(entity.Name, out pendingPresentation))
            {
                boatNumber = pendingPresentation.boatNumber;
                emblemId = pendingPresentation.emblemId;
                isPlayerCommanded = pendingPresentation.isPlayerCommanded;
                return true;
            }
            if (_campaign == null) return false;
            for (int i = 0; i < _campaign.boats.Count; i++)
            {
                RecruitedBoatRecord boat = _campaign.boats[i];
                if (boat == null || !string.Equals(boat.linkedWorldEntityId, entity.Name, StringComparison.OrdinalIgnoreCase)) continue;
                boatNumber = boat.boatNumber;
                emblemId = boat.emblemId;
                isPlayerCommanded = boat.isPlayerCommanded;
                return true;
            }
            for (int i = 0; i < _campaign.pendingRecruitments.Count; i++)
            {
                PendingRecruitmentRecord pending = _campaign.pendingRecruitments[i];
                if (pending == null || !string.Equals(pending.spawnedWorldEntityId, entity.Name, StringComparison.OrdinalIgnoreCase)) continue;
                boatNumber = pending.boatNumber;
                emblemId = pending.emblemId;
                return true;
            }
            return false;
        }

        private static bool IsCommissionedEntity(SandboxEntity entity)
        {
            string boatNumber;
            string emblemId;
            return TryGetCommissionedPresentation(entity, out boatNumber, out emblemId);
        }

        private static void EnsureCommissionedPresentation(SandboxEntity entity, string boatNumber, string emblemId)
        {
            EnsureCommissionedPresentation(entity, boatNumber, emblemId, true);
        }

        private static void EnsureCommissionedPresentation(SandboxEntity entity, string boatNumber,
            string emblemId, bool isPlayerCommanded)
        {
            if (entity == null) return;
            RecruitedBoatRecord record = MapRecord(entity);
            bool persistent = record != null && WolfpackMapVisibility.AddsPlot(_campaign, record);
            // Publicity is NOT necessary for a mod-owned marker. Native observations must remain
            // available to LOS mode, but no radio report/publicity flag may grant a live position.
            bool ownsVisibility = IsWolfpackGroup(entity.Group) || (entity.Name != null && entity.Name.StartsWith(EntityPrefix, StringComparison.Ordinal));
            if (ownsVisibility) { entity.DisplayInUI = true; entity.IsPublic = false; }
            if (entity.Group != null && IsWolfpackGroup(entity.Group)) entity.Group.IsGroupPublic = false;
            if (entity.Group != null && IsWolfpackGroup(entity.Group))
                entity.Group.UpdateObservationList = isPlayerCommanded;
            if (persistent)
            {
                RemoveGroupMarker(entity.Group, true);
                EntityMarkerUI marker;
                EnsurePlayerStyleMarker(entity, boatNumber, out marker);
            }
            else
            {
                RemovePlayerStyleMarker(entity);
                RemoveGroupMarker(entity.Group, true);
            }
            EnsurePhysicalEmblem(entity, emblemId);
        }

        private static void EnsurePhysicalEmblem(SandboxEntity entity, string emblemIdentity)
        {
            if (entity == null || entity.SpawnedEntity == null || !entity.SpawnedEntity || _resourceManager == null
                || string.IsNullOrEmpty(emblemIdentity)) return;
            string badgeName = WolfpackVisualIdentity.VanillaBadgeName(emblemIdentity);
            string hostName = "WA_CommissionedEmblem_" + badgeName.Replace(" ", "_");
            Transform existing = entity.SpawnedEntity.transform.Find(hostName);
            if (existing != null) return;
            string attempt = entity.Name + "|" + entity.SpawnedEntity.GetInstanceID() + "|" + badgeName;
            if (PhysicalEmblemAttempts.Contains(attempt)) return;

            try
            {
                Resource<GameObject> resource;
                if (!CommissionedEmblemPrefabs.TryGetValue(badgeName, out resource))
                {
                    resource = _resourceManager.RetrieveAsset<GameObject>("Equipment/" + badgeName, null, true);
                    CommissionedEmblemPrefabs[badgeName] = resource;
                }
                GameObject prefab = resource.Valid ? resource.Asset : null;
                Emblem source = prefab != null ? prefab.GetComponent<Emblem>() : null;
                if (source == null || EmblemAffectedSlotsField == null) return;
                PhysicalEmblemAttempts.Add(attempt);

                string[] sourceSlots = EmblemAffectedSlotsField.GetValue(source) as string[];
                string[] slots = sourceSlots != null ? (string[])sourceSlots.Clone() : new string[0];
                Emblem.ReplacementData[] sourceData = source.ReplacementDataList;
                Emblem.ReplacementData[] replacements = new Emblem.ReplacementData[sourceData != null ? sourceData.Length : 0];
                for (int i = 0; i < replacements.Length; i++)
                {
                    Emblem.ReplacementData value = sourceData[i];
                    replacements[i] = new Emblem.ReplacementData
                    {
                        alias = value.alias,
                        replacedAlbedoGUID = value.replacedAlbedoGUID,
                        sourceAlbedoTransparencyGUID = value.sourceAlbedoTransparencyGUID,
                        preparsedAlbedoTransparencyGUID = value.preparsedAlbedoTransparencyGUID,
                        preparsedTextureCoords = value.preparsedTextureCoords
                    };
                }
                GameObject host = new GameObject(hostName);
                host.transform.SetParent(entity.SpawnedEntity.transform, false);
                Emblem.Attach(host, slots, replacements);
                WolfpackAtlantikLog.Info("Applied commissioned emblem " + badgeName + " to the physical hull " + entity.Name + ".");
            }
            catch (Exception ex) { WolfpackAtlantikLog.Error("Physical recruited-boat emblem failed for " + entity.Name, ex); }
        }

        private static uint StableEntityHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                string text = value ?? string.Empty;
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619u;
                }
                return hash;
            }
        }

        private static bool RecoverOrphanedWolfpackEntities(WolfpackCampaignState state)
        {
            if (state == null) return false;
            bool changed = false;
            HashSet<string> claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < state.boats.Count; i++)
                if (state.boats[i] != null && !string.IsNullOrEmpty(state.boats[i].linkedWorldEntityId))
                    claimed.Add(state.boats[i].linkedWorldEntityId);
            for (int i = 0; i < state.pendingRecruitments.Count; i++)
                if (state.pendingRecruitments[i] != null && !string.IsNullOrEmpty(state.pendingRecruitments[i].spawnedWorldEntityId))
                    claimed.Add(state.pendingRecruitments[i].spawnedWorldEntityId);

            List<SandboxEntity> candidates = new List<SandboxEntity>();
            foreach (KeyValuePair<string, SandboxEntity> pair in EntitiesById) candidates.Add(pair.Value);
            // A legacy entity may already have lost its WA name and its runtime ownership component.
            // Its commissioned group name remains different from the random historical identity,
            // while the v0.4.x spawn flags (private U-boat, manual observations) remain intact.
            if (_sandbox != null && _sandbox)
            {
                List<SandboxGroup> groups = _sandbox.GetGroupsDirect();
                for (int i = 0; i < groups.Count; i++)
                {
                    SandboxGroup group = groups[i];
                    if (group == null || !group || !(group is SandboxMobileGroup)
                        || group is SandboxPlayerWolfpack || group.Entities == null || group.Entities.Count != 1) continue;
                    SandboxEntity entity = group.Entities[0];
                    string display = group.DisplayName != null ? group.DisplayName.ToString() : string.Empty;
                    bool legacySignature = LooksLikeLegacyWolfpackEntity(entity);
                    if (legacySignature) candidates.Add(entity);
                }
            }

            HashSet<SandboxEntity> seen = new HashSet<SandboxEntity>();
            for (int c = 0; c < candidates.Count; c++)
            {
                SandboxEntity entity = candidates[c];
                if (entity == null || !seen.Add(entity) || claimed.Contains(entity.Name) || entity.Group == null) continue;
                bool owned = entity.Name.StartsWith(EntityPrefix, StringComparison.Ordinal)
                    || entity.Group.GetComponent<WolfpackBoatAgent>() != null
                    || LooksLikeLegacyWolfpackEntity(entity);
                if (!owned) continue;

                string display = entity.Group.DisplayName != null ? entity.Group.DisplayName.ToString() : string.Empty;
                if (string.IsNullOrEmpty(display) || display.IndexOf("U-", StringComparison.OrdinalIgnoreCase) < 0)
                    display = "U-" + Math.Abs(entity.Name.GetHashCode() % 900 + 100).ToString();
                NormalizeWolfpackIdentity(entity, display);
                SandboxEntity dock = FindDockContaining(entity);
                string hull = entity.Blueprint != null && entity.Blueprint.Type != null
                    ? CanonicalEntityTypeName(entity.Blueprint.Type.Name) : "Type VIIC";
                CaptainDefinition registryCaptain = FindRegistryCaptainForBoat(display);
                CaptainSnapshot recoveredCaptain = registryCaptain != null ? CaptainRegistry.Snapshot(registryCaptain.id) : null;
                RecruitedBoatRecord recovered = new RecruitedBoatRecord
                {
                    persistentBoatId = "recovered-" + unchecked((uint)entity.Name.GetHashCode()).ToString("x8"),
                    captainDefinitionId = registryCaptain != null ? registryCaptain.id : null,
                    captainSnapshot = recoveredCaptain ?? new CaptainSnapshot
                    {
                        displayName = "Commanding Officer, " + display,
                        biography = "B.d.U. command record recovered from the live flotilla after a legacy save-link failure.",
                        portraitId = "captain-recovered-" + unchecked((uint)entity.Name.GetHashCode()).ToString("x8"),
                        serviceBackground = "Recovered flotilla command",
                        temperament = "Unassessed",
                        commandStyleSummary = "Awaiting a complete B.d.U. personnel assessment."
                    },
                    hullVariantId = hull,
                    boatNumber = display,
                    emblemId = registryCaptain != null ? registryCaptain.suggestedEmblemId : null,
                    emblemDisplayName = registryCaptain != null ? registryCaptain.suggestedEmblemName : "Recovered emblem",
                    homePortId = dock != null ? dock.Name : RecoveryHomePort(state, registryCaptain),
                    operationalState = dock != null ? BoatOperationalState.InPort : BoatOperationalState.AtSea,
                    orders = WolfpackPreferences.CreateDefaultOrders(),
                    joinPlayerNextPatrol = dock != null,
                    linkedWorldEntityId = entity.Name,
                    availableAfterGameTicks = GameTimeAdapter.CurrentTicks,
                    isCustomCaptain = true,
                    isVirtualRecordOnly = false
                };
                RefreshRecordFromWorld(recovered, entity);
                state.boats.Add(recovered);
                claimed.Add(entity.Name);
                changed = true;
                WolfpackAtlantikLog.Info("Recovered orphaned live Wolfpack entity " + entity.Name
                    + " as " + display + " and restored German flotilla ownership.");
            }
            return changed;
        }

        private static CaptainDefinition FindRegistryCaptainForBoat(string boatNumber)
        {
            if (string.IsNullOrEmpty(boatNumber)) return null;
            foreach (CaptainDefinition captain in CaptainRegistry.All)
                if (captain != null && string.Equals(captain.suggestedBoatNumber, boatNumber, StringComparison.OrdinalIgnoreCase))
                    return captain;
            return null;
        }

        private static string RecoveryHomePort(WolfpackCampaignState state, CaptainDefinition registryCaptain)
        {
            string current = CurrentPortId;
            if (!string.IsNullOrEmpty(current)) return current;
            if (state != null && state.knownHomePorts != null)
            {
                for (int i = 0; i < state.knownHomePorts.Count; i++)
                {
                    KnownHomePortRecord known = state.knownHomePorts[i];
                    if (known != null && !string.IsNullOrEmpty(known.id)) return known.id;
                }
            }
            return registryCaptain != null ? registryCaptain.suggestedHomePort : null;
        }

        private static SandboxEntity FindDock(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            SandboxEntity dock;
            if (DocksById.TryGetValue(id, out dock) && dock != null) return dock;

            // Port groups live in UBOAT's public-group registry while the player is at sea. The
            // previous cache only followed ordinary groups, so a perfectly valid saved home-base
            // name such as "Wilhelmshaven Port" became unresolvable after leaving harbour.
            List<SandboxDock> liveDocks = GetAllLiveDocks();
            string wanted = NormalizePortName(id);
            for (int i = 0; i < liveDocks.Count; i++)
            {
                SandboxDock candidate = liveDocks[i];
                if (candidate == null) continue;
                string groupName = candidate.Group != null ? candidate.Group.Name : null;
                string displayName = candidate.Group != null && candidate.Group.DisplayName != null
                    ? candidate.Group.DisplayName.ToString() : null;
                if (string.Equals(candidate.Name, id, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(groupName, id, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(displayName, id, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(wanted) && (NormalizePortName(candidate.Name) == wanted
                        || NormalizePortName(groupName) == wanted || NormalizePortName(displayName) == wanted)))
                {
                    TrackEntity(candidate);
                    return candidate;
                }
            }
            return null;
        }

        private static List<SandboxDock> GetAllLiveDocks()
        {
            if (Time.realtimeSinceStartup < _nextDockSnapshotRefresh) return LiveDockSnapshot;
            List<SandboxDock> result = LiveDockSnapshot;
            result.Clear();
            HashSet<SandboxDock> seen = new HashSet<SandboxDock>();
            foreach (SandboxEntity value in DocksById.Values)
            {
                SandboxDock cached = value as SandboxDock;
                if (cached != null && cached.SpawnState != SandboxEntity.EntitySpawnState.Destroyed && seen.Add(cached)) result.Add(cached);
            }
            if (_sandbox == null || !_sandbox) return result;
            AddDocksFromGroups(_sandbox.GetGroupsDirect(), result, seen);
            AddDocksFromGroups(_sandbox.GetPublicGroupsDirect(), result, seen);
            _nextDockSnapshotRefresh = Time.realtimeSinceStartup + 5f;
            return result;
        }

        private static void AddDocksFromGroups(List<SandboxGroup> groups, List<SandboxDock> result,
            HashSet<SandboxDock> seen)
        {
            for (int i = 0; groups != null && i < groups.Count; i++)
            {
                SandboxGroup group = groups[i];
                if (group == null || !group) continue;
                SandboxDock liveDock = group.GetEntity<SandboxDock>();
                if (liveDock == null || !seen.Add(liveDock)) continue;
                result.Add(liveDock);
                TrackEntity(liveDock);
            }
        }

        /// <summary>The port-name key other systems must use to match the same base.</summary>
        public static string NormalizePortNamePublic(string value) { return NormalizePortName(value); }

        private static string NormalizePortName(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            char[] chars = value.ToLowerInvariant().ToCharArray();
            System.Text.StringBuilder result = new System.Text.StringBuilder(chars.Length);
            for (int i = 0; i < chars.Length; i++) if (char.IsLetterOrDigit(chars[i])) result.Append(chars[i]);
            string normalized = result.ToString();
            // "Port of Brest" and "Salamis Naval Base" must reduce to the same key as "Brest" and
            // "Salamis", or the historical base table silently never matches those docks.
            if (normalized.StartsWith("portof", StringComparison.Ordinal)) normalized = normalized.Substring(6);
            else if (normalized.StartsWith("port", StringComparison.Ordinal) && normalized.Length > 4) normalized = normalized.Substring(4);
            string[] suffixes = { "navalbase", "naval", "harbour", "harbor", "base", "port" };
            for (int i = 0; i < suffixes.Length; i++)
                if (normalized.Length > suffixes[i].Length && normalized.EndsWith(suffixes[i], StringComparison.Ordinal))
                { normalized = normalized.Substring(0, normalized.Length - suffixes[i].Length); break; }
            return normalized;
        }

        private static WolfpackBoatAgent GetAgent(SandboxMobileGroup group)
        {
            WolfpackBoatAgent agent = group.GetComponent<WolfpackBoatAgent>();
            return agent != null ? agent : group.gameObject.AddComponent<WolfpackBoatAgent>();
        }

        private static bool EnsureMapMarker(SandboxGroup group, GroupObservation observation)
        {
            if (group == null) return false;
            try
            {
                if (_markerManager == null || !_markerManager)
                    _markerManager = UnityEngine.Object.FindObjectOfType<GroupMarkersManager>();
                if (_markerManager == null || !_markerManager) return false;
                if (_markerManager.FindMarker(group) != null) return true;
                if (CreateMapMarkerMethod == null) return false;
                CreateMapMarkerMethod.Invoke(_markerManager, new object[] { group, observation });
                return _markerManager.FindMarker(group) != null;
            }
            catch (Exception ex)
            {
                WolfpackAtlantikLog.Error("Failed to create allied position-report marker", ex);
                return false;
            }
        }


        private static void RemovePlayerStyleMarker(SandboxEntity entity)
        {
            if (entity == null || EntityMarkersMapField == null) return;
            try
            {
                if (_entityMarkerManager == null || !_entityMarkerManager)
                    ResolveEntityMarkerManager(false);
                // Safe split: caching an inactive manager is allowed; mutating its UI is not.
                if (!EntityMarkerManagerActive()) return;
                IDictionary map = EntityMarkersMapField.GetValue(_entityMarkerManager) as IDictionary;
                EntityMarkerUI existing = map != null && map.Contains(entity) ? map[entity] as EntityMarkerUI : null;
                WolfpackPlayerStyleMarkerTag tag = existing != null && existing
                    ? existing.GetComponent<WolfpackPlayerStyleMarkerTag>() : null;
                if (existing != null && existing && tag != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                    RestoreNativeObservedMarker(entity);
                }
            }
            catch (Exception ex) { WolfpackAtlantikLog.Error("Failed to remove commissioned live map marker", ex); }
        }

        // Retire only our old synthetic radio plots. Never erase genuine native LOS contacts.
        private static void RemoveGroupMarker(SandboxGroup group, bool removeUnrequestedOnly)
        {
            if (group == null || !group) return;
            try
            {
                if (_markerManager == null || !_markerManager)
                    _markerManager = UnityEngine.Object.FindObjectOfType<GroupMarkersManager>();
                GroupMarkerUI marker = _markerManager != null && _markerManager ? _markerManager.FindMarker(group) : null;
                if (marker == null || !marker) return;
                bool isRadioPlot = marker.GetComponent<WolfpackTransientPositionPlot>() != null;
                if (isRadioPlot) UnityEngine.Object.DestroyImmediate(marker.gameObject);
            }
            catch (Exception ex) { WolfpackAtlantikLog.Error("Failed to remove commissioned observation marker", ex); }
        }

        private static bool EnsurePlayerStyleMarker(SandboxEntity entity, string boatNumber,
                                                    out EntityMarkerUI marker)
        {
            marker = null;
            if (entity == null || entity.Group == null || !entity.Group) return false;
            try
            {
                if (_entityMarkerManager == null || !_entityMarkerManager)
                    ResolveEntityMarkerManager(false);
                // Do not create/replace markers while the map hierarchy is inactive. Once the
                // player opens the map, the cached native manager becomes active in place and the
                // normal creation path runs immediately without another global search.
                if (!EntityMarkerManagerActive()
                    || CreatePlayerEntityMarkerMethod == null || PlayerShipMarkerPrefabField == null
                    || EntityMarkersMapField == null) return false;

                IDictionary map = EntityMarkersMapField.GetValue(_entityMarkerManager) as IDictionary;
                EntityMarkerUI existing = map != null && map.Contains(entity) ? map[entity] as EntityMarkerUI : null;
                WolfpackPlayerStyleMarkerTag existingTag = existing != null && existing
                    ? existing.GetComponent<WolfpackPlayerStyleMarkerTag>() : null;
                if (existingTag != null)
                {
                    marker = existing;
                    existingTag.EnsureRegistered();
                    WolfpackCommissionedMapMarker.Apply(marker);
                    return true;
                }

                // EntityMarkersManager destroys replaced markers asynchronously. Its old marker's
                // Destroyed callback then removes the dictionary key that the new marker just took,
                // causing a fresh duplicate every reconciliation tick. Complete this one UI-only
                // replacement first so the manager's native bookkeeping is empty before recreation.
                if (existing != null && existing) UnityEngine.Object.DestroyImmediate(existing.gameObject);

                EntityMarkerUI playerPrefab = PlayerShipMarkerPrefabField.GetValue(_entityMarkerManager) as EntityMarkerUI;
                if (playerPrefab == null || !playerPrefab) return false;
                WolfpackPlayerMapArtwork.UsePrefab(playerPrefab);
                CreatePlayerEntityMarkerMethod.Invoke(_entityMarkerManager, new object[] { entity, playerPrefab });

                map = EntityMarkersMapField.GetValue(_entityMarkerManager) as IDictionary;
                marker = map != null && map.Contains(entity) ? map[entity] as EntityMarkerUI : null;
                if (marker == null || !marker) return false;
                WolfpackPlayerStyleMarkerTag tag = marker.GetComponent<WolfpackPlayerStyleMarkerTag>();
                if (tag == null) tag = marker.gameObject.AddComponent<WolfpackPlayerStyleMarkerTag>();
                // The player-marker clone is commonly inactive while the map is closed. Unity does
                // not guarantee OnEnable in that state, yet vanilla will keep the exact allied marker
                // disabled until our late pass can revive it. Register explicitly to break that
                // inactive-marker cycle.
                tag.EnsureRegistered();
                WolfpackCommissionedMapMarker.Apply(marker);
                WolfpackAtlantikLog.Info("Registered player-style commissioned map marker for "
                    + (!string.IsNullOrEmpty(boatNumber) ? boatNumber : entity.Name) + ".");
                return true;
            }
            catch (Exception ex)
            {
                WolfpackAtlantikLog.Error("Failed to register player-style commissioned map marker", ex);
                marker = null;
                return false;
            }
        }

        private static void EnsureMapCommandRelay()
        {
            if (_mapCommandRelay != null && _mapCommandRelay) return;
            GameObject host = new GameObject("WA_MapCommandRelay");
            UnityEngine.Object.DontDestroyOnLoad(host);
            _mapCommandRelay = host.AddComponent<MapCommandRelay>();
        }

        public static void ClearMapSelection()
        {
            if (!ResolveBindings(false)) return;
            if (_selectionManager != null) _selectionManager.DeselectAll(true);
        }


        private static void RefreshRecordFromWorld(RecruitedBoatRecord boat, SandboxEntity entity)
        {
            if (boat == null || entity == null || entity.Group == null) return;
            NormalizeWolfpackIdentity(entity, boat.boatNumber);
            if (IsWolfpackGroup(entity.Group) && !string.IsNullOrEmpty(boat.boatNumber))
                entity.Group.DisplayName = LocalizedString.CreateUnlocalized(boat.boatNumber);
            SandboxMobileGroup travelling = entity.Group as SandboxMobileGroup;
            if (travelling != null && travelling.SpawnState == SpawnState.NotSpawned)
                travelling.UpdateGroup(); // Native time integration; no competing position simulator.
            Vector2 position = entity.Position;
            boat.distanceFromPlayerKm = (position - PlayerStrategicPosition()).magnitude;
            if (_navalGrid != null)
            {
                NavalGridSquare square = _navalGrid.GetSquare(position, 3);
                if (square != null) boat.lastKnownGrid = square.Name;
            }
            SandboxMobileGroup mobile = entity.Group as SandboxMobileGroup;
            if (mobile != null && entity.Blueprint != null)
            {
                float range = (float)entity.Blueprint.Range;
                if (range > 0.001f && !float.IsInfinity(mobile.Fuel) && !float.IsNaN(mobile.Fuel))
                    boat.fuelPercent = Mathf.Clamp01(mobile.Fuel / range) * 100f;
                float complement = (float)entity.Blueprint.CrewCount;
                if (complement > 0.001f)
                    boat.crewReadinessPercent = Mathf.Clamp01(entity.CrewCount / complement) * 100f;
            }
            Vector2 latLong = Coordinates.ToLatLong(position);
            boat.lastKnownLatitude = latLong.x;
            boat.lastKnownLongitude = latLong.y;
        }

        private static Vector2 PlayerStrategicPosition()
        {
            if (_playerShip != null && _playerShip)
            {
                if (_playerShip.Docked && _playerShip.CurrentDock != null)
                    return _playerShip.CurrentDock.Position;
                if (_playerShip.SandboxEntity != null && _playerShip.SandboxEntity.Group != null)
                    return _playerShip.SandboxEntity.Group.Position;
            }
            if (_lastPlayerDock != null) return _lastPlayerDock.Position;
            return _wolfpack != null && _wolfpack ? _wolfpack.Position : Vector2.zero;
        }

        private static bool UpdateVanillaReturn(RecruitedBoatRecord boat, SandboxEntity entity)
        {
            if (boat == null || entity == null || boat.orders == null || !boat.orders.returnToPortRequested) return false;
            SandboxDock currentDock = FindDockContaining(entity) as SandboxDock;
            if (currentDock != null)
            {
                return CompleteVanillaReturn(boat, entity, currentDock);
            }

            SandboxMobileGroup group = entity.Group as SandboxMobileGroup;
            if (group == null || EnsureGroupAI(group) == null) return false;
            if (!EnsureMobileNavigationReady(group))
            {
                LogMissingBoatDiagnostic(boat, "return-to-port update", "mobile navigation body is unavailable");
                return false;
            }
            WolfpackBoatAgent agent = GetAgent(group);
            SandboxDock destination = ResolveReturnDock(boat, group.Position);
            if (destination == null)
            {
                group.Velocity = 0f;
                SetNavigationIssue(boat, "Assigned home port is unavailable. Choose a friendly base and reissue orders.");
                return false;
            }
            Vector2 approach = ReturnApproach(destination, group.Position);
            if ((group.Position - approach).sqrMagnitude <= 9f || (group.Position - destination.Position).sqrMagnitude <= 9f)
            {
                // The last few kilometres are abstracted by vanilla docking too. Clear the travel
                // behaviour first, then let the priority-pen allocator preserve the commissioned
                // hull even when ambient traffic filled the harbour during its voyage.
                if (TryDockCommissionedWithPriority(destination, entity))
                {
                    agent.ClearManaged();
                    return CompleteVanillaReturn(boat, entity, destination);
                }
                group.Velocity = 0f;
                SetNavigationIssue(boat, "No compatible berth is free at the assigned home port; holding in navigable water.");
                return false;
            }

            ReturnToPortBehaviour activeReturn = group.AI.GetBehaviour<ReturnToPortBehaviour>();
            if (activeReturn != null && agent.LastOrderSequence == boat.orderSequence) return false;

            // Native AI behaviours are not guaranteed to survive every save/load boundary.
            // Reissue only when the expected return behaviour is genuinely absent, and never on
            // the one-second campaign tick that produced the old log/performance storm.
            string recordId = BoatRecordId(boat);
            float retryAfter;
            if (ReturnRouteRetryAfter.TryGetValue(recordId, out retryAfter)
                && Time.realtimeSinceStartup < retryAfter) return false;
            ReturnRouteRetryAfter[recordId] = Time.realtimeSinceStartup + 30f;
            agent.LastOrderSequence = int.MinValue;
            ApplyOrders(boat);
            return false;
        }

        public static bool RepairCommissionedReturnStart(ReturnToPortBehaviour behaviour)
        {
            SandboxMobileGroup group;
            RecruitedBoatRecord boat;
            SandboxEntity entity;
            SandboxDock destination;
            if (!TryResolveCommissionedReturn(behaviour, out group, out boat, out entity, out destination)) return true;
            if (!boat.orders.returnToPortRequested) { DiscardStaleReturn(group, behaviour, boat); return false; }
            try
            {
                if (ReturnDockField != null) ReturnDockField.SetValue(behaviour, destination);
                bool routed = TryRouteCommissionedReturn(boat, group, destination);
                // Retain vanilla's physical ShipDockBehaviour handoff, but do not execute its
                // straight-line home-port rejection / nearest-port substitution / random route.
                if (routed && ReturnSpawnedMethod != null)
                {
                    Action<SandboxGroup> onSpawn = (Action<SandboxGroup>)Delegate.CreateDelegate(
                        typeof(Action<SandboxGroup>), behaviour, ReturnSpawnedMethod);
                    group.Spawned -= onSpawn;
                    group.Spawned += onSpawn;
                    if (group.SpawnState == SpawnState.Spawned) onSpawn(group);
                }
            }
            catch (Exception ex)
            {
                LogMissingBoatDiagnostic(boat, "native return start", ex.GetType().Name + ": " + ex.Message);
                group.Velocity = 0f;
                SetNavigationIssue(boat, "Return navigation could not start. Reissue orders after checking the home port.");
            }
            return false;
        }

        // Returns true only for non-Wolfpack behaviours, allowing vanilla to run untouched.
        // Commissioned returns retain vanilla start/path/background selection while this safe
        // update prevents full harbours from deleting the hull and completes our persistent record.
        public static bool HandleCommissionedReturnUpdate(ReturnToPortBehaviour behaviour)
        {
            SandboxMobileGroup group;
            RecruitedBoatRecord boat;
            SandboxEntity entity;
            SandboxDock destination;
            if (!TryResolveCommissionedReturn(behaviour, out group, out boat, out entity, out destination)) return true;
            if (!boat.orders.returnToPortRequested) { DiscardStaleReturn(group, behaviour, boat); return false; }
            try
            {
                WolfpackBoatAgent agent = GetAgent(group);
                if (Time.realtimeSinceStartup < agent.NextReturnUpdate) return false;
                agent.NextReturnUpdate = Time.realtimeSinceStartup + 0.5f;
                if (destination == null)
                {
                    group.Velocity = 0f;
                    SetNavigationIssue(boat, "Assigned home port is unavailable. Choose a friendly base and reissue orders.");
                    return false;
                }
                if (ReturnDockField != null && behaviour.Dock != destination)
                    ReturnDockField.SetValue(behaviour, destination);

                SandboxDock currentDock = FindDockContaining(entity) as SandboxDock;
                if (currentDock != null)
                {
                    GetAgent(group).ClearManaged();
                    CompleteVanillaReturn(boat, entity, currentDock);
                    return false;
                }

                Vector2 approach = ReturnApproach(destination, group.Position);
                if ((group.Position - approach).sqrMagnitude <= 9f
                    || (group.Position - destination.Position).sqrMagnitude <= 9f)
                {
                    if (TryDockCommissionedWithPriority(destination, entity))
                    {
                        agent.ClearManaged();
                        CompleteVanillaReturn(boat, entity, destination);
                    }
                    else
                    {
                        group.Velocity = 0f;
                        SetNavigationIssue(boat, "No compatible berth is free at the assigned home port; holding in navigable water.");
                    }
                    return false;
                }

                if (group.Route.Count == 0)
                {
                    string recordId = BoatRecordId(boat);
                    float retryAfter;
                    if (!ReturnRouteRetryAfter.TryGetValue(recordId, out retryAfter)
                        || Time.realtimeSinceStartup >= retryAfter)
                    {
                        ReturnRouteRetryAfter[recordId] = Time.realtimeSinceStartup + 30f;
                        TryRouteCommissionedReturn(boat, group, destination);
                    }
                }
                if (group.Route.Count > 0 && string.IsNullOrEmpty(boat.navigationIssue))
                    group.Velocity = group.MaxVelocity * 0.65f;
            }
            catch (Exception ex)
            {
                LogMissingBoatDiagnostic(boat, "native return update", ex.GetType().Name + ": " + ex.Message);
            }
            return false;
        }

        private static bool TryResolveCommissionedReturn(ReturnToPortBehaviour behaviour,
            out SandboxMobileGroup group, out RecruitedBoatRecord boat, out SandboxEntity entity,
            out SandboxDock destination)
        {
            group = ReturnGroupField != null && behaviour != null
                ? ReturnGroupField.GetValue(behaviour) as SandboxMobileGroup : null;
            boat = null;
            entity = group != null && group.Entities != null && group.Entities.Count > 0 ? group.Entities[0] : null;
            destination = null;
            if (group == null || entity == null || _campaign == null) return false;
            for (int i = 0; i < _campaign.boats.Count; i++)
            {
                RecruitedBoatRecord candidate = _campaign.boats[i];
                // Resolve every Wolfpack-owned boat, not only those currently ordered home, so a
                // return behaviour restored from an older save cannot run vanilla's destructive path.
                if (candidate == null || candidate.orders == null) continue;
                if (!string.Equals(candidate.linkedWorldEntityId, entity.Name, StringComparison.OrdinalIgnoreCase)) continue;
                boat = candidate;
                break;
            }
            if (boat == null) return false;
            destination = ResolveReturnDock(boat, group.Position);
            return true;
        }

        // A ReturnToPortBehaviour deserialised with the game state while the record no longer asks
        // for a return: left alone, vanilla sails the boat home and, at a full pen, destroys the hull.
        private static void DiscardStaleReturn(SandboxMobileGroup group, ReturnToPortBehaviour behaviour, RecruitedBoatRecord boat)
        {
            try { if (group != null && group.AI != null && behaviour != null) group.AI.RemoveBehaviourImmediate(behaviour); } catch { }
            try { if (group != null) GetAgent(group).LastOrderSequence = int.MinValue; } catch { }
            LogMissingBoatDiagnostic(boat, "stale return behaviour", "discarded a return-to-port behaviour no current order requested; current orders will be reissued");
        }

        private static SandboxDock ResolveReturnDock(RecruitedBoatRecord boat, Vector2 reference)
        {
            SandboxDock preferred = boat != null ? FindDock(boat.homePortId) as SandboxDock : null;
            if (preferred != null && preferred.Enabled
                && (_wolfpack == null || preferred.Country == null || _wolfpack.Country == null
                    || preferred.Country.GetRelationWith(_wolfpack.Country) != Country.Relation.Enemy))
                return preferred;
            // The player's assigned base is an order, not a suggestion. An unavailable base
            // requires a visible hold; never silently send a Kiel boat to an unrelated port.
            return null;
        }

        private static Vector2 ReturnApproach(SandboxDock dock, Vector2 from)
        {
            if (dock == null || _worldNavMesh == null) return new Vector2(float.NaN, float.NaN);
            Vector2 cached;
            int dockId = dock.GetHashCode();
            if (ReturnApproaches.TryGetValue(dockId, out cached) && IsFinite(cached)
                && _worldNavMesh.IsOnNavMesh(cached)) return cached;

            // WorldNavMesh's exact strategic water layers are y=300.5, mask=96. Sampling the
            // nearest WATER once replaces 115 candidates, each with a complete route search.
            Vector2 approach;
            if (IsFinite(dock.IconPosition) && (dock.IconPosition - dock.Position).sqrMagnitude < 625f
                && TryProjectToNavigableWater(dock.IconPosition, 2f, out approach))
            {
                ReturnApproaches[dockId] = approach;
                return approach;
            }
            if (!TryProjectToNavigableWater(dock.Position, 25f, out approach))
                return new Vector2(float.NaN, float.NaN);
            ReturnApproaches[dockId] = approach;
            return approach;
        }

        private static bool TryProjectToNavigableWater(Vector2 position, float maximumDistance, out Vector2 projected)
        {
            projected = new Vector2(float.NaN, float.NaN);
            if (_worldNavMesh == null || !IsFinite(position)) return false;
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(new Vector3(position.x, 300.5f, position.y),
                out hit, maximumDistance, 96)) return false;
            projected = new Vector2(hit.position.x, hit.position.z);
            return IsFinite(projected) && _worldNavMesh.IsOnNavMesh(projected);
        }

        private static bool TryRouteCommissionedReturn(RecruitedBoatRecord boat, SandboxMobileGroup group, SandboxDock dock)
        {
            WolfpackBoatAgent agent = GetAgent(group);
            if (agent.ReturnRouteFailures >= 3) return false;
            Vector2 approach = ReturnApproach(dock, group.Position);
            bool sourceValid = _worldNavMesh != null && (_worldNavMesh.IsOnNavMesh(group.Position)
                || (WorldNavMesh.IsAreaNavMeshPresent && _worldNavMesh.IsOnAreaNavMesh(group.Position)));
            bool ready = sourceValid && IsFinite(approach) && EnsureMobileNavigationReady(group);
            NavMeshPathStatus result = ready ? group.Route.FindPathTo(approach) : NavMeshPathStatus.PathInvalid;
            if (result == NavMeshPathStatus.PathComplete && (group.Route.Count > 0
                || (group.Position - approach).sqrMagnitude <= 9f))
            {
                agent.LastTarget = approach;
                agent.ReturnRouteFailures = 0;
                boat.operationalState = BoatOperationalState.Returning;
                group.Velocity = group.MaxVelocity * 0.65f;
                SetNavigationIssue(boat, null);
                return true;
            }
            if (group.Route != null) group.Route.Clear();
            group.Velocity = 0f;
            agent.ReturnRouteFailures++;
            boat.operationalState = BoatOperationalState.AtSea;
            string reason = !sourceValid ? "Boat position is outside navigable water."
                : !IsFinite(approach) ? "Home port has no navigable approach." : "No complete water route to the assigned home port.";
            SetNavigationIssue(boat, reason + (agent.ReturnRouteFailures < 3
                ? " Retrying in 30 seconds (" + agent.ReturnRouteFailures + "/3)."
                : " Three attempts failed; change the base or reissue orders."));
            ReturnRouteRetryAfter[BoatRecordId(boat)] = Time.realtimeSinceStartup + 30f;
            LogMissingBoatDiagnostic(boat, "return route held", reason);
            return false;
        }

        private static void SetNavigationIssue(RecruitedBoatRecord boat, string message)
        {
            if (boat == null || string.Equals(boat.navigationIssue, message, StringComparison.Ordinal)) return;
            boat.navigationIssue = message;
            if (_campaign != null) WolfpackSaveStore.Save(_campaign);
            WolfpackCommanderWindow.NotifyStateChanged();
            if (!string.IsNullOrEmpty(message)) WolfpackNativeUI.ShowToast((boat.boatNumber ?? "U-boat") + ": " + WolfpackText.T(message));
        }

        private static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsNaN(value.y)
                && !float.IsInfinity(value.x) && !float.IsInfinity(value.y);
        }

        private static int StableSeed(string value)
        {
            unchecked
            {
                int hash = 17;
                string text = value ?? string.Empty;
                for (int i = 0; i < text.Length; i++) hash = hash * 31 + text[i];
                return hash;
            }
        }

        private static bool RecoverCommissionedNavigationOnce(WolfpackCampaignState state)
        {
            const int RecoveryVersion = 2;
            if (state == null || state.navigationRecoveryVersion >= RecoveryVersion) return false;
            if (_worldNavMesh == null || _sandbox == null || !_sandbox) return false;
            bool allResolved = true;
            bool changed = false;
            for (int i = 0; i < state.boats.Count; i++)
            {
                RecruitedBoatRecord boat = state.boats[i];
                if (boat == null) continue;
                string recoveryId = BoatRecordId(boat);
                if (NavigationRecoveryProcessed.Contains(recoveryId)) continue;
                SandboxEntity entity = FindEntityForRecord(boat);
                if (entity == null) { allResolved = false; continue; }
                if (entity.IsPlayerShip || entity.Group is SandboxPlayerWolfpack) continue;
                if (IsUnavailableManagedHull(entity))
                { NavigationRecoveryProcessed.Add(recoveryId); continue; }
                SandboxDock actualDock = FindDockContaining(entity) as SandboxDock;
                if (actualDock != null)
                {
                    // Native dock points are authoritative even where the coarse water mesh
                    // excludes the quays. Never reproject a valid berth or award repair credit.
                    if (boat.operationalState != BoatOperationalState.InPort || boat.orders.returnToPortRequested)
                    {
                        boat.operationalState = BoatOperationalState.InPort;
                        boat.orders.returnToPortRequested = false;
                        boat.navigationIssue = null;
                        boat.patrolDepartureRecorded = false;
                        changed = true;
                    }
                    NavigationRecoveryProcessed.Add(recoveryId);
                    continue;
                }
                SandboxMobileGroup group = entity != null ? entity.Group as SandboxMobileGroup : null;
                if (group == null) { allResolved = false; continue; }
                bool localWater = WorldNavMesh.IsAreaNavMeshPresent && _worldNavMesh.IsOnAreaNavMesh(group.Position);
                if (!_worldNavMesh.IsOnNavMesh(group.Position) && !localWater)
                {
                    Vector2 repaired;
                    if (!TryProjectToNavigableWater(group.Position, 25f, out repaired))
                    {
                        SandboxDock home = ResolveReturnDock(boat, group.Position);
                        repaired = ReturnApproach(home, group.Position);
                    }
                    if (!IsFinite(repaired))
                    {
                        allResolved = false;
                        SetNavigationIssue(boat, "Water navigation is not ready; commissioned recovery is deferred.");
                        continue;
                    }
                    group.SetPosition(repaired);
                    entity.Position = repaired;
                    boat.patrolDepartureRecorded = false;
                    boat.navigationIssue = null;
                    changed = true;
                    WolfpackAtlantikLog.Info("Navigation recovery reprojected "
                        + (boat.boatNumber ?? BoatRecordId(boat)) + " to valid strategic water.");
                }
                if (boat.operationalState == BoatOperationalState.InPort || boat.operationalState == BoatOperationalState.Preparing)
                {
                    SandboxDock home = ResolveReturnDock(boat, group.Position);
                    if (home != null && TryDockCommissionedWithPriority(home, entity))
                    {
                        GetAgent(group).ClearManaged();
                        boat.operationalState = BoatOperationalState.InPort;
                        boat.orders.returnToPortRequested = false;
                        boat.patrolDepartureRecorded = false;
                        boat.navigationIssue = null;
                        changed = true;
                        NavigationRecoveryProcessed.Add(recoveryId);
                        continue;
                    }
                    // A full port is an ordinary recoverable condition, not permission to
                    // teleport an unberthed entity onto the dock or delete somebody else's hull.
                    boat.operationalState = BoatOperationalState.AtSea;
                    boat.orders.returnToPortRequested = true;
                    changed = true;
                }
                if (boat.operationalState == BoatOperationalState.Returning || boat.orders.returnToPortRequested)
                {
                    boat.orders.returnToPortRequested = true;
                    WolfpackBoatAgent agent = GetAgent(group);
                    agent.ClearManaged();
                    agent.LastOrderSequence = int.MinValue;
                    agent.ReturnRouteFailures = 0;
                    ReturnRouteRetryAfter.Remove(BoatRecordId(boat));
                }
                NavigationRecoveryProcessed.Add(recoveryId);
            }
            if (!allResolved) return changed;
            state.navigationRecoveryVersion = RecoveryVersion;
            WolfpackAtlantikLog.Info("Applied commissioned navigation recovery migration v" + RecoveryVersion + ".");
            return true;
        }

        private static Vector2 FindSafeOffshoreSpawn(Vector2 anchor, uint seed)
        {
            float baseAngle = (seed % 360u) * Mathf.Deg2Rad;
            float baseRadius = 18f + (seed % 18u);
            for (int ring = 0; ring < 5; ring++)
            {
                float radius = baseRadius + ring * 7f;
                for (int step = 0; step < 16; step++)
                {
                    float angle = baseAngle + step * (Mathf.PI * 2f / 16f);
                    Vector2 sample = anchor + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
                    if (_worldNavMesh == null || _worldNavMesh.IsOnNavMesh(sample)) return sample;
                }
            }
            return _worldNavMesh != null ? _worldNavMesh.SnapWorld(anchor) : anchor;
        }

        private static bool EnsureMobileNavigationReady(SandboxMobileGroup group)
        {
            if (group == null || !group || MobileRigidBodyField == null) return false;
            try
            {
                if (EnsureGroupAI(group) == null) return false;
                Rigidbody2D body = MobileRigidBodyField.GetValue(group) as Rigidbody2D;
                if (body == null || !body) body = group.GetComponent<Rigidbody2D>();
                if (body == null || !body) body = group.gameObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = 0f;
                body.position = group.Position;
                MobileRigidBodyField.SetValue(group, body);
                return group.Route != null;
            }
            catch (Exception ex)
            {
                WolfpackAtlantikLog.Error("Failed to prepare commissioned mobile-group navigation", ex);
                return false;
            }
        }

        private static CharacterAI EnsureGroupAI(SandboxMobileGroup group)
        {
            if (group == null || !group) return null;
            try
            {
                CharacterAI ai = group.AI;
                if (ai == null || !ai) ai = group.GetComponent<CharacterAI>();
                if (ai == null || !ai) ai = group.gameObject.AddComponent<CharacterAI>();

                // Older saves and the previous recruitment path attached CharacterAI after
                // SandboxGroup.StartGroup. Repair the exact cached fields StartGroup owns so the
                // sandbox calls UpdateAI while the boat is off-screen.
                if (GroupAiField != null && group.AI != ai) GroupAiField.SetValue(group, ai);
                if (GroupHasAiField != null) GroupHasAiField.SetValue(group, true);
                ai.MoreBackgroundUpdates = true;
                ai.SingleBehaviourMode = true;
                return ai;
            }
            catch (Exception ex)
            {
                WolfpackAtlantikLog.Error("Failed to initialise commissioned background AI", ex);
                return null;
            }
        }

        private static bool CompleteVanillaReturn(RecruitedBoatRecord boat, SandboxEntity entity, SandboxDock actualDock)
        {
            if (boat == null || entity == null || actualDock == null) return false;
            if (boat.operationalState == BoatOperationalState.InPort && !boat.orders.returnToPortRequested) return false;
            bool completedPatrol = boat.patrolDepartureRecorded;
            boat.operationalState = BoatOperationalState.InPort;
            boat.orders.returnToPortRequested = false;
            boat.joinPlayerNextPatrol = false;
            boat.patrolDepartureRecorded = false;
            boat.navigationIssue = null;
            if (completedPatrol)
            {
                boat.patrolsCompleted++;
                ProgressionService.AddExperience(boat, 50, "a completed patrol");
                WolfpackResponsibilityProgression.RecordBoatPatrolCompleted(_campaign, boat);
            }
            string recordId = BoatRecordId(boat);
            ClearMissingBoatDiagnostics(recordId);
            ReturnRouteRetryAfter.Remove(recordId);
            RefreshRecordFromWorld(boat, entity);
            EnsureCommissionedPresentation(entity, boat.boatNumber, boat.emblemId, boat.isPlayerCommanded);
            string actualName = actualDock.Group != null ? actualDock.Group.DisplayName.ToString() : actualDock.Name;
            WolfpackNativeUI.ShowToast((boat.boatNumber ?? "U-boat") + " has berthed at " + actualName + ".");
            if (_campaign != null) WolfpackSaveStore.Save(_campaign);
            WolfpackCommanderWindow.NotifyStateChanged();
            WolfpackAtlantikLog.Info("Completed vanilla return-to-port for "
                + (boat.boatNumber ?? recordId) + " at " + actualDock.Name + ".");
            return true;
        }

        private static string BoatRecordId(RecruitedBoatRecord boat)
        {
            if (boat == null) return "unknown";
            return !string.IsNullOrEmpty(boat.persistentBoatId)
                ? boat.persistentBoatId : (!string.IsNullOrEmpty(boat.boatNumber) ? boat.boatNumber : "unknown");
        }

        private static bool IsDocked(SandboxEntity entity)
        {
            List<SandboxDock> docks = GetAllLiveDocks();
            for (int d = 0; d < docks.Count; d++)
            {
                SandboxDock dock = docks[d];
                if (dock == null) continue;
                SandboxDock.DockPoint[] points = dock.DockPoints;
                for (int i = 0; points != null && i < points.Length; i++) if (points[i].DockedEntity == entity) return true;
            }
            return false;
        }

        private static Vector2 ResolveOrderTarget(RecruitedBoatRecord boat, SandboxMobileGroup group)
        {
            if (boat.orders.patrolMode == PatrolMode.ShadowPlayer)
            {
                float spacing = Mathf.Clamp(boat.orders.shadowDistanceKm, 2f, 40f);
                return PlayerStrategicPosition() + new Vector2(Mathf.Clamp(boat.orders.patrolOffsetX, -4, 4) * spacing,
                    Mathf.Clamp(boat.orders.patrolOffsetY, -4, 4) * spacing);
            }
            if (boat.orders.patrolMode == PatrolMode.AdjacentGrid)
            {
                NavalGridSquare relative = ResolveMissionRelativeSquare(boat.orders);
                if (relative != null) return relative.Center;
            }
            if (!string.IsNullOrEmpty(boat.orders.patrolGrid))
            {
                try
                {
                    NavalGridSquare square = new NavalGridSquareReference(boat.orders.patrolGrid.Trim().ToUpperInvariant()).GetSquare();
                    if (square != null) return square.Center;
                }
                catch { }
            }
            return group.Position + group.Up.normalized * 25f;
        }

        private static WeightedNavalGridSquare[] ResolvePatrolSquares(RecruitedBoatRecord boat, SandboxMobileGroup group)
        {
            NavalGridSquare square = null;
            if (boat.orders.patrolMode == PatrolMode.AdjacentGrid)
                square = ResolveMissionRelativeSquare(boat.orders);
            if (square == null && !string.IsNullOrEmpty(boat.orders.patrolGrid))
            {
                try { square = new NavalGridSquareReference(boat.orders.patrolGrid.Trim().ToUpperInvariant()).GetSquare(); }
                catch { }
            }
            if (square == null && _navalGrid != null) square = _navalGrid.GetSquare(group.Position, 3);
            if (square == null) return new WeightedNavalGridSquare[0];
            return new[] { new WeightedNavalGridSquare(square, 1f) };
        }

        private static NavalGridSquare ResolveMissionRelativeSquare(BoatOrders orders)
        {
            if (orders == null) return null;
            NavalGridSquare center;
            string grid;
            string description;
            if (TryGetCurrentMissionPatrolSquare(out center, out grid, out description))
            {
                NavalGridSquare detailedCenter = _navalGrid != null ? _navalGrid.GetSquare(center.Center, 3) : null;
                if (detailedCenter != null) center = detailedCenter;
                NavalGridSquare relative = OffsetSquare(center, Mathf.Clamp(orders.patrolOffsetX, -4, 4),
                    Mathf.Clamp(orders.patrolOffsetY, -4, 4));
                if (relative != null) return relative;
            }
            if (!string.IsNullOrEmpty(orders.patrolGrid))
            {
                try { return new NavalGridSquareReference(orders.patrolGrid.Trim().ToUpperInvariant()).GetSquare(); }
                catch { }
            }
            return null;
        }

        private static float EngageWeight(RecruitedBoatRecord boat)
        {
            BoatOrders orders = boat.orders;
            if (orders.rulesOfEngagement == RulesOfEngagement.CeasefireCalculate || orders.rulesOfEngagement == RulesOfEngagement.CalculateOnly) return 0f;
            if (orders.rulesOfEngagement == RulesOfEngagement.HoldUntilPlayerHit && _lastPlayerHitTicks <= orders.issuedGameTicks) return 0f;
            if (orders.rulesOfEngagement == RulesOfEngagement.HoldUntilPlayerLaunch && _lastPlayerLaunchTicks <= orders.issuedGameTicks) return 0f;
            float weight = orders.posture == TacticalPosture.Aggressive ? 12f : (orders.posture == TacticalPosture.Defensive ? 3f : 7f);
            if (boat.unlockedUpgradeIds != null && boat.unlockedUpgradeIds.Contains("aw_attack_1_coordinated_spread")) weight *= 1.1f;
            if (HasUpgrade(boat, "aw_attack_2_firing_protocols")) weight *= 1.12f;
            if (HasUpgrade(boat, "aw_attack_3_pack_tactics")) weight *= 1.15f;
            return weight;
        }

        private static float ConvoyHuntWeight(RecruitedBoatRecord boat)
        {
            float weight = 6f;
            if (HasUpgrade(boat, "aw_detection_1_hydrophone_watch")) weight += 0.75f;
            if (HasUpgrade(boat, "aw_detection_2_contact_plot")) weight += 0.75f;
            if (HasUpgrade(boat, "aw_detection_3_wolfpack_grid")) weight += 1f;
            return weight;
        }

        private static void OnPlayerFiredTorpedo(Torpedo torpedo)
        {
            _lastPlayerLaunchTicks = GameTimeAdapter.CurrentTicks;
            WakeHeldBoats(RulesOfEngagement.HoldUntilPlayerLaunch);
        }

        private static void OnPlayerDamaged(DamageEvent damage)
        {
            _lastPlayerHitTicks = GameTimeAdapter.CurrentTicks;
            WakeHeldBoats(RulesOfEngagement.HoldUntilPlayerHit);
        }

        private static void WakeHeldBoats(RulesOfEngagement trigger)
        {
            if (_campaign == null) return;
            bool changed = false;
            for (int i = 0; i < _campaign.boats.Count; i++)
            {
                RecruitedBoatRecord boat = _campaign.boats[i];
                if (boat == null || boat.operationalState != BoatOperationalState.AtSea || boat.orders.rulesOfEngagement != trigger) continue;
                boat.orderSequence++;
                ApplyOrders(boat);
                changed = true;
            }
            if (changed)
            {
                WolfpackSaveStore.Save(_campaign);
                WolfpackNativeUI.ShowToast("Wolfpack engagement release transmitted.");
            }
        }

        private static bool ValidateReadyRendezvous(RecruitedBoatRecord boat, out string error)
        {
            error = null;
            if (boat == null || !ResolveBindings(false) || IsPlayerDocked || boat.operationalState != BoatOperationalState.AtSea)
            {
                error = "Both U-boats must be at sea for a transfer.";
                return false;
            }
            float distance = DistanceToPlayerKm(boat);
            if (boat.supplyRendezvousState != SupplyRendezvousState.Ready || distance > SupplyReadyDistance(boat) + 0.25f)
            {
                error = "Hold the rendezvous inside " + SupplyReadyDistance(boat).ToString("0.0") + " km before transferring stores.";
                return false;
            }
            return true;
        }

        private static SandboxEquipmentData SelectTransferTorpedo()
        {
            if (_equipmentDatabase == null || _resourceManager == null) return null;
            SandboxEquipmentData[] torpedoes = _equipmentDatabase.GetEquipmentByTag("Torpedo");
            SandboxEquipmentData selected = null;
            if (torpedoes == null) return null;
            for (int i = 0; i < torpedoes.Length; i++)
            {
                SandboxEquipmentData candidate = torpedoes[i];
                if (candidate == null || !candidate.Researched || !_resourceManager.AssetExists("Equipment/" + candidate.Name)) continue;
                if (selected == null || candidate.Price < selected.Price) selected = candidate;
            }
            return selected;
        }

        private static void ApplyRecruitEmblem(SandboxEntity entity, string emblemIdentity)
        {
            if (entity == null || entity.Blueprint == null || _equipmentDatabase == null) return;
            try
            {
                string badgeName = WolfpackVisualIdentity.VanillaBadgeName(emblemIdentity);
                SandboxEquipmentData badge = _equipmentDatabase.GetEquipmentByName(badgeName);
                if (badge == null) return;
                EntityBlueprintSlot[] slots = entity.Blueprint.Slots;
                for (int i = 0; slots != null && i < slots.Length; i++)
                {
                    EntityBlueprintSlot slot = slots[i];
                    if (slot == null || !badge.HasTag(slot.EquipmentTagID)) continue;
                    entity.SetEquipment(i, badge, true, false);
                    WolfpackAtlantikLog.Info("Applied vanilla insignia " + badgeName + " to " + entity.Name + ".");
                    return;
                }
                // NPC submarine blueprints normally have no equipment badge slot. Their scene
                // hull receives the same vanilla emblem material layer once it is instantiated.
                EnsurePhysicalEmblem(entity, emblemIdentity);
            }
            catch (Exception ex) { WolfpackAtlantikLog.Error("Vanilla recruited-boat insignia failed", ex); }
        }

        private static float SupplyReadyDistance(RecruitedBoatRecord boat)
        {
            if (HasUpgrade(boat, "aw_support_3_maintenance_tender")) return 3.5f;
            return HasUpgrade(boat, "aw_support_1_replenishment_drills") ? 3f : 2.5f;
        }

        private static bool HasUpgrade(RecruitedBoatRecord boat, string id)
        {
            return boat != null && boat.unlockedUpgradeIds != null && boat.unlockedUpgradeIds.Contains(id);
        }

        private static BoatOrders CloneOrders(BoatOrders source)
        {
            source = source ?? new BoatOrders();
            return new BoatOrders
            {
                posture = source.posture,
                patrolMode = source.patrolMode,
                patrolGrid = source.patrolGrid,
                patrolOffsetX = source.patrolOffsetX,
                patrolOffsetY = source.patrolOffsetY,
                shadowDistanceKm = source.shadowDistanceKm,
                convoyResponse = source.convoyResponse,
                rulesOfEngagement = source.rulesOfEngagement,
                returnToPortRequested = source.returnToPortRequested,
                supplyExchangeRequested = source.supplyExchangeRequested,
                issuedGameTicks = source.issuedGameTicks
            };
        }

        private static float PostureVelocity(TacticalPosture posture)
        {
            return posture == TacticalPosture.Aggressive ? 1f : (posture == TacticalPosture.Defensive ? 0.55f : 0.8f);
        }
    }

    public sealed class AlliedRefitChoice
    {
        public string worldEntityId;
        public int slotIndex;
        public string slotName;
        public string currentName;
        public string displayName;
        public int price;
        public SandboxEquipmentData equipmentData;
    }

    [UBOAT.Game.Core.Serialization.NonSerializedInGameState]
    public sealed class WolfpackBoatAgent : MonoBehaviour
    {
        private readonly List<AIBehaviour> _managed = new List<AIBehaviour>();
        public int LastOrderSequence = int.MinValue;
        public Vector2 LastTarget = new Vector2(float.NaN, float.NaN);
        public int ReturnRouteFailures;
        public float NextReturnUpdate;
        public float NextFormationRefresh;

        public void AddManaged(AIBehaviour behaviour)
        {
            if (behaviour == null) return;
            CharacterAI ai = GetComponent<CharacterAI>();
            if (ai == null) return;
            ai.AddBehaviour(behaviour);
            _managed.Add(behaviour);
        }

        public void ClearManaged()
        {
            CharacterAI ai = GetComponent<CharacterAI>();
            if (ai != null)
            {
                for (int i = _managed.Count - 1; i >= 0; i--)
                    try { ai.RemoveBehaviourImmediate(_managed[i]); } catch { }
                // CharacterAI behaviours are part of the native game state, but this agent is not.
                // After a reload the list above is empty while the AI still carries the behaviours
                // we issued before saving; a stale ReturnToPortBehaviour (profit 5000) then outranks
                // every new order and, at a full pen, vanilla destroys the hull. This component only
                // exists on Wolfpack-owned groups, so stripping our own behaviour types is safe.
                StripNative<ReturnToPortBehaviour>(ai);
                StripNative<SailToBehaviour>(ai);
                StripNative<ConvoyHuntBehaviour>(ai);
                StripNative<EngageBehaviour>(ai);
            }
            _managed.Clear();
            LastOrderSequence = int.MinValue;
        }

        private static void StripNative<T>(CharacterAI ai) where T : AIBehaviour
        {
            for (int guard = 0; guard < 40; guard++)
            {
                T stale = null;
                try { stale = ai.GetBehaviour<T>(); } catch { }
                if (stale == null) return;
                try { ai.RemoveBehaviourImmediate(stale); } catch { return; }
            }
        }
    }

    public static class PlayerCrewAdapter
    {
        public static bool LiveTransferAvailable
        {
            get
            {
                try
                {
                    return WorldBoatAdapter.IsPlayerDocked && InjectionFramework.Instance.GetInstance<PlayerCrew>() != null && ScriptableObjectSingleton.LoadSingleton<CharacterGenerator>() != null;
                }
                catch { return false; }
            }
        }

        public static PromotedOfficerCandidate SelectOfficerForAICommandPromotion(RecruitedBoatRecord boat)
        {
            return PromotedOfficerCandidate.CreateGeneric(boat);
        }

        public static string AddTransferredWatchOfficer(WatchOfficerRecord officer)
        {
            if (officer == null || !LiveTransferAvailable) return null;
            try
            {
                PlayerCrew crew = InjectionFramework.Instance.GetInstance<PlayerCrew>();
                PlayableCharacterData data = WolfpackArtwork.CreateTransferredOfficer(officer.portraitId, officer.displayName);
                if (data == null) return null;
                data.Name = officer.displayName;
                data.CharacterType = PlayableCharacterData.Type.Officer;
                PlayableCharacter added = crew.AddCharacter(data);
                if (added == null) return null;
                WolfpackAtlantikLog.Info("Transferred " + officer.displayName + " to the player crew as a watch officer.");
                return officer.officerId;
            }
            catch (Exception ex)
            {
                WolfpackAtlantikLog.Error("Live player-crew transfer failed", ex);
                return null;
            }
        }

        public static bool RemovePromotedWatchOfficer(string officerId)
        {
            if (string.IsNullOrEmpty(officerId)) return false;
            try
            {
                PlayerCrew crew = InjectionFramework.Instance.GetInstance<PlayerCrew>();
                if (crew == null || crew.Officers == null) return false;
                PlayableCharacter skipper = null;
                try { skipper = crew.Skipper; } catch { }
                for (int i = 0; i < crew.Officers.Length; i++)
                {
                    PlayableCharacter pc = crew.Officers[i];
                    if (pc == null || pc == skipper || pc.Data == null) continue;
                    if (!string.Equals(PromotionIdentity(pc.Data), officerId, StringComparison.Ordinal)) continue;
                    try { pc.StopCurrentAction(true); } catch { }
                    crew.RemoveCharacter(pc);
                    if (pc != null && pc.gameObject != null) UnityEngine.Object.Destroy(pc.gameObject);
                    WolfpackAtlantikLog.Info("Promoted player watch officer " + (pc.Data.Name ?? officerId) + " into independent AI command.");
                    return true;
                }
            }
            catch (Exception ex) { WolfpackAtlantikLog.Error("Player watch-officer removal failed", ex); }
            return false;
        }

        private static string PromotionIdentity(PlayableCharacterData data)
        {
            unchecked
            {
                uint h = 2166136261;
                string value = (data != null ? data.Name : "officer") + "|" + (data != null ? data.birthDateTicks : 0L) + "|" + (data != null ? data.enrollmentTicks : 0L);
                for (int i = 0; i < value.Length; i++) { h ^= value[i]; h *= 16777619; }
                return h.ToString("x8");
            }
        }
    }

    public static class GameBuffAdapter
    {
        private static readonly Dictionary<string, List<Modifier>> Active = new Dictionary<string, List<Modifier>>(StringComparer.Ordinal);
        private static PlayerShip _boundPlayer;

        public static bool LiveEffectsAvailable
        {
            get
            {
                try { return ResolvePlayer() != null; }
                catch { return false; }
            }
        }

        public static void ApplyOrRefresh(string sourceKey, PerkDefinition perk)
        {
            if (string.IsNullOrEmpty(sourceKey) || perk == null || perk.modifiers == null) return;
            PlayerShip player = ResolvePlayer();
            if (player == null || Active.ContainsKey(sourceKey)) return;
            List<Modifier> applied = new List<Modifier>();
            for (int i = 0; i < perk.modifiers.Length; i++)
            {
                StatModifier definition = perk.modifiers[i];
                if (definition == null || string.IsNullOrEmpty(definition.statKey)) continue;
                float conventionalScale = Mathf.Max(0.25f, definition.multiplier * (1f + definition.additive));
                string modifierName = "Wolfpack Atlantik/" + sourceKey + "/" + definition.statKey;
                switch (definition.statKey)
                {
                    case "fuel_consumption":
                        AddScale(player.DieselEngine != null ? player.DieselEngine.FuelUsage : null, modifierName, conventionalScale, applied);
                        break;
                    case "fuel_efficiency":
                        AddScale(player.DieselEngine != null ? player.DieselEngine.FuelUsage : null, modifierName, 1f / Mathf.Max(0.25f, definition.multiplier), applied);
                        break;
                    case "maximum_speed":
                        AddScale(player.DieselEngine != null ? player.DieselEngine.Power : null, modifierName + "/Diesel", conventionalScale, applied);
                        AddScale(player.ElectricEngine != null ? player.ElectricEngine.Power : null, modifierName + "/Electric", conventionalScale, applied);
                        break;
                    case "engine_reliability":
                        AddScale(player.EquipmentDamageProtection, modifierName, conventionalScale, applied);
                        break;
                }
            }
            // Track the source even when every conceptual modifier is AI-only; this preserves exact
            // source lifetime and prevents repeated work/log spam at the one-second proximity tick.
            Active[sourceKey] = applied;
        }

        public static void Remove(string sourceKey)
        {
            List<Modifier> modifiers;
            if (!Active.TryGetValue(sourceKey, out modifiers)) return;
            for (int i = modifiers.Count - 1; i >= 0; i--)
                try { if (modifiers[i] != null) modifiers[i].Remove(); } catch { }
            Active.Remove(sourceKey);
        }

        public static void ClearAll()
        {
            List<string> keys = new List<string>(Active.Keys);
            for (int i = 0; i < keys.Count; i++) Remove(keys[i]);
            _boundPlayer = null;
        }

        private static PlayerShip ResolvePlayer()
        {
            PlayerShip player = null;
            try { player = InjectionFramework.Instance.GetInstance<PlayerShip>(); }
            catch { }
            if (_boundPlayer != player)
            {
                ClearAll();
                _boundPlayer = player;
                RemovePersistedWolfpackModifiers(player);
            }
            return player != null && player ? player : null;
        }

        private static void AddScale(Parameter parameter, string name, float value, List<Modifier> output)
        {
            if (parameter == null || float.IsNaN(value) || float.IsInfinity(value)) return;
            Modifier modifier = parameter.GetScaleModifier(name) ?? parameter.AddScaleModifier(name);
            modifier.Value = value;
            modifier.Hidden = false;
            output.Add(modifier);
        }

        private static void RemovePersistedWolfpackModifiers(PlayerShip player)
        {
            if (player == null || !player) return;
            List<Parameter> parameters = new List<Parameter>();
            if (player.DieselEngine != null)
            {
                parameters.Add(player.DieselEngine.FuelUsage);
                parameters.Add(player.DieselEngine.Power);
            }
            if (player.ElectricEngine != null) parameters.Add(player.ElectricEngine.Power);
            parameters.Add(player.EquipmentDamageProtection);
            for (int p = 0; p < parameters.Count; p++)
            {
                Parameter parameter = parameters[p];
                if (parameter == null) continue;
                for (int i = parameter.ScaleModifiers.Count - 1; i >= 0; i--)
                {
                    Modifier modifier = parameter.ScaleModifiers[i];
                    if (modifier != null && modifier.Name != null && modifier.Name.StartsWith("Wolfpack Atlantik/", StringComparison.Ordinal))
                        parameter.RemoveScaleModifier(modifier);
                }
            }
        }
    }

    public static class EventWidgetAdapter
    {
        public static void ShowPerkActivated(RecruitedBoatRecord boat, PerkDefinition perk)
        {
            WolfpackAtlantikLog.Info("Perk active: " + (boat != null && boat.captainSnapshot != null ? boat.captainSnapshot.displayName : "Unknown") + " / " + (perk != null ? perk.displayName : "Unknown"));
            WolfpackNativeUI.ShowToast((perk != null ? perk.displayName : "Command effect") + " active near " + (boat != null ? boat.boatNumber : "allied U-boat") + ".");
        }

        public static void ShowCaptainTransfer(RecruitedBoatRecord boat, WatchOfficerRecord outgoing, CaptainSnapshot replacement)
        {
            WolfpackAtlantikLog.Info("Captain transfer: " + outgoing.displayName + " moved to player crew; " + replacement.displayName + " promoted aboard " + boat.boatNumber + ".");
            WolfpackNativeUI.ShowToast(outgoing.displayName + " joined your watch officers; " + replacement.displayName + " assumed command of " + boat.boatNumber + ".");
        }

        public static void ShowUrgentBduDefection(RecruitedBoatRecord boat, string message, bool replacementRequired)
        {
            WolfpackAtlantikLog.Info(message + (replacementRequired ? " [PORT RELIEF REQUIRED]" : string.Empty));
            WolfpackNativeUI.ShowToast(message + (replacementRequired ? " — port relief required." : string.Empty));
        }
    }
}
