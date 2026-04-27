using System;
using System.Collections.Generic;
using UnityEngine;

public enum LogisticsCargoType
{
    Evidence = 0,
    Detainee = 1,
    Equipment = 2
}

public enum LogisticsShipmentPriority
{
    Normal = 0,
    Urgent = 1,
    Critical = 2
}

public enum LogisticsShipmentStatus
{
    Preparing = 0,
    Cancelled = 1,
    InTransit = 2,
    Delayed = 3,
    Arrived = 4,
    Intercepted = 5,
    Lost = 6,
    Stolen = 7,
    Compromised = 8
}

public enum LogisticsIncidentVisibility
{
    Internal = 0,
    ExternalLeak = 1,
    Public = 2
}

[Serializable]
public class LogisticsInventoryItem
{
    public string inventoryItemId;
    public string ownerOrganizationId;
    public LogisticsCargoType cargoType;
    public string itemLabel;
    public int quantity;
    public string storageId;
    public int lastUpdatedDay;
}

[Serializable]
public class LogisticsStorage
{
    public string storageId;
    public string ownerOrganizationId;
    public string locationId;
    public string displayName;
    public int capacity;
    public int currentLoad;
    public bool acceptsEvidence = true;
    public bool acceptsDetainees = true;
    public bool acceptsEquipment = true;
}

[Serializable]
public class LogisticsRoute
{
    public string routeId;
    public string startLocationId;
    public string endLocationId;
    public int travelTimeHours;
    public int exposureRisk;
    public int interceptionRisk;
    public int operationalReliability;
    public List<string> knownByOrganizations = new List<string>();
}

[Serializable]
public class LogisticsShipment
{
    public string shipmentId;
    public string ownerOrganizationId;
    public LogisticsCargoType cargoType;
    public List<string> cargoItems = new List<string>();
    public int quantity;
    public string originLocationId;
    public string destinationLocationId;
    public string routeId;
    public string transportUnitId;
    public List<string> assignedCharacterIds = new List<string>();
    public string requestedByRole;
    public string approvedByRole;
    public LogisticsShipmentPriority priority;
    public bool chainOfCustodyRequired;
    public LogisticsShipmentStatus status;
    public long departureTime;
    public long estimatedArrivalTime;
    public long actualArrivalTime;
    public int exposureRisk;
    public int interceptionRisk;
    public int operationalReliability;
}

[Serializable]
public class LogisticsSupplyNeed
{
    public string needId;
    public string ownerOrganizationId;
    public string locationId;
    public LogisticsCargoType cargoType;
    public string summary;
    public int severity; // 0..100
}

[Serializable]
public class LogisticsTransportUnit
{
    public string transportUnitId;
    public string ownerOrganizationId;
    public string label;
    public int reliability;
    public int stealth;
    public int capacity;
}

[Serializable]
public class LogisticsIncident
{
    public string incidentId;
    public string shipmentId;
    public string incidentType;
    public string locationId;
    public List<string> involvedPeople = new List<string>();
    public List<string> lostItems = new List<string>();
    public string discoveredBy;
    public LogisticsIncidentVisibility visibility;
    public string consequences;
    public int dayIndex;
}

public static class PoliceLogisticsSystem
{
    private const int MinRisk = 0;
    private const int MaxRisk = 100;

    public static void EnsureBootstrapped(PoliceHeadquartersProfile organization, int dayIndex)
    {
        if (organization == null)
            return;
        if (PoliceWorldState.LogStorages.Count > 0 && PoliceWorldState.LogRoutes.Count > 0)
            return;

        string owner = string.IsNullOrEmpty(organization.HqId) ? "police_hq" : organization.HqId;
        EnsureDefaultTransportUnits(owner);
        EnsureDefaultStorages(owner, organization);
        EnsureDefaultRoutes(owner, organization);
        EnsureDefaultSupplyNeeds(owner, dayIndex);
    }

    public static bool TryApproveAndDispatch(LogisticsShipment shipment, int dayIndex, out string reason)
    {
        reason = string.Empty;
        if (!ValidateShipment(shipment, out reason))
            return false;
        if (shipment.status != LogisticsShipmentStatus.Preparing)
        {
            reason = "Shipment is not in Preparing status.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(shipment.approvedByRole))
        {
            reason = "approvedByRole is required.";
            return false;
        }

        shipment.status = LogisticsShipmentStatus.InTransit;
        shipment.departureTime = DayToUnix(dayIndex);
        int travelHours = ResolveTravelHours(shipment.routeId);
        shipment.estimatedArrivalTime = shipment.departureTime + Mathf.Max(1, travelHours) * 3600L;
        PoliceWorldState.LogShipments.Add(shipment);
        return true;
    }

    public static LogisticsShipment CreatePreparingShipment(
        string ownerOrganizationId,
        LogisticsCargoType cargoType,
        int quantity,
        string originLocationId,
        string destinationLocationId,
        string routeId,
        string transportUnitId,
        string requestedByRole,
        string approvedByRole,
        LogisticsShipmentPriority priority,
        bool chainOfCustodyRequired,
        List<string> cargoItems = null,
        List<string> assignedCharacterIds = null)
    {
        LogisticsRoute route = FindRoute(routeId);
        return new LogisticsShipment
        {
            shipmentId = "ship_" + Guid.NewGuid().ToString("N"),
            ownerOrganizationId = ownerOrganizationId,
            cargoType = cargoType,
            cargoItems = cargoItems ?? new List<string>(),
            quantity = Mathf.Max(1, quantity),
            originLocationId = originLocationId,
            destinationLocationId = destinationLocationId,
            routeId = routeId,
            transportUnitId = transportUnitId,
            assignedCharacterIds = assignedCharacterIds ?? new List<string>(),
            requestedByRole = requestedByRole,
            approvedByRole = approvedByRole,
            priority = priority,
            chainOfCustodyRequired = chainOfCustodyRequired,
            status = LogisticsShipmentStatus.Preparing,
            departureTime = 0,
            estimatedArrivalTime = 0,
            actualArrivalTime = 0,
            exposureRisk = route != null ? route.exposureRisk : 25,
            interceptionRisk = route != null ? route.interceptionRisk : 20,
            operationalReliability = route != null ? route.operationalReliability : 65
        };
    }

    public static void AdvanceDay(int dayIndex)
    {
        long now = DayToUnix(dayIndex);
        for (int i = 0; i < PoliceWorldState.LogShipments.Count; i++)
        {
            LogisticsShipment shipment = PoliceWorldState.LogShipments[i];
            if (shipment == null)
                continue;
            if (shipment.status != LogisticsShipmentStatus.InTransit && shipment.status != LogisticsShipmentStatus.Delayed)
                continue;

            if (shipment.estimatedArrivalTime > now)
                continue;

            int roll = BuildDeterministicRoll(shipment.shipmentId, dayIndex);
            int risk = Mathf.Clamp(
                Mathf.RoundToInt(shipment.exposureRisk * 0.35f + shipment.interceptionRisk * 0.45f + (100 - shipment.operationalReliability) * 0.2f),
                MinRisk, MaxRisk);
            int failThreshold = Mathf.Clamp(10 + risk / 2, 10, 85);
            int loudThreshold = Mathf.Clamp(4 + shipment.interceptionRisk / 5, 4, 30);
            int delayThreshold = Mathf.Clamp(18 + (100 - shipment.operationalReliability) / 2, 18, 65);

            if (roll < loudThreshold)
            {
                shipment.status = LogisticsShipmentStatus.Intercepted;
                shipment.actualArrivalTime = now;
                RegisterIncident(shipment, "Intercepted", LogisticsIncidentVisibility.ExternalLeak, dayIndex,
                    "Shipment intercepted in transit.");
            }
            else if (roll < failThreshold)
            {
                shipment.status = LogisticsShipmentStatus.Compromised;
                shipment.actualArrivalTime = now;
                RegisterIncident(shipment, "Compromised", LogisticsIncidentVisibility.Internal, dayIndex,
                    "Shipment compromised before secure handoff.");
            }
            else if (roll < delayThreshold)
            {
                shipment.status = LogisticsShipmentStatus.Delayed;
                shipment.estimatedArrivalTime = now + 12 * 3600L;
                RegisterIncident(shipment, "Delayed", LogisticsIncidentVisibility.Internal, dayIndex,
                    "Shipment delayed due to operational friction.");
            }
            else
            {
                shipment.status = LogisticsShipmentStatus.Arrived;
                shipment.actualArrivalTime = now;
                ApplyArrivalToInventory(shipment, dayIndex);
            }
        }
    }

    public static bool ValidateShipment(LogisticsShipment shipment, out string reason)
    {
        reason = string.Empty;
        if (shipment == null)
        {
            reason = "Shipment is null.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(shipment.shipmentId))
        {
            reason = "shipmentId is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(shipment.ownerOrganizationId))
        {
            reason = "ownerOrganizationId is required.";
            return false;
        }
        if (shipment.quantity <= 0)
        {
            reason = "quantity must be > 0.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(shipment.originLocationId) || string.IsNullOrWhiteSpace(shipment.destinationLocationId))
        {
            reason = "originLocationId and destinationLocationId are required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(shipment.routeId) || string.IsNullOrWhiteSpace(shipment.transportUnitId))
        {
            reason = "routeId and transportUnitId are required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(shipment.requestedByRole) || string.IsNullOrWhiteSpace(shipment.approvedByRole))
        {
            reason = "requestedByRole and approvedByRole are required.";
            return false;
        }
        return true;
    }

    private static void EnsureDefaultTransportUnits(string owner)
    {
        if (PoliceWorldState.LogTransportUnits.Count > 0)
            return;
        PoliceWorldState.LogTransportUnits.Add(new LogisticsTransportUnit
        {
            transportUnitId = "police_van_01",
            ownerOrganizationId = owner,
            label = "Police Van 01",
            reliability = 70,
            stealth = 30,
            capacity = 8
        });
        PoliceWorldState.LogTransportUnits.Add(new LogisticsTransportUnit
        {
            transportUnitId = "police_car_01",
            ownerOrganizationId = owner,
            label = "Patrol Car 01",
            reliability = 65,
            stealth = 40,
            capacity = 4
        });
    }

    private static void EnsureDefaultStorages(string owner, PoliceHeadquartersProfile organization)
    {
        if (organization.Stations == null)
            return;
        for (int i = 0; i < organization.Stations.Count; i++)
        {
            PoliceStationProfile station = organization.Stations[i];
            if (station == null || string.IsNullOrWhiteSpace(station.StationId))
                continue;
            AddStorageIfMissing(owner, station.StationId + "_evidence", station.StationId, station.DisplayName + " Evidence", 500, true, false, false);
            AddStorageIfMissing(owner, station.StationId + "_equipment", station.StationId, station.DisplayName + " Equipment", 800, false, false, true);
            AddStorageIfMissing(owner, station.StationId + "_custody", station.StationId, station.DisplayName + " Custody", 300, false, true, false);
        }
    }

    private static void EnsureDefaultRoutes(string owner, PoliceHeadquartersProfile organization)
    {
        if (organization.Stations == null || organization.Stations.Count < 1)
            return;
        string hqLocation = organization.HqId;
        for (int i = 0; i < organization.Stations.Count; i++)
        {
            PoliceStationProfile station = organization.Stations[i];
            if (station == null || string.IsNullOrWhiteSpace(station.StationId))
                continue;
            string routeId = "route_hq_to_" + station.StationId;
            if (FindRoute(routeId) != null)
                continue;
            LogisticsRoute route = new LogisticsRoute
            {
                routeId = routeId,
                startLocationId = hqLocation,
                endLocationId = station.StationId,
                travelTimeHours = 4,
                exposureRisk = 25,
                interceptionRisk = 20,
                operationalReliability = 72
            };
            route.knownByOrganizations.Add(owner);
            PoliceWorldState.LogRoutes.Add(route);
        }
    }

    private static void EnsureDefaultSupplyNeeds(string owner, int dayIndex)
    {
        if (PoliceWorldState.LogSupplyNeeds.Count > 0)
            return;
        PoliceWorldState.LogSupplyNeeds.Add(new LogisticsSupplyNeed
        {
            needId = "need_eq_" + dayIndex,
            ownerOrganizationId = owner,
            locationId = owner,
            cargoType = LogisticsCargoType.Equipment,
            summary = "Refresh station equipment baseline",
            severity = 40
        });
        PoliceWorldState.LogSupplyNeeds.Add(new LogisticsSupplyNeed
        {
            needId = "need_ev_" + dayIndex,
            ownerOrganizationId = owner,
            locationId = owner,
            cargoType = LogisticsCargoType.Evidence,
            summary = "Evidence storage pressure rising",
            severity = 35
        });
    }

    private static void AddStorageIfMissing(
        string owner, string storageId, string locationId, string displayName, int capacity,
        bool acceptsEvidence, bool acceptsDetainees, bool acceptsEquipment)
    {
        for (int i = 0; i < PoliceWorldState.LogStorages.Count; i++)
        {
            if (PoliceWorldState.LogStorages[i] != null && PoliceWorldState.LogStorages[i].storageId == storageId)
                return;
        }
        PoliceWorldState.LogStorages.Add(new LogisticsStorage
        {
            storageId = storageId,
            ownerOrganizationId = owner,
            locationId = locationId,
            displayName = string.IsNullOrWhiteSpace(displayName) ? storageId : displayName,
            capacity = Mathf.Max(10, capacity),
            currentLoad = 0,
            acceptsEvidence = acceptsEvidence,
            acceptsDetainees = acceptsDetainees,
            acceptsEquipment = acceptsEquipment
        });
    }

    private static LogisticsRoute FindRoute(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId))
            return null;
        for (int i = 0; i < PoliceWorldState.LogRoutes.Count; i++)
        {
            LogisticsRoute route = PoliceWorldState.LogRoutes[i];
            if (route != null && route.routeId == routeId)
                return route;
        }
        return null;
    }

    private static int ResolveTravelHours(string routeId)
    {
        LogisticsRoute route = FindRoute(routeId);
        return route != null ? Mathf.Max(1, route.travelTimeHours) : 6;
    }

    private static long DayToUnix(int dayIndex)
    {
        return Mathf.Max(1, dayIndex) * 86400L;
    }

    private static int BuildDeterministicRoll(string key, int dayIndex)
    {
        int hash = 17;
        if (!string.IsNullOrEmpty(key))
            hash = hash * 31 + key.GetHashCode();
        hash = hash * 31 + dayIndex;
        System.Random rng = new System.Random(hash);
        return rng.Next(0, 100);
    }

    private static void RegisterIncident(LogisticsShipment shipment, string incidentType, LogisticsIncidentVisibility visibility, int dayIndex, string consequences)
    {
        LogisticsIncident incident = new LogisticsIncident
        {
            incidentId = "log_inc_" + Guid.NewGuid().ToString("N"),
            shipmentId = shipment.shipmentId,
            incidentType = incidentType,
            locationId = shipment.destinationLocationId,
            discoveredBy = shipment.ownerOrganizationId,
            visibility = visibility,
            consequences = consequences,
            dayIndex = dayIndex
        };
        for (int i = 0; i < shipment.cargoItems.Count; i++)
            incident.lostItems.Add(shipment.cargoItems[i]);
        for (int i = 0; i < shipment.assignedCharacterIds.Count; i++)
            incident.involvedPeople.Add(shipment.assignedCharacterIds[i]);
        PoliceWorldState.LogIncidents.Add(incident);
        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.LogisticsEvent,
            correlationId: shipment.shipmentId,
            dayIndex: dayIndex,
            actorId: shipment.approvedByRole,
            subjectId: shipment.shipmentId,
            subjectType: "Shipment",
            summary: "Logistics incident: " + incidentType,
            details: consequences,
            visibility: ToRecordVisibility(visibility));
    }

    private static void ApplyArrivalToInventory(LogisticsShipment shipment, int dayIndex)
    {
        string label = shipment.cargoItems.Count > 0 ? shipment.cargoItems[0] : shipment.cargoType.ToString();
        LogisticsInventoryItem item = new LogisticsInventoryItem
        {
            inventoryItemId = "inv_" + Guid.NewGuid().ToString("N"),
            ownerOrganizationId = shipment.ownerOrganizationId,
            cargoType = shipment.cargoType,
            itemLabel = label,
            quantity = shipment.quantity,
            storageId = shipment.destinationLocationId,
            lastUpdatedDay = dayIndex
        };
        PoliceWorldState.LogInventory.Add(item);
        PoliceRecordsSystem.AddRecord(
            PoliceRecordKind.LogisticsEvent,
            correlationId: shipment.shipmentId,
            dayIndex: dayIndex,
            actorId: shipment.approvedByRole,
            subjectId: shipment.shipmentId,
            subjectType: "Shipment",
            summary: "Shipment arrived",
            details: "Inventory updated at destination storage.",
            visibility: PoliceRecordVisibility.Internal);
    }

    private static PoliceRecordVisibility ToRecordVisibility(LogisticsIncidentVisibility visibility)
    {
        if (visibility == LogisticsIncidentVisibility.Public) return PoliceRecordVisibility.Public;
        if (visibility == LogisticsIncidentVisibility.ExternalLeak) return PoliceRecordVisibility.ExternalLeak;
        return PoliceRecordVisibility.Internal;
    }
}
