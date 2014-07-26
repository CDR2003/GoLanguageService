// Guids.cs
// MUST match guids.h
using System;

namespace Fitbos.GoLanguageService
{
	static class GuidList
	{
		public const string guidGoLanguageServicePkgString = "d5d91170-63ed-4d94-8457-87d8992c5d91";
		public const string guidGoLanguageServiceCmdSetString = "440a8fb1-5998-4a37-beeb-26f9e1e4236d";

		public static readonly Guid guidGoLanguageServiceCmdSet = new Guid( guidGoLanguageServiceCmdSetString );
	};
}