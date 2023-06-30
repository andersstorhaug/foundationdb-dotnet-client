#region Copyright (c) 2005-2023 Doxense SAS
//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//
#endregion

namespace Doxense.Reactive.Disposables
{
	using System;

	internal sealed class NullDisposable : IDisposable
	{
		public void Dispose()
		{
			// NO OP
		}
	}

}
