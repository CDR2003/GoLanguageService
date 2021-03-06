﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoFieldList : GoNode
	{
		public int Opening { get; set; }

		public List<GoField> List { get; set; }

		public int Closing { get; set; }

		public override int Pos
		{
			get
			{
				if( this.Opening != 0 )
				{
					return this.Opening;
				}
				if( this.List.Count > 0 )
				{
					return this.List.First().Pos;
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
				if( this.List.Count > 0 )
				{
					return this.List.Last().End;
				}
				return 0;
			}
		}

		public int NumFields
		{
			get
			{
				var n = 0;
				foreach( var g in this.List )
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
