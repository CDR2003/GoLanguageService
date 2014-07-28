using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoField : GoNode
	{
		public GoCommentGroup Doc { get; set; }

		public List<GoIdentifier> Names { get; set; }

		public GoExpression Type { get; set; }

		public GoBasicLiteral Tag { get; set; }

		public GoCommentGroup Comment { get; set; }

		public override int Pos
		{
			get
			{
				if( this.Names.Count > 0 )
				{
					return this.Names[0].Pos;
				}
				return this.Type.Pos;
			}
		}

		public override int End
		{
			get
			{
				if( this.Tag != null )
				{
					return this.Tag.End;
				}
				return this.Type.End;
			}
		}
	}
}
