using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpBot
{
	public class MessageHandler
	{
		private Bot _bot;
		private const char _CMD_PREFIX = '.';

		public MessageHandler(Bot bot)
		{
			_bot = bot;
		}

		public void HandleMessage(string msg)
		{
			string[] splitMsg = msg.Split(' ');
			string channel;

			Console.WriteLine(" [IN] {0}", msg);

			switch(splitMsg[0])
			{
				case "PING": //We should respond with PONG
					_bot.SendRaw(string.Format("PONG {0}", splitMsg[1]));
					break;
				default:
					switch (splitMsg[1])
					{
						case "001": //First message sent by server after connecting
							_bot.DoJoinQueue();
							break;
						case "353": //Sent after a NAMES command
							channel = splitMsg[4];
							var users = new List<string>();
							for (int i = 5; i < splitMsg.Length; i++ )
								if (splitMsg[i].TrimStart(':').StartsWith("@"))
									users.Add(splitMsg[i]);
							_bot.SetOps(channel, users.ToArray());
							break;
						case "433": //Nick already in use
							_bot.ChangeNick(_bot.Nick + "|2");
							break;
						case "JOIN": //User joins channel
							string nick = Bot.HostToNick(splitMsg[0]);
							if (nick == _bot.Nick)
								break;
							channel = splitMsg[2].TrimStart(':');
							_bot.SendRaw(string.Format("NAMES {0}", channel));
							_bot.SendToChannel(string.Format("Welcome to {0}, {1}!", channel, nick), channel);
							_bot.SendToNick(string.Format("Your current access level in {0} is {1}", channel, _bot.IsOp(channel, nick)), nick);
							break;
						case "MODE":
							if (splitMsg[3].ToLower().Contains("o"))
							{
								channel = splitMsg[2];
								_bot.SendRaw("NAMES " + channel);
							}
							break;
						case "PART": //User parts channel
							break;
						case "QUIT": //User quits
							break;
						case "PRIVMSG": //Message received (either private or in one of the joined channels)
							if ((_bot.IsChannel(splitMsg[2]) || splitMsg[2] == _bot.Nick) && splitMsg[3].StartsWith(":."))
							{
								string user = splitMsg[0].TrimStart(':').Split('!')[0];
								string command = string.Empty;
								for (int i = 3; i < splitMsg.Length; i++)
								{
									command += splitMsg[i] + ' ';
								}
								command = command.TrimStart(new char[]{':', '.'});
								HandleCommand(command, splitMsg[2], user);
							}
							break;
						default:
							break;
					}
					break;
			}
		}

		private void HandleCommand(string command, string channel, string user)
		{
			if (_bot.IsOp(channel, user) < 1)
				return;
			string[] args = command.Split(' ');
			string arg = string.Empty;
			for (int i = 1; i < args.Length; i++)
				arg += args[i] + ' ';
			if (!_bot.IsChannel(channel)) //This was a private message
			{
				channel = args[1];
				if (args[0] != "raw" && args[0] != "irccmd" && args[0] != "cmd" && args[0] != "command")
				{
					arg = string.Empty;
					for (int i = 2; i < args.Length; i++)
						arg += args[i];
				}
			}

			switch (args[0])
			{
				case "print":
				case "echo":
				case "say":
					_bot.SendToChannel(arg, channel);
					break;
				case "act":
				case "do":
				case "me":
				case "em":
				case "emote":
					_bot.SendRaw(string.Format("PRIVMSG {0} :\u0001ACTION {1}\u0001", channel, arg));
					break;
				case "raw":
				case "irccmd":
				case "cmd":
				case "command":
					_bot.SendRaw(arg);
					break;
				case "join":
					if (!string.IsNullOrEmpty(args[1]))
						_bot.JoinChannel(args[1]);
					break;
				case "part":
					if (string.IsNullOrEmpty(args[1]))
						_bot.PartChannel(channel);
					else if (_bot.IsOp(channel, user) >= 2 && _bot.IsChannel(args[1]))
						_bot.PartChannel(args[1]);
					break;
				case "exit":
				case "quit":
					if (user.ToLower().Contains("bot"))
					{
						_bot.SendToChannel("Bots can't harm me.", channel);
						return;
					}
					if (!string.IsNullOrEmpty(arg))
						_bot.Quit(arg);
					else
						_bot.Quit();
					break;
				default:
					break;
			}
		}
	}
}
