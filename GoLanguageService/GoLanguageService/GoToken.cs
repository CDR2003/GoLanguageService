using Microsoft.VisualStudio.Package;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoToken
	{
		public int ID { get; set; }

		public TokenType Type { get; set; }

		public int StartIndex { get; set; }

		public string Text { get; set; }
	}
}
