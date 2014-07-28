using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoValueSpec : GoSpec
	{
		public GoCommentGroup Doc { get; set; }

		public List<GoIdent> Names { get; set; }

		public GoExpr Type { get; set; }

		public List<GoExpr> Values { get; set; }

		public GoCommentGroup Comment { get; set; }

		public override int Pos
		{
			get
			{
				return this.Names.First().Pos;
			}
		}

		public override int End
		{
			get
			{
				if( this.Values.Count > 0 )
				{
					return this.Values.Last().End;
				}
				if( this.Type != null )
				{
					return this.Type.End;
				}
				return this.Names.Last().End;
			}
		}
	}
}
