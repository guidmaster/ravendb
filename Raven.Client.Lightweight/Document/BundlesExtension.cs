namespace Raven.Client.Document
{
	internal class BundlesExtension : IDocumentSessionBundleEndpoint
	{
		private readonly IDocumentSession _documentSession;

		public BundlesExtension(IDocumentSession documentSession)
		{
			_documentSession = documentSession;
		}

		public IDocumentSession Session
		{
			get { return _documentSession;}
		}
	}
}