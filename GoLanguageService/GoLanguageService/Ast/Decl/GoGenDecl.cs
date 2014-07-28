using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoGenDecl : GoDecl
	{
		public GoCommentGroup Doc { get; set; }

		public int TokPos { get; set; }

		public GoTokenID Tok { get; set; }

		public int Lparen { get; set; }

		public List<GoSpec> Specs { get; set; }

		public int Rparen { get; set; }

		public override int Pos
		{
			get
			{
				return this.TokPos;
			}
		}

		public override int End
		{
			get
			{
				if( this.Rparen != 0 )
				{
					return this.Rparen + 1;
				}
				return this.Specs.First().End;
			}
		}
	}
}
