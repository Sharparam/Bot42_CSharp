using System;
using System.Collections.Generic;

namespace CSharpBot
{
	public static class BotHandler
	{
		private static readonly List<Bot> _bots = new List<Bot>();

		public static Bot CreateBot(string server, int port, string nick)
		{
			var bot = new Bot(server, port, nick);
			_bots.Add(bot);
			return bot;
		}

		public static Bot GetBot(string name)
		{
			foreach (var bot in _bots)
			{
				if (bot.Nick == name)
					return bot;
			}
			return null;
		}
	}
}
