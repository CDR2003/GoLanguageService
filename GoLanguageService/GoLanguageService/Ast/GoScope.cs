using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoScope
	{
		public GoScope Outer { get; set; }

		public Dictionary<string, GoObject> Objects { get; set; }

		public GoScope( GoScope outer )
		{
			this.Outer = outer;
			this.Objects = new Dictionary<string, GoObject>( 4 );
		}

		public GoObject Lookup( string name )
		{
			return this.Objects[name];
		}

		public GoObject Insert( GoObject obj )
		{
			var alt = this.Objects[obj.Name];
			if( alt == null )
			{
				this.Objects[obj.Name] = obj;
			}
			return alt;
		}
	}
}
