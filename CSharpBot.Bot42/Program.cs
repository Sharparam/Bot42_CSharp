using System;

namespace CSharpBot.Bot42
{
	class Program
	{
		static void Main(string[] args)
		{
			Bot testBot = BotHandler.CreateBot("irc.kottnet.net", 6667, "Bot42_CSharp");
			testBot.AddJoinQueue("#Bot42");
			//testBot.AddJoinQueue("#botz");
			testBot.Connect();
			Console.WriteLine("Bot terminated, press any key to exit...");
			Console.Read();
		}
	}
}
