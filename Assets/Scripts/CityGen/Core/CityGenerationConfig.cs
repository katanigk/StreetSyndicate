using FamilyBusiness.CityGen.Debug;
using UnityEngine;

namespace FamilyBusiness.CityGen.Core
{
    /// <summary>
    /// Tunable parameters for procedural city generation (Batch 1: initial fields only).
    /// </summary>
    [CreateAssetMenu(fileName = "CityGenerationConfig", menuName = "Family Business/City Gen/City Generation Config")]
    public sealed class CityGenerationConfig : ScriptableObject
    {
        [Header("City identity")]
        public string cityDisplayName = "Ashkelton";

        [Header("City extent (grid / abstract cells)")]
        [Min(8)] public int cityWidthCells = 128;
        [Min(8)] public int cityHeightCells = 128;

        [Header("Macro layout (Batch 2)")]
        [Range(0f, 0.35f)] public float boundaryIrregularity = 0.1f;
        [Range(0f, 1f)] public float waterFeatureChance = 0.7f;
        public bool forceWaterFeature;
        [Range(0f, 1f)] public float railFeatureChance = 0.6f;
        public bool forceRailFeature;
        [Min(3)] public int macroAnchorCountMin = 5;
        [Min(3)] public int macroAnchorCountMax = 8;
        [Min(1f)] public float riverApproxWidthCells = 5f;
        [Min(1f)] public float railCorridorWidthCells = 3.5f;
        [Min(2f)] public float coastStripDepthCells = 14f;

        [Header("Road graph (Batch 3)")]
        [Tooltip("Long major chords added after the anchor MST (same connected component only).")]
        [Min(0)] public int majorRoadExtraEdgesBeyondMst = 2;
        [Tooltip("How many Secondary edges to aim for after the major network.")]
        [Min(0)] public int secondaryRoadTargetEdges = 12;
        [Min(0.5f)] public float minRoadSegmentLength = 2.5f;
        [Min(0)] public int maxRoadSubdivideDepth = 5;
        [Min(0f)] public float roadNodeMergeDistance = 1.25f;
        [Min(0)] public int secondaryBranchAttemptsPerNode = 2;
        [Min(1f)] public float secondaryRoadTypicalLength = 18f;
        [Tooltip("Extra clearance beyond macro water feature half-width when rejecting segments.")]
        [Min(0f)] public float waterRoadClearanceCells = 2f;
        [Tooltip("Half-width used with rail path to flag crossing edges.")]
        [Min(0.5f)] public float railCorridorHalfWidthForCrossingCells = 2f;
        [Min(3)] public int segmentSampleCount = 12;

        [Min(2)] public int minimumBlockSizeCells = 8;

        [Header("Blocks (Batch 4)")]
        [Tooltip("Axis-aligned grid step for coarse blocks (district ownership cells).")]
        [Min(4f)] public float blockGridCellSizeCells = 14f;
        [Tooltip("Block centers closer than this to river/coast centerline (incl. width) are skipped.")]
        [Min(0f)] public float blockWaterExclusionPaddingCells = 2f;

        [Header("Districts (Batch 4)")]
        [Min(3)] public int districtCountMin = 5;
        [Min(3)] public int districtCountMax = 9;
        [Min(1)] public int minBlocksPerDistrict = 2;
        [Min(4f)] public float districtSeedMergeDistanceCells = 22f;
        [Min(1)] public int districtLloydIterations = 3;
        [Range(0f, 3f)] public float downtownCentralityWeight = 1.25f;
        [Range(0f, 3f)] public float industrialRailAffinity = 1.2f;
        [Range(0f, 3f)] public float wealthyEdgeAversion = 1.1f;
        [Range(0f, 3f)] public float fringeBoundaryAffinity = 1.15f;
        [Range(0f, 3f)] public float docksWaterAffinity = 1.35f;

        [Header("Lots (Batch 5)")]
        [Tooltip("Desired lot size along each axis; blocks are split evenly to approximate this.")]
        [Min(2f)] public float lotTargetCellSizeCells = 6f;
        [Tooltip("Inset from block edge before gridding (setback / alley strip).")]
        [Min(0f)] public float lotInsetFromBlockCells = 1f;
        [Tooltip("Lots smaller than this after splitting are skipped (or divisions reduced).")]
        [Min(0.5f)] public float minLotSizeCells = 3f;
        [Tooltip("0 = unlimited. Otherwise shrink row/column counts while respecting min lot size.")]
        [Min(0)] public int maxLotsPerBlock = 0;

        [Header("Anchor institutions (Batch 6)")]
        public bool placeAnchorInstitutions = true;
        [Range(0f, 1f)] public float institutionHospitalMinAccessibility = 0.2f;
        [Min(1f)] public float institutionPrisonMinLotAreaCells = 48f;
        [Tooltip("Beyond this plan distance to rail, rail station score tapers off.")]
        [Min(8f)] public float institutionRailMaxDistanceCells = 120f;
        [Tooltip("Beyond this plan distance to river/coast, dock authority score tapers off.")]
        [Min(8f)] public float institutionWaterMaxDistanceCells = 140f;
        [Tooltip("Placement passes: 1 = strict only; higher = extra relaxed passes (reduces FAILED to place on tight seeds).")]
        [Range(1, 4)] public int institutionPlacementRelaxationPasses = 4;
        [Min(0.5f)] public float institutionMinFrontageStrictCells = 2.5f;
        [Range(0.25f, 2f)] public float institutionAccessibilityWeight = 1f;
        [Range(0.25f, 2f)] public float institutionFrontageWeight = 1f;
        [Range(0.25f, 2f)] public float institutionCentralityWeight = 1f;
        [Range(0.25f, 2f)] public float institutionLargeLotBonus = 1f;
        [Range(0.25f, 2f)] public float institutionRailProximityWeight = 1f;
        [Range(0.25f, 2f)] public float institutionWaterProximityWeight = 1f;
        [Range(0.25f, 2f)] public float institutionLogisticsWeight = 1f;

        [Header("Lots — access & placement (Batch 5.5)")]
        [Tooltip("Max distance from lot AABB to a road segment to count as adjacent / touching.")]
        [Min(0.25f)] public float lotRoadTouchDistanceCells = 2f;
        [Tooltip("How close a lot side must be to its block edge to count as block-perimeter (access heuristic).")]
        [Min(0.01f)] public float lotBlockEdgeEpsilonCells = 0.35f;
        [Min(1f)] public float lotSizeClassSmallMaxAreaCells = 28f;
        [Min(1f)] public float lotSizeClassMediumMaxAreaCells = 72f;
        [Min(1f)] public float lotSizeClassLargeMaxAreaCells = 180f;
        [Min(1f)] public float lotLargeBuildingMinAreaCells = 55f;
        [Min(0.5f)] public float lotMinStreetFrontageCells = 3f;
        [Min(1f)] public float lotBackUseMinAreaCells = 40f;
        [Min(1f)] public float lotBackUseMinDepthCells = 8f;

        [Header("Regular buildings (Batch 7)")]
        public bool placeRegularBuildings = true;
        [Tooltip("Scales weights for empty / yard / vacant / reserved-future outcomes.")]
        [Range(0.25f, 4f)] public float regularBuildingUndevelopedWeightScale = 1f;
        [Tooltip("Max plan distance to rail macro feature for rail-utility candidates.")]
        [Min(8f)] public float regularBuildingRailContextMaxCells = 95f;
        [Tooltip("Max plan distance to water macro feature for dock-freight candidates.")]
        [Min(8f)] public float regularBuildingWaterContextMaxCells = 110f;

        [Header("Building crime potential (Batch 8)")]
        public bool computeBuildingCrimePotential = true;
        [Tooltip("Deterministic per-building jitter applied to each scalar before clamp [0,1].")]
        [Range(0f, 0.12f)] public float crimePotentialJitterAmplitude = 0.04f;

        [Header("Discovery defaults (Batch 9)")]
        [Tooltip("Attach discovery/fog hooks and initial knowledge to districts, institutions, and buildings.")]
        public bool applyDiscoveryDefaults = true;

        [Header("Discovery / movement reveal (Batch 10)")]
        [Tooltip("If true, city starts mostly Unknown until StartingReveal / MovementDiscoveryService.")]
        public bool applyColdStartDiscoveryDefaults = true;
        [Tooltip("Outer radius (plan cells) for district rumor/known rings at run start.")]
        [Min(4f)] public float startingDistrictRevealRadiusCells = 52f;
        [Min(2f)] public float startingBuildingRevealRadiusCells = 24f;
        [Min(2f)] public float startingInstitutionRevealRadiusCells = 36f;
        [Tooltip("Radii used on each movement reveal tick (typically ≤ starting).")]
        [Min(4f)] public float movementDistrictRevealRadiusCells = 40f;
        [Min(2f)] public float movementBuildingRevealRadiusCells = 20f;
        [Min(2f)] public float movementInstitutionRevealRadiusCells = 30f;
        [Tooltip("Inner band = radius × this (Known / facade). Should be ≤ rumored multiplier.")]
        [Range(0.05f, 1f)] public float knownRevealDistanceMultiplier = 0.38f;
        [Tooltip("Outer band = radius × this (Rumor).")]
        [Range(0.1f, 2.5f)] public float rumoredRevealDistanceMultiplier = 1f;
        [Tooltip("Low-profile institutions use a tighter Known threshold (fraction of inner institution radius).")]
        [Range(0.08f, 0.95f)] public float lowProfileInstitutionKnownRadiusScale = 0.32f;

        [Header("World entry / gang start (Batch 11)")]
        [Tooltip("Deterministic pick among the top N scored start candidates after sorting by score.")]
        [Min(1)] public int gangSpawnCandidatePoolSize = 6;
        [Tooltip("Score penalty when the lot fronts a major road (exposure / police visibility).")]
        [Min(0f)] public float gangSpawnMajorRoadExposurePenalty = 8f;
        [Tooltip("Scaled by lot accessibility [0,1]; higher = more visible / central.")]
        [Min(0f)] public float gangSpawnHighAccessibilityPenalty = 9f;

        [Header("Sandbox city (micro / Ops)")]
        [Tooltip("Skip full procedural pipeline: coarse block grid + 3×3 lots per block.")]
        public bool singleBlockSandboxMap;
        [Tooltip("Sandbox grid width in blocks (e.g. 3 × 4 = 12 blocks).")]
        [Range(1, 8)] public int singleBlockSandboxBlocksAlongX = 3;
        [Tooltip("Sandbox grid height in blocks.")]
        [Range(1, 8)] public int singleBlockSandboxBlocksAlongY = 4;
        [Tooltip("Plan-cell edge length of each square block footprint (hex art swaps in later).")]
        [Min(20f)] public float singleBlockSandboxBlockSquareCells = 42f;
        [Tooltip("Gap between block footprints (reads as alleys / connectors).")]
        [Min(0f)] public float singleBlockSandboxInterBlockGapCells = 2.5f;

        [Header("Debug")]
        public CityDebugSettings debugSettings;
    }
}
