namespace Raven.Client
{
	/// <summary>
	/// Marker interface for bundle extension methods
	/// </summary>
	public interface IDocumentSessionBundleEndpoint
	{
		/// <summary>
		/// The associated session
		/// </summary>
		IDocumentSession Session { get; }
	}
}