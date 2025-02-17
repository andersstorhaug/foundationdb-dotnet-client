#region Copyright (c) 2023-2024 SnowBank SAS, (c) 2005-2023 Doxense SAS
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

// ReSharper disable StringLiteralTypo
#pragma warning disable NUnit2009

namespace Doxense.Serialization.Json.Tests
{

	[TestFixture]
	[Category("Core-SDK")]
	[Category("Core-JSON")]
	[Parallelizable(ParallelScope.All)]
	public sealed class JsonPathFacts : SimpleTest
	{

		[Test]
		public void Test_JsonPath_Empty()
		{
			Assert.That(JsonPath.Empty.IsEmpty(), Is.True);
			Assert.That(JsonPath.Empty.ToString(), Is.EqualTo(""));
			Assert.That(JsonPath.Empty, Is.EqualTo(JsonPath.Empty));
			Assert.That(JsonPath.Empty, Is.EqualTo(""));
			Assert.That(JsonPath.Empty.Equals(default(string)), Is.True);

			Assert.That(JsonPath.Empty.IsParentOf(JsonPath.Empty), Is.False);
			Assert.That(JsonPath.Empty.IsChildOf(JsonPath.Empty), Is.False);

			Assert.That(JsonPath.Empty.TryGetLastKey(out var key), Is.False);
			Assert.That(key.Length, Is.Zero);
			Assert.That(JsonPath.Empty.GetLastKey().Length, Is.Zero);

			Assert.That(JsonPath.Empty.TryGetLastIndex(out var index), Is.False);
			Assert.That(index, Is.EqualTo(default(Index)));
			Assert.That(JsonPath.Empty.GetLastIndex(), Is.Null);

			Assert.That(JsonPath.ParseNext(default, out var keyLength, out var idx), Is.EqualTo(0));
			Assert.That(keyLength, Is.Zero);
			Assert.That(idx, Is.Default);
		}

		[Test]
		public void Test_JsonPath_Basics()
		{
			Assert.Multiple(() =>
			{
				var path = JsonPath.Create("foo");
				Assert.That(path.IsEmpty(), Is.False);
				Assert.That(path.ToString(), Is.EqualTo("foo"));
				Assert.That(path, Is.EqualTo(path));
				Assert.That(path, Is.Not.EqualTo(JsonPath.Empty));
				Assert.That(path.Equals("foo"), Is.True);

				Assert.That(JsonPath.ParseNext("foo", out var keyLength, out var idx), Is.EqualTo(3));
				Assert.That(keyLength, Is.EqualTo(3));
				Assert.That(idx, Is.Default);

				Assert.That(path.GetLastKey().ToString(), Is.EqualTo("foo"));
				Assert.That(path.GetLastIndex(), Is.Null);
				Assert.That(path.GetParent(), Is.EqualTo(JsonPath.Empty));
				Assert.That(path.GetSegmentCount(), Is.EqualTo(1));
				Assert.That(path.GetSegments(), Is.EqualTo((string[]) [ "foo", ]));
			});
			Assert.Multiple(() =>
			{
				var path = JsonPath.Create("[42]");
				Assert.That(path.IsEmpty(), Is.False);
				Assert.That(path.ToString(), Is.EqualTo("[42]"));
				Assert.That(path, Is.EqualTo(path));
				Assert.That(path, Is.Not.EqualTo(JsonPath.Empty));
				Assert.That(path.Equals("[42]"), Is.True);

				Assert.That(JsonPath.ParseNext("[42]", out var keyLength, out var idx), Is.EqualTo(4));
				Assert.That(keyLength, Is.EqualTo(0));
				Assert.That(idx, Is.EqualTo(new Index(42)));

				Assert.That(path.GetLastKey().ToString(), Is.EqualTo(""));
				Assert.That(path.GetLastIndex(), Is.EqualTo(new Index(42)));
				Assert.That(path.GetParent(), Is.EqualTo(JsonPath.Empty));
				Assert.That(path.GetSegmentCount(), Is.EqualTo(1));
				Assert.That(path.GetSegments(), Is.EqualTo((string[]) [ "[42]", ]));

			});
			Assert.Multiple(() =>
			{
				var path = JsonPath.Create("foo.bar");
				Assert.That(path.IsEmpty(), Is.False);
				Assert.That(path.ToString(), Is.EqualTo("foo.bar"));
				Assert.That(path, Is.EqualTo(path));
				Assert.That(path, Is.Not.EqualTo(JsonPath.Create("foo")));
				Assert.That(path, Is.Not.EqualTo(JsonPath.Empty));
				Assert.That(path.Equals("foo.bar"), Is.True);

				Assert.That(JsonPath.ParseNext("foo.bar", out var keyLength, out var idx), Is.EqualTo(4));
				Assert.That(keyLength, Is.EqualTo(3));
				Assert.That(idx, Is.Default);

				Assert.That(path.GetLastKey().ToString(), Is.EqualTo("bar"));
				Assert.That(path.GetLastIndex(), Is.Null);
				Assert.That(path.GetParent(), Is.EqualTo("foo"));
				Assert.That(path.GetSegmentCount(), Is.EqualTo(2));
				Assert.That(path.GetSegments(), Is.EqualTo((string[]) [ "foo", "bar" ]));
			});
			Assert.Multiple(() =>
			{
				var path = JsonPath.Create("foo[42]");
				Assert.That(path.IsEmpty(), Is.False);
				Assert.That(path.ToString(), Is.EqualTo("foo[42]"));
				Assert.That(path, Is.EqualTo(path));
				Assert.That(path, Is.EqualTo(JsonPath.Empty["foo"][42]));
				Assert.That(path, Is.Not.EqualTo(JsonPath.Create("[42]")));
				Assert.That(path, Is.Not.EqualTo(JsonPath.Create("foo")));
				Assert.That(path, Is.Not.EqualTo(JsonPath.Empty));
				Assert.That(path.Equals("foo[42]"), Is.True);

				Assert.That(JsonPath.ParseNext("foo[42]", out var keyLength, out var idx), Is.EqualTo(3));
				Assert.That(keyLength, Is.EqualTo(3));
				Assert.That(idx, Is.Default);

				Assert.That(path.GetLastKey().ToString(), Is.EqualTo(""));
				Assert.That(path.GetLastIndex(), Is.EqualTo((Index) 42));
				Assert.That(path.GetParent(), Is.EqualTo("foo"));
				Assert.That(path.GetSegmentCount(), Is.EqualTo(2));
				Assert.That(path.GetSegments(), Is.EqualTo((string[]) [ "foo", "[42]" ]));
			});
			Assert.Multiple(() =>
			{
				var path = JsonPath.Create("[42].foo");
				Assert.That(path.IsEmpty(), Is.False);
				Assert.That(path.ToString(), Is.EqualTo("[42].foo"));
				Assert.That(path, Is.EqualTo(path));
				Assert.That(path, Is.EqualTo(JsonPath.Empty[42]["foo"]));
				Assert.That(path, Is.Not.EqualTo(JsonPath.Create("foo")));
				Assert.That(path, Is.Not.EqualTo(JsonPath.Create("[42]")));
				Assert.That(path, Is.Not.EqualTo(JsonPath.Empty));
				Assert.That(path.Equals("[42].foo"), Is.True);

				Assert.That(JsonPath.ParseNext("[42].foo", out var keyLength, out var idx), Is.EqualTo(5));
				Assert.That(keyLength, Is.EqualTo(0));
				Assert.That(idx, Is.EqualTo(new Index(42)));

				Assert.That(path.GetLastKey().ToString(), Is.EqualTo("foo"));
				Assert.That(path.GetLastIndex(), Is.Default);
				Assert.That(path.GetParent(), Is.EqualTo("[42]"));
				Assert.That(path.GetSegmentCount(), Is.EqualTo(2));
				Assert.That(path.GetSegments(), Is.EqualTo((string[]) [ "[42]", "foo" ]));
			});
			Assert.Multiple(() =>
			{
				var path = JsonPath.Create("foo.bar.baz");
				Assert.That(path.IsEmpty(), Is.False);
				Assert.That(path.ToString(), Is.EqualTo("foo.bar.baz"));
				Assert.That(path, Is.EqualTo(path));
				Assert.That(path, Is.EqualTo(JsonPath.Empty["foo"]["bar"]["baz"]));
				Assert.That(path, Is.Not.EqualTo(JsonPath.Empty));
				Assert.That(path.Equals("foo.bar.baz"), Is.True);

				Assert.That(JsonPath.ParseNext("foo.bar.bar", out var keyLength, out var idx), Is.EqualTo(4));
				Assert.That(keyLength, Is.EqualTo(3));
				Assert.That(idx, Is.Default);

				Assert.That(path.GetLastKey().ToString(), Is.EqualTo("baz"));
				Assert.That(path.GetLastIndex(), Is.Null);
				Assert.That(path.GetParent(), Is.EqualTo("foo.bar"));
				Assert.That(path.GetSegmentCount(), Is.EqualTo(3));
				Assert.That(path.GetSegments(), Is.EqualTo((string[]) [ "foo", "bar", "baz" ]));
			});
			Assert.Multiple(() =>
			{
				var path = JsonPath.Create("foo[42].bar");
				Assert.That(path.IsEmpty(), Is.False);
				Assert.That(path.ToString(), Is.EqualTo("foo[42].bar"));
				Assert.That(path, Is.EqualTo(path));
				Assert.That(path, Is.EqualTo(JsonPath.Empty["foo"][42]["bar"]));
				Assert.That(path, Is.Not.EqualTo(JsonPath.Empty));
				Assert.That(path.Equals("foo[42].bar"), Is.True);

				Assert.That(JsonPath.ParseNext("foo[42].bar", out var keyLength, out var idx), Is.EqualTo(3));
				Assert.That(keyLength, Is.EqualTo(3));
				Assert.That(idx, Is.Default);

				Assert.That(path.GetLastKey().ToString(), Is.EqualTo("bar"));
				Assert.That(path.GetLastIndex(), Is.Null);
				Assert.That(path.GetParent(), Is.EqualTo("foo[42]"));
				Assert.That(path.GetSegmentCount(), Is.EqualTo(3));
				Assert.That(path.GetSegments(), Is.EqualTo((string[]) [ "foo", "[42]", "bar" ]));
			});
			Assert.Multiple(() =>
			{ // spaces are not special...
				var path = JsonPath.Create("foo bar.baz");
				Assert.That(path.IsEmpty(), Is.False);
				Assert.That(path.ToString(), Is.EqualTo("foo bar.baz"));
				Assert.That(path, Is.EqualTo(path));
				Assert.That(path, Is.EqualTo(JsonPath.Empty["foo bar"]["baz"]));
				Assert.That(path.Equals("foo bar.baz"), Is.True);

				Assert.That(JsonPath.ParseNext("foo bar.baz", out var keyLength, out var idx), Is.EqualTo(8));
				Assert.That(keyLength, Is.EqualTo(7));
				Assert.That(idx, Is.Default);

				Assert.That(path.GetLastKey().ToString(), Is.EqualTo("baz"));
				Assert.That(path.GetLastIndex(), Is.Null);
				Assert.That(path.GetParent(), Is.EqualTo("foo bar"));
				Assert.That(path.GetParent().GetLastKey().ToString(), Is.EqualTo("foo bar"));
				Assert.That(path.GetParent().GetLastIndex(), Is.Null);
				Assert.That(path.GetParent().GetParent(), Is.EqualTo(JsonPath.Empty));
				Assert.That(path.GetSegmentCount(), Is.EqualTo(2));
			});
			Assert.Multiple(() =>
			{ // escaping
				Assert.That(JsonPath.Create(@"foo\.bar.baz").GetSegments(), Is.EqualTo((string[]) [ "foo.bar", "baz" ]));
				Assert.That(JsonPath.Create(@"hosts.192\.168\.1\.23.name").GetSegments(), Is.EqualTo((string[]) [ "hosts", "192.168.1.23", "name" ]));
				Assert.That(JsonPath.Create(@"users.DOMACME\\j\.smith.groups").GetSegments(), Is.EqualTo((string[]) [ "users", @"DOMACME\j.smith", "groups" ]));
				Assert.That(JsonPath.Create(@"domains.\[::1\].allowed").GetSegments(), Is.EqualTo((string[]) [ "domains", "[::1]", "allowed" ]));
				Assert.That(JsonPath.Create(@"foos.foo\.0.bars.bar\.1\.2.baz").GetSegments(), Is.EqualTo((string[]) [ "foos", "foo.0", "bars", "bar.1.2", "baz" ]));
				Assert.That(JsonPath.Create(@"foos.foo\[0\].bars.bar\[1\]\[2\].baz").GetSegments(), Is.EqualTo((string[]) [ "foos", "foo[0]", "bars", "bar[1][2]", "baz" ]));
			});

		}

		[Test]
		public void Test_JsonPath_Concat()
		{
			Assert.That(JsonPath.Empty["foo"], Is.EqualTo(JsonPath.Create("foo")));
			Assert.That(JsonPath.Empty["foo"]["bar"], Is.EqualTo(JsonPath.Create("foo.bar")));
			Assert.That(JsonPath.Empty["foo"]["bar"]["baz"], Is.EqualTo(JsonPath.Create("foo.bar.baz")));
			Assert.That(JsonPath.Empty["foo"][1], Is.EqualTo(JsonPath.Create("foo[1]")));
			Assert.That(JsonPath.Empty["foo"][42], Is.EqualTo(JsonPath.Create("foo[42]")));
			Assert.That(JsonPath.Empty["foo"][1234], Is.EqualTo(JsonPath.Create("foo[1234]")));
			Assert.That(JsonPath.Empty["foo"][42]["bar"], Is.EqualTo(JsonPath.Create("foo[42].bar")));
			Assert.That(JsonPath.Empty["foo"][42][^1], Is.EqualTo(JsonPath.Create("foo[42][^1]")));
			Assert.That(JsonPath.Empty[42], Is.EqualTo(JsonPath.Create("[42]")));
			Assert.That(JsonPath.Empty[42]["foo"], Is.EqualTo(JsonPath.Create("[42].foo")));
			Assert.That(JsonPath.Empty[42][^1], Is.EqualTo(JsonPath.Create("[42][^1]")));
			Assert.That(JsonPath.Empty["thisisaverylargestringthatwilltakemorethansixtyfourcharacterstoencodewithnoinvalidchars"], Is.EqualTo(JsonPath.Create("thisisaverylargestringthatwilltakemorethansixtyfourcharacterstoencodewithnoinvalidchars")));
			Assert.That(JsonPath.Empty["foo"]["thisisaverylargestringthatwilltakemorethansixtyfourcharacterstoencodewithnoinvalidchars"], Is.EqualTo(JsonPath.Create("foo.thisisaverylargestringthatwilltakemorethansixtyfourcharacterstoencodewithnoinvalidchars")));

			// test that we can index an object with weird keys like "foo bar" or "foo.bar" or "foo\bar" are escaped properly
			// We use a similar encoding as json string where '\' is encoded as '\\', '.' as '\.', '[' as '\[' and ']' as '\]'

			Assert.That(JsonPath.Empty["foo"]["bar.baz"].ToString(), Is.EqualTo(@"foo.bar\.baz"));
			Assert.That(JsonPath.Empty["foo"]["bar[baz"].ToString(), Is.EqualTo(@"foo.bar\[baz"));
			Assert.That(JsonPath.Empty["foo"]["bar]baz"].ToString(), Is.EqualTo(@"foo.bar\]baz"));
			Assert.That(JsonPath.Empty["foo"]["bar\\baz"].ToString(), Is.EqualTo(@"foo.bar\\baz"));
			Assert.That(JsonPath.Empty["foo"]["192.168.1.23"].ToString(), Is.EqualTo(@"foo.192\.168\.1\.23"));
			Assert.That(JsonPath.Empty["foo"]["[42]"].ToString(), Is.EqualTo(@"foo.\[42\]"));

			Assert.That(JsonPath.Empty["foo"]["bar baz"].ToString(), Is.EqualTo("foo.bar baz")); // space should NOT be escaped
			Assert.That(JsonPath.Empty["foo"]["bar/baz"].ToString(), Is.EqualTo("foo.bar/baz")); // '/' should NOT be escaped

			Assert.That(JsonPath.Empty["""this.is.a[very.large]string.that.will\take.more.than.sixty.four.characters.to.encode.with.invalid.chars"""], Is.EqualTo(JsonPath.Create("""this\.is\.a\[very\.large\]string\.that\.will\\take\.more\.than\.sixty\.four\.characters\.to\.encode\.with\.invalid\.chars""")));
			Assert.That(JsonPath.Empty["foo"]["""this.is.a[very.large]string.that.will\take.more.than.sixty.four.characters.to.encode.with.invalid.chars"""], Is.EqualTo(JsonPath.Create("""foo.this\.is\.a\[very\.large\]string\.that\.will\\take\.more\.than\.sixty\.four\.characters\.to\.encode\.with\.invalid\.chars""")));
		}

		[Test]
		public void Test_Json_GetKey()
		{
			static void Verify(string path, string expected)
			{
				var p = JsonPath.Create(path);
				Log($"# '{p}'.GetIndex() => {(expected.Length > 0 ? expected :  "<null>")}");
				var actual = p.GetLastKey();
				Assert.That(actual.ToString(), Is.EqualTo(expected), $"Path '{p}' should end with key '{expected}'");

				if (expected == "")
				{
					Assert.That(p.TryGetLastKey(out actual), Is.False, $"Path '{p}' should not end with a key");
					Assert.That(actual.Length, Is.Zero);
				}
				else
				{
					Assert.That(p.TryGetLastKey(out actual), Is.True, $"Path '{p}' should not end with a key");
					Assert.That(actual.ToString(), Is.EqualTo(expected), $"Path '{p}' should end with key '{expected}'");
				}
			}

			Verify("foo", "foo");
			Verify("foo.bar", "bar");
			Verify("foo\\.bar", "foo.bar");
			Verify("foo.bar.baz", "baz");
			Verify("foo[42].bar", "bar");
			Verify("[42].bar", "bar");
			Verify("foo.bar\\.baz", "bar.baz");
			Verify("foo.\\[42\\]", "[42]");

			Verify("", "");
			Verify("[42]", "");
			Verify("foo[42]", "");
			Verify("foo.bar[42]", "");
			Verify("foo\\.bar[42]", "");
		}

		[Test]
		public void Test_Json_Index()
		{
			static void Verify(string path, Index? expected)
			{
				var p = JsonPath.Create(path);
				Log($"# '{p}'.GetIndex() => {(expected?.ToString() ?? "<null>")}");
				var actual = p.GetLastIndex();
				Assert.That(actual, Is.EqualTo(expected), $"Path '{p}' should end with index '{expected}'");

				if (expected == null)
				{
					Assert.That(p.TryGetLastIndex(out var index), Is.False, $"Path '{p}' should not end with an index");
					Assert.That(index, Is.Default);
				}
				else
				{
					Assert.That(p.TryGetLastIndex(out var index), Is.True, $"Path '{p}' should not end with an index");
					Assert.That(index, Is.EqualTo(expected.Value), $"Path '{p}' should end with index '{expected}'");
				}

			}

			Verify("[42]", 42);
			Verify("foo[42]", 42);
			Verify("foo.bar[42]", 42);
			Verify("[^1]", ^1);
			Verify("foo[^1]", ^1);
			Verify("foo.bar[^1]", ^1);
			
			Verify("", null);
			Verify("foo", null);
			Verify("foo.bar", null);
			Verify("foo.bar.baz", null);
			Verify("foo[42].bar", null);
			Verify("[42].bar", null);
			Verify("foo\\[123\\]", null);

		}

		[Test]
		public void Test_JsonPath_GetParent()
		{
			static void Verify(string path, string parent)
			{
				Log($"# '{path}'.GetParent() => '{parent}'");
				Assert.That(JsonPath.Create(path).GetParent().ToString(), Is.EqualTo(parent), $"Parent of '{path}' should be '{parent}'");
			}

			Verify("", "");
			Verify("foo", "");
			Verify("[42]", "");
			Verify("foo.bar", "foo");
			Verify("foo[42]", "foo");
			Verify("[42].foo", "[42]");
			Verify("[42][^1]", "[42]");
			Verify("foo.bar.baz", "foo.bar");
			Verify("foo.bar[42]", "foo.bar");
			Verify("foo[42].baz", "foo[42]");
			Verify("[42].bar.baz", "[42].bar");
			Verify("foo.bar\\.baz", "foo");
			Verify("foo\\.bar.baz", "foo\\.bar");
			Verify("foo.bar\\[42\\]", "foo");
		}

		[Test]
		public void Test_JsonPath_IsParentOf()
		{

			static void ShouldBeParent(string parent, string child)
			{
				Log($"# '{parent}'.IsParentOf('{child}') => true");
				Assert.That(JsonPath.Create(parent).IsParentOf(JsonPath.Create(child)), Is.True, $"Path '{parent}' should be a parent of '{child}'");
				Assert.That(JsonPath.Create(parent).IsParentOf(child.AsSpan()), Is.True, $"Path '{parent}' should be a parent of '{child}'");
			}

			static void ShouldNotBeParent(string parent, string child)
			{
				Log($"# '{parent}'.IsParentOf('{child}') => false");
				Assert.That(JsonPath.Create(parent).IsParentOf(JsonPath.Create(child)), Is.False, $"Path '{parent}' should NOT be a parent of '{child}'");
				Assert.That(JsonPath.Create(parent).IsParentOf(child.AsSpan()), Is.False, $"Path '{parent}' should NOT be a parent of '{child}'");
			}

			// PARENT
			ShouldBeParent("", "foo");
			ShouldBeParent("", "[42]");
			ShouldBeParent("foo", "foo.bar");
			ShouldBeParent("foo", "foo[42]");
			ShouldBeParent("foo[42]", "foo[42].bar");
			ShouldBeParent("foo[42]", "foo[42][^1]");
			ShouldBeParent("a.b.c.d.e.f", "a.b.c.d.e.f.g.h.i");
			ShouldBeParent("foo", "foo.bar\\.baz");
			ShouldBeParent("foo\\.bar", "foo\\.bar.baz");

			// NOT PARENT
			ShouldNotBeParent("foo", "bar");
			ShouldNotBeParent("foo.bar", "foo");
			ShouldNotBeParent("foo[42]", "foo");
			ShouldNotBeParent("foos", "foo.bar");
			ShouldNotBeParent("foos", "foo[42]");
			ShouldNotBeParent("foo", "foos.bar");
			ShouldNotBeParent("foo", "foos[42]");
			ShouldNotBeParent("a.a.a.a.a.a", "a.a.a.A.a.a.a");
			ShouldNotBeParent("foo", "foo\\.bar.baz");
		}

		[Test]
		public void Test_JsonPath_IsChildOf()
		{

			static void ShouldBeChild(string child, string parent, string relative)
			{
				var childPath = JsonPath.Create(child);
				var parentPath = JsonPath.Create(parent);

				Log($"# '{child}'.IsChildOf('{parent}') => (true, '{relative}')");
				Assert.That(childPath.IsChildOf(parentPath), Is.True, $"Path '{child}' should be a child of '{parent}'");
				Assert.That(childPath.IsChildOf(parent.AsSpan()), Is.True, $"Path '{child}' should be a child of '{parent}'");

				Assert.That(childPath.IsChildOf(parentPath, out var rp), Is.True, $"Path '{child}' should be a child of '{parent}'");
				Assert.That(rp.ToString(), Is.EqualTo(relative), $"Relative path from '{child}' to '{parent}' should be '{relative}'");

				Assert.That(childPath.IsChildOf(parent.AsSpan(), out rp), Is.True, $"Path '{child}' should be a child of '{parent}'");
				Assert.That(rp.ToString(), Is.EqualTo(relative), $"Relative path from '{child}' to '{parent}' should be '{relative}'");
			}

			static void ShouldNotBeChild(string child, string parent)
			{
				var childPath = JsonPath.Create(child);
				var parentPath = JsonPath.Create(parent);

				Log($"# '{child}'.IsChildOf('{parent}') => false");
				Assert.That(childPath.IsChildOf(parentPath), Is.False, $"Path '{child}' should NOT be a child of '{parent}'");
				Assert.That(childPath.IsChildOf(parent.AsSpan()), Is.False, $"Path '{child}' should NOT be a child of '{parent}'");
				Assert.That(childPath.IsChildOf(parentPath, out _), Is.False, $"Path '{child}' should NOT be a child of '{parent}'");
			}

			// PARENT
			ShouldBeChild("foo", "", "foo");
			ShouldBeChild("[42]", "", "[42]");
			ShouldBeChild("foo.bar", "foo", "bar");
			ShouldBeChild("foo[42]", "foo", "[42]");
			ShouldBeChild("foo[42].bar", "foo[42]", "bar");
			ShouldBeChild("foo[42][^1]", "foo[42]", "[^1]");
			ShouldBeChild("a.b.c.d.e.f.g.h.i", "a.b.c.d.e.f", "g.h.i");
			ShouldBeChild("foo.bar\\.baz", "foo", "bar\\.baz");
			ShouldBeChild("foo\\.bar.baz", "foo\\.bar", "baz");

			// NOT PARENT
			ShouldNotBeChild("bar", "foo");
			ShouldNotBeChild("foo", "foo.bar");
			ShouldNotBeChild("foo", "foo[42]");
			ShouldNotBeChild("foo.bar", "foos");
			ShouldNotBeChild("foo[42]", "foos");
			ShouldNotBeChild("foos.bar", "foo");
			ShouldNotBeChild("foos[42]", "foo");
			ShouldNotBeChild("a.a.a.A.a.a.a", "a.a.a.a.a.a");
			ShouldNotBeChild("foo\\.bar.baz", "foo");
		}

#if NET8_0_OR_GREATER

		[Test]
		public void Test_JsonPath_GetCommonAncestor()
		{
			static void Verify(string a, string b, string common, string left, string right)
			{
				Log($"# '{a}' / '{b}'");
				var c = JsonPath.Create(a).GetCommonAncestor(JsonPath.Create(b), out var l, out var r);
				Log($"- '{c}' > '{l}'");
				Log($"- '{c}' > '{r}'");
				Assert.That(c.ToString(), Is.EqualTo(common), $"Common path must be '{common}'");
				Assert.That(l.ToString(), Is.EqualTo(left), $"Left path must be '{left}'");
				Assert.That(r.ToString(), Is.EqualTo(right), $"Right path must be '{right}'");
			}

			{
				// ""
				// ""
				Verify("", "", "", "", "");
			}
			{
				// ""
				// foo
				Verify("", "foo", "", "", "foo");
			}
			{
				// foo
				// ""
				Verify("foo", "", "", "foo", "");
			}
			{
				// hello
				// world
				Verify("hello", "world", "", "hello", "world");
			}
			{
				// hello.world
				// foo.bar
				Verify("hello.world", "foo.bar", "", "hello.world", "foo.bar");
			}
			{
				// foo
				// [42]
				Verify("foo", "[42]", "", "foo", "[42]");
			}
			{
				// foo.bar
				// foo.jazz
				Verify("foo.bar", "foo.jazz", "foo", "bar", "jazz");
			}
			{ 
				// foo[1]
				// foo[42]
				Verify("foo[1]", "foo[42]", "foo", "[1]", "[42]");
			}
			{ 
				// foo.bar
				// foo[42]
				Verify("foo.bar", "foo[42]", "foo", "bar", "[42]");
			}
			{ 
				// foo.bar
				// foo.baz
				Verify("foo.bar", "foo.baz", "foo", "bar", "baz");
			}
			{ 
				// foo[41]
				// foo[42]
				Verify("foo[41]", "foo[42]", "foo", "[41]", "[42]");
			}

		}

#endif

		[Test]
		public void Test_JsonPath_Tokenizer()
		{

			[MustDisposeResource]
			static JsonPath.Tokenizer Tokenize(string path)
			{
				Log($"Tokenizing '{path}'...");
				return JsonPath.Create(path).GetEnumerator();
			}

			static void Step(ref JsonPath.Tokenizer it, string? key, Index? index, string parent, bool last = false)
			{
				Assert.That(it.MoveNext(), Is.True, $"Expected Next() to be {key}/{index}/{parent}");
				var current = it.Current;
				Log($"- Current: key={(current.Key.IsEmpty ? "<null>" : $"'{current.Key.ToString()}'")} / idx={current.Index.ToString()} / path='{current.Parent}'");
				if (key != null)
				{
					Assert.That(current.Key.ToString(), Is.EqualTo(key), $"Expected next token to be key '{key}' with parent '{parent}'");
				}
				else
				{
					Assert.That(current.Key.Length, Is.Zero, $"Expected next token to be index '{index}' with parent '{parent}'");
				}

				if (index != null)
				{
					Assert.That(current.Index, Is.EqualTo(index.Value), $"Expected next token to be index '{index}' with parent '{parent}'");
				}
				else
				{
					Assert.That(current.Index, Is.Default, $"Expected next token to be key '{key}' with parent '{parent}'");
				}
				Assert.That(current.Parent.ToString(), Is.EqualTo(parent));
				Assert.That(current.Last, Is.EqualTo(last), last ? "Should be the last segment" : "Should not be the last segment");
			}

			static void End(ref JsonPath.Tokenizer it)
			{
				Assert.That(it.MoveNext(), Is.False);
				Log("- End");
				it.Dispose();
			}

			{
				var it = Tokenize("");
				End(ref it);
			}

			{
				var it = Tokenize("foo");
				Step(ref it, "foo", null, "", last: true);
				End(ref it);
			}

			{
				var it = Tokenize("[42]");
				Step(ref it, null, 42, "", last: true);
				End(ref it);
			}

			{
				var it = Tokenize("foo.bar");
				Step(ref it, "foo", null, "");
				Step(ref it, "bar", null, "foo", last: true);
				End(ref it);
			}

			{
				var it = Tokenize("foo[42]");
				Step(ref it, "foo", null, "");
				Step(ref it, null, 42, "foo", last: true);
				End(ref it);
			}

			{
				var it = Tokenize("[42].foo");
				Step(ref it, null, 42, "");
				Step(ref it, "foo", null, "[42]", last: true);
				End(ref it);
			}

			{
				var it = Tokenize("[42][^1]");
				Step(ref it, null, 42, "");
				Step(ref it, null, ^1, "[42]", last: true);
				End(ref it);
			}

			{
				var it = Tokenize("foo[42].bar");
				Step(ref it, "foo", null, "");
				Step(ref it, null, 42, "foo");
				Step(ref it, "bar", null, "foo[42]", last: true);
				End(ref it);
			}

			{
				var it = Tokenize(@"images.x\.y\.z.id");
				Step(ref it, "images", null, "");
				Step(ref it, "x.y.z", null, "images");
				Step(ref it, "id", null, @"images.x\.y\.z", last: true);
				End(ref it);
			}
			{
				var it = Tokenize(@"images.\[42\].id");
				Step(ref it, "images", null, "");
				Step(ref it, "[42]", null, "images");
				Step(ref it, "id", null, @"images.\[42\]", last: true);
				End(ref it);
			}

			{
				var it = Tokenize("foo.bar[42].baz[^1][2].jazz[123][456]");
				Step(ref it, "foo", null, "");
				Step(ref it, "bar", null, "foo");
				Step(ref it, null, 42, "foo.bar");
				Step(ref it, "baz", null, "foo.bar[42]");
				Step(ref it, null, ^1, "foo.bar[42].baz");
				Step(ref it, null, 2, "foo.bar[42].baz[^1]");
				Step(ref it, "jazz", null, "foo.bar[42].baz[^1][2]");
				Step(ref it, null, 123, "foo.bar[42].baz[^1][2].jazz");
				Step(ref it, null, 456, "foo.bar[42].baz[^1][2].jazz[123]", last: true);
				End(ref it);
			}

			{
				
				var it = Tokenize(@"foos.foo\.0.bars.bar\.1\.2.baz");
				Step(ref it, "foos", null, "");
				Step(ref it, "foo.0", null, "foos");
				Step(ref it, "bars", null, @"foos.foo\.0");
				Step(ref it, "bar.1.2", null, @"foos.foo\.0.bars");
				Step(ref it, "baz", null, @"foos.foo\.0.bars.bar\.1\.2", last: true);
				End(ref it);
			}
		}
	}

}
