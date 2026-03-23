using System.Reflection;
using UnityEngine;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Quests;
using Il2CppScheduleOne.Cartel;
using Il2CppScheduleOne.Police;

namespace MiniMap
{
    public class RadarController
    {
        private Dictionary<object, EntityBlip> activeBlips = new();
        private List<object> staleEntities = new();
        private float radarUpdateTimer = 0f;
        private Transform rotationRoot;

        // Reflection caching for Customer methods
        private static MethodInfo showOfferMethod;
        private static MethodInfo offerValidMethod;
        private static MethodInfo showDirectMethod;
        private static MethodInfo sampleValidMethod;

        public RadarController(Transform rotationRoot)
        {
            this.rotationRoot = rotationRoot;
            InitializeReflection();
        }

        private void InitializeReflection()
        {
            var type = typeof(Customer);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
            showOfferMethod = type.GetMethod("ShowOfferDealOption", flags);
            offerValidMethod = type.GetMethod("OfferDealValid", flags);
            showDirectMethod = type.GetMethod("ShowDirectApproachOption", flags);
            sampleValidMethod = type.GetMethod("SampleOptionValid", flags);
        }

        public void Update(float deltaTime)
        {
            radarUpdateTimer += deltaTime;
            if (radarUpdateTimer < MapConstants.RadarUpdateInterval) return;
            radarUpdateTimer = 0f;

            HashSet<object> currentEntities = GatherEntities();
            SyncBlips(currentEntities);
        }

        private HashSet<object> GatherEntities()
        {
            HashSet<object> entities = new();

            // 1. Gather Players & NPCs
            foreach (var officer in PoliceOfficer.Officers)
                if (officer.Health.Health > 0f && (officer.isVisible || officer.isInBuilding)) entities.Add(officer);

            foreach (var customer in Customer.UnlockedCustomers)
                if (customer.NPC.Health.Health > 0f && (customer.NPC.isVisible || customer.NPC.isInBuilding)) entities.Add(customer);

            foreach (var customer in Customer.LockedCustomers)
                if (customer.NPC.Health.Health > 0f && (customer.NPC.isVisible || customer.NPC.isInBuilding)) entities.Add(customer);

            foreach (var p in Player.PlayerList)
                if (p != null && p != Player.Local && p.Health != null && p.Health.IsAlive) entities.Add(p);

            if (NPCManager.InstanceExists)
            {
                foreach (var npc in NPCManager.NPCRegistry)
                {
                    if (npc is CartelGoon goon && goon.IsGoonSpawned && goon.Health.Health > 0f && (goon.isVisible || goon.isInBuilding))
                        entities.Add(goon);
                }
            }

            // 2. Gather POIs
            foreach (var quest in Quest.Quests)
            {
                if (quest == null || quest.State != EQuestState.Active) continue;
                if (quest is Contract contract && contract.Dealer != null) continue;

                foreach (var entry in quest.Entries)
                {
                    if (entry?.State == EQuestState.Active && entry.PoI != null)
                        entities.Add(entry.PoI);
                }
            }

            foreach (var drop in DeadDrop.DeadDrops)
            {
                if (drop.PoI != null && drop.Storage.ItemCount > 0) entities.Add(drop.PoI);
            }

            // 3. Gather Scheduled Deals
            foreach (var customer in Customer.UnlockedCustomers)
            {
                if (customer.IsAwaitingDelivery && customer.CurrentContract != null && customer.CurrentContract.Dealer == null)
                {
                    entities.Add(customer);
                    if (customer.CurrentContract.DeliveryLocation != null) entities.Add(customer.CurrentContract);
                }
            }

            return entities;
        }

        private void SyncBlips(HashSet<object> currentEntities)
        {
            staleEntities.Clear();
            foreach (var key in activeBlips.Keys) if (!currentEntities.Contains(key)) staleEntities.Add(key);
            foreach (var entity in staleEntities) { activeBlips[entity].Destroy(); activeBlips.Remove(entity); }

            foreach (var entity in currentEntities)
            {
                BlipType type = GetBlipType(entity);
                if (!activeBlips.ContainsKey(entity))
                {
                    activeBlips[entity] = new EntityBlip(entity, type, rotationRoot);
                }
                else if (activeBlips[entity].Type != type)
                {
                    activeBlips[entity].Type = type;
                    activeBlips[entity].UpdateVisuals();
                }
            }
        }

        private BlipType GetBlipType(object entity)
        {
            if (entity is Player) return BlipType.OtherPlayer;
            if (entity is PoliceOfficer) return BlipType.Police;
            if (entity is CartelGoon) return BlipType.Enemy;
            if (entity is Customer cust) return GetCustomerBlipType(cust);
            if (entity is POI poi)
            {
                foreach (var drop in DeadDrop.DeadDrops) if (drop.PoI == poi) return BlipType.DeadDrop;
                return BlipType.Quest;
            }
            if (entity is Contract) return BlipType.PendingDeal;
            return BlipType.None;
        }

        private BlipType GetCustomerBlipType(Customer cust)
        {
            if (cust.NPC == null) return BlipType.None;
            if (cust.IsAwaitingDelivery && cust.CurrentContract != null && cust.CurrentContract.Dealer == null)
                return BlipType.PendingDeal;

            if (cust.NPC.RelationData.Unlocked)
            {
                bool canShow = (bool)showOfferMethod.Invoke(cust, new object[] { true });
                if (canShow && (bool)offerValidMethod.Invoke(cust, new object[] { "" }))
                    return BlipType.UnlockedCustomer;
            }
            else
            {
                bool canShow = (bool)showDirectMethod.Invoke(cust, new object[] { true });
                if (canShow && (bool)sampleValidMethod.Invoke(cust, new object[] { "" }))
                    return BlipType.PotentialCustomer;
            }

            return BlipType.CustomerInactive;
        }

        public void UpdateBlips(MapPositionUtility util, Vector2 playerMapPos, float zoom, float adaptiveScale, float mapRot)
        {
            foreach (var blip in activeBlips.Values)
            {
                blip.Update(util, playerMapPos, zoom, adaptiveScale, mapRot);
            }
        }

        public void Clear()
        {
            foreach (var blip in activeBlips.Values) blip.Destroy();
            activeBlips.Clear();
        }
    }
}
