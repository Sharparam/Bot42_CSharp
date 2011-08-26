using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

namespace CSharpBot
{
	public class Bot
	{
		private readonly string _server;
		private readonly int _port;
		private string _channel;
		private string _nick;
		public int NickNum = 2;
		private Random _rand;

		private bool _quitting;

		private TcpClient _ircServ;
		private NetworkStream _ircStream;
		private StreamWriter _ircWriter;
		private StreamReader _ircReader;

		private MessageHandler _msgHandler;
		private QuoteHandler _quoteHandler;

		private readonly List<string> _joinQueue;
		private readonly List<string> _joinedChannels;
		private Dictionary<string, List<string>> _chanOps;

		public string Nick { get { return _nick; } }

		public Bot(string server, int port, string nick)
		{
			_server = server;
			_port = port;
			_nick = nick;
			_msgHandler = new MessageHandler(this);
			_quoteHandler = new QuoteHandler("Quotes");
			_joinQueue = new List<string>();
			_joinedChannels = new List<string>();
			_chanOps = new Dictionary<string, List<string>>();
		}

		public void Connect()
		{
			try
			{
				Console.WriteLine("Connecting to {0} on port {1}...", _server, _port);
				int connTries = 0;
				bool connected = false;
				while (!connected && connTries < 5)
				{
					connTries++;
					Console.WriteLine("Connection try #{0}...", connTries);
					try
					{
						_ircServ = new TcpClient(_server, _port);
					}
					catch (Exception)
					{
						Console.WriteLine("Failed to connect" + (connTries < 5 ? ", retrying..." : "."));
					}
				}
				if (!connected)
					throw new Exception(string.Format("Connection failed after {0} tries.", connTries));
				Console.WriteLine("Creating server stream...");
				_ircStream = _ircServ.GetStream();
				Console.WriteLine("Creating IRC reader...");
				_ircReader = new StreamReader(_ircStream);
				Console.WriteLine("Creating IRC writer...");
				_ircWriter = new StreamWriter(_ircStream);
				Console.WriteLine("Setting writer properties...");
				_ircWriter.AutoFlush = true;
				Console.WriteLine("Sending NICK command with parameter: {0}", _nick);
				_ircWriter.WriteLine("NICK {0}", _nick);
				Console.WriteLine("Sending USER command with parameter: {0} 0 * :{0}", _nick);
				_ircWriter.WriteLine("USER {0} 0 * :{0}", _nick);
			}
			catch(Exception ex)
			{
				Console.WriteLine("[ERR] Exception: " + ex.GetType() + " " + ex.Message);
				_quitting = true;
			}
			string inputLine;
			while (!_quitting)
			{
				while ((inputLine = _ircReader.ReadLine()) != null)
				{
					_msgHandler.HandleMessage(inputLine);
				}
			}
			Disconnect();
		}

		public void Disconnect()
		{
			if (_ircServ == null)
			{
				Console.WriteLine("Connection failed. Exiting...");
				return;
			}
			Console.WriteLine("Disconnecting from {0}", _server);
			if (!_quitting)
				Quit();
			Console.WriteLine("Closing IRC writer...");
			_ircWriter.Close();
			Console.WriteLine("Closing IRC reader...");
			_ircReader.Close();
			Console.WriteLine("Closing IRC stream...");
			_ircStream.Close();
			Console.WriteLine("Closing server connection...");
			_ircServ.Close();
			Console.WriteLine("Disconnected from {0}", _server);
		}

		public void LoadQuotes()
		{
			_quoteHandler.LoadAllQuotes();
		}

		public List<string> GetLoadedQuotes()
		{
			return _quoteHandler.GetLoadedQuotes();
		}

		public string GetRandomQuote(string quoteName)
		{
			if (!_quoteHandler.QuotesLoaded(quoteName))
			{
				Console.WriteLine("Quotes from " + quoteName + " are not loaded.");
				return quoteName + " quotes not loaded!";
			}
			var tempQuotes = _quoteHandler.GetQuotes(quoteName);
			_rand = new Random(DateTime.Now.Millisecond);
			int randIndex = _rand.Next(0, tempQuotes.Count - 1);
			string quote = string.Format("{0}. {1}", randIndex + 1, tempQuotes[randIndex]);
			return quote;
		}

		public string GetQuote(string quoteName, int quoteIndex)
		{
			if (!_quoteHandler.QuotesLoaded(quoteName))
			{
				Console.WriteLine("Quotes from " + quoteName + " are not loaded.");
				return quoteName + " quotes not loaded!";
			}
			var tempQuotes = _quoteHandler.GetQuotes(quoteName);
			string quote;
			try
			{
				quote = string.Format("{0}. {1}", quoteIndex + 1, tempQuotes[quoteIndex]);
			}
			catch(ArgumentOutOfRangeException ex)
			{
				Console.WriteLine(ex.GetType() + "! Details: " + ex.Message);
				quote = "The specified quote was not found in the quote list.";
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.GetType() + "! Details: " + ex.Message);
				quote = "Unknown error occurred (" + ex.GetType() + ")!";
			}
			return quote;
		}

		public void ChangeNick(string newNick)
		{
			_nick = newNick;
			SendRaw("NICK " + _nick);
		}

		public static string HostToNick(string hostString)
		{
			return hostString.TrimStart(':').Split('!')[0];
		}

		public static string ParseChannel(string channel)
		{
			if (!channel.StartsWith("#"))
				channel = "#" + channel;
			return channel;
		}

		public void JoinChannel(string channel)
		{
			channel = ParseChannel(channel);
			if (_joinedChannels.Contains(channel))
			{
				Console.WriteLine("ERROR: Can't join channel {0}, already in that channel.", channel);
				return;
			}
			_ircWriter.WriteLine("JOIN {0}", channel);
			_joinedChannels.Add(channel);
			if (_chanOps.ContainsKey(channel))
				_chanOps.Remove(channel);
			_chanOps.Add(channel, new List<string>());
			Console.WriteLine("Joined channel: {0}", channel);
		}

		public void AddJoinQueue(string channel)
		{
			channel = ParseChannel(channel);
			if (!_joinQueue.Contains(ParseChannel(channel)))
			{
				_joinQueue.Add(channel);
				Console.WriteLine("Added channel {0} to join queue.", channel);
			}
		}

		public void DoJoinQueue()
		{
			foreach (var channel in _joinQueue)
			{
				JoinChannel(channel);
			}
			_joinQueue.Clear();
		}

		public void PartChannel(string channel)
		{
			channel = ParseChannel(channel);

			if (!_joinedChannels.Contains(channel))
				return;

			_ircWriter.WriteLine("PART {0}", channel);
			Console.WriteLine("Parted channel {0}", channel);
			_joinedChannels.Remove(channel);
		}

		public void SetOp(string channel, string user)
		{
			channel = ParseChannel(channel);
			if (_chanOps.ContainsKey(channel))
			{
				_chanOps[channel].Clear();
				AddOp(channel, user);
			}
		}

		public void SetOps(string channel, string[] users)
		{
			channel = ParseChannel(channel);
			if (_chanOps.ContainsKey(channel))
			{
				Console.WriteLine("Resetting ops list for channel {0}...", channel);
				_chanOps[channel].Clear();
				AddOps(channel, users);
			}
		}

		public void AddOp(string channel, string user)
		{
			channel = ParseChannel(channel);
			if (_chanOps.ContainsKey(channel))
			{
				foreach (var op in _chanOps[channel])
				{
					if (user == op)
						return;
				}
				user = user.TrimStart(new[]{':', '@'});
				_chanOps[channel].Add(user);
				Console.WriteLine("Added {0} to the op list of {1}", user, channel);
			}
		}

		public void AddOps(string channel, string[] users)
		{
			foreach (var user in users)
			{
				AddOp(channel, user);
			}
		}

		public int IsOp(string channel, string user)
		{
			channel = ParseChannel(channel);
			foreach (var op in Globals.Ops)
			{
				if (user == op)
				{
					Console.WriteLine("IsOp: User \"{0}\" was found in the global ops array.", user);
					return 2;
				}
			}
			if (_chanOps.ContainsKey(channel))
				if (_chanOps[channel].Contains(user))
				{
					Console.WriteLine("IsOp: User \"{0}\" is an op in channel {1}", user, channel);
					return 1;
				}
			return 0;
		}

		public bool IsChannel(string channel)
		{
			return channel.StartsWith("#");
		}

		public bool IsInChannel(string channel)
		{
			return _joinedChannels.Contains(channel);
		}

		public void Quit()
		{
			Quit("Quit");
		}

		public void Quit(string quitMsg)
		{
			Console.WriteLine("Quitting with message \"{0}\"...", quitMsg);
			_ircWriter.WriteLine("QUIT :{0}", quitMsg);
			_quitting = true;
		}

		public void SendRaw(string msg)
		{
			Console.WriteLine("[OUT] {0}", msg);
			_ircWriter.WriteLine(msg);
		}

		public void SendToChannel(string msg, string channel)
		{
			channel = ParseChannel(channel);

			if (_joinedChannels.Contains(channel))
			{
				Console.WriteLine("[OUT] PRIVMSG {0} :{1}", channel, msg);
				_ircWriter.WriteLine("PRIVMSG {0} :{1}", channel, msg);
			}
		}

		public void SendToNick(string msg, string nick)
		{
			Console.WriteLine("[OUT] NOTICE {0} :{1}", nick, msg);
			_ircWriter.WriteLine("NOTICE {0} :{1}", nick, msg);
		}
	}
}
