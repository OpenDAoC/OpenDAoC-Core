using System;

namespace DOL
{
	/// <summary>
	/// A wrapper for <see cref="WeakReference" /> to handle null targets under Mono.
	/// </summary>
	/// <remarks>A flaw in the Mono GC crashes the Mono runtime when you provide 
	/// a null target to WeakReference.  This wrapper exists to work around the 
	/// problem when running under the Mono runtime.</remarks>
	public class WeakRef : WeakReference
	{
		private static readonly NullValue Null = new NullValue();

		/// <summary>
		/// Creates a new weak reference to the given target.
		/// </summary>
		/// <param name="target">The target of this weak reference</param>
		public WeakRef(object target)
			: base(target ?? Null)
		{
		}

		/// <summary>
		/// Creates a new weak reference to the given target, taking into consideration 
		/// resurrection tracking.
		/// </summary>
		/// <param name="target">The target of this weak reference</param>
		/// <param name="trackResurrection">Track the resurrection of the target</param>
		public WeakRef(object target, bool trackResurrection)
			: base(target ?? Null, trackResurrection)
		{
		}

		/// <summary>
		/// Gets or sets the currently referenced target.
		/// </summary>
		public override object Target
		{
			get
			{
				object o = base.Target;
				return ((o == Null) ? null : o);
			}
			set { base.Target = value ?? Null; }
		}

		#region Nested type: NullValue

		private class NullValue
		{
		} ;

		#endregion
	}
}