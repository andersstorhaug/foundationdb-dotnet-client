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

namespace Doxense
{
	using System.Runtime.CompilerServices;
	using NUnit.Framework.Constraints;

	public static class TestConstraintsExtensions
	{

		/// <summary>Permet de capturer l'exception interceptée et de l'exposer a la méthode de test</summary>
		/// <param name="constraint">Containte de type <c>Throws.InstanceOf</c></param>
		/// <param name="capture">Boite qui recevra l'exception capturée, après l'execution de l'assertion</param>
		public static Constraint Catch(this InstanceOfTypeConstraint constraint, out Pokeball<Exception> capture)
		{
			capture = new Pokeball<Exception>();
			var x = new CatchExceptionConstraint<Exception>(capture);
			constraint.Builder?.Append(x);
			return x;
		}

		/// <summary>Permet de capturer l'exception interceptée et de l'exposer a la méthode de test</summary>
		/// <param name="constraint">Containte de type <c>Throws.InstanceOf</c></param>
		/// <param name="capture">Boite qui recevra l'exception capturée, après l'execution de l'assertion</param>
		public static Constraint Catch<TException>(this InstanceOfTypeConstraint constraint, out Pokeball<TException> capture)
			where TException : Exception
		{
			capture = new Pokeball<TException>();
			var x = new CatchExceptionConstraint<TException>(capture);
			constraint.Builder?.Append(x);
			return x;
		}

		/// <summary>Permet de capturer l'exception interceptée et de l'exposer a la méthode de test</summary>
		/// <param name="expr">Containte de type <c>Throws.Exception</c></param>
		/// <param name="capture">Boite qui recevra l'exception capturée, après l'execution de l'assertion</param>
		public static ResolvableConstraintExpression Catch(this ResolvableConstraintExpression expr, out Pokeball<Exception> capture)
		{
			capture = new Pokeball<Exception>();
			expr.Append(new CatchExceptionConstraint<Exception>(capture));
			return expr;
		}

		/// <summary>Permet de capturer l'exception interceptée et de l'exposer a la méthode de test</summary>
		/// <param name="expr">Containte de type <c>Throws.Exception</c></param>
		/// <param name="capture">Boite qui recevra l'exception capturée, après l'execution de l'assertion</param>
		public static ResolvableConstraintExpression Catch<TException>(this ResolvableConstraintExpression expr, out Pokeball<TException> capture)
			where TException : Exception
		{
			capture = new Pokeball<TException>();
			expr.Append(new CatchExceptionConstraint<TException>(capture));
			return expr;
		}

		/// <summary>Continue an assertion using the result of a capture <see langword="out"/> argument of a method call, instead of the original actual value</summary>
		/// <remarks>This is equivalent to a logical <c>AND</c> but with both sides testing a different value.</remarks>
		/// <example>
		/// Assert.That(dict.TryGetValue("hello", out var value), Is.True.WithOutput(value).EqualTo("world"));
		/// Assert.That(dict.TryGetValue("not_found", out var value), Is.False.WithOutput(value).Default);
		/// </example>
		public static ConstraintExpression WithOutput(this Constraint self, object? actual, [CallerArgumentExpression(nameof(actual))] string literal = "")
		{
			var builder = self.Builder;
			if (builder == null)
			{
				builder = new ConstraintBuilder();
				builder.Append(self);
			}
			builder.Append(new WithOutputOperator(actual, literal));
			return new ConstraintExpression(builder);
		}

	}

}
