using System;
using System.Collections.Generic;
using System.IO;
using IronRuby;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json.Linq;
using Raven.Database.Extensions;
using Raven.Database.Plugins;
using Raven.Http;

namespace Raven.Bundles.Scripting
{
	public class ScriptingPutTrigger : AbstractPutTrigger
	{
		private readonly List<ScriptSource> puts = new List<ScriptSource>();
		private readonly ScriptEngine pythonEngine = Ruby.CreateEngine();

		public override void Initialize()
		{
			string scriptsDirectory = Database.Configuration.Settings["Raven/ScriptsDirectory"] ?? @"~\Scripts";
			scriptsDirectory = scriptsDirectory.ToFullPath();
			string putsScriptsDirectory = Path.Combine(scriptsDirectory.ToFullPath(), "Puts");
			if (Directory.Exists(putsScriptsDirectory) == false)
				return;

			foreach (string file in Directory.GetFiles(putsScriptsDirectory, "*.rb"))
			{
				puts.Add(pythonEngine.CreateScriptSourceFromString(file));
			}
		}

		public override VetoResult AllowPut(string key, JObject document, JObject metadata,
		                                    TransactionInformation transactionInformation)
		{
			ScriptScope scriptScope = pythonEngine.CreateScope();
			scriptScope.SetVariable("document", document);
			scriptScope.SetVariable("metadata", metadata);

			var reasonsToReject = new List<string>();
			scriptScope.SetVariable("reject", reasonsToReject);

			foreach (var scriptSource in puts)
			{
				scriptSource.Execute(scriptScope);
			}

			if(reasonsToReject.Count == 0)
				return VetoResult.Allowed;

			return VetoResult.Deny(string.Join(Environment.NewLine, reasonsToReject));
		}
	}
}