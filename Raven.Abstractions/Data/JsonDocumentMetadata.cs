﻿using System;
using Newtonsoft.Json.Linq;

namespace Raven.Database
{
	/// <summary>
	/// A document representation:
	/// * Etag
	/// * Metadata
	/// </summary>
	public class JsonDocumentMetadata : IJsonDocumentMetadata
	{
		/// <summary>
		/// 	Gets or sets the metadata for the document
		/// </summary>
		/// <value>The metadata.</value>
		public JObject Metadata { get; set; }

		/// <summary>
		/// 	Gets or sets the key for the document
		/// </summary>
		/// <value>The key.</value>
		public string Key { get; set; }

		/// <summary>
		/// 	Gets or sets a value indicating whether this document is non authoritive (modified by uncommitted transaction).
		/// </summary>
		public bool NonAuthoritiveInformation { get; set; }

		/// <summary>
		/// Gets or sets the etag.
		/// </summary>
		/// <value>The etag.</value>
		public Guid Etag { get; set; }

		/// <summary>
		/// 	Gets or sets the last modified date for the document
		/// </summary>
		/// <value>The last modified.</value>
		public DateTime LastModified { get; set; }
	}
}