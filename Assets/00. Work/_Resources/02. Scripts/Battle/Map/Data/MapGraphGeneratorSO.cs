using System.Collections.Generic;
using Battle.Data;
using Battle.Map.Enums;
using UnityEngine;

namespace Battle.Map.Data
{
    [CreateAssetMenu(menuName = "Battle/Map/Map Graph Generator")]
    public class MapGraphGeneratorSO : ScriptableObject
    {
        [Header("Stage Pools — Battle")]
        public List<BattleStageSO> lowStagePool  = new(); // floor 1~4
        public List<BattleStageSO> midStagePool  = new(); // floor 5~8
        public List<BattleStageSO> highStagePool = new(); // floor 9~11

        [Header("Stage Pools — Special")]
        public List<BattleStageSO> eliteStagePool = new();
        public BattleStageSO       bossStage;

        [Header("Node Contents")]
        public RestContentSO defaultRestContent;
        public ShopContentSO defaultShopContent;

        private const int TotalFloors  = 12;
        private const int MinNodes     = 5;
        private const int MaxNodes     = 7;
        private const int MinElite     = 3;
        private const int MinRest      = 2;
        private const int MinShop      = 2;
        private const int EligibleFrom = 3; // floor 3부터 Elite/Rest/Shop 등장
        private const int LowMaxFloor  = 4;
        private const int MidMaxFloor  = 8;  // 9 이상은 고층

        private static readonly float[] XOffsets5 = { -0.24f, -0.12f, 0.00f, 0.12f, 0.24f };
        private static readonly float[] XOffsets7 = { -0.36f, -0.24f, -0.12f, 0.00f, 0.12f, 0.24f, 0.36f };

        public MapGraphSO Generate(int seed)
        {
            var rng   = new System.Random(seed);
            var graph = CreateInstance<MapGraphSO>();
            graph.name = $"GeneratedMap_{seed}";

            // 1. 층별 노드 수 (홀수만: 5 또는 7)
            int[] counts = new int[TotalFloors + 1];
            counts[0]           = 1; // Start
            counts[TotalFloors] = 1; // Boss
            for (int f = 1; f < TotalFloors; f++)
                counts[f] = rng.Next(0, 2) == 0 ? MinNodes : MaxNodes;

            // 2. 타입 그리드 초기화 (기본값 Battle)
            var typeGrid = new MapNodeType[TotalFloors + 1][];
            for (int f = 0; f <= TotalFloors; f++)
            {
                typeGrid[f] = new MapNodeType[counts[f]];
                for (int s = 0; s < counts[f]; s++)
                    typeGrid[f][s] = MapNodeType.Battle;
            }
            typeGrid[0][0]           = MapNodeType.Start;
            typeGrid[TotalFloors][0] = MapNodeType.Boss;

            // 3. 보장 배치 (floor EligibleFrom~TotalFloors-1)
            var eligibleFloors = new List<int>();
            for (int f = EligibleFrom; f < TotalFloors; f++)
                eligibleFloors.Add(f);

            PlaceGuaranteed(rng, typeGrid, counts, eligibleFloors, MapNodeType.Elite, MinElite);
            PlaceGuaranteed(rng, typeGrid, counts, eligibleFloors, MapNodeType.Rest,  MinRest);
            PlaceGuaranteed(rng, typeGrid, counts, eligibleFloors, MapNodeType.Shop,  MinShop);

            // 4. 확률적 추가 배치
            for (int f = EligibleFrom; f < TotalFloors; f++)
            {
                bool hasElite = false, hasRest = false, hasShop = false;
                for (int s = 0; s < counts[f]; s++)
                {
                    if      (typeGrid[f][s] == MapNodeType.Elite) hasElite = true;
                    else if (typeGrid[f][s] == MapNodeType.Rest)  hasRest  = true;
                    else if (typeGrid[f][s] == MapNodeType.Shop)  hasShop  = true;
                }
                for (int s = 0; s < counts[f]; s++)
                {
                    if (typeGrid[f][s] != MapNodeType.Battle) continue;
                    if      (!hasElite && rng.NextDouble() < 0.25) { typeGrid[f][s] = MapNodeType.Elite; hasElite = true; }
                    else if (!hasRest  && rng.NextDouble() < 0.20) { typeGrid[f][s] = MapNodeType.Rest;  hasRest  = true; }
                    else if (!hasShop  && rng.NextDouble() < 0.20) { typeGrid[f][s] = MapNodeType.Shop;  hasShop  = true; }
                }
            }

            // 5. 노드 인스턴스 생성
            var floorNodes = new List<MapNodeDefinition>[TotalFloors + 1];
            for (int f = 0; f <= TotalFloors; f++)
            {
                floorNodes[f] = new List<MapNodeDefinition>();
                float[] offsets = counts[f] == 7 ? XOffsets7 : counts[f] == 5 ? XOffsets5 : new[] { 0f };

                for (int s = 0; s < counts[f]; s++)
                {
                    var node = new MapNodeDefinition
                    {
                        nodeId     = $"floor_{f}_slot_{s}",
                        floorIndex = f,
                        xOffset    = offsets[s],
                        nodeType   = typeGrid[f][s],
                    };
                    AssignContent(node, rng);
                    floorNodes[f].Add(node);
                    graph.nodes.Add(node);
                }
            }

            // 6. 연결 생성
            BuildConnections(rng, floorNodes);

            return graph;
        }

        // 해당 타입을 required개만큼 floor 3~11 중 랜덤 배치 (층당 최대 1개)
        private static void PlaceGuaranteed(System.Random rng, MapNodeType[][] typeGrid, int[] counts,
                                            List<int> floors, MapNodeType type, int required)
        {
            var shuffled = new List<int>(floors);
            Shuffle(rng, shuffled);

            int placed = 0;
            foreach (int f in shuffled)
            {
                if (placed >= required) break;

                bool alreadyHas = false;
                var battleSlots = new List<int>();
                for (int s = 0; s < counts[f]; s++)
                {
                    if (typeGrid[f][s] == type) { alreadyHas = true; break; }
                    if (typeGrid[f][s] == MapNodeType.Battle) battleSlots.Add(s);
                }
                if (alreadyHas || battleSlots.Count == 0) continue;

                typeGrid[f][battleSlots[rng.Next(battleSlots.Count)]] = type;
                placed++;
            }
        }

        private List<BattleStageSO> GetBattlePool(int floor)
        {
            if (floor <= LowMaxFloor) return lowStagePool;
            if (floor <= MidMaxFloor) return midStagePool;
            return highStagePool;
        }

        private void AssignContent(MapNodeDefinition node, System.Random rng)
        {
            switch (node.nodeType)
            {
                case MapNodeType.Battle:
                    var pool = GetBattlePool(node.floorIndex);
                    if (pool.Count > 0)
                        node.stageRef = pool[rng.Next(pool.Count)];
                    break;
                case MapNodeType.Elite:
                    if (eliteStagePool.Count > 0)
                        node.stageRef = eliteStagePool[rng.Next(eliteStagePool.Count)];
                    break;
                case MapNodeType.Boss:
                    node.stageRef = bossStage;
                    break;
                case MapNodeType.Rest:
                    node.restContent = defaultRestContent;
                    break;
                case MapNodeType.Shop:
                    node.shopContent = defaultShopContent;
                    break;
            }
        }

        // 층 간 연결 생성 (max outgoing 2, 선 교차 금지, 고립 노드 방지)
        private static void BuildConnections(System.Random rng, List<MapNodeDefinition>[] floorNodes)
        {
            for (int f = 0; f < floorNodes.Length - 1; f++)
            {
                var from = floorNodes[f];
                var to   = floorNodes[f + 1];
                int nA = from.Count, nB = to.Count;
                var outDeg = new int[nA];
                var inDeg  = new int[nB];
                var edges  = new List<(int a, int b)>();

                void Connect(int a, int b)
                {
                    from[a].nextNodeIds.Add(to[b].nodeId);
                    outDeg[a]++;
                    inDeg[b]++;
                    edges.Add((a, b));
                }

                // (a-k)*(b-l) < 0 이면 두 간선이 교차
                bool CanConnect(int a, int b)
                {
                    if (outDeg[a] >= 2) return false;
                    if (from[a].nextNodeIds.Contains(to[b].nodeId)) return false;
                    foreach (var (k, l) in edges)
                        if ((a - k) * (b - l) < 0) return false;
                    return true;
                }

                // Start 노드(floor 0): floor 1 전체 연결, max-outgoing 예외
                if (f == 0)
                {
                    for (int b = 0; b < nB; b++) Connect(0, b);
                    continue;
                }

                // Step 1: 비례 매핑으로 primary 연결 (교차 없음 보장)
                for (int a = 0; a < nA; a++)
                {
                    int b = nA == 1 ? nB / 2 : Mathf.RoundToInt((float)a * (nB - 1) / (nA - 1));
                    b = Mathf.Clamp(b, 0, nB - 1);
                    if (!from[a].nextNodeIds.Contains(to[b].nodeId))
                        Connect(a, b);
                }

                // Step 2: 고립 노드(incoming 0) 수정
                for (int b = 0; b < nB; b++)
                {
                    if (inDeg[b] > 0) continue;
                    for (int a = 0; a < nA; a++)
                    {
                        if (CanConnect(a, b)) { Connect(a, b); break; }
                    }
                }

                // Step 3: 선택적 두 번째 연결 (랜덤)
                var aOrder = new List<int>();
                for (int a = 0; a < nA; a++) aOrder.Add(a);
                Shuffle(rng, aOrder);

                foreach (int a in aOrder)
                {
                    if (outDeg[a] >= 2) continue;
                    var bCandidates = new List<int>();
                    for (int b = 0; b < nB; b++) bCandidates.Add(b);
                    Shuffle(rng, bCandidates);
                    foreach (int b in bCandidates)
                    {
                        if (CanConnect(a, b)) { Connect(a, b); break; }
                    }
                }
            }
        }

        private static void Shuffle<T>(System.Random rng, List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        [ContextMenu("Test Generate (seed=12345)")]
        private void TestGenerate()
        {
            var graph = Generate(12345);
            var sb = new System.Text.StringBuilder();
            int eliteCount = 0, restCount = 0, shopCount = 0;

            sb.AppendLine($"=== GeneratedMap (seed=12345, nodes={graph.nodes.Count}) ===");
            for (int f = 0; f <= TotalFloors; f++)
            {
                var floorNodes = graph.nodes.FindAll(n => n.floorIndex == f);
                foreach (var n in floorNodes)
                {
                    if      (n.nodeType == MapNodeType.Elite) eliteCount++;
                    else if (n.nodeType == MapNodeType.Rest)  restCount++;
                    else if (n.nodeType == MapNodeType.Shop)  shopCount++;
                    sb.AppendLine($"  F{f:D2} {n.nodeType,-8} xOff={n.xOffset:F2} out={n.nextNodeIds.Count}");
                }
            }
            sb.AppendLine($"--- Elite={eliteCount}(min {MinElite}), Rest={restCount}(min {MinRest}), Shop={shopCount}(min {MinShop}) ---");
            Debug.Log(sb.ToString());
            DestroyImmediate(graph);
        }
    }
}
