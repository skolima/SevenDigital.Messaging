using System;

namespace SevenDigital.Messaging.EventStoreHooks
{
	public class ConsoleEventStoreHook : IEventStoreHook
	{
		public void MessageSent(IMessage msg){
			Console.WriteLine("Sent: "+msg);
		}
		public void MessageReceived(IMessage msg){
			Console.WriteLine("Got: "+msg);
		}
	}
}