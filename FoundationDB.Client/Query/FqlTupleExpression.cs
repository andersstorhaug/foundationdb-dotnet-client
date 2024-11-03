﻿#region Copyright (c) 2023-2024 SnowBank SAS, (c) 2005-2023 Doxense SAS
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 	* Redistributions of source code must retain the above copyright
// 	  notice, this list of conditions and the following disclaimer.
// 	* Redistributions in binary form must reproduce the above copyright
// 	  notice, this list of conditions and the following disclaimer in the
// 	  documentation and/or other materials provided with the distribution.
// 	* Neither the name of SnowBank nor the
// 	  names of its contributors may be used to endorse or promote products
// 	  derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL SNOWBANK SAS BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

#if NET8_0_OR_GREATER

namespace FoundationDB.Client
{
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Numerics;
	using System.Runtime.InteropServices;
	using System.Text;
	using Doxense.Collections.Tuples;

	public enum FqlItemType
	{
		Variable = -2,
		MaybeMore = -1,

		Invalid = 0,

		Nil,
		Bool,
		Int,
		Num,
		String,
		Bytes,
		Uuid,
		Tuple,

	}

	public enum FqlVariableType
	{
		//note: must be in sync with FqlVariableTypes (flags)

		Any = -1,

		None = 0,

		Nil = 1 << 0,
		Bool = 1 << 1,
		Int = 1 << 2,
		Num = 1 << 3,
		String = 1 << 4,
		Uuid = 1 << 5,
		Bytes = 1 << 6,
		Tuple = 1 << 7,
	}

	[Flags]
	public enum FqlVariableTypes
	{
		//note: must be in sync with FqlVariableType (non-flags)

		Any = -1,

		None = 0,

		Nil = 1 << 0,
		Bool = 1 << 1,
		Int = 1 << 2,
		Num = 1 << 3,
		String = 1 << 4,
		Uuid = 1 << 5,
		Bytes = 1 << 6,
		Tuple = 1 << 7,
	}

	[DebuggerDisplay("{ToString(),nq}")]
	public readonly struct FqlTupleItem : IFqlExpression,
		IEquatable<FqlTupleItem>,
		IEquatable<int>,
		IEquatable<long>,
		IEquatable<uint>,
		IEquatable<ulong>,
		IEquatable<float>,
		IEquatable<double>,
		IEquatable<decimal>,
		IEquatable<Half>,
		IEquatable<Int128>,
		IEquatable<UInt128>,
		IEquatable<string>,
#if NET9_0_OR_GREATER
		IEquatable<ReadOnlySpan<char>>,
#endif
		IEquatable<Slice>,
		IEquatable<Guid>,
		IEquatable<Uuid128>,
		IEquatable<IVarTuple>
	{
		private static readonly object s_false = true;

		private static readonly object s_true = true;

		private static readonly object[] s_smallIntCache = Enumerable.Range(0, 100).Select(i => (object) (long) i).ToArray();

		private static readonly object[] s_smallUIntCache = Enumerable.Range(0, 100).Select(i => (object) (ulong) i).ToArray();

		public readonly FqlItemType Type;

		public readonly object? Value;

		public readonly string? Name;

		public FqlTupleItem(FqlItemType type, object? value = null, string? name = null)
		{
			this.Type = type;
			this.Value = value;
			this.Name = name;
		}

		/// <inheritdoc />
		public bool IsPattern => this.Type is FqlItemType.Variable or FqlItemType.MaybeMore;

		/// <inheritdoc />
		public override bool Equals([NotNullWhen(true)] object? obj) => obj is FqlTupleItem other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => this.Type switch
		{
			FqlItemType.Variable => HashCode.Combine(FqlItemType.Tuple, (FqlVariableTypes) this.Value!),
			FqlItemType.MaybeMore => HashCode.Combine(FqlItemType.MaybeMore),
			FqlItemType.Nil => HashCode.Combine(FqlItemType.Nil),
			FqlItemType.Bool => HashCode.Combine(FqlItemType.Bytes, (bool) this.Value!),
			FqlItemType.Int => HashCode.Combine(FqlItemType.String, (long) this.Value!),
			FqlItemType.Num => HashCode.Combine(FqlItemType.String, (double) this.Value!),
			FqlItemType.String => HashCode.Combine(FqlItemType.String, (string) this.Value!),
			FqlItemType.Bytes => HashCode.Combine(FqlItemType.Bytes, (Slice) this.Value!),
			FqlItemType.Tuple => HashCode.Combine(FqlItemType.Tuple, (FqlTupleExpression) this.Value!),
			_ => HashCode.Combine(this.Type),
		};

		public bool Matches(object? value) => this.Type switch
		{
			FqlItemType.Variable => MatchType((FqlVariableTypes) this.Value!, value),
			FqlItemType.MaybeMore => ReferenceEquals(value, null),
			FqlItemType.Nil => ReferenceEquals(value, null),
			FqlItemType.Bool => value is bool b && Equals(b),
			FqlItemType.Int => value switch
			{
				int i => Equals(i),
				long l => Equals(l),
				uint ui => Equals(ui),
				ulong ul => Equals(ul),
				Int128 i => Equals(i),
				UInt128 ui => Equals(ui),
				_ => false,
			},
			FqlItemType.Num => value switch
			{
				float f => Equals(f),
				double d => Equals(d),
				Half h => Equals(h),
				decimal d => Equals(d),
				//TODO: decimal, Half
				_ => false,
			},
			FqlItemType.String => value is string str && Equals(str),
			FqlItemType.Bytes => value is Slice s && Equals(s),
			FqlItemType.Uuid => value switch
			{
				Uuid128 u => Equals(u),
				Guid g => Equals(g),
				_ => false,
			},
			_ => throw new NotImplementedException(this.Type.ToString())
		};

		public bool Equals(FqlTupleItem other) => other.Type == this.Type && object.Equals(this.Value, other.Value);

		public bool Equals(int value) => this.Type == FqlItemType.Int && this.Value switch
		{
			int i => i == value,
			uint ui => value >= 0 && ui == (uint) value,
			long l => l == value,
			ulong ul => value >= 0 && ul == (ulong) value,
			Int128 l => l == value,
			UInt128 ul => value >= 0 && ul == (UInt128) value,
			_ => false
		};

		public bool Equals(long value) => this.Type == FqlItemType.Int && this.Value switch
		{
			int i => i == value,
			uint ui => value >= 0 && ui == (ulong) value,
			long l => l == value,
			ulong ul => value >= 0 && ul == (ulong) value,
			Int128 l => l == value,
			UInt128 ul => value >= 0 && ul == (UInt128) value,
			_ => false
		};

		public bool Equals(uint value) => this.Type == FqlItemType.Int && this.Value switch
		{
			int i => i >= 0 && (uint) i == value,
			uint ui => ui == value,
			long l => l >= 0 && (ulong) l == value,
			ulong ul => ul == value,
			Int128 l => l == value,
			UInt128 ul => ul == value,
			_ => false
		};

		public bool Equals(ulong value) => this.Type == FqlItemType.Int && this.Value switch
		{
			int i => i >= 0 && (ulong) i == value,
			uint ui => ui == value,
			long l => l >= 0 && (ulong) l == value,
			ulong ul => ul == value,
			Int128 l => l == value,
			UInt128 ul => ul == value,
			_ => false
		};

		public bool Equals(Int128 value) => this.Type == FqlItemType.Int && this.Value switch
		{
			int i => i == value,
			uint ui => value >= 0 && ui == value,
			long l => l == value,
			ulong ul => value >= 0 && ul == value,
			Int128 l => l == value,
			UInt128 ul => value >= 0 && ul == (UInt128) value,
			_ => false
		};

		public bool Equals(UInt128 value) => this.Type == FqlItemType.Int && this.Value switch
		{
			int i => i >= 0 && (UInt128) i == value,
			uint ui => ui == value,
			long l => l >= 0 && (UInt128) l == value,
			ulong ul => ul == value,
			Int128 l => l >= 0 && (UInt128) l == value,
			UInt128 ul => ul == value,
			_ => false
		};

		public bool Equals(float value) => this.Type == FqlItemType.Num && this.Value switch
		{
			// ReSharper disable CompareOfFloatsByEqualityOperator
			float f => float.IsNaN(value) ? float.IsNaN(f) : f == value,
			double d => float.IsNaN(value) ? double.IsNaN(d) : d == value,
			decimal d => !float.IsNaN(value) && d == (decimal) value,
			Half h => float.IsNaN(value) ? Half.IsNaN(h) : (float) h == value,
			_ => false,
			// ReSharper restore CompareOfFloatsByEqualityOperator
		};

		public bool Equals(double value) => this.Type == FqlItemType.Num && this.Value switch
		{
			// ReSharper disable CompareOfFloatsByEqualityOperator
			float f => double.IsNaN(value) ? float.IsNaN(f) : f == value,
			double d => double.IsNaN(value) ? double.IsNaN(d) : d == value,
			decimal d => !double.IsNaN(value) && d == (decimal) value,
			Half h => double.IsNaN(value) ? Half.IsNaN(h) : (float) h == value,
			_ => false,
			// ReSharper restore CompareOfFloatsByEqualityOperator
		};

		public bool Equals(Half value) => this.Type == FqlItemType.Num && this.Value switch
		{
			// ReSharper disable CompareOfFloatsByEqualityOperator
			float f => Half.IsNaN(value) ? float.IsNaN(f) : f == (float) value,
			double d => Half.IsNaN(value) ? double.IsNaN(d) : d == (double) value,
			decimal d => !Half.IsNaN(value) && d == (decimal) value,
			Half h => Half.IsNaN(value) ? Half.IsNaN(h) : h == value,
			_ => false,
			// ReSharper restore CompareOfFloatsByEqualityOperator
		};

		public bool Equals(decimal value) => this.Type == FqlItemType.Num && this.Value switch
		{
			float f => !float.IsNaN(f) && (decimal) f == value,
			double d => !double.IsNaN(d) && (decimal) d == value,
			decimal d => d == value,
			Half h => !Half.IsNaN(h) && (decimal) h == value,
			_ => false,
		};

		public bool Equals(string value)
		{
			return this.Type == FqlItemType.String && ((string) this.Value!) == value;
		}

		public bool Equals(ReadOnlySpan<char> value)
		{
			return this.Type == FqlItemType.String && value.SequenceEqual((string) this.Value!);
		}

		public bool Equals(Guid value)
		{
			return this.Type == FqlItemType.Uuid && ((Uuid128) this.Value!) == (Uuid128) value;
		}

		public bool Equals(Uuid128 value)
		{
			return this.Type == FqlItemType.Uuid && ((Uuid128) this.Value!) == value;
		}

		public bool Equals(Slice value)
		{
			return this.Type == FqlItemType.Bytes && ((Slice) this.Value!).Equals(value);
		}

		public bool Equals(IVarTuple value)
		{
			return this.Type == FqlItemType.Tuple && ((FqlTupleExpression) this.Value!).Match(value);
		}

		public static bool MatchType(FqlVariableTypes types, object? value)
		{
			return value switch
			{
				null => types.HasFlag(FqlVariableTypes.Nil),
				bool => types.HasFlag(FqlVariableTypes.Bool),
				int => types.HasFlag(FqlVariableTypes.Int),
				uint => types.HasFlag(FqlVariableTypes.Int),
				long => types.HasFlag(FqlVariableTypes.Int),
				ulong => types.HasFlag(FqlVariableTypes.Int),
				Int128 => types.HasFlag(FqlVariableTypes.Int),
				UInt128 => types.HasFlag(FqlVariableTypes.Int),
				float => types.HasFlag(FqlVariableTypes.Num),
				double => types.HasFlag(FqlVariableTypes.Num),
				Half => types.HasFlag(FqlVariableTypes.Num),
				decimal => types.HasFlag(FqlVariableTypes.Num),
				string => types.HasFlag(FqlVariableTypes.String),
				Slice => types.HasFlag(FqlVariableTypes.Bytes),
				Guid => types.HasFlag(FqlVariableTypes.Uuid),
				Uuid128 => types.HasFlag(FqlVariableTypes.Uuid),
				IVarTuple => types.HasFlag(FqlVariableTypes.Tuple),
				_ => false,
			};
		}

		/// <inheritdoc />
		public override string ToString() => this.Type switch
		{
			FqlItemType.Variable => "<" + (this.Name != null ? (this.Name + ":") : "") + ToVariableTypeLiteral((FqlVariableTypes) this.Value!) + ">",
			FqlItemType.MaybeMore => "...",
			FqlItemType.Nil => "nil",
			FqlItemType.Bool => ((bool) this.Value!) ? "true" : "false",
			FqlItemType.Int => this.Value switch
			{
				int x        => x.ToString(null, CultureInfo.InvariantCulture),
				uint x       => x.ToString(null, CultureInfo.InvariantCulture),
				long x       => x.ToString(null, CultureInfo.InvariantCulture),
				ulong x      => x.ToString(null, CultureInfo.InvariantCulture),
				Int128 x     => x.ToString(null, CultureInfo.InvariantCulture),
				UInt128 x    => x.ToString(null, CultureInfo.InvariantCulture),
				BigInteger x => x.ToString(null, CultureInfo.InvariantCulture),
				_ => throw new InvalidOperationException("Invalid Int storage type"),
			},
			FqlItemType.Num => this.Value switch
			{
				Half x    => x.ToString("R", CultureInfo.InvariantCulture),
				float x   => x.ToString("R", CultureInfo.InvariantCulture),
				double x  => x.ToString("R", CultureInfo.InvariantCulture),
				decimal x => x.ToString("R", CultureInfo.InvariantCulture),
				_ => throw new InvalidOperationException("Invalid Int storage type"),
			},
			FqlItemType.String => $"\"{((string) this.Value!).Replace("\"", "\\\"")}\"",
			FqlItemType.Uuid => ((Uuid128) this.Value!).ToString("B"),
			FqlItemType.Bytes => "0x" + ((Slice) this.Value!).ToString("x"),
			FqlItemType.Tuple => ((FqlTupleExpression) this.Value!).ToString(),
			_ => $"<?{this.Type}?>",
		};

		public void Explain(ExplanationBuilder builder)
		{
			if (this.Type == FqlItemType.Tuple)
			{
				((FqlTupleExpression) this.Value!).Explain(builder);
				return;
			}

			builder.WriteLine($"{this.Type}: {this.ToString()}");
		}

		private static readonly Dictionary<FqlVariableTypes, string> s_typesLiteralCache = CreateInitialTypeLiteralCache();

		private static Dictionary<FqlVariableTypes, string> CreateInitialTypeLiteralCache() => new()
		{
			[FqlVariableTypes.Nil]    = "nil",
			[FqlVariableTypes.Bool]   = "bool",
			[FqlVariableTypes.Int]    = "int",
			[FqlVariableTypes.Num]    = "num",
			[FqlVariableTypes.String] = "str",
			[FqlVariableTypes.Uuid]   = "uuid",
			[FqlVariableTypes.Bytes]  = "bytes",
			[FqlVariableTypes.Tuple]  = "tup",
			// some of the usual suspects
		};

		public static FqlVariableTypes ParseVariableTypeLiteral(ReadOnlySpan<char> literal) => literal switch
		{
			"nil"   => FqlVariableTypes.Nil,
			"bool"  => FqlVariableTypes.Bool,
			"int"   => FqlVariableTypes.Int,
			"num"   => FqlVariableTypes.Num,
			"str"   => FqlVariableTypes.String,
			"uuid"  => FqlVariableTypes.Uuid,
			"bytes" => FqlVariableTypes.Bytes,
			"tup"   => FqlVariableTypes.Tuple,
			_       => FqlVariableTypes.None
		};

		public static string ToVariableTypeLiteral(FqlVariableType type) => ToVariableTypeLiteral((FqlVariableTypes) type);

		public static string ToVariableTypeLiteral(FqlVariableTypes types)
		{
			lock (s_typesLiteralCache)
			{
				if (s_typesLiteralCache.TryGetValue(types, out var s))
				{
					return s;
				}

				s = CreateTypeLiteral(types);
				s_typesLiteralCache[types] = s;
				return s;
			}

			static string CreateTypeLiteral(FqlVariableTypes types)
			{
				var sb = new StringBuilder();
				foreach (var x in Enum.GetValues<FqlVariableTypes>())
				{
					if ((types & x) != 0)
					{
						if (sb.Length != 0) sb.Append('|');
						sb.Append(s_typesLiteralCache[x]);
					}
				}

				return sb.ToString();
			}

		}

		public static FqlTupleItem Variable(FqlVariableTypes types, string? name = null) => new(FqlItemType.Variable, types, name);

		public static FqlTupleItem MaybeMore() => new(FqlItemType.MaybeMore, null);

		public static FqlTupleItem Nil() => new(FqlItemType.Nil, null);

		public static FqlTupleItem Boolean(bool value) => new(FqlItemType.Bool, value ? s_true : s_false);

		public static FqlTupleItem Int(int value) => new(FqlItemType.Int, (uint) value < s_smallIntCache.Length ? s_smallIntCache[value] : (long) value);

		public static FqlTupleItem Int(long value) => new(FqlItemType.Int, (value >= 0 && value < s_smallIntCache.Length) ? s_smallIntCache[value] : value);

		public static FqlTupleItem Int(uint value) => new(FqlItemType.Int, value < s_smallUIntCache.Length ? s_smallUIntCache[value] : (ulong) value);

		public static FqlTupleItem Int(ulong value) => new(FqlItemType.Int, value < (ulong) s_smallUIntCache.Length ? s_smallUIntCache[value] : value);

		public static FqlTupleItem Int(Int128 value) => new(FqlItemType.Int, value);

		public static FqlTupleItem Int(UInt128 value) => new(FqlItemType.Int, value);

		public static FqlTupleItem Int(BigInteger value) => new(FqlItemType.Int, value);

		public static FqlTupleItem Num(float value) => new(FqlItemType.Num, value);

		public static FqlTupleItem Num(double value) => new(FqlItemType.Num, value);

		public static FqlTupleItem Num(Half value) => new(FqlItemType.Num, value);

		public static FqlTupleItem Num(decimal value) => new(FqlItemType.Num, value);

		public static FqlTupleItem String(string value) => new(FqlItemType.String, value);

		public static FqlTupleItem Bytes(Slice value) => new(FqlItemType.Bytes, value);

		public static FqlTupleItem Uuid(Guid value) => new(FqlItemType.Uuid, (Uuid128) value);

		public static FqlTupleItem Uuid(Uuid128 value) => new(FqlItemType.Uuid, value);

		public static FqlTupleItem Tuple(FqlTupleExpression value) => new(FqlItemType.Tuple, value);

	}

	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class FqlTupleExpression : IEquatable<FqlTupleExpression>, IFqlExpression
	{
		public List<FqlTupleItem> Items { get; } = [];

		/// <inheritdoc />
		public bool IsPattern => this.Items.Any(x => x.IsPattern);

		public bool Match(IVarTuple? tuple)
		{
			if (tuple == null) return false;

			var items = CollectionsMarshal.AsSpan(this.Items);

			// if the last is MaybeMore, we don't need to check
			bool exactSize = true;
			while(items.Length > 0 && items[^1].Type == FqlItemType.MaybeMore)
			{
				exactSize = false;
				items = items[..^1];
			}

			// if the tuple is smaller, it will not match
			if (exactSize)
			{
				if (tuple.Count != items.Length) return false;
			}
			else
			{
				if (tuple.Count < items.Length) return false;
			}

			for (int i = 0; i < items.Length; i++)
			{
				if (!items[i].Matches(tuple[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <inheritdoc />
		public override bool Equals([NotNullWhen(true)] object? obj) => obj is FqlTupleExpression other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode()
		{
			var hc = new HashCode();
			foreach (var item in this.Items)
			{
				hc.Add(item);
			}
			return hc.ToHashCode();
		}

		public bool Equals(FqlTupleExpression? other)
		{
			if (other is null) return false;
			if (ReferenceEquals(other, this)) return true;

			var items = this.Items;
			var otherItems = other.Items;
			if (items.Count != otherItems.Count) return false;

			for (int i = 0; i < items.Count; i++)
			{
				if (!items[i].Equals(otherItems[i])) return false;
			}

			return true;
		}

		#region Builder Pattern...

		public static FqlTupleExpression Create() => new();

		public FqlTupleExpression Add(FqlTupleItem item)
		{
			this.Items.Add(item);
			return this;
		}

		public FqlTupleExpression AddMaybeMore() => Add(FqlTupleItem.MaybeMore());

		public FqlTupleExpression AddVariable(FqlVariableTypes types, string? name = null) => Add(FqlTupleItem.Variable(types, name));

		public FqlTupleExpression AddAnyVariable(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Any, name));

		public FqlTupleExpression AddBytesVariable(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Bytes, name));

		public FqlTupleExpression AddStringVariable(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.String, name));

		public FqlTupleExpression AddBooleanVariable(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Bool, name));

		public FqlTupleExpression AddNilVariable(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Nil, name));

		public FqlTupleExpression AddIntVariable(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Int, name));

		public FqlTupleExpression AddNumVariable(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Num, name));

		public FqlTupleExpression AddUuidVariable(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Uuid, name));

		public FqlTupleExpression AddTupleVariable(string? name = null) => Add(FqlTupleItem.Variable(FqlVariableTypes.Tuple, name));

		public FqlTupleExpression AddNilConst() => Add(FqlTupleItem.Nil());

		public FqlTupleExpression AddBooleanConst(bool value) => Add(FqlTupleItem.Boolean(value));

		public FqlTupleExpression AddIntConst(int value) => Add(FqlTupleItem.Int(value));

		public FqlTupleExpression AddIntConst(long value) => Add(FqlTupleItem.Int(value));

		public FqlTupleExpression AddUIntConst(uint value) => Add(FqlTupleItem.Int(value));

		public FqlTupleExpression AddUIntConst(ulong value) => Add(FqlTupleItem.Int(value));

		public FqlTupleExpression AddBigIntConst(Int128 value) => Add(FqlTupleItem.Int(value));

		public FqlTupleExpression AddBigIntConst(UInt128 value) => Add(FqlTupleItem.Int(value));

		public FqlTupleExpression AddFloatConst(float value) => Add(FqlTupleItem.Num(value));

		public FqlTupleExpression AddFloatConst(double value) => Add(FqlTupleItem.Num(value));

		public FqlTupleExpression AddStringConst(string value) => Add(FqlTupleItem.String(value));

		public FqlTupleExpression AddBytesConst(Slice value) => Add(FqlTupleItem.Bytes(value));

		public FqlTupleExpression AddUuidConst(Guid value) => Add(FqlTupleItem.Uuid(value));

		public FqlTupleExpression AddUuidConst(Uuid128 value) => Add(FqlTupleItem.Uuid(value));

		public FqlTupleExpression AddTupleConst(FqlTupleExpression value) => Add(FqlTupleItem.Tuple(value));

		#endregion

		/// <inheritdoc />
		public override string ToString()
		{
			switch (this.Items.Count)
			{
				case 0: return "()";
				case 1: return $"({this.Items[0]})";
				default:
				{
					var sb = new StringBuilder();
					sb.Append('(');
					foreach (var item in this.Items)
					{
						if (sb.Length > 1) sb.Append(", ");
						sb.Append(item.ToString());
					}
					sb.Append(')');
					return sb.ToString();
				}
			}
		}

		/// <inheritdoc />
		public void Explain(ExplanationBuilder builder)
		{
			if (!builder.Recursive)
			{
				builder.WriteLine($"Tuple: [{this.Items.Count}] {ToString()}");
				return;
			}

			builder.WriteLine($"Tuple: [{this.Items.Count}]");
			builder.ExplainChildren(this.Items);
		}

	}

}

#endif
