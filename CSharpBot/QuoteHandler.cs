using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;

namespace CSharpBot
{
	class QuoteHandler
	{
		private readonly string _quoteDir;
		private readonly Dictionary<string, List<string>> _quoteDb = new Dictionary<string, List<string>>();
		private BackgroundWorker _quoteLoader;

		public QuoteHandler(string quoteDir)
		{
			_quoteDir = quoteDir;

			if (!Directory.Exists(_quoteDir))
			{
				Console.WriteLine("The quote directory (Quotes) was not found.");
				return;
			}

			//_quoteDb = new Dictionary<string, List<string>>();
		}

		public void LoadQuotes(string quoteName)
		{
			if (_quoteDb.ContainsKey(quoteName))
			{
				Console.WriteLine("Quotes already loaded! (" + quoteName + ")");
				return;
			}

			string quoteFile = _quoteDir + "\\" + quoteName + ".txt";

			if (!File.Exists(quoteFile))
			{
				Console.WriteLine("The quote file requested was not found (" + quoteFile + ")!");
				return;
			}

			//Load the quotes in another thread, this may take some time depending on the size of the quote file
			Console.WriteLine("Starting QuoteLoader thread...");
			_quoteLoader = new BackgroundWorker();
			_quoteLoader.DoWork += ReadQuotes;
			_quoteLoader.RunWorkerAsync(new[]{quoteFile, quoteName});
		}

		public void LoadAllQuotes()
		{
			string[] files = Directory.GetFiles(_quoteDir);
			if (files.Length < 1)
			{
				Console.WriteLine("No quote files to load!");
				return;
			}
			foreach (var file in files)
			{
				try
				{
					string quoteName = Path.GetFileNameWithoutExtension(file);
					Console.WriteLine("Loading " + quoteName + " quotes...");
					LoadQuotes(quoteName);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Unhandled exception " + ex.GetType() + " occurred! Details: " + ex.Message);
				}
				
			}
		}

		private void ReadQuotes(object sender, DoWorkEventArgs e)
		{
			var args = (string[]) e.Argument;
			string quoteFile = args[0];
			string quoteName = args[1];
			Console.WriteLine("Reading quotes from " + quoteFile + "...");
			string[] lines = File.ReadAllLines(quoteFile);
			Console.WriteLine("Found " + lines.Length + " quotes in " + quoteFile);
			Console.WriteLine("Loading quotes from " + quoteFile + " into quote database...");
			var tempQuotes = new List<string>();
			int quoteNum = 1;
			foreach (var line in lines)
			{
				tempQuotes.Add(line);
				Console.WriteLine("Quote #" + quoteNum + " added to " + quoteName + " quotes!");
				if (quoteNum < lines.Length)
					quoteNum++;
			}
			Console.WriteLine(quoteNum + " quotes loaded into " + quoteName + "!");
			Console.WriteLine("Saving " + quoteName + " quotes to database...");
			_quoteDb.Add(quoteName, tempQuotes);
			Console.WriteLine("Done!");
		}

		public bool QuotesLoaded(string quoteName)
		{
			return _quoteDb.ContainsKey(quoteName);
		}

		public List<string> GetLoadedQuotes()
		{
			return _quoteDb.Keys.ToList();
		}

		public List<string> GetQuotes(string quoteName)
		{
			return _quoteDb.ContainsKey(quoteName) ? _quoteDb[quoteName] : new List<string>();	
		}
	}
}
