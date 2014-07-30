using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoComment : GoNode
	{
		public int Slash { get; set; }

		public string Text { get; set; }

		public override int Pos
		{
			get
			{
				return this.Slash;
			}
		}

		public override int End
		{
			get
			{
				return this.Slash + this.Text.Length;
			}
		}

		public GoComment( int slash, string text )
		{
			this.Slash = slash;
			this.Text = text;
		}
	}
}
