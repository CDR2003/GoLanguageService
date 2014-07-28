using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoBranchStmt : GoStmt
	{
		public int TokPos { get; set; }

		public GoTokenID Tok { get; set; }

		public GoIdent Label { get; set; }

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
				if( this.Label != null )
				{
					return this.Label.End;
				}
				return this.TokPos + GoToken.ToString( this.Tok ).Length;
			}
		}
	}
}
