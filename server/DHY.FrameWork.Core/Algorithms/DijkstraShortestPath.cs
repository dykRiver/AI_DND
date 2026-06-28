using QuikGraph;
using QuikGraph.Algorithms.Observers;
using QuikGraph.Algorithms.ShortestPath;

namespace DHY.FrameWork.Core.Algorithms
{
    public class DijkstraShortestPath
    {
        /// <summary>
        /// 获取从起点到终点的最短路径
        /// </summary>
        /// <param name="edges">带权重的边</param>
        /// <param name="source">起点</param>
        /// <param name="target">终点</param>
        /// <returns>路径</returns>
        public static IEnumerable<string> GetShortestPath(IEnumerable<EdgeWithWeight> edges, string source, string target)
        {
            // 创建邻接图，使用string类型作为顶点、边的唯一标识
            var graph = new AdjacencyGraph<string, Edge<string>>(true);
            var vertexs = edges.Select(a => a.Source).Distinct().ToList().Union(edges.Select(b => b.Target).Distinct());
            // 添加顶点到图中
            foreach (var vertex in vertexs)
            {
                graph.AddVertex(vertex);
            }
            //添加边到图中
            foreach (var edge in edges)
            {
                graph.AddEdge(new Edge<string>(edge.Source, edge.Target));
            }
            // 创建算法，传入图和权重
            var algorithm = new DijkstraShortestPathAlgorithm<string, Edge<string>>(graph, e => edges.Single(a => a.Source == e.Source && a.Target == e.Target).Weight);
            // 使用顶点前置记录器，以提供路径计算
            var predecessorObserver = new VertexPredecessorRecorderObserver<string, Edge<string>>();
            using (predecessorObserver.Attach(algorithm))
                //以顶点source为起点，运行算法
                algorithm.Compute(source);
            IEnumerable<Edge<string>> outEdges;
            predecessorObserver.TryGetPath(target, out outEdges);
            var list = outEdges.Select(a => a.Target).ToList();
            return list;
        }
    }
    /// <summary>
    /// 带权重的边
    /// </summary>
    public class EdgeWithWeight
    {
        /// <summary>
        /// 起点
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// 终点
        /// </summary>
        public string Target { get; set; }
        /// <summary>
        /// 权重
        /// </summary>
        public double Weight { get; set; }
    }
}
