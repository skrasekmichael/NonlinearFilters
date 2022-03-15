using NonlinearFilters.APP.Messages;

namespace NonlinearFilters.APP.Services
{
	public class Mediator
	{
		private readonly Dictionary<Type, List<Delegate>> registeredActions = new();

		public void Register<TMessage>(Action<TMessage> action) where TMessage : IMessage
		{
			var key = typeof(TMessage);
			if (!registeredActions.TryGetValue(key, out _))
				registeredActions[key] = new List<Delegate>();

			registeredActions[key].Add(action);
		}

		public void Send<TMessage>(TMessage message) where TMessage : IMessage
		{
			if (!registeredActions.TryGetValue(message.GetType(), out List<Delegate>? actions))
				return;

			foreach (var action in actions)
				action.DynamicInvoke(message);
		}

		public void UnRegister<TMessage>(Action<TMessage> action) where TMessage : IMessage
		{
			var key = typeof(TMessage);
			if (!registeredActions.TryGetValue(typeof(TMessage), out List<Delegate>? actions))
				return;

			actions.Remove(action);
			registeredActions[key] = new List<Delegate>(actions);
		}
	}
}
