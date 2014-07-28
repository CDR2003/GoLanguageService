using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoFieldList : GoNode
	{
		public int Opening { get; set; }

		public List<GoField> Fields { get; set; }

		public int Closing { get; set; }

		public override int Pos
		{
			get
			{
				if( this.Opening != 0 )
				{
					return this.Opening;
				}
				if( this.Fields.Count > 0 )
				{
					return this.Fields.First().Pos;
				}
				return 0;
			}
		}

		public override int End
		{
			get
			{
				if( this.Closing != 0 )
				{
					return this.Closing + 1;
				}
				if( this.Fields.Count > 0 )
				{
					return this.Fields.Last().End;
				}
				return 0;
			}
		}

		public int FieldCount
		{
			get
			{
				var n = 0;
				foreach( var g in this.Fields )
				{
					var m = g.Names.Count;
					if( m == 0 )
					{
						m = 1;
					}
					n += m;
				}
				return n;
			}
		}
	}
}
