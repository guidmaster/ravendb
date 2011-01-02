namespace Raven.Bundles.Graphs
{
	public class ShortestDistanceQuery
	{
		public ShortestDistanceAlgorithm Algorithm { get; set; }
		public string Source { get; set; }
		public string Target { get; set; }
		public string GraphName { get; set; }
	}
}