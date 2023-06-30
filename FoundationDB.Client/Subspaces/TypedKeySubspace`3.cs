#region BSD License
/* Copyright (c) 2005-2023 Doxense SAS
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
	* Redistributions of source code must retain the above copyright
	  notice, this list of conditions and the following disclaimer.
	* Redistributions in binary form must reproduce the above copyright
	  notice, this list of conditions and the following disclaimer in the
	  documentation and/or other materials provided with the distribution.
	* Neither the name of Doxense nor the
	  names of its contributors may be used to endorse or promote products
	  derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion

namespace FoundationDB.Client
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Runtime.CompilerServices;
	using Doxense.Collections.Tuples;
	using Doxense.Diagnostics.Contracts;
	using Doxense.Serialization.Encoders;
	using JetBrains.Annotations;

	/// <summary>Represents a key subspace than can encoded pairs of statically typed values to and from their binary representation</summary>
	/// <typeparam name="T1">Type of the first element of the key</typeparam>
	/// <typeparam name="T2">Type of the second element of the key</typeparam>
	/// <typeparam name="T3">Type of the third element of the key</typeparam>
	[PublicAPI]
	public interface ITypedKeySubspace<T1, T2, T3> : IKeySubspace
	{

		/// <summary>Encoding used to generate and parse the keys of this subspace</summary>
		ICompositeKeyEncoder<T1, T2, T3> KeyEncoder { get; }

		Slice this[T1? item1, T2? item2, T3? item3] { [Pure]  get; }

		Slice this[in (T1, T2, T3) items] { [Pure]  get; }

		[Pure]
		Slice Encode(T1? item1, T2? item2, T3? item3);

		/// <summary>Encode only the first part of pair into a key in this subspace</summary>
		/// <param name="item1">First part of the key</param>
		/// <returns>Partial key that can be used to create custom <see cref="KeyRange">key ranges</see></returns>
		Slice EncodePartial(T1? item1);

		/// <summary>Encode only the first and second part of pair into a key in this subspace</summary>
		/// <param name="item1">First part of the key</param>
		/// <param name="item2">Second part of the key</param>
		/// <returns>Partial key that can be used to create custom <see cref="KeyRange">key ranges</see></returns>
		Slice EncodePartial(T1? item1, T2? item2);

		(T1, T2, T3) Decode(Slice packedKey);

		/// <summary>Decode only some elements of a key from this subspace</summary>
		/// <param name="packedKey">Key previously generated by calling <see cref="Encode"/></param>
		/// <param name="count">Number of elements to decode (from 1 to 3)</param>
		/// <returns>Only the first <see cref="count"/> elements in the result are decoded. The remaining elements will be discarded.</returns>
		(T1, T2, T3) DecodePartial(Slice packedKey, int count);

	}

	/// <summary>Represents a key subspace than can encoded pairs of statically typed values to and from their binary representation</summary>
	/// <typeparam name="T1">Type of the first element of the key</typeparam>
	/// <typeparam name="T2">Type of the second element of the key</typeparam>
	/// <typeparam name="T3">Type of the third element of the key</typeparam>
	[PublicAPI]
	public sealed class TypedKeySubspace<T1, T2, T3> : KeySubspace, ITypedKeySubspace<T1, T2, T3>
	{
		public ICompositeKeyEncoder<T1, T2, T3> KeyEncoder { get; }

		internal TypedKeySubspace(Slice prefix, ICompositeKeyEncoder<T1, T2, T3> encoder, ISubspaceContext context)
			: base(prefix, context)
		{
			Contract.Debug.Requires(encoder != null);
			this.KeyEncoder = encoder;
		}

		public Slice this[T1 item1, T2 item2, T3 item3]
		{
			[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Encode(item1, item2, item3);
		}

		public Slice this[in (T1, T2, T3) items]
		{
			[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Encode(items.Item1, items.Item2, items.Item3);
		}

		[Pure]
		public Slice Encode(T1 item1, T2 item2, T3 item3)
		{
			var sw = this.OpenWriter(3 * 16);
			this.KeyEncoder.WriteKeyTo(ref sw, (item1, item2, item3));
			return sw.ToSlice();
		}

		[Pure]
		public Slice EncodePartial(T1 item1, T2 item2)
		{
			var sw = this.OpenWriter(2 * 16);
			var tuple = (item1, item2, default(T3));
			this.KeyEncoder.WriteKeyPartsTo(ref sw, 2, in tuple);
			return sw.ToSlice();
		}

		[Pure]
		public Slice EncodePartial(T1 item1)
		{
			var sw = this.OpenWriter(16);
			var tuple = (item1, default(T2), default(T3));
			this.KeyEncoder.WriteKeyPartsTo(ref sw, 1, in tuple);
			return sw.ToSlice();
		}

		[Pure]
		public (T1, T2, T3) Decode(Slice packedKey)
		{
			return this.KeyEncoder.DecodeKey(ExtractKey(packedKey));
		}

		public (T1, T2, T3) DecodePartial(Slice packedKey, int count)
		{
			return this.KeyEncoder.DecodeKeyParts(count, ExtractKey(packedKey));
		}

		#region Dump()

		/// <summary>Return a user-friendly string representation of a key of this subspace</summary>
		[Pure]
		public override string PrettyPrint(Slice packedKey)
		{
			if (packedKey.IsNull) return "<null>";
			var key = ExtractKey(packedKey, boundCheck: true);

			if (this.KeyEncoder.TryDecodeKey(key, out var items))
			{
				return items.ToSTuple().ToString();
			}

			// decoding failed, or some other non-trivial error
			return key.PrettyPrint();
		}

		#endregion

	}

	public static partial class TypedKeysExtensions
	{

		#region Ranges...

		/// <summary>Return the range of all legal keys in this subspace, that start with the specified triple of values</summary>
		/// <returns>Range that encompass all keys that start with (tuple.Item1, tuple.Item2, tuple.Item3)</returns>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static KeyRange PackRange<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, STuple<T1, T2, T3> tuple)
		{
			return KeyRange.PrefixedBy(self.Encode(tuple.Item1, tuple.Item2, tuple.Item3));
		}

		/// <summary>Return the range of all legal keys in this subspace, that start with the specified triple of values</summary>
		/// <returns>Range that encompass all keys that start with (tuple.Item1, tuple.Item2, tuple.Item3)</returns>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static KeyRange PackRange<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, (T1, T2, T3) tuple)
		{
			return KeyRange.PrefixedBy(self.Encode(tuple.Item1, tuple.Item2, tuple.Item3));
		}

		/// <summary>Return the range of all legal keys in this subspace, that start with the specified triple of values</summary>
		/// <returns>Range that encompass all keys that start with (item1, item2, item3)</returns>
		public static KeyRange EncodeRange<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, T1? item1, T2? item2, T3? item3)
		{
			//HACKHACK: add concept of "range" on  IKeyEncoder ?
			return KeyRange.PrefixedBy(self.Encode(item1, item2, item3));
		}

		/// <summary>Return the range of all legal keys in this subspace, that start with the specified triple of values</summary>
		/// <returns>Range that encompass all keys that start with (item1, item2, item3)</returns>
		public static KeyRange PackPartialRange<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, STuple<T1, T2> tuple)
		{
			//HACKHACK: add concept of "range" on  IKeyEncoder ?
			return KeyRange.PrefixedBy(self.EncodePartial(tuple.Item1, tuple.Item2));
		}

		/// <summary>Return the range of all legal keys in this subspace, that start with the specified triple of values</summary>
		/// <returns>Range that encompass all keys that start with (item1, item2, item3)</returns>
		public static KeyRange PackPartialRange<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, (T1, T2) tuple)
		{
			//HACKHACK: add concept of "range" on  IKeyEncoder ?
			return KeyRange.PrefixedBy(self.EncodePartial(tuple.Item1, tuple.Item2));
		}

		/// <summary>Return the range of all legal keys in this subspace, that start with the specified triple of values</summary>
		/// <returns>Range that encompass all keys that start with (item1, item2, item3)</returns>
		public static KeyRange EncodePartialRange<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, T1? item1, T2? item2)
		{
			//HACKHACK: add concept of "range" on  IKeyEncoder ?
			return KeyRange.PrefixedBy(self.EncodePartial(item1, item2));
		}

		/// <summary>Return the range of all legal keys in this subspace, that start with the specified triple of values</summary>
		/// <returns>Range that encompass all keys that start with (item1, item2, item3)</returns>
		public static KeyRange EncodePartialRange<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, T1? item1)
		{
			//HACKHACK: add concept of "range" on  IKeyEncoder ?
			return KeyRange.PrefixedBy(self.EncodePartial(item1));
		}

		#endregion

		#region Pack()

		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Slice Pack<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, (T1, T2, T3) tuple)
		{
			return self.Encode(tuple.Item1, tuple.Item2, tuple.Item3);
		}

		[Pure]
		public static Slice Pack<T1, T2, T3, TTuple>(this ITypedKeySubspace<T1, T2, T3> self, TTuple tuple)
			where TTuple : IVarTuple
		{
			tuple.OfSize(3);
			return self.Encode(tuple.Get<T1>(0), tuple.Get<T2>(1), tuple.Get<T3>(2));
		}

		/// <summary>Encode an array of items into an array of keys</summary>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Slice[] Pack<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, params (T1, T2, T3)[] items)
		{
			return self.KeyEncoder.EncodeKeys(self.GetPrefix(), items);
		}

		/// <summary>Encode an array of items into an array of keys</summary>
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IEnumerable<Slice> Pack<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, IEnumerable<(T1, T2, T3)> items)
		{
			return self.KeyEncoder.EncodeKeys(self.GetPrefix(), items);
		}

		#endregion

		#region Decode()

		public static void Decode<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, Slice packedKey, out T1? item1, out T2? item2, out T3? item3)
		{
			(item1, item2, item3) = self.Decode(packedKey);
		}

		public static void DecodePartial<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, Slice packedKey, out T1? item1, out T2? item2)
		{
			(item1, item2, _) = self.DecodePartial(packedKey, 2);
		}

		/// <summary>Decode only the first element of the key</summary>
		public static T1 DecodeFirst<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, Slice packedKey)
		{
			//TODO: PERF: we need to add "DecodeLast" to key encoders because this is very frequently called (indexes!)
			// => for now, we have to decode the whole tuple, and throw all items except the last one!
			return self.DecodePartial(packedKey, 1).Item1;
		}

		/// <summary>Decode only the first element of the key</summary>
		public static void DecodeFirst<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, Slice packedKey, out T1? first)
		{
			//TODO: PERF: we need to add "DecodeLast" to key encoders because this is very frequently called (indexes!)
			// => for now, we have to decode the whole tuple, and throw all items except the last one!
			(first, _, _) = self.DecodePartial(packedKey, 1);
		}

		/// <summary>Decode only the last element of the key</summary>
		public static T3 DecodeLast<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, Slice packedKey)
		{
			//TODO: PERF: we need to add "DecodeLast" to key encoders because this is very frequently called (indexes!)
			// => for now, we have to decode the whole tuple, and throw all items except the last one!
			return self.Decode(packedKey).Item3;
		}

		/// <summary>Decode only the last element of the key</summary>
		public static void DecodeLast<T1, T2, T3>(this ITypedKeySubspace<T1, T2, T3> self, Slice packedKey, out T3? last)
		{
			//TODO: PERF: we need to add "DecodeLast" to key encoders because this is very frequently called (indexes!)
			// => for now, we have to decode the whole tuple, and throw all items except the last one!
			last = self.Decode(packedKey).Item3;
		}

		#endregion

	}

}
