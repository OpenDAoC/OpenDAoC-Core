using System;
using System.Reflection;
using System.Text;
using DOL.Logging;
using DOL.Timing;

namespace DOL.Events
{
    // Represents a LIFO chain of event handlers for a specific event.
    // This class is immutable; adding or removing handlers creates new instances of the chain.
    public sealed class DOLEventHandlerChain
    {
        private static readonly Logger Log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const int LONG_EXECUTION_THRESHOLD = 5;

        private readonly DOLEventHandler _handler;
        private readonly DOLEventHandlerChain _next;

        public DOLEventHandlerChain(DOLEventHandler handler) : this(handler, null) { }

        private DOLEventHandlerChain(DOLEventHandler handler, DOLEventHandlerChain next)
        {
            ArgumentNullException.ThrowIfNull(handler);
            _handler = handler;
            _next = next;
        }

        public static DOLEventHandlerChain Subscribe(DOLEventHandlerChain chain, DOLEventHandler handler)
        {
            return chain == null ? new(handler) : chain.Subscribe(handler);
        }

        public static DOLEventHandlerChain SubscribeOnce(DOLEventHandlerChain chain, DOLEventHandler handler)
        {
            return chain == null ? new(handler) : chain.SubscribeOnce(handler);
        }

        private DOLEventHandlerChain Subscribe(DOLEventHandler handler)
        {
            return new(handler, this);
        }

        private DOLEventHandlerChain SubscribeOnce(DOLEventHandler handler)
        {
            return Contains(handler) ? this : Subscribe(handler);
        }

        private bool Contains(DOLEventHandler handler)
        {
            if (handler == null)
                return false;

            DOLEventHandlerChain current = this;

            while (current != null)
            {
                if (current._handler == handler)
                    return true;

                current = current._next;
            }

            return false;
        }

        public static DOLEventHandlerChain Unsubscribe(DOLEventHandlerChain chain, DOLEventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(chain);

            if (chain._handler == handler)
                return chain._next;

            DOLEventHandlerChain newNext = Unsubscribe(chain._next, handler);
            return newNext == chain._next ? chain : new(chain._handler, newNext);
        }

        public void Invoke(DOLEvent e, object sender, EventArgs args)
        {
            DOLEventHandlerChain current = this;

            while (current != null)
            {
                long startTick = MonotonicTime.NowMs;

                try
                {
                    current._handler(e, sender, args);
                }
                catch (Exception ex)
                {
                    if (Log.IsErrorEnabled)
                        Log.Error($"Error in {nameof(Invoke)}. Event: {e?.Name ?? "null"}", ex);
                }

                long stopTick = MonotonicTime.NowMs;

                if (stopTick - startTick > LONG_EXECUTION_THRESHOLD)
                    Log.Warn($"{nameof(Invoke)} took {stopTick - startTick}ms. Target: {current}");

                current = current._next;
            }
        }

        public string Dump()
        {
            StringBuilder builder = new();
            DOLEventHandlerChain current = this;
            int count = 0;

            while (current != null)
            {
                count++;
                builder.Append('\t')
                    .Append(count)
                    .Append(") ")
                    .Append(current._handler.Target)
                    .Append('.')
                    .Append(current._handler.Method.Name)
                    .AppendLine();
                current = current._next;
            }

            return builder.ToString();
        }

        public override string ToString()
        {
            if (_handler == null)
                return "(null)";

            object target = _handler.Target;
            MethodInfo method = _handler.Method;

            return new StringBuilder(64)
                .Append("method: ").Append(method.DeclaringType?.FullName ?? "(null)")
                .Append('.').Append(method.Name)
                .Append(" target: ").Append(target?.ToString() ?? "null")
                .ToString();
        }
    }
}
