using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoImportSpec : GoSpec
	{
		public GoCommentGroup Doc { get; set; }

		public GoIdent Name { get; set; }

		public GoBasicLit Path { get; set; }

		public GoCommentGroup Comment { get; set; }

		public int EndPos { get; set; }

		public override int Pos
		{
			get
			{
				if( this.Name != null )
				{
					return this.Name.Pos;
				}
				return this.Path.Pos;
			}
		}

		public override int End
		{
			get
			{
				if( this.EndPos != 0 )
				{
					return this.EndPos;
				}
				return this.Path.End;
			}
		}
	}
}
