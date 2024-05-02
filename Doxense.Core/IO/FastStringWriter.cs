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

namespace Doxense.IO
{
	using System.Globalization;
	using System.IO;
	using System.Runtime;
	using System.Text;
	using Doxense.Serialization;

	/// <summary>"Fast" version of StringWriter, that performs less checks, but is a good fit for specific use cases (serialization, ...)</summary>
	/// <remarks>This type is "unsafe" and should only be used internally, and not exposed to the caller.</remarks>
	public sealed class FastStringWriter : TextWriter
	{
		// "bare metal" version of StringWriter that:
		// * Disable all checks if the stream is still open/closed (ie: can still write Dispose has been called!)
		// * Optimize the hot paths that are frequently called by a serializer (strings, numbers, ...) that already checks its own inputs
		// * Rely on the inner StringBuilder to do param checks
		// * All operations are InvariantCulture by default
		// * Attempt to better inline code

		private readonly StringBuilder m_buffer;

		private static readonly UnicodeEncoding s_encoding = new(bigEndian: false, byteOrderMark: false);

		#region Constructors...

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public FastStringWriter()
			: this(new StringBuilder())
		{ }

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public FastStringWriter(int capacity)
			: this(new StringBuilder(capacity))
		{ }

		public FastStringWriter(StringBuilder buffer)
			 : base(CultureInfo.InvariantCulture)
		{
			Contract.NotNull(buffer);

			m_buffer = buffer;
		}

		#endregion

		/// <summary>Return the underlying buffer used by this writer</summary>
		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public StringBuilder GetStringBuilder()
		{
			return m_buffer;
		}

		/// <summary>Return the current buffer as a string</summary>
		/// <returns>Text that has been written so far</returns>
		/// <remarks>Caution: please don't call this method if more text will be written later, because it will cause a lot of extra memory allocations!</remarks>
		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public override string ToString()
		{
			return m_buffer.ToString();
		}

		/// <summary>Return the current buffer as an array of char</summary>
		/// <returns>Text that has been written so far</returns>
		/// <remarks>Caution: please don't call this method if more text will be written later, because it will cause a lot of extra memory allocations!</remarks>
		public char[] ToCharArray()
		{
			char[] data = new char[m_buffer.Length];
			m_buffer.CopyTo(0, data, 0, m_buffer.Length);
			return data;
		}

		/// <summary>Return the content of the buffer, as bytes, using the specified encoding</summary>
		/// <param name="encoding">Encoding used to convert text into bytes (ex: Encoding.UTF8)</param>
		/// <returns>Content of the buffer, encoded into bytes</returns>
		public byte[] GetBytes(Encoding encoding)
		{
			return encoding.GetBytes(m_buffer.ToString());
		}

		/// <summary>Copy the content of the buffer to another text writer</summary>
		/// <param name="output">Writer where to write the content of the buffer</param>
		/// <param name="buffer">Optional buffer used for the copy (if not <c>null</c>)</param>
		/// <remarks>Perform an "optimized" copy by preventing the string allocation from the inner StringBuilder</remarks>
		public void CopyTo(TextWriter output, char[]? buffer = null)
		{
			Contract.NotNull(output);

			if (m_buffer.Length == 0) return;

			buffer ??= new char[Math.Min(m_buffer.Length, 0x400)];
			if (buffer.Length == 0) throw new ArgumentException("Buffer cannot be empty", nameof(buffer));

			int remaining = m_buffer.Length;
			int p = 0;
			while (remaining > 0)
			{
				int n = Math.Min(remaining, buffer.Length);
				m_buffer.CopyTo(p, buffer, 0, remaining);
				output.Write(buffer, 0, n);
				p += n;
				remaining -= n;
			}
		}

		#region TextWriter Implementation...

		public override Encoding Encoding
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get => s_encoding;
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public override void Close()
		{
			this.Dispose(true);
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public override void Write(char value)
		{
			m_buffer.Append(value);
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public override void Write(string? value)
		{
			m_buffer.Append(value);
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public override void Write(char[]? value)
		{
			m_buffer.Append(value);
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public override void Write(char[] buffer, int index, int count)
		{
			m_buffer.Append(buffer, index, count);
		}

		public override void Write(int value)
		{
			m_buffer.Append(StringConverters.ToString(value));
		}

		public override void Write(long value)
		{
			m_buffer.Append(StringConverters.ToString(value));
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public override void WriteLine(string? value)
		{
			m_buffer.AppendLine(value);
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public override void WriteLine()
		{
			m_buffer.AppendLine();
		}

		#endregion

		#region Async Implementation...

		public override Task FlushAsync()
		{
			return Task.CompletedTask;
		}

		public override Task WriteAsync(string? value)
		{
			m_buffer.Append(value);
			return Task.CompletedTask;
		}

		public override Task WriteAsync(char value)
		{
			m_buffer.Append(value);
			return Task.CompletedTask;
		}

		public override Task WriteAsync(char[] value, int index, int count)
		{
			m_buffer.Append(value, index, count);
			return Task.CompletedTask;
		}

		#endregion

	}

}
