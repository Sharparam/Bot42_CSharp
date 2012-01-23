using System;

namespace CSharpBot.Bot42
{
	class Program
	{
		static void Main(string[] args)
		{
			Bot testBot = BotHandler.CreateBot("localhost", 49186, "Bot42_CSharp");
			testBot.AddJoinQueue("#Bot42");
			testBot.AddJoinQueue("#Bot42_CSharp");
			testBot.AddJoinQueue("#botz");
			testBot.AddJoinQueue("#CSharp");
			testBot.Connect();
			Console.WriteLine("Bot terminated, press any key to exit...");
			Console.Read();
		}
	}
}
